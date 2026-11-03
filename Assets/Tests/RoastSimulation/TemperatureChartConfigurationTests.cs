using NUnit.Framework;
using UnityEditor;

namespace ToastedVr.RoastSimulation.Tests
{
    public sealed class TemperatureChartConfigurationTests
    {
        private const string ConfigurationPath = "Assets/Settings/RoastSimulation/TemperatureChartConfiguration.asset";

        private static TemperatureChartConfiguration Configuration()
        {
            var configuration = AssetDatabase.LoadAssetAtPath<TemperatureChartConfiguration>(ConfigurationPath);
            Assert.IsNotNull(configuration, ConfigurationPath);
            return configuration;
        }

        // Mismos valores que RoastConstants.js, para que la curva de Unity se vea como la de la web.
        [Test]
        public void DataMatchesTheWebChart()
        {
            var configuration = Configuration();

            Assert.AreEqual(2, configuration.SampleIntervalSeconds);
            Assert.AreEqual(900, configuration.TimeWindowSeconds);
            Assert.AreEqual(33, configuration.VerticalStepCelsius);
            Assert.AreEqual(6, configuration.VerticalTickCount);
            Assert.AreEqual(80f, configuration.FloorReferenceCelsius);
        }

        [Test]
        public void LayoutMatchesTheWebChart()
        {
            var configuration = Configuration();

            Assert.AreEqual(820f, configuration.ViewWidth);
            Assert.AreEqual(230f, configuration.ViewHeight);
            Assert.AreEqual(15f, configuration.PaddingTop);
            Assert.AreEqual(20f, configuration.PaddingRight);
            Assert.AreEqual(12f, configuration.PaddingBottom);
            Assert.AreEqual(46f, configuration.PaddingLeft);
            Assert.AreEqual(40f, configuration.EdgePadding);
        }

        [Test]
        public void PanelKeepsTheWebProportion()
        {
            var configuration = Configuration();

            Assert.AreEqual(configuration.ViewWidth / configuration.ViewHeight,
                configuration.PanelWidth / configuration.PlotHeight, 1e-4);
            Assert.AreEqual(configuration.PlotHeight + configuration.TitleHeight, configuration.PanelHeight, 1e-6);
        }

        [Test]
        public void CurveIsTheThickestLine()
        {
            var configuration = Configuration();

            Assert.Greater(configuration.CurveWidth, configuration.ReferenceWidth);
            Assert.Greater(configuration.ReferenceWidth, configuration.GridWidth);
            Assert.Greater(configuration.LayerSpacing, 0f);
        }
    }
}
