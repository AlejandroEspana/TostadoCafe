using System.Collections.Generic;
using NUnit.Framework;

namespace ToastedVr.RoastAlerts.Tests
{
    public sealed class AlertChannelTests
    {
        private static readonly AlertRuleModel Critical = new AlertRuleModel { Id = "C", Priority = "CRITICAL" };
        private static readonly AlertRuleModel High = new AlertRuleModel { Id = "H", Priority = "HIGH" };
        private static readonly AlertRuleModel Normal = new AlertRuleModel { Id = "N", Priority = "NORMAL" };
        private static readonly AlertRuleModel Low = new AlertRuleModel { Id = "L", Priority = "LOW" };

        private static AlertChannel Channel() => new AlertChannel(new ChannelPolicy
        {
            QueueMaxWaitSeconds = 10,
            MinGapSeconds = 5,
            UrgentPriorities = new List<string> { "CRITICAL" },
            UnqueuedPriorities = new List<string> { "LOW" }
        });

        private static AlertRuleModel Offer(AlertChannel channel, int second, bool busy = false, params AlertRuleModel[] triggered) =>
            channel.Offer(triggered, second, rule => true, busy);

        [Test]
        public void NeverDeliveredIsInfinitelyQuiet()
        {
            Assert.AreEqual(double.PositiveInfinity, Channel().SecondsSinceLastDelivery(0));
        }

        [Test]
        public void HighestPriorityWinsAndTheRestWait()
        {
            var channel = Channel();

            Assert.AreSame(High, Offer(channel, 0, false, Normal, High));
            Assert.IsNull(Offer(channel, 4), "mínimo de 5 s entre alertas");
            Assert.AreSame(Normal, Offer(channel, 5));
        }

        [Test]
        public void UrgentIgnoresGapAndBusyAssistant()
        {
            var channel = Channel();
            Offer(channel, 0, false, Normal);

            Assert.AreSame(Critical, Offer(channel, 1, true, Critical));
        }

        [Test]
        public void BusyAssistantHoldsNormalAlerts()
        {
            var channel = Channel();

            Assert.IsNull(Offer(channel, 0, true, Normal));
            Assert.AreSame(Normal, Offer(channel, 1));
        }

        [Test]
        public void UnqueuedPrioritiesAreDroppedWhenTheyCannotSpeak()
        {
            var channel = Channel();

            Assert.IsNull(Offer(channel, 0, true, Low));
            Assert.IsNull(Offer(channel, 1));
        }

        [Test]
        public void QueueDropsOldOrInactiveAlerts()
        {
            var channel = Channel();
            Offer(channel, 0, true, Normal, High);

            Assert.IsNull(channel.Offer(new AlertRuleModel[0], 1, rule => rule != High, true));
            Assert.IsNull(Offer(channel, 11), "pasaron más de 10 s en cola");
        }

        [Test]
        public void SamePriorityKeepsTheOlderFirstThenPackageOrder()
        {
            var first = new AlertRuleModel { Id = "A", Priority = "NORMAL" };
            var second = new AlertRuleModel { Id = "B", Priority = "NORMAL" };
            var channel = Channel();
            Offer(channel, 0, true, second);

            Assert.AreSame(second, Offer(channel, 1, false, first));
            Assert.AreSame(first, Offer(channel, 6));

            var fresh = Channel();
            Assert.AreSame(first, Offer(fresh, 0, false, first, second));
        }
    }
}
