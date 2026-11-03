using System.Collections;
using System.Linq;
using NUnit.Framework;
using ToastedVr.RoastSimulation;
using ToastedVr.SessionRefresh;
using ToastedVr.SessionRefresh.Tests;
using UnityEngine.TestTools;

namespace ToastedVr.RoastAlerts.Tests
{
    public sealed class AlertPackageApiClientTests
    {
        private const string AlertsPath = "/api/v1/roasting/assistant/alerts";
        private const string RefreshPath = "/api/v1/auth/refresh";
        private const string OldAccess = "old-access-secret";
        private const string NewAccess = "new-access-secret";
        private const string ETag = "\"beginner-r4-abc\"";

        private const string RenewedBody =
            "{\"accessToken\":\"" + NewAccess + "\",\"tokenType\":\"Bearer\"," +
            "\"expiresAt\":\"2026-10-31T14:10:00Z\",\"refreshToken\":\"refresh-2\"}";

        private static readonly string PackageJson = RoastJson.Serialize(AlertTestData.PackageFor("BEGINNER"));

        private CoroutineDriver driver;
        private AlertPackageApiClient client;
        private int expiredEvents;

        private void CreateClient(FakeBackend backend)
        {
            var store = new FakeSessionTokenStore(OldAccess, "refresh-secret");
            driver = new CoroutineDriver();
            var refresher = new SessionRefresher(store, new SessionRefreshApiClient(backend.BaseUrl, RefreshPath, 5), driver.Start);
            var sender = new AuthorizedRequestSender(store, refresher);
            expiredEvents = 0;
            sender.SessionExpired += () => expiredEvents++;
            client = new AlertPackageApiClient(backend.BaseUrl, AlertsPath, 5, sender);
        }

        private RoastApiResult<AlertPackageDownload> result;

        private IEnumerator Fetch(string savedETag)
        {
            result = null;
            driver.Start(client.Fetch(savedETag, r => result = r));
            yield return driver.RunUntilDone();
        }

        [UnityTest]
        public IEnumerator DownloadsThePackageWithItsETag()
        {
            using (var backend = new FakeBackend(_ => (200, PackageJson)))
            {
                backend.ResponseHeaders["ETag"] = ETag;
                CreateClient(backend);

                yield return Fetch(null);

                Assert.IsTrue(result.IsSuccess);
                Assert.IsFalse(result.Value.IsNotModified);
                Assert.AreEqual(ETag, result.Value.ETag);
                Assert.AreEqual(18, result.Value.Package.Rules.Count);
                Assert.AreEqual(PackageJson, result.Value.Json);

                var request = backend.Requests.Single();
                Assert.AreEqual("GET", request.Method);
                Assert.AreEqual(AlertsPath, request.Path);
                Assert.AreEqual("Bearer " + OldAccess, request.Authorization);
                Assert.IsNull(request.IfNoneMatch);
            }
        }

        [UnityTest]
        public IEnumerator SendsTheSavedETagAndAcceptsNotModified()
        {
            using (var backend = new FakeBackend(_ => (304, null)))
            {
                CreateClient(backend);

                yield return Fetch(ETag);

                Assert.IsTrue(result.IsSuccess);
                Assert.IsTrue(result.Value.IsNotModified);
                Assert.AreEqual(ETag, backend.Requests.Single().IfNoneMatch);
            }
        }

        [UnityTest]
        public IEnumerator ReportsUnavailableWithTheBackendMessage()
        {
            using (var backend = new FakeBackend(_ => (503, "{\"code\":\"ALERTS_UNAVAILABLE\",\"message\":\"Sin alertas por ahora\"}")))
            {
                CreateClient(backend);

                yield return Fetch(ETag);

                Assert.AreEqual(RoastApiFailure.Unavailable, result.Failure);
                Assert.AreEqual("ALERTS_UNAVAILABLE", result.Error.Code);
            }
        }

        [UnityTest]
        public IEnumerator RejectsABodyThatIsNotAPackage()
        {
            using (var backend = new FakeBackend(_ => (200, "{\"hello\":1}")))
            {
                CreateClient(backend);

                yield return Fetch(null);

                Assert.AreEqual(RoastApiFailure.Unexpected, result.Failure);
            }
        }

        [UnityTest]
        public IEnumerator RejectsAnUnknownSchemaVersion()
        {
            var package = AlertTestData.PackageFor("BEGINNER");
            package.SchemaVersion = AlertPackage.SupportedSchemaVersion + 1;
            using (var backend = new FakeBackend(_ => (200, RoastJson.Serialize(package))))
            {
                CreateClient(backend);

                yield return Fetch(null);

                Assert.AreEqual(RoastApiFailure.Unexpected, result.Failure);
            }
        }

        [UnityTest]
        public IEnumerator RenewsTheSessionOnceOnUnauthorized()
        {
            using (var backend = new FakeBackend(request =>
                request.Path == RefreshPath ? (200, RenewedBody)
                : request.Authorization == "Bearer " + NewAccess ? (200, PackageJson) : (401, null)))
            {
                CreateClient(backend);

                yield return Fetch(null);

                Assert.IsTrue(result.IsSuccess);
                Assert.AreEqual(3, backend.Requests.Count);
                Assert.AreEqual(0, expiredEvents);
            }
        }

        [UnityTest]
        public IEnumerator ReportsUnauthorizedWhenTheSessionCannotBeRenewed()
        {
            using (var backend = new FakeBackend(_ => (401, null)))
            {
                CreateClient(backend);

                yield return Fetch(null);

                Assert.AreEqual(RoastApiFailure.Unauthorized, result.Failure);
                Assert.AreEqual(1, expiredEvents);
            }
        }

        [UnityTest]
        public IEnumerator ReportsUnavailableWhenTheServerIsDown()
        {
            FakeBackend backend;
            using (backend = new FakeBackend(_ => (200, PackageJson)))
            {
            }

            CreateClient(backend);

            yield return Fetch(null);

            Assert.AreEqual(RoastApiFailure.Unavailable, result.Failure);
        }
    }
}
