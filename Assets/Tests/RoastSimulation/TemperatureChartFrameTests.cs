using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace ToastedVr.RoastSimulation.Tests
{
    // Con la configuración real (vista de 820 x 230, márgenes 46/20/15/12): la gráfica mide 754 x 203 y avanza 754/900 por segundo.
    public sealed class TemperatureChartFrameTests
    {
        private const string ConfigurationPath = "Assets/Settings/RoastSimulation/TemperatureChartConfiguration.asset";
        private const float BasePlotWidth = 754f;
        private const float BasePlotHeight = 203f;
        private const float Tolerance = 1e-3f;

        private TemperatureChartConfiguration configuration;

        [SetUp]
        public void LoadConfiguration()
        {
            configuration = AssetDatabase.LoadAssetAtPath<TemperatureChartConfiguration>(ConfigurationPath);
            Assert.IsNotNull(configuration, ConfigurationPath);
        }

        private static List<RoastState> Ticks(int from, int to, double temperature = 150) =>
            Enumerable.Range(from, to - from + 1).Select(t => new RoastState { Tick = t, Temperature = temperature }).ToList();

        private static RoastState State(int tick, string phase) =>
            new RoastState { Tick = tick, Phase = phase, TargetTemperature = 210 };

        [Test]
        public void NeedsTwoPointsToDraw()
        {
            Assert.IsNull(TemperatureChartFrame.Build(Ticks(1, 3), State(3, RoastPhaseRules.Preheat), configuration));
            Assert.IsNotNull(TemperatureChartFrame.Build(Ticks(1, 4), State(4, RoastPhaseRules.Preheat), configuration));
            Assert.IsNull(TemperatureChartFrame.Build(null, State(4, RoastPhaseRules.Preheat), configuration));
        }

        [Test]
        public void DrawsOnePointEveryTwoSeconds()
        {
            var frame = TemperatureChartFrame.Build(Ticks(1, 10), State(10, RoastPhaseRules.Preheat), configuration);

            Assert.AreEqual(5, frame.Curve.Count);
            Assert.AreEqual(2 * BasePlotWidth / 900f, frame.Curve[1].x - frame.Curve[0].x, Tolerance);
        }

        [Test]
        public void FollowsPreheatChargeDipAndRise()
        {
            var temperatures = new[] { 20.0, 100, 190, 120, 150 };
            var received = temperatures.Select((t, i) => new RoastState { Tick = (i + 1) * 2, Temperature = t }).ToList();

            var frame = TemperatureChartFrame.Build(received, State(10, RoastPhaseRules.Roasting), configuration);

            var y = frame.Curve.Select(p => p.y).ToArray();
            Assert.Less(y[2], y[4], "la carga está por encima de la subida");
            Assert.Less(y[4], y[3], "la subida está por encima del fondo de la caída");
            Assert.Less(y[3], y[1]);
            Assert.Less(y[1], y[0], "el precalentado arranca abajo");
            Assert.IsTrue(frame.Curve.Zip(frame.Curve.Skip(1), (a, b) => b.x > a.x).All(advances => advances));
        }

        [Test]
        public void LiveChartShowsTheLastFifteenMinutes()
        {
            var frame = TemperatureChartFrame.Build(Ticks(1, 1000), State(1000, RoastPhaseRules.Roasting), configuration);

            Assert.AreEqual(451, frame.Curve.Count, "de 100 a 1000, uno cada 2 s");
            Assert.AreEqual(0f, frame.Curve[0].x, Tolerance);
            Assert.AreEqual(BasePlotWidth, frame.Curve[frame.Curve.Count - 1].x, Tolerance);
            Assert.AreEqual(BasePlotWidth + 40f, frame.PlotWidth, Tolerance);
            Assert.AreEqual(BasePlotHeight, frame.PlotHeight, Tolerance);
            Assert.IsFalse(frame.IsFinal);
        }

        [Test]
        public void FinishedLongRoastShowsTheWholeCurve()
        {
            var frame = TemperatureChartFrame.Build(Ticks(1, 1500), State(1500, RoastPhaseRules.Finished), configuration);

            var dataWidth = 1498 * BasePlotWidth / 900f;
            Assert.AreEqual(750, frame.Curve.Count);
            Assert.AreEqual(40f, frame.Curve[0].x, Tolerance);
            Assert.AreEqual(40f + dataWidth, frame.Curve[frame.Curve.Count - 1].x, 0.01f);
            Assert.AreEqual(dataWidth + 80f, frame.PlotWidth, 0.01f);
            Assert.AreEqual((264 - 33) * BasePlotHeight / 165f, frame.PlotHeight, 0.01f);
            Assert.IsTrue(frame.IsFinal);
        }

        [Test]
        public void FinishedShortRoastKeepsTheLiveSize()
        {
            var frame = TemperatureChartFrame.Build(Ticks(1, 600), State(600, RoastPhaseRules.Finished), configuration);

            Assert.AreEqual(300, frame.Curve.Count);
            Assert.AreEqual(0f, frame.Curve[0].x, Tolerance);
            Assert.AreEqual(BasePlotWidth + 40f, frame.PlotWidth, Tolerance);
            Assert.IsTrue(frame.IsFinal);
        }

        [Test]
        public void IgnoresTicksOfAPreviousRoast()
        {
            Assert.IsNull(TemperatureChartFrame.Build(Ticks(1, 2000), State(0, RoastPhaseRules.Preheat), configuration));
        }

        [Test]
        public void ReferenceLinesUseTheAxisScale()
        {
            var frame = TemperatureChartFrame.Build(Ticks(1, 10), State(10, RoastPhaseRules.Roasting), configuration);

            CollectionAssert.AreEqual(new[] { 33, 66, 99, 132, 165, 198, 231, 264 }, frame.Ticks);
            Assert.AreEqual(0f, frame.Y(264), Tolerance);
            Assert.AreEqual(frame.PlotHeight, frame.Y(33), Tolerance);
            Assert.AreEqual(frame.Y(80), frame.FloorY, Tolerance);
            Assert.AreEqual(frame.Y(210), frame.TargetY, Tolerance);
        }

        [Test]
        public void PanelFitsTheWholeViewWithoutStretching()
        {
            var frame = TemperatureChartFrame.Build(Ticks(1, 10), State(10, RoastPhaseRules.Roasting), configuration);
            var viewBoxWidth = frame.PlotWidth + 46f + 20f;
            var viewBoxHeight = frame.PlotHeight + 15f + 12f;

            var center = frame.ToPanel(new Vector2(viewBoxWidth / 2f - 46f, viewBoxHeight / 2f - 15f));
            var rightEdge = frame.ToPanel(new Vector2(frame.PlotWidth + 20f, 0f));

            Assert.AreEqual(0f, center.x, Tolerance);
            Assert.AreEqual(-configuration.TitleHeight / 2f, center.y, Tolerance);
            Assert.AreEqual(configuration.PanelWidth / 2f, rightEdge.x, Tolerance);
            Assert.LessOrEqual(viewBoxHeight * frame.MetersPerUnit, configuration.PlotHeight + Tolerance);
        }
    }
}
