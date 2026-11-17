using System;
using System.Collections.Generic;
using NUnit.Framework;
using Zmywak.Core.Data;
using Zmywak.Kitchen.Dishes;
using Zmywak.Kitchen.Shifts;

namespace Zmywak.Tests
{
    public class SpecialShiftTests
    {
        /// <summary>The date of a day of the story: chapters 1-3 from 1 May 2004, chapter 4 from 1 May 2006 (day 19),
        /// chapter 5 from 29 September 2008 (day 25), the epilogue from 29 November 2010 (day 31).</summary>
        private static DateTime StoryDate(int day)
        {
            if (day >= 31)
            {
                return new DateTime(2010, 11, 29).AddDays(day - 31);
            }

            if (day >= 25)
            {
                return new DateTime(2008, 9, 29).AddDays(day - 25);
            }

            return day >= 19 ? new DateTime(2006, 5, 1).AddDays(day - 19) : new DateTime(2004, 5, 1).AddDays(day - 1);
        }

        [Test]
        public void TheFirstWeek_IsKubasToLearnTheSinkInPeace()
        {
            for (int day = 1; day <= SpecialShifts.CalmFirstDays; day++)
            {
                Assert.AreEqual(SpecialShift.None, SpecialShifts.On(StoryDate(day), day, Place.Dublin), $"Day {day}");
            }
        }

        [Test]
        public void Saturdays_AreWeddings_AndSundays_TheLasagneTray()
        {
            Assert.AreEqual(DayOfWeek.Saturday, StoryDate(8).DayOfWeek);
            Assert.AreEqual(SpecialShift.Wedding, SpecialShifts.On(StoryDate(8), 8, Place.Dublin));
            Assert.AreEqual(SpecialShift.LasagneTray, SpecialShifts.On(StoryDate(9), 9, Place.Dublin));
            Assert.AreEqual(SpecialShift.Wedding, SpecialShifts.On(new DateTime(2010, 12, 4), 36, Place.Krakow), "A Kraków hotel has weddings too");
            Assert.AreEqual(SpecialShift.None, SpecialShifts.On(new DateTime(2010, 12, 4), 36, Place.Pilbara), "No weddings in a mining camp");
        }

        [Test]
        public void TheInspection_IsUnannounced_EveryNinthDay_OnAWeekday()
        {
            Assert.AreEqual(SpecialShift.Inspection, SpecialShifts.On(StoryDate(14), 14, Place.Dublin));
            Assert.AreEqual(SpecialShift.Inspection, SpecialShifts.On(StoryDate(23), 23, Place.Dublin));
            Assert.AreEqual(SpecialShift.None, SpecialShifts.On(StoryDate(13), 13, Place.Dublin));
            for (int day = 1; day <= 120; day++)
            {
                DateTime date = new DateTime(2010, 11, 29).AddDays(day);
                if (SpecialShifts.On(date, day + 35, Place.Dublin) == SpecialShift.Inspection)
                {
                    Assert.AreNotEqual(DayOfWeek.Saturday, date.DayOfWeek);
                    Assert.AreNotEqual(DayOfWeek.Sunday, date.DayOfWeek);
                }
            }
        }

        [Test]
        public void StPatricksDay_IsDublins()
        {
            var paddysDay = new DateTime(2011, 3, 17);
            Assert.AreEqual(SpecialShift.StPatricks, SpecialShifts.On(paddysDay, 143, Place.Dublin));
            Assert.AreNotEqual(SpecialShift.StPatricks, SpecialShifts.On(paddysDay, 143, Place.Krakow));
            Assert.AreNotEqual(SpecialShift.StPatricks, SpecialShifts.On(paddysDay, 143, Place.Pilbara));
        }

        [Test]
        public void TheStorysWeeks_MeetAWedding_AnInspection_AndTheTray()
        {
            var met = new HashSet<SpecialShift>();
            for (int day = 1; day <= 35; day++)
            {
                met.Add(SpecialShifts.On(StoryDate(day), day, Place.Dublin));
            }

            CollectionAssert.IsSubsetOf(new[] { SpecialShift.Wedding, SpecialShift.Inspection, SpecialShift.LasagneTray }, met);
        }

        [Test]
        public void ABusyNight_IsFaster_AndPaysBetter()
        {
            SpecialShiftRules wedding = SpecialShifts.Rules(SpecialShift.Wedding);
            SpecialShiftRules paddys = SpecialShifts.Rules(SpecialShift.StPatricks);
            Assert.Less(wedding.PaceScale, 1f);
            Assert.Greater(wedding.ExtraOpeningDishes, 0);
            Assert.Less(paddys.PaceScale, wedding.PaceScale, "St. Patrick's is the busiest night of the year");
            Assert.AreEqual(2f, paddys.TipsScale);
            CollectionAssert.Contains(SpecialShifts.Queue(SpecialShift.StPatricks), "glass");
            Assert.IsNull(SpecialShifts.Queue(SpecialShift.Inspection), "An inspection is an ordinary night, watched");
            Assert.AreEqual(1f, SpecialShifts.Rules(SpecialShift.None).PaceScale);
        }

        [Test]
        public void TheBonus_IsTheEnvelope_TheThanks_OrTheWhipRound()
        {
            Assert.AreEqual(15f, SpecialShifts.Bonus(SpecialShift.Wedding, dishesBroken: 3, pileCollapsed: true, trayScrubbed: false), "The newlyweds pay anyway");
            Assert.AreEqual(10f, SpecialShifts.Bonus(SpecialShift.Inspection, 0, false, false));
            Assert.AreEqual(0f, SpecialShifts.Bonus(SpecialShift.Inspection, 1, false, false), "A breakage fails the inspection");
            Assert.AreEqual(0f, SpecialShifts.Bonus(SpecialShift.Inspection, 0, true, false), "So does the pile coming down");
            Assert.AreEqual(10f, SpecialShifts.Bonus(SpecialShift.LasagneTray, 0, false, trayScrubbed: true));
            Assert.AreEqual(0f, SpecialShifts.Bonus(SpecialShift.LasagneTray, 0, false, trayScrubbed: false));
            Assert.AreEqual(0f, SpecialShifts.Bonus(SpecialShift.None, 0, false, true));
        }

        [Test]
        public void TheTips_AndTheBreakages_FollowTheNight()
        {
            const float shift = 180f;
            ShiftResult plain = ShiftResultCalculator.Calculate(new List<WashStage>(), 10, 2, shift, shift, 1);
            ShiftResult wedding = ShiftResultCalculator.Calculate(new List<WashStage>(), 10, 2, shift, shift, 1, special: SpecialShift.Wedding);
            ShiftResult inspection = ShiftResultCalculator.Calculate(new List<WashStage>(), 10, 2, shift, shift, 1, special: SpecialShift.Inspection);

            Assert.AreEqual(plain.Tips * 1.5f, wedding.Tips, 1e-4f);
            Assert.AreEqual(SpecialShift.Wedding, wedding.Special);
            Assert.AreEqual(plain.Deductions * 2f, inspection.Deductions, 1e-4f, "A breakage in front of the inspector costs double");
        }
    }
}
