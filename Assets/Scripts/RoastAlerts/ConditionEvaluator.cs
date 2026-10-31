using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace ToastedVr.RoastAlerts
{
    public static class ConditionEvaluator
    {
        // Todas a la vez. Una señal sin valor (la humedad si no llega, el pico antes del punto de retorno) nunca cumple.
        public static bool Holds(IEnumerable<AlertCondition> conditions, AlertSignals signals)
        {
            foreach (var condition in conditions)
            {
                if (!Holds(condition, signals))
                {
                    return false;
                }
            }

            return true;
        }

        public static bool Holds(AlertCondition condition, AlertSignals signals)
        {
            var value = signals.Value(condition.Signal);
            if (value == null || condition.Value == null)
            {
                return false;
            }

            switch (value)
            {
                case double number when IsNumber(condition.Value):
                    return Compare(number.CompareTo(condition.Value.Value<double>()), condition.Op);
                case bool flag when condition.Value.Type == JTokenType.Boolean:
                    return Equality(flag == condition.Value.Value<bool>(), condition.Op);
                case string text when condition.Value.Type == JTokenType.String:
                    return Equality(text == condition.Value.Value<string>(), condition.Op);
                default:
                    return false;
            }
        }

        private static bool IsNumber(JToken token) => token.Type == JTokenType.Integer || token.Type == JTokenType.Float;

        private static bool Compare(int comparison, string op)
        {
            switch (op)
            {
                case "eq": return comparison == 0;
                case "ne": return comparison != 0;
                case "lt": return comparison < 0;
                case "le": return comparison <= 0;
                case "gt": return comparison > 0;
                case "ge": return comparison >= 0;
                default: return false;
            }
        }

        private static bool Equality(bool equal, string op)
        {
            switch (op)
            {
                case "eq": return equal;
                case "ne": return !equal;
                default: return false;
            }
        }
    }
}
