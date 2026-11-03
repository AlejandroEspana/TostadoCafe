using System;
using System.Collections.Generic;

namespace ToastedVr.RoastAlerts
{
    // Lleva el historial de un tueste y entrega las señales de cada tick, igual que IA/app/alerts/signals.py.
    public sealed class SignalTracker
    {
        private readonly SignalSettings settings;
        private readonly Queue<double> window = new Queue<double>();
        private readonly List<double> windowView = new List<double>();
        private AlertSample? previous;
        private AlertSample? twoBefore;
        private int phaseSeconds;
        private int stagnation;
        private int aboveCeiling;
        private double? peak;
        private double? lowestAfterCharge;
        private bool turningPoint;
        private int? firstCrackRoastSeconds;

        public SignalTracker(SignalSettings settings)
        {
            this.settings = settings;
        }

        // Math.round de JavaScript: los medios suben (2,5 da 3 y −2,5 da −2), a diferencia de Math.Round de .NET.
        public static int JsRound(double value) => (int)Math.Floor(value + 0.5);

        public AlertSignals Update(AlertSample sample)
        {
            var charged = AlertPhases.IsCharged(sample.Phase);
            if (charged && (!previous.HasValue || !AlertPhases.IsCharged(previous.Value.Phase)))
            {
                // Al cargar el termómetro pasa del aire al grano: las lecturas del aire marcarían una caída falsa.
                window.Clear();
            }

            window.Enqueue(sample.Temperature);
            while (window.Count > settings.RiseWindowSeconds + 1)
            {
                window.Dequeue();
            }

            var samePhase = previous.HasValue && previous.Value.Phase == sample.Phase;
            phaseSeconds = samePhase ? phaseSeconds + 1 : 1;
            if (charged)
            {
                TrackCharged(sample);
            }

            stagnation = NextStagnation(sample);
            if (sample.FirstCrackReached && !firstCrackRoastSeconds.HasValue)
            {
                firstCrackRoastSeconds = sample.RoastSeconds;
            }

            var signals = new AlertSignals
            {
                Temperature = sample.Temperature,
                RisePerMinute = RisePerMinute(),
                StagnationSeconds = stagnation,
                SecondsAboveCeiling = aboveCeiling,
                DevelopmentRatio = DevelopmentRatio(sample.RoastSeconds),
                RoastSeconds = sample.RoastSeconds,
                MoisturePct = sample.MoisturePct,
                Phase = sample.Phase,
                FirstCrackReached = sample.FirstCrackReached,
                SecondsInPhase = phaseSeconds,
                PeakTemperature = peak,
                TurningPointReached = turningPoint
            };

            twoBefore = previous;
            previous = sample;
            return signals;
        }

        private void TrackCharged(AlertSample sample)
        {
            var temperature = sample.Temperature;
            if (sample.Phase == AlertPhases.Roasting)
            {
                // Durante la carga la sonda todavía marca el calor del tambor: pico y techo cuentan desde el punto de retorno.
                peak = peak.HasValue ? Math.Max(peak.Value, temperature) : temperature;
                aboveCeiling = temperature > settings.BurnCeilingTemperature ? aboveCeiling + 1 : 0;
            }

            if (!lowestAfterCharge.HasValue || temperature < lowestAfterCharge.Value)
            {
                lowestAfterCharge = temperature;
            }

            var rise = temperature - lowestAfterCharge.Value;
            if (rise > 0 && rise >= settings.TurningPointMinRise)
            {
                turningPoint = true;
            }
        }

        private int RisePerMinute()
        {
            var elapsed = window.Count - 1;
            if (elapsed <= 0)
            {
                return 0;
            }

            windowView.Clear();
            windowView.AddRange(window);
            return JsRound((windowView[windowView.Count - 1] - windowView[0]) / elapsed * 60);
        }

        // MaillardStagnationMonitor: solo en ROASTING y sin primer crack en el tick anterior; compara con dos ticks atrás.
        private int NextStagnation(AlertSample sample)
        {
            var firstCrackBefore = previous.HasValue && previous.Value.FirstCrackReached;
            if (sample.Phase != AlertPhases.Roasting || firstCrackBefore)
            {
                return stagnation;
            }

            if (sample.Temperature < settings.MaillardStartTemperature || sample.Temperature > settings.MaillardEndTemperature)
            {
                return stagnation;
            }

            var reference = twoBefore ?? previous ?? sample;
            return sample.Temperature <= reference.Temperature ? stagnation + 1 : 0;
        }

        private double DevelopmentRatio(int roastSeconds)
        {
            if (!firstCrackRoastSeconds.HasValue || roastSeconds <= 0)
            {
                return 0;
            }

            return (double)(roastSeconds - firstCrackRoastSeconds.Value) / roastSeconds;
        }
    }
}
