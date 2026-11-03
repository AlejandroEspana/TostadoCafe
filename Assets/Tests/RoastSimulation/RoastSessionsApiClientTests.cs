using System.Collections;
using System.Linq;
using NUnit.Framework;
using ToastedVr.SessionRefresh;
using ToastedVr.SessionRefresh.Tests;
using UnityEngine.TestTools;

namespace ToastedVr.RoastSimulation.Tests
{
    public sealed class RoastSessionsApiClientTests
    {
        private const string SessionsPath = "/api/v1/roasting/sessions";
        private const string RefreshPath = "/api/v1/auth/refresh";
        private const string Access = "access-secret";

        private CoroutineDriver driver;
        private RoastSessionsApiClient client;

        private void CreateClient(FakeBackend backend)
        {
            var store = new FakeSessionTokenStore(Access, "refresh-secret");
            driver = new CoroutineDriver();
            var refresher = new SessionRefresher(store, new SessionRefreshApiClient(backend.BaseUrl, RefreshPath, 5), driver.Start);
            client = new RoastSessionsApiClient(backend.BaseUrl, SessionsPath, 5, 5, new AuthorizedRequestSender(store, refresher));
        }

        private IEnumerator Run(IEnumerator routine)
        {
            driver.Start(routine);
            yield return driver.RunUntilDone();
        }

        [UnityTest]
        public IEnumerator GetSessionReadsTheEvaluationWithTheToken()
        {
            using (var backend = new FakeBackend(_ => (200,
                "{\"id\": 42, \"finalScore\": 77, \"grade\": \"GOOD\", \"evaluation\": {\"status\": \"COMPLETED\", \"rulesScore\": 80}}")))
            {
                CreateClient(backend);
                RoastApiResult<RoastSessionSummary> result = null;

                yield return Run(client.GetSession(42, r => result = r));

                Assert.IsTrue(result.IsSuccess);
                Assert.AreEqual(80, result.Value.Evaluation.RulesScore);
                var request = backend.Requests.Single();
                Assert.AreEqual("GET", request.Method);
                Assert.AreEqual(SessionsPath + "/42", request.Path);
                Assert.AreEqual("Bearer " + Access, request.Authorization);
            }
        }

        [UnityTest]
        public IEnumerator GetFeedbackUsesTheFeedbackRoute()
        {
            using (var backend = new FakeBackend(_ => (200, "{\"feedback\": \"Buen control del gas.\"}")))
            {
                CreateClient(backend);
                RoastApiResult<RoastFeedback> result = null;

                yield return Run(client.GetFeedback(42, r => result = r));

                Assert.IsTrue(result.IsSuccess);
                Assert.AreEqual("Buen control del gas.", result.Value.Feedback);
                Assert.AreEqual(SessionsPath + "/42/feedback", backend.Requests.Single().Path);
            }
        }

        [UnityTest]
        public IEnumerator UnavailableFeedbackCarriesTheBackendMessage()
        {
            using (var backend = new FakeBackend(_ => (503, "{\"code\": \"AI_UNAVAILABLE\", \"message\": \"La IA no responde.\"}")))
            {
                CreateClient(backend);
                RoastApiResult<RoastFeedback> result = null;

                yield return Run(client.GetFeedback(42, r => result = r));

                Assert.AreEqual(RoastApiFailure.Unavailable, result.Failure);
                Assert.AreEqual("La IA no responde.", result.Error.Message);
            }
        }

        [Test]
        public void ClientWithoutBackendIsNotConfigured()
        {
            Assert.IsFalse(new RoastSessionsApiClient("", SessionsPath, 5, 5, new AuthorizedRequestSender(null, null)).IsConfigured);
            Assert.IsFalse(new RoastSessionsApiClient("ftp://host", SessionsPath, 5, 5, new AuthorizedRequestSender(null, null)).IsConfigured);
        }
    }
}
