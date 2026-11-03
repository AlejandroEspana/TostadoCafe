using NUnit.Framework;
using UnityEditor;

namespace ToastedVr.RoastSimulation.Tests
{
    public sealed class RoastActionRejectionTests
    {
        private RoastStationMessages texts;

        [SetUp]
        public void LoadTexts()
        {
            texts = AssetDatabase.LoadAssetAtPath<RoastStationMessages>("Assets/Settings/RoastSimulation/RoastStationMessages.asset");
            Assert.IsNotNull(texts);
        }

        [TestCase(RoastAction.Charge, "PREHEAT")]
        [TestCase(RoastAction.Charge, "READY")]
        [TestCase(RoastAction.Discharge, "ROASTING")]
        public void AllowedActionsAreNotRejected(RoastAction action, string phase)
        {
            Assert.IsNull(RoastActionRejection.MessageFor(action, true, phase, texts));
        }

        [TestCase(RoastAction.Charge)]
        [TestCase(RoastAction.Discharge)]
        public void PoweredOffStationAsksToPowerOnFirst(RoastAction action)
        {
            Assert.AreEqual(texts.PowerOnFirstMessage, RoastActionRejection.MessageFor(action, false, null, texts));
            Assert.AreEqual(texts.PowerOnFirstMessage, RoastActionRejection.MessageFor(action, false, "READY", texts));
        }

        [TestCase(RoastAction.Discharge, "PREHEAT", nameof(RoastStationMessages.NotChargedYetMessage))]
        [TestCase(RoastAction.Discharge, "READY", nameof(RoastStationMessages.NotChargedYetMessage))]
        [TestCase(RoastAction.Discharge, "CHARGE_DIP", nameof(RoastStationMessages.WaitForChargeDipMessage))]
        [TestCase(RoastAction.Charge, "CHARGE_DIP", nameof(RoastStationMessages.AlreadyChargedMessage))]
        [TestCase(RoastAction.Charge, "ROASTING", nameof(RoastStationMessages.AlreadyChargedMessage))]
        [TestCase(RoastAction.Charge, "FINISHED", nameof(RoastStationMessages.RoastFinishedMessage))]
        [TestCase(RoastAction.Discharge, "FINISHED", nameof(RoastStationMessages.RoastFinishedMessage))]
        [TestCase(RoastAction.Charge, null, nameof(RoastStationMessages.ActionNotAvailableMessage))]
        [TestCase(RoastAction.Discharge, "UNKNOWN", nameof(RoastStationMessages.ActionNotAvailableMessage))]
        public void RejectedActionsExplainWhy(RoastAction action, string phase, string expectedText)
        {
            var expected = (string)typeof(RoastStationMessages).GetProperty(expectedText).GetValue(texts);

            Assert.AreEqual(expected, RoastActionRejection.MessageFor(action, true, phase, texts));
        }
    }
}
