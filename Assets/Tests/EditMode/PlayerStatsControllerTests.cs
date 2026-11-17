// Reconstructed on 2026-11-17 by decompiling Library/ScriptAssemblies/Zmywak.Tests.EditMode.dll (built 2026-10-11):
// the original source was lost from disk. Behaviour matches that build; original comments are gone.
using NUnit.Framework;
using Zmywak.Meta.Stats;

namespace Zmywak.Tests
{
    public class PlayerStatsControllerTests
    {
        [Test]
        public void ChangeEnergy_ClampsToZeroAndHundred()
        {
            PlayerStatsController stats = new PlayerStatsController();
            stats.ChangeEnergy(-1000f);
            Assert.AreEqual(0f, stats.Energy);
            stats.ChangeEnergy(1000f);
            Assert.AreEqual(100f, stats.Energy);
        }

        [Test]
        public void ChangeHomesickness_ClampsToZeroAndHundred()
        {
            PlayerStatsController stats = new PlayerStatsController();
            stats.ChangeHomesickness(-1000f);
            Assert.AreEqual(0f, stats.Homesickness);
            stats.ChangeHomesickness(1000f);
            Assert.AreEqual(100f, stats.Homesickness);
        }

        [Test]
        public void LoadFrom_RestoresAndClampsValues()
        {
            PlayerStatsController stats = new PlayerStatsController();
            stats.LoadFrom(150f, -50f, 42f);
            Assert.AreEqual(100f, stats.Energy);
            Assert.AreEqual(0f, stats.Homesickness);
            Assert.AreEqual(42f, stats.Integration);
        }
    }
}
