using System;
using System.Collections;
using UnityEngine;

namespace ToastedVr.RoastSimulation
{
    // Igual que la web: mientras la IA califica, vuelve a pedir la sesión cada intervalo hasta tener la
    // nota final. Después de varios intentos deja de preguntar y avisa que está tardando.
    public sealed class RoastEvaluationPoller
    {
        private const string Pending = "PENDING";

        private readonly RoastSessionsApiClient client;
        private readonly float intervalSeconds;
        private readonly int maxAttempts;
        private int generation;

        public RoastEvaluationPoller(RoastSessionsApiClient client, float intervalSeconds, int maxAttempts)
        {
            this.client = client;
            this.intervalSeconds = intervalSeconds;
            this.maxAttempts = maxAttempts;
        }

        public event Action<RoastSessionSummary> Updated;
        public event Action TimedOut;

        public static bool IsPending(RoastSessionSummary session) => session?.Evaluation?.Status == Pending;

        // La generación se toma al llamar, para que un Stop inmediato también cuente.
        public IEnumerator Run(long sessionId)
        {
            return Loop(sessionId, ++generation);
        }

        public void Stop()
        {
            generation++;
        }

        private IEnumerator Loop(long sessionId, int myGeneration)
        {
            for (var attempt = 0; attempt < maxAttempts; attempt++)
            {
                yield return new WaitForSecondsRealtime(intervalSeconds);
                if (myGeneration != generation)
                {
                    yield break;
                }

                RoastApiResult<RoastSessionSummary> result = null;
                yield return client.GetSession(sessionId, r => result = r);
                if (myGeneration != generation)
                {
                    yield break;
                }

                if (result.IsSuccess && !IsPending(result.Value))
                {
                    Updated?.Invoke(result.Value);
                    yield break;
                }

                // Un fallo de red no cambia nada y se reintenta; sin sesión o sin permiso ya no hay a quién preguntar.
                if (result.Failure == RoastApiFailure.NotFound || result.Failure == RoastApiFailure.Unauthorized)
                {
                    yield break;
                }
            }

            if (myGeneration == generation)
            {
                TimedOut?.Invoke();
            }
        }
    }
}
