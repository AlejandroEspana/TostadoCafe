using System;
using System.IO;
using System.Text;

namespace ToastedVr.RoastVoice
{
    public sealed class DecodedWav
    {
        public DecodedWav(float[] samples, int sampleRateHz, int channels)
        {
            Samples = samples;
            SampleRateHz = sampleRateHz;
            Channels = channels;
        }

        public float[] Samples { get; }
        public int SampleRateHz { get; }
        public int Channels { get; }
    }

    // WAV PCM de 16 bits: el formato que pide el backend para la pregunta y el que devuelve en la respuesta.
    public static class WavCodec
    {
        private const short PcmFormat = 1;
        private const short BitsPerSample = 16;
        private const int HeaderBytes = 44;

        public static int EncodedLength(int sampleCount) => HeaderBytes + sampleCount * 2;

        public static byte[] EncodeMono16(float[] samples, int sampleRateHz)
        {
            var dataBytes = samples.Length * 2;
            using (var stream = new MemoryStream(HeaderBytes + dataBytes))
            using (var writer = new BinaryWriter(stream))
            {
                writer.Write(Encoding.ASCII.GetBytes("RIFF"));
                writer.Write(36 + dataBytes);
                writer.Write(Encoding.ASCII.GetBytes("WAVE"));
                writer.Write(Encoding.ASCII.GetBytes("fmt "));
                writer.Write(16);
                writer.Write(PcmFormat);
                writer.Write((short)1);
                writer.Write(sampleRateHz);
                writer.Write(sampleRateHz * 2);
                writer.Write((short)2);
                writer.Write(BitsPerSample);
                writer.Write(Encoding.ASCII.GetBytes("data"));
                writer.Write(dataBytes);
                foreach (var sample in samples)
                {
                    var clamped = Math.Max(-1f, Math.Min(1f, sample));
                    writer.Write((short)Math.Round(clamped * short.MaxValue));
                }

                return stream.ToArray();
            }
        }

        // Recorre los bloques en vez de asumir 44 bytes de cabecera: Azure y Piper pueden agregar bloques extra.
        public static DecodedWav DecodePcm16(byte[] wav)
        {
            if (wav == null || wav.Length < 12 || ReadTag(wav, 0) != "RIFF" || ReadTag(wav, 8) != "WAVE")
            {
                throw new FormatException("Not a RIFF/WAVE file.");
            }

            int? channels = null;
            int sampleRate = 0;
            var position = 12;
            while (position + 8 <= wav.Length)
            {
                var tag = ReadTag(wav, position);
                var size = BitConverter.ToInt32(wav, position + 4);
                var body = position + 8;
                if (size < 0 || body + size > wav.Length)
                {
                    size = wav.Length - body;
                }

                if (tag == "fmt ")
                {
                    if (size < 16 || BitConverter.ToInt16(wav, body) != PcmFormat || BitConverter.ToInt16(wav, body + 14) != BitsPerSample)
                    {
                        throw new FormatException("Only PCM 16-bit WAV is supported.");
                    }

                    channels = BitConverter.ToInt16(wav, body + 2);
                    sampleRate = BitConverter.ToInt32(wav, body + 4);
                }
                else if (tag == "data")
                {
                    if (!channels.HasValue || channels.Value < 1 || sampleRate <= 0)
                    {
                        throw new FormatException("The data chunk came before a valid fmt chunk.");
                    }

                    var samples = new float[size / 2];
                    for (var i = 0; i < samples.Length; i++)
                    {
                        samples[i] = BitConverter.ToInt16(wav, body + i * 2) / 32768f;
                    }

                    return new DecodedWav(samples, sampleRate, channels.Value);
                }

                position = body + size + (size & 1);
            }

            throw new FormatException("The WAV has no data chunk.");
        }

        private static string ReadTag(byte[] bytes, int offset) => Encoding.ASCII.GetString(bytes, offset, 4);
    }
}
