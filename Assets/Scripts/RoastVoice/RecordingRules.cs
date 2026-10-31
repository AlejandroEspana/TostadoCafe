namespace ToastedVr.RoastVoice
{
    public enum RecordingVerdict
    {
        Accepted,
        TooShort,
        TooLarge
    }

    // Revisa antes de enviar lo mismo que rechazaría el backend, para no gastar un pedido ni la cuota del jugador.
    // Lo demasiado largo no llega aquí: el micrófono se corta solo en el máximo.
    public static class RecordingRules
    {
        public static RecordingVerdict Check(int sampleCount, int sampleRateHz, float minSeconds, int maxBytes)
        {
            // Se compara en muestras enteras: 0,3 s en float no es exacto y dejaría fuera justo el mínimo.
            if (sampleRateHz <= 0 || sampleCount < (int)System.Math.Round(minSeconds * sampleRateHz))
            {
                return RecordingVerdict.TooShort;
            }

            return WavCodec.EncodedLength(sampleCount) > maxBytes ? RecordingVerdict.TooLarge : RecordingVerdict.Accepted;
        }
    }
}
