using System;
using System.Collections.Generic;

namespace ToastedVr.RoastSimulation
{
    // Marcas del eje vertical, con la misma regla de ChartAxisScale.js: al menos tickCount marcas separadas por step,
    // que suben con la temperatura y bajan solo si un dato lo necesita.
    public static class TemperatureChartScale
    {
        public static IReadOnlyList<int> VerticalTicks(double minObserved, double maxObserved, int step, int tickCount)
        {
            if (step <= 0 || tickCount < 2)
            {
                throw new ArgumentOutOfRangeException(nameof(step), "The axis needs a positive step and two marks.");
            }

            var headRoom = step;
            var top = Math.Max(step * tickCount, (int)Math.Ceiling((maxObserved + headRoom) / step) * step);
            var defaultBottom = top - (tickCount - 1) * step;
            var bottom = Math.Min(defaultBottom, Math.Max(0, (int)Math.Floor((minObserved - headRoom) / step) * step));

            var ticks = new List<int>();
            for (var tick = bottom; tick <= top; tick += step)
            {
                ticks.Add(tick);
            }

            return ticks;
        }
    }
}
