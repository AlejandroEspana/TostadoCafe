using System.Collections.Generic;
using System.Linq;

namespace ToastedVr.RoastAlerts
{
    public static class AlertDeliveryModes
    {
        public const string Voice = "VOICE";

        // Mientras el jugador graba una pregunta: así el transcriptor no oye la alerta.
        public const string Subtitle = "SUBTITLE";
    }

    public sealed class AlertDelivery
    {
        public AlertDelivery(int second, int roastSeconds, AlertRuleModel rule, string mode)
        {
            Second = second;
            RoastSeconds = roastSeconds;
            Rule = rule;
            Mode = mode;
        }

        public int Second { get; }
        public int RoastSeconds { get; }
        public AlertRuleModel Rule { get; }
        public string Mode { get; }
    }

    // Un tueste de un nivel: Step recibe una muestra por segundo simulado, en orden (IA/app/alerts/evaluator.py).
    public sealed class AlertEvaluator
    {
        private readonly SignalTracker tracker;
        private readonly List<RuleState> states;
        private readonly AlertChannel channel;
        private int second = -1;

        public AlertEvaluator(AlertPackage package)
        {
            tracker = new SignalTracker(package.Signals);
            states = package.Rules.Select(rule => new RuleState(rule)).ToList();
            channel = new AlertChannel(package.Channel);
        }

        public AlertDelivery Step(AlertSample sample, bool recording, bool assistantBusy)
        {
            second++;
            var signals = tracker.Update(sample);
            var quiet = channel.SecondsSinceLastDelivery(second);
            var triggered = states.Where(state => state.Update(signals, second, quiet)).Select(state => state.Rule).ToList();
            var rule = channel.Offer(triggered, second, pending => StateOf(pending).IsActive(signals), assistantBusy);
            if (rule == null)
            {
                return null;
            }

            return new AlertDelivery(second, sample.RoastSeconds, rule, recording ? AlertDeliveryModes.Subtitle : AlertDeliveryModes.Voice);
        }

        private RuleState StateOf(AlertRuleModel rule) => states.First(state => state.Rule == rule);
    }
}
