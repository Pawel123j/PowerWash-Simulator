namespace Zmywak.Kitchen.Dishwasher
{
    /// <summary>
    /// Decides which dishwasher cycles break down: an optional scripted cycle (so a shift can guarantee the
    /// player meets the repair) plus a chance per cycle, capped per shift and kept apart - after a breakdown, a few
    /// cycles must run clean before the next can come, so they never come in an irritating string. The first cycle
    /// never breaks by chance - the player should see the machine work before it lets them down.
    /// </summary>
    public sealed class DishwasherBreakdownSchedule
    {
        public static readonly DishwasherBreakdownSchedule Never = new DishwasherBreakdownSchedule(0, 0f, 0);

        private readonly int _scriptedCycle;
        private readonly float _chancePerCycle;
        private readonly int _maxBreakdowns;
        private readonly int _minCyclesBetween;
        private int _breakdowns;
        private int _lastBreakdownCycle;

        /// <param name="scriptedCycle">1-based cycle that always breaks; 0 for none.</param>
        /// <param name="minCyclesBetween">Cycles that must run clean after a breakdown before one can come by chance.</param>
        public DishwasherBreakdownSchedule(int scriptedCycle, float chancePerCycle, int maxBreakdowns, int minCyclesBetween = 0)
        {
            _scriptedCycle = scriptedCycle;
            _chancePerCycle = chancePerCycle;
            _maxBreakdowns = maxBreakdowns;
            _minCyclesBetween = minCyclesBetween;
        }

        public int Breakdowns => _breakdowns;

        /// <summary>Called once as each cycle starts. <paramref name="roll01"/> is a uniform random number.</summary>
        public bool WillBreak(int cycleNumber, float roll01)
        {
            if (_breakdowns >= _maxBreakdowns)
            {
                return false;
            }

            bool tooSoon = _lastBreakdownCycle > 0 && cycleNumber - _lastBreakdownCycle <= _minCyclesBetween;
            bool breaks = cycleNumber == _scriptedCycle || (cycleNumber > 1 && !tooSoon && roll01 < _chancePerCycle);
            if (breaks)
            {
                _breakdowns++;
                _lastBreakdownCycle = cycleNumber;
            }

            return breaks;
        }
    }
}
