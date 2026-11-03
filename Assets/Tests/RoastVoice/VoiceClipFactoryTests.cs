using System;
using NUnit.Framework;
using UnityEngine;

namespace ToastedVr.RoastVoice.Tests
{
    public sealed class VoiceClipFactoryTests
    {
        [Test]
        public void BuildsAClipFromTheAssistantAudio()
        {
            var samples = new[] { 0f, 0.5f, -0.5f, 0.25f };
            var audio = new VoiceAudio
            {
                Format = "wav",
                SampleRateHz = 22050,
                Base64 = Convert.ToBase64String(WavCodec.EncodeMono16(samples, 22050))
            };

            var clip = VoiceClipFactory.TryCreate(audio, "answer");
            try
            {
                Assert.IsNotNull(clip);
                Assert.AreEqual(22050, clip.frequency);
                Assert.AreEqual(1, clip.channels);
                Assert.AreEqual(4, clip.samples);
                var read = new float[4];
                clip.GetData(read, 0);
                Assert.AreEqual(0.5f, read[1], 1e-3);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(clip);
            }
        }

        [Test]
        public void MissingAudioShowsOnlyText()
        {
            Assert.IsNull(VoiceClipFactory.TryCreate(null, "answer"));
            Assert.IsNull(VoiceClipFactory.TryCreate(new VoiceAudio { Base64 = "" }, "answer"));
        }

        [Test]
        public void UnreadableAudioShowsOnlyText()
        {
            Assert.IsNull(VoiceClipFactory.TryCreate(new VoiceAudio { Base64 = "not base64!" }, "answer"));
            Assert.IsNull(VoiceClipFactory.TryCreate(new VoiceAudio { Base64 = Convert.ToBase64String(new byte[] { 1, 2, 3 }) }, "answer"));
        }

        [Test]
        public void EmptyWavShowsOnlyText()
        {
            var audio = new VoiceAudio { Base64 = Convert.ToBase64String(WavCodec.EncodeMono16(new float[0], 22050)) };

            Assert.IsNull(VoiceClipFactory.TryCreate(audio, "answer"));
        }
    }
}
