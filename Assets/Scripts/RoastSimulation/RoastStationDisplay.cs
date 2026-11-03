using System.Text;
using TMPro;
using UnityEngine;

namespace ToastedVr.RoastSimulation
{
    public sealed class RoastStationDisplay : MonoBehaviour
    {
        private const string EvaluationPending = "PENDING";

        [SerializeField] private RoastStationController controller;
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private TMP_Text messageText;

        private RoastSessionSummary finishedSession;

        private void OnEnable()
        {
            controller.Changed += Render;
            controller.MessageChanged += ShowMessage;
            controller.Finished += ShowResult;
            controller.EvaluationUpdated += ShowResult;
            Render();
        }

        private void OnDisable()
        {
            controller.Changed -= Render;
            controller.MessageChanged -= ShowMessage;
            controller.Finished -= ShowResult;
            controller.EvaluationUpdated -= ShowResult;
        }

        private void ShowMessage(string message)
        {
            messageText.text = message;
        }

        // Llega al descargar y otra vez cuando la IA termina de calificar, con la nota final.
        private void ShowResult(RoastSessionSummary session)
        {
            if (session == null || !controller.IsPowered)
            {
                return;
            }

            finishedSession = session;
            Render();
        }

        private void Render()
        {
            var texts = controller.Messages;
            var state = controller.State;
            if (!controller.IsPowered)
            {
                finishedSession = null;
            }

            var lines = new StringBuilder();
            var phaseName = controller.IsPowered && state != null ? texts.PhaseName(state.Phase) : texts.PhaseOffName;
            lines.AppendLine(string.Format(texts.PhaseFormat, phaseName));

            if (controller.IsPowered && state != null)
            {
                lines.AppendLine(string.Format(texts.TemperatureFormat, state.Temperature));
                lines.AppendLine(string.Format(texts.RoastTimeFormat, Clock(state.RoastSeconds)));
                lines.AppendLine(string.Format(texts.GasFormat, state.GasPercent));
                if (state.RisePerMinute.HasValue)
                {
                    lines.AppendLine(string.Format(texts.RiseFormat, state.RisePerMinute.Value));
                }

                lines.AppendLine(string.Format(texts.GrainFormat, texts.GrainStateName(state.GrainState)));
                lines.AppendLine(string.Format(texts.FirstCrackFormat, CrackTime(state.FirstCrack, texts)));
                lines.AppendLine(string.Format(texts.SecondCrackFormat, CrackTime(state.SecondCrack, texts)));
                AppendAlarms(lines, state.Alarms, texts);
            }

            if (finishedSession != null)
            {
                lines.AppendLine(string.Format(texts.ScoreFormat, finishedSession.FinalScore, texts.GradeName(finishedSession.Grade)));
                if (finishedSession.Evaluation?.Status == EvaluationPending)
                {
                    lines.AppendLine(texts.EvaluationPendingMessage);
                }
            }

            statusText.text = lines.ToString();
        }

        private static void AppendAlarms(StringBuilder lines, RoastAlarms alarms, RoastStationMessages texts)
        {
            if (alarms == null)
            {
                return;
            }

            if (alarms.Burned) lines.AppendLine(texts.BurnedAlarm);
            if (alarms.Baked) lines.AppendLine(texts.BakedAlarm);
            if (alarms.BurnWarning) lines.AppendLine(texts.BurnWarningAlarm);
            if (alarms.AlarmLimitExceeded) lines.AppendLine(texts.AlarmLimitExceededAlarm);
            if (alarms.ChargeOvershoot) lines.AppendLine(texts.ChargeOvershootAlarm);
        }

        private static string CrackTime(CrackStatus crack, RoastStationMessages texts)
        {
            return crack != null && crack.Reached && crack.RoastSeconds.HasValue
                ? Clock(crack.RoastSeconds.Value)
                : texts.CrackPendingText;
        }

        private static string Clock(int seconds)
        {
            return $"{seconds / 60:00}:{seconds % 60:00}";
        }
    }
}
