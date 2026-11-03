using TMPro;
using UnityEngine;

namespace ToastedVr.RoastSimulation
{
    // Panel del resultado del tueste: aparece al descargar y se actualiza con la nota final y la retroalimentación.
    // Toma los colores de la gráfica para que las dos piezas se vean iguales.
    public sealed class RoastResultPanel : MonoBehaviour
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        [SerializeField] private RoastStationController controller;
        [SerializeField] private TemperatureChartConfiguration style;
        [SerializeField] private RoastResultPanelConfiguration configuration;
        [SerializeField] private GameObject content;
        [SerializeField] private Renderer background;
        [SerializeField] private TMP_Text title;
        [SerializeField] private TMP_Text body;

        private RoastSessionSummary session;
        private bool feedbackArrived;
        private string feedback;

        private void Awake()
        {
            var block = new MaterialPropertyBlock();
            background.GetPropertyBlock(block);
            block.SetColor(BaseColorId, style.BackgroundColor);
            background.SetPropertyBlock(block);

            title.color = style.TitleColor;
            title.fontSize = style.TitleFontSize;
            body.color = style.LabelColor;
            body.enableAutoSizing = true;
            body.fontSizeMin = configuration.BodyFontSizeMin;
            body.fontSizeMax = configuration.BodyFontSizeMax;
        }

        private void OnEnable()
        {
            controller.Finished += OnFinished;
            controller.EvaluationUpdated += OnEvaluationUpdated;
            controller.FeedbackReceived += OnFeedbackReceived;
            controller.RoastStarted += OnRoastStarted;
            Render();
        }

        private void OnDisable()
        {
            controller.Finished -= OnFinished;
            controller.EvaluationUpdated -= OnEvaluationUpdated;
            controller.FeedbackReceived -= OnFeedbackReceived;
            controller.RoastStarted -= OnRoastStarted;
        }

        private void OnFinished(RoastSessionSummary finished)
        {
            session = finished;
            feedbackArrived = false;
            feedback = null;
            Render();
        }

        private void OnEvaluationUpdated(RoastSessionSummary updated)
        {
            session = updated ?? session;
            Render();
        }

        private void OnFeedbackReceived(string text)
        {
            feedbackArrived = true;
            feedback = text;
            Render();
        }

        private void OnRoastStarted(string simulationId)
        {
            session = null;
            Render();
        }

        private void Render()
        {
            content.SetActive(session != null);
            if (session == null)
            {
                return;
            }

            var texts = controller.Messages;
            title.text = texts.ResultTitle;
            body.text = RoastResultText.Build(session, controller.EvaluationTimedOut, feedbackArrived, feedback, texts);
        }
    }
}
