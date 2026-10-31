using System.Collections.Generic;

namespace ToastedVr.RoastSimulation
{
    // Guarda los ticks en orden para la curva y las alertas. El servidor reenvía lo que falte si se corta el WiFi,
    // así que basta con pedir siempre desde el último tick guardado.
    public sealed class RoastTickBuffer
    {
        private readonly List<RoastState> ticks = new List<RoastState>();

        public int LastTick { get; private set; }

        public IReadOnlyList<RoastState> Ticks => ticks;

        public void Reset(int startTick)
        {
            ticks.Clear();
            LastTick = startTick;
        }

        // Devuelve true si faltan ticks: el servidor solo guarda la última hora y el primero que llegó es posterior a LastTick + 1.
        public bool Append(IEnumerable<RoastState> received)
        {
            var gap = false;
            if (received == null)
            {
                return false;
            }

            foreach (var tick in received)
            {
                if (tick == null || tick.Tick <= LastTick)
                {
                    continue;
                }

                if (tick.Tick > LastTick + 1)
                {
                    gap = true;
                }

                ticks.Add(tick);
                LastTick = tick.Tick;
            }

            return gap;
        }
    }
}
