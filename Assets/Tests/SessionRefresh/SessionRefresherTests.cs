using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;

namespace ToastedVr.SessionRefresh.Tests
{
    public sealed class SessionRefresherTests
    {
        private sealed class ScriptedRefreshApi : ISessionRefreshApi
        {
            private readonly SessionRefreshOutcome outcome;

            public ScriptedRefreshApi(SessionRefreshOutcome outcome)
            {
                this.outcome = outcome;
            }

            public int Calls { get; private set; }
            public string LastRefreshToken { get; private set; }

            public IEnumerator Refresh(string refreshToken, Action<SessionRefreshOutcome, SessionTokens> onCompleted)
            {
                Calls++;
                LastRefreshToken = refreshToken;
                yield return null;
                var tokens = outcome == SessionRefreshOutcome.Renewed
                    ? new SessionTokens { accessToken = "access-2", refreshToken = "refresh-2", tokenType = "Bearer", expiresAt = "2026-10-31T14:10:00Z" }
                    : null;
                onCompleted(outcome, tokens);
            }
        }

        // Simula el objeto persistente: guarda las corrutinas para avanzarlas cuando el test quiera.
        private readonly List<IEnumerator> hostRoutines = new List<IEnumerator>();

        [SetUp]
        public void ClearHost()
        {
            hostRoutines.Clear();
        }

        [Test]
        public void RenewedTokensAreSaved()
        {
            var store = new FakeSessionTokenStore("access-1", "refresh-1");
            var api = new ScriptedRefreshApi(SessionRefreshOutcome.Renewed);
            var refresher = new SessionRefresher(store, api, hostRoutines.Add);
            var outcome = SessionRefreshOutcome.Unavailable;

            RunAll(refresher.Refresh(result => outcome = result));

            Assert.AreEqual(SessionRefreshOutcome.Renewed, outcome);
            Assert.AreEqual("refresh-1", api.LastRefreshToken);
            Assert.AreEqual("access-2", store.AccessToken);
            Assert.AreEqual("refresh-2", store.RefreshToken);
            Assert.IsFalse(refresher.IsRefreshing);
        }

        [TestCase(SessionRefreshOutcome.Rejected)]
        [TestCase(SessionRefreshOutcome.Unavailable)]
        public void FailedRefreshKeepsCurrentTokens(SessionRefreshOutcome expected)
        {
            var store = new FakeSessionTokenStore("access-1", "refresh-1");
            var refresher = new SessionRefresher(store, new ScriptedRefreshApi(expected), hostRoutines.Add);
            var outcome = SessionRefreshOutcome.Renewed;

            RunAll(refresher.Refresh(result => outcome = result));

            Assert.AreEqual(expected, outcome);
            Assert.AreEqual(0, store.SaveCount);
            Assert.AreEqual("access-1", store.AccessToken);
        }

        [TestCase(null)]
        [TestCase("")]
        public void MissingRefreshTokenIsRejectedWithoutCallingBackend(string refreshToken)
        {
            var api = new ScriptedRefreshApi(SessionRefreshOutcome.Renewed);
            var refresher = new SessionRefresher(new FakeSessionTokenStore("access-1", refreshToken), api, hostRoutines.Add);
            var outcome = SessionRefreshOutcome.Renewed;

            RunAll(refresher.Refresh(result => outcome = result));

            Assert.AreEqual(SessionRefreshOutcome.Rejected, outcome);
            Assert.AreEqual(0, api.Calls);
        }

        [Test]
        public void ConcurrentCallersShareOneRefresh()
        {
            var store = new FakeSessionTokenStore("access-1", "refresh-1");
            var api = new ScriptedRefreshApi(SessionRefreshOutcome.Renewed);
            var refresher = new SessionRefresher(store, api, hostRoutines.Add);
            var first = SessionRefreshOutcome.Unavailable;
            var second = SessionRefreshOutcome.Unavailable;

            var firstCaller = refresher.Refresh(result => first = result);
            var secondCaller = refresher.Refresh(result => second = result);
            Assert.IsTrue(firstCaller.MoveNext());
            Assert.IsTrue(secondCaller.MoveNext());

            Assert.AreEqual(1, hostRoutines.Count);
            var hostedRefresh = hostRoutines[0];
            hostRoutines.Clear();
            RunAll(hostedRefresh);
            RunAll(firstCaller);
            RunAll(secondCaller);

            Assert.AreEqual(1, api.Calls);
            Assert.AreEqual(1, store.SaveCount);
            Assert.AreEqual(SessionRefreshOutcome.Renewed, first);
            Assert.AreEqual(SessionRefreshOutcome.Renewed, second);
        }

        [Test]
        public void NextRefreshUsesTheRotatedToken()
        {
            var store = new FakeSessionTokenStore("access-1", "refresh-1");
            var api = new ScriptedRefreshApi(SessionRefreshOutcome.Renewed);
            var refresher = new SessionRefresher(store, api, hostRoutines.Add);

            RunAll(refresher.Refresh(null));
            RunAll(refresher.Refresh(null));

            Assert.AreEqual(2, api.Calls);
            Assert.AreEqual("refresh-2", api.LastRefreshToken);
        }

        // Avanza una corrutina hasta el final, entrando en las anidadas y en las que se mandaron al objeto persistente.
        private void RunAll(IEnumerator routine)
        {
            var stack = new Stack<IEnumerator>();
            stack.Push(routine);
            var guard = 0;
            while (stack.Count > 0)
            {
                Assert.Less(guard++, 1000, "La corrutina no terminó");
                var current = stack.Peek();
                if (!current.MoveNext())
                {
                    stack.Pop();
                    continue;
                }

                if (current.Current is IEnumerator nested)
                {
                    stack.Push(nested);
                }

                foreach (var hosted in hostRoutines.ToArray())
                {
                    hostRoutines.Remove(hosted);
                    RunAll(hosted);
                }
            }
        }
    }
}
