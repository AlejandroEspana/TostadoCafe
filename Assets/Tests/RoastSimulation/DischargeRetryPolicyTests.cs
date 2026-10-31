using NUnit.Framework;

namespace ToastedVr.RoastSimulation.Tests
{
    public sealed class DischargeRetryPolicyTests
    {
        private const int MaxRetries = 3;

        [TestCase(0)]
        [TestCase(2)]
        public void RetriesTurningPointConflictWhileRoasting(int retriesDone)
        {
            Assert.IsTrue(DischargeRetryPolicy.ShouldRetry(RoastApiFailure.InvalidPhase, "ROASTING", retriesDone, MaxRetries));
        }

        [Test]
        public void StopsAfterMaxRetries()
        {
            Assert.IsFalse(DischargeRetryPolicy.ShouldRetry(RoastApiFailure.InvalidPhase, "ROASTING", MaxRetries, MaxRetries));
        }

        [TestCase("PREHEAT")]
        [TestCase("READY")]
        [TestCase("CHARGE_DIP")]
        [TestCase("FINISHED")]
        public void ConflictOutsideRoastingIsNotRetried(string phase)
        {
            Assert.IsFalse(DischargeRetryPolicy.ShouldRetry(RoastApiFailure.InvalidPhase, phase, 0, MaxRetries));
        }

        [TestCase(RoastApiFailure.None)]
        [TestCase(RoastApiFailure.BadRequest)]
        [TestCase(RoastApiFailure.Unauthorized)]
        [TestCase(RoastApiFailure.NotFound)]
        [TestCase(RoastApiFailure.Unavailable)]
        [TestCase(RoastApiFailure.Unexpected)]
        public void OtherFailuresAreNotRetried(RoastApiFailure failure)
        {
            Assert.IsFalse(DischargeRetryPolicy.ShouldRetry(failure, "ROASTING", 0, MaxRetries));
        }
    }
}
