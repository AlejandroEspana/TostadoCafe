using System;
using System.Collections.Generic;
using System.Linq;

namespace ToastedVr.RoastAlerts
{
    // Un solo canal de voz: decide qué alerta sale en cada segundo y cuáles esperan (IA/app/alerts/channel.py).
    public sealed class AlertChannel
    {
        private static readonly string[] PriorityOrder = { "CRITICAL", "HIGH", "NORMAL", "LOW" };

        private readonly ChannelPolicy policy;
        private List<PendingAlert> queue = new List<PendingAlert>();
        private int? lastDeliverySecond;

        public AlertChannel(ChannelPolicy policy)
        {
            this.policy = policy;
        }

        public double SecondsSinceLastDelivery(int second) =>
            lastDeliverySecond.HasValue ? second - lastDeliverySecond.Value : double.PositiveInfinity;

        // Como máximo una alerta por segundo; null si ninguna puede salir ahora.
        public AlertRuleModel Offer(IEnumerable<AlertRuleModel> triggered, int second, Func<AlertRuleModel, bool> isActive,
            bool assistantBusy)
        {
            var waiting = queue.Where(pending => second - pending.QueuedAt <= policy.QueueMaxWaitSeconds && isActive(pending.Rule));
            // OrderBy es estable, como sort de Python: a igual prioridad y segundo se respeta el orden del paquete.
            var candidates = waiting
                .Concat(triggered.Select(rule => new PendingAlert(rule, second)))
                .OrderBy(pending => Rank(pending.Rule.Priority))
                .ThenBy(pending => pending.QueuedAt)
                .ToList();

            var chosen = candidates.Count > 0 && CanDeliver(candidates[0].Rule.Priority, second, assistantBusy)
                ? candidates[0]
                : null;
            queue = candidates
                .Where(pending => pending != chosen && !policy.UnqueuedPriorities.Contains(pending.Rule.Priority))
                .ToList();
            if (chosen == null)
            {
                return null;
            }

            lastDeliverySecond = second;
            return chosen.Rule;
        }

        private static int Rank(string priority)
        {
            var index = Array.IndexOf(PriorityOrder, priority);
            return index < 0 ? PriorityOrder.Length : index;
        }

        private bool CanDeliver(string priority, int second, bool assistantBusy)
        {
            if (policy.UrgentPriorities.Contains(priority))
            {
                return true;
            }

            return !assistantBusy && SecondsSinceLastDelivery(second) >= policy.MinGapSeconds;
        }

        private sealed class PendingAlert
        {
            public PendingAlert(AlertRuleModel rule, int queuedAt)
            {
                Rule = rule;
                QueuedAt = queuedAt;
            }

            public AlertRuleModel Rule { get; }
            public int QueuedAt { get; }
        }
    }
}
