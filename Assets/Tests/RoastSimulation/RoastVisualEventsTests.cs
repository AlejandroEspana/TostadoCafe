using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace ToastedVr.RoastSimulation.Tests
{
    public sealed class RoastVisualEventsTests
    {
        private static RoastState State(string phase, string grain = "DRYING", bool first = false, bool second = false,
            bool warning = false, bool burned = false) => new RoastState
        {
            Phase = phase,
            GrainState = grain,
            FirstCrack = new CrackStatus { Reached = first },
            SecondCrack = new CrackStatus { Reached = second },
            Alarms = new RoastAlarms { BurnWarning = warning, Burned = burned }
        };

        private static string[] Kinds(IEnumerable<RoastVisualEvent> events) =>
            events.Select(e => e.Value == null ? e.Kind.ToString() : e.Kind + ":" + e.Value).ToArray();

        [Test]
        public void FollowsAWholeRoastOnceEach()
        {
            var detector = new RoastVisualEventDetector();
            var seen = new List<string>();
            void Step(bool powered, RoastState state) => seen.AddRange(Kinds(detector.Update(powered, state)));

            Step(true, State("PREHEAT"));
            Step(true, State("READY"));
            Step(true, State("READY"));
            Step(true, State("CHARGE_DIP"));
            Step(true, State("ROASTING", "MAILLARD"));
            Step(true, State("ROASTING", "FIRST_CRACK", first: true));
            Step(true, State("ROASTING", "FIRST_CRACK", first: true));
            Step(true, State("ROASTING", "DARK", first: true, warning: true));
            Step(true, State("ROASTING", "SECOND_CRACK", first: true, second: true, warning: true));
            Step(true, State("ROASTING", "SECOND_CRACK", first: true, second: true));
            Step(true, State("ROASTING", "BURNED", first: true, second: true, burned: true));
            Step(true, State("FINISHED", "BURNED", first: true, second: true, burned: true));
            Step(false, null);

            CollectionAssert.AreEqual(new[]
            {
                "PoweredOn", "Charged", "GrainStateChanged:DRYING", "TurningPointReached", "GrainStateChanged:MAILLARD",
                "FirstCrack", "GrainStateChanged:FIRST_CRACK", "GrainStateChanged:DARK", "BurnWarningStarted",
                "SecondCrack", "GrainStateChanged:SECOND_CRACK", "BurnWarningEnded", "GrainStateChanged:BURNED", "Burned",
                "PoweredOff"
            }, seen);
        }

        [Test]
        public void ASkippedChargeDipStillCountsAsChargeAndTurningPoint()
        {
            var detector = new RoastVisualEventDetector();
            detector.Update(true, State("READY"));

            CollectionAssert.AreEqual(new[] { "Charged", "TurningPointReached", "GrainStateChanged:MAILLARD" },
                Kinds(detector.Update(true, State("ROASTING", "MAILLARD"))));
        }

        [Test]
        public void PoweringOnAgainStartsFromScratch()
        {
            var detector = new RoastVisualEventDetector();
            detector.Update(true, State("ROASTING", "FIRST_CRACK", first: true));
            detector.Update(false, null);

            CollectionAssert.AreEqual(new[] { "PoweredOn" }, Kinds(detector.Update(true, State("PREHEAT"))));
            CollectionAssert.Contains(Kinds(detector.Update(true, State("ROASTING", "FIRST_CRACK", first: true))), "FirstCrack");
        }

        [Test]
        public void TheComponentRaisesItsUnityEvents()
        {
            var host = new GameObject("VisualEventsTest");
            host.SetActive(false);
            try
            {
                var events = host.AddComponent<RoastStationVisualEvents>();
                var seen = new List<string>();
                events.PoweredOn.AddListener(() => seen.Add("on"));
                events.Charged.AddListener(() => seen.Add("charged"));
                events.GrainStateChanged.AddListener(grain => seen.Add("grain:" + grain));
                events.Discharged.AddListener(result => seen.Add("result:" + result));

                events.Observe(true, State("READY"));
                events.Observe(true, State("CHARGE_DIP"));
                events.OnDischarged(new RoastSessionSummary { Result = "PERFECT" });
                events.OnDischarged(null);

                CollectionAssert.AreEqual(new[] { "on", "charged", "grain:DRYING", "result:PERFECT", "result:" }, seen);
            }
            finally
            {
                Object.DestroyImmediate(host);
            }
        }
    }
}
