namespace Zmywak.Core.Data
{
    /// <summary>Outcome of a finished shift: what was washed, broken and left, and the pay it earned - in the money of
    /// <see cref="Place"/>.</summary>
    public class ShiftResult
    {
        public int DishesCleaned;
        public int DishesMissed;
        public int DishesBroken;
        public int FullRackLoads;
        public float ElapsedSeconds;
        public float PlannedSeconds;

        /// <summary>Where the shift was worked: the pay is in its money.</summary>
        public Place Place;

        /// <summary>The pay: <see cref="BaseWage"/> - <see cref="IdleDocked"/> + <see cref="QuotaBonus"/> + <see cref="GradeBonus"/> +
        /// <see cref="Tips"/> - <see cref="Deductions"/> + <see cref="PrepBonus"/> + <see cref="SpecialBonus"/>.</summary>
        public float Wage;
        public float BaseWage;
        public float Tips;
        public float Deductions;

        /// <summary>Docked from the base for a night with next to nothing washed (ShiftResultCalculator.IdleDockShare).</summary>
        public float IdleDocked;

        /// <summary>Paid for meeting the quota (<see cref="Quota"/>).</summary>
        public float QuotaBonus;

        /// <summary>Paid for a great shift (grade A or S).</summary>
        public float GradeBonus;

        /// <summary>The kind of shift (its name in the kitchen) and the clean dishes the night asked for.</summary>
        public string ShiftTitle;
        public int Quota;

        /// <summary>How the shift went, out of 100, its parts, and the grade.</summary>
        public ShiftScore Score;
        public ShiftGrade Grade;

        /// <summary>The chef's urgent orders, and how many were on the rack in time.</summary>
        public int UrgentOrders;
        public int UrgentOrdersMet;

        /// <summary>The share of scrubbing done with the right tool for the dirt (0-1).</summary>
        public float WashAccuracy = 1f;

        public int RackDropsMissed;
        public int PileCollapses;

        /// <summary>Times the dishwasher broke down.</summary>
        public int Breakdowns;

        /// <summary>Paid for even prep at the board before the shift (0 for a shift at the sink only).</summary>
        public float PrepBonus;

        /// <summary>What kind of shift it was (<see cref="SpecialShifts"/>).</summary>
        public SpecialShift Special;

        /// <summary>What the special shift paid on top at the end (part of <see cref="Wage"/>).</summary>
        public float SpecialBonus;

        /// <summary>An inspection shift: whether the kitchen passed (true on any other shift).</summary>
        public bool InspectionPassed = true;

        /// <summary>A lasagne tray shift: whether the tray was scrubbed clean by hand.</summary>
        public bool TrayScrubbed;

        /// <summary>The longest Flow streak of the shift.</summary>
        public int BestFlow;

        /// <summary>What the kind of shift takes out of Kuba (0: the plain shift's, LifeRules.ShiftEnergyCost).</summary>
        public float EnergyCost;

        /// <summary>Dishes a friend from the crew took off the pile on a hard night (counted in <see cref="DishesCleaned"/>).</summary>
        public int CrewHelped;
    }
}
