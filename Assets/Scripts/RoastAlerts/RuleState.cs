namespace ToastedVr.RoastAlerts
{
    // Estado de una regla durante un tueste: sostén, re-arme, espera entre repeticiones y máximo (IA/app/alerts/rules.py).
    public sealed class RuleState
    {
        private int heldSeconds;
        private bool armed = true;
        private int timesFired;
        private int? lastFiredSecond;

        public RuleState(AlertRuleModel rule)
        {
            Rule = rule;
        }

        public AlertRuleModel Rule { get; }

        // secondsSinceLastAlert es double para representar "nunca" con infinito, como la referencia.
        public bool Update(AlertSignals signals, int second, double secondsSinceLastAlert)
        {
            var active = IsActive(signals);
            heldSeconds = active ? heldSeconds + 1 : 0;
            if (!armed)
            {
                armed = Rule.RearmWhen == null ? !active : ConditionEvaluator.Holds(Rule.RearmWhen, signals);
            }

            if (!IsReady(second, secondsSinceLastAlert))
            {
                return false;
            }

            armed = false;
            timesFired++;
            lastFiredSecond = second;
            return true;
        }

        public bool IsActive(AlertSignals signals) => ConditionEvaluator.Holds(Rule.When, signals);

        private bool IsReady(int second, double secondsSinceLastAlert)
        {
            var cooledDown = !lastFiredSecond.HasValue || second - lastFiredSecond.Value >= Rule.CooldownSeconds;
            return armed && cooledDown
                && heldSeconds >= Rule.SustainSeconds
                && timesFired < Rule.MaxRepeats
                && secondsSinceLastAlert >= Rule.RequiresQuietSeconds;
        }
    }
}
