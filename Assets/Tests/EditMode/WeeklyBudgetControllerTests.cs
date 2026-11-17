// Reconstructed on 2026-11-17 by decompiling Library/ScriptAssemblies/Zmywak.Tests.EditMode.dll (built 2026-10-11):
// the original source was lost from disk. Behaviour matches that build; original comments are gone.
using NUnit.Framework;
using Zmywak.Meta.Economy;

namespace Zmywak.Tests
{
    public class WeeklyBudgetControllerTests
    {
        [Test]
        public void ApplyWeeklyExpenses_DeductsFixedCosts()
        {
            WeeklyBudgetController budget = new WeeklyBudgetController();
            float result = budget.ApplyWeeklyExpenses(200f);
            Assert.AreEqual(200f - budget.TotalWeeklyExpenses, result, 9.999999747378752E-05);
        }

        [Test]
        public void ApplyWeeklyExpenses_CanGoNegative_NoClampingHere()
        {
            Assert.Less(new WeeklyBudgetController().ApplyWeeklyExpenses(10f), 0f);
        }
    }
}
