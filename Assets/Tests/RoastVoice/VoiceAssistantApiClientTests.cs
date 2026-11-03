using System.Collections;
using System.Linq;
using NUnit.Framework;
using ToastedVr.RoastSimulation;
using ToastedVr.SessionRefresh;
using ToastedVr.SessionRefresh.Tests;
using UnityEngine.TestTools;

namespace ToastedVr.RoastVoice.Tests
{
    public sealed class VoiceAssistantApiClientTests
    {
        private const string QuestionsPath = "/api/v1/roasting/assistant/questions";
        private const string RefreshPath = "/api/v1/auth/refresh";
        private const string OldAccess = "old-access-secret";
        private const string NewAccess = "new-access-secret";
        private const string RefreshToken = "refresh-secret";
        private const string AttemptId = "3f6c1b52-9a1e-4c55-9d7e-2b8f0c4a7e11";

        private const string AnsweredBody =
            "{\"status\": \"ANSWERED\", \"question\": \"¿Bajo el gas?\", \"answer\": \"Sí, bájalo un poco.\", \"citations\": []," +
            "\"audio\": {\"format\": \"wav\", \"sampleRateHz\": 22050, \"base64\": \"UklGR\"}}";

        private const string RenewedBody =
            "{\"accessToken\":\"" + NewAccess + "\",\"tokenType\":\"Bearer\"," +
            "\"expiresAt\":\"2026-10-31T14:10:00Z\",\"refreshToken\":\"refresh-2\"}";

        private CoroutineDriver driver;
        private VoiceAssistantApiClient client;
        private int expiredEvents;

        private static readonly byte[] Wav = WavCodec.EncodeMono16(new[] { 0f, 0.5f, -0.5f, 1f }, 16000);

        private static VoiceQuestionContext Context() => VoiceContextBuilder.Build(AttemptId, new RoastState
        {
            RoastSeconds = 510, Temperature = 198, GasPercent = 80, RisePerMinute = 14,
            FirstCrack = new CrackStatus()
        });

        private void CreateClient(FakeBackend backend)
        {
            var store = new FakeSessionTokenStore(OldAccess, RefreshToken);
            driver = new CoroutineDriver();
            var refresher = new SessionRefresher(store, new SessionRefreshApiClient(backend.BaseUrl, RefreshPath, 5), driver.Start);
            var sender = new AuthorizedRequestSender(store, refresher);
            expiredEvents = 0;
            sender.SessionExpired += () => expiredEvents++;
            client = new VoiceAssistantApiClient(backend.BaseUrl, QuestionsPath, 5, sender);
        }

        private IEnumerator Ask(System.Action<RoastApiResult<VoiceAnswer>> onCompleted)
        {
            driver.Start(client.Ask(Wav, Context(), onCompleted));
            yield return driver.RunUntilDone();
        }

        [UnityTest]
        public IEnumerator SendsAudioAndContextAsTheBackendExpects()
        {
            using (var backend = new FakeBackend(_ => (200, AnsweredBody)))
            {
                CreateClient(backend);
                RoastApiResult<VoiceAnswer> result = null;

                yield return Ask(r => result = r);

                Assert.IsTrue(result.IsSuccess);
                Assert.AreEqual("Sí, bájalo un poco.", result.Value.Answer);

                var request = backend.Requests.Single();
                Assert.AreEqual("POST", request.Method);
                Assert.AreEqual(QuestionsPath, request.Path);
                Assert.AreEqual("Bearer " + OldAccess, request.Authorization);
                StringAssert.StartsWith("multipart/form-data", request.ContentType);

                var parts = MultipartReader.Read(request.ContentType, request.BodyBytes);
                Assert.AreEqual("question.wav", parts["audio"].FileName);
                Assert.AreEqual("audio/wav", parts["audio"].ContentType);
                CollectionAssert.AreEqual(Wav, parts["audio"].Body);
                Assert.IsNull(parts["context"].FileName, "context debe ser un campo de texto, no un archivo");
                Assert.AreEqual(RoastJson.Serialize(Context()), MultipartReader.Text(parts["context"]));
            }
        }

        [UnityTest]
        public IEnumerator ReadsAbstainedAndUnavailableAnswers([Values("ABSTAINED", "UNAVAILABLE")] string status)
        {
            using (var backend = new FakeBackend(_ => (200,
                "{\"status\": \"" + status + "\", \"question\": null, \"answer\": \"No puedo responder.\", \"citations\": [], \"audio\": null}")))
            {
                CreateClient(backend);
                RoastApiResult<VoiceAnswer> result = null;

                yield return Ask(r => result = r);

                Assert.IsTrue(result.IsSuccess);
                Assert.AreEqual(status, result.Value.Status);
                Assert.IsNull(result.Value.Audio);
            }
        }

        [UnityTest]
        public IEnumerator BackendErrorsCarryTheirSpanishMessage([Values(400, 409, 413, 422, 429, 503)] int status)
        {
            using (var backend = new FakeBackend(_ => (status, "{\"code\": \"SOME_CODE\", \"message\": \"Repite la pregunta, por favor.\"}")))
            {
                CreateClient(backend);
                RoastApiResult<VoiceAnswer> result = null;

                yield return Ask(r => result = r);

                Assert.IsFalse(result.IsSuccess);
                Assert.AreEqual(status, result.StatusCode);
                Assert.AreEqual("Repite la pregunta, por favor.", result.Error.Message);
                Assert.AreEqual(0, expiredEvents);
            }
        }

        [UnityTest]
        public IEnumerator UnauthorizedRefreshesAndResendsTheSameAudio()
        {
            using (var backend = new FakeBackend(request =>
                request.Path == RefreshPath ? (200, RenewedBody)
                : request.Authorization == "Bearer " + NewAccess ? (200, AnsweredBody)
                : (401, "")))
            using (var logs = new TokenLogWatcher())
            {
                CreateClient(backend);
                RoastApiResult<VoiceAnswer> result = null;

                yield return Ask(r => result = r);

                Assert.IsTrue(result.IsSuccess);
                var questions = backend.Requests.Where(r => r.Path == QuestionsPath).ToArray();
                Assert.AreEqual(2, questions.Length);
                var retried = MultipartReader.Read(questions[1].ContentType, questions[1].BodyBytes);
                CollectionAssert.AreEqual(Wav, retried["audio"].Body);
                Assert.AreEqual(0, expiredEvents);
                logs.AssertNeverLogged(OldAccess, NewAccess, RefreshToken);
            }
        }

        [UnityTest]
        public IEnumerator RejectedRefreshExpiresTheSession()
        {
            using (var backend = new FakeBackend(_ => (401, "")))
            {
                CreateClient(backend);
                RoastApiResult<VoiceAnswer> result = null;

                yield return Ask(r => result = r);

                Assert.AreEqual(RoastApiFailure.Unauthorized, result.Failure);
                Assert.AreEqual(1, expiredEvents);
            }
        }

        [UnityTest]
        public IEnumerator EmptyAnswerIsUnexpected()
        {
            using (var backend = new FakeBackend(_ => (200, "{\"status\": \"ANSWERED\"}")))
            {
                CreateClient(backend);
                RoastApiResult<VoiceAnswer> result = null;

                yield return Ask(r => result = r);

                Assert.AreEqual(RoastApiFailure.Unexpected, result.Failure);
            }
        }

        [UnityTest]
        public IEnumerator UnreachableBackendIsUnavailable()
        {
            using (var backend = new FakeBackend(_ => (200, AnsweredBody)))
            {
                CreateClient(backend);
            }

            RoastApiResult<VoiceAnswer> result = null;
            yield return Ask(r => result = r);

            Assert.AreEqual(RoastApiFailure.Unavailable, result.Failure);
        }
    }
}
