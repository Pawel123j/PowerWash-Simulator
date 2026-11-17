// Reconstructed on 2026-11-17 by decompiling Library/ScriptAssemblies/Zmywak.Tests.EditMode.dll (built 2026-10-11):
// the original source was lost from disk. Behaviour matches that build; original comments are gone.
using System;
using System.Linq;
using NUnit.Framework;
using Zmywak.Core.Data;

namespace Zmywak.Tests
{
    public class MechanicUnlocksTests
    {
        private static Mechanic[] All => (Mechanic[])Enum.GetValues(typeof(Mechanic));

        [Test]
        public void DayOne_IsTheSinkItself_OnOnePlateFirst()
        {
            Assert.IsTrue(MechanicUnlocks.IsUnlocked(Mechanic.GuidedFirstDish, 1));
            Assert.IsTrue(MechanicUnlocks.IsUnlocked(Mechanic.BasicTools, 1));
            Mechanic[] array = new Mechanic[10]
            {
                Mechanic.Glasses,
                Mechanic.ChefOrders,
                Mechanic.Soaking,
                Mechanic.Breakdowns,
                Mechanic.Pub,
                Mechanic.Scraper,
                Mechanic.UrgentOrders,
                Mechanic.Shower,
                Mechanic.BigDishwasher,
                Mechanic.SendMoney
            };
            foreach (Mechanic later in array)
            {
                Assert.IsFalse(MechanicUnlocks.IsUnlocked(later, 1), $"{later} on the very first day");
            }
        }

        [Test]
        public void EachDay_BringsInAFewThingsAtMost_AndTheFirstWeekBringsThemAll()
        {
            for (int day = 2; day <= 10; day++)
            {
                Assert.LessOrEqual(Enumerable.Count(MechanicUnlocks.NewOn(day), (Mechanic m) => m != Mechanic.SendMoney && m != Mechanic.Pub), 3, $"Day {day} brings too much at the sink at once");
            }
            Mechanic[] all = All;
            foreach (Mechanic mechanic in all)
            {
                Assert.LessOrEqual(MechanicUnlocks.DayOf(mechanic), 7, $"{mechanic} comes too late for the first week");
            }
        }

        [Test]
        public void TheGlass_ComesWithTheLight_TheBowlWithTheSoak_ThePotsWithTheBusyShifts()
        {
            Assert.AreEqual(MechanicUnlocks.DayOf(Mechanic.Glasses), FirstDay((int d) => MechanicUnlocks.IsDishUnlocked("glass", d)));
            Assert.AreEqual(MechanicUnlocks.DayOf(Mechanic.Soaking), FirstDay((int d) => MechanicUnlocks.IsDishUnlocked("bowl", d)));
            Assert.AreEqual(MechanicUnlocks.DayOf(Mechanic.HeavySteel), FirstDay((int d) => MechanicUnlocks.IsDishUnlocked("pot", d)));
            Assert.IsTrue(MechanicUnlocks.IsDishUnlocked("plate", 1) && MechanicUnlocks.IsDishUnlocked("pan", 1) && MechanicUnlocks.IsDishUnlocked("mug", 1), "The first day has no dishes to wash");
            Assert.AreEqual(MechanicUnlocks.DayOf(Mechanic.Scraper), FirstDay((int d) => MechanicUnlocks.IsToolUnlocked("scraper", d)));
            Assert.AreEqual(MechanicUnlocks.DayOf(Mechanic.Shower), FirstDay((int d) => MechanicUnlocks.IsToolUnlocked("shower", d)));
            Assert.IsTrue(MechanicUnlocks.IsToolUnlocked("sponge", 1) && MechanicUnlocks.IsToolUnlocked("steel_wool", 1) && MechanicUnlocks.IsToolUnlocked("soak", 1), "The first day's dishes need the sponge, the steel wool and the soap");
        }

        [Test]
        public void TheKitchenOnItsOwn_HasEverything()
        {
            Mechanic[] all = All;
            for (int i = 0; i < all.Length; i++)
            {
                Mechanic mechanic = all[i];
                Assert.IsTrue(MechanicUnlocks.IsUnlocked(mechanic, int.MaxValue), mechanic.ToString());
            }
        }

        [Test]
        public void TheFirstBreakdown_WaitsForItsDay()
        {
            Assert.AreEqual(MechanicUnlocks.DayOf(Mechanic.Breakdowns), ShiftSchedule.FirstTutorialBreakdownDay);
            Assert.IsFalse(ShiftSchedule.For(2, SpecialShift.None, tutorialBreakdownDone: false, lastShiftBrokeDown: false).TutorialBreakdown);
            Assert.IsTrue(ShiftSchedule.For(MechanicUnlocks.DayOf(Mechanic.Breakdowns), SpecialShift.None, tutorialBreakdownDone: false, lastShiftBrokeDown: false).TutorialBreakdown);
        }

        [Test]
        public void EveryDayWithNews_SaysWhatItIs()
        {
            for (int day = 2; day <= 7; day++)
            {
                foreach (Mechanic mechanic in MechanicUnlocks.NewOn(day))
                {
                    Assert.IsNotEmpty(MechanicUnlocks.Describe(mechanic), $"{mechanic} comes in on day {day} without a word");
                }
            }
        }

        private static int FirstDay(Func<int, bool> unlocked)
        {
            for (int day = 1; day < 30; day++)
            {
                if (unlocked(day))
                {
                    return day;
                }
            }
            return -1;
        }
    }
}
