using System;
using System.Collections;
using ToastedVr.RoastSimulation;
using ToastedVr.SessionRefresh;
using UnityEngine;
using UnityEngine.Networking;

namespace ToastedVr.RoastVoice
{
    // Pide al backend la nota final y la retroalimentación en voz alta. No manda texto: el backend lo arma
    // desde la sesión guardada, así nadie gasta la cuota de voz con lo que quiera.
    public sealed class FeedbackSpeechApiClient
    {
        private readonly string baseUrl;
        private readonly string pathFormat;
        private readonly int timeoutSeconds;
        private readonly AuthorizedRequestSender sender;

        public FeedbackSpeechApiClient(string baseUrl, string pathFormat, int timeoutSeconds, AuthorizedRequestSender sender)
        {
            this.baseUrl = NormalizeBaseUrl(baseUrl);
            this.pathFormat = pathFormat;
            this.timeoutSeconds = timeoutSeconds;
            this.sender = sender;
        }

        public bool IsConfigured => baseUrl != null && !string.IsNullOrWhiteSpace(pathFormat);

        public IEnumerator Speak(long sessionId, Action<RoastApiResult<FeedbackSpeech>> onCompleted)
        {
            if (!IsConfigured)
            {
                onCompleted?.Invoke(new RoastApiResult<FeedbackSpeech>(null, RoastApiFailure.Unexpected, 0, null));
                yield break;
            }

            var url = baseUrl + "/" + string.Format(pathFormat, sessionId).Trim().TrimStart('/');
            yield return sender.Send(() => CreateRequest(url), request => onCompleted?.Invoke(ToResult(request, url)));
        }

        // Sin cuerpo: el backend acepta el POST vacío que manda UnityWebRequest.
        private UnityWebRequest CreateRequest(string url)
        {
            var request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST)
            {
                downloadHandler = new DownloadHandlerBuffer(),
                timeout = timeoutSeconds
            };
            request.SetRequestHeader("Accept", "application/json");
            return request;
        }

        private static RoastApiResult<FeedbackSpeech> ToResult(UnityWebRequest request, string url)
        {
            var reachedServer = request.result == UnityWebRequest.Result.Success || request.result == UnityWebRequest.Result.ProtocolError;
            var failure = RoastApiFailureClassifier.Classify(reachedServer, request.responseCode);
            var text = request.downloadHandler?.text;

            if (failure != RoastApiFailure.None)
            {
                LogDiagnostic(url, failure, request.responseCode);
                return new RoastApiResult<FeedbackSpeech>(null, failure, request.responseCode, RoastJson.TryDeserialize<ApiError>(text));
            }

            var speech = RoastJson.TryDeserialize<FeedbackSpeech>(text);
            if (speech?.Audio == null || string.IsNullOrWhiteSpace(speech.Audio.Base64))
            {
                LogDiagnostic(url, RoastApiFailure.Unexpected, request.responseCode);
                return new RoastApiResult<FeedbackSpeech>(null, RoastApiFailure.Unexpected, request.responseCode, null);
            }

            return new RoastApiResult<FeedbackSpeech>(speech, RoastApiFailure.None, request.responseCode, null);
        }

        private static string NormalizeBaseUrl(string baseUrl)
        {
            if (string.IsNullOrWhiteSpace(baseUrl))
            {
                return null;
            }

            var normalized = baseUrl.Trim().TrimEnd('/');
            return Uri.TryCreate(normalized, UriKind.Absolute, out var uri) && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
                ? normalized
                : null;
        }

        // Nunca registra el audio ni el token.
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        private static void LogDiagnostic(string url, RoastApiFailure failure, long statusCode)
        {
            Debug.Log($"[RoastVoice] Feedback speech failed. Category={failure}; Status={statusCode}; Url={url}");
        }
    }
}
