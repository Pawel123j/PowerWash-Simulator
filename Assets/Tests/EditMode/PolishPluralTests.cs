using NUnit.Framework;
using Zmywak.Core.Data;

namespace Zmywak.Tests
{
    /// <summary>PWS-021: "Nowy rekord: 22 naczyń" - the form after a number is by its last digits (1: one; 2-4 but not 12-14: few; the rest: many).</summary>
    [Category("QARegression")]
    public class PolishPluralTests
    {
        [TestCase(0, "naczyń")]
        [TestCase(1, "naczynie")]
        [TestCase(2, "naczynia")]
        [TestCase(3, "naczynia")]
        [TestCase(4, "naczynia")]
        [TestCase(5, "naczyń")]
        [TestCase(9, "naczyń")]
        [TestCase(10, "naczyń")]
        [TestCase(11, "naczyń")]
        [TestCase(12, "naczyń")]
        [TestCase(13, "naczyń")]
        [TestCase(14, "naczyń")]
        [TestCase(15, "naczyń")]
        [TestCase(21, "naczyń")]
        [TestCase(22, "naczynia")]
        [TestCase(23, "naczynia")]
        [TestCase(24, "naczynia")]
        [TestCase(25, "naczyń")]
        [TestCase(31, "naczyń")]
        [TestCase(32, "naczynia")]
        [TestCase(34, "naczynia")]
        [TestCase(40, "naczyń")]
        [TestCase(100, "naczyń")]
        [TestCase(101, "naczyń")]
        [TestCase(102, "naczynia")]
        [TestCase(111, "naczyń")]
        [TestCase(112, "naczyń")]
        [TestCase(114, "naczyń")]
        [TestCase(122, "naczynia")]
        [TestCase(1012, "naczyń")]
        [TestCase(int.MaxValue, "naczyń")]
        [TestCase(int.MinValue, "naczyń")]
        [TestCase(-1, "naczynie")]
        [TestCase(-22, "naczynia")]
        public void TheDishes_TakeTheFormOfTheirCount(int count, string expected)
        {
            Assert.AreEqual(expected, PolishPlural.Form(count, "naczynie", "naczynia", "naczyń"));
            Assert.AreEqual($"{count} {expected}", PolishPlural.Dishes(count));
        }

        [Test]
        public void EveryCountFromZeroTo200_HasExactlyOneRightForm()
        {
            for (int n = 0; n <= 200; n++)
            {
                string form = PolishPlural.Form(n, "one", "few", "many");
                bool few = (n % 10 >= 2 && n % 10 <= 4) && !(n % 100 >= 12 && n % 100 <= 14);
                string expected = n == 1 ? "one" : few ? "few" : "many";
                Assert.AreEqual(expected, form, $"count {n}");
            }
        }
    }
}
