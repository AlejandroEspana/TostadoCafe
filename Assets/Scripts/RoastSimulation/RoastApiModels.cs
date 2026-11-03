using System.Collections.Generic;

namespace ToastedVr.RoastSimulation
{
    // Forma de las respuestas de /api/v1/roasting/simulations (IA/docs/unity-roast-simulation-contract.md).
    public sealed class RoastState
    {
        public int Tick { get; set; }
        public string Phase { get; set; }
        public int RoastSeconds { get; set; }
        public double Temperature { get; set; }
        public double AirTemperature { get; set; }
        public double? MoisturePct { get; set; }
        public int GasPercent { get; set; }
        public double? RisePerMinute { get; set; }
        public double ChargeTemperature { get; set; }
        public double TargetTemperature { get; set; }
        public string GrainState { get; set; }
        public CrackStatus FirstCrack { get; set; } = new CrackStatus();
        public CrackStatus SecondCrack { get; set; } = new CrackStatus();
        public RoastAlarms Alarms { get; set; } = new RoastAlarms();
    }

    public sealed class CrackStatus
    {
        public bool Reached { get; set; }
        public int? RoastSeconds { get; set; }
    }

    public sealed class RoastAlarms
    {
        public bool Burned { get; set; }
        public bool Baked { get; set; }
        public bool BurnWarning { get; set; }
        public bool AlarmLimitExceeded { get; set; }
        public bool ChargeOvershoot { get; set; }
    }

    public sealed class SimulationCreated
    {
        public string SimulationId { get; set; }
        public RoastState State { get; set; }
    }

    public sealed class SimulationStateResponse
    {
        public RoastState State { get; set; }
    }

    public sealed class SimulationPoll
    {
        public RoastState State { get; set; }
        public List<RoastState> Ticks { get; set; } = new List<RoastState>();
    }

    public sealed class GasRequest
    {
        public int GasPercent { get; set; }
    }

    public sealed class ProactiveAlert
    {
        public string Id { get; set; }
        public int T { get; set; }
    }

    public sealed class DischargeRequest
    {
        public List<ProactiveAlert> Alerts { get; set; } = new List<ProactiveAlert>();
    }

    public sealed class DischargeResult
    {
        public RoastState State { get; set; }
        public RoastSessionSummary Session { get; set; }
    }

    public sealed class RoastSessionSummary
    {
        public long Id { get; set; }
        public string Result { get; set; }
        public int? QualityScore { get; set; }
        public int? FinalScore { get; set; }
        public string Grade { get; set; }
        public RoastEvaluationSummary Evaluation { get; set; }
    }

    // Calificación combinada (GET /api/v1/roasting/sessions/{id}). FinalScore es null mientras está PENDING
    // o si quedó FAILED; RubricScore y los pesos solo llegan cuando la IA calificó (COMPLETED).
    public sealed class RoastEvaluationSummary
    {
        public string Status { get; set; }
        public string SkipReason { get; set; }
        public int? FinalScore { get; set; }
        public int? RulesScore { get; set; }
        public double? RubricScore { get; set; }
        public double? RulesWeight { get; set; }
        public double? AiWeight { get; set; }
        public bool DefectCapApplied { get; set; }
        public List<RoastCriterionScore> Criteria { get; set; } = new List<RoastCriterionScore>();
        public List<string> NotApplicable { get; set; } = new List<string>();
    }

    public sealed class RoastCriterionScore
    {
        public string Id { get; set; }
        public int Level { get; set; }
        public int MaxLevel { get; set; }
        public string Justification { get; set; }
    }

    public sealed class RoastFeedback
    {
        public string Feedback { get; set; }
    }

    public sealed class ApiError
    {
        public string Code { get; set; }
        public string Message { get; set; }
    }
}
