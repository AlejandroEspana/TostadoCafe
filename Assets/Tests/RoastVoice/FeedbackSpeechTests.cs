using System.Collections;
using System.Linq;
using NUnit.Framework;
using ToastedVr.RoastSimulation;
using ToastedVr.SessionRefresh;
using ToastedVr.SessionRefresh.Tests;
using UnityEngine.TestTools;

namespace ToastedVr.RoastVoice.Tests
{
    public sealed class FeedbackSpeechTests
    {
        private const string PathFormat = "/api/v1/roasting/sessions/{0}/feedback/speech";
        private const string Access = "access-secret";
        private const string SpeechBody = "{\"audio\": {\"format\": \"wav\", \"sampleRateHz\": 22050, \"base64\": \"UklGRg==\"}, \"provider\": \"azure\"}";

        private CoroutineDriver driver;
        private FeedbackSpeechApiClient client;

        private void CreateClient(FakeBackend backend)
        {
            var store = new FakeSessionTokenStore(Access, "refresh-secret");
            driver = new CoroutineDriver();
            var refresher = new SessionRefresher(store, new SessionRefreshApiClient(backend.BaseUrl, "/api/v1/auth/refresh", 5), driver.Start);
            client = new FeedbackSpeechApiClient(backend.BaseUrl + "/", PathFormat, 5, new AuthorizedRequestSender(store, refresher));
        }

        private IEnumerator Speak(long sessionId, System.Action<RoastApiResult<FeedbackSpeech>> onCompleted)
        {
            driver.Start(client.Speak(sessionId, onCompleted));
            yield return driver.RunUntilDone();
        }

        [UnityTest]
        public IEnumerator PostsWithoutTextAndReadsTheAudio()
        {
            using (var backend = new FakeBackend(_ => (200, SpeechBody)))
            {
                CreateClient(backend);
                RoastApiResult<FeedbackSpeech> result = null;

                yield return Speak(42, r => result = r);

                Assert.IsTrue(result.IsSuccess);
                Assert.AreEqual("UklGRg==", result.Value.Audio.Base64);
                Assert.AreEqual(22050, result.Value.Audio.SampleRateHz);
                var request = backend.Requests.Single();
                Assert.AreEqual("POST", request.Method);
                Assert.AreEqual("/api/v1/roasting/sessions/42/feedback/speech", request.Path);
                Assert.AreEqual("Bearer " + Access, request.Authorization);
                Assert.IsTrue(string.IsNullOrEmpty(request.Body), "Unity no manda texto: lo arma el backend");
            }
        }

        [UnityTest]
        public IEnumerator UnavailableSpeechCarriesTheBackendMessage()
        {
            using (var backend = new FakeBackend(_ => (503, "{\"code\": \"SPEECH_UNAVAILABLE\", \"message\": \"No pude leer.\"}")))
            {
                CreateClient(backend);
                RoastApiResult<FeedbackSpeech> result = null;

                yield return Speak(42, r => result = r);

                Assert.AreEqual(RoastApiFailure.Unavailable, result.Failure);
                Assert.AreEqual("SPEECH_UNAVAILABLE", result.Error.Code);
            }
        }

        [UnityTest]
        public IEnumerator AnswerWithoutAudioIsUnexpected()
        {
            using (var backend = new FakeBackend(_ => (200, "{\"provider\": \"azure\"}")))
            {
                CreateClient(backend);
                RoastApiResult<FeedbackSpeech> result = null;

                yield return Speak(42, r => result = r);

                Assert.AreEqual(RoastApiFailure.Unexpected, result.Failure);
            }
        }

        [Test]
        public void ClientWithoutBackendIsNotConfigured()
        {
            Assert.IsFalse(new FeedbackSpeechApiClient(null, PathFormat, 5, new AuthorizedRequestSender(null, null)).IsConfigured);
            Assert.IsFalse(new FeedbackSpeechApiClient("file:///x", PathFormat, 5, new AuthorizedRequestSender(null, null)).IsConfigured);
            Assert.IsFalse(new FeedbackSpeechApiClient("http://host", "", 5, new AuthorizedRequestSender(null, null)).IsConfigured);
        }

        private static RoastSessionSummary Session(string status)
        {
            return new RoastSessionSummary { Id = 42, Evaluation = status == null ? null : new RoastEvaluationSummary { Status = status } };
        }

        [TestCase("COMPLETED", false, true)]
        [TestCase("FAILED", false, true)]
        [TestCase("NOT_APPLICABLE", false, true)]
        [TestCase(null, false, true)]
        [TestCase("PENDING", false, false)]
        [TestCase("PENDING", true, true)]
        public void SpeaksOnlyWithTheFinalScore(string status, bool timedOut, bool expected)
        {
            Assert.AreEqual(expected, FeedbackSpeechPlayer.IsReadyToSpeak("Buen control del gas.", Session(status), timedOut));
        }

        [TestCase(null)]
        [TestCase(" ")]
        public void DoesNotSpeakWithoutFeedback(string feedback)
        {
            Assert.IsFalse(FeedbackSpeechPlayer.IsReadyToSpeak(feedback, Session("COMPLETED"), false));
        }

        [Test]
        public void DoesNotSpeakWithoutSession()
        {
            Assert.IsFalse(FeedbackSpeechPlayer.IsReadyToSpeak("Texto", null, true));
        }
    }
}
