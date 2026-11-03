using NUnit.Framework;

namespace ToastedVr.RoastSimulation.Tests
{
    public sealed class RoastApiFailureClassifierTests
    {
        [TestCase(200, RoastApiFailure.None)]
        [TestCase(201, RoastApiFailure.None)]
        [TestCase(204, RoastApiFailure.None)]
        [TestCase(400, RoastApiFailure.BadRequest)]
        [TestCase(401, RoastApiFailure.Unauthorized)]
        [TestCase(404, RoastApiFailure.NotFound)]
        [TestCase(409, RoastApiFailure.InvalidPhase)]
        [TestCase(500, RoastApiFailure.Unavailable)]
        [TestCase(502, RoastApiFailure.Unavailable)]
        [TestCase(503, RoastApiFailure.Unavailable)]
        [TestCase(403, RoastApiFailure.Unexpected)]
        [TestCase(302, RoastApiFailure.Unexpected)]
        public void ClassifiesServerAnswers(long status, RoastApiFailure expected)
        {
            Assert.AreEqual(expected, RoastApiFailureClassifier.Classify(true, status));
        }

        [TestCase(0)]
        [TestCase(200)]
        public void NoAnswerIsUnavailable(long status)
        {
            Assert.AreEqual(RoastApiFailure.Unavailable, RoastApiFailureClassifier.Classify(false, status));
        }
    }
}
