namespace ToastedVr.RoastAlerts
{
    public static class AlertPhases
    {
        public const string Preheat = "PREHEAT";
        public const string Ready = "READY";
        public const string ChargeDip = "CHARGE_DIP";
        public const string Roasting = "ROASTING";

        public static bool IsCharged(string phase) => phase == ChargeDip || phase == Roasting;
    }

    // Lo que Unity tiene en cada tick: temperature es lo que marca el termómetro y roastSeconds el reloj de la nota.
    public readonly struct AlertSample
    {
        public AlertSample(string phase, double temperature, int roastSeconds, bool firstCrackReached, double? moisturePct)
        {
            Phase = phase;
            Temperature = temperature;
            RoastSeconds = roastSeconds;
            FirstCrackReached = firstCrackReached;
            MoisturePct = moisturePct;
        }

        public string Phase { get; }
        public double Temperature { get; }
        public int RoastSeconds { get; }
        public bool FirstCrackReached { get; }
        public double? MoisturePct { get; }
    }

    public sealed class AlertSignals
    {
        public double Temperature { get; set; }
        public int RisePerMinute { get; set; }
        public int StagnationSeconds { get; set; }
        public int SecondsAboveCeiling { get; set; }
        public double DevelopmentRatio { get; set; }
        public int RoastSeconds { get; set; }
        public double? MoisturePct { get; set; }
        public string Phase { get; set; }
        public bool FirstCrackReached { get; set; }
        public int SecondsInPhase { get; set; }
        public double? PeakTemperature { get; set; }
        public bool TurningPointReached { get; set; }

        // Los nombres son los del catálogo. null: la señal no tiene valor, o no existe, y la condición no se cumple.
        public object Value(string signal)
        {
            switch (signal)
            {
                case "temperature": return Temperature;
                case "risePerMinute": return (double)RisePerMinute;
                case "stagnationSeconds": return (double)StagnationSeconds;
                case "secondsAboveCeiling": return (double)SecondsAboveCeiling;
                case "developmentRatio": return DevelopmentRatio;
                case "roastSeconds": return (double)RoastSeconds;
                case "moisturePct": return MoisturePct;
                case "phase": return Phase;
                case "firstCrackReached": return FirstCrackReached;
                case "secondsInPhase": return (double)SecondsInPhase;
                case "peakTemperature": return PeakTemperature;
                case "turningPointReached": return TurningPointReached;
                default: return null;
            }
        }
    }
}
