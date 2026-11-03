using NUnit.Framework;
using ToastedVr.SessionRefresh;
using UnityEditor;
using UnityEngine;

namespace ToastedVr.RoastSimulation.Tests
{
    public sealed class RoastStationControllerTicksTests
    {
        private const string ConfigurationPath = "Assets/Settings/RoastSimulation/RoastSimulationConfiguration.asset";

        private GameObject station;

        [TearDown]
        public void DestroyStation()
        {
            Object.DestroyImmediate(station);
        }

        [Test]
        public void HasNoTicksBeforeConnecting()
        {
            station = new GameObject("Station");
            var controller = station.AddComponent<RoastStationController>();

            Assert.IsNotNull(controller.Ticks);
            Assert.AreEqual(0, controller.Ticks.Count);
        }

        [Test]
        public void HasNoTicksBeforePoweringOn()
        {
            station = new GameObject("Station");
            var controller = station.AddComponent<RoastStationController>();
            var serialized = new SerializedObject(controller);
            serialized.FindProperty("configuration").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<RoastSimulationConfiguration>(ConfigurationPath);
            serialized.ApplyModifiedPropertiesWithoutUndo();

            controller.Initialize("http://127.0.0.1:1", new AuthorizedRequestSender(null, null));

            Assert.AreEqual(0, controller.Ticks.Count);
        }
    }
}
