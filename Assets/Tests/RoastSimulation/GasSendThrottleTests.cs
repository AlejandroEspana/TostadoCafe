using NUnit.Framework;

namespace ToastedVr.RoastSimulation.Tests
{
    public sealed class GasSendThrottleTests
    {
        private const float Interval = 0.5f;

        [Test]
        public void FirstChangeIsSentRightAway()
        {
            var throttle = new GasSendThrottle(Interval);
            throttle.Begin(40);

            Assert.IsTrue(throttle.ShouldSendWhileTurning(45, 10f));
        }

        [Test]
        public void UnchangedGasIsNeverSent()
        {
            var throttle = new GasSendThrottle(Interval);
            throttle.Begin(40);

            Assert.IsFalse(throttle.ShouldSendWhileTurning(40, 10f));
            Assert.IsFalse(throttle.ShouldSendOnRelease(40, 10f));
        }

        [Test]
        public void SendsAtMostOncePerIntervalWhileTurning()
        {
            var throttle = new GasSendThrottle(Interval);
            throttle.Begin(40);

            Assert.IsTrue(throttle.ShouldSendWhileTurning(45, 10f));
            Assert.IsFalse(throttle.ShouldSendWhileTurning(50, 10.2f));
            Assert.IsFalse(throttle.ShouldSendWhileTurning(55, 10.49f));
            Assert.IsTrue(throttle.ShouldSendWhileTurning(60, 10.5f));
        }

        [Test]
        public void ReleaseSendsTheLastValueEvenInsideTheInterval()
        {
            var throttle = new GasSendThrottle(Interval);
            throttle.Begin(40);
            throttle.ShouldSendWhileTurning(45, 10f);

            Assert.IsTrue(throttle.ShouldSendOnRelease(70, 10.1f));
        }

        [Test]
        public void ReleaseDoesNotRepeatWhatWasAlreadySent()
        {
            var throttle = new GasSendThrottle(Interval);
            throttle.Begin(40);
            throttle.ShouldSendWhileTurning(45, 10f);

            Assert.IsFalse(throttle.ShouldSendOnRelease(45, 10.1f));
        }

        [Test]
        public void TurningBackToTheStartingGasAfterSendingIsSent()
        {
            var throttle = new GasSendThrottle(Interval);
            throttle.Begin(40);
            throttle.ShouldSendWhileTurning(60, 10f);

            Assert.IsTrue(throttle.ShouldSendOnRelease(40, 10.1f));
        }

        [Test]
        public void EachGrabStartsFresh()
        {
            var throttle = new GasSendThrottle(Interval);
            throttle.Begin(40);
            throttle.ShouldSendWhileTurning(60, 10f);
            throttle.ShouldSendOnRelease(60, 10.1f);

            throttle.Begin(60);

            Assert.IsTrue(throttle.ShouldSendWhileTurning(65, 10.2f));
        }
    }
}
