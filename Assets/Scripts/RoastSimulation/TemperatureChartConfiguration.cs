using UnityEngine;

namespace ToastedVr.RoastSimulation
{
    // Valores de la curva del tueste. Los de datos y diseño son los de TemperatureChart.js y RoastConstants.js de la web.
    [CreateAssetMenu(fileName = "TemperatureChartConfiguration", menuName = "Toasted VR/Temperature Chart Configuration")]
    public sealed class TemperatureChartConfiguration : ScriptableObject
    {
        [Header("Data")]
        [Tooltip("One point every this many simulated seconds (CHART_SAMPLE_INTERVAL_SECONDS).")]
        [SerializeField, Min(1)] private int sampleIntervalSeconds;
        [Tooltip("Moving window of the live chart (CHART_TIME_WINDOW_SECONDS).")]
        [SerializeField, Min(1)] private int timeWindowSeconds;

        [Header("Vertical axis")]
        [SerializeField, Min(1)] private int verticalStepCelsius;
        [SerializeField, Min(2)] private int verticalTickCount;
        [Tooltip("Fixed floor reference line (CHART_FLOOR_REFERENCE_TEMP_C).")]
        [SerializeField] private float floorReferenceCelsius;

        [Header("Layout in web units (viewBox of TemperatureChart.js)")]
        [SerializeField, Min(1f)] private float viewWidth;
        [SerializeField, Min(1f)] private float viewHeight;
        [SerializeField, Min(0f)] private float paddingTop;
        [SerializeField, Min(0f)] private float paddingRight;
        [SerializeField, Min(0f)] private float paddingBottom;
        [SerializeField, Min(0f)] private float paddingLeft;
        [Tooltip("Empty margin after the last point, and before the first one in a finished long roast.")]
        [SerializeField, Min(0f)] private float edgePadding;
        [SerializeField, Min(0f)] private float tickLabelOffset;
        [SerializeField, Min(0f)] private float axisTitleOffset;
        [SerializeField, Min(0.1f)] private float gridDash;
        [SerializeField, Min(0.1f)] private float gridGap;
        [SerializeField, Min(0.1f)] private float referenceDash;
        [SerializeField, Min(0.1f)] private float referenceGap;

        [Header("Panel in meters (before the station scale)")]
        [SerializeField, Min(0.01f)] private float panelWidth;
        [Tooltip("Band above the plot for the title.")]
        [SerializeField, Min(0f)] private float titleHeight;
        [Tooltip("Distance between layers toward the player, so they do not flicker.")]
        [SerializeField, Min(0f)] private float layerSpacing;
        [Tooltip("Widths do not shrink with the view, so the curve stays readable in VR.")]
        [SerializeField, Min(0.0001f)] private float curveWidth;
        [SerializeField, Min(0.0001f)] private float referenceWidth;
        [SerializeField, Min(0.0001f)] private float gridWidth;
        [SerializeField, Min(0.0001f)] private float baselineWidth;
        [SerializeField, Min(0.01f)] private float titleFontSize;
        [SerializeField, Min(0.01f)] private float labelFontSize;
        [SerializeField] private Vector2 tickLabelSize;

        [Header("Colors")]
        [SerializeField] private Color backgroundColor;
        [SerializeField] private Color gridColor;
        [SerializeField] private Color floorReferenceColor;
        [SerializeField] private Color targetReferenceColor;
        [SerializeField] private Color curveColor;
        [SerializeField] private Color baselineColor;
        [SerializeField] private Color titleColor;
        [SerializeField] private Color labelColor;

        public int SampleIntervalSeconds => sampleIntervalSeconds;
        public int TimeWindowSeconds => timeWindowSeconds;
        public int VerticalStepCelsius => verticalStepCelsius;
        public int VerticalTickCount => verticalTickCount;
        public float FloorReferenceCelsius => floorReferenceCelsius;
        public float ViewWidth => viewWidth;
        public float ViewHeight => viewHeight;
        public float PaddingTop => paddingTop;
        public float PaddingRight => paddingRight;
        public float PaddingBottom => paddingBottom;
        public float PaddingLeft => paddingLeft;
        public float EdgePadding => edgePadding;
        public float TickLabelOffset => tickLabelOffset;
        public float AxisTitleOffset => axisTitleOffset;
        public float GridDash => gridDash;
        public float GridGap => gridGap;
        public float ReferenceDash => referenceDash;
        public float ReferenceGap => referenceGap;
        public float PanelWidth => panelWidth;
        public float PlotHeight => panelWidth * viewHeight / viewWidth;
        public float TitleHeight => titleHeight;
        public float PanelHeight => PlotHeight + titleHeight;
        public float LayerSpacing => layerSpacing;
        public float CurveWidth => curveWidth;
        public float ReferenceWidth => referenceWidth;
        public float GridWidth => gridWidth;
        public float BaselineWidth => baselineWidth;
        public float TitleFontSize => titleFontSize;
        public float LabelFontSize => labelFontSize;
        public Vector2 TickLabelSize => tickLabelSize;
        public Color BackgroundColor => backgroundColor;
        public Color GridColor => gridColor;
        public Color FloorReferenceColor => floorReferenceColor;
        public Color TargetReferenceColor => targetReferenceColor;
        public Color CurveColor => curveColor;
        public Color BaselineColor => baselineColor;
        public Color TitleColor => titleColor;
        public Color LabelColor => labelColor;
    }
}
