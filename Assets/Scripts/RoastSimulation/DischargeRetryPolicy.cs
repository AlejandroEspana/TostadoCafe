namespace ToastedVr.RoastSimulation
{
    // En el tick en que la sonda llega al punto de retorno el pico todavía no cuenta y la descarga responde 409.
    // Solo ese 409 se repite: en otra fase el 409 es un error real del jugador y se muestra.
    public static class DischargeRetryPolicy
    {
        public static bool ShouldRetry(RoastApiFailure failure, string lastKnownPhase, int retriesDone, int maxRetries)
        {
            return failure == RoastApiFailure.InvalidPhase
                && RoastPhaseRules.CanDischarge(lastKnownPhase)
                && retriesDone < maxRetries;
        }
    }
}
