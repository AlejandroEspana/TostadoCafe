using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace ToastedVr.RoastSimulation
{
    // Dibuja en la estación la curva del tueste con los ticks que ya guardó el controlador.
    // Solo escucha a la estación; se oculta apagada y queda visible después de descargar.
    public sealed class TemperatureChart : MonoBehaviour
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        [SerializeField] private RoastStationController controller;
        [SerializeField] private TemperatureChartConfiguration configuration;
        [SerializeField] private GameObject content;
        [SerializeField] private Renderer background;
        [SerializeField] private MeshFilter grid;
        [SerializeField] private MeshFilter floorReference;
        [SerializeField] private MeshFilter targetReference;
        [SerializeField] private MeshFilter curve;
        [SerializeField] private MeshFilter baseline;
        [SerializeField] private TMP_Text title;
        [SerializeField] private TMP_Text axisTitle;
        [SerializeField] private TMP_Text tickLabelTemplate;

        private readonly ChartLineMesh line = new ChartLineMesh();
        private readonly List<TMP_Text> tickLabels = new List<TMP_Text>();
        private readonly List<Vector2> panelPoints = new List<Vector2>();
        private readonly List<Mesh> meshes = new List<Mesh>();
        private (int count, int lastTick, int stateTick, string phase, double target)? drawnKey;

        private void Awake()
        {
            SetUpLayer(grid, configuration.GridColor);
            SetUpLayer(floorReference, configuration.FloorReferenceColor);
            SetUpLayer(targetReference, configuration.TargetReferenceColor);
            SetUpLayer(curve, configuration.CurveColor);
            SetUpLayer(baseline, configuration.BaselineColor);
            SetColor(background, configuration.BackgroundColor);

            title.color = configuration.TitleColor;
            title.fontSize = configuration.TitleFontSize;
            axisTitle.color = configuration.LabelColor;
            axisTitle.fontSize = configuration.LabelFontSize;
            tickLabelTemplate.color = configuration.LabelColor;
            tickLabelTemplate.fontSize = configuration.LabelFontSize;
            tickLabelTemplate.rectTransform.sizeDelta = configuration.TickLabelSize;
            tickLabelTemplate.gameObject.SetActive(false);
        }

        private void OnEnable()
        {
            controller.Changed += Render;
            Render();
        }

        private void OnDisable()
        {
            controller.Changed -= Render;
        }

        private void OnDestroy()
        {
            foreach (var mesh in meshes)
            {
                Destroy(mesh);
            }

            meshes.Clear();
        }

        // Changed llega también por mensajes y botones; solo se redibuja si cambió algo de la curva.
        private void Render()
        {
            var state = controller.State;
            if (!controller.IsPowered || state == null)
            {
                Hide();
                return;
            }

            var ticks = controller.Ticks;
            var key = (ticks.Count, ticks.Count > 0 ? ticks[ticks.Count - 1].Tick : -1, state.Tick, state.Phase, state.TargetTemperature);
            if (drawnKey == key)
            {
                return;
            }

            var frame = TemperatureChartFrame.Build(ticks, state, configuration);
            if (frame == null)
            {
                Hide();
                return;
            }

            drawnKey = key;
            content.SetActive(true);
            Draw(frame);
        }

        private void Hide()
        {
            drawnKey = null;
            content.SetActive(false);
        }

        private void Draw(TemperatureChartFrame frame)
        {
            var units = frame.MetersPerUnit;

            line.Clear();
            foreach (var tick in frame.Ticks)
            {
                AddHorizontal(frame, frame.Y(tick), configuration.GridWidth, configuration.GridDash * units, configuration.GridGap * units);
            }

            line.ApplyTo(grid.sharedMesh);
            DrawHorizontal(floorReference, frame, frame.FloorY, configuration.ReferenceDash * units, configuration.ReferenceGap * units);
            DrawHorizontal(targetReference, frame, frame.TargetY, configuration.ReferenceDash * units, configuration.ReferenceGap * units);

            line.Clear();
            line.AddSegment(frame.ToPanel(new Vector2(0f, frame.PlotHeight)), frame.ToPanel(new Vector2(frame.PlotWidth, frame.PlotHeight)), configuration.BaselineWidth);
            line.ApplyTo(baseline.sharedMesh);

            panelPoints.Clear();
            foreach (var point in frame.Curve)
            {
                panelPoints.Add(frame.ToPanel(point));
            }

            line.Clear();
            line.AddPolyline(panelPoints, configuration.CurveWidth);
            line.ApplyTo(curve.sharedMesh);

            var texts = controller.Messages;
            title.text = frame.IsFinal ? texts.ChartTitleFinal : texts.ChartTitle;
            axisTitle.text = texts.ChartAxisTitle;
            PlaceText(axisTitle.rectTransform, frame.ToPanel(new Vector2(-configuration.AxisTitleOffset, frame.PlotHeight / 2f)));
            DrawTickLabels(frame);
        }

        private void DrawHorizontal(MeshFilter target, TemperatureChartFrame frame, float y, float dash, float gap)
        {
            line.Clear();
            AddHorizontal(frame, y, configuration.ReferenceWidth, dash, gap);
            line.ApplyTo(target.sharedMesh);
        }

        private void AddHorizontal(TemperatureChartFrame frame, float y, float width, float dash, float gap)
        {
            line.AddDashedSegment(frame.ToPanel(new Vector2(0f, y)), frame.ToPanel(new Vector2(frame.PlotWidth, y)), width, dash, gap);
        }

        // Los números van alineados a la derecha, a la izquierda de la gráfica, como en la web.
        private void DrawTickLabels(TemperatureChartFrame frame)
        {
            var format = controller.Messages.ChartTickFormat;
            while (tickLabels.Count < frame.Ticks.Count)
            {
                tickLabels.Add(Instantiate(tickLabelTemplate, tickLabelTemplate.transform.parent));
            }

            for (var index = 0; index < tickLabels.Count; index++)
            {
                var label = tickLabels[index];
                var visible = index < frame.Ticks.Count;
                label.gameObject.SetActive(visible);
                if (!visible)
                {
                    continue;
                }

                var tick = frame.Ticks[index];
                label.text = string.Format(format, tick);
                var rightEdge = frame.ToPanel(new Vector2(-configuration.TickLabelOffset, frame.Y(tick)));
                PlaceText(label.rectTransform, new Vector2(rightEdge.x - configuration.TickLabelSize.x / 2f, rightEdge.y));
            }
        }

        private static void PlaceText(RectTransform text, Vector2 position)
        {
            text.localPosition = new Vector3(position.x, position.y, text.localPosition.z);
        }

        private void SetUpLayer(MeshFilter layer, Color color)
        {
            var mesh = new Mesh { name = layer.name };
            mesh.MarkDynamic();
            meshes.Add(mesh);
            layer.sharedMesh = mesh;
            SetColor(layer.GetComponent<Renderer>(), color);
        }

        // Un bloque de propiedades por pieza: así no se cambia el material compartido.
        private static void SetColor(Renderer target, Color color)
        {
            var block = new MaterialPropertyBlock();
            target.GetPropertyBlock(block);
            block.SetColor(BaseColorId, color);
            target.SetPropertyBlock(block);
        }
    }
}
