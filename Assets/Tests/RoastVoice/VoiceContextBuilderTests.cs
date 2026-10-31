using System;
using NUnit.Framework;
using ToastedVr.RoastSimulation;

namespace ToastedVr.RoastVoice.Tests
{
    public sealed class VoiceContextBuilderTests
    {
        private const string SimulationId = "3f6c1b52-9a1e-4c55-9d7e-2b8f0c4a7e11";

        private static RoastState Roasting() => new RoastState
        {
            Tick = 1040,
            Phase = "ROASTING",
            RoastSeconds = 510,
            Temperature = 198.42,
            GasPercent = 80,
            RisePerMinute = 13.6,
            ChargeTemperature = 190.3,
            TargetTemperature = 210,
            FirstCrack = new CrackStatus { Reached = false, RoastSeconds = null }
        };

        [Test]
        public void MapsTheSimulatorStateToTheVoiceContract()
        {
            var context = VoiceContextBuilder.Build(SimulationId, Roasting());

            Assert.AreEqual(SimulationId, context.RoastAttemptId);
            var state = context.RoastState;
            Assert.AreEqual(510, state.ElapsedSeconds);
            Assert.AreEqual(198.42, state.BeanTemperature, 1e-9);
            Assert.IsFalse(state.FirstCrackReached);
            Assert.IsNull(state.FirstCrackElapsedSeconds);
            Assert.AreEqual(210, state.TargetTemperature);
            Assert.AreEqual(190.3, state.ChargeTemperature);
            Assert.AreEqual(80, state.HeatLevelPercent);
            Assert.AreEqual(14, state.TemperatureRisePerMinute);
        }

        [Test]
        public void IncludesTheFirstCrackSecondOnceItHappened()
        {
            var roasting = Roasting();
            roasting.FirstCrack = new CrackStatus { Reached = true, RoastSeconds = 495 };

            var state = VoiceContextBuilder.Build(SimulationId, roasting).RoastState;

            Assert.IsTrue(state.FirstCrackReached);
            Assert.AreEqual(495, state.FirstCrackElapsedSeconds);
        }

        [Test]
        public void BeforeChargingTheRoastClockIsZero()
        {
            var preheat = new RoastState { Phase = "PREHEAT", RoastSeconds = 0, Temperature = 150, GasPercent = 60, RisePerMinute = null };

            var state = VoiceContextBuilder.Build(SimulationId, preheat).RoastState;

            Assert.AreEqual(0, state.ElapsedSeconds);
            Assert.IsFalse(state.FirstCrackReached);
            Assert.IsNull(state.TemperatureRisePerMinute);
        }

        [Test]
        public void SerializesWithoutMissingOptionalFields()
        {
            var preheat = new RoastState { Phase = "PREHEAT", Temperature = 150, GasPercent = 60 };

            var json = RoastJson.Serialize(VoiceContextBuilder.Build(SimulationId, preheat));

            StringAssert.DoesNotContain("firstCrackElapsedSeconds", json);
            StringAssert.DoesNotContain("temperatureRisePerMinute", json);
            StringAssert.Contains("\"firstCrackReached\":false", json);
        }

        [TestCase(null)]
        [TestCase("")]
        public void RequiresASimulation(string simulationId)
        {
            Assert.Throws<ArgumentException>(() => VoiceContextBuilder.Build(simulationId, Roasting()));
            Assert.Throws<ArgumentException>(() => VoiceContextBuilder.Build(SimulationId, null));
        }
    }
}
