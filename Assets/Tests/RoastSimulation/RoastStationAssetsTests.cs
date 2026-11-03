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
            Assert.AreEqual(60, configuration.MaxDischargeAlerts);
            Assert.AreEqual(40, configuration.MaxAlertIdLength);
            Assert.AreEqual(3f, configuration.RejectionMessageSeconds, 1e-6);
            Assert.AreEqual("/api/v1/roasting/sessions", configuration.SessionsPath);
            // GET /feedback puede tardar ~125 s cuando la IA la escribe por primera vez.
            Assert.GreaterOrEqual(configuration.FeedbackTimeoutSeconds, 125);
            Assert.AreEqual(5f, configuration.EvaluationPollIntervalSeconds, 1e-6);
            Assert.AreEqual(24, configuration.EvaluationPollMaxAttempts);
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
                string.Format(messages.ChartTickFormat, 198);
                string.Format(messages.ResultScoreWithGradeFormat, 77, "x");
                string.Format(messages.ResultRulesScoreFormat, 70);
                string.Format(messages.ResultRubricScoreFormat, 30);
                string.Format(messages.ResultPartScoreFormat, 70.5);
                string.Format(messages.ResultCriterionLevelFormat, "x", 2, 3);
                string.Format(messages.ResultNotApplicableFormat, "x");
                string.Format(messages.ResultSectionFormat, "x");
                string.Format(messages.ResultRowFormat, "x", "y");
                string.Format(messages.ResultCriterionFormat, "x", "y");
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

        [TestCase("DRYING_GAS")]
        [TestCase("RISE_TO_FIRST_CRACK")]
        [TestCase("GAS_AFTER_FIRST_CRACK")]
        [TestCase("ALERT_REACTION")]
        public void EveryCriterionHasAName(string criterion)
        {
            Assert.AreNotEqual(criterion, Messages().CriterionName(criterion));
        }

        [TestCase("NO_TRACE")]
        [TestCase("INVALID_TRACE")]
        [TestCase("AUTO_MODE")]
        [TestCase("NO_CRITERIA")]
        public void EverySkipReasonHasItsOwnMessage(string skipReason)
        {
            var messages = Messages();

            Assert.AreNotEqual(skipReason, messages.SkipReasonMessage(skipReason));
            Assert.AreNotEqual(messages.ResultFailedMessage, messages.SkipReasonMessage(skipReason));
        }

        [TestCase("NEW_REASON")]
        [TestCase(null)]
        public void UnknownSkipReasonFallsBackToTheFailedMessage(string skipReason)
        {
            var messages = Messages();

            Assert.AreEqual(messages.ResultFailedMessage, messages.SkipReasonMessage(skipReason));
        }

        [Test]
        public void ResultTextsMatchTheWeb()
        {
            var messages = Messages();

            Assert.AreEqual("Calificación del tueste", messages.ResultTitle);
            Assert.AreEqual("77% · Buena", string.Format(messages.ResultScoreWithGradeFormat, 77, messages.GradeName("GOOD")));
            Assert.AreEqual("Gas en el secado: nivel 2 de 3.",
                string.Format(messages.ResultCriterionLevelFormat, messages.CriterionName("DRYING_GAS"), 2, 3));
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
                messages.BakedAlarm, messages.BurnWarningAlarm, messages.AlarmLimitExceededAlarm, messages.ChargeOvershootAlarm,
                messages.ChartTitle, messages.ChartTitleFinal, messages.ChartAxisTitle, messages.PowerOnFirstMessage,
                messages.NotChargedYetMessage, messages.WaitForChargeDipMessage, messages.AlreadyChargedMessage,
                messages.RoastFinishedMessage, messages.ActionNotAvailableMessage, messages.ResultTitle,
                messages.ResultFinalScoreLabel, messages.ResultDefectCapMessage, messages.ResultCriteriaTitle,
                messages.ResultPendingMessage, messages.ResultSlowMessage, messages.ResultFailedMessage,
                messages.ResultLegacyMessage, messages.FeedbackTitle, messages.FeedbackLoadingMessage,
                messages.FeedbackUnavailableMessage
            })
            {
                Assert.IsFalse(string.IsNullOrWhiteSpace(text));
            }

            Assert.AreEqual("Tu sesión venció, vuelve a ingresar", messages.SessionExpiredMessage);
        }
    }
}
