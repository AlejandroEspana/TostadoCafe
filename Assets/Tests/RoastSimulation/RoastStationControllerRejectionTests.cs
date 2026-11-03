using NUnit.Framework;
using ToastedVr.SessionRefresh;
using UnityEditor;
using UnityEngine;

namespace ToastedVr.RoastSimulation.Tests
{
    public sealed class RoastStationControllerRejectionTests
    {
        private GameObject station;
        private RoastStationController controller;
        private RoastStationMessages texts;
        private string shownMessage;

        [SetUp]
        public void CreateStation()
        {
            shownMessage = null;
            texts = AssetDatabase.LoadAssetAtPath<RoastStationMessages>("Assets/Settings/RoastSimulation/RoastStationMessages.asset");
            station = new GameObject("Station");
            controller = station.AddComponent<RoastStationController>();
            var serialized = new SerializedObject(controller);
            serialized.FindProperty("configuration").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<RoastSimulationConfiguration>("Assets/Settings/RoastSimulation/RoastSimulationConfiguration.asset");
            serialized.FindProperty("messages").objectReferenceValue = texts;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            controller.MessageChanged += message => shownMessage = message;
        }

        [TearDown]
        public void DestroyStation()
        {
            Object.DestroyImmediate(station);
        }

        [Test]
        public void PressingChargeOrDischargeWhileOffExplainsWhy()
        {
            controller.Initialize("http://127.0.0.1:1", new AuthorizedRequestSender(null, null));

            controller.Charge();
            Assert.AreEqual(texts.PowerOnFirstMessage, shownMessage);
            Assert.IsFalse(controller.IsBusy);

            shownMessage = null;
            controller.Discharge();
            Assert.AreEqual(texts.PowerOnFirstMessage, shownMessage);
            Assert.IsFalse(controller.IsBusy);
        }

        [Test]
        public void StationThatIsNotReadyStaysQuiet()
        {
            controller.Charge();
            controller.Discharge();

            Assert.IsNull(shownMessage);
        }
    }
}
