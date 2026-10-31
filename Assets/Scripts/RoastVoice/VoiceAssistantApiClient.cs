using System;
using System.Collections;
using System.Collections.Generic;
using ToastedVr.RoastSimulation;
using ToastedVr.SessionRefresh;
using UnityEngine;
using UnityEngine.Networking;

namespace ToastedVr.RoastVoice
{
    // Manda la pregunta al backend; el backend agrega el usuario y su nivel y se la pasa al servicio IA.
    public sealed class VoiceAssistantApiClient
    {
        private const string AudioPart = "audio";
        private const string ContextPart = "context";
        private const string AudioFileName = "question.wav";
        private const string AudioContentType = "audio/wav";

        private readonly string questionsUrl;
        private readonly int timeoutSeconds;
        private readonly AuthorizedRequestSender sender;

        public VoiceAssistantApiClient(string baseUrl, string questionsPath, int timeoutSeconds, AuthorizedRequestSender sender)
        {
            questionsUrl = BuildUrl(baseUrl, questionsPath);
            this.timeoutSeconds = timeoutSeconds;
            this.sender = sender;
        }

        public bool IsConfigured => questionsUrl != null;

        public IEnumerator Ask(byte[] wav, VoiceQuestionContext context, Action<RoastApiResult<VoiceAnswer>> onCompleted)
        {
            if (!IsConfigured)
            {
                onCompleted?.Invoke(new RoastApiResult<VoiceAnswer>(null, RoastApiFailure.Unexpected, 0, null));
                yield break;
            }

            var contextJson = RoastJson.Serialize(context);
            yield return sender.Send(() => CreateRequest(wav, contextJson), request => onCompleted?.Invoke(ToResult(request)));
        }

        private UnityWebRequest CreateRequest(byte[] wav, string contextJson)
        {
            var sections = new List<IMultipartFormSection>
            {
                new MultipartFormFileSection(AudioPart, wav, AudioFileName, AudioContentType),
                new MultipartFormDataSection(ContextPart, contextJson)
            };

            var request = UnityWebRequest.Post(questionsUrl, sections);
            request.timeout = timeoutSeconds;
            request.SetRequestHeader("Accept", "application/json");
            return request;
        }

        private RoastApiResult<VoiceAnswer> ToResult(UnityWebRequest request)
        {
            var reachedServer = request.result == UnityWebRequest.Result.Success || request.result == UnityWebRequest.Result.ProtocolError;
            var failure = RoastApiFailureClassifier.Classify(reachedServer, request.responseCode);
            var text = request.downloadHandler?.text;

            if (failure != RoastApiFailure.None)
            {
                LogDiagnostic(failure, request.responseCode);
                return new RoastApiResult<VoiceAnswer>(null, failure, request.responseCode, RoastJson.TryDeserialize<ApiError>(text));
            }

            var answer = RoastJson.TryDeserialize<VoiceAnswer>(text);
            if (answer == null || string.IsNullOrWhiteSpace(answer.Answer))
            {
                LogDiagnostic(RoastApiFailure.Unexpected, request.responseCode);
                return new RoastApiResult<VoiceAnswer>(null, RoastApiFailure.Unexpected, request.responseCode, null);
            }

            return new RoastApiResult<VoiceAnswer>(answer, RoastApiFailure.None, request.responseCode, null);
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

        // Nunca registra el audio, el contexto ni el token.
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        private void LogDiagnostic(RoastApiFailure failure, long statusCode)
        {
            Debug.Log($"[RoastVoice] Question failed. Category={failure}; Status={statusCode}; Url={questionsUrl}");
        }
    }
}
