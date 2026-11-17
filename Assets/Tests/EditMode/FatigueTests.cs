// Reconstructed on 2026-11-17 by decompiling Library/ScriptAssemblies/Zmywak.Tests.EditMode.dll (built 2026-10-11):
// the original source was lost from disk. Behaviour matches that build; original comments are gone.
using NUnit.Framework;
using Zmywak.Kitchen.Shifts;

namespace Zmywak.Tests
{
    public class FatigueTests
    {
        [Test]
        public void Rested_ScrubsAtFullStrength()
        {
            Assert.AreEqual(1.0, Fatigue.ScrubStrength(100f), 9.999999747378752E-06);
            Assert.AreEqual(1.0, Fatigue.ScrubStrength(60f), 9.999999747378752E-06);
            Assert.IsFalse(Fatigue.IsTired(60f));
        }

        [Test]
        public void Tired_ScrubsSlower_TheMoreTiredTheSlower()
        {
            Assert.IsTrue(Fatigue.IsTired(59f));
            Assert.AreEqual(0.699999988079071, Fatigue.ScrubStrength(0f), 9.999999747378752E-06);
            float previous = 0f;
            for (float energy = 0f; energy <= 100f; energy += 5f)
            {
                float num = Fatigue.ScrubStrength(energy);
                Assert.GreaterOrEqual(num, previous, $"energy {energy}");
                Assert.That(num, Is.InRange(0.7f, 1f));
                previous = num;
            }
        }

        [Test]
        public void EnergyOutOfRange_IsClamped()
        {
            Assert.AreEqual(0.699999988079071, Fatigue.ScrubStrength(-20f), 9.999999747378752E-06);
            Assert.AreEqual(1.0, Fatigue.ScrubStrength(250f), 9.999999747378752E-06);
        }
    }
}
