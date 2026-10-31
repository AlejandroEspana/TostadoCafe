using System;
using NUnit.Framework;

namespace ToastedVr.RoastVoice.Tests
{
    public sealed class PcmResamplerTests
    {
        [Test]
        public void StereoIsAveragedToMono()
        {
            var mono = PcmResampler.ToMono(new[] { 1f, 0f, -0.5f, 0.5f, 0.2f, 0.4f }, 2);

            Assert.AreEqual(3, mono.Length);
            Assert.AreEqual(0.5f, mono[0], 1e-6);
            Assert.AreEqual(0f, mono[1], 1e-6);
            Assert.AreEqual(0.3f, mono[2], 1e-6);
        }

        [Test]
        public void MonoIsUntouched()
        {
            var samples = new[] { 0.1f, 0.2f };

            Assert.AreSame(samples, PcmResampler.ToMono(samples, 1));
        }

        [Test]
        public void SameRateIsUntouched()
        {
            var samples = new[] { 0.1f, 0.2f };

            Assert.AreSame(samples, PcmResampler.Resample(samples, 16000, 16000));
        }

        [Test]
        public void DownsamplingKeepsTheDurationAndTheShape()
        {
            const int fromRate = 48000;
            const int toRate = 16000;
            var oneSecond = new float[fromRate];
            for (var i = 0; i < oneSecond.Length; i++)
            {
                oneSecond[i] = (float)Math.Sin(2 * Math.PI * 440 * i / fromRate);
            }

            var resampled = PcmResampler.Resample(oneSecond, fromRate, toRate);

            Assert.AreEqual(toRate, resampled.Length);
            for (var i = 0; i < resampled.Length; i += 97)
            {
                Assert.AreEqual(Math.Sin(2 * Math.PI * 440 * i / toRate), resampled[i], 1e-3, $"muestra {i}");
            }
        }

        [Test]
        public void UpsamplingInterpolatesBetweenSamples()
        {
            var resampled = PcmResampler.Resample(new[] { 0f, 1f }, 8000, 16000);

            CollectionAssert.AreEqual(new[] { 0f, 0.5f, 1f, 1f }, resampled);
        }

        [Test]
        public void EmptyRecordingStaysEmpty()
        {
            Assert.AreEqual(0, PcmResampler.Resample(new float[0], 44100, 16000).Length);
        }

        [Test]
        public void RejectsInvalidRates()
        {
            Assert.Throws<ArgumentException>(() => PcmResampler.Resample(new[] { 0f }, 0, 16000));
        }
    }
}
