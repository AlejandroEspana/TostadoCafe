using NUnit.Framework;

namespace ToastedVr.RoastSimulation.Tests
{
    public sealed class RoastPhaseRulesTests
    {
        [TestCase("PREHEAT", true, false, true)]
        [TestCase("READY", true, false, true)]
        [TestCase("CHARGE_DIP", false, false, true)]
        [TestCase("ROASTING", false, true, true)]
        [TestCase("FINISHED", false, false, false)]
        [TestCase(null, false, false, false)]
        [TestCase("UNKNOWN", false, false, false)]
        public void AllowedActionsFollowThePhase(string phase, bool canCharge, bool canDischarge, bool canChangeGas)
        {
            Assert.AreEqual(canCharge, RoastPhaseRules.CanCharge(phase), "cargar");
            Assert.AreEqual(canDischarge, RoastPhaseRules.CanDischarge(phase), "descargar");
            Assert.AreEqual(canChangeGas, RoastPhaseRules.CanChangeGas(phase), "gas");
            Assert.AreEqual(canChangeGas, RoastPhaseRules.IsRunning(phase), "en marcha");
        }
    }
}
