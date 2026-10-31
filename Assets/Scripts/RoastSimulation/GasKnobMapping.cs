using System;

namespace ToastedVr.RoastSimulation
{
    // Convierte el ángulo de la perilla en gas por saltos y al revés. El ángulo mínimo puede ser mayor que el máximo
    // si la perilla abre girando hacia el otro lado.
    public sealed class GasKnobMapping
    {
        private readonly float minAngle;
        private readonly float maxAngle;
        private readonly int minGas;
        private readonly int maxGas;
        private readonly int step;

        public GasKnobMapping(float minAngle, float maxAngle, int minGas, int maxGas, int step)
        {
            if (Math.Abs(maxAngle - minAngle) < float.Epsilon)
            {
                throw new ArgumentException("The knob needs a non-empty angle range.");
            }

            if (maxGas <= minGas || step <= 0)
            {
                throw new ArgumentException("The gas range and step must be positive.");
            }

            this.minAngle = minAngle;
            this.maxAngle = maxAngle;
            this.minGas = minGas;
            this.maxGas = maxGas;
            this.step = step;
        }

        public float ClampAngle(float angle)
        {
            return Math.Min(Math.Max(angle, Math.Min(minAngle, maxAngle)), Math.Max(minAngle, maxAngle));
        }

        public int GasForAngle(float angle)
        {
            var fraction = (ClampAngle(angle) - minAngle) / (maxAngle - minAngle);
            var rawGas = minGas + fraction * (maxGas - minGas);
            var steps = (int)Math.Round((rawGas - minGas) / step, MidpointRounding.AwayFromZero);

            // El tope siempre se alcanza aunque el rango no sea múltiplo del salto.
            return Math.Min(minGas + steps * step, maxGas);
        }

        public float AngleForGas(int gas)
        {
            var clampedGas = Math.Min(Math.Max(gas, minGas), maxGas);
            var fraction = (float)(clampedGas - minGas) / (maxGas - minGas);
            return minAngle + fraction * (maxAngle - minAngle);
        }
    }
}
