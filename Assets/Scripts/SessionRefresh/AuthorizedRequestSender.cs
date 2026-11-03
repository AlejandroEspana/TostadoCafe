using System;
using System.Collections;
using UnityEngine.Networking;

namespace ToastedVr.SessionRefresh
{
    public sealed class AuthorizedRequestSender
    {
        private const long Unauthorized = 401;

        private readonly ISessionTokenStore store;
        private readonly SessionRefresher refresher;

        public AuthorizedRequestSender(ISessionTokenStore store, SessionRefresher refresher)
        {
            this.store = store;
            this.refresher = refresher;
        }

        public event Action SessionExpired;

        // createRequest se llama otra vez para repetir el pedido, porque un UnityWebRequest no se puede reenviar.
        // onCompleted recibe la respuesta final; el pedido se libera al volver.
        public IEnumerator Send(Func<UnityWebRequest> createRequest, Action<UnityWebRequest> onCompleted)
        {
            var usedToken = store.AccessToken;
            var request = createRequest();
            yield return SendWithToken(request, usedToken);

            if (request.responseCode == Unauthorized)
            {
                // Si otro pedido ya renovó mientras este viajaba, basta con repetir.
                var canRetry = true;
                if (store.AccessToken == usedToken)
                {
                    var outcome = SessionRefreshOutcome.Unavailable;
                    yield return refresher.Refresh(result => outcome = result);
                    canRetry = outcome == SessionRefreshOutcome.Renewed;
                    if (outcome == SessionRefreshOutcome.Rejected)
                    {
                        SessionExpired?.Invoke();
                    }
                }

                if (canRetry)
                {
                    request.Dispose();
                    request = createRequest();
                    yield return SendWithToken(request, store.AccessToken);
                    if (request.responseCode == Unauthorized)
                    {
                        SessionExpired?.Invoke();
                    }
                }
            }

            try
            {
                onCompleted?.Invoke(request);
            }
            finally
            {
                request.Dispose();
            }
        }

        private static IEnumerator SendWithToken(UnityWebRequest request, string accessToken)
        {
            request.SetRequestHeader("Authorization", "Bearer " + accessToken);
            var operation = request.SendWebRequest();
            while (!operation.isDone)
            {
                yield return null;
            }
        }
    }
}
