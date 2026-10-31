using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine.Networking;
using UnityEngine.TestTools;

namespace ToastedVr.SessionRefresh.Tests
{
    public sealed class AuthorizedRequestSenderTests
    {
        private const string RefreshPath = "/api/v1/auth/refresh";
        private const string ResourcePath = "/api/v1/roasting/simulations";
        private const string OldAccess = "old-access-secret";
        private const string OldRefresh = "old-refresh-secret";
        private const string NewAccess = "new-access-secret";
        private const string NewRefresh = "new-refresh-secret";
        private const int TimeoutSeconds = 5;

        private const string RenewedBody =
            "{\"accessToken\":\"" + NewAccess + "\",\"tokenType\":\"Bearer\"," +
            "\"expiresAt\":\"2026-10-31T14:10:00Z\",\"refreshToken\":\"" + NewRefresh + "\"}";

        private FakeSessionTokenStore store;
        private CoroutineDriver driver;
        private AuthorizedRequestSender sender;
        private int expiredEvents;

        private void CreateSender(FakeBackend backend)
        {
            store = new FakeSessionTokenStore(OldAccess, OldRefresh);
            driver = new CoroutineDriver();
            var api = new SessionRefreshApiClient(backend.BaseUrl, RefreshPath, TimeoutSeconds);
            sender = new AuthorizedRequestSender(store, new SessionRefresher(store, api, driver.Start));
            expiredEvents = 0;
            sender.SessionExpired += () => expiredEvents++;
        }

        private IEnumerator SendResource(FakeBackend backend, System.Action<long> onStatus)
        {
            driver.Start(sender.Send(
                () => UnityWebRequest.Get(backend.BaseUrl + ResourcePath),
                request => onStatus(request.responseCode)));
            yield return driver.RunUntilDone();
        }

        private static int Count(FakeBackend backend, string path)
        {
            return backend.Requests.Count(request => request.Path == path);
        }

        [UnityTest]
        public IEnumerator ValidTokenPassesThroughWithoutRefreshing()
        {
            using (var backend = new FakeBackend(_ => (200, "{}")))
            {
                CreateSender(backend);
                long status = 0;

                yield return SendResource(backend, code => status = code);

                Assert.AreEqual(200, status);
                Assert.AreEqual(1, backend.Requests.Count);
                Assert.IsTrue(backend.Requests.TryPeek(out var request));
                Assert.AreEqual("Bearer " + OldAccess, request.Authorization);
                Assert.AreEqual(0, expiredEvents);
            }
        }

        [UnityTest]
        public IEnumerator UnauthorizedRefreshesOnceAndRetriesWithNewToken()
        {
            using (var backend = new FakeBackend(request =>
                request.Path == RefreshPath ? (200, RenewedBody)
                : request.Authorization == "Bearer " + NewAccess ? (200, "{}")
                : (401, "")))
            using (var logs = new TokenLogWatcher())
            {
                CreateSender(backend);
                long status = 0;

                yield return SendResource(backend, code => status = code);

                Assert.AreEqual(200, status);
                Assert.AreEqual(1, Count(backend, RefreshPath));
                Assert.AreEqual(2, Count(backend, ResourcePath));
                Assert.AreEqual("Bearer " + NewAccess, backend.Requests.Last().Authorization);
                Assert.AreEqual(NewRefresh, store.RefreshToken);
                Assert.AreEqual(0, expiredEvents);
                logs.AssertNeverLogged(OldAccess, OldRefresh, NewAccess, NewRefresh);
            }
        }

        [UnityTest]
        public IEnumerator RejectedRefreshExpiresSessionWithoutRetrying()
        {
            using (var backend = new FakeBackend(request => request.Path == RefreshPath ? (401, "{}") : (401, "")))
            using (var logs = new TokenLogWatcher())
            {
                CreateSender(backend);
                long status = 0;

                yield return SendResource(backend, code => status = code);

                Assert.AreEqual(401, status);
                Assert.AreEqual(1, Count(backend, ResourcePath));
                Assert.AreEqual(1, expiredEvents);
                Assert.AreEqual(OldAccess, store.AccessToken);
                logs.AssertNeverLogged(OldAccess, OldRefresh);
            }
        }

        [UnityTest]
        public IEnumerator SecondUnauthorizedDoesNotLoopAndExpiresSession()
        {
            using (var backend = new FakeBackend(request => request.Path == RefreshPath ? (200, RenewedBody) : (401, "")))
            {
                CreateSender(backend);
                long status = 0;

                yield return SendResource(backend, code => status = code);

                Assert.AreEqual(401, status);
                Assert.AreEqual(1, Count(backend, RefreshPath));
                Assert.AreEqual(2, Count(backend, ResourcePath));
                Assert.AreEqual(1, expiredEvents);
            }
        }

        [UnityTest]
        public IEnumerator UnavailableRefreshKeepsSessionAndReturnsOriginalResponse()
        {
            using (var backend = new FakeBackend(request => request.Path == RefreshPath ? (503, "{}") : (401, "")))
            {
                CreateSender(backend);
                long status = 0;

                yield return SendResource(backend, code => status = code);

                Assert.AreEqual(401, status);
                Assert.AreEqual(1, Count(backend, ResourcePath));
                Assert.AreEqual(0, expiredEvents);
                Assert.AreEqual(OldRefresh, store.RefreshToken);
            }
        }

        [UnityTest]
        public IEnumerator TokenRenewedElsewhereIsRetriedWithoutRefreshing()
        {
            FakeSessionTokenStore sharedStore = null;
            using (var backend = new FakeBackend(request =>
            {
                if (request.Authorization == "Bearer " + OldAccess)
                {
                    sharedStore.Save(new SessionTokens { accessToken = NewAccess, refreshToken = NewRefresh });
                    return (401, "");
                }

                return (200, "{}");
            }))
            {
                CreateSender(backend);
                sharedStore = store;
                long status = 0;

                yield return SendResource(backend, code => status = code);

                Assert.AreEqual(200, status);
                Assert.AreEqual(0, Count(backend, RefreshPath));
                Assert.AreEqual(2, Count(backend, ResourcePath));
            }
        }

        [UnityTest]
        public IEnumerator ConcurrentUnauthorizedRequestsShareOneRefresh()
        {
            using (var backend = new FakeBackend(request =>
                request.Path == RefreshPath ? (200, RenewedBody)
                : request.Authorization == "Bearer " + NewAccess ? (200, "{}")
                : (401, "")))
            {
                CreateSender(backend);
                long first = 0;
                long second = 0;

                driver.Start(sender.Send(() => UnityWebRequest.Get(backend.BaseUrl + ResourcePath), r => first = r.responseCode));
                driver.Start(sender.Send(() => UnityWebRequest.Get(backend.BaseUrl + ResourcePath), r => second = r.responseCode));
                yield return driver.RunUntilDone();

                Assert.AreEqual(200, first);
                Assert.AreEqual(200, second);
                Assert.AreEqual(1, Count(backend, RefreshPath));
                Assert.AreEqual(0, expiredEvents);
            }
        }
    }
}
