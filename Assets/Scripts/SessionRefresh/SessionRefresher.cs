using System;
using System.Collections;

namespace ToastedVr.SessionRefresh
{
    // El backend rota el refresh token en cada uso: dos renovaciones a la vez harían que la segunda
    // llegue con un token ya invalidado y saque al jugador. Por eso solo corre una y las demás la esperan.
    public sealed class SessionRefresher
    {
        private readonly ISessionTokenStore store;
        private readonly ISessionRefreshApi api;
        private readonly Action<IEnumerator> runOnPersistentHost;
        private SessionRefreshOutcome lastOutcome;

        // runOnPersistentHost debe correr la renovación en un objeto que no se destruya: si la corrutina se corta
        // después de que el backend rotó el token, el nuevo se perdería.
        public SessionRefresher(ISessionTokenStore store, ISessionRefreshApi api, Action<IEnumerator> runOnPersistentHost)
        {
            this.store = store;
            this.api = api;
            this.runOnPersistentHost = runOnPersistentHost;
        }

        public bool IsRefreshing { get; private set; }

        public IEnumerator Refresh(Action<SessionRefreshOutcome> onCompleted)
        {
            if (!IsRefreshing)
            {
                IsRefreshing = true;
                runOnPersistentHost(RunRefresh());
            }

            while (IsRefreshing)
            {
                yield return null;
            }

            onCompleted?.Invoke(lastOutcome);
        }

        private IEnumerator RunRefresh()
        {
            var refreshToken = store.RefreshToken;
            if (string.IsNullOrWhiteSpace(refreshToken))
            {
                Finish(SessionRefreshOutcome.Rejected);
                yield break;
            }

            var outcome = SessionRefreshOutcome.Unavailable;
            SessionTokens tokens = null;
            yield return api.Refresh(refreshToken, (result, received) =>
            {
                outcome = result;
                tokens = received;
            });

            if (outcome == SessionRefreshOutcome.Renewed)
            {
                store.Save(tokens);
            }

            Finish(outcome);
        }

        private void Finish(SessionRefreshOutcome outcome)
        {
            lastOutcome = outcome;
            IsRefreshing = false;
        }
    }
}
