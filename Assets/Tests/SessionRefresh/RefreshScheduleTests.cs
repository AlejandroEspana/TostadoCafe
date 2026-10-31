using System;
using NUnit.Framework;

namespace ToastedVr.SessionRefresh.Tests
{
    public sealed class RefreshScheduleTests
    {
        private static readonly DateTimeOffset ReceivedAt = new DateTimeOffset(2026, 10, 31, 14, 0, 0, TimeSpan.Zero);
        private static readonly TimeSpan Margin = TimeSpan.FromSeconds(60);

        [Test]
        public void LongTokenRefreshesMarginBeforeExpiry()
        {
            var delay = RefreshSchedule.DelayUntilRefresh(ReceivedAt, ReceivedAt.AddSeconds(600), ReceivedAt, Margin);

            Assert.AreEqual(TimeSpan.FromSeconds(540), delay);
        }

        [Test]
        public void DelayShrinksAsTimePasses()
        {
            var delay = RefreshSchedule.DelayUntilRefresh(ReceivedAt, ReceivedAt.AddSeconds(600), ReceivedAt.AddSeconds(500), Margin);

            Assert.AreEqual(TimeSpan.FromSeconds(40), delay);
        }

        [Test]
        public void TokenInsideMarginRefreshesNow()
        {
            var delay = RefreshSchedule.DelayUntilRefresh(ReceivedAt, ReceivedAt.AddSeconds(600), ReceivedAt.AddSeconds(570), Margin);

            Assert.AreEqual(TimeSpan.Zero, delay);
        }

        [Test]
        public void ExpiredTokenRefreshesNow()
        {
            var delay = RefreshSchedule.DelayUntilRefresh(ReceivedAt, ReceivedAt.AddSeconds(600), ReceivedAt.AddSeconds(900), Margin);

            Assert.AreEqual(TimeSpan.Zero, delay);
        }

        [Test]
        public void ShortTokenRefreshesAtHalfLife()
        {
            var delay = RefreshSchedule.DelayUntilRefresh(ReceivedAt, ReceivedAt.AddSeconds(90), ReceivedAt, Margin);

            Assert.AreEqual(TimeSpan.FromSeconds(45), delay);
        }

        [Test]
        public void TokenShorterThanMarginStillWaitsHalfLife()
        {
            var delay = RefreshSchedule.DelayUntilRefresh(ReceivedAt, ReceivedAt.AddSeconds(30), ReceivedAt, Margin);

            Assert.AreEqual(TimeSpan.FromSeconds(15), delay);
        }

        [Test]
        public void TokenOfExactlyTwiceTheMarginUsesTheMargin()
        {
            var delay = RefreshSchedule.DelayUntilRefresh(ReceivedAt, ReceivedAt.AddSeconds(120), ReceivedAt, Margin);

            Assert.AreEqual(TimeSpan.FromSeconds(60), delay);
        }

        [Test]
        public void TokenAlreadyExpiredOnArrivalIsNotScheduled()
        {
            var delay = RefreshSchedule.DelayUntilRefresh(ReceivedAt, ReceivedAt.AddSeconds(-5), ReceivedAt, Margin);

            Assert.IsNull(delay);
        }

        [TestCase("2026-10-31T14:10:00Z")]
        [TestCase("2026-10-31T14:10:00.000Z")]
        [TestCase("2026-10-31T09:10:00-05:00")]
        public void ParsesBackendExpiryAsUtc(string expiresAt)
        {
            Assert.IsTrue(RefreshSchedule.TryParseExpiry(expiresAt, out var expiry));
            Assert.AreEqual(ReceivedAt.AddMinutes(10), expiry);
            Assert.AreEqual(TimeSpan.Zero, expiry.Offset);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("not a date")]
        public void RejectsUnreadableExpiry(string expiresAt)
        {
            Assert.IsFalse(RefreshSchedule.TryParseExpiry(expiresAt, out _));
        }
    }
}
