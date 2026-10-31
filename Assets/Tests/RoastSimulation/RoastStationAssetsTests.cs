using System;
using NUnit.Framework;
using UnityEditor;

namespace ToastedVr.RoastSimulation.Tests
{
    public sealed class RoastStationAssetsTests
    {
        private const string ConfigurationPath = "Assets/Settings/RoastSimulation/RoastSimulationConfiguration.asset";
        private const string MessagesPath = "Assets/Settings/RoastSimulation/RoastStationMessages.asset";

        private static RoastSimulationConfiguration Configuration()
        {
            var configuration = AssetDatabase.LoadAssetAtPath<RoastSimulationConfiguration>(ConfigurationPath);
            Assert.IsNotNull(configuration, ConfigurationPath);
            return configuration;
        }

        private static RoastStationMessages Messages()
        {
            var messages = AssetDatabase.LoadAssetAtPath<RoastStationMessages>(MessagesPath);
            Assert.IsNotNull(messages, MessagesPath);
            return messages;
        }

        [Test]
        public void ConfigurationBuildsAValidKnob()
        {
            var configuration = Configuration();

            var mapping = new GasKnobMapping(configuration.KnobMinAngle, configuration.KnobMaxAngle,
                configuration.MinGasPercent, configuration.MaxGasPercent, configuration.GasStepPercent);

            Assert.AreEqual(configuration.MinGasPercent, mapping.GasForAngle(configuration.KnobMinAngle));
            Assert.AreEqual(configuration.MaxGasPercent, mapping.GasForAngle(configuration.KnobMaxAngle));
            Assert.AreEqual(5, configuration.GasStepPercent);
            Assert.AreEqual(0.5f, configuration.GasSendIntervalSeconds, 1e-6);
        }

        [Test]
        public void ConfigurationTimingsFitTheContract()
        {
            var configuration = Configuration();

            StringAssert.StartsWith("/", configuration.SimulationsPath);
            Assert.Greater(configuration.RequestTimeoutSeconds, 0);
            // El servidor borra la simulación a los 120 s sin consultas.
            Assert.Less(configuration.PollIntervalSeconds, 120f);
            Assert.Greater(configuration.DischargeMaxRetries, 0);
            Assert.Greater(configuration.DischargeRetryDelaySeconds, 0f);
        }

        [Test]
        public void EveryFormatAcceptsItsArguments()
        {
            var messages = Messages();

            Assert.DoesNotThrow(() =>
            {
                string.Format(messages.GasKnobFormat, 55);
                string.Format(messages.PhaseFormat, "x");
                string.Format(messages.TemperatureFormat, 198.42);
                string.Format(messages.RoastTimeFormat, "08:30");
                string.Format(messages.GasFormat, 55);
                string.Format(messages.RiseFormat, 14.2);
                string.Format(messages.GrainFormat, "x");
                string.Format(messages.FirstCrackFormat, "x");
                string.Format(messages.SecondCrackFormat, "x");
                string.Format(messages.ScoreFormat, 80, "x");
            });
        }

        [TestCase("PREHEAT")]
        [TestCase("READY")]
        [TestCase("CHARGE_DIP")]
        [TestCase("ROASTING")]
        [TestCase("FINISHED")]
        public void EveryPhaseHasAName(string phase)
        {
            Assert.AreNotEqual(phase, Messages().PhaseName(phase));
        }

        [TestCase("DRYING")]
        [TestCase("MAILLARD")]
        [TestCase("FIRST_CRACK")]
        [TestCase("DARK")]
        [TestCase("SECOND_CRACK")]
        [TestCase("BURNED")]
        public void EveryGrainStateHasAName(string grainState)
        {
            Assert.AreNotEqual(grainState, Messages().GrainStateName(grainState));
        }

        [TestCase("EXCELLENT")]
        [TestCase("GOOD")]
        [TestCase("FAIR")]
        [TestCase("POOR")]
        public void EveryGradeHasAName(string grade)
        {
            Assert.AreNotEqual(grade, Messages().GradeName(grade));
        }

        [Test]
        public void UnknownKeysAreShownAsIs()
        {
            Assert.AreEqual("NEW_PHASE", Messages().PhaseName("NEW_PHASE"));
        }

        [Test]
        public void StationMessagesAreFilled()
        {
            var messages = Messages();

            foreach (var text in new[]
            {
                messages.PowerOnLabel, messages.PowerOffLabel, messages.ChargeLabel, messages.DischargeLabel,
                messages.PhaseOffName, messages.CrackPendingText, messages.ConnectingMessage, messages.OfflineMessage,
                messages.SessionExpiredMessage, messages.SimulationLostMessage, messages.DischargingMessage,
                messages.EvaluationPendingMessage, messages.UnexpectedErrorMessage, messages.BurnedAlarm,
                messages.BakedAlarm, messages.BurnWarningAlarm, messages.AlarmLimitExceededAlarm, messages.ChargeOvershootAlarm
            })
            {
                Assert.IsFalse(string.IsNullOrWhiteSpace(text));
            }

            Assert.AreEqual("Tu sesión venció, vuelve a ingresar", messages.SessionExpiredMessage);
        }
    }
}
