using System;
using System.Globalization;
using System.Linq;
using System.Text;

namespace ToastedVr.RoastSimulation
{
    // Arma el texto del resultado igual que RoastEvaluationBreakdown y la retroalimentación de la web.
    public static class RoastResultText
    {
        private const string Completed = "COMPLETED";
        private const string Pending = "PENDING";
        private const string Failed = "FAILED";
        private const string NotApplicable = "NOT_APPLICABLE";
        private const string ListSeparator = ", ";

        // feedbackArrived en false muestra "generando"; si llegó y es null, no estuvo disponible.
        public static string Build(RoastSessionSummary session, bool evaluationTimedOut, bool feedbackArrived, string feedback,
            RoastStationMessages texts)
        {
            var lines = new StringBuilder();
            var evaluation = session.Evaluation;
            var score = Format(texts.ResultScoreWithGradeFormat, session.FinalScore ?? session.QualityScore, texts.GradeName(session.Grade));
            Row(lines, texts, texts.ResultFinalScoreLabel, score);

            switch (evaluation == null ? null : evaluation.Status ?? string.Empty)
            {
                case null:
                    lines.AppendLine(texts.ResultLegacyMessage);
                    break;
                case Completed:
                    AppendCompleted(lines, evaluation, texts);
                    break;
                case Pending:
                    lines.AppendLine(evaluationTimedOut ? texts.ResultSlowMessage : texts.ResultPendingMessage);
                    break;
                case Failed:
                    lines.AppendLine(texts.ResultFailedMessage);
                    break;
                case NotApplicable:
                    lines.AppendLine(texts.SkipReasonMessage(evaluation.SkipReason));
                    break;
            }

            lines.AppendLine();
            lines.AppendLine(Format(texts.ResultSectionFormat, texts.FeedbackTitle));
            if (!feedbackArrived)
            {
                lines.AppendLine(texts.FeedbackLoadingMessage);
            }
            else
            {
                lines.AppendLine(feedback == null ? texts.FeedbackUnavailableMessage : Plain(feedback));
            }

            return lines.ToString().TrimEnd();
        }

        private static void AppendCompleted(StringBuilder lines, RoastEvaluationSummary evaluation, RoastStationMessages texts)
        {
            Row(lines, texts, Format(texts.ResultRulesScoreFormat, Percent(evaluation.RulesWeight)), Format(texts.ResultPartScoreFormat, evaluation.RulesScore));
            Row(lines, texts, Format(texts.ResultRubricScoreFormat, Percent(evaluation.AiWeight)), Format(texts.ResultPartScoreFormat, evaluation.RubricScore));
            if (evaluation.DefectCapApplied)
            {
                lines.AppendLine(texts.ResultDefectCapMessage);
            }

            lines.AppendLine();
            lines.AppendLine(Format(texts.ResultSectionFormat, texts.ResultCriteriaTitle));
            foreach (var criterion in evaluation.Criteria ?? Enumerable.Empty<RoastCriterionScore>())
            {
                var level = Format(texts.ResultCriterionLevelFormat, texts.CriterionName(criterion.Id), criterion.Level, criterion.MaxLevel);
                lines.AppendLine(Format(texts.ResultCriterionFormat, level, Plain(criterion.Justification)));
            }

            if (evaluation.NotApplicable != null && evaluation.NotApplicable.Count > 0)
            {
                var names = string.Join(ListSeparator, evaluation.NotApplicable.Select(texts.CriterionName));
                lines.AppendLine(Format(texts.ResultNotApplicableFormat, names));
            }
        }

        private static void Row(StringBuilder lines, RoastStationMessages texts, string label, string value)
        {
            lines.AppendLine(Format(texts.ResultRowFormat, label, value));
        }

        // La web redondea con Math.round: 0.7 → 70.
        private static double? Percent(double? weight)
        {
            return weight.HasValue ? Math.Round(weight.Value * 100, MidpointRounding.AwayFromZero) : (double?)null;
        }

        // Los números con punto, como en la web.
        private static string Format(string format, params object[] values)
        {
            return string.Format(CultureInfo.InvariantCulture, format, values);
        }

        // Lo que escribe la IA no debe interpretarse como etiquetas de TextMeshPro.
        private static string Plain(string text)
        {
            return "<noparse>" + (text ?? string.Empty) + "</noparse>";
        }
    }
}
