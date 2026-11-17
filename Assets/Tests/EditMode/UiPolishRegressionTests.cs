// Reconstructed on 2026-11-17 by decompiling Library/ScriptAssemblies/Zmywak.Tests.EditMode.dll (built 2026-10-11):
// the original source was lost from disk. Behaviour matches that build; original comments are gone.
using NUnit.Framework;
using Zmywak.Core.Data;

namespace Zmywak.Tests
{
    [Category("QARegression")]
    public class UiPolishRegressionTests
    {
        [Test]
        public void PWS027_ThePromotionHint_KeepsEachDotAtTheEndOfAWrappedLine_NeverAtTheStartOfTheNext()
        {
            string goal = Career.NextGoal(new CareerRecord
            {
                dishesWashed = 0,
                fullRacks = 0,
                cleanShifts = 0
            }, 0);
            StringAssert.StartsWith("Do awansu:", goal);
            StringAssert.Contains("\u00a0· ", goal, "The dots are not tied to the item before them");
            StringAssert.DoesNotContain(" · ", goal, "An ordinary space before a dot lets the line break in front of it");
            int dishes = goal.IndexOf("naczynia 0/40");
            int racks = goal.IndexOf("kosze 0/2");
            int actual = goal.IndexOf("czyste zmiany 0/1");
            Assert.That(dishes, Is.GreaterThan(0));
            Assert.That(racks, Is.GreaterThan(dishes));
            Assert.That(actual, Is.GreaterThan(racks));
        }

        [Test]
        public void PWS027_AHintWithASingleItem_HasNoDot()
        {
            string goal = Career.NextGoal(new CareerRecord
            {
                dishesWashed = 40,
                fullRacks = 2,
                cleanShifts = 0
            }, 0);
            StringAssert.Contains("czyste zmiany 0/1", goal);
            StringAssert.DoesNotContain("naczynia", goal, "Met requirements are not listed");
            StringAssert.DoesNotContain("·", goal);
        }
    }
}
