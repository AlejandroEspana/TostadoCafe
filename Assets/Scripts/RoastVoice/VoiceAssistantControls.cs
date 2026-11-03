using TMPro;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace ToastedVr.RoastVoice
{
    // Botón de mantener presionado para preguntar y subtítulo con la respuesta.
    public sealed class VoiceAssistantControls : MonoBehaviour
    {
        [SerializeField] private VoiceAssistantController assistant;
        [SerializeField] private XRSimpleInteractable talkButton;
        [SerializeField] private TMP_Text talkLabel;
        [SerializeField] private TMP_Text subtitleText;

        private void OnEnable()
        {
            talkButton.selectEntered.AddListener(OnPressed);
            talkButton.selectExited.AddListener(OnReleased);
            assistant.Changed += Refresh;
            assistant.SubtitleChanged += ShowSubtitle;
            subtitleText.text = string.Empty;
            Refresh();
        }

        private void OnDisable()
        {
            talkButton.selectEntered.RemoveListener(OnPressed);
            talkButton.selectExited.RemoveListener(OnReleased);
            assistant.Changed -= Refresh;
            assistant.SubtitleChanged -= ShowSubtitle;
        }

        private void OnPressed(SelectEnterEventArgs args) => assistant.BeginQuestion();

        private void OnReleased(SelectExitEventArgs args) => assistant.EndQuestion();

        private void ShowSubtitle(string text) => subtitleText.text = text;

        // Mientras graba sigue activo para recibir el soltar; bloqueado mientras espera o habla, como pide el contrato.
        private void Refresh()
        {
            talkButton.enabled = assistant.CanAsk || assistant.IsRecording;
            talkLabel.text = assistant.Messages.TalkLabel;
        }
    }
}
