using UnityEngine;

namespace ToastedVr.RoastSimulation
{
    [CreateAssetMenu(fileName = "RoastSimulationConfiguration", menuName = "Toasted VR/Roast Simulation Configuration")]
    public sealed class RoastSimulationConfiguration : ScriptableObject
    {
        [Header("Backend")]
        [SerializeField] private string simulationsPath;
        [SerializeField, Min(1)] private int requestTimeoutSeconds;
        [Tooltip("Seconds between state queries. The server deletes a simulation nobody queries for 120 s.")]
        [SerializeField, Min(0.1f)] private float pollIntervalSeconds;

        [Header("Session result")]
        [SerializeField] private string sessionsPath;
        [Tooltip("The AI can take about two minutes to write the feedback.")]
        [SerializeField, Min(1)] private int feedbackTimeoutSeconds;
        [Tooltip("Same rhythm as the web while the AI grades the decisions.")]
        [SerializeField, Min(0.5f)] private float evaluationPollIntervalSeconds;
        [SerializeField, Min(1)] private int evaluationPollMaxAttempts;

        [Header("Discharge")]
        [Tooltip("At the turning point the backend answers 409 for one tick; the discharge is repeated after this delay.")]
        [SerializeField, Min(0.1f)] private float dischargeRetryDelaySeconds;
        [SerializeField, Min(0)] private int dischargeMaxRetries;
        [Tooltip("Same limit the backend validates in the trace (roast-trace.max-alerts).")]
        [SerializeField, Min(0)] private int maxDischargeAlerts;
        [Tooltip("Same limit the backend validates in the trace (roast-trace.max-alert-id-length).")]
        [SerializeField, Min(1)] private int maxAlertIdLength;

        [Header("Station messages")]
        [Tooltip("Seconds a rejected charge or discharge stays on screen.")]
        [SerializeField, Min(0.5f)] private float rejectionMessageSeconds;

        [Header("Gas knob")]
        [SerializeField] private int minGasPercent;
        [SerializeField] private int maxGasPercent;
        [SerializeField, Min(1)] private int gasStepPercent;
        [Tooltip("Knob angle, in degrees, that maps to the minimum gas.")]
        [SerializeField] private float knobMinAngle;
        [Tooltip("Knob angle, in degrees, that maps to the maximum gas.")]
        [SerializeField] private float knobMaxAngle;
        [Tooltip("While the knob turns, the latest gas is sent at most this often. It is always sent on release.")]
        [SerializeField, Min(0.05f)] private float gasSendIntervalSeconds;
        [Tooltip("Hand positions closer than this to the knob axis are ignored, because their angle is unstable.")]
        [SerializeField, Min(0f)] private float knobDeadZoneMeters;

        public string SimulationsPath => simulationsPath;
        public int RequestTimeoutSeconds => requestTimeoutSeconds;
        public float PollIntervalSeconds => pollIntervalSeconds;
        public string SessionsPath => sessionsPath;
        public int FeedbackTimeoutSeconds => feedbackTimeoutSeconds;
        public float EvaluationPollIntervalSeconds => evaluationPollIntervalSeconds;
        public int EvaluationPollMaxAttempts => evaluationPollMaxAttempts;
        public float DischargeRetryDelaySeconds => dischargeRetryDelaySeconds;
        public int DischargeMaxRetries => dischargeMaxRetries;
        public int MaxDischargeAlerts => maxDischargeAlerts;
        public int MaxAlertIdLength => maxAlertIdLength;
        public float RejectionMessageSeconds => rejectionMessageSeconds;
        public int MinGasPercent => minGasPercent;
        public int MaxGasPercent => maxGasPercent;
        public int GasStepPercent => gasStepPercent;
        public float KnobMinAngle => knobMinAngle;
        public float KnobMaxAngle => knobMaxAngle;
        public float GasSendIntervalSeconds => gasSendIntervalSeconds;
        public float KnobDeadZoneMeters => knobDeadZoneMeters;
    }
}
