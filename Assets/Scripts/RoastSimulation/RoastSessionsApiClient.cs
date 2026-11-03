using System;
using System.Collections;
using ToastedVr.SessionRefresh;
using UnityEngine;
using UnityEngine.Networking;

namespace ToastedVr.RoastSimulation
{
    // Lee la sesión guardada y su retroalimentación con el token del jugador, igual que el historial web.
    public sealed class RoastSessionsApiClient
    {
        private readonly string sessionsUrl;
        private readonly int timeoutSeconds;
        private readonly int feedbackTimeoutSeconds;
        private readonly AuthorizedRequestSender sender;

        public RoastSessionsApiClient(string baseUrl, string sessionsPath, int timeoutSeconds, int feedbackTimeoutSeconds, AuthorizedRequestSender sender)
        {
            sessionsUrl = BuildUrl(baseUrl, sessionsPath);
            this.timeoutSeconds = timeoutSeconds;
            this.feedbackTimeoutSeconds = feedbackTimeoutSeconds;
            this.sender = sender;
        }

        public bool IsConfigured => sessionsUrl != null;

        public IEnumerator GetSession(long sessionId, Action<RoastApiResult<RoastSessionSummary>> onCompleted)
        {
            return Get(SessionUrl(sessionId), timeoutSeconds, onCompleted);
        }

        // La IA puede tardar cerca de dos minutos en escribirla, por eso lleva su propio timeout.
        public IEnumerator GetFeedback(long sessionId, Action<RoastApiResult<RoastFeedback>> onCompleted)
        {
            return Get(SessionUrl(sessionId) + "/feedback", feedbackTimeoutSeconds, onCompleted);
        }

        private string SessionUrl(long sessionId)
        {
            return sessionsUrl + "/" + sessionId;
        }

        private IEnumerator Get<T>(string url, int timeout, Action<RoastApiResult<T>> onCompleted) where T : class
        {
            if (!IsConfigured)
            {
                onCompleted?.Invoke(new RoastApiResult<T>(null, RoastApiFailure.Unexpected, 0, null));
                yield break;
            }

            yield return sender.Send(() => CreateRequest(url, timeout), request => onCompleted?.Invoke(ToResult<T>(request, url)));
        }

        private static UnityWebRequest CreateRequest(string url, int timeout)
        {
            var request = UnityWebRequest.Get(url);
            request.timeout = timeout;
            request.SetRequestHeader("Accept", "application/json");
            return request;
        }

        private static RoastApiResult<T> ToResult<T>(UnityWebRequest request, string url) where T : class
        {
            var reachedServer = request.result == UnityWebRequest.Result.Success || request.result == UnityWebRequest.Result.ProtocolError;
            var failure = RoastApiFailureClassifier.Classify(reachedServer, request.responseCode);
            var text = request.downloadHandler?.text;

            if (failure != RoastApiFailure.None)
            {
                LogDiagnostic(url, failure, request.responseCode);
                return new RoastApiResult<T>(null, failure, request.responseCode, RoastJson.TryDeserialize<ApiError>(text));
            }

            var value = RoastJson.TryDeserialize<T>(text);
            if (value == null)
            {
                LogDiagnostic(url, RoastApiFailure.Unexpected, request.responseCode);
                return new RoastApiResult<T>(null, RoastApiFailure.Unexpected, request.responseCode, null);
            }

            return new RoastApiResult<T>(value, RoastApiFailure.None, request.responseCode, null);
        }

        private static string BuildUrl(string baseUrl, string path)
        {
            if (string.IsNullOrWhiteSpace(baseUrl) || string.IsNullOrWhiteSpace(path))
            {
                return null;
            }

            var normalizedBaseUrl = baseUrl.Trim().TrimEnd('/');
            if (!Uri.TryCreate(normalizedBaseUrl, UriKind.Absolute, out var baseUri) ||
                (baseUri.Scheme != Uri.UriSchemeHttp && baseUri.Scheme != Uri.UriSchemeHttps))
            {
                return null;
            }

            return normalizedBaseUrl + "/" + path.Trim().Trim('/');
        }

        // El token va en la cabecera y nunca se registra.
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        private static void LogDiagnostic(string url, RoastApiFailure failure, long statusCode)
        {
            Debug.Log($"[RoastSimulation] GET failed. Category={failure}; Status={statusCode}; Url={url}");
        }
    }
}
