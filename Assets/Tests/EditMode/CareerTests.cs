using System.Collections.Generic;
using NUnit.Framework;
using Zmywak.Core.Data;
using Zmywak.Core.Save;
using Zmywak.Meta.Economy;
using Zmywak.Meta.Progress;

namespace Zmywak.Tests
{
    public class CareerTests
    {
        private static ShiftResult Shift(int cleaned, int broken = 0, int racks = 1, int flow = 2, bool wholeShift = true,
            SpecialShift special = SpecialShift.None, bool inspectionPassed = true, bool tray = false) =>
            new ShiftResult
            {
                DishesCleaned = cleaned,
                DishesBroken = broken,
                FullRackLoads = racks,
                BestFlow = flow,
                PlannedSeconds = 180f,
                ElapsedSeconds = wholeShift ? 180f : 60f,
                Special = special,
                InspectionPassed = inspectionPassed,
                TrayScrubbed = tray
            };

        [Test]
        public void ANewPorter_StartsAtTheBottom()
        {
            var record = new CareerRecord();
            Assert.AreEqual(0, Career.RankFor(record));
            Assert.AreEqual("Nowy na zmywaku", Career.Title(0));
            Assert.AreEqual("Król Zmywaka", Career.Title(Career.KingRank));
            Assert.AreEqual(0f, Career.HourlyBonusEuros(0));
            Assert.AreEqual(4 * Career.HourlyBonusPerRankEuros, Career.HourlyBonusEuros(Career.KingRank), 1e-4f);
        }

        [Test]
        public void ARank_AsksForEverything_NotJustTheDishes()
        {
            var record = new CareerRecord { dishesWashed = 5000 };
            Assert.AreEqual(0, Career.RankFor(record), "Dishes alone, with nothing sent off full and no clean shift, are not a promotion");

            record = new CareerRecord { dishesWashed = 40, fullRacks = 2, cleanShifts = 1 };
            Assert.AreEqual(1, Career.RankFor(record));
        }

        [Test]
        public void TheKing_HasPassedInspections_ScrubbedTheTray_AndKeptAFlow()
        {
            var record = new CareerRecord { dishesWashed = 1000, fullRacks = 100, cleanShifts = 50, inspectionsPassed = 2, traysScrubbed = 2, bestFlow = 7 };
            Assert.AreEqual(3, Career.RankFor(record), "A Flow of 7 is not a King's");
            record.bestFlow = 8;
            Assert.AreEqual(Career.KingRank, Career.RankFor(record));
            Assert.AreEqual(1f, Career.ProgressToNext(record, Career.KingRank));
            Assert.IsEmpty(Career.NextGoal(record, Career.KingRank));
        }

        [Test]
        public void ACleanShift_IsAWholeShiftWithoutABreakage()
        {
            var record = new CareerRecord();
            record.Record(Shift(10));
            record.Record(Shift(10, broken: 1));
            record.Record(Shift(5, wholeShift: false));
            Assert.AreEqual(1, record.cleanShifts, "A breakage, or walking out early, is not a clean shift");
            Assert.AreEqual(3, record.shifts);
            Assert.AreEqual(25, record.dishesWashed);

            record.Record(Shift(8, special: SpecialShift.Inspection));
            record.Record(Shift(8, broken: 1, special: SpecialShift.Inspection, inspectionPassed: false));
            record.Record(Shift(8, special: SpecialShift.LasagneTray, tray: true, flow: 6));
            Assert.AreEqual(1, record.inspectionsPassed);
            Assert.AreEqual(1, record.traysScrubbed);
            Assert.AreEqual(6, record.bestFlow);
        }

        [Test]
        public void TheNextGoal_SaysWhatIsMissing_AndTheProgressIsTheFurthestOff()
        {
            var record = new CareerRecord { dishesWashed = 30, fullRacks = 2, cleanShifts = 0 };
            Assert.AreEqual(0f, Career.ProgressToNext(record, 0), 1e-4f, "No clean shift yet");
            string goal = Career.NextGoal(record, 0);
            StringAssert.StartsWith("Do awansu:", goal);
            StringAssert.Contains("naczynia 30/40", goal);
            StringAssert.Contains("czyste zmiany 0/1", goal);
            StringAssert.DoesNotContain("kosze", goal, "Met requirements are not listed");
        }

        [Test]
        public void TheLedger_Promotes_Announces_AndNeverTakesARankAway()
        {
            var ledger = new LifeLedger(LifeRules.Default, null);
            var notices = new List<string>();
            ledger.OnNotice += notices.Add;
            for (int day = 0; day < 4; day++)
            {
                ledger.CompleteShift(Shift(10, racks: 1));
            }

            Assert.AreEqual(1, ledger.Rank);
            Assert.IsTrue(notices.Exists(n => n.StartsWith("Awans: Pomywacz!")), "The promotion was not announced");

            ledger.CompleteShift(Shift(0, broken: 5, racks: 0, wholeShift: false));
            Assert.AreEqual(1, ledger.Rank, "A bad shift does not take a rank away");

            var data = new SaveData();
            ledger.WriteTo(data);
            var reloaded = new LifeLedger(LifeRules.Default, null);
            reloaded.LoadFrom(data);
            Assert.AreEqual(1, reloaded.Rank);
            Assert.AreEqual(ledger.WorkRecord.dishesWashed, reloaded.WorkRecord.dishesWashed);
        }
    }
}
