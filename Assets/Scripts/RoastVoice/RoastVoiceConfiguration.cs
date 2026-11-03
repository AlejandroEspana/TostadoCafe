using UnityEngine;

namespace ToastedVr.RoastVoice
{
    // Los límites del audio son los del contrato (IA/docs/unity-voice-contract.md); si el backend los cambia, se cambian aquí.
    [CreateAssetMenu(fileName = "RoastVoiceConfiguration", menuName = "Toasted VR/Roast Voice Configuration")]
    public sealed class RoastVoiceConfiguration : ScriptableObject
    {
        [Header("Backend")]
        [SerializeField] private string questionsPath;
        [Tooltip("The backend waits up to 30 s for the AI service; add a margin for the upload.")]
        [SerializeField, Min(1)] private int requestTimeoutSeconds;

        [Header("Feedback speech")]
        [Tooltip("{0} = session id.")]
        [SerializeField] private string feedbackSpeechPathFormat;
        [Tooltip("The backend waits up to 30 s for the speech; add a margin for the download.")]
        [SerializeField, Min(1)] private int feedbackSpeechTimeoutSeconds;

        [Header("Recording")]
        [SerializeField, Min(8000)] private int sampleRateHz;
        [SerializeField, Min(0f)] private float minRecordingSeconds;
        [SerializeField, Min(1f)] private float maxRecordingSeconds;
        [SerializeField, Min(1)] private int maxAudioBytes;

        public string QuestionsPath => questionsPath;
        public int RequestTimeoutSeconds => requestTimeoutSeconds;
        public string FeedbackSpeechPathFormat => feedbackSpeechPathFormat;
        public int FeedbackSpeechTimeoutSeconds => feedbackSpeechTimeoutSeconds;
        public int SampleRateHz => sampleRateHz;
        public float MinRecordingSeconds => minRecordingSeconds;
        public float MaxRecordingSeconds => maxRecordingSeconds;
        public int MaxAudioBytes => maxAudioBytes;
    }
}
