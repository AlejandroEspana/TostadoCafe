using System.Collections.Generic;

namespace ToastedVr.RoastSimulation
{
    // Las alertas que el jugador recibió en este tueste, para la traza de la descarga (criterio ALERT_REACTION).
    // Respeta los límites que valida el backend, porque una sola alerta inválida invalida toda la traza.
    public sealed class DeliveredAlertLog
    {
        private readonly List<ProactiveAlert> alerts = new List<ProactiveAlert>();
        private readonly int maxAlerts;
        private readonly int maxIdLength;

        public DeliveredAlertLog(int maxAlerts, int maxIdLength)
        {
            this.maxAlerts = maxAlerts;
            this.maxIdLength = maxIdLength;
        }

        public IReadOnlyList<ProactiveAlert> Alerts => alerts;

        public void Record(string id, int roastSeconds)
        {
            if (alerts.Count >= maxAlerts || string.IsNullOrWhiteSpace(id) || id.Length > maxIdLength || roastSeconds < 0)
            {
                return;
            }

            alerts.Add(new ProactiveAlert { Id = id, T = roastSeconds });
        }

        public List<ProactiveAlert> Snapshot() => new List<ProactiveAlert>(alerts);

        public void Clear() => alerts.Clear();
    }
}
