using System.Collections;
using ToastedVr.RoastSimulation;
using UnityEngine;

namespace ToastedVr.RoastVoice
{
    // Lee en voz alta la nota final y la retroalimentación una vez por tueste, cuando ya están las dos.
    // Si el jugador está preguntando o suena otra voz, espera; el texto ya se ve en el panel del resultado.
    public sealed class FeedbackSpeechPlayer : MonoBehaviour
    {
        private const string ClipName = "FeedbackSpeech";

        [SerializeField] private RoastVoiceConfiguration configuration;
        [SerializeField] private RoastStationController station;
        [SerializeField] private VoiceAssistantController voiceAssistant;
        [SerializeField] private VoiceAnswerPlayer player;

        private FeedbackSpeechApiClient client;
        private string feedback;
        private bool requested;
        private int version;

        // La nota se lee solo cuando es la final: con la IA ya terminada o cansada de esperar.
        public static bool IsReadyToSpeak(string feedback, RoastSessionSummary session, bool evaluationTimedOut)
        {
            return !string.IsNullOrWhiteSpace(feedback) && session != null
                && (!RoastEvaluationPoller.IsPending(session) || evaluationTimedOut);
        }

        private void OnEnable()
        {
            station.FeedbackReceived += OnFeedbackReceived;
            station.EvaluationUpdated += OnEvaluationUpdated;
            station.RoastStarted += OnRoastStarted;
        }

        private void OnDisable()
        {
            station.FeedbackReceived -= OnFeedbackReceived;
            station.EvaluationUpdated -= OnEvaluationUpdated;
            station.RoastStarted -= OnRoastStarted;
        }

        private void OnFeedbackReceived(string text)
        {
            feedback = text;
            TrySpeak();
        }

        private void OnEvaluationUpdated(RoastSessionSummary session)
        {
            TrySpeak();
        }

        private void OnRoastStarted(string simulationId)
        {
            version++;
            feedback = null;
            requested = false;
        }

        private void TrySpeak()
        {
            var session = station.LastSession;
            if (requested || !IsReadyToSpeak(feedback, session, station.EvaluationTimedOut))
            {
                return;
            }

            client ??= new FeedbackSpeechApiClient(station.BackendBaseUrl, configuration.FeedbackSpeechPathFormat,
                configuration.FeedbackSpeechTimeoutSeconds, station.Sender);
            requested = true;
            StartCoroutine(Speak(session.Id, version));
        }

        private IEnumerator Speak(long sessionId, int myVersion)
        {
            RoastApiResult<FeedbackSpeech> result = null;
            yield return client.Speak(sessionId, r => result = r);
            if (myVersion != version || !result.IsSuccess)
            {
                yield break;
            }

            var clip = VoiceClipFactory.TryCreate(result.Value.Audio, ClipName);
            if (clip == null)
            {
                yield break;
            }

            while (player.IsPlaying || voiceAssistant.IsRecording || voiceAssistant.IsBusy)
            {
                yield return null;
            }

            if (myVersion != version)
            {
                Destroy(clip);
                yield break;
            }

            player.Play(clip);
        }
    }
}
