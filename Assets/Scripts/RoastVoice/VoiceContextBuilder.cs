using System;
using ToastedVr.RoastSimulation;

namespace ToastedVr.RoastVoice
{
    // El contrato del simulador dice que el simulationId sirve como roastAttemptId: así el asistente
    // relaciona las preguntas de seguimiento del mismo tueste.
    public static class VoiceContextBuilder
    {
        public static VoiceQuestionContext Build(string simulationId, RoastState state)
        {
            if (string.IsNullOrWhiteSpace(simulationId) || state == null)
            {
                throw new ArgumentException("A question needs an active simulation and its state.");
            }

            return new VoiceQuestionContext
            {
                RoastAttemptId = simulationId,
                RoastState = new VoiceRoastState
                {
                    ElapsedSeconds = state.RoastSeconds,
                    BeanTemperature = state.Temperature,
                    FirstCrackReached = state.FirstCrack?.Reached ?? false,
                    FirstCrackElapsedSeconds = state.FirstCrack?.Reached == true ? state.FirstCrack.RoastSeconds : null,
                    TargetTemperature = state.TargetTemperature,
                    ChargeTemperature = state.ChargeTemperature,
                    HeatLevelPercent = state.GasPercent,
                    TemperatureRisePerMinute = state.RisePerMinute.HasValue
                        ? (int?)Math.Round(state.RisePerMinute.Value, MidpointRounding.AwayFromZero)
                        : null
                }
            };
        }
    }
}
