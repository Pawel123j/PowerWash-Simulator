using UnityEngine;

namespace Zmywak.Core.Data
{
    /// <summary>The day's shift as the evening plans it (<see cref="ShiftSchedule.For"/>) and the kitchen takes it (<see cref="ShiftPlan"/>).</summary>
    public readonly struct ScheduledShift
    {
        public ScheduledShift(string archetypeId, int dayNumber, int seed, float pace, bool tutorialBreakdown, float breakdownChanceScale)
        {
            ArchetypeId = archetypeId;
            DayNumber = dayNumber;
            Seed = seed;
            Pace = pace;
            TutorialBreakdown = tutorialBreakdown;
            BreakdownChanceScale = breakdownChanceScale;
        }

        /// <summary>Which kind of shift (<see cref="ShiftDefinition.Id"/>, one of <see cref="ShiftSchedule.Archetypes"/>).</summary>
        public string ArchetypeId { get; }

        public int DayNumber { get; }

        /// <summary>Deals the day's dishes (<see cref="ShuffleBag{T}"/>): the same day comes the same way.</summary>
        public int Seed { get; }

        /// <summary>The story's tightening: the gaps between dishes times this - 1 in the first week, down to
        /// <see cref="ShiftSchedule.FastestPace"/> by the last chapter.</summary>
        public float Pace { get; }

        /// <summary>The machine's one scripted breakdown, so the repair is met once (until Kuba has fixed it once).</summary>
        public bool TutorialBreakdown { get; }

        /// <summary>The shift's chance of a breakdown, times this: none before the scripted one, less the day after one.</summary>
        public float BreakdownChanceScale { get; }
    }

    /// <summary>
    /// What kind of shift each day is. The story's days carry the difficulty: a calm first three days to learn the sink,
    /// plain shifts with a first lunch rush in the rest of the first week, then the rushes come in turn and more often,
    /// and from chapter four the hard nights - while every gap between dishes slowly closes up. The special shifts of the
    /// calendar (<see cref="SpecialShifts"/>) keep their own rules on top of the kind of night they fall on. Pure and
    /// deterministic: a day is the same day however often it is loaded.
    /// </summary>
    public static class ShiftSchedule
    {
        public const string CalmStart = "calm_start";
        public const string Normal = "normal";
        public const string LunchRush = "lunch_rush";
        public const string EveningRush = "evening_rush";
        public const string HardNight = "hard_night";

        public static readonly string[] Archetypes = { CalmStart, Normal, LunchRush, EveningRush, HardNight };

        /// <summary>Days of calm shifts at the start.</summary>
        public const int CalmDays = 3;

        /// <summary>The first day's dishes open in a fixed order (what the hints teach on); after it, the deal is shuffled.</summary>
        public const int FixedOpeningDays = 1;

        /// <summary>The scripted breakdown waits for its day in the run of new things (<see cref="MechanicUnlocks"/>): the first
        /// days are for the sink, the glass and the chef.</summary>
        public static int FirstTutorialBreakdownDay => MechanicUnlocks.DayOf(Mechanic.Breakdowns);

        /// <summary>Which machine cycle breaks in the scripted breakdown (the first one is always seen working).</summary>
        public const int TutorialBreakdownCycle = 2;

        /// <summary>The day after a breakdown, the chance of another is cut to this share: no run of them day after day.</summary>
        public const float AfterBreakdownChanceScale = 0.35f;

        /// <summary>The story's last day (chapter six's last scene); the pace stops tightening there.</summary>
        public const int LastStoryDay = 35;

        /// <summary>The gaps between dishes on the story's last day and after it, against the first week's.</summary>
        public const float FastestPace = 0.85f;

        /// <summary>No shift asks for more clean dishes than a practised porter washes in one (~13).</summary>
        public const int QuotaCap = 12;

        private const int FirstWeek = 7;
        private const int LastOfChapterThree = 18;
        private const int LastOfChapterFive = 30;

        private static readonly string[] RestOfFirstWeek = { Normal, Normal, LunchRush, Normal };
        private static readonly string[] ChaptersTwoAndThree = { Normal, LunchRush, Normal, EveningRush, Normal, LunchRush, EveningRush };
        private static readonly string[] ChaptersFourAndFive = { LunchRush, EveningRush, Normal, HardNight, LunchRush, EveningRush, Normal };
        private static readonly string[] Finale = { EveningRush, HardNight, LunchRush, Normal, HardNight, EveningRush, LunchRush };

        /// <summary>The day's shift: its kind, how fast, how it is dealt, and what the dishwasher may do.</summary>
        /// <param name="tutorialBreakdownDone">Kuba has already met (and fixed) a breakdown.</param>
        /// <param name="lastShiftBrokeDown">The machine broke down on the shift before.</param>
        public static ScheduledShift For(int dayNumber, SpecialShift special, bool tutorialBreakdownDone, bool lastShiftBrokeDown)
        {
            bool tutorial = !tutorialBreakdownDone && dayNumber >= FirstTutorialBreakdownDay;
            float chanceScale = !tutorialBreakdownDone ? 0f : lastShiftBrokeDown ? AfterBreakdownChanceScale : 1f;
            return new ScheduledShift(ArchetypeFor(dayNumber, special), dayNumber, SeedFor(dayNumber), PaceFor(dayNumber), tutorial, chanceScale);
        }

        /// <summary>
        /// The kind of night: a wedding or St. Patrick's fills the evening, Sunday's lasagne tray comes with the lunch crowd,
        /// and the unannounced inspection falls on a plain shift (nothing gives it away before it starts). Otherwise the
        /// story's stretch decides - see the tables above.
        /// </summary>
        public static string ArchetypeFor(int dayNumber, SpecialShift special)
        {
            switch (special)
            {
                case SpecialShift.Wedding:
                case SpecialShift.StPatricks:
                    return EveningRush;
                case SpecialShift.LasagneTray:
                    return LunchRush;
                case SpecialShift.Inspection:
                    return Normal;
            }

            if (dayNumber <= CalmDays)
            {
                return CalmStart;
            }

            if (dayNumber <= FirstWeek)
            {
                return RestOfFirstWeek[(dayNumber - CalmDays - 1) % RestOfFirstWeek.Length];
            }

            if (dayNumber <= LastOfChapterThree)
            {
                return ChaptersTwoAndThree[(dayNumber - FirstWeek - 1) % ChaptersTwoAndThree.Length];
            }

            if (dayNumber <= LastOfChapterFive)
            {
                return ChaptersFourAndFive[(dayNumber - LastOfChapterThree - 1) % ChaptersFourAndFive.Length];
            }

            return Finale[(dayNumber - LastOfChapterFive - 1) % Finale.Length];
        }

        /// <summary>1 through the first week, then closing up evenly to <see cref="FastestPace"/> by the story's last day.</summary>
        public static float PaceFor(int dayNumber)
        {
            if (dayNumber <= FirstWeek)
            {
                return 1f;
            }

            float through = Mathf.Clamp01((dayNumber - FirstWeek) / (float)(LastStoryDay - FirstWeek));
            return Mathf.Lerp(1f, FastestPace, through);
        }

        public static int SeedFor(int dayNumber) => unchecked(dayNumber * 7919 + 104729);

        /// <summary>
        /// The clean dishes a shift asks for: its kind's quota, grown with how much faster the dishes come (the story's pace and
        /// a busy night's), never more than <see cref="QuotaCap"/>.
        /// </summary>
        public static int QuotaFor(int baseQuota, float pace, float specialPace)
        {
            float faster = Mathf.Max(0.3f, pace * specialPace);
            return Mathf.Clamp(Mathf.RoundToInt(baseQuota / faster), 1, QuotaCap);
        }
    }
}
