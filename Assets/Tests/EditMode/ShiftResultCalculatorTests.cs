// Reconstructed on 2026-11-17 by decompiling Library/ScriptAssemblies/Zmywak.Tests.EditMode.dll (built 2026-10-11):
// the original source was lost from disk. Behaviour matches that build; original comments are gone.
using System;
using System.Collections.Generic;
using System.Globalization;
using NUnit.Framework;
using Zmywak.Core.Data;
using Zmywak.Kitchen.Dishes;
using Zmywak.Kitchen.Shifts;
using Zmywak.Meta.Economy;

namespace Zmywak.Tests
{
    public class ShiftResultCalculatorTests
    {
        private const float Shift = 180f;

        [Test]
        public void AllDone_NoneMissed()
        {
            ShiftResult result = ShiftResultCalculator.Calculate(new List<WashStage>
            {
                WashStage.Done,
                WashStage.Done
            }, 2, 0, 100f, 180f);
            Assert.AreEqual(2, result.DishesCleaned);
            Assert.AreEqual(0, result.DishesMissed);
        }

        [Test]
        public void SomeUnfinished_CountsAsMissed()
        {
            ShiftResult result = ShiftResultCalculator.Calculate(new List<WashStage>
            {
                WashStage.Done,
                WashStage.AwaitingScrub,
                WashStage.AwaitingRinse
            }, 1, 0, 180f, 180f);
            Assert.AreEqual(1, result.DishesCleaned);
            Assert.AreEqual(2, result.DishesMissed);
        }

        [Test]
        public void TheHourlyRate_IsTheIrishMinimumWageOfTheDay()
        {
            Assert.AreEqual(7.0, ShiftResultCalculator.HourlyWageOn(new DateTime(2004, 5, 1)), 9.999999747378752E-05);
            Assert.AreEqual(7.0, ShiftResultCalculator.HourlyWageOn(new DateTime(2005, 4, 30)), 9.999999747378752E-05);
            Assert.AreEqual(7.650000095367432, ShiftResultCalculator.HourlyWageOn(new DateTime(2005, 5, 1)), 9.999999747378752E-05);
            Assert.AreEqual(7.650000095367432, ShiftResultCalculator.HourlyWageOn(new DateTime(2006, 5, 1)), 9.999999747378752E-05);
            Assert.AreEqual(8.300000190734863, ShiftResultCalculator.HourlyWageOn(new DateTime(2007, 3, 1)), 9.999999747378752E-05);
            Assert.AreEqual(8.649999618530273, ShiftResultCalculator.HourlyWageOn(new DateTime(2008, 9, 1)), 9.999999747378752E-05);
        }

        [Test]
        public void AFullShift_PaysSixHoursAtTheRateGiven()
        {
            ShiftResult in2004 = ShiftResultCalculator.Calculate(new List<WashStage>(), 0, 0, 180f, 180f);
            ShiftResult in2006 = ShiftResultCalculator.Calculate(new List<WashStage>(), 0, 0, 180f, 180f, 0, 7.65f);
            Assert.AreEqual(42.0, in2004.BaseWage, 0.0010000000474974513);
            Assert.AreEqual(45.900001525878906, in2006.BaseWage, 0.0010000000474974513);
        }

        [Test]
        public void ElapsedAndPlannedSecondsPassedThrough()
        {
            ShiftResult result = ShiftResultCalculator.Calculate(new List<WashStage>(), 0, 0, 42.5f, 180f);
            Assert.AreEqual(42.5f, result.ElapsedSeconds);
            Assert.AreEqual(180f, result.PlannedSeconds);
        }

        [Test]
        public void AFullShift_PaysSixHoursAtTheMinimumWage()
        {
            ShiftResult result = ShiftResultCalculator.Calculate(new List<WashStage>(), 0, 0, 180f, 180f);
            Assert.AreEqual(42.0, result.BaseWage, 9.999999747378752E-05, "6 h x 7.00 EUR");
            Assert.AreEqual(42.0, result.Wage, 9.999999747378752E-05);
        }

        [Test]
        public void ClockingOutEarly_PaysTheHoursWorked()
        {
            ShiftResult half = ShiftResultCalculator.Calculate(new List<WashStage>(), 0, 0, 90f, 180f);
            ShiftResult overtime = ShiftResultCalculator.Calculate(new List<WashStage>(), 0, 0, 360f, 180f);
            Assert.AreEqual(21.0, half.BaseWage, 9.999999747378752E-05);
            Assert.AreEqual(42.0, overtime.BaseWage, 9.999999747378752E-05, "The clock stops at the shift's end");
        }

        [Test]
        public void CleanDishesAndFullRacks_EarnTips()
        {
            ShiftResult result = ShiftResultCalculator.Calculate(new List<WashStage>(), 10, 0, 180f, 180f, 2);
            Assert.AreEqual(4.5, result.Tips, 9.999999747378752E-05);
            Assert.AreEqual(result.BaseWage + result.Tips, result.Wage, 9.999999747378752E-05);
            Assert.AreEqual(2, result.FullRackLoads);
        }

        [Test]
        public void BrokenDishes_AreDocked()
        {
            ShiftResult shiftResult = ShiftResultCalculator.Calculate(new List<WashStage>(), 10, 0, 180f, 180f);
            ShiftResult withBreakage = ShiftResultCalculator.Calculate(new List<WashStage>(), 10, 3, 180f, 180f);
            Assert.AreEqual(9.0, withBreakage.Deductions, 9.999999747378752E-05);
            Assert.AreEqual(shiftResult.Wage - withBreakage.Deductions, withBreakage.Wage, 9.999999747378752E-05);
        }

        [Test]
        public void ADisastrousShift_CostsUnderAThirdOfTheBase_NotTheWholeDay()
        {
            ShiftResult result = ShiftResultCalculator.Calculate(new List<WashStage>(), 0, 30, 180f, 180f);
            Assert.Less(0.3f, 1f / 3f);
            Assert.AreEqual(0.3f * result.BaseWage, result.Deductions, 9.999999747378752E-05, "Breakages are docked, but never more than the cap");
            Assert.AreEqual(result.BaseWage * 0.7f, result.Wage, 9.999999747378752E-05);
            ShiftResult inspection = ShiftResultCalculator.Calculate(new List<WashStage>(), 0, 30, 180f, 180f, 0, 7f, Place.Dublin, SpecialShift.Inspection);
            Assert.AreEqual(result.Deductions, inspection.Deductions, 9.999999747378752E-05, "Double breakage at an inspection hits the same cap");
        }

        [Test]
        public void DoingNothing_PaysHalfTheBase_AndFarLessThanAGreatShift()
        {
            ShiftResult idle = ShiftResultCalculator.Calculate(new List<WashStage>(), 0, 0, 180f, 180f, 0, 7f, Place.Dublin, SpecialShift.None, 12, ShiftGrade.D);
            ShiftResult quarter = ShiftResultCalculator.Calculate(new List<WashStage>(), 3, 0, 180f, 180f, 0, 7f, Place.Dublin, SpecialShift.None, 12, ShiftGrade.D);
            ShiftResult great = ShiftResultCalculator.Calculate(new List<WashStage>(), 15, 0, 180f, 180f, 2, 7f, Place.Dublin, SpecialShift.None, 12, ShiftGrade.S);
            Assert.AreEqual(ShiftResultCalculator.FullShiftBaseWage * 0.5f, idle.IdleDocked, 0.0010000000474974513, "Nothing washed should dock half the base");
            Assert.AreEqual(ShiftResultCalculator.FullShiftBaseWage * 0.5f, idle.Wage, 0.0010000000474974513);
            Assert.AreEqual(0.0, quarter.IdleDocked, 9.999999747378752E-05, "A quarter of the quota washed is paid in full");
            Assert.Greater(great.Wage, 2.5f * idle.Wage, $"A great shift ({great.Wage:0.00}) should pay far more than doing nothing ({idle.Wage:0.00})");
            Assert.AreEqual(great.BaseWage - great.IdleDocked + great.QuotaBonus + great.GradeBonus + great.Tips - great.Deductions, great.Wage, 0.0010000000474974513, "The pay is not the sum of its lines");
            ShiftResult idleAndClumsy = ShiftResultCalculator.Calculate(new List<WashStage>(), 0, 30, 180f, 180f, 0, 7f, Place.Dublin, SpecialShift.None, 12, ShiftGrade.D);
            Assert.AreEqual(ShiftResultCalculator.FullShiftBaseWage * 0.5f * 0.7f, idleAndClumsy.Wage, 0.0010000000474974513);
        }

        [Test]
        public void MeetingTheQuota_PaysAnHour_AndAGreatShiftUpToAnotherOne()
        {
            ShiftResult under = ShiftResultCalculator.Calculate(new List<WashStage>(), 7, 0, 180f, 180f, 0, 7f, Place.Dublin, SpecialShift.None, 8, ShiftGrade.C);
            ShiftResult met = ShiftResultCalculator.Calculate(new List<WashStage>(), 8, 0, 180f, 180f, 0, 7f, Place.Dublin, SpecialShift.None, 8, ShiftGrade.B);
            ShiftResult greatA = ShiftResultCalculator.Calculate(new List<WashStage>(), 10, 0, 180f, 180f, 0, 7f, Place.Dublin, SpecialShift.None, 8, ShiftGrade.A);
            ShiftResult greatS = ShiftResultCalculator.Calculate(new List<WashStage>(), 12, 0, 180f, 180f, 0, 7f, Place.Dublin, SpecialShift.None, 8, ShiftGrade.S);
            Assert.AreEqual(0f, under.QuotaBonus, "One short of the quota earns nothing for it");
            Assert.AreEqual(0f, under.GradeBonus + met.GradeBonus, "B and below earn nothing extra");
            Assert.AreEqual(7.0, met.QuotaBonus, 9.999999747378752E-05);
            Assert.AreEqual(3.5, greatA.GradeBonus, 9.999999747378752E-05);
            Assert.AreEqual(7.0, greatS.GradeBonus, 9.999999747378752E-05);
            Assert.AreEqual(greatS.BaseWage + greatS.QuotaBonus + greatS.GradeBonus + greatS.Tips, greatS.Wage, 9.999999747378752E-05);
            Assert.AreEqual(under.BaseWage, greatS.BaseWage, 9.999999747378752E-05);
            Assert.Greater(greatS.Wage / under.Wage, 1.3f, "A masterclass pays clearly more than a near miss");
        }

        [Test]
        public void TheBonuses_FollowTheHourlyRate_AndTheShareOfTheShiftWorked()
        {
            ShiftResult inKrakow = ShiftResultCalculator.Calculate(new List<WashStage>(), 8, 0, 180f, 180f, 0, 18f, Place.Krakow, SpecialShift.None, 8, ShiftGrade.S);
            Assert.AreEqual(18.0, inKrakow.QuotaBonus, 9.999999747378752E-05, "An hour of a Kraków wage, in złoty");
            Assert.AreEqual(18.0, inKrakow.GradeBonus, 9.999999747378752E-05);
            ShiftResult halfShift = ShiftResultCalculator.Calculate(new List<WashStage>(), 4, 0, 90f, 180f, 0, 7f, Place.Dublin, SpecialShift.None, 8, ShiftGrade.S);
            Assert.AreEqual(4, halfShift.Quota, "Half the shift asks for half the quota");
            Assert.AreEqual(3.5, halfShift.QuotaBonus, 9.999999747378752E-05, "...and pays half the hour for it");
            Assert.AreEqual(3.5, halfShift.GradeBonus, 9.999999747378752E-05);
        }

        [Test]
        public void AfterTheEpilogue_TheKitchenPaysInTheMoneyOfThePlace()
        {
            DateTime bailout = new DateTime(2010, 11, 29);
            Assert.AreEqual(8.649999618530273, ShiftResultCalculator.HourlyWageOn(bailout, Place.Dublin), 9.999999747378752E-05, "Dublin keeps the Irish minimum wage");
            Assert.AreEqual(18f, ShiftResultCalculator.HourlyWageOn(bailout, Place.Krakow));
            Assert.AreEqual(32f, ShiftResultCalculator.HourlyWageOn(bailout, Place.Pilbara));
            ShiftResult krakow = ShiftResultCalculator.Calculate(new List<WashStage>(), 10, 1, 180f, 180f, 2, 18f, Place.Krakow);
            Assert.AreEqual(Place.Krakow, krakow.Place);
            Assert.AreEqual(108.0, krakow.BaseWage, 0.0010000000474974513);
            float small = PlaceMoney.SmallSums(Place.Krakow);
            Assert.AreEqual(4.5f * small, krakow.Tips, 0.0010000000474974513, "Tips in złoty");
            Assert.AreEqual(3f * small, krakow.Deductions, 0.0010000000474974513, "A breakage docked in złoty");
            StringAssert.EndsWith("zł", PlaceMoney.Format(krakow.Wage, krakow.Place));
        }

        [Test]
        public void Money_IsWrittenThePolishWay_OnAnEnglishPhone()
        {
            CultureInfo previous = CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
                Assert.AreEqual("12,50 EUR", PlaceMoney.Format(12.5f, Place.Dublin));
                Assert.AreEqual("1234,00 zł", PlaceMoney.Format(1234f, Place.Krakow), "No thousands separator, as before");
            }
            finally
            {
                CultureInfo.CurrentCulture = previous;
            }
        }

        [Test]
        public void AfterTheBailout_WagesAndRentsMoveOn()
        {
            Assert.AreEqual(7.650000095367432, ShiftResultCalculator.HourlyWageOn(new DateTime(2011, 3, 1)), 9.999999747378752E-05, "February 2011: the minimum wage cut");
            Assert.AreEqual(8.649999618530273, ShiftResultCalculator.HourlyWageOn(new DateTime(2011, 7, 1)), 9.999999747378752E-05, "...restored in July");
            Assert.AreEqual(9.149999618530273, ShiftResultCalculator.HourlyWageOn(new DateTime(2016, 1, 1)), 9.999999747378752E-05);
            Assert.Greater(ShiftResultCalculator.HourlyWageOn(new DateTime(2012, 6, 1), Place.Krakow), ShiftResultCalculator.HourlyWageOn(new DateTime(2010, 12, 1), Place.Krakow), "Polish wages rise");
            Assert.Greater(ShiftResultCalculator.HourlyWageOn(new DateTime(2012, 6, 1), Place.Pilbara), ShiftResultCalculator.HourlyWageOn(new DateTime(2015, 6, 1), Place.Pilbara), "The mining boom peaked in 2012");
            LifeRules dublin = LifeRules.Default;
            Assert.Less(dublin.WeeklyRentIn(2011), dublin.WeeklyRentIn(2010), "Rents still falling after the crash");
            Assert.Greater(dublin.WeeklyRentIn(2015), dublin.WeeklyRentIn(2008), "...and past the boom's by 2015");
            Assert.Greater(LifeRules.Krakow.WeeklyRentIn(2012), LifeRules.Krakow.WeeklyRentIn(2010));
        }
    }
}
