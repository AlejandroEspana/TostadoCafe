using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ToastedVr.SessionRefresh;
using ToastedVr.SessionRefresh.Tests;
using UnityEngine.TestTools;

namespace ToastedVr.RoastSimulation.Tests
{
    public sealed class RoastSimulationApiClientTests
    {
        private const string SimulationsPath = "/api/v1/roasting/simulations";
        private const string RefreshPath = "/api/v1/auth/refresh";
        private const string SimulationId = "0b7f0f7e-8f6b-4c1e-9d1b-1d2c3e4f5a6b";
        private const string OldAccess = "old-access-secret";
        private const string NewAccess = "new-access-secret";
        private const string RefreshToken = "refresh-secret";
        private const string State = "{\"tick\": 12, \"phase\": \"PREHEAT\", \"roastSeconds\": 0, \"gasPercent\": 40}";

        private const string RenewedBody =
            "{\"accessToken\":\"" + NewAccess + "\",\"tokenType\":\"Bearer\"," +
            "\"expiresAt\":\"2026-10-31T14:10:00Z\",\"refreshToken\":\"refresh-2\"}";

        private CoroutineDriver driver;
        private RoastSimulationApiClient client;
        private int expiredEvents;

        private void CreateClient(FakeBackend backend)
        {
            var store = new FakeSessionTokenStore(OldAccess, RefreshToken);
            driver = new CoroutineDriver();
            var refresher = new SessionRefresher(store, new SessionRefreshApiClient(backend.BaseUrl, RefreshPath, 5), driver.Start);
            var sender = new AuthorizedRequestSender(store, refresher);
            expiredEvents = 0;
            sender.SessionExpired += () => expiredEvents++;
            client = new RoastSimulationApiClient(backend.BaseUrl, SimulationsPath, 5, sender);
        }

        private IEnumerator Run(IEnumerator routine)
        {
            driver.Start(routine);
            yield return driver.RunUntilDone();
        }

        private static FakeBackend.ReceivedRequest Single(FakeBackend backend, string path)
        {
            return backend.Requests.Single(request => request.Path == path);
        }

        [UnityTest]
        public IEnumerator CreatePostsWithTheTokenAndReadsTheSimulation()
        {
            using (var backend = new FakeBackend(_ => (201, "{\"simulationId\": \"" + SimulationId + "\", \"state\": " + State + "}")))
            {
                CreateClient(backend);
                RoastApiResult<SimulationCreated> result = null;

                yield return Run(client.Create(r => result = r));

                Assert.IsTrue(result.IsSuccess);
                Assert.AreEqual(SimulationId, result.Value.SimulationId);
                Assert.AreEqual(12, result.Value.State.Tick);
                var request = Single(backend, SimulationsPath);
                Assert.AreEqual("POST", request.Method);
                Assert.AreEqual("Bearer " + OldAccess, request.Authorization);
            }
        }

        [UnityTest]
        public IEnumerator PollSendsAfterTickAndReadsTicks()
        {
            string query = null;
            using (var backend = new FakeBackend(request => (200, "{\"state\": " + State + ", \"ticks\": [" + State + "]}")))
            {
                CreateClient(backend);
                RoastApiResult<SimulationPoll> result = null;

                yield return Run(client.Poll(SimulationId, 11, r => result = r));

                Assert.IsTrue(result.IsSuccess);
                Assert.AreEqual(1, result.Value.Ticks.Count);
                var request = Single(backend, SimulationsPath + "/" + SimulationId);
                Assert.AreEqual("GET", request.Method);
                query = request.Query;
            }

            Assert.AreEqual("?afterTick=11", query);
        }

        [UnityTest]
        public IEnumerator ActionsUseTheContractRoutesAndBodies()
        {
            using (var backend = new FakeBackend(request => request.Method == "DELETE" ? (204, "") : (200, "{\"state\": " + State + "}")))
            {
                CreateClient(backend);
                var results = new List<bool>();

                yield return Run(client.SetGas(SimulationId, 55, r => results.Add(r.IsSuccess)));
                yield return Run(client.Charge(SimulationId, r => results.Add(r.IsSuccess)));
                yield return Run(client.Delete(SimulationId, r => results.Add(r.IsSuccess)));

                CollectionAssert.AreEqual(new[] { true, true, true }, results);
                var gas = Single(backend, SimulationsPath + "/" + SimulationId + "/gas");
                Assert.AreEqual("PUT", gas.Method);
                Assert.AreEqual("{\"gasPercent\":55}", gas.Body);
                Assert.AreEqual("POST", Single(backend, SimulationsPath + "/" + SimulationId + "/charge").Method);
                Assert.AreEqual("DELETE", Single(backend, SimulationsPath + "/" + SimulationId).Method);
            }
        }

        [UnityTest]
        public IEnumerator DischargeSendsAlertsAndReadsTheSession()
        {
            using (var backend = new FakeBackend(_ => (200,
                "{\"state\": " + State + ", \"session\": {\"id\": 7, \"finalScore\": 80, \"grade\": \"GOOD\", \"evaluation\": {\"status\": \"PENDING\"}}}")))
            {
                CreateClient(backend);
                RoastApiResult<DischargeResult> result = null;

                yield return Run(client.Discharge(SimulationId, null, r => result = r));

                Assert.IsTrue(result.IsSuccess);
                Assert.AreEqual(80, result.Value.Session.FinalScore);
                Assert.AreEqual("{\"alerts\":[]}", Single(backend, SimulationsPath + "/" + SimulationId + "/discharge").Body);
            }
        }

        [UnityTest]
        public IEnumerator BackendErrorsCarryTheirSpanishMessage(
            [Values(400, 404, 409, 503)] int status)
        {
            using (var backend = new FakeBackend(_ => (status, "{\"code\": \"SOME_CODE\", \"message\": \"Mensaje para el jugador.\"}")))
            {
                CreateClient(backend);
                RoastApiResult<SimulationStateResponse> result = null;

                yield return Run(client.Charge(SimulationId, r => result = r));

                Assert.IsFalse(result.IsSuccess);
                Assert.AreEqual(RoastApiFailureClassifier.Classify(true, status), result.Failure);
                Assert.AreEqual("Mensaje para el jugador.", result.Error.Message);
            }
        }

        [UnityTest]
        public IEnumerator UnauthorizedRefreshesAndRepeatsTheRequest()
        {
            using (var backend = new FakeBackend(request =>
                request.Path == RefreshPath ? (200, RenewedBody)
                : request.Authorization == "Bearer " + NewAccess ? (200, "{\"state\": " + State + ", \"ticks\": []}")
                : (401, "")))
            using (var logs = new TokenLogWatcher())
            {
                CreateClient(backend);
                RoastApiResult<SimulationPoll> result = null;

                yield return Run(client.Poll(SimulationId, 0, r => result = r));

                Assert.IsTrue(result.IsSuccess);
                Assert.AreEqual(2, backend.Requests.Count(r => r.Path.StartsWith(SimulationsPath)));
                Assert.AreEqual(0, expiredEvents);
                logs.AssertNeverLogged(OldAccess, NewAccess, RefreshToken);
            }
        }

        [UnityTest]
        public IEnumerator RejectedRefreshExpiresTheSession()
        {
            using (var backend = new FakeBackend(_ => (401, "")))
            {
                CreateClient(backend);
                RoastApiResult<SimulationPoll> result = null;

                yield return Run(client.Poll(SimulationId, 0, r => result = r));

                Assert.AreEqual(RoastApiFailure.Unauthorized, result.Failure);
                Assert.AreEqual(1, expiredEvents);
            }
        }

        [UnityTest]
        public IEnumerator UnreachableBackendIsUnavailable()
        {
            string baseUrl;
            using (var backend = new FakeBackend(_ => (200, "{}")))
            {
                baseUrl = backend.BaseUrl;
                CreateClient(backend);
            }

            RoastApiResult<SimulationPoll> result = null;
            yield return Run(client.Poll(SimulationId, 0, r => result = r));

            Assert.AreEqual(RoastApiFailure.Unavailable, result.Failure);
            Assert.AreEqual(0, expiredEvents);
            StringAssert.StartsWith("http://127.0.0.1:", baseUrl);
        }

        [UnityTest]
        public IEnumerator MalformedSuccessIsUnexpected()
        {
            using (var backend = new FakeBackend(_ => (200, "<html>proxy</html>")))
            {
                CreateClient(backend);
                RoastApiResult<SimulationPoll> result = null;

                yield return Run(client.Poll(SimulationId, 0, r => result = r));

                Assert.AreEqual(RoastApiFailure.Unexpected, result.Failure);
            }
        }

        [UnityTest]
        public IEnumerator InvalidConfigurationFailsWithoutSending()
        {
            using (var backend = new FakeBackend(_ => (200, "{}")))
            {
                CreateClient(backend);
                var store = new FakeSessionTokenStore(OldAccess, RefreshToken);
                var sender = new AuthorizedRequestSender(store, new SessionRefresher(store, new SessionRefreshApiClient(backend.BaseUrl, RefreshPath, 5), driver.Start));
                var misconfigured = new RoastSimulationApiClient("not a url", SimulationsPath, 5, sender);
                RoastApiResult<SimulationCreated> result = null;

                Assert.IsFalse(misconfigured.IsConfigured);
                yield return Run(misconfigured.Create(r => result = r));

                Assert.AreEqual(RoastApiFailure.Unexpected, result.Failure);
                Assert.AreEqual(0, backend.Requests.Count);
            }
        }
    }
}
