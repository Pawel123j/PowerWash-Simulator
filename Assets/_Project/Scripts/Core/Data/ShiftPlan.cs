namespace Zmywak.Core.Data
{
    /// <summary>
    /// What the evening decided about the next shift: set by the hub as Kuba leaves for work, taken once by the
    /// kitchen as the shift begins - so a kitchen loaded on its own (the tests, the editor) gets a plain shift.
    /// </summary>
    public static class ShiftPlan
    {
        /// <summary>Kuba works the board now (the story's knife): the shift opens with prep before the sink.</summary>
        public static bool PrepBeforeShift { get; set; }

        /// <summary>The day's shift is a special one (<see cref="SpecialShifts.On"/>).</summary>
        public static SpecialShift Special { get; set; }

        /// <summary>The day's kind of shift, pace and deal (<see cref="ShiftSchedule.For"/>). None - the kitchen on its own -
        /// plays the kitchen's own default shift.</summary>
        public static ScheduledShift? Schedule { get; set; }

        /// <summary>Reads the schedule and clears it, as <see cref="TakePrepBeforeShift"/>.</summary>
        public static ScheduledShift? TakeSchedule()
        {
            ScheduledShift? schedule = Schedule;
            Schedule = null;
            return schedule;
        }

        /// <summary>Reads the plan and clears it, so it never carries over into a later shift by mistake.</summary>
        public static bool TakePrepBeforeShift()
        {
            bool prep = PrepBeforeShift;
            PrepBeforeShift = false;
            return prep;
        }

        /// <summary>Reads the special shift and clears it, as <see cref="TakePrepBeforeShift"/>.</summary>
        public static SpecialShift TakeSpecial()
        {
            SpecialShift special = Special;
            Special = SpecialShift.None;
            return special;
        }
    }
}
