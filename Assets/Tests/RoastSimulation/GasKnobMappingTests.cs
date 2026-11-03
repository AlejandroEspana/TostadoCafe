using System;
using NUnit.Framework;

namespace ToastedVr.RoastSimulation.Tests
{
    public sealed class GasKnobMappingTests
    {
        private static GasKnobMapping Default() => new GasKnobMapping(-135f, 135f, 0, 100, 5);

        [TestCase(-135f, 0)]
        [TestCase(0f, 50)]
        [TestCase(135f, 100)]
        [TestCase(-67.5f, 25)]
        [TestCase(67.5f, 75)]
        public void MapsAngleRangeToGasRange(float angle, int expected)
        {
            Assert.AreEqual(expected, Default().GasForAngle(angle));
        }

        [TestCase(1f, 50)]
        [TestCase(6f, 50)]
        [TestCase(7f, 55)]
        [TestCase(-7f, 45)]
        public void SnapsToTheNearestStep(float angle, int expected)
        {
            // 2,7° por punto de gas: 6° son 2,2 puntos y 7° son 2,6 puntos.
            Assert.AreEqual(expected, Default().GasForAngle(angle));
        }

        [TestCase(-400f, 0)]
        [TestCase(400f, 100)]
        public void ClampsOutsideTheStops(float angle, int expected)
        {
            Assert.AreEqual(expected, Default().GasForAngle(angle));
        }

        [Test]
        public void ResultIsAlwaysAMultipleOfTheStep()
        {
            var mapping = Default();
            for (var angle = -135f; angle <= 135f; angle += 0.25f)
            {
                Assert.AreEqual(0, mapping.GasForAngle(angle) % 5, $"ángulo {angle}");
            }
        }

        [Test]
        public void MaximumIsReachableWhenRangeIsNotAMultipleOfTheStep()
        {
            var mapping = new GasKnobMapping(0f, 90f, 0, 100, 30);

            Assert.AreEqual(100, mapping.GasForAngle(90f));
            Assert.AreEqual(90, mapping.GasForAngle(80f));
        }

        [Test]
        public void ReversedKnobOpensTheOtherWay()
        {
            var mapping = new GasKnobMapping(135f, -135f, 0, 100, 5);

            Assert.AreEqual(0, mapping.GasForAngle(135f));
            Assert.AreEqual(100, mapping.GasForAngle(-135f));
            Assert.AreEqual(100, mapping.GasForAngle(-500f));
        }

        [TestCase(0, -135f)]
        [TestCase(50, 0f)]
        [TestCase(100, 135f)]
        [TestCase(-20, -135f)]
        [TestCase(150, 135f)]
        public void AngleForGasPlacesTheKnob(int gas, float expected)
        {
            Assert.AreEqual(expected, Default().AngleForGas(gas), 1e-4);
        }

        [Test]
        public void RoundTripKeepsTheGas()
        {
            var mapping = Default();
            for (var gas = 0; gas <= 100; gas += 5)
            {
                Assert.AreEqual(gas, mapping.GasForAngle(mapping.AngleForGas(gas)));
            }
        }

        [Test]
        public void RejectsInvalidConfiguration()
        {
            Assert.Throws<ArgumentException>(() => new GasKnobMapping(10f, 10f, 0, 100, 5));
            Assert.Throws<ArgumentException>(() => new GasKnobMapping(-135f, 135f, 100, 0, 5));
            Assert.Throws<ArgumentException>(() => new GasKnobMapping(-135f, 135f, 0, 100, 0));
        }
    }
}
