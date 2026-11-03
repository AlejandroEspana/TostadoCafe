using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ToastedVr.SessionRefresh;
using ToastedVr.SessionRefresh.Tests;
using UnityEngine.TestTools;

namespace ToastedVr.RoastSimulation.Tests
{
    public sealed class RoastEvaluationPollerTests
    {
        private const string Pending = "{\"id\": 42, \"finalScore\": 80, \"evaluation\": {\"status\": \"PENDING\"}}";
        private const string Completed = "{\"id\": 42, \"finalScore\": 77, \"evaluation\": {\"status\": \"COMPLETED\", \"finalScore\": 77}}";

        private CoroutineDriver driver;
        private readonly List<RoastSessionSummary> updates = new List<RoastSessionSummary>();
        private int timeouts;

        private RoastEvaluationPoller CreatePoller(FakeBackend backend, int maxAttempts)
        {
            var store = new FakeSessionTokenStore("access", "refresh");
            driver = new CoroutineDriver();
            var refresher = new SessionRefresher(store, new SessionRefreshApiClient(backend.BaseUrl, "/api/v1/auth/refresh", 5), driver.Start);
            var client = new RoastSessionsApiClient(backend.BaseUrl, "/api/v1/roasting/sessions", 5, 5, new AuthorizedRequestSender(store, refresher));
            var poller = new RoastEvaluationPoller(client, 0.01f, maxAttempts);
            updates.Clear();
            timeouts = 0;
            poller.Updated += updates.Add;
            poller.TimedOut += () => timeouts++;
            return poller;
        }

        [UnityTest]
        public IEnumerator AsksUntilTheFinalScoreArrives()
        {
            var responses = new Queue<string>(new[] { Pending, Pending, Completed });
            using (var backend = new FakeBackend(_ => (200, responses.Dequeue())))
            {
                var poller = CreatePoller(backend, 24);

                driver.Start(poller.Run(42));
                yield return driver.RunUntilDone();

                Assert.AreEqual(3, backend.Requests.Count);
                Assert.IsTrue(backend.Requests.All(r => r.Path == "/api/v1/roasting/sessions/42"));
                Assert.AreEqual(1, updates.Count);
                Assert.AreEqual("COMPLETED", updates[0].Evaluation.Status);
                Assert.AreEqual(0, timeouts);
            }
        }

        [UnityTest]
        public IEnumerator GivesUpAfterTheMaxAttempts()
        {
            using (var backend = new FakeBackend(_ => (200, Pending)))
            {
                var poller = CreatePoller(backend, 3);

                driver.Start(poller.Run(42));
                yield return driver.RunUntilDone();

                Assert.AreEqual(3, backend.Requests.Count);
                Assert.AreEqual(0, updates.Count);
                Assert.AreEqual(1, timeouts);
            }
        }

        [UnityTest]
        public IEnumerator NetworkFailuresAreRetried()
        {
            var responses = new Queue<(int, string)>(new[] { (503, ""), (200, Completed) });
            using (var backend = new FakeBackend(_ => responses.Dequeue()))
            {
                var poller = CreatePoller(backend, 24);

                driver.Start(poller.Run(42));
                yield return driver.RunUntilDone();

                Assert.AreEqual(2, backend.Requests.Count);
                Assert.AreEqual(1, updates.Count);
            }
        }

        [UnityTest]
        public IEnumerator MissingSessionStopsWithoutTimingOut()
        {
            using (var backend = new FakeBackend(_ => (404, "{\"code\": \"SESSION_NOT_FOUND\", \"message\": \"No existe.\"}")))
            {
                var poller = CreatePoller(backend, 24);

                driver.Start(poller.Run(42));
                yield return driver.RunUntilDone();

                Assert.AreEqual(1, backend.Requests.Count);
                Assert.AreEqual(0, updates.Count);
                Assert.AreEqual(0, timeouts);
            }
        }

        [UnityTest]
        public IEnumerator StopBeforeTheFirstAnswerReportsNothing()
        {
            using (var backend = new FakeBackend(_ => (200, Completed)))
            {
                var poller = CreatePoller(backend, 24);

                driver.Start(poller.Run(42));
                poller.Stop();
                yield return driver.RunUntilDone();

                Assert.AreEqual(0, backend.Requests.Count);
                Assert.AreEqual(0, updates.Count);
                Assert.AreEqual(0, timeouts);
            }
        }

        [TestCase("PENDING", true)]
        [TestCase("COMPLETED", false)]
        [TestCase("FAILED", false)]
        [TestCase("NOT_APPLICABLE", false)]
        public void OnlyPendingEvaluationsAreFollowed(string status, bool pending)
        {
            var session = new RoastSessionSummary { Evaluation = new RoastEvaluationSummary { Status = status } };

            Assert.AreEqual(pending, RoastEvaluationPoller.IsPending(session));
        }

        [Test]
        public void SessionsWithoutEvaluationAreNotFollowed()
        {
            Assert.IsFalse(RoastEvaluationPoller.IsPending(new RoastSessionSummary()));
            Assert.IsFalse(RoastEvaluationPoller.IsPending(null));
        }
    }
}
