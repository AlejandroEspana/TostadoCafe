using System;
using System.Collections.Generic;
using UnityEngine;

namespace ToastedVr.RoastSimulation
{
    // Un dibujo de la curva: qué puntos entran y dónde caen, con las reglas de TemperatureChart.js.
    // Las coordenadas de la gráfica son unidades de la web con el eje Y hacia abajo; ToPanel las pasa a metros del panel.
    public sealed class TemperatureChartFrame
    {
        private readonly List<Vector2> curve;
        private readonly IReadOnlyList<int> ticks;
        private readonly float minTick;
        private readonly float maxTick;
        private readonly float paddingLeft;
        private readonly float paddingTop;
        private readonly float viewBoxWidth;
        private readonly float viewBoxHeight;
        private readonly float plotCenterY;

        private TemperatureChartFrame(List<Vector2> curve, IReadOnlyList<int> ticks, float plotWidth, float plotHeight,
            bool isFinal, float targetTemperature, TemperatureChartConfiguration configuration)
        {
            this.curve = curve;
            this.ticks = ticks;
            minTick = ticks[0];
            maxTick = ticks[ticks.Count - 1];
            PlotWidth = plotWidth;
            PlotHeight = plotHeight;
            IsFinal = isFinal;
            FloorY = Y(configuration.FloorReferenceCelsius);
            TargetY = Y(targetTemperature);

            paddingLeft = configuration.PaddingLeft;
            paddingTop = configuration.PaddingTop;
            viewBoxWidth = plotWidth + configuration.PaddingLeft + configuration.PaddingRight;
            viewBoxHeight = plotHeight + configuration.PaddingTop + configuration.PaddingBottom;

            // Como el SVG con preserveAspectRatio "meet": todo el viewBox cabe en el recuadro sin deformarse.
            MetersPerUnit = Mathf.Min(configuration.PanelWidth / viewBoxWidth, configuration.PlotHeight / viewBoxHeight);
            plotCenterY = -configuration.TitleHeight / 2f;
        }

        public IReadOnlyList<Vector2> Curve => curve;
        public IReadOnlyList<int> Ticks => ticks;
        public float PlotWidth { get; }
        public float PlotHeight { get; }
        public bool IsFinal { get; }
        public float FloorY { get; }
        public float TargetY { get; }
        public float MetersPerUnit { get; }

        // Devuelve null mientras no haya dos puntos, igual que la web, que no dibuja la gráfica.
        public static TemperatureChartFrame Build(IReadOnlyList<RoastState> received, RoastState state, TemperatureChartConfiguration configuration)
        {
            if (received == null || state == null || configuration == null)
            {
                return null;
            }

            var elapsed = state.Tick;
            var isFinal = state.Phase == RoastPhaseRules.Finished;
            var window = configuration.TimeWindowSeconds;
            var fitToData = isFinal && elapsed > window;
            var windowStart = fitToData ? 0 : Math.Max(0, elapsed - window);

            var samples = new List<RoastState>();
            foreach (var tick in received)
            {
                // Lo posterior al estado es de una simulación anterior que todavía no se borró del búfer.
                if (tick != null && tick.Tick <= elapsed && tick.Tick >= windowStart && tick.Tick % configuration.SampleIntervalSeconds == 0)
                {
                    samples.Add(tick);
                }
            }

            if (samples.Count < 2)
            {
                return null;
            }

            var maxObserved = state.TargetTemperature;
            var minObserved = (double)configuration.FloorReferenceCelsius;
            foreach (var sample in samples)
            {
                maxObserved = Math.Max(maxObserved, sample.Temperature);
                minObserved = Math.Min(minObserved, sample.Temperature);
            }

            var ticks = TemperatureChartScale.VerticalTicks(minObserved, maxObserved,
                configuration.VerticalStepCelsius, configuration.VerticalTickCount);
            var minTick = ticks[0];
            var maxTick = ticks[ticks.Count - 1];

            var basePlotWidth = configuration.ViewWidth - configuration.PaddingLeft - configuration.PaddingRight;
            var basePlotHeight = configuration.ViewHeight - configuration.PaddingTop - configuration.PaddingBottom;
            // Velocidad fija: un punto ya dibujado no se mueve, la curva solo avanza a la derecha.
            var unitsPerSecond = basePlotWidth / window;
            var unitsPerDegree = basePlotHeight / ((configuration.VerticalTickCount - 1) * (float)configuration.VerticalStepCelsius);

            var minTime = samples[0].Tick;
            var duration = samples[samples.Count - 1].Tick - minTime;
            var dataPlotWidth = fitToData ? Mathf.Max(basePlotWidth, duration * unitsPerSecond) : basePlotWidth;
            var startPadding = fitToData ? configuration.EdgePadding : 0f;
            var plotWidth = dataPlotWidth + startPadding + configuration.EdgePadding;
            var plotHeight = fitToData ? Mathf.Max(basePlotHeight, (maxTick - minTick) * unitsPerDegree) : basePlotHeight;

            var curve = new List<Vector2>(samples.Count);
            foreach (var sample in samples)
            {
                var x = startPadding + Mathf.Min(dataPlotWidth, (sample.Tick - minTime) * unitsPerSecond);
                var y = plotHeight - ((float)sample.Temperature - minTick) / (maxTick - minTick) * plotHeight;
                curve.Add(new Vector2(x, y));
            }

            return new TemperatureChartFrame(curve, ticks, plotWidth, plotHeight, isFinal, (float)state.TargetTemperature, configuration);
        }

        public float Y(float temperature) => PlotHeight - (temperature - minTick) / (maxTick - minTick) * PlotHeight;

        public Vector2 ToPanel(Vector2 plotPoint)
        {
            var x = paddingLeft + plotPoint.x - viewBoxWidth / 2f;
            var y = paddingTop + plotPoint.y - viewBoxHeight / 2f;
            return new Vector2(x * MetersPerUnit, plotCenterY - y * MetersPerUnit);
        }
    }
}
