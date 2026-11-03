using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;

namespace ToastedVr.RoastSimulation.Tests
{
    public sealed class RoastResultTextTests
    {
        private RoastStationMessages texts;

        [SetUp]
        public void LoadTexts()
        {
            texts = AssetDatabase.LoadAssetAtPath<RoastStationMessages>("Assets/Settings/RoastSimulation/RoastStationMessages.asset");
            Assert.IsNotNull(texts);
        }

        private static RoastSessionSummary Session(RoastEvaluationSummary evaluation)
        {
            return new RoastSessionSummary { Id = 42, QualityScore = 80, FinalScore = 77, Grade = "GOOD", Evaluation = evaluation };
        }

        private static RoastEvaluationSummary Completed()
        {
            return new RoastEvaluationSummary
            {
                Status = "COMPLETED",
                FinalScore = 77,
                RulesScore = 80,
                RubricScore = 70.5,
                RulesWeight = 0.7,
                AiWeight = 0.3,
                DefectCapApplied = true,
                Criteria = new List<RoastCriterionScore>
                {
                    new RoastCriterionScore { Id = "DRYING_GAS", Level = 2, MaxLevel = 3, Justification = "Subiste el gas a tiempo." },
                    new RoastCriterionScore { Id = "RISE_TO_FIRST_CRACK", Level = 1, MaxLevel = 3, Justification = "La subida fue lenta." }
                },
                NotApplicable = new List<string> { "ALERT_REACTION", "GAS_AFTER_FIRST_CRACK" }
            };
        }

        [Test]
        public void CompletedEvaluationShowsEveryPartLikeTheWeb()
        {
            var text = RoastResultText.Build(Session(Completed()), false, true, "Buen control del gas.", texts);

            StringAssert.Contains("Nota final: 77% · Buena", text);
            StringAssert.Contains("Reglas del tueste (70 %): 80%", text);
            StringAssert.Contains("Tus decisiones según la IA (30 %): 70.5%", text);
            StringAssert.Contains(texts.ResultDefectCapMessage, text);
            StringAssert.Contains("<b>Cómo calificó la IA tus decisiones</b>", text);
            StringAssert.Contains("• <b>Gas en el secado: nivel 2 de 3.</b> <noparse>Subiste el gas a tiempo.</noparse>", text);
            StringAssert.Contains("• <b>Subida hasta el first crack: nivel 1 de 3.</b>", text);
            StringAssert.Contains("No aplicó en este tueste: Reacción a las alertas, Gas después del first crack.", text);
            StringAssert.Contains("<b>" + texts.FeedbackTitle + "</b>", text);
            StringAssert.Contains("<noparse>Buen control del gas.</noparse>", text);
            Assert.Less(text.IndexOf("Nota final"), text.IndexOf("Reglas del tueste"));
            Assert.Less(text.IndexOf("Cómo calificó"), text.IndexOf(texts.FeedbackTitle));
        }

        [Test]
        public void NoDefectCapAndNoSkippedCriteriaLeaveThoseLinesOut()
        {
            var evaluation = Completed();
            evaluation.DefectCapApplied = false;
            evaluation.NotApplicable.Clear();

            var text = RoastResultText.Build(Session(evaluation), false, true, null, texts);

            StringAssert.DoesNotContain(texts.ResultDefectCapMessage, text);
            StringAssert.DoesNotContain("No aplicó", text);
        }

        [Test]
        public void PendingEvaluationUsesTheRulesScoreAndSaysItIsWaiting()
        {
            var session = Session(new RoastEvaluationSummary { Status = "PENDING" });
            session.FinalScore = null;

            var waiting = RoastResultText.Build(session, false, false, null, texts);
            var slow = RoastResultText.Build(session, true, false, null, texts);

            StringAssert.Contains("Nota final: 80% · Buena", waiting);
            StringAssert.Contains(texts.ResultPendingMessage, waiting);
            StringAssert.DoesNotContain("Reglas del tueste", waiting);
            StringAssert.Contains(texts.ResultSlowMessage, slow);
            StringAssert.DoesNotContain(texts.ResultPendingMessage, slow);
        }

        [Test]
        public void FailedEvaluationSaysTheRulesScoreCounts()
        {
            var text = RoastResultText.Build(Session(new RoastEvaluationSummary { Status = "FAILED" }), false, true, null, texts);

            StringAssert.Contains(texts.ResultFailedMessage, text);
        }

        [TestCase("AUTO_MODE")]
        [TestCase("NO_TRACE")]
        public void SkippedEvaluationExplainsWhy(string skipReason)
        {
            var evaluation = new RoastEvaluationSummary { Status = "NOT_APPLICABLE", SkipReason = skipReason };

            var text = RoastResultText.Build(Session(evaluation), false, true, null, texts);

            StringAssert.Contains(texts.SkipReasonMessage(skipReason), text);
        }

        [Test]
        public void SessionSavedBeforeTheCombinedScoreSaysSo()
        {
            var text = RoastResultText.Build(Session(null), false, true, null, texts);

            StringAssert.Contains(texts.ResultLegacyMessage, text);
        }

        [Test]
        public void FeedbackGoesFromLoadingToTextOrUnavailable()
        {
            var session = Session(Completed());

            StringAssert.Contains(texts.FeedbackLoadingMessage, RoastResultText.Build(session, false, false, null, texts));
            StringAssert.Contains(texts.FeedbackUnavailableMessage, RoastResultText.Build(session, false, true, null, texts));
            var ready = RoastResultText.Build(session, false, true, "Texto", texts);
            StringAssert.DoesNotContain(texts.FeedbackLoadingMessage, ready);
            StringAssert.DoesNotContain(texts.FeedbackUnavailableMessage, ready);
        }

        [Test]
        public void PanelConfigurationIsUsable()
        {
            var configuration = AssetDatabase.LoadAssetAtPath<RoastResultPanelConfiguration>(
                "Assets/Settings/RoastSimulation/RoastResultPanelConfiguration.asset");

            Assert.IsNotNull(configuration);
            Assert.Greater(configuration.PanelWidth, 2f * configuration.Padding);
            Assert.Greater(configuration.PanelHeight, configuration.TitleHeight + 2f * configuration.Padding);
            Assert.Greater(configuration.LayerSpacing, 0f);
            Assert.Greater(configuration.BodyFontSizeMin, 0f);
            Assert.LessOrEqual(configuration.BodyFontSizeMin, configuration.BodyFontSizeMax);
        }

        [Test]
        public void TextFromTheAiIsNotReadAsRichText()
        {
            var evaluation = Completed();
            evaluation.Criteria[0].Justification = "<color=red>x</color>";

            var text = RoastResultText.Build(Session(evaluation), false, true, "<b>hola</b>", texts);

            StringAssert.Contains("<noparse><color=red>x</color></noparse>", text);
            StringAssert.Contains("<noparse><b>hola</b></noparse>", text);
        }
    }
}
