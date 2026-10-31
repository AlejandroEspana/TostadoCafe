using ToastedVr.RoastSimulation;

namespace ToastedVr.RoastAlerts
{
    public static class AlertSampleFactory
    {
        // El evaluador corre desde READY hasta la descarga; el precalentado y el final no cuentan.
        public static bool TryCreate(RoastState tick, out AlertSample sample)
        {
            sample = default;
            if (tick == null || !(tick.Phase == AlertPhases.Ready || AlertPhases.IsCharged(tick.Phase)))
            {
                return false;
            }

            sample = new AlertSample(tick.Phase, tick.Temperature, tick.RoastSeconds, tick.FirstCrack?.Reached ?? false, tick.MoisturePct);
            return true;
        }
    }
}
