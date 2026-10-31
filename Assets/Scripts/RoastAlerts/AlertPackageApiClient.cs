using System;
using System.Collections;
using ToastedVr.RoastSimulation;
using ToastedVr.SessionRefresh;
using UnityEngine;
using UnityEngine.Networking;

namespace ToastedVr.RoastAlerts
{
    public sealed class AlertPackageDownload
    {
        public static readonly AlertPackageDownload NotModified = new AlertPackageDownload(null, null, null);

        public AlertPackageDownload(AlertPackage package, string json, string eTag)
        {
            Package = package;
            Json = json;
            ETag = eTag;
        }

        // null cuando el backend respondió 304: el guardado sigue vigente.
        public AlertPackage Package { get; }
        public string Json { get; }
        public string ETag { get; }
        public bool IsNotModified => Package == null;
    }

    // Descarga el paquete del nivel del jugador; el backend saca el nivel del token.
    public sealed class AlertPackageApiClient
    {
        private const long NotModifiedStatus = 304;

        private readonly string alertsUrl;
        private readonly int timeoutSeconds;
        private readonly AuthorizedRequestSender sender;

        public AlertPackageApiClient(string baseUrl, string alertsPath, int timeoutSeconds, AuthorizedRequestSender sender)
        {
            alertsUrl = BuildUrl(baseUrl, alertsPath);
            this.timeoutSeconds = timeoutSeconds;
            this.sender = sender;
        }

        public bool IsConfigured => alertsUrl != null;

        public IEnumerator Fetch(string savedETag, Action<RoastApiResult<AlertPackageDownload>> onCompleted)
        {
            if (!IsConfigured)
            {
                onCompleted?.Invoke(new RoastApiResult<AlertPackageDownload>(null, RoastApiFailure.Unexpected, 0, null));
                yield break;
            }

            yield return sender.Send(() => CreateRequest(savedETag), request => onCompleted?.Invoke(ToResult(request)));
        }

        private UnityWebRequest CreateRequest(string savedETag)
        {
            var request = UnityWebRequest.Get(alertsUrl);
            request.timeout = timeoutSeconds;
            request.SetRequestHeader("Accept", "application/json");
            if (!string.IsNullOrEmpty(savedETag))
            {
                request.SetRequestHeader("If-None-Match", savedETag);
            }

            return request;
        }

        private RoastApiResult<AlertPackageDownload> ToResult(UnityWebRequest request)
        {
            var reachedServer = request.result == UnityWebRequest.Result.Success || request.result == UnityWebRequest.Result.ProtocolError;
            if (reachedServer && request.responseCode == NotModifiedStatus)
            {
                return new RoastApiResult<AlertPackageDownload>(AlertPackageDownload.NotModified, RoastApiFailure.None, NotModifiedStatus, null);
            }

            var failure = RoastApiFailureClassifier.Classify(reachedServer, request.responseCode);
            var text = request.downloadHandler?.text;
            if (failure != RoastApiFailure.None)
            {
                LogDiagnostic(failure, request.responseCode);
                return new RoastApiResult<AlertPackageDownload>(null, failure, request.responseCode, RoastJson.TryDeserialize<ApiError>(text));
            }

            var package = RoastJson.TryDeserialize<AlertPackage>(text);
            if (!AlertPackage.IsUsable(package))
            {
                LogDiagnostic(RoastApiFailure.Unexpected, request.responseCode);
                return new RoastApiResult<AlertPackageDownload>(null, RoastApiFailure.Unexpected, request.responseCode, null);
            }

            var download = new AlertPackageDownload(package, text, request.GetResponseHeader("ETag"));
            return new RoastApiResult<AlertPackageDownload>(download, RoastApiFailure.None, request.responseCode, null);
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

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        private void LogDiagnostic(RoastApiFailure failure, long statusCode)
        {
            Debug.Log($"[RoastAlerts] Package download failed. Category={failure}; Status={statusCode}; Url={alertsUrl}");
        }
    }
}
