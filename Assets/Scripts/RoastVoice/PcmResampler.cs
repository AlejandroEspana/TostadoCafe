using System;

namespace ToastedVr.RoastVoice
{
    // El backend pide mono de 16 kHz y el micrófono del visor puede grabar a otra frecuencia o en estéreo.
    // ponytail: interpolación lineal sin filtro antialiasing; basta para voz hacia el transcriptor, un filtro si se oye mal.
    public static class PcmResampler
    {
        public static float[] ToMono(float[] interleaved, int channels)
        {
            if (channels <= 1)
            {
                return interleaved;
            }

            var frames = interleaved.Length / channels;
            var mono = new float[frames];
            for (var frame = 0; frame < frames; frame++)
            {
                var sum = 0f;
                for (var channel = 0; channel < channels; channel++)
                {
                    sum += interleaved[frame * channels + channel];
                }

                mono[frame] = sum / channels;
            }

            return mono;
        }

        public static float[] Resample(float[] mono, int fromRateHz, int toRateHz)
        {
            if (fromRateHz <= 0 || toRateHz <= 0)
            {
                throw new ArgumentException("Sample rates must be positive.");
            }

            if (fromRateHz == toRateHz || mono.Length == 0)
            {
                return mono;
            }

            var length = (int)((long)mono.Length * toRateHz / fromRateHz);
            var result = new float[length];
            var step = (double)fromRateHz / toRateHz;
            for (var i = 0; i < length; i++)
            {
                var position = i * step;
                var index = (int)position;
                var next = Math.Min(index + 1, mono.Length - 1);
                var fraction = (float)(position - index);
                result[i] = mono[index] + (mono[next] - mono[index]) * fraction;
            }

            return result;
        }
    }
}
