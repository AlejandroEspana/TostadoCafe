using System.Linq;
using NUnit.Framework;

namespace ToastedVr.RoastSimulation.Tests
{
    public sealed class DeliveredAlertLogTests
    {
        [Test]
        public void KeepsAlertsInOrderWithTheirRoastSecond()
        {
            var log = new DeliveredAlertLog(60, 40);
            log.Record("CHARGE_LOW", 0);
            log.Record("FIRST_CRACK", 625);

            CollectionAssert.AreEqual(new[] { "CHARGE_LOW:0", "FIRST_CRACK:625" }, log.Alerts.Select(a => $"{a.Id}:{a.T}"));
        }

        [Test]
        public void SkipsWhatTheBackendWouldReject()
        {
            var log = new DeliveredAlertLog(2, 5);
            log.Record("", 1);
            log.Record("TOO_LONG", 1);
            log.Record("NEG", -1);
            log.Record("A", 1);
            log.Record("B", 2);
            log.Record("C", 3);

            CollectionAssert.AreEqual(new[] { "A", "B" }, log.Alerts.Select(a => a.Id));
        }

        [Test]
        public void SnapshotIsACopyAndClearEmpties()
        {
            var log = new DeliveredAlertLog(60, 40);
            log.Record("A", 1);
            var snapshot = log.Snapshot();
            log.Clear();

            Assert.AreEqual(1, snapshot.Count);
            Assert.AreEqual(0, log.Alerts.Count);
        }
    }
}
