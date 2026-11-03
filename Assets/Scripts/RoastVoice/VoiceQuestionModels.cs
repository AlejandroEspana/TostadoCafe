using System.Collections.Generic;

namespace ToastedVr.RoastVoice
{
    // Forma de la pregunta y la respuesta de /api/v1/roasting/assistant/questions (IA/docs/unity-voice-contract.md).
    public sealed class VoiceQuestionContext
    {
        public string RoastAttemptId { get; set; }
        public VoiceRoastState RoastState { get; set; }
    }

    public sealed class VoiceRoastState
    {
        public int ElapsedSeconds { get; set; }
        public double BeanTemperature { get; set; }
        public bool FirstCrackReached { get; set; }
        public double? TargetTemperature { get; set; }
        public double? ChargeTemperature { get; set; }
        public int? FirstCrackElapsedSeconds { get; set; }
        public int? HeatLevelPercent { get; set; }
        public int? TemperatureRisePerMinute { get; set; }
    }

    public sealed class VoiceAnswer
    {
        public const string Answered = "ANSWERED";
        public const string Abstained = "ABSTAINED";
        public const string Unavailable = "UNAVAILABLE";

        public string Status { get; set; }
        public string Question { get; set; }
        public string Answer { get; set; }
        public List<VoiceCitation> Citations { get; set; } = new List<VoiceCitation>();
        public VoiceAudio Audio { get; set; }
    }

    public sealed class VoiceCitation
    {
        public string FileName { get; set; }
        public int? PageNumber { get; set; }
        public string Title { get; set; }
    }

    public sealed class VoiceAudio
    {
        public string Format { get; set; }
        public int SampleRateHz { get; set; }
        public string Base64 { get; set; }
    }
}
