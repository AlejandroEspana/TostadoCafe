using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using ToastedVr.RoastVoice;

namespace ToastedVr.RoastAlerts
{
    // Forma del paquete de GET /api/v1/roasting/assistant/alerts (IA/docs/unity-proactive-alerts-contract.md).
    public sealed class AlertPackage
    {
        public const int SupportedSchemaVersion = 1;

        public int SchemaVersion { get; set; }
        public int CatalogRevision { get; set; }
        public string Level { get; set; }
        public SignalSettings Signals { get; set; }
        public ChannelPolicy Channel { get; set; }
        public List<AlertRuleModel> Rules { get; set; } = new List<AlertRuleModel>();

        // Un schemaVersion desconocido significa que cambió la forma de las reglas: ese paquete se descarta.
        public static bool IsUsable(AlertPackage package) =>
            package != null && package.SchemaVersion == SupportedSchemaVersion
            && package.Rules != null && package.Signals != null && package.Channel != null;
    }

    public sealed class SignalSettings
    {
        public int RiseWindowSeconds { get; set; }
        public double MaillardStartTemperature { get; set; }
        public double MaillardEndTemperature { get; set; }
        public double BurnCeilingTemperature { get; set; }
        public double TurningPointMinRise { get; set; }
    }

    public sealed class ChannelPolicy
    {
        public int QueueMaxWaitSeconds { get; set; }
        public int MinGapSeconds { get; set; }
        public List<string> UrgentPriorities { get; set; } = new List<string>();
        public List<string> UnqueuedPriorities { get; set; } = new List<string>();
    }

    public sealed class AlertRuleModel
    {
        public string Id { get; set; }
        public string Priority { get; set; }
        public List<AlertCondition> When { get; set; } = new List<AlertCondition>();
        public int SustainSeconds { get; set; }

        // null: se re-arma cuando deja de cumplirse When. Lista vacía: se re-arma de inmediato.
        public List<AlertCondition> RearmWhen { get; set; }
        public int CooldownSeconds { get; set; }
        public int MaxRepeats { get; set; }
        public int RequiresQuietSeconds { get; set; }
        public string Text { get; set; }
        public VoiceAudio Audio { get; set; }
    }

    // value puede ser número, booleano o el nombre de una fase.
    public sealed class AlertCondition
    {
        public string Signal { get; set; }
        public string Op { get; set; }
        public JToken Value { get; set; }
    }
}
