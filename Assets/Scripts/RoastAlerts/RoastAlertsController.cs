using System.Collections;
using System.Collections.Generic;
using System.IO;
using ToastedVr.RoastSimulation;
using ToastedVr.RoastVoice;
using ToastedVr.SessionRefresh;
using UnityEngine;

namespace ToastedVr.RoastAlerts
{
    // Alertas proactivas: baja el paquete al encender, evalúa cada tick y entrega por el mismo parlante y subtítulo del asistente.
    public sealed class RoastAlertsController : MonoBehaviour
    {
        private const string AlertClipName = "ProactiveAlert";

        [SerializeField] private RoastAlertsConfiguration configuration;
        [SerializeField] private RoastStationController station;
        [SerializeField] private VoiceAssistantController voiceAssistant;
        [SerializeField] private VoiceAnswerPlayer player;

        private AlertPackageApiClient client;
        private AlertPackageStore store;
        private AlertEvaluator evaluator;
        private bool preparing;

        // Ticks que llegan mientras baja el paquete; se evalúan en orden cuando esté listo.
        private readonly List<RoastState> pendingTicks = new List<RoastState>();

        public void Initialize(string backendBaseUrl, AuthorizedRequestSender sender)
        {
            client = new AlertPackageApiClient(backendBaseUrl, configuration.AlertsPath, configuration.RequestTimeoutSeconds, sender);
            store = new AlertPackageStore(Path.Combine(Application.persistentDataPath, configuration.StorageFolderName));
            station.RoastStarted += OnRoastStarted;
            station.TickReceived += OnTick;
        }

        private void OnRoastStarted(string simulationId)
        {
            evaluator = null;
            pendingTicks.Clear();
            StopAllCoroutines();
            StartCoroutine(PreparePackage());
        }

        private IEnumerator PreparePackage()
        {
            preparing = true;
            var stored = store.Load();
            RoastApiResult<AlertPackageDownload> result = null;
            yield return client.Fetch(stored?.ETag, r => result = r);

            if (result.IsSuccess && !result.Value.IsNotModified && !store.Save(result.Value.Json, result.Value.ETag))
            {
                LogDiagnostic("The package could not be saved; it will be downloaded again next time.");
            }

            var (package, source) = AlertPackageChooser.Choose(stored, result);
            preparing = false;
            if (package == null)
            {
                LogDiagnostic($"No package available (Status={result.StatusCode}); this roast has no proactive alerts.");
                pendingTicks.Clear();
                yield break;
            }

            LogDiagnostic($"Package ready. Source={source}; Level={package.Level}; Revision={package.CatalogRevision}; Rules={package.Rules.Count}");
            evaluator = new AlertEvaluator(package);
            foreach (var tick in pendingTicks)
            {
                Evaluate(tick);
            }

            pendingTicks.Clear();
        }

        private void OnTick(RoastState tick)
        {
            if (preparing)
            {
                pendingTicks.Add(tick);
            }
            else if (evaluator != null)
            {
                Evaluate(tick);
            }
        }

        private void Evaluate(RoastState tick)
        {
            if (!AlertSampleFactory.TryCreate(tick, out var sample))
            {
                return;
            }

            var delivery = evaluator.Step(sample, voiceAssistant.IsRecording, voiceAssistant.IsBusy);
            if (delivery != null)
            {
                Deliver(delivery);
            }
        }

        private void Deliver(AlertDelivery delivery)
        {
            station.RecordDeliveredAlert(delivery.Rule.Id, delivery.RoastSeconds);
            voiceAssistant.ShowAlertSubtitle(delivery.Rule.Text);
            LogDiagnostic($"Delivered {delivery.Rule.Id} ({delivery.Rule.Priority}) at roast second {delivery.RoastSeconds} as {delivery.Mode}");

            // Grabando, solo subtítulo: así el transcriptor no oye la alerta. Una CRITICAL corta la respuesta que suene.
            if (delivery.Mode != AlertDeliveryModes.Voice)
            {
                return;
            }

            var clip = VoiceClipFactory.TryCreate(delivery.Rule.Audio, AlertClipName);
            if (clip != null)
            {
                player.Play(clip);
            }
        }

        private void OnDestroy()
        {
            if (station != null)
            {
                station.RoastStarted -= OnRoastStarted;
                station.TickReceived -= OnTick;
            }
        }

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        private static void LogDiagnostic(string message)
        {
            Debug.Log($"[RoastAlerts] {message}");
        }
    }
}
