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
            if (sampleRateHz <= 0 || (float)sampleCount / sampleRateHz < minSeconds)
            {
                return RecordingVerdict.TooShort;
            }

            return WavCodec.EncodedLength(sampleCount) > maxBytes ? RecordingVerdict.TooLarge : RecordingVerdict.Accepted;
        }
    }
}
