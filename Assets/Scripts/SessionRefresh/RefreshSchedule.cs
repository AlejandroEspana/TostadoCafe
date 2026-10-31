using System;
using System.Globalization;

namespace ToastedVr.SessionRefresh
{
    public static class RefreshSchedule
    {
        public static bool TryParseExpiry(string expiresAt, out DateTimeOffset expiry)
        {
            return DateTimeOffset.TryParse(
                expiresAt,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out expiry);
        }

        // Devuelve null si el reloj del visor va tan adelantado que el token parece vencido al recibirlo:
        // renovar en ese caso entraría en ciclo, y el 401 del siguiente pedido se encarga.
        public static TimeSpan? DelayUntilRefresh(DateTimeOffset receivedAt, DateTimeOffset expiresAt, DateTimeOffset now, TimeSpan margin)
        {
            var lifetime = expiresAt - receivedAt;
            if (lifetime <= TimeSpan.Zero)
            {
                return null;
            }

            // Con un token más corto que el doble del margen se renueva a la mitad de su vida, para no renovar sin parar.
            var effectiveMargin = lifetime < margin + margin
                ? TimeSpan.FromTicks(lifetime.Ticks / 2)
                : margin;

            var delay = expiresAt - effectiveMargin - now;
            return delay > TimeSpan.Zero ? delay : TimeSpan.Zero;
        }
    }
}
