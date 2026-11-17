// Reconstructed on 2026-11-17 by decompiling Library/ScriptAssemblies/Zmywak.Tests.EditMode.dll (built 2026-10-11):
// the original source was lost from disk. Behaviour matches that build; original comments are gone.
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using Zmywak.Core.Data;
using Zmywak.Core.Save;
using Zmywak.Meta.Economy;
using Zmywak.Meta.Progress;

namespace Zmywak.Tests
{
    public class ShiftScheduleTests
    {
        private const string CatalogPath = "Assets/_Project/Resources/ShiftCatalog.asset";

        private static ShiftCatalog Catalog()
        {
            ShiftCatalog shiftCatalog = AssetDatabase.LoadAssetAtPath<ShiftCatalog>("Assets/_Project/Resources/ShiftCatalog.asset");
            Assert.IsNotNull(shiftCatalog, "Assets/_Project/Resources/ShiftCatalog.asset has not been generated (Zmywak > Setup > 1. Create Kitchen Content Assets)");
            return shiftCatalog;
        }

        private static int Arrivals(ShiftDefinition shift, float storyPace = 1f, float specialPace = 1f, int extraOpening = 0)
        {
            float t = 0f;
            int count;
            float p;
            for (count = 0; t < shift.DurationSeconds; t += ((count < shift.OpeningDishes + extraOpening) ? 0.6f : (shift.SpawnIntervalAt(p) * specialPace * storyPace / ServiceWaves.PaceAt(shift.Waves, p))))
            {
                count++;
                p = t / shift.DurationSeconds;
            }
            return count;
        }

        [Test]
        public void TheFirstDays_AreCalm_AndHardNightsComeOnlyFromChapterFour()
        {
            for (int day = 1; day <= 3; day++)
            {
                Assert.AreEqual("calm_start", ShiftSchedule.ArchetypeFor(day, SpecialShift.None), $"Day {day}");
            }
            for (int i = 4; i <= 80; i++)
            {
                string kind = ShiftSchedule.ArchetypeFor(i, SpecialShift.None);
                Assert.AreNotEqual("calm_start", kind, $"Day {i} is still calm");
                if (i < 19)
                {
                    Assert.AreNotEqual("hard_night", kind, $"A hard night on day {i}, before chapter four");
                }
            }
            HashSet<string> storyKinds = new HashSet<string>(from d in Enumerable.Range(1, 35)
                select ShiftSchedule.ArchetypeFor(d, SpecialShift.None));
            CollectionAssert.AreEquivalent(ShiftSchedule.Archetypes, storyKinds, "Every kind of shift is played in the story");
        }

        [Test]
        public void FromTheSecondWeek_EverySevenDays_MixAtLeastThreeKindsOfShift()
        {
            for (int day = 8; day <= 60; day++)
            {
                Assert.Greater((from d in Enumerable.Range(day, 7)
                    select ShiftSchedule.ArchetypeFor(d, SpecialShift.None)).Distinct().Count(), 2, $"The week from day {day} has fewer than three kinds of shift");
            }
        }

        [Test]
        public void SpecialNights_FallOnTheirKindOfShift_AndTheInspectionOnAPlainOne()
        {
            Assert.AreEqual("evening_rush", ShiftSchedule.ArchetypeFor(20, SpecialShift.Wedding));
            Assert.AreEqual("evening_rush", ShiftSchedule.ArchetypeFor(20, SpecialShift.StPatricks));
            Assert.AreEqual("lunch_rush", ShiftSchedule.ArchetypeFor(20, SpecialShift.LasagneTray));
            Assert.AreEqual("normal", ShiftSchedule.ArchetypeFor(20, SpecialShift.Inspection));
        }

        [Test]
        public void ThePace_TightensWithTheStory_AndStopsAtItsEnd()
        {
            Assert.AreEqual(1.0, ShiftSchedule.PaceFor(1), 9.999999747378752E-06);
            Assert.AreEqual(1.0, ShiftSchedule.PaceFor(7), 9.999999747378752E-06, "The first week keeps the first pace");
            Assert.AreEqual(0.8500000238418579, ShiftSchedule.PaceFor(35), 9.999999747378752E-06);
            Assert.AreEqual(0.8500000238418579, ShiftSchedule.PaceFor(200), 9.999999747378752E-06, "It never gets faster after the story");
            for (int day = 2; day <= 60; day++)
            {
                Assert.LessOrEqual(ShiftSchedule.PaceFor(day), ShiftSchedule.PaceFor(day - 1) + 1E-06f, $"Day {day} is slower than the day before");
            }
        }

        [Test]
        public void ThePressure_RisesFromChapterToChapter()
        {
            ShiftCatalog catalog = Catalog();
            float firstWeek = Average(1, 7);
            float chaptersTwoThree = Average(8, 18);
            float chaptersFourFive = Average(19, 30);
            float finale = Average(31, 35);
            TestContext.WriteLine($"Dishes a shift brings on average: days 1-7 {firstWeek:0.0}, 8-18 {chaptersTwoThree:0.0}, " + $"19-30 {chaptersFourFive:0.0}, 31-35 {finale:0.0}");
            Assert.Less(firstWeek, chaptersTwoThree);
            Assert.Less(chaptersTwoThree, chaptersFourFive);
            Assert.Less(chaptersFourFive, finale);
            float Average(int from, int to)
            {
                return (float)(from d in Enumerable.Range(@from, to - @from + 1)
                    select Arrivals(catalog.Find(ShiftSchedule.ArchetypeFor(d, SpecialShift.None)), ShiftSchedule.PaceFor(d))).Average();
            }
        }

        [Test]
        public void EveryKindOfShift_IsInTheCatalog_DealtShuffled_WithQuotasABeginnerCanReachAndAProCanBeat()
        {
            ShiftCatalog catalog = Catalog();
            string[] archetypes = ShiftSchedule.Archetypes;
            foreach (string id in archetypes)
            {
                ShiftDefinition shift = catalog.Find(id);
                Assert.IsNotNull(shift, "No '" + id + "' in the catalog");
                Assert.IsTrue(shift.ShuffleQueue, id + " is dealt in a fixed order");
                Assert.IsTrue(shift.RepeatQueue, id + " runs out of dishes");
                Assert.Greater(shift.Waves.Count, 0, id + " has one even pace");
                Assert.GreaterOrEqual(shift.MinCyclesBetweenBreakdowns, 2, id + " lets breakdowns come back to back");
                foreach (ServiceWave wave in shift.Waves)
                {
                    Assert.That(wave.start01, Is.InRange(0f, 1f));
                    Assert.That(wave.end01, Is.InRange(wave.start01, 1f));
                }
                float[] array = new float[2] { 1f, 0.85f };
                foreach (float pace in array)
                {
                    int arrivals = Arrivals(shift, pace);
                    int quota = ShiftSchedule.QuotaFor(shift.Quota, pace, 1f);
                    TestContext.WriteLine($"{id} at pace {pace:0.00}: {arrivals} dishes, quota {quota}");
                    Assert.That((float)quota / (float)arrivals, Is.InRange(0.3f, 0.75f), $"{id} at pace {pace}: quota {quota} of {arrivals} dishes");
                    Assert.LessOrEqual(quota, 12, "More than a practised porter washes in a shift");
                }
            }
            Assert.AreEqual(0f, catalog.Find("calm_start").UrgentOrderChance, "Urgent orders while Kuba is still learning");
            Assert.Greater(catalog.Find("hard_night").UrgentOrderChance, catalog.Find("normal").UrgentOrderChance);
            Assert.AreEqual(3, catalog.Find("calm_start").FixedOpening.Count, "The first day opens with the dishes the hints teach on");
        }

        [Test]
        public void TheBusyNights_FindTheirDishes_InTheKindOfShiftTheyFallOn()
        {
            ShiftCatalog catalog = Catalog();
            SpecialShift[] array = new SpecialShift[2]
            {
                SpecialShift.Wedding,
                SpecialShift.StPatricks
            };
            foreach (SpecialShift special in array)
            {
                ShiftDefinition shift = catalog.Find(ShiftSchedule.ArchetypeFor(20, special));
                HashSet<string> ids = new HashSet<string>(shift.DishSpawnQueue.Select((DishDefinition d) => d.Id));
                string[] array2 = SpecialShifts.Queue(special);
                foreach (string id in array2)
                {
                    Assert.IsTrue(ids.Contains(id), $"{special} needs a {id}, which {shift.Id} never brings");
                }
            }
        }

        [Test]
        public void TheDishwashersFirstBreakdown_IsScripted_ThenOnlyByChance_AndRarelyTwoDaysRunning()
        {
            ScheduledShift firstDay = ShiftSchedule.For(1, SpecialShift.None, tutorialBreakdownDone: false, lastShiftBrokeDown: false);
            Assert.IsFalse(firstDay.TutorialBreakdown, "The first day is for the sink");
            Assert.AreEqual(0f, firstDay.BreakdownChanceScale);
            ScheduledShift waiting = ShiftSchedule.For(5, SpecialShift.None, tutorialBreakdownDone: false, lastShiftBrokeDown: false);
            Assert.IsTrue(waiting.TutorialBreakdown, "Until Kuba has met a breakdown, the next shift scripts one");
            Assert.AreEqual(0f, waiting.BreakdownChanceScale, "Nothing by chance before the scripted one");
            ScheduledShift dayAfter = ShiftSchedule.For(6, SpecialShift.None, tutorialBreakdownDone: true, lastShiftBrokeDown: true);
            Assert.IsFalse(dayAfter.TutorialBreakdown);
            Assert.AreEqual(0.3499999940395355, dayAfter.BreakdownChanceScale, 9.999999747378752E-06);
            Assert.AreEqual(1.0, ShiftSchedule.For(7, SpecialShift.None, tutorialBreakdownDone: true, lastShiftBrokeDown: false).BreakdownChanceScale, 9.999999747378752E-06);
        }

        [Test]
        public void EachDay_IsDealtItsOwnWay_AndTheSameDayTheSameWay()
        {
            HashSet<int> seeds = new HashSet<int>(Enumerable.Range(1, 60).Select(ShiftSchedule.SeedFor));
            Assert.AreEqual(60, seeds.Count, "Two days are dealt alike");
            Assert.AreEqual(ShiftSchedule.For(9, SpecialShift.None, tutorialBreakdownDone: true, lastShiftBrokeDown: false).Seed, ShiftSchedule.For(9, SpecialShift.None, tutorialBreakdownDone: true, lastShiftBrokeDown: false).Seed);
        }

        [Test]
        public void TheQuota_GrowsWithAFasterNight_ButNotPastWhatAProCanWash()
        {
            Assert.AreEqual(8, ShiftSchedule.QuotaFor(8, 1f, 1f));
            Assert.AreEqual(9, ShiftSchedule.QuotaFor(8, 0.85f, 1f));
            Assert.AreEqual(12, ShiftSchedule.QuotaFor(9, 1f, 0.6f), "St. Patrick's caps out");
        }

        [Test]
        public void TheLedger_RemembersTheMachinesBreakdowns_ForTheNextDaysPlan()
        {
            LifeLedger ledger = new LifeLedger(LifeRules.Default, null);
            Assert.IsTrue(ledger.PlanShift(SpecialShift.None).ArchetypeId == "calm_start");
            Assert.IsFalse(ledger.PlanShift(SpecialShift.None).TutorialBreakdown, "Day one has no breakdown");
            ledger.CompleteShift(Shift(0));
            Assert.IsFalse(ledger.PlanShift(SpecialShift.None).TutorialBreakdown, "Day two is the glass's and the chef's, not the machine's");
            ledger.CompleteShift(Shift(0));
            Assert.AreEqual(MechanicUnlocks.DayOf(Mechanic.Breakdowns), ledger.DayNumber);
            Assert.IsTrue(ledger.PlanShift(SpecialShift.None).TutorialBreakdown, "Day three scripts the first breakdown");
            ledger.CompleteShift(Shift(1));
            Assert.IsTrue(ledger.BreakdownTutorialDone);
            Assert.IsTrue(ledger.LastShiftBrokeDown);
            ScheduledShift day4 = ledger.PlanShift(SpecialShift.None);
            Assert.IsFalse(day4.TutorialBreakdown);
            Assert.AreEqual(0.3499999940395355, day4.BreakdownChanceScale, 9.999999747378752E-06);
            ledger.CompleteShift(Shift(0));
            Assert.AreEqual(1.0, ledger.PlanShift(SpecialShift.None).BreakdownChanceScale, 9.999999747378752E-06);
            SaveData data = new SaveData();
            ledger.WriteTo(data);
            LifeLedger reloaded = new LifeLedger(LifeRules.Default, null);
            reloaded.LoadFrom(data);
            Assert.IsTrue(reloaded.BreakdownTutorialDone, "The first breakdown came back after a reload");
            Assert.AreEqual(ledger.PlanShift(SpecialShift.None).Seed, reloaded.PlanShift(SpecialShift.None).Seed);
        }

        private static ShiftResult Shift(int breakdowns)
        {
            return new ShiftResult
            {
                PlannedSeconds = 180f,
                ElapsedSeconds = 180f,
                Wage = 40f,
                Breakdowns = breakdowns
            };
        }
    }
}
