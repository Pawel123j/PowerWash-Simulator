using System;
using System.Collections.Generic;
using UnityEngine;
using Zmywak.Core.Data;
using Zmywak.Kitchen.Dishes;

namespace Zmywak.Kitchen.Shifts
{
    /// <summary>
    /// Turns the final wash stage of every spawned dish into a <see cref="ShiftResult"/>. Pure and testable.
    /// The pay is an hourly wage for the part of the shift worked (the three minutes on screen stand for a
    /// six-hour evening) and on top of it what the night earned: an hour's pay from the boss for meeting the quota, half
    /// an hour or an hour for a great shift (A, S), a cut of the floor's tips for clean dishes and full racks. A night
    /// stood at the sink with next to nothing washed (under a quarter of the quota) is not paid in full: the boss docks
    /// up to half the base - nothing washed, half the pay - so doing nothing never earns close to doing it well.
    /// Breakages are docked, but never more than 30% of what is left of the base: one bad night costs, it does not wipe
    /// out the day. The economy simulator test (EconomySimulationTests) lives weeks on
    /// these numbers.
    /// </summary>
    public static class ShiftResultCalculator
    {
        public const float HourlyWageEuros = 7f; // Irish minimum wage from February 2004 (see HourlyWageOn for later years)
        public const float ShiftHours = 6f;
        // Tips and breakages, in Dublin euros; elsewhere as many of the place's small sums (PlaceMoney.SmallSums).
        public const float TipPerDishCleaned = 0.25f;
        public const float TipPerFullRack = 1f;
        public const float DeductionPerDishBroken = 3f;

        /// <summary>Meeting the quota is worth this many hours of the day's wage.</summary>
        public const float QuotaBonusHours = 1f;

        /// <summary>A great shift is worth this many hours of the day's wage: S, A (B and below: nothing extra).</summary>
        public const float GradeSBonusHours = 1f;
        public const float GradeABonusHours = 0.5f;

        /// <summary>Breakages are docked up to this share of the shift's base wage, no more (two breakages in front of the
        /// inspector, at double cost, still cost what they say).</summary>
        public const float MaxDeductionShare = 0.3f;

        /// <summary>Below this share of the quota the boss docks the base (<see cref="IdleDockShare"/>).</summary>
        public const float IdleQuotaShare = 0.25f;

        /// <summary>The most the boss docks for a night with nothing washed: this share of the base.</summary>
        public const float MaxIdleDockShare = 0.5f;

        /// <summary>
        /// The share of the base docked for a night with too little washed: <see cref="MaxIdleDockShare"/> with nothing
        /// washed, shrinking to none at <see cref="IdleQuotaShare"/> of the quota. No quota, nothing docked.
        /// </summary>
        public static float IdleDockShare(int dishesCleaned, int quota)
        {
            if (quota <= 0)
            {
                return 0f;
            }

            float done = Mathf.Clamp01(dishesCleaned / (quota * IdleQuotaShare));
            return MaxIdleDockShare * (1f - done);
        }

        public static float GradeBonusHours(ShiftGrade grade) =>
            grade == ShiftGrade.S ? GradeSBonusHours : grade == ShiftGrade.A ? GradeABonusHours : 0f;

        public static float FullShiftBaseWage => HourlyWageEuros * ShiftHours;

        /// <summary>
        /// The Irish national minimum wage on a date - what the kitchen pays Kuba the day he is in: 7.00 EUR an hour from
        /// February 2004, 7.65 from May 2005, 8.30 from January 2007, 8.65 from July 2007; cut back to 7.65 from February
        /// 2011 (the National Recovery Plan that came with the bailout) and restored that July; then 9.15 from 2016,
        /// 9.25 from 2017, 9.55 from 2018, 9.80 from 2019 and 10.10 from February 2020.
        /// </summary>
        public static float HourlyWageOn(DateTime date)
        {
            foreach ((DateTime from, float euros) in IrishMinimumWage)
            {
                if (date >= from)
                {
                    return euros;
                }
            }

            return HourlyWageEuros;
        }

        // Latest first: the rate from each date.
        private static readonly (DateTime from, float euros)[] IrishMinimumWage =
        {
            (new DateTime(2020, 2, 1), 10.1f),
            (new DateTime(2019, 1, 1), 9.8f),
            (new DateTime(2018, 1, 1), 9.55f),
            (new DateTime(2017, 1, 1), 9.25f),
            (new DateTime(2016, 1, 1), 9.15f),
            (new DateTime(2011, 7, 1), 8.65f),
            (new DateTime(2011, 2, 1), 7.65f),
            (new DateTime(2007, 7, 1), 8.65f),
            (new DateTime(2007, 1, 1), 8.3f),
            (new DateTime(2005, 5, 1), 7.65f)
        };

        /// <summary>A hotel kitchen by Kraków's market square, late 2010: about 3,000 zł a month gross, when the
        /// national minimum was 1,317 - rising a złoty a year with Polish wages, to 21 from 2013.</summary>
        public const float KrakowHourlyWage = 18f;

        /// <summary>A camp kitchen in the Pilbara in the mining boom, in Australian dollars: 32 in 2010, the boom's peak
        /// of 36 in 2012, easing back to 33 once the mines stopped hiring.</summary>
        public const float PilbaraHourlyWage = 32f;

        /// <summary>What the kitchen pays Kuba an hour where he lives, in that place's money: the Irish minimum wage of
        /// the day in Dublin (<see cref="HourlyWageOn(DateTime)"/>), a cook's rate of the year in Kraków or the Pilbara.</summary>
        public static float HourlyWageOn(DateTime date, Place place)
        {
            int year = date.Year;
            switch (place)
            {
                case Place.Krakow:
                    return year <= 2010 ? KrakowHourlyWage : year == 2011 ? 19f : year == 2012 ? 20f : 21f;
                case Place.Pilbara:
                    return year <= 2010 ? PilbaraHourlyWage : year == 2011 ? 34f : year == 2012 ? 36f : year == 2013 ? 35f : 33f;
                default:
                    return HourlyWageOn(date);
            }
        }

        /// <param name="plannedSeconds">How long the shift was meant to run; clocking out early pays that
        /// share of the hours. Zero or less pays the whole shift.</param>
        /// <param name="hourlyWage">The hourly rate of the day where the shift is worked (<see cref="HourlyWageOn(DateTime, Place)"/>);
        /// Dublin's in 2004 by default.</param>
        /// <param name="place">Where the shift is worked: the pay is in its money, and the tips and breakages are its small
        /// sums (<see cref="PlaceMoney.SmallSums"/>).</param>
        /// <param name="special">A special shift's tips and breakages (<see cref="SpecialShiftRules"/>); its bonus at
        /// the end is the kitchen's to add (<see cref="SpecialShifts.Bonus"/>).</param>
        /// <param name="quota">The clean dishes the whole shift asked for (0: none - nothing paid for it); clocking out early
        /// asks for, and pays, that share of it (<see cref="ShiftScoring.EffectiveQuota"/>).</param>
        /// <param name="grade">How the shift went (<see cref="ShiftScoring"/>): A and S are paid extra.</param>
        public static ShiftResult Calculate(IReadOnlyList<WashStage> finalStages, int dishesCleaned, int dishesBroken, float elapsedSeconds,
            float plannedSeconds, int fullRackLoads = 0, float hourlyWage = HourlyWageEuros, Place place = Place.Dublin,
            SpecialShift special = SpecialShift.None, int quota = 0, ShiftGrade grade = ShiftGrade.None)
        {
            int missed = 0;
            foreach (WashStage stage in finalStages)
            {
                if (stage != WashStage.Done)
                {
                    missed++;
                }
            }

            float shareWorked = plannedSeconds > 0f ? Mathf.Clamp01(elapsedSeconds / plannedSeconds) : 1f;
            float baseWage = hourlyWage * ShiftHours * shareWorked;
            float small = PlaceMoney.SmallSums(place);
            SpecialShiftRules rules = SpecialShifts.Rules(special);
            float tips = (dishesCleaned * TipPerDishCleaned + fullRackLoads * TipPerFullRack) * small * rules.TipsScale;

            // Paid for the share of the shift worked, as the base is: an early exit with the quota done earns no full hour.
            int quotaWorked = ShiftScoring.EffectiveQuota(quota, shareWorked);
            // Next to nothing washed: the boss docks the base (half of it for nothing at all).
            float idleDocked = IdleDockShare(dishesCleaned, quotaWorked) * baseWage;
            // Breakages docked no more than 30% of what is left: a disastrous night costs, it does not take the day's pay.
            float deductions = Mathf.Min(dishesBroken * DeductionPerDishBroken * small * rules.BreakageScale,
                MaxDeductionShare * (baseWage - idleDocked));
            float quotaBonus = quotaWorked > 0 && dishesCleaned >= quotaWorked ? QuotaBonusHours * hourlyWage * shareWorked : 0f;
            float gradeBonus = GradeBonusHours(grade) * hourlyWage * shareWorked;

            return new ShiftResult
            {
                DishesCleaned = dishesCleaned,
                DishesMissed = missed,
                DishesBroken = dishesBroken,
                FullRackLoads = fullRackLoads,
                ElapsedSeconds = elapsedSeconds,
                PlannedSeconds = plannedSeconds,
                Place = place,
                Special = special,
                BaseWage = baseWage,
                Tips = tips,
                Deductions = deductions,
                IdleDocked = idleDocked,
                Quota = quotaWorked,
                QuotaBonus = quotaBonus,
                Grade = grade,
                GradeBonus = gradeBonus,
                Wage = Mathf.Max(0f, baseWage - idleDocked + quotaBonus + gradeBonus + tips - deductions)
            };
        }
    }
}
