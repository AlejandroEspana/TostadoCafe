using NUnit.Framework;

namespace ToastedVr.RoastVoice.Tests
{
    public sealed class RecordingRulesTests
    {
        private const int Rate = 16000;
        private const float MinSeconds = 0.3f;
        private const int MaxBytes = 600000;

        [TestCase(0)]
        [TestCase(4799)]
        public void UnderTheMinimumIsTooShort(int samples)
        {
            Assert.AreEqual(RecordingVerdict.TooShort, RecordingRules.Check(samples, Rate, MinSeconds, MaxBytes));
        }

        [TestCase(4800)]
        [TestCase(16000 * 15)]
        public void ContractRangeIsAccepted(int samples)
        {
            Assert.AreEqual(RecordingVerdict.Accepted, RecordingRules.Check(samples, Rate, MinSeconds, MaxBytes));
        }

        [Test]
        public void ExactlyTheMaximumSizeIsAccepted()
        {
            var samples = (MaxBytes - 44) / 2;

            Assert.AreEqual(RecordingVerdict.Accepted, RecordingRules.Check(samples, Rate, MinSeconds, MaxBytes));
            Assert.AreEqual(RecordingVerdict.TooLarge, RecordingRules.Check(samples + 1, Rate, MinSeconds, MaxBytes));
        }

        [Test]
        public void InvalidRateIsNeverSent()
        {
            Assert.AreEqual(RecordingVerdict.TooShort, RecordingRules.Check(16000, 0, MinSeconds, MaxBytes));
        }
    }
}
