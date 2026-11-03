using NUnit.Framework;
using UnityEditor;

namespace ToastedVr.RoastVoice.Tests
{
    public sealed class RoastVoiceAssetsTests
    {
        private const string ConfigurationPath = "Assets/Settings/RoastVoice/RoastVoiceConfiguration.asset";
        private const string MessagesPath = "Assets/Settings/RoastVoice/RoastVoiceMessages.asset";

        [Test]
        public void ConfigurationMatchesTheVoiceContract()
        {
            var configuration = AssetDatabase.LoadAssetAtPath<RoastVoiceConfiguration>(ConfigurationPath);

            Assert.IsNotNull(configuration);
            Assert.AreEqual("/api/v1/roasting/assistant/questions", configuration.QuestionsPath);
            Assert.AreEqual(16000, configuration.SampleRateHz);
            Assert.AreEqual(0.3f, configuration.MinRecordingSeconds, 1e-6);
            Assert.AreEqual(15f, configuration.MaxRecordingSeconds, 1e-6);
            Assert.AreEqual(600000, configuration.MaxAudioBytes);
            Assert.GreaterOrEqual(configuration.RequestTimeoutSeconds, 30, "el backend espera hasta 30 s a la IA");
            Assert.AreEqual("/api/v1/roasting/sessions/42/feedback/speech", string.Format(configuration.FeedbackSpeechPathFormat, 42));
            Assert.Greater(configuration.FeedbackSpeechTimeoutSeconds, 30, "el backend espera hasta 30 s la voz");
        }

        [Test]
        public void TheLongestRecordingFitsTheSizeLimit()
        {
            var configuration = AssetDatabase.LoadAssetAtPath<RoastVoiceConfiguration>(ConfigurationPath);
            var longest = (int)(configuration.MaxRecordingSeconds * configuration.SampleRateHz);

            Assert.AreEqual(RecordingVerdict.Accepted,
                RecordingRules.Check(longest, configuration.SampleRateHz, configuration.MinRecordingSeconds, configuration.MaxAudioBytes));
        }

        [Test]
        public void EveryMessageIsFilled()
        {
            var messages = AssetDatabase.LoadAssetAtPath<RoastVoiceMessages>(MessagesPath);

            Assert.IsNotNull(messages);
            foreach (var text in new[]
            {
                messages.TalkLabel, messages.ListeningMessage, messages.ThinkingMessage, messages.TooShortMessage,
                messages.TooLargeMessage, messages.NoMicrophoneMessage, messages.MicrophonePermissionMessage,
                messages.OfflineMessage, messages.UnexpectedErrorMessage
            })
            {
                Assert.IsFalse(string.IsNullOrWhiteSpace(text));
            }

            Assert.DoesNotThrow(() => string.Format(messages.QuestionFormat, "¿Bajo el gas?"));
        }

        [Test]
        public void NoProviderAddressOrKeyLivesInTheAssets()
        {
            foreach (var path in new[] { ConfigurationPath, MessagesPath })
            {
                var text = System.IO.File.ReadAllText(path).ToLowerInvariant();
                foreach (var forbidden in new[] { "groq", "azure", "piper", "api_key", "apikey", "8082", "https://" })
                {
                    StringAssert.DoesNotContain(forbidden, text, path);
                }
            }
        }
    }
}
