using NUnit.Framework;

namespace ToastedVr.RoastSimulation.Tests
{
    // Valores calculados con ChartAxisScale.computeVerticalTicks de la web (paso 33, 6 marcas).
    public sealed class TemperatureChartScaleTests
    {
        [Test]
        public void ColdStartReachesZero()
        {
            CollectionAssert.AreEqual(new[] { 0, 33, 66, 99, 132, 165, 198, 231, 264 },
                TemperatureChartScale.VerticalTicks(20, 210, 33, 6));
        }

        [Test]
        public void FloorReferenceKeepsTheBottomMark()
        {
            CollectionAssert.AreEqual(new[] { 33, 66, 99, 132, 165, 198, 231, 264 },
                TemperatureChartScale.VerticalTicks(80, 210, 33, 6));
        }

        [Test]
        public void LowTemperaturesStillShowTheMinimumMarks()
        {
            CollectionAssert.AreEqual(new[] { 33, 66, 99, 132, 165, 198 },
                TemperatureChartScale.VerticalTicks(80, 100, 33, 6));
        }

        [Test]
        public void TopMovesUpWithTheTemperature()
        {
            CollectionAssert.AreEqual(new[] { 66, 99, 132, 165, 198, 231, 264 },
                TemperatureChartScale.VerticalTicks(100, 230, 33, 6));
        }

        [Test]
        public void OtherStepsFollowTheSameRule()
        {
            CollectionAssert.AreEqual(new[] { 60, 80, 100,120, 140, 160, 180, 200, 220, 240 },
                TemperatureChartScale.VerticalTicks(80, 215, 20, 6));
        }
    }
}
