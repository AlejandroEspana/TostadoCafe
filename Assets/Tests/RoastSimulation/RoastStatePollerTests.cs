using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using NUnit.Framework;
using ToastedVr.SessionRefresh;
using ToastedVr.SessionRefresh.Tests;
using UnityEngine.TestTools;

namespace ToastedVr.RoastSimulation.Tests
{
    public sealed class RoastStatePollerTests
    {
        private const string SimulationId = "0b7f0f7e-8f6b-4c1e-9d1b-1d2c3e4f5a6b";
        private const float Interval = 0.01f;

        private CoroutineDriver driver;

        private RoastStatePoller CreatePoller(FakeBackend backend)
        {
            var store = new FakeSessionTokenStore("access", "refresh");
            driver = new CoroutineDriver();
            var refresher = new SessionRefresher(store, new SessionRefreshApiClient(backend.BaseUrl, "/api/v1/auth/refresh", 5), driver.Start);
            var client = new RoastSimulationApiClient(backend.BaseUrl, "/api/v1/roasting/simulations", 5, new AuthorizedRequestSender(store, refresher));
            return new RoastStatePoller(client, Interval);
        }

        private static string Tick(int tick, string phase) => "{\"tick\": " + tick + ", \"phase\": \"" + phase + "\"}";

        private static string PollBody(string phase, params int[] ticks) =>
            "{\"state\": " + Tick(ticks.Length > 0 ? ticks.Last() : 0, phase) + ", \"ticks\": [" + string.Join(",", ticks.Select(t => Tick(t, phase))) + "]}";

        [UnityTest]
        public IEnumerator AsksFromTheLastTickAndStopsWhenFinished()
        {
            var responses = new Queue<string>(new[]
            {
                PollBody("PREHEAT", 1, 2, 3),
                PollBody("ROASTING", 4),
                PollBody("ROASTING"),
                PollBody("FINISHED", 5)
            });
            using (var backend = new FakeBackend(_ => (200, responses.Dequeue())))
            {
                var poller = CreatePoller(backend);
                var phases = new List<string>();
                poller.StateReceived += state => phases.Add(state.Phase);

                driver.Start(poller.Run(SimulationId, 0));
                yield return driver.RunUntilDone();

                CollectionAssert.AreEqual(
                    new[] { "?afterTick=0", "?afterTick=3", "?afterTick=4", "?afterTick=4" },
                    backend.Requests.Select(r => r.Query));
                CollectionAssert.AreEqual(new[] { "PREHEAT", "ROASTING", "ROASTING", "FINISHED" }, phases);
                CollectionAssert.AreEqual(new[] { 1, 2, 3, 4, 5 }, poller.Buffer.Ticks.Select(t => t.Tick));
                Assert.IsFalse(poller.IsRunning);
            }
        }

        [UnityTest]
        public IEnumerator KeepsPollingWhileTheSimulatorIsDown()
        {
            var calls = 0;
            using (var backend = new FakeBackend(_ =>
            {
                var call = Interlocked.Increment(ref calls);
                return call <= 2 ? (503, "{\"code\": \"SIMULATOR_UNAVAILABLE\", \"message\": \"Sin simulador\"}") : (200, PollBody("FINISHED", 1));
            }))
            {
                var poller = CreatePoller(backend);
                var failures = new List<RoastApiFailure>();
                poller.PollFailed += result => failures.Add(result.Failure);

                driver.Start(poller.Run(SimulationId, 0));
                yield return driver.RunUntilDone();

                CollectionAssert.AreEqual(new[] { RoastApiFailure.Unavailable, RoastApiFailure.Unavailable }, failures);
                Assert.AreEqual(3, backend.Requests.Count);
                Assert.AreEqual("?afterTick=0", backend.Requests.Last().Query);
            }
        }

        [UnityTest]
        public IEnumerator StopsWhenTheSimulationIsGone()
        {
            using (var backend = new FakeBackend(_ => (404, "{\"code\": \"SIMULATION_NOT_FOUND\", \"message\": \"No existe\"}")))
            {
                var poller = CreatePoller(backend);
                RoastApiResult<SimulationPoll> failure = null;
                poller.PollFailed += result => failure = result;

                driver.Start(poller.Run(SimulationId, 0));
                yield return driver.RunUntilDone();

                Assert.AreEqual(RoastApiFailure.NotFound, failure.Failure);
                Assert.AreEqual("No existe", failure.Error.Message);
                Assert.AreEqual(1, backend.Requests.Count);
                Assert.IsFalse(poller.IsRunning);
            }
        }

        [UnityTest]
        public IEnumerator StopEndsTheLoopWithoutReportingLateAnswers()
        {
            using (var backend = new FakeBackend(_ => (200, PollBody("ROASTING", 1))))
            {
                var poller = CreatePoller(backend);
                var states = 0;
                poller.StateReceived += _ => states++;

                driver.Start(poller.Run(SimulationId, 0));
                poller.Stop();
                yield return driver.RunUntilDone();

                Assert.AreEqual(0, states);
                Assert.IsFalse(poller.IsRunning);
            }
        }
    }
}
