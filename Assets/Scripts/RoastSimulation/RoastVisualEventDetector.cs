using System.Collections.Generic;

namespace ToastedVr.RoastSimulation
{
    public enum RoastVisualEventKind
    {
        PoweredOn,
        PoweredOff,
        Charged,
        TurningPointReached,
        FirstCrack,
        SecondCrack,
        GrainStateChanged,
        BurnWarningStarted,
        BurnWarningEnded,
        Burned
    }

    public readonly struct RoastVisualEvent
    {
        public RoastVisualEvent(RoastVisualEventKind kind, string value = null)
        {
            Kind = kind;
            Value = value;
        }

        public RoastVisualEventKind Kind { get; }

        // Solo GrainStateChanged lo usa: el grainState nuevo.
        public string Value { get; }
    }

    // Compara cada estado con el anterior y dice qué cambió, para que la tostadora lo muestre.
    // Cada evento sale una sola vez por tueste aunque el estado llegue repetido o se salte ticks.
    public sealed class RoastVisualEventDetector
    {
        private bool powered;
        private string phase;
        private bool firstCrack;
        private bool secondCrack;
        private string grainState;
        private bool burnWarning;
        private bool burned;

        public List<RoastVisualEvent> Update(bool isPowered, RoastState state)
        {
            var events = new List<RoastVisualEvent>();
            if (isPowered != powered)
            {
                Reset();
                powered = isPowered;
                events.Add(new RoastVisualEvent(isPowered ? RoastVisualEventKind.PoweredOn : RoastVisualEventKind.PoweredOff));
            }

            if (!isPowered || state == null)
            {
                return events;
            }

            if (!HasBeans(phase) && HasBeans(state.Phase))
            {
                events.Add(new RoastVisualEvent(RoastVisualEventKind.Charged));
            }

            if (!IsPastTurningPoint(phase) && IsPastTurningPoint(state.Phase))
            {
                events.Add(new RoastVisualEvent(RoastVisualEventKind.TurningPointReached));
            }

            phase = state.Phase;
            AddWhenStarts(events, ref firstCrack, state.FirstCrack?.Reached ?? false, RoastVisualEventKind.FirstCrack);
            AddWhenStarts(events, ref secondCrack, state.SecondCrack?.Reached ?? false, RoastVisualEventKind.SecondCrack);

            // Antes de cargar no hay grano en el tambor; el primer valor después de cargar también se avisa.
            if (HasBeans(state.Phase) && !string.IsNullOrEmpty(state.GrainState) && state.GrainState != grainState)
            {
                grainState = state.GrainState;
                events.Add(new RoastVisualEvent(RoastVisualEventKind.GrainStateChanged, grainState));
            }

            var warning = state.Alarms?.BurnWarning ?? false;
            if (warning != burnWarning)
            {
                burnWarning = warning;
                events.Add(new RoastVisualEvent(warning ? RoastVisualEventKind.BurnWarningStarted : RoastVisualEventKind.BurnWarningEnded));
            }

            AddWhenStarts(events, ref burned, state.Alarms?.Burned ?? false, RoastVisualEventKind.Burned);
            return events;
        }

        private static void AddWhenStarts(List<RoastVisualEvent> events, ref bool current, bool now, RoastVisualEventKind kind)
        {
            if (now && !current)
            {
                events.Add(new RoastVisualEvent(kind));
            }

            current = now;
        }

        private static bool HasBeans(string value) =>
            value == RoastPhaseRules.ChargeDip || value == RoastPhaseRules.Roasting || value == RoastPhaseRules.Finished;

        private static bool IsPastTurningPoint(string value) => value == RoastPhaseRules.Roasting || value == RoastPhaseRules.Finished;

        private void Reset()
        {
            phase = null;
            firstCrack = false;
            secondCrack = false;
            grainState = null;
            burnWarning = false;
            burned = false;
        }
    }
}
