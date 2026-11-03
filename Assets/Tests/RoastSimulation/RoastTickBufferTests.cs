using System.Linq;
using NUnit.Framework;

namespace ToastedVr.RoastSimulation.Tests
{
    public sealed class RoastTickBufferTests
    {
        private static RoastState[] Ticks(params int[] numbers) => numbers.Select(n => new RoastState { Tick = n }).ToArray();

        [Test]
        public void AppendsConsecutiveTicksAndAdvancesAfterTick()
        {
            var buffer = new RoastTickBuffer();
            buffer.Reset(10);

            var gap = buffer.Append(Ticks(11, 12, 13));

            Assert.IsFalse(gap);
            Assert.AreEqual(13, buffer.LastTick);
            CollectionAssert.AreEqual(new[] { 11, 12, 13 }, buffer.Ticks.Select(t => t.Tick));
        }

        [Test]
        public void IgnoresTicksAlreadyStored()
        {
            var buffer = new RoastTickBuffer();
            buffer.Reset(0);
            buffer.Append(Ticks(1, 2, 3));

            var gap = buffer.Append(Ticks(2, 3, 4));

            Assert.IsFalse(gap);
            CollectionAssert.AreEqual(new[] { 1, 2, 3, 4 }, buffer.Ticks.Select(t => t.Tick));
        }

        [Test]
        public void EmptyOrMissingResponseKeepsState()
        {
            var buffer = new RoastTickBuffer();
            buffer.Reset(5);

            Assert.IsFalse(buffer.Append(Ticks()));
            Assert.IsFalse(buffer.Append(null));
            Assert.AreEqual(5, buffer.LastTick);
            Assert.AreEqual(0, buffer.Ticks.Count);
        }

        [Test]
        public void DetectsGapWhenServerNoLongerHasOldTicks()
        {
            var buffer = new RoastTickBuffer();
            buffer.Reset(100);

            var gap = buffer.Append(Ticks(250, 251));

            Assert.IsTrue(gap);
            Assert.AreEqual(251, buffer.LastTick);
        }

        [Test]
        public void ResetStartsANewRoast()
        {
            var buffer = new RoastTickBuffer();
            buffer.Reset(0);
            buffer.Append(Ticks(1, 2));

            buffer.Reset(0);

            Assert.AreEqual(0, buffer.LastTick);
            Assert.AreEqual(0, buffer.Ticks.Count);
        }
    }
}
