using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace ToastedVr.SessionRefresh
{
    public sealed class SessionRefreshApiClient : ISessionRefreshApi
    {
        private const long BadRequest = 400;
        private const long Unauthorized = 401;
        private const long Forbidden = 403;

        private readonly string endpoint;
        private readonly int timeoutSeconds;

        public SessionRefreshApiClient(string baseUrl, string refreshPath, int timeoutSeconds)
        {
            endpoint = BuildEndpoint(baseUrl, refreshPath);
            this.timeoutSeconds = timeoutSeconds;
        }

        public IEnumerator Refresh(string refreshToken, Action<SessionRefreshOutcome, SessionTokens> onCompleted)
        {
            if (endpoint == null)
            {
                LogDiagnostic("InvalidConfiguration", 0);
                onCompleted?.Invoke(SessionRefreshOutcome.Unavailable, null);
                yield break;
            }

            var body = JsonUtility.ToJson(new SessionRefreshRequest { refreshToken = refreshToken });
            using (var request = new UnityWebRequest(endpoint, UnityWebRequest.kHttpVerbPOST))
            {
                request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(body));
                request.downloadHandler = new DownloadHandlerBuffer();
                request.timeout = timeoutSeconds;
                request.SetRequestHeader("Content-Type", "application/json");
                request.SetRequestHeader("Accept", "application/json");

                // Se espera así y no con yield de la operación para que también funcione en los tests de edit mode.
                var operation = request.SendWebRequest();
                while (!operation.isDone)
                {
                    yield return null;
                }

                if (request.result == UnityWebRequest.Result.Success)
                {
                    var tokens = TryParse(request.downloadHandler.text);
                    if (tokens == null)
                    {
                        LogDiagnostic("MalformedResponse", request.responseCode);
                    }

                    onCompleted?.Invoke(tokens != null ? SessionRefreshOutcome.Renewed : SessionRefreshOutcome.Unavailable, tokens);
                    yield break;
                }

                var outcome = IsRejection(request.responseCode) ? SessionRefreshOutcome.Rejected : SessionRefreshOutcome.Unavailable;
                LogDiagnostic(outcome.ToString(), request.responseCode);
                onCompleted?.Invoke(outcome, null);
            }
        }

        private static bool IsRejection(long statusCode)
        {
            return statusCode == BadRequest || statusCode == Unauthorized || statusCode == Forbidden;
        }

        private static SessionTokens TryParse(string json)
        {
            try
            {
                var tokens = JsonUtility.FromJson<SessionTokens>(json);
                return tokens != null && !string.IsNullOrWhiteSpace(tokens.accessToken) && !string.IsNullOrWhiteSpace(tokens.refreshToken)
                    ? tokens
                    : null;
            }
            catch (ArgumentException)
            {
                return null;
            }
        }

        private static string BuildEndpoint(string baseUrl, string refreshPath)
        {
            if (string.IsNullOrWhiteSpace(baseUrl) || string.IsNullOrWhiteSpace(refreshPath))
            {
                return null;
            }

            var normalizedBaseUrl = baseUrl.Trim().TrimEnd('/');
            if (!Uri.TryCreate(normalizedBaseUrl, UriKind.Absolute, out var baseUri) ||
                (baseUri.Scheme != Uri.UriSchemeHttp && baseUri.Scheme != Uri.UriSchemeHttps))
            {
                return null;
            }

            return normalizedBaseUrl + "/" + refreshPath.Trim().TrimStart('/');
        }

        // Nunca registra el cuerpo: lleva el refresh token.
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        private void LogDiagnostic(string category, long statusCode)
        {
            Debug.Log($"[SessionRefresh] Refresh failed. Category={category}; Status={statusCode}; Url={endpoint ?? "invalid URL"}");
        }
    }
}
