using NUnit.Framework;

namespace ToastedVr.RoastSimulation.Tests
{
    public sealed class RoastJsonTests
    {
        // El ejemplo del contrato, tal cual.
        private const string ContractState =
            "{\"tick\": 1040, \"phase\": \"ROASTING\", \"roastSeconds\": 510," +
            "\"temperature\": 198.42, \"airTemperature\": 231.1, \"moisturePct\": 4.1," +
            "\"gasPercent\": 60, \"risePerMinute\": 14," +
            "\"chargeTemperature\": 190.3, \"targetTemperature\": 210, \"grainState\": \"MAILLARD\"," +
            "\"firstCrack\": {\"reached\": false, \"roastSeconds\": null}," +
            "\"secondCrack\": {\"reached\": true, \"roastSeconds\": 700}," +
            "\"alarms\": {\"burned\": false, \"baked\": true, \"burnWarning\": false, \"alarmLimitExceeded\": false, \"chargeOvershoot\": true}}";

        [Test]
        public void ReadsContractState()
        {
            var poll = RoastJson.TryDeserialize<SimulationPoll>("{\"state\": " + ContractState + ", \"ticks\": [" + ContractState + "]}");

            var state = poll.State;
            Assert.AreEqual(1040, state.Tick);
            Assert.AreEqual("ROASTING", state.Phase);
            Assert.AreEqual(510, state.RoastSeconds);
            Assert.AreEqual(198.42, state.Temperature, 1e-9);
            Assert.AreEqual(231.1, state.AirTemperature, 1e-9);
            Assert.AreEqual(4.1, state.MoisturePct.Value, 1e-9);
            Assert.AreEqual(60, state.GasPercent);
            Assert.AreEqual(14, state.RisePerMinute.Value, 1e-9);
            Assert.AreEqual(190.3, state.ChargeTemperature, 1e-9);
            Assert.AreEqual(210, state.TargetTemperature, 1e-9);
            Assert.AreEqual("MAILLARD", state.GrainState);
            Assert.IsFalse(state.FirstCrack.Reached);
            Assert.IsNull(state.FirstCrack.RoastSeconds);
            Assert.IsTrue(state.SecondCrack.Reached);
            Assert.AreEqual(700, state.SecondCrack.RoastSeconds);
            Assert.IsTrue(state.Alarms.Baked);
            Assert.IsTrue(state.Alarms.ChargeOvershoot);
            Assert.IsFalse(state.Alarms.Burned);
            Assert.AreEqual(1, poll.Ticks.Count);
        }

        [Test]
        public void ReadsCreatedSimulation()
        {
            var created = RoastJson.TryDeserialize<SimulationCreated>(
                "{\"simulationId\": \"0b7f0f7e-8f6b-4c1e-9d1b-1d2c3e4f5a6b\", \"state\": " + ContractState + "}");

            Assert.AreEqual("0b7f0f7e-8f6b-4c1e-9d1b-1d2c3e4f5a6b", created.SimulationId);
            Assert.AreEqual(1040, created.State.Tick);
        }

        [Test]
        public void ReadsDischargeSession()
        {
            var result = RoastJson.TryDeserialize<DischargeResult>(
                "{\"state\": " + ContractState + ", \"session\": {\"id\": 42, \"result\": \"GOOD\", \"qualityScore\": 74," +
                "\"finalScore\": 74, \"grade\": \"GOOD\", \"evaluation\": {\"status\": \"PENDING\", \"criteria\": []}}}");

            Assert.AreEqual(42, result.Session.Id);
            Assert.AreEqual(74, result.Session.FinalScore);
            Assert.AreEqual("GOOD", result.Session.Grade);
            Assert.AreEqual("PENDING", result.Session.Evaluation.Status);
        }

        [Test]
        public void ReadsBackendError()
        {
            var error = RoastJson.TryDeserialize<ApiError>(
                "{\"code\": \"SIMULATION_INVALID_PHASE\", \"message\": \"No puedes descargar todavía.\"}");

            Assert.AreEqual("SIMULATION_INVALID_PHASE", error.Code);
            Assert.AreEqual("No puedes descargar todavía.", error.Message);
        }

        [Test]
        public void WritesCamelCaseBodies()
        {
            Assert.AreEqual("{\"gasPercent\":55}", RoastJson.Serialize(new GasRequest { GasPercent = 55 }));
            Assert.AreEqual("{\"alerts\":[]}", RoastJson.Serialize(new DischargeRequest()));
            Assert.AreEqual(
                "{\"alerts\":[{\"id\":\"FIRST_CRACK\",\"t\":625}]}",
                RoastJson.Serialize(new DischargeRequest { Alerts = { new ProactiveAlert { Id = "FIRST_CRACK", T = 625 } } }));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("<html>502</html>")]
        [TestCase("{\"tick\": ")]
        public void UnreadableBodyGivesNull(string body)
        {
            Assert.IsNull(RoastJson.TryDeserialize<SimulationPoll>(body));
        }
    }
}
