using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using ToastedVr.SessionRefresh;
using UnityEngine;
using UnityEngine.Networking;

namespace ToastedVr.RoastSimulation
{
    // Solo transporte: arma cada pedido del contrato y lo manda con el token por AuthorizedRequestSender,
    // que renueva una vez si llega un 401.
    public sealed class RoastSimulationApiClient
    {
        private readonly string simulationsUrl;
        private readonly int timeoutSeconds;
        private readonly AuthorizedRequestSender sender;

        public RoastSimulationApiClient(string baseUrl, string simulationsPath, int timeoutSeconds, AuthorizedRequestSender sender)
        {
            simulationsUrl = BuildUrl(baseUrl, simulationsPath);
            this.timeoutSeconds = timeoutSeconds;
            this.sender = sender;
        }

        public bool IsConfigured => simulationsUrl != null;

        public IEnumerator Create(Action<RoastApiResult<SimulationCreated>> onCompleted)
        {
            return Send(UnityWebRequest.kHttpVerbPOST, simulationsUrl, null, onCompleted);
        }

        public IEnumerator Poll(string simulationId, int afterTick, Action<RoastApiResult<SimulationPoll>> onCompleted)
        {
            return Send(UnityWebRequest.kHttpVerbGET, SimulationUrl(simulationId) + "?afterTick=" + afterTick, null, onCompleted);
        }

        public IEnumerator SetGas(string simulationId, int gasPercent, Action<RoastApiResult<SimulationStateResponse>> onCompleted)
        {
            var body = RoastJson.Serialize(new GasRequest { GasPercent = gasPercent });
            return Send(UnityWebRequest.kHttpVerbPUT, SimulationUrl(simulationId) + "/gas", body, onCompleted);
        }

        public IEnumerator Charge(string simulationId, Action<RoastApiResult<SimulationStateResponse>> onCompleted)
        {
            return Send(UnityWebRequest.kHttpVerbPOST, SimulationUrl(simulationId) + "/charge", null, onCompleted);
        }

        public IEnumerator Discharge(string simulationId, List<ProactiveAlert> alerts, Action<RoastApiResult<DischargeResult>> onCompleted)
        {
            var body = RoastJson.Serialize(new DischargeRequest { Alerts = alerts ?? new List<ProactiveAlert>() });
            return Send(UnityWebRequest.kHttpVerbPOST, SimulationUrl(simulationId) + "/discharge", body, onCompleted);
        }

        public IEnumerator Delete(string simulationId, Action<RoastApiResult<SimulationStateResponse>> onCompleted)
        {
            return Send(UnityWebRequest.kHttpVerbDELETE, SimulationUrl(simulationId), null, onCompleted);
        }

        private string SimulationUrl(string simulationId)
        {
            return simulationsUrl + "/" + Uri.EscapeDataString(simulationId ?? string.Empty);
        }

        private IEnumerator Send<T>(string method, string url, string jsonBody, Action<RoastApiResult<T>> onCompleted) where T : class
        {
            if (!IsConfigured)
            {
                onCompleted?.Invoke(new RoastApiResult<T>(null, RoastApiFailure.Unexpected, 0, null));
                yield break;
            }

            yield return sender.Send(
                () => CreateRequest(method, url, jsonBody),
                request => onCompleted?.Invoke(ToResult<T>(request, method, url)));
        }

        private UnityWebRequest CreateRequest(string method, string url, string jsonBody)
        {
            var request = new UnityWebRequest(url, method)
            {
                downloadHandler = new DownloadHandlerBuffer(),
                timeout = timeoutSeconds
            };

            if (jsonBody != null)
            {
                request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(jsonBody));
                request.SetRequestHeader("Content-Type", "application/json");
            }

            request.SetRequestHeader("Accept", "application/json");
            return request;
        }

        private static RoastApiResult<T> ToResult<T>(UnityWebRequest request, string method, string url) where T : class
        {
            var reachedServer = request.result == UnityWebRequest.Result.Success || request.result == UnityWebRequest.Result.ProtocolError;
            var failure = RoastApiFailureClassifier.Classify(reachedServer, request.responseCode);
            var text = request.downloadHandler?.text;

            if (failure != RoastApiFailure.None)
            {
                LogDiagnostic(method, url, failure, request.responseCode);
                return new RoastApiResult<T>(null, failure, request.responseCode, RoastJson.TryDeserialize<ApiError>(text));
            }

            if (request.responseCode == 204)
            {
                return new RoastApiResult<T>(null, RoastApiFailure.None, request.responseCode, null);
            }

            var value = RoastJson.TryDeserialize<T>(text);
            if (value == null)
            {
                LogDiagnostic(method, url, RoastApiFailure.Unexpected, request.responseCode);
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

        // La URL sin query; el token va en la cabecera y nunca se registra.
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        private static void LogDiagnostic(string method, string url, RoastApiFailure failure, long statusCode)
        {
            var queryStart = url.IndexOf('?');
            var safeUrl = queryStart >= 0 ? url.Substring(0, queryStart) : url;
            Debug.Log($"[RoastSimulation] {method} failed. Category={failure}; Status={statusCode}; Url={safeUrl}");
        }
    }
}
