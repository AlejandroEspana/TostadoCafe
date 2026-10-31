using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace ToastedVr.RoastAlerts.Tests
{
    // Mismas curvas y mismas alertas, segundo a segundo, que el evaluador de referencia en Python.
    public sealed class AlertConformanceTests
    {
        private static IEnumerable<TestCaseData> Cases()
        {
            var vectors = AlertTestData.Conformance()["vectors"];
            foreach (var vector in vectors)
            {
                foreach (var level in AlertTestData.Levels)
                {
                    yield return new TestCaseData((string)vector["curve"], level).SetName($"Conformance_{vector["curve"]}_{level}");
                }
            }
        }

        [TestCaseSource(nameof(Cases))]
        public void MatchesTheReference(string curve, string level)
        {
            var vector = AlertTestData.Conformance()["vectors"].First(item => (string)item["curve"] == curve);
            var evaluator = new AlertEvaluator(AlertTestData.PackageFor(level));
            var actual = new List<string>();

            foreach (var sample in vector["samples"])
            {
                var moisture = sample[4].Type == JTokenType.Null ? (double?)null : (double)sample[4];
                var delivery = evaluator.Step(
                    new AlertSample((string)sample[0], (double)sample[1], (int)sample[2], (bool)sample[3], moisture),
                    (bool)sample[5], (bool)sample[6]);
                if (delivery != null)
                {
                    actual.Add($"{delivery.Second}/{delivery.RoastSeconds}/{delivery.Rule.Id}/{delivery.Mode}");
                }
            }

            var expected = vector["expected"][level]
                .Select(item => $"{item["second"]}/{item["roastSeconds"]}/{item["alertId"]}/{item["mode"]}")
                .ToList();
            CollectionAssert.AreEqual(expected, actual);
        }
    }
}
