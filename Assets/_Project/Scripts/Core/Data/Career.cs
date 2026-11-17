using System;
using UnityEngine;

namespace Zmywak.Core.Data
{
    /// <summary>Everything Kuba has done at the sink, over all his shifts - what the promotions are judged on.</summary>
    [Serializable]
    public class CareerRecord
    {
        public int shifts;
        public int dishesWashed;
        public int fullRacks;

        /// <summary>Shifts worked to the end without a single breakage.</summary>
        public int cleanShifts;

        public int inspectionsPassed;
        public int traysScrubbed;

        /// <summary>The longest Flow streak of any shift.</summary>
        public int bestFlow;

        /// <summary>Personal bests of a single shift: the score out of 100 and the dishes washed.</summary>
        public int bestScore;
        public int mostDishes;

        public CareerRecord Clone() => (CareerRecord)MemberwiseClone();

        public void Record(ShiftResult shift)
        {
            shifts++;
            dishesWashed += shift.DishesCleaned;
            fullRacks += shift.FullRackLoads;
            if (shift.DishesBroken == 0 && shift.PlannedSeconds > 0f && shift.ElapsedSeconds >= shift.PlannedSeconds - 0.5f)
            {
                cleanShifts++;
            }

            if (shift.Special == SpecialShift.Inspection && shift.InspectionPassed)
            {
                inspectionsPassed++;
            }

            if (shift.TrayScrubbed)
            {
                traysScrubbed++;
            }

            bestFlow = Mathf.Max(bestFlow, shift.BestFlow);
            bestScore = Mathf.Max(bestScore, shift.Score.Total);
            mostDishes = Mathf.Max(mostDishes, shift.DishesCleaned);
        }
    }

    /// <summary>What one rank asks for, all of it over Kuba's whole career.</summary>
    public readonly struct RankRequirement
    {
        public RankRequirement(int dishes, int fullRacks, int cleanShifts, int inspections, int trays, int bestFlow)
        {
            Dishes = dishes;
            FullRacks = fullRacks;
            CleanShifts = cleanShifts;
            Inspections = inspections;
            Trays = trays;
            BestFlow = bestFlow;
        }

        public int Dishes { get; }
        public int FullRacks { get; }
        public int CleanShifts { get; }
        public int Inspections { get; }
        public int Trays { get; }
        public int BestFlow { get; }
    }

    /// <summary>
    /// The ladder at the sink: from the new boy to the King of the Dishwashers. Each rank asks for more of everything a
    /// good porter does - dishes washed, racks sent off full, whole shifts without a breakage, inspections passed, the
    /// legendary tray scrubbed, a long Flow - and pays a little more an hour. Promotions are for good: a bad week does
    /// not take a rank away.
    /// </summary>
    public static class Career
    {
        public const int KingRank = 4;

        /// <summary>What every rank adds to the hourly wage, in Dublin euros (elsewhere the place's small sums).</summary>
        public const float HourlyBonusPerRankEuros = 0.35f;

        private static readonly string[] Titles = { "Nowy na zmywaku", "Pomywacz", "Starszy pomywacz", "Szef zmywaka", "Król Zmywaka" };

        private static readonly RankRequirement[] Requirements =
        {
            new RankRequirement(0, 0, 0, 0, 0, 0),
            new RankRequirement(40, 2, 1, 0, 0, 0),
            new RankRequirement(150, 10, 5, 0, 0, 3),
            new RankRequirement(400, 30, 15, 1, 1, 5),
            new RankRequirement(800, 60, 30, 2, 2, 8)
        };

        public static string Title(int rank) => Titles[Mathf.Clamp(rank, 0, KingRank)];

        public static RankRequirement Requirement(int rank) => Requirements[Mathf.Clamp(rank, 0, KingRank)];

        public static float HourlyBonusEuros(int rank) => Mathf.Clamp(rank, 0, KingRank) * HourlyBonusPerRankEuros;

        /// <summary>The highest rank the record has earned.</summary>
        public static int RankFor(CareerRecord record)
        {
            int rank = 0;
            while (rank < KingRank && Meets(record, Requirements[rank + 1]))
            {
                rank++;
            }

            return rank;
        }

        /// <summary>How far towards the next rank, 0-1: the share of whichever requirement is furthest off (1 for the King).</summary>
        public static float ProgressToNext(CareerRecord record, int rank)
        {
            if (rank >= KingRank)
            {
                return 1f;
            }

            RankRequirement next = Requirements[rank + 1];
            float progress = 1f;
            progress = Mathf.Min(progress, Share(record.dishesWashed, next.Dishes));
            progress = Mathf.Min(progress, Share(record.fullRacks, next.FullRacks));
            progress = Mathf.Min(progress, Share(record.cleanShifts, next.CleanShifts));
            progress = Mathf.Min(progress, Share(record.inspectionsPassed, next.Inspections));
            progress = Mathf.Min(progress, Share(record.traysScrubbed, next.Trays));
            progress = Mathf.Min(progress, Share(record.bestFlow, next.BestFlow));
            return progress;
        }

        /// <summary>What is still missing for the next rank, in a line for the hub (empty for the King).</summary>
        public static string NextGoal(CareerRecord record, int rank)
        {
            if (rank >= KingRank)
            {
                return string.Empty;
            }

            RankRequirement next = Requirements[rank + 1];
            var missing = new System.Collections.Generic.List<string>();
            // Short enough for one line of the hub's notice: the rank to come is on the wallet's line already.
            Add(missing, record.dishesWashed, next.Dishes, "naczynia");
            Add(missing, record.fullRacks, next.FullRacks, "kosze");
            Add(missing, record.cleanShifts, next.CleanShifts, "czyste zmiany");
            Add(missing, record.inspectionsPassed, next.Inspections, "inspekcje");
            Add(missing, record.traysScrubbed, next.Trays, "tace");
            Add(missing, record.bestFlow, next.BestFlow, "Flow");
            // A no-break space before each dot: where the line wraps, the dot stays at the end of the first line, never begins the next.
            return "Do awansu: " + string.Join("\u00A0· ", missing);
        }

        private static bool Meets(CareerRecord r, RankRequirement need) =>
            r.dishesWashed >= need.Dishes && r.fullRacks >= need.FullRacks && r.cleanShifts >= need.CleanShifts &&
            r.inspectionsPassed >= need.Inspections && r.traysScrubbed >= need.Trays && r.bestFlow >= need.BestFlow;

        private static float Share(int have, int need) => need <= 0 ? 1f : Mathf.Clamp01(have / (float)need);

        private static void Add(System.Collections.Generic.List<string> missing, int have, int need, string what)
        {
            if (have < need)
            {
                missing.Add($"{what} {have}/{need}");
            }
        }
    }
}
