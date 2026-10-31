namespace ToastedVr.RoastSimulation
{
    // Mientras la perilla gira manda como mucho un valor por intervalo; al soltarla manda el último si no se envió.
    public sealed class GasSendThrottle
    {
        private readonly float intervalSeconds;
        private int lastSentGas;
        private float lastSentAt;
        private bool hasSent;

        public GasSendThrottle(float intervalSeconds)
        {
            this.intervalSeconds = intervalSeconds;
        }

        // currentGas es el gas con el que corre el quemador al agarrar la perilla, para no reenviarlo.
        public void Begin(int currentGas)
        {
            lastSentGas = currentGas;
            hasSent = false;
        }

        public bool ShouldSendWhileTurning(int gas, float now)
        {
            if (gas == lastSentGas || (hasSent && now - lastSentAt < intervalSeconds))
            {
                return false;
            }

            MarkSent(gas, now);
            return true;
        }

        public bool ShouldSendOnRelease(int gas, float now)
        {
            if (gas == lastSentGas)
            {
                return false;
            }

            MarkSent(gas, now);
            return true;
        }

        private void MarkSent(int gas, float now)
        {
            lastSentGas = gas;
            lastSentAt = now;
            hasSent = true;
        }
    }
}
