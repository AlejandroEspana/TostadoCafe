using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using ToastedVr.RoastSimulation;

namespace ToastedVr.RoastAlerts.Tests
{
    // Copias de IA/config/proactive_alerts.json y de IA/eval/alerts/conformance.json (revisión 4 del catálogo).
    // Si el catálogo cambia en Graduation_Project, se vuelven a copiar los dos juntos.
    public static class AlertTestData
    {
        public const string DataFolder = "Assets/Tests/RoastAlerts/Data";
        public static readonly string[] Levels = { "BEGINNER", "INTERMEDIATE", "ADVANCED" };

        public static JObject Catalog() => JObject.Parse(File.ReadAllText(Path.Combine(DataFolder, "proactive_alerts.json")));

        public static JObject Conformance() => JObject.Parse(File.ReadAllText(Path.Combine(DataFolder, "conformance.json")));

        // Arma el paquete de un nivel igual que IA/app/alerts/package.py, sin audio.
        public static AlertPackage PackageFor(string level)
        {
            var catalog = Catalog();
            var package = new JObject
            {
                ["schemaVersion"] = catalog["schemaVersion"],
                ["catalogRevision"] = catalog["catalogRevision"],
                ["level"] = level,
                ["signals"] = catalog["signals"],
                ["channel"] = catalog["channel"],
                ["rules"] = new JArray(catalog["rules"]
                    .Where(rule => rule["levels"].Values<string>().Contains(level))
                    .Select(rule => new JObject
                    {
                        ["id"] = rule["id"],
                        ["priority"] = rule["priority"],
                        ["when"] = rule["when"],
                        ["sustainSeconds"] = rule["sustainSeconds"],
                        ["rearmWhen"] = rule["rearmWhen"],
                        ["cooldownSeconds"] = rule["cooldownSeconds"],
                        ["maxRepeats"] = rule["maxRepeats"],
                        ["requiresQuietSeconds"] = rule["requiresQuietSeconds"],
                        ["text"] = rule["phrases"][level],
                        ["audio"] = null
                    }))
            };

            return RoastJson.TryDeserialize<AlertPackage>(package.ToString());
        }
    }
}
