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
        public void ReadsCompletedEvaluation()
        {
            var session = RoastJson.TryDeserialize<RoastSessionSummary>(
                "{\"id\": 42, \"result\": \"PERFECT\", \"qualityScore\": 80, \"finalScore\": 77, \"grade\": \"GOOD\"," +
                "\"evaluation\": {\"status\": \"COMPLETED\", \"skipReason\": null, \"finalScore\": 77, \"rulesScore\": 80," +
                "\"rubricScore\": 70.5, \"rulesWeight\": 0.7, \"aiWeight\": 0.3, \"defectCapApplied\": true," +
                "\"rubricVersion\": \"draft-2026-10-30\", \"model\": \"gpt-oss-120b\"," +
                "\"criteria\": [{\"id\": \"DRYING_GAS\", \"level\": 2, \"maxLevel\": 3, \"justification\": \"Subiste el gas a tiempo.\"}]," +
                "\"notApplicable\": [\"ALERT_REACTION\"]}}");

            var evaluation = session.Evaluation;
            Assert.AreEqual(80, session.QualityScore);
            Assert.AreEqual("COMPLETED", evaluation.Status);
            Assert.IsNull(evaluation.SkipReason);
            Assert.AreEqual(77, evaluation.FinalScore);
            Assert.AreEqual(80, evaluation.RulesScore);
            Assert.AreEqual(70.5, evaluation.RubricScore.Value, 1e-9);
            Assert.AreEqual(0.7, evaluation.RulesWeight.Value, 1e-9);
            Assert.AreEqual(0.3, evaluation.AiWeight.Value, 1e-9);
            Assert.IsTrue(evaluation.DefectCapApplied);
            Assert.AreEqual(1, evaluation.Criteria.Count);
            Assert.AreEqual("DRYING_GAS", evaluation.Criteria[0].Id);
            Assert.AreEqual(2, evaluation.Criteria[0].Level);
            Assert.AreEqual(3, evaluation.Criteria[0].MaxLevel);
            Assert.AreEqual("Subiste el gas a tiempo.", evaluation.Criteria[0].Justification);
            CollectionAssert.AreEqual(new[] { "ALERT_REACTION" }, evaluation.NotApplicable);
        }

        [Test]
        public void ReadsSkippedEvaluationWithNullLists()
        {
            var evaluation = RoastJson.TryDeserialize<RoastEvaluationSummary>(
                "{\"status\": \"NOT_APPLICABLE\", \"skipReason\": \"AUTO_MODE\", \"finalScore\": null, \"rulesScore\": 64," +
                "\"rubricScore\": null, \"rulesWeight\": null, \"aiWeight\": null, \"defectCapApplied\": null," +
                "\"criteria\": null, \"notApplicable\": null}");

            Assert.AreEqual("AUTO_MODE", evaluation.SkipReason);
            Assert.IsNull(evaluation.FinalScore);
            Assert.IsNull(evaluation.RubricScore);
            Assert.IsFalse(evaluation.DefectCapApplied);
            Assert.IsNotNull(evaluation.Criteria);
            Assert.IsNotNull(evaluation.NotApplicable);
        }

        [Test]
        public void ReadsSessionSavedBeforeTheCombinedScore()
        {
            var session = RoastJson.TryDeserialize<RoastSessionSummary>(
                "{\"id\": 7, \"qualityScore\": 55, \"finalScore\": 55, \"grade\": \"FAIR\", \"evaluation\": null}");

            Assert.IsNull(session.Evaluation);
            Assert.AreEqual(55, session.FinalScore);
        }

        [Test]
        public void ReadsFeedback()
        {
            var feedback = RoastJson.TryDeserialize<RoastFeedback>("{\"feedback\": \"Buen control del gas.\"}");

            Assert.AreEqual("Buen control del gas.", feedback.Feedback);
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
