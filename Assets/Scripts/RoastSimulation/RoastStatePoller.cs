using System;
using System.Collections;
using UnityEngine;

namespace ToastedVr.RoastSimulation
{
    // Consulta el estado con afterTick: un pedido a la vez y espera el intervalo después de cada respuesta.
    // Consultar también mantiene viva la simulación, que el servidor borra a los 120 s sin consultas.
    public sealed class RoastStatePoller
    {
        private readonly RoastSimulationApiClient client;
        private readonly float intervalSeconds;
        private int generation;

        public RoastStatePoller(RoastSimulationApiClient client, float intervalSeconds)
        {
            this.client = client;
            this.intervalSeconds = intervalSeconds;
        }

        public RoastTickBuffer Buffer { get; } = new RoastTickBuffer();

        public bool IsRunning { get; private set; }

        public event Action<RoastState> StateReceived;

        // Una vez por tick nuevo, en orden y sin repetir, antes de StateReceived.
        public event Action<RoastState> TickReceived;
        public event Action<RoastApiResult<SimulationPoll>> PollFailed;

        // La generación se toma al llamar y no en el primer paso de la corrutina, para que un Stop inmediato también cuente.
        public IEnumerator Run(string simulationId, int startTick)
        {
            var myGeneration = ++generation;
            Buffer.Reset(startTick);
            IsRunning = true;
            return Loop(simulationId, myGeneration);
        }

        private IEnumerator Loop(string simulationId, int myGeneration)
        {
            while (myGeneration == generation)
            {
                RoastApiResult<SimulationPoll> result = null;
                yield return client.Poll(simulationId, Buffer.LastTick, r => result = r);

                if (myGeneration != generation)
                {
                    yield break;
                }

                if (result.IsSuccess)
                {
                    var known = Buffer.Ticks.Count;
                    if (Buffer.Append(result.Value.Ticks))
                    {
                        LogDiagnostic("Some ticks were older than what the server keeps; the curve has a gap.");
                    }

                    for (var index = known; index < Buffer.Ticks.Count && myGeneration == generation; index++)
                    {
                        TickReceived?.Invoke(Buffer.Ticks[index]);
                    }

                    StateReceived?.Invoke(result.Value.State);
                    if (result.Value.State?.Phase == RoastPhaseRules.Finished)
                    {
                        break;
                    }
                }
                else
                {
                    PollFailed?.Invoke(result);
                    if (result.Failure == RoastApiFailure.NotFound || result.Failure == RoastApiFailure.Unauthorized)
                    {
                        break;
                    }
                }

                yield return new WaitForSecondsRealtime(intervalSeconds);
            }

            if (myGeneration == generation)
            {
                IsRunning = false;
            }
        }

        public void Stop()
        {
            generation++;
            IsRunning = false;
        }

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        private static void LogDiagnostic(string message)
        {
            Debug.Log($"[RoastSimulation] {message}");
        }
    }
}
