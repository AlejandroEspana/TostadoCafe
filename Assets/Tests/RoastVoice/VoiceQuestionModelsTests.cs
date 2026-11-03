using NUnit.Framework;
using ToastedVr.RoastSimulation;

namespace ToastedVr.RoastVoice.Tests
{
    public sealed class VoiceQuestionModelsTests
    {
        [Test]
        public void ContextUsesTheContractFieldNames()
        {
            var context = new VoiceQuestionContext
            {
                RoastAttemptId = "3f6c1b52-9a1e-4c55-9d7e-2b8f0c4a7e11",
                RoastState = new VoiceRoastState
                {
                    ElapsedSeconds = 510,
                    BeanTemperature = 198.0,
                    FirstCrackReached = false,
                    TargetTemperature = 205.0,
                    ChargeTemperature = 190.0,
                    HeatLevelPercent = 80,
                    TemperatureRisePerMinute = 14
                }
            };

            Assert.AreEqual(
                "{\"roastAttemptId\":\"3f6c1b52-9a1e-4c55-9d7e-2b8f0c4a7e11\",\"roastState\":{\"elapsedSeconds\":510," +
                "\"beanTemperature\":198.0,\"firstCrackReached\":false,\"targetTemperature\":205.0,\"chargeTemperature\":190.0," +
                "\"heatLevelPercent\":80,\"temperatureRisePerMinute\":14}}",
                RoastJson.Serialize(context));
        }

        [Test]
        public void ReadsTheContractAnswer()
        {
            var answer = RoastJson.TryDeserialize<VoiceAnswer>(
                "{\"status\": \"ANSWERED\", \"question\": \"¿Debería bajar el gas ahora?\", \"answer\": \"Texto de 2 o 3 frases.\"," +
                "\"citations\": [{\"fileName\": \"cafe-manual-de-tostado_1.pdf\", \"pageNumber\": 8, \"title\": \"Manual básico\"}]," +
                "\"audio\": {\"format\": \"wav\", \"sampleRateHz\": 22050, \"base64\": \"UklGR\"}," +
                "\"providers\": {\"transcription\": \"groq\", \"answer\": \"groq\", \"speech\": \"azure\"}," +
                "\"latency\": {\"totalMillis\": 5938}}");

            Assert.AreEqual(VoiceAnswer.Answered, answer.Status);
            Assert.AreEqual("¿Debería bajar el gas ahora?", answer.Question);
            Assert.AreEqual("Texto de 2 o 3 frases.", answer.Answer);
            Assert.AreEqual(8, answer.Citations[0].PageNumber);
            Assert.AreEqual(22050, answer.Audio.SampleRateHz);
            Assert.AreEqual("UklGR", answer.Audio.Base64);
        }

        [Test]
        public void ReadsUnavailableAnswerWithoutAudioOrQuestion()
        {
            var answer = RoastJson.TryDeserialize<VoiceAnswer>(
                "{\"status\": \"UNAVAILABLE\", \"question\": null, \"answer\": \"No puedo responder en este momento.\", \"citations\": [], \"audio\": null}");

            Assert.AreEqual(VoiceAnswer.Unavailable, answer.Status);
            Assert.IsNull(answer.Question);
            Assert.IsNull(answer.Audio);
        }
    }
}
