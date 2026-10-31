using System;
using System.Collections;
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
        private int? pendingGas;
        private bool gasInFlight;
        private bool showingOfflineMessage;
        private bool sessionExpired;

        public event Action Changed;
        public event Action<string> MessageChanged;
        public event Action<RoastSessionSummary> Finished;

        public RoastSimulationConfiguration Configuration => configuration;
        public RoastStationMessages Messages => messages;
        public string SimulationId { get; private set; }
        public RoastState State { get; private set; }
        public bool IsBusy { get; private set; }
        public bool IsReady => client != null && client.IsConfigured && !sessionExpired;
        public bool IsPowered => SimulationId != null;
        public string Phase => State?.Phase;

        public void Initialize(string backendBaseUrl, AuthorizedRequestSender sender)
        {
            client = new RoastSimulationApiClient(backendBaseUrl, configuration.SimulationsPath, configuration.RequestTimeoutSeconds, sender);
            poller = new RoastStatePoller(client, configuration.PollIntervalSeconds);
            poller.StateReceived += OnPolledState;
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
            if (IsReady && IsPowered && !IsBusy && RoastPhaseRules.CanCharge(Phase))
            {
                StartCoroutine(RunCharge());
            }
        }

        public void Discharge()
        {
            if (IsReady && IsPowered && !IsBusy && RoastPhaseRules.CanDischarge(Phase))
            {
                StartCoroutine(RunDischarge());
            }
        }

        public void ShowSessionExpired()
        {
            sessionExpired = true;
            poller?.Stop();
            SetMessage(messages.SessionExpiredMessage);
            Changed?.Invoke();
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
            ClearMessage();
            ApplyState(result.Value.State);
            StartCoroutine(poller.Run(SimulationId, result.Value.State?.Tick ?? 0));
        }

        private IEnumerator PowerOff()
        {
            var simulationId = SimulationId;
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

            while (true)
            {
                RoastApiResult<DischargeResult> result = null;
                yield return client.Discharge(SimulationId, null, r => result = r);

                if (result.IsSuccess)
                {
                    poller.Stop();
                    ClearMessage();
                    ApplyState(result.Value.State);
                    Finished?.Invoke(result.Value.Session);
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
            MessageChanged?.Invoke(message);
        }

        private void ClearMessage()
        {
            SetMessage(string.Empty);
        }

        private void OnDestroy()
        {
            poller?.Stop();
        }
    }
}
