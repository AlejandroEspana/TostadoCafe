using System.IO;
using NUnit.Framework;
using ToastedVr.RoastSimulation;

namespace ToastedVr.RoastAlerts.Tests
{
    public sealed class AlertPackageStoreTests
    {
        private string directory;
        private AlertPackageStore store;

        [SetUp]
        public void SetUp()
        {
            directory = Path.Combine(Path.GetTempPath(), "ToastedVrAlertsTest_" + System.Guid.NewGuid().ToString("N"));
            store = new AlertPackageStore(directory);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, true);
            }
        }

        [Test]
        public void NothingSavedLoadsNull()
        {
            Assert.IsNull(store.Load());
        }

        [Test]
        public void SavesAndLoadsThePackageWithItsETag()
        {
            Assert.IsTrue(store.Save(RoastJson.Serialize(AlertTestData.PackageFor("ADVANCED")), "\"etag-1\""));

            var stored = store.Load();

            Assert.AreEqual("\"etag-1\"", stored.ETag);
            Assert.AreEqual("ADVANCED", stored.Package.Level);
            Assert.AreEqual(13, stored.Package.Rules.Count);
        }

        [Test]
        public void SavingWithoutETagForgetsTheOldOne()
        {
            var json = RoastJson.Serialize(AlertTestData.PackageFor("BEGINNER"));
            store.Save(json, "\"etag-1\"");
            store.Save(json, null);

            Assert.IsNull(store.Load().ETag);
        }

        [Test]
        public void DiscardsUnknownSchemaOrDamagedFiles()
        {
            var package = AlertTestData.PackageFor("BEGINNER");
            package.SchemaVersion = AlertPackage.SupportedSchemaVersion + 1;
            store.Save(RoastJson.Serialize(package), "\"etag-2\"");
            Assert.IsNull(store.Load(), "schemaVersion desconocido");

            store.Save("{not json", "\"etag-3\"");
            Assert.IsNull(store.Load(), "archivo dañado");
        }
    }
}
