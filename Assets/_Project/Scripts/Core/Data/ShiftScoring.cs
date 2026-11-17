using UnityEngine;

namespace Zmywak.Core.Data
{
    /// <summary>The shift's grade, D (a hard day) to S (a masterclass). None: not graded (a result made before grades).</summary>
    public enum ShiftGrade
    {
        None,
        D,
        C,
        B,
        A,
        S
    }

    /// <summary>What a shift is scored on (<see cref="ShiftScoring.Score"/>).</summary>
    public struct ShiftPerformance
    {
        /// <summary>Clean dishes: out of the machine, the closing load, the big dishwasher's.</summary>
        public int DishesCleaned;

        /// <summary>The whole shift's quota: walking out early leaves it unmet (the night is graded, not the minutes worked).</summary>
        public int Quota;

        public int DishesBroken;
        public int PileCollapses;

        /// <summary>Dishes dropped beside the rack (they spring back to the sink).</summary>
        public int RackDropsMissed;

        /// <summary>The share of the scrubbing done with a tool that works on the dirt in front of it, 0-1 (1 with no scrubbing).</summary>
        public float WashAccuracy;

        public int BestFlow;
        public int UrgentOrders;
        public int UrgentOrdersMet;
    }

    /// <summary>A shift's score out of 100, the points of each part, and its grade.</summary>
    public readonly struct ShiftScore
    {
        public ShiftScore(float work, float care, float flow, float orders, float penalties)
        {
            Work = work;
            Care = care;
            Flow = flow;
            Orders = orders;
            Penalties = penalties;
            Total = Mathf.Clamp(Mathf.RoundToInt(work + care + flow + orders - penalties), 0, 100);
            Grade = ShiftScoring.GradeFor(Total);
        }

        /// <summary>The quota met (up to <see cref="ShiftScoring.WorkPoints"/>) and more on top (<see cref="ShiftScoring.OverQuotaPoints"/>).</summary>
        public float Work { get; }

        /// <summary>The right tool for the dirt, dishes put on the rack and not beside it.</summary>
        public float Care { get; }

        public float Flow { get; }

        /// <summary>The chef's urgent orders met (full marks on a shift without any).</summary>
        public float Orders { get; }

        /// <summary>Taken off for breakages and the pile coming down.</summary>
        public float Penalties { get; }

        public int Total { get; }
        public ShiftGrade Grade { get; }
    }

    /// <summary>
    /// How well a shift went, out of 100. Most of it is the quota (the work the night needed done); the rest rewards
    /// washing well - the right tool for the dirt, keeping the Flow going, the chef's urgent orders - and breakages and a
    /// collapsed pile are taken off, capped so that one bad moment never sinks a whole night. Fair to a beginner: a
    /// shift that meets its quota without breaking anything is at least a B, nothing goes below D, and every shift ends
    /// with its pay. A
    /// practised porter is clearly rewarded: S takes the quota met with next to nothing lost anywhere else - or beaten.
    /// </summary>
    public static class ShiftScoring
    {
        public const float WorkPoints = 50f;
        public const float OverQuotaPoints = 10f;
        public const float CarePoints = 15f;
        public const float FlowPoints = 10f;
        public const float OrderPoints = 15f;

        /// <summary>The Flow streak that earns all the Flow points.</summary>
        public const int FlowTarget = 4;

        public const float PointsPerMissedDrop = 2f;
        public const float PointsPerBreakage = 8f;
        public const float MaxBreakagePenalty = 24f;
        public const float PointsPerCollapse = 6f;
        public const float MaxCollapsePenalty = 12f;

        public const int GradeS = 90;
        public const int GradeA = 75;
        public const int GradeB = 60;
        public const int GradeC = 40;

        public static ShiftScore Score(ShiftPerformance p)
        {
            int quota = Mathf.Max(1, p.Quota);
            float work = WorkPoints * Mathf.Clamp01(p.DishesCleaned / (float)quota);
            // Beyond the quota: the rest of the points over another quarter of it (at least two dishes).
            int overStep = Mathf.Max(2, Mathf.CeilToInt(quota * 0.25f));
            work += OverQuotaPoints * Mathf.Clamp01((p.DishesCleaned - quota) / (float)overStep);

            float care = Mathf.Max(0f, CarePoints * Mathf.Clamp01(p.WashAccuracy) - PointsPerMissedDrop * p.RackDropsMissed);
            float flow = FlowPoints * Mathf.Clamp01(p.BestFlow / (float)FlowTarget);
            float orders = p.UrgentOrders > 0 ? OrderPoints * Mathf.Clamp01(p.UrgentOrdersMet / (float)p.UrgentOrders) : OrderPoints;
            float penalties = Mathf.Min(MaxBreakagePenalty, PointsPerBreakage * p.DishesBroken) +
                              Mathf.Min(MaxCollapsePenalty, PointsPerCollapse * p.PileCollapses);
            return new ShiftScore(work, care, flow, orders, penalties);
        }

        public static ShiftGrade GradeFor(int total)
        {
            return total >= GradeS ? ShiftGrade.S
                : total >= GradeA ? ShiftGrade.A
                : total >= GradeB ? ShiftGrade.B
                : total >= GradeC ? ShiftGrade.C
                : ShiftGrade.D;
        }

        /// <summary>
        /// The quota for the part of the shift worked (at least one dish) - what the pay for the quota asks of an early exit,
        /// paid for that share too (ShiftResultCalculator). The grade keeps the whole quota.
        /// </summary>
        public static int EffectiveQuota(int quota, float shareWorked)
        {
            return quota <= 0 ? 0 : Mathf.Max(1, Mathf.CeilToInt(quota * Mathf.Clamp01(shareWorked) - 1e-4f));
        }

        public static string Letter(ShiftGrade grade) => grade == ShiftGrade.None ? "-" : grade.ToString();

        /// <summary>What the kitchen makes of the night.</summary>
        public static string Verdict(ShiftGrade grade)
        {
            switch (grade)
            {
                case ShiftGrade.S:
                    return "Mistrzowska zmiana. Szef kiwa głową z uznaniem.";
                case ShiftGrade.A:
                    return "Bardzo dobra robota.";
                case ShiftGrade.B:
                    return "Solidna zmiana.";
                case ShiftGrade.C:
                    return "Dało radę, ale było nerwowo.";
                case ShiftGrade.D:
                    return "Ciężki dzień. Jutro będzie lepiej.";
                default:
                    return string.Empty;
            }
        }
    }
}
