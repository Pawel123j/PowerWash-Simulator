namespace Zmywak.Meta.Economy
{
    /// <summary>The week's cost of living (<see cref="LifeRules.WeeklyBillsIn"/>), settled from the wallet at the
    /// week's end. The rent follows the year - up through the boom, down after the bust.</summary>
    public class WeeklyBudgetController
    {
        private readonly LifeRules _rules;

        public WeeklyBudgetController(LifeRules rules = null)
        {
            _rules = rules ?? LifeRules.Default;
        }

        /// <summary>2004's bills, when Kuba arrives.</summary>
        public float TotalWeeklyExpenses => _rules.WeeklyBills;

        public float TotalWeeklyExpensesIn(int year) => _rules.WeeklyBillsIn(year);

        public float ApplyWeeklyExpenses(float walletEuros)
        {
            return walletEuros - TotalWeeklyExpenses;
        }
    }
}
