using System;
using System.Collections;
using System.Collections.Generic;
using ToastedVr.SessionRefresh;
using UnityEngine;

namespace ToastedVr.RoastSimulation
{
    // Orquesta un tueste en modo manual: encendido, gas, carga y descarga contra el simulador del backend.
    public sealed class RoastStationController : MonoBehaviour
    {
        [SerializeField] private RoastSimulationConfiguration configuration;
        [SerializeField] private RoastStationMessages messages;

        private RoastSimulationApiClient client;
        private RoastStatePoller poller;
        private RoastSessionsApiClient sessionsClient;
        private RoastEvaluationPoller evaluationPoller;
        private int resultVersion;
        private DeliveredAlertLog deliveredAlerts;
        private int? pendingGas;
        private bool gasInFlight;
        private bool showingOfflineMessage;
        private bool sessionExpired;
        private int messageVersion;

        public event Action Changed;
        public event Action<string> MessageChanged;
        public event Action<RoastSessionSummary> Finished;

        // Para las alertas: cada tick nuevo en orden, y el inicio de cada simulación.
        public event Action<RoastState> TickReceived;
        public event Action<string> RoastStarted;

        // Para el resultado: la nota final cuando la IA termina de calificar (o se cansa de esperar) y la
        // retroalimentación, que es null si no estuvo disponible.
        public event Action<RoastSessionSummary> EvaluationUpdated;
        public event Action<string> FeedbackReceived;

        public RoastSimulationConfiguration Configuration => configuration;
        public RoastStationMessages Messages => messages;
        public string SimulationId { get; private set; }
        public RoastState State { get; private set; }
        public bool IsBusy { get; private set; }
        public bool IsReady => client != null && client.IsConfigured && !sessionExpired;
        public bool IsPowered => SimulationId != null;
        public string Phase => State?.Phase;
        public RoastSessionSummary LastSession { get; private set; }
        public bool EvaluationTimedOut { get; private set; }

        // Para las piezas que piden al backend con el mismo token, como la voz de la retroalimentación.
        public string BackendBaseUrl { get; private set; }
        public AuthorizedRequestSender Sender { get; private set; }

        // Ticks guardados del tueste actual, en orden, para la curva. Siguen ahí después de descargar.
        public IReadOnlyList<RoastState> Ticks => poller != null ? poller.Buffer.Ticks : Array.Empty<RoastState>();

        public void Initialize(string backendBaseUrl, AuthorizedRequestSender sender)
        {
            BackendBaseUrl = backendBaseUrl;
            Sender = sender;
            client = new RoastSimulationApiClient(backendBaseUrl, configuration.SimulationsPath, configuration.RequestTimeoutSeconds, sender);
            poller = new RoastStatePoller(client, configuration.PollIntervalSeconds);
            poller.StateReceived += OnPolledState;
            poller.TickReceived += tick => TickReceived?.Invoke(tick);
            deliveredAlerts = new DeliveredAlertLog(configuration.MaxDischargeAlerts, configuration.MaxAlertIdLength);
            sessionsClient = new RoastSessionsApiClient(backendBaseUrl, configuration.SessionsPath, configuration.RequestTimeoutSeconds,
                configuration.FeedbackTimeoutSeconds, sender);
            evaluationPoller = new RoastEvaluationPoller(sessionsClient, configuration.EvaluationPollIntervalSeconds, configuration.EvaluationPollMaxAttempts);
            evaluationPoller.Updated += OnEvaluationUpdated;
            evaluationPoller.TimedOut += OnEvaluationTimedOut;
            poller.PollFailed += result => HandleFailure(result.Failure, result.Error);
            Changed?.Invoke();
        }

        public void TogglePower()
        {
            if (!IsReady || IsBusy)
            {
                return;
            }

            StartCoroutine(IsPowered ? PowerOff() : PowerOn());
        }

        // Solo vale el último valor: si hay un envío en curso, el nuevo espera y reemplaza a los anteriores.
        public void RequestGas(int gasPercent)
        {
            if (!IsReady || !IsPowered || !RoastPhaseRules.CanChangeGas(Phase))
            {
                return;
            }

            pendingGas = Mathf.Clamp(gasPercent, configuration.MinGasPercent, configuration.MaxGasPercent);
            if (!gasInFlight)
            {
                StartCoroutine(SendPendingGas());
            }
        }

        public void Charge()
        {
            if (CanAttempt(RoastAction.Charge))
            {
                StartCoroutine(RunCharge());
            }
        }

        public void Discharge()
        {
            if (CanAttempt(RoastAction.Discharge))
            {
                StartCoroutine(RunDischarge());
            }
        }

        // Toda alerta entregada, por voz o en subtítulo, va en la traza de la descarga.
        public void RecordDeliveredAlert(string alertId, int roastSeconds)
        {
            if (IsPowered)
            {
                deliveredAlerts.Record(alertId, roastSeconds);
            }
        }

        public void ShowSessionExpired()
        {
            sessionExpired = true;
            poller?.Stop();
            SetMessage(messages.SessionExpiredMessage);
            Changed?.Invoke();
        }

        // Si la acción no vale en esta fase, el jugador ve por qué en vez de un botón que no responde.
        private bool CanAttempt(RoastAction action)
        {
            if (!IsReady || IsBusy)
            {
                return false;
            }

            var rejection = RoastActionRejection.MessageFor(action, IsPowered, Phase, messages);
            if (rejection == null)
            {
                return true;
            }

            SetMessage(rejection);
            StartCoroutine(ClearMessageLater(messageVersion));
            return false;
        }

        // Solo borra el rechazo si nadie mostró otro mensaje mientras tanto.
        private IEnumerator ClearMessageLater(int version)
        {
            yield return new WaitForSecondsRealtime(configuration.RejectionMessageSeconds);
            if (messageVersion == version)
            {
                ClearMessage();
            }
        }

        private IEnumerator PowerOn()
        {
            SetBusy(true);
            SetMessage(messages.ConnectingMessage);
            RoastApiResult<SimulationCreated> result = null;
            yield return client.Create(r => result = r);
            SetBusy(false);

            if (!result.IsSuccess)
            {
                HandleFailure(result.Failure, result.Error);
                yield break;
            }

            SimulationId = result.Value.SimulationId;
            ForgetResult();
            deliveredAlerts.Clear();
            ClearMessage();
            ApplyState(result.Value.State);
            RoastStarted?.Invoke(SimulationId);
            StartCoroutine(poller.Run(SimulationId, result.Value.State?.Tick ?? 0));
        }

        private IEnumerator PowerOff()
        {
            var simulationId = SimulationId;
            evaluationPoller.Stop();
            ResetStation();
            ClearMessage();

            // Si falla da igual: el servidor borra la simulación sola a los 120 s sin consultas.
            yield return client.Delete(simulationId, _ => { });
        }

        private IEnumerator SendPendingGas()
        {
            gasInFlight = true;
            while (pendingGas.HasValue && IsPowered)
            {
                var gas = pendingGas.Value;
                pendingGas = null;
                RoastApiResult<SimulationStateResponse> result = null;
                yield return client.SetGas(SimulationId, gas, r => result = r);
                HandleActionResult(result);
            }

            gasInFlight = false;
        }

        private IEnumerator RunCharge()
        {
            SetBusy(true);
            RoastApiResult<SimulationStateResponse> result = null;
            yield return client.Charge(SimulationId, r => result = r);
            SetBusy(false);
            HandleActionResult(result);
        }

        private IEnumerator RunDischarge()
        {
            SetBusy(true);
            SetMessage(messages.DischargingMessage);
            var retries = 0;
            var alerts = deliveredAlerts.Snapshot();

            while (true)
            {
                RoastApiResult<DischargeResult> result = null;
                yield return client.Discharge(SimulationId, alerts, r => result = r);

                if (result.IsSuccess)
                {
                    poller.Stop();
                    ClearMessage();
                    ApplyState(result.Value.State);
                    Finished?.Invoke(result.Value.Session);
                FollowResult(result.Value.Session);
                    break;
                }

                if (!DischargeRetryPolicy.ShouldRetry(result.Failure, Phase, retries, configuration.DischargeMaxRetries))
                {
                    HandleFailure(result.Failure, result.Error);
                    break;
                }

                retries++;
                yield return new WaitForSecondsRealtime(configuration.DischargeRetryDelaySeconds);
            }

            SetBusy(false);
        }

        private void FollowResult(RoastSessionSummary session)
        {
            LastSession = session;
            EvaluationTimedOut = false;
            if (session == null)
            {
                return;
            }

            if (RoastEvaluationPoller.IsPending(session))
            {
                StartCoroutine(evaluationPoller.Run(session.Id));
            }

            StartCoroutine(RequestFeedback(session.Id, resultVersion));
        }

        // Un tueste nuevo descarta lo que siga llegando del anterior.
        private void ForgetResult()
        {
            resultVersion++;
            evaluationPoller.Stop();
            LastSession = null;
            EvaluationTimedOut = false;
        }

        private IEnumerator RequestFeedback(long sessionId, int version)
        {
            RoastApiResult<RoastFeedback> result = null;
            yield return sessionsClient.GetFeedback(sessionId, r => result = r);
            if (version != resultVersion)
            {
                yield break;
            }

            var feedback = result.IsSuccess && !string.IsNullOrWhiteSpace(result.Value.Feedback) ? result.Value.Feedback.Trim() : null;
            FeedbackReceived?.Invoke(feedback);
        }

        private void OnEvaluationUpdated(RoastSessionSummary session)
        {
            LastSession = session;
            EvaluationUpdated?.Invoke(session);
        }

        private void OnEvaluationTimedOut()
        {
            EvaluationTimedOut = true;
            EvaluationUpdated?.Invoke(LastSession);
        }

        private void HandleActionResult(RoastApiResult<SimulationStateResponse> result)
        {
            if (result.IsSuccess)
            {
                ApplyState(result.Value.State);
            }
            else
            {
                HandleFailure(result.Failure, result.Error);
            }
        }

        private void OnPolledState(RoastState state)
        {
            if (showingOfflineMessage)
            {
                ClearMessage();
            }

            ApplyState(state);
        }

        private void HandleFailure(RoastApiFailure failure, ApiError error)
        {
            switch (failure)
            {
                case RoastApiFailure.Unauthorized:
                    ShowSessionExpired();
                    break;
                case RoastApiFailure.NotFound:
                    ResetStation();
                    SetMessage(error?.Message ?? messages.SimulationLostMessage);
                    break;
                case RoastApiFailure.Unavailable:
                    SetMessage(messages.OfflineMessage);
                    showingOfflineMessage = true;
                    break;
                default:
                    SetMessage(error?.Message ?? messages.UnexpectedErrorMessage);
                    break;
            }
        }

        private void ApplyState(RoastState state)
        {
            if (state == null || !IsPowered)
            {
                return;
            }

            State = state;
            Changed?.Invoke();
        }

        private void ResetStation()
        {
            poller.Stop();
            SimulationId = null;
            State = null;
            pendingGas = null;
            deliveredAlerts.Clear();
            Changed?.Invoke();
        }

        private void SetBusy(bool busy)
        {
            IsBusy = busy;
            Changed?.Invoke();
        }

        private void SetMessage(string message)
        {
            showingOfflineMessage = false;
            messageVersion++;
            MessageChanged?.Invoke(message);
        }

        private void ClearMessage()
        {
            SetMessage(string.Empty);
        }

        private void OnDestroy()
        {
            poller?.Stop();
            evaluationPoller?.Stop();
        }
    }
}
