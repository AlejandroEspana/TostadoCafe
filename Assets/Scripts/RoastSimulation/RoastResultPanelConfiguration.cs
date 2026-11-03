using UnityEngine;

namespace ToastedVr.RoastSimulation
{
    // Medidas del panel del resultado, en metros. Los colores salen de TemperatureChartConfiguration.
    [CreateAssetMenu(fileName = "RoastResultPanelConfiguration", menuName = "Toasted VR/Roast Result Panel Configuration")]
    public sealed class RoastResultPanelConfiguration : ScriptableObject
    {
        [Header("Layout")]
        [SerializeField, Min(0.1f)] private float panelWidth;
        [SerializeField, Min(0.1f)] private float panelHeight;
        [Tooltip("Space between the board and the panel.")]
        [SerializeField, Min(0f)] private float gap;
        [SerializeField, Min(0f)] private float padding;
        [SerializeField, Min(0.01f)] private float titleHeight;
        [Tooltip("Distance of the texts in front of the background, to avoid flickering.")]
        [SerializeField, Min(0.0001f)] private float layerSpacing;

        [Header("Text")]
        [Tooltip("The body shrinks down to this size when the feedback is long.")]
        [SerializeField, Min(0.01f)] private float bodyFontSizeMin;
        [SerializeField, Min(0.01f)] private float bodyFontSizeMax;

        public float PanelWidth => panelWidth;
        public float PanelHeight => panelHeight;
        public float Gap => gap;
        public float Padding => padding;
        public float TitleHeight => titleHeight;
        public float LayerSpacing => layerSpacing;
        public float BodyFontSizeMin => bodyFontSizeMin;
        public float BodyFontSizeMax => bodyFontSizeMax;
    }
}
