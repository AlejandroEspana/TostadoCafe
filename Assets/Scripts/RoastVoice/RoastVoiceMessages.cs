using UnityEngine;

namespace ToastedVr.RoastVoice
{
    // Textos de la voz que no vienen del backend; los errores del backend ya traen su mensaje en español.
    [CreateAssetMenu(fileName = "RoastVoiceMessages", menuName = "Toasted VR/Roast Voice Messages")]
    public sealed class RoastVoiceMessages : ScriptableObject
    {
        [SerializeField] private string talkLabel;
        [SerializeField] private string listeningMessage;
        [SerializeField] private string thinkingMessage;
        [SerializeField] private string tooShortMessage;
        [SerializeField] private string tooLargeMessage;
        [SerializeField] private string noMicrophoneMessage;
        [SerializeField] private string microphonePermissionMessage;
        [SerializeField] private string offlineMessage;
        [SerializeField] private string unexpectedErrorMessage;
        [Tooltip("{0} = what the assistant understood.")]
        [SerializeField] private string questionFormat;

        public string TalkLabel => talkLabel;
        public string ListeningMessage => listeningMessage;
        public string ThinkingMessage => thinkingMessage;
        public string TooShortMessage => tooShortMessage;
        public string TooLargeMessage => tooLargeMessage;
        public string NoMicrophoneMessage => noMicrophoneMessage;
        public string MicrophonePermissionMessage => microphonePermissionMessage;
        public string OfflineMessage => offlineMessage;
        public string UnexpectedErrorMessage => unexpectedErrorMessage;
        public string QuestionFormat => questionFormat;
    }
}
