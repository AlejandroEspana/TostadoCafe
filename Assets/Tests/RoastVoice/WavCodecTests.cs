using System;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace ToastedVr.RoastVoice.Tests
{
    public sealed class WavCodecTests
    {
        [Test]
        public void EncodesTheHeaderTheBackendExpects()
        {
            var wav = WavCodec.EncodeMono16(new float[16000], 16000);

            Assert.AreEqual(WavCodec.EncodedLength(16000), wav.Length);
            Assert.AreEqual("RIFF", Encoding.ASCII.GetString(wav, 0, 4));
            Assert.AreEqual(wav.Length - 8, BitConverter.ToInt32(wav, 4));
            Assert.AreEqual("WAVEfmt ", Encoding.ASCII.GetString(wav, 8, 8));
            Assert.AreEqual(16, BitConverter.ToInt32(wav, 16));
            Assert.AreEqual(1, BitConverter.ToInt16(wav, 20), "PCM");
            Assert.AreEqual(1, BitConverter.ToInt16(wav, 22), "mono");
            Assert.AreEqual(16000, BitConverter.ToInt32(wav, 24), "frecuencia");
            Assert.AreEqual(32000, BitConverter.ToInt32(wav, 28), "bytes por segundo");
            Assert.AreEqual(2, BitConverter.ToInt16(wav, 32), "alineación");
            Assert.AreEqual(16, BitConverter.ToInt16(wav, 34), "bits");
            Assert.AreEqual("data", Encoding.ASCII.GetString(wav, 36, 4));
            Assert.AreEqual(32000, BitConverter.ToInt32(wav, 40));
        }

        [Test]
        public void ClampsAndConvertsSamples()
        {
            var wav = WavCodec.EncodeMono16(new[] { 0f, 1f, -1f, 2f, -2f, 0.5f }, 16000);

            var pcm = Enumerable.Range(0, 6).Select(i => BitConverter.ToInt16(wav, 44 + i * 2)).ToArray();
            CollectionAssert.AreEqual(new short[] { 0, 32767, -32767, 32767, -32767, 16384 }, pcm);
        }

        [Test]
        public void RoundTripKeepsTheAudio()
        {
            var samples = new[] { 0f, 0.25f, -0.25f, 0.9f, -0.9f };

            var decoded = WavCodec.DecodePcm16(WavCodec.EncodeMono16(samples, 16000));

            Assert.AreEqual(16000, decoded.SampleRateHz);
            Assert.AreEqual(1, decoded.Channels);
            for (var i = 0; i < samples.Length; i++)
            {
                Assert.AreEqual(samples[i], decoded.Samples[i], 1e-4);
            }
        }

        [Test]
        public void ReadsTheAssistantVoiceWithExtraChunks()
        {
            var wav = BuildWav(22050, 1, new short[] { 1000, -1000, 0 }, extraChunk: true);

            var decoded = WavCodec.DecodePcm16(wav);

            Assert.AreEqual(22050, decoded.SampleRateHz);
            Assert.AreEqual(3, decoded.Samples.Length);
            Assert.AreEqual(1000 / 32768f, decoded.Samples[0], 1e-6);
        }

        [Test]
        public void ReadsStereo()
        {
            var decoded = WavCodec.DecodePcm16(BuildWav(44100, 2, new short[] { 1, 2, 3, 4 }, extraChunk: false));

            Assert.AreEqual(2, decoded.Channels);
            Assert.AreEqual(4, decoded.Samples.Length);
        }

        [Test]
        public void RejectsWhatIsNotPcm16()
        {
            Assert.Throws<FormatException>(() => WavCodec.DecodePcm16(null));
            Assert.Throws<FormatException>(() => WavCodec.DecodePcm16(Encoding.ASCII.GetBytes("not a wav file at all")));
            Assert.Throws<FormatException>(() => WavCodec.DecodePcm16(BuildWav(16000, 1, new short[] { 1 }, false, bitsPerSample: 8)));
            Assert.Throws<FormatException>(() => WavCodec.DecodePcm16(BuildWav(16000, 1, new short[] { 1 }, false, format: 3)));
        }

        private static byte[] BuildWav(int rate, short channels, short[] samples, bool extraChunk, short bitsPerSample = 16, short format = 1)
        {
            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream))
            {
                writer.Write(Encoding.ASCII.GetBytes("RIFF"));
                writer.Write(0);
                writer.Write(Encoding.ASCII.GetBytes("WAVE"));
                writer.Write(Encoding.ASCII.GetBytes("fmt "));
                writer.Write(16);
                writer.Write(format);
                writer.Write(channels);
                writer.Write(rate);
                writer.Write(rate * channels * 2);
                writer.Write((short)(channels * 2));
                writer.Write(bitsPerSample);
                if (extraChunk)
                {
                    writer.Write(Encoding.ASCII.GetBytes("LIST"));
                    writer.Write(3);
                    writer.Write(new byte[] { 1, 2, 3, 0 });
                }

                writer.Write(Encoding.ASCII.GetBytes("data"));
                writer.Write(samples.Length * 2);
                foreach (var sample in samples)
                {
                    writer.Write(sample);
                }

                return stream.ToArray();
            }
        }
    }
}
