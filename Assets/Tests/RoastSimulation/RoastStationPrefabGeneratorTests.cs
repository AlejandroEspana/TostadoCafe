using NUnit.Framework;
using ToastedVr.RoastSimulation.Editor;
using UnityEditor;

namespace ToastedVr.RoastSimulation.Tests
{
    public sealed class RoastStationPrefabGeneratorTests
    {
        private const string TemporaryFolder = "Assets/Tests/RoastSimulation/GeneratedByTest";
        private const string TemporaryPrefab = TemporaryFolder + "/RoastStation.prefab";

        [TearDown]
        public void DeleteGeneratedPrefab()
        {
            AssetDatabase.DeleteAsset(TemporaryFolder);
        }

        [Test]
        public void BuildsAFullyWiredStation()
        {
            var prefab = RoastStationPrefabGenerator.Build(TemporaryPrefab);

            RoastStationPrefabAssertions.AssertFullyWired(prefab);
        }

        [Test]
        public void RebuildingReplacesThePrefab()
        {
            RoastStationPrefabGenerator.Build(TemporaryPrefab);
            var guid = AssetDatabase.AssetPathToGUID(TemporaryPrefab);

            var rebuilt = RoastStationPrefabGenerator.Build(TemporaryPrefab);

            Assert.AreEqual(guid, AssetDatabase.AssetPathToGUID(TemporaryPrefab));
            RoastStationPrefabAssertions.AssertFullyWired(rebuilt);
        }
    }
}
