using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace ToastedVr.Authentication
{
    public enum UnityLoginFailure
    {
        InvalidCode,
        Connection,
        Timeout,
        Server,
        MalformedResponse
    }

    public sealed class UnityLoginApiClient
    {
        private const string LoginEndpoint = "/api/v1/auth/unity-login";
        private const int RequestTimeoutSeconds = 15;

        public IEnumerator Login(string baseUrl, string code, Action<UnityLoginResponse> onSuccess, Action<UnityLoginFailure> onFailure)
        {
            if (!TryCreateEndpoint(baseUrl, out var endpoint))
            {
                LogDiagnostic(UnityLoginFailure.Connection, 0, baseUrl);
                onFailure?.Invoke(UnityLoginFailure.Connection);
                yield break;
            }

            var requestBody = JsonUtility.ToJson(new UnityLoginRequest { code = code });
            using (var request = new UnityWebRequest(endpoint, UnityWebRequest.kHttpVerbPOST))
            {
                request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(requestBody));
                request.downloadHandler = new DownloadHandlerBuffer();
                request.timeout = RequestTimeoutSeconds;
                request.SetRequestHeader("Content-Type", "application/json");
                request.SetRequestHeader("Accept", "application/json");

                yield return request.SendWebRequest();

                if (request.result != UnityWebRequest.Result.Success || request.responseCode < 200 || request.responseCode >= 300)
                {
                    var failure = GetFailureCategory(request);
                    LogDiagnostic(failure, request.responseCode, endpoint);
                    onFailure?.Invoke(failure);
                    yield break;
                }

                UnityLoginResponse response;
                try
                {
                    response = JsonUtility.FromJson<UnityLoginResponse>(request.downloadHandler.text);
                }
                catch (ArgumentException)
                {
                    LogDiagnostic(UnityLoginFailure.MalformedResponse, request.responseCode, endpoint);
                    onFailure?.Invoke(UnityLoginFailure.MalformedResponse);
                    yield break;
                }

                if (response == null || string.IsNullOrWhiteSpace(response.accessToken))
                {
                    LogDiagnostic(UnityLoginFailure.MalformedResponse, request.responseCode, endpoint);
                    onFailure?.Invoke(UnityLoginFailure.MalformedResponse);
                    yield break;
                }

                onSuccess?.Invoke(response);
            }
        }

        private static bool TryCreateEndpoint(string baseUrl, out string endpoint)
        {
            endpoint = null;
            if (string.IsNullOrWhiteSpace(baseUrl))
            {
                return false;
            }

            var normalizedBaseUrl = baseUrl.Trim().TrimEnd('/');
            if (!Uri.TryCreate(normalizedBaseUrl, UriKind.Absolute, out var baseUri) ||
                (baseUri.Scheme != Uri.UriSchemeHttp && baseUri.Scheme != Uri.UriSchemeHttps))
            {
                return false;
            }

            endpoint = normalizedBaseUrl + LoginEndpoint;
            return true;
        }

        private static UnityLoginFailure GetFailureCategory(UnityWebRequest request)
        {
            if (request.result == UnityWebRequest.Result.ConnectionError)
            {
                return request.error != null && request.error.IndexOf("timeout", StringComparison.OrdinalIgnoreCase) >= 0
                    ? UnityLoginFailure.Timeout
                    : UnityLoginFailure.Connection;
            }

            if (request.result == UnityWebRequest.Result.ProtocolError)
            {
                return request.responseCode >= 500
                    ? UnityLoginFailure.Server
                    : UnityLoginFailure.InvalidCode;
            }

            return UnityLoginFailure.Connection;
        }

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        private static void LogDiagnostic(UnityLoginFailure category, long statusCode, string requestUrl)
        {
            var safeUrl = TryGetSafeUrl(requestUrl);
            Debug.Log($"[UnityLogin] Request failed. Category={category}; Status={statusCode}; Url={safeUrl}");
        }

        private static string TryGetSafeUrl(string requestUrl)
        {
            if (!Uri.TryCreate(requestUrl, UriKind.Absolute, out var uri))
            {
                return "invalid URL";
            }

            var port = uri.IsDefaultPort ? string.Empty : $":{uri.Port}";
            return $"{uri.Scheme}://{uri.Host}{port}{uri.AbsolutePath}";
        }
    }
}
