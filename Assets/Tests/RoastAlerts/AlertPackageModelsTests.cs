using Newtonsoft.Json.Linq;
using NUnit.Framework;
using ToastedVr.RoastSimulation;

namespace ToastedVr.RoastAlerts.Tests
{
    public sealed class AlertPackageModelsTests
    {
        [Test]
        public void ReadsTheContractExample()
        {
            var package = RoastJson.TryDeserialize<AlertPackage>(
                "{\"schemaVersion\": 1, \"catalogRevision\": 4, \"level\": \"BEGINNER\"," +
                "\"signals\": {\"riseWindowSeconds\": 30, \"maillardStartTemperature\": 160, \"maillardEndTemperature\": 200," +
                "\"burnCeilingTemperature\": 220, \"turningPointMinRise\": 0.5}," +
                "\"channel\": {\"queueMaxWaitSeconds\": 5, \"minGapSeconds\": 8, \"urgentPriorities\": [\"CRITICAL\"], \"unqueuedPriorities\": [\"LOW\"]}," +
                "\"rules\": [{\"id\": \"BURN_WARNING\", \"priority\": \"HIGH\"," +
                "\"when\": [{\"signal\": \"phase\", \"op\": \"eq\", \"value\": \"ROASTING\"}, {\"signal\": \"temperature\", \"op\": \"ge\", \"value\": 215}]," +
                "\"sustainSeconds\": 1, \"rearmWhen\": [{\"signal\": \"temperature\", \"op\": \"lt\", \"value\": 213}]," +
                "\"cooldownSeconds\": 20, \"maxRepeats\": 2, \"requiresQuietSeconds\": 0, \"text\": \"Cuidado\"," +
                "\"audio\": {\"format\": \"wav\", \"sampleRateHz\": 22050, \"provider\": \"azure\", \"base64\": \"UklGR\"}}]}");

            Assert.AreEqual(1, package.SchemaVersion);
            Assert.AreEqual(4, package.CatalogRevision);
            Assert.AreEqual(30, package.Signals.RiseWindowSeconds);
            Assert.AreEqual(0.5, package.Signals.TurningPointMinRise, 1e-9);
            CollectionAssert.AreEqual(new[] { "CRITICAL" }, package.Channel.UrgentPriorities);
            var rule = package.Rules[0];
            Assert.AreEqual("BURN_WARNING", rule.Id);
            Assert.AreEqual("ROASTING", rule.When[0].Value.Value<string>());
            Assert.AreEqual(215, rule.When[1].Value.Value<double>(), 1e-9);
            Assert.AreEqual(1, rule.RearmWhen.Count);
            Assert.AreEqual(22050, rule.Audio.SampleRateHz);
        }

        [Test]
        public void NullRearmIsKeptApartFromAnEmptyList()
        {
            var package = RoastJson.TryDeserialize<AlertPackage>(
                "{\"rules\": [{\"id\": \"A\", \"rearmWhen\": null}, {\"id\": \"B\", \"rearmWhen\": []}, {\"id\": \"C\", \"audio\": null}]}");

            Assert.IsNull(package.Rules[0].RearmWhen);
            Assert.IsNotNull(package.Rules[1].RearmWhen);
            Assert.AreEqual(0, package.Rules[1].RearmWhen.Count);
            Assert.IsNull(package.Rules[2].Audio);
        }

        [TestCase("BEGINNER", 18)]
        [TestCase("INTERMEDIATE", 18)]
        [TestCase("ADVANCED", 13)]
        public void BuildsEachLevelFromTheCatalogCopy(string level, int expectedRules)
        {
            var package = AlertTestData.PackageFor(level);

            Assert.AreEqual(AlertPackage.SupportedSchemaVersion, package.SchemaVersion);
            Assert.AreEqual(4, package.CatalogRevision);
            Assert.AreEqual(expectedRules, package.Rules.Count);
            Assert.IsTrue(package.Rules.TrueForAll(rule => !string.IsNullOrWhiteSpace(rule.Text)));
        }

        [Test]
        public void TestDataBelongsToTheSameCatalogRevision()
        {
            Assert.AreEqual(
                AlertTestData.Catalog()["catalogRevision"].Value<int>(),
                AlertTestData.Conformance()["catalogRevision"].Value<int>());
        }
    }
}
