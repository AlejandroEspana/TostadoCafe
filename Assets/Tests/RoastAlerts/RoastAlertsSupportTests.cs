using NUnit.Framework;
using ToastedVr.RoastSimulation;
using UnityEditor;

namespace ToastedVr.RoastAlerts.Tests
{
    public sealed class RoastAlertsSupportTests
    {
        private const string ConfigurationPath = "Assets/Settings/RoastAlerts/RoastAlertsConfiguration.asset";

        private static readonly AlertPackage Saved = AlertTestData.PackageFor("BEGINNER");
        private static readonly AlertPackage Fresh = AlertTestData.PackageFor("ADVANCED");

        private static RoastApiResult<AlertPackageDownload> Success(AlertPackageDownload download) =>
            new RoastApiResult<AlertPackageDownload>(download, RoastApiFailure.None, 200, null);

        [Test]
        public void ConfigurationMatchesTheAlertsContract()
        {
            var configuration = AssetDatabase.LoadAssetAtPath<RoastAlertsConfiguration>(ConfigurationPath);

            Assert.IsNotNull(configuration);
            Assert.AreEqual("/api/v1/roasting/assistant/alerts", configuration.AlertsPath);
            Assert.AreEqual(30, configuration.RequestTimeoutSeconds);
            Assert.IsNotEmpty(configuration.StorageFolderName);
        }

        [Test]
        public void ChoosesTheDownloadedPackageFirst()
        {
            var choice = AlertPackageChooser.Choose(new StoredAlertPackage(Saved, "e"), Success(new AlertPackageDownload(Fresh, "{}", "f")));

            Assert.AreSame(Fresh, choice.package);
            Assert.AreEqual(AlertPackageSource.Downloaded, choice.source);
        }

        [Test]
        public void UsesTheStoredOneWhenNotModifiedOrFailed()
        {
            var stored = new StoredAlertPackage(Saved, "e");
            var failed = new RoastApiResult<AlertPackageDownload>(null, RoastApiFailure.Unavailable, 503, null);

            Assert.AreSame(Saved, AlertPackageChooser.Choose(stored, Success(AlertPackageDownload.NotModified)).package);
            Assert.AreSame(Saved, AlertPackageChooser.Choose(stored, failed).package);
            Assert.AreEqual(AlertPackageSource.None, AlertPackageChooser.Choose(null, failed).source);
        }

        [TestCase("PREHEAT", false)]
        [TestCase("READY", true)]
        [TestCase("CHARGE_DIP", true)]
        [TestCase("ROASTING", true)]
        [TestCase("FINISHED", false)]
        public void OnlyReadyToDischargeTicksAreEvaluated(string phase, bool evaluated)
        {
            Assert.AreEqual(evaluated, AlertSampleFactory.TryCreate(new RoastState { Phase = phase }, out _));
        }

        [Test]
        public void SampleCopiesWhatTheEvaluatorReads()
        {
            var tick = new RoastState
            {
                Phase = "ROASTING", Temperature = 198.4, RoastSeconds = 510, MoisturePct = 4.2,
                FirstCrack = new CrackStatus { Reached = true, RoastSeconds = 500 }
            };

            Assert.IsTrue(AlertSampleFactory.TryCreate(tick, out var sample));
            Assert.AreEqual("ROASTING", sample.Phase);
            Assert.AreEqual(198.4, sample.Temperature);
            Assert.AreEqual(510, sample.RoastSeconds);
            Assert.IsTrue(sample.FirstCrackReached);
            Assert.AreEqual(4.2, sample.MoisturePct);
        }
    }
}
