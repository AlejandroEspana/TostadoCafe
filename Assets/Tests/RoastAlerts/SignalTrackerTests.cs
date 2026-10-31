using NUnit.Framework;

namespace ToastedVr.RoastAlerts.Tests
{
    public sealed class SignalTrackerTests
    {
        private static SignalSettings Settings(int window = 30) => new SignalSettings
        {
            RiseWindowSeconds = window,
            MaillardStartTemperature = 160,
            MaillardEndTemperature = 200,
            BurnCeilingTemperature = 220,
            TurningPointMinRise = 0.5
        };

        private static AlertSample Sample(string phase, double temperature, int roastSeconds = 0, bool firstCrack = false) =>
            new AlertSample(phase, temperature, roastSeconds, firstCrack, null);

        [TestCase(2.5, 3)]
        [TestCase(-2.5, -2)]
        [TestCase(-2.6, -3)]
        [TestCase(14.49, 14)]
        public void RoundsLikeJavaScript(double value, int expected)
        {
            Assert.AreEqual(expected, SignalTracker.JsRound(value));
        }

        [Test]
        public void RiseUsesTheWindowAndStartsAtZero()
        {
            var tracker = new SignalTracker(Settings(window: 2));

            Assert.AreEqual(0, tracker.Update(Sample("READY", 190)).RisePerMinute);
            Assert.AreEqual(60, tracker.Update(Sample("READY", 191)).RisePerMinute);
            Assert.AreEqual(90, tracker.Update(Sample("READY", 193)).RisePerMinute);
            // La ventana guarda 3 temperaturas (window + 1): ya no ve la de 190.
            Assert.AreEqual(150, tracker.Update(Sample("READY", 196)).RisePerMinute);
        }

        [Test]
        public void ChargingEmptiesTheWindow()
        {
            var tracker = new SignalTracker(Settings());
            tracker.Update(Sample("READY", 200));
            tracker.Update(Sample("READY", 201));

            var signals = tracker.Update(Sample("CHARGE_DIP", 120, 1));

            Assert.AreEqual(0, signals.RisePerMinute);
        }

        [Test]
        public void SecondsInPhaseRestartOnEachPhase()
        {
            var tracker = new SignalTracker(Settings());

            Assert.AreEqual(1, tracker.Update(Sample("READY", 190)).SecondsInPhase);
            Assert.AreEqual(2, tracker.Update(Sample("READY", 190)).SecondsInPhase);
            Assert.AreEqual(1, tracker.Update(Sample("CHARGE_DIP", 150, 1)).SecondsInPhase);
        }

        [Test]
        public void TurningPointNeedsTheMinimumRiseFromTheLowest()
        {
            var tracker = new SignalTracker(Settings());
            tracker.Update(Sample("CHARGE_DIP", 150, 1));
            tracker.Update(Sample("CHARGE_DIP", 100, 2));

            Assert.IsFalse(tracker.Update(Sample("CHARGE_DIP", 100.4, 3)).TurningPointReached);
            Assert.IsTrue(tracker.Update(Sample("ROASTING", 100.5, 4)).TurningPointReached);
            Assert.IsTrue(tracker.Update(Sample("ROASTING", 99, 5)).TurningPointReached, "no vuelve a falso");
        }

        [Test]
        public void PeakAndCeilingCountOnlyWhileRoasting()
        {
            var tracker = new SignalTracker(Settings());

            Assert.IsNull(tracker.Update(Sample("CHARGE_DIP", 230, 1)).PeakTemperature);
            Assert.AreEqual(0, tracker.Update(Sample("CHARGE_DIP", 230, 2)).SecondsAboveCeiling);
            Assert.AreEqual(221, tracker.Update(Sample("ROASTING", 221, 3)).PeakTemperature);
            Assert.AreEqual(2, tracker.Update(Sample("ROASTING", 222, 4)).SecondsAboveCeiling);
            var cooled = tracker.Update(Sample("ROASTING", 219, 5));
            Assert.AreEqual(0, cooled.SecondsAboveCeiling);
            Assert.AreEqual(222, cooled.PeakTemperature);
        }

        [Test]
        public void StagnationComparesWithTwoTicksBefore()
        {
            var tracker = new SignalTracker(Settings());
            tracker.Update(Sample("ROASTING", 170, 100));
            tracker.Update(Sample("ROASTING", 171, 101));

            Assert.AreEqual(1, tracker.Update(Sample("ROASTING", 170, 102)).StagnationSeconds, "170 ≤ 170");
            Assert.AreEqual(2, tracker.Update(Sample("ROASTING", 171, 103)).StagnationSeconds, "171 ≤ 171");
            Assert.AreEqual(0, tracker.Update(Sample("ROASTING", 172, 104)).StagnationSeconds, "172 > 170");
        }

        [Test]
        public void StagnationKeepsItsValueOutsideMaillardOrAfterFirstCrack()
        {
            var tracker = new SignalTracker(Settings());
            tracker.Update(Sample("ROASTING", 170, 100));
            tracker.Update(Sample("ROASTING", 170, 101));
            tracker.Update(Sample("ROASTING", 170, 102));

            Assert.AreEqual(2, tracker.Update(Sample("ROASTING", 150, 103)).StagnationSeconds, "fuera de Maillard");
            tracker.Update(Sample("ROASTING", 170, 104, firstCrack: true));
            Assert.AreEqual(tracker.Update(Sample("ROASTING", 160, 105, firstCrack: true)).StagnationSeconds,
                tracker.Update(Sample("ROASTING", 160, 106, firstCrack: true)).StagnationSeconds);
        }

        [Test]
        public void DevelopmentRatioStartsAtTheFirstCrack()
        {
            var tracker = new SignalTracker(Settings());

            Assert.AreEqual(0, tracker.Update(Sample("ROASTING", 195, 400)).DevelopmentRatio);
            Assert.AreEqual(0, tracker.Update(Sample("ROASTING", 196, 500, firstCrack: true)).DevelopmentRatio);
            Assert.AreEqual(100.0 / 600, tracker.Update(Sample("ROASTING", 205, 600, firstCrack: true)).DevelopmentRatio, 1e-12);
        }

        [Test]
        public void UnknownSignalsAndMissingMoistureHaveNoValue()
        {
            var signals = new SignalTracker(Settings()).Update(Sample("READY", 190));

            Assert.IsNull(signals.Value("moisturePct"));
            Assert.IsNull(signals.Value("peakTemperature"));
            Assert.IsNull(signals.Value("doesNotExist"));
            Assert.AreEqual("READY", signals.Value("phase"));
        }
    }
}
