using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace ToastedVr.RoastAlerts.Tests
{
    public sealed class RuleStateTests
    {
        private static AlertCondition Condition(string signal, string op, JToken value) =>
            new AlertCondition { Signal = signal, Op = op, Value = value };

        private static AlertSignals Hot(double temperature) => new AlertSignals { Phase = "ROASTING", Temperature = temperature };

        private static AlertRuleModel Rule(List<AlertCondition> rearm, int sustain = 1, int cooldown = 0, int maxRepeats = 5, int quiet = 0) =>
            new AlertRuleModel
            {
                Id = "TEST",
                When = new List<AlertCondition> { Condition("temperature", "ge", 215) },
                RearmWhen = rearm,
                SustainSeconds = sustain,
                CooldownSeconds = cooldown,
                MaxRepeats = maxRepeats,
                RequiresQuietSeconds = quiet
            };

        [TestCase("eq", 200, false)]
        [TestCase("ne", 200, true)]
        [TestCase("lt", 216, true)]
        [TestCase("le", 215.5, true)]
        [TestCase("gt", 215.5, false)]
        [TestCase("ge", 215.5, true)]
        public void ComparesNumbers(string op, double value, bool expected)
        {
            Assert.AreEqual(expected, ConditionEvaluator.Holds(Condition("temperature", op, value), Hot(215.5)));
        }

        [Test]
        public void ComparesPhasesAndFlagsOnlyByEquality()
        {
            var signals = new AlertSignals { Phase = "READY", FirstCrackReached = true };

            Assert.IsTrue(ConditionEvaluator.Holds(Condition("phase", "eq", "READY"), signals));
            Assert.IsTrue(ConditionEvaluator.Holds(Condition("phase", "ne", "ROASTING"), signals));
            Assert.IsTrue(ConditionEvaluator.Holds(Condition("firstCrackReached", "eq", true), signals));
            Assert.IsFalse(ConditionEvaluator.Holds(Condition("firstCrackReached", "gt", false), signals));
        }

        [Test]
        public void MissingSignalNeverHolds()
        {
            var signals = Hot(200);

            Assert.IsFalse(ConditionEvaluator.Holds(Condition("moisturePct", "le", 3), signals));
            Assert.IsFalse(ConditionEvaluator.Holds(Condition("moisturePct", "ne", 3), signals));
            Assert.IsFalse(ConditionEvaluator.Holds(Condition("peakTemperature", "le", 220), signals));
        }

        [Test]
        public void FiresAfterSustainAndOnlyOncePerArming()
        {
            var state = new RuleState(Rule(rearm: null, sustain: 3));

            Assert.IsFalse(state.Update(Hot(216), 0, double.PositiveInfinity));
            Assert.IsFalse(state.Update(Hot(216), 1, double.PositiveInfinity));
            Assert.IsTrue(state.Update(Hot(216), 2, double.PositiveInfinity));
            Assert.IsFalse(state.Update(Hot(216), 3, double.PositiveInfinity), "sigue activa pero ya se disparó");
        }

        [Test]
        public void NullRearmWaitsUntilTheConditionStops()
        {
            var state = new RuleState(Rule(rearm: null));
            state.Update(Hot(216), 0, double.PositiveInfinity);

            Assert.IsFalse(state.Update(Hot(210), 1, double.PositiveInfinity));
            Assert.IsTrue(state.Update(Hot(216), 2, double.PositiveInfinity));
        }

        [Test]
        public void EmptyRearmListRearmsAtOnce()
        {
            var state = new RuleState(Rule(rearm: new List<AlertCondition>()));

            Assert.IsTrue(state.Update(Hot(216), 0, double.PositiveInfinity));
            Assert.IsTrue(state.Update(Hot(216), 1, double.PositiveInfinity));
        }

        [Test]
        public void RearmListNeedsItsOwnConditions()
        {
            var state = new RuleState(Rule(rearm: new List<AlertCondition> { Condition("temperature", "lt", 213) }));
            state.Update(Hot(216), 0, double.PositiveInfinity);

            Assert.IsFalse(state.Update(Hot(214), 1, double.PositiveInfinity), "bajó pero no de 213");
            Assert.IsFalse(state.Update(Hot(212), 2, double.PositiveInfinity), "re-armada, pero no activa");
            Assert.IsTrue(state.Update(Hot(216), 3, double.PositiveInfinity));
        }

        [Test]
        public void RespectsCooldownMaxRepeatsAndQuiet()
        {
            var state = new RuleState(Rule(rearm: new List<AlertCondition>(), cooldown: 20, maxRepeats: 2, quiet: 60));

            Assert.IsFalse(state.Update(Hot(216), 0, 59), "falta silencio");
            Assert.IsTrue(state.Update(Hot(216), 1, 60));
            Assert.IsFalse(state.Update(Hot(216), 20, 100), "espera de 20 s");
            Assert.IsTrue(state.Update(Hot(216), 21, 100));
            Assert.IsFalse(state.Update(Hot(216), 60, 100), "máximo de 2");
        }
    }
}
