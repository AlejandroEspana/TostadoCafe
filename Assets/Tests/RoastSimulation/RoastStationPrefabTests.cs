using NUnit.Framework;
using ToastedVr.RoastSimulation.Editor;
using UnityEditor;
using UnityEngine;

namespace ToastedVr.RoastSimulation.Tests
{
    public sealed class RoastStationPrefabTests
    {
        [Test]
        public void VersionedPrefabIsFullyWired()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RoastStationPrefabGenerator.PrefabPath);

            RoastStationPrefabAssertions.AssertFullyWired(prefab);
        }
    }
}
