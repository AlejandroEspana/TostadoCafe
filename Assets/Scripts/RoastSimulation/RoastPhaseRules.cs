namespace ToastedVr.RoastSimulation
{
    // Las mismas reglas de la web: se carga en PREHEAT o READY y se descarga en ROASTING.
    public static class RoastPhaseRules
    {
        public const string Preheat = "PREHEAT";
        public const string Ready = "READY";
        public const string ChargeDip = "CHARGE_DIP";
        public const string Roasting = "ROASTING";
        public const string Finished = "FINISHED";

        public static bool CanCharge(string phase) => phase == Preheat || phase == Ready;

        public static bool CanDischarge(string phase) => phase == Roasting;

        public static bool CanChangeGas(string phase) => IsRunning(phase);

        public static bool IsRunning(string phase) =>
            phase == Preheat || phase == Ready || phase == ChargeDip || phase == Roasting;
    }
}
