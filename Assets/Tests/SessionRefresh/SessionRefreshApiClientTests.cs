using System.Collections;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace ToastedVr.SessionRefresh.Tests
{
    public sealed class SessionRefreshApiClientTests
    {
        private const string RefreshPath = "/api/v1/auth/refresh";
        private const string OldRefreshToken = "old-refresh-token-secret";
        private const string NewAccessToken = "new-access-token-secret";
        private const string NewRefreshToken = "new-refresh-token-secret";
        private const int TimeoutSeconds = 5;

        private const string RenewedBody =
            "{\"accessToken\":\"" + NewAccessToken + "\",\"tokenType\":\"Bearer\"," +
            "\"expiresAt\":\"2026-10-31T14:10:00Z\",\"refreshToken\":\"" + NewRefreshToken + "\"}";

        private const string ErrorBody = "{\"code\":\"AUTHENTICATION_FAILED\",\"message\":\"El refresh token no es válido.\"}";

        [UnityTest]
        public IEnumerator SendsRefreshTokenAndReturnsRotatedTokens()
        {
            using (var backend = new FakeBackend(_ => (200, RenewedBody)))
            using (var logs = new TokenLogWatcher())
            {
                var client = new SessionRefreshApiClient(backend.BaseUrl, RefreshPath, TimeoutSeconds);
                var outcome = SessionRefreshOutcome.Unavailable;
                SessionTokens tokens = null;

                yield return client.Refresh(OldRefreshToken, (o, t) => { outcome = o; tokens = t; });

                Assert.AreEqual(SessionRefreshOutcome.Renewed, outcome);
                Assert.AreEqual(NewAccessToken, tokens.accessToken);
                Assert.AreEqual(NewRefreshToken, tokens.refreshToken);
                Assert.AreEqual("2026-10-31T14:10:00Z", tokens.expiresAt);

                Assert.IsTrue(backend.Requests.TryDequeue(out var request));
                Assert.AreEqual("POST", request.Method);
                Assert.AreEqual(RefreshPath, request.Path);
                Assert.IsNull(request.Authorization);
                StringAssert.Contains(OldRefreshToken, request.Body);
                logs.AssertNeverLogged(OldRefreshToken, NewAccessToken, NewRefreshToken);
            }
        }

        [UnityTest]
        public IEnumerator ToleratesTrailingSlashesInConfiguration()
        {
            using (var backend = new FakeBackend(_ => (200, RenewedBody)))
            {
                var client = new SessionRefreshApiClient(backend.BaseUrl + "/", "api/v1/auth/refresh", TimeoutSeconds);

                yield return client.Refresh(OldRefreshToken, (o, t) => { });

                Assert.IsTrue(backend.Requests.TryDequeue(out var request));
                Assert.AreEqual(RefreshPath, request.Path);
            }
        }

        [UnityTest]
        public IEnumerator UnauthorizedIsRejected([Values(400, 401, 403)] int status)
        {
            using (var backend = new FakeBackend(_ => (status, ErrorBody)))
            using (var logs = new TokenLogWatcher())
            {
                var client = new SessionRefreshApiClient(backend.BaseUrl, RefreshPath, TimeoutSeconds);
                var outcome = SessionRefreshOutcome.Renewed;

                yield return client.Refresh(OldRefreshToken, (o, t) => outcome = o);

                Assert.AreEqual(SessionRefreshOutcome.Rejected, outcome);
                logs.AssertNeverLogged(OldRefreshToken);
            }
        }

        [UnityTest]
        public IEnumerator ServerErrorIsUnavailable([Values(500, 503)] int status)
        {
            using (var backend = new FakeBackend(_ => (status, ErrorBody)))
            {
                var client = new SessionRefreshApiClient(backend.BaseUrl, RefreshPath, TimeoutSeconds);
                var outcome = SessionRefreshOutcome.Renewed;

                yield return client.Refresh(OldRefreshToken, (o, t) => outcome = o);

                Assert.AreEqual(SessionRefreshOutcome.Unavailable, outcome);
            }
        }

        [UnityTest]
        public IEnumerator MalformedResponseIsUnavailable()
        {
            using (var backend = new FakeBackend(_ => (200, "{\"accessToken\":\"only-access\"}")))
            {
                var client = new SessionRefreshApiClient(backend.BaseUrl, RefreshPath, TimeoutSeconds);
                var outcome = SessionRefreshOutcome.Renewed;

                yield return client.Refresh(OldRefreshToken, (o, t) => outcome = o);

                Assert.AreEqual(SessionRefreshOutcome.Unavailable, outcome);
            }
        }

        [UnityTest]
        public IEnumerator UnreachableBackendIsUnavailable()
        {
            string unusedBaseUrl;
            using (var backend = new FakeBackend(_ => (200, RenewedBody)))
            {
                unusedBaseUrl = backend.BaseUrl;
            }

            var client = new SessionRefreshApiClient(unusedBaseUrl, RefreshPath, TimeoutSeconds);
            var outcome = SessionRefreshOutcome.Renewed;

            yield return client.Refresh(OldRefreshToken, (o, t) => outcome = o);

            Assert.AreEqual(SessionRefreshOutcome.Unavailable, outcome);
        }

        [UnityTest]
        public IEnumerator InvalidBaseUrlIsUnavailableWithoutSending()
        {
            var client = new SessionRefreshApiClient("not a url", RefreshPath, TimeoutSeconds);
            var outcome = SessionRefreshOutcome.Renewed;

            yield return client.Refresh(OldRefreshToken, (o, t) => outcome = o);

            Assert.AreEqual(SessionRefreshOutcome.Unavailable, outcome);
        }
    }
}
