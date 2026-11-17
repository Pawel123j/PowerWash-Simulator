// Reconstructed on 2026-11-17 by decompiling Library/ScriptAssemblies/Zmywak.Tests.EditMode.dll (built 2026-10-11):
// the original source was lost from disk. Behaviour matches that build; original comments are gone.
using NUnit.Framework;
using Zmywak.Kitchen.Dishes;

namespace Zmywak.Tests
{
    public class DirtMaskPercentTests
    {
        [Test]
        public void NoLayers_IsFullyClean()
        {
            Assert.AreEqual(1f, DirtMaskMath.ComputeCleanliness01(null));
            Assert.AreEqual(1f, DirtMaskMath.ComputeCleanliness01(new float[0]));
        }

        [Test]
        public void AllLayersFullyDirty_IsFullyDirty()
        {
            Assert.AreEqual(0.0, DirtMaskMath.ComputeCleanliness01(new float[2] { 1f, 1f }), 9.999999747378752E-06);
        }

        [Test]
        public void MixedLayers_AveragesRemaining()
        {
            float result = DirtMaskMath.ComputeCleanliness01(new float[2] { 0f, 1f });
            Assert.AreEqual(0.5, result, 9.999999747378752E-06);
        }

        [Test]
        public void ApplyScrub_NeverGoesBelowZero()
        {
            float result = DirtMaskMath.ApplyScrub(0.1f, 10f, 1f, 1f);
            Assert.AreEqual(0f, result);
        }

        [Test]
        public void ApplyScrub_HigherDifficulty_SlowerProgress()
        {
            float easy = DirtMaskMath.ApplyScrub(1f, 1f, 1f, 1f);
            Assert.Greater(DirtMaskMath.ApplyScrub(1f, 1f, 1f, 2f), easy);
        }

        [Test]
        public void ApplyScrub_StopsAtTheToolsFloor_AndLeavesThinnerDirtAlone()
        {
            Assert.AreEqual(0.30000001192092896, DirtMaskMath.ApplyScrub(1f, 10f, 1f, 1f, 0.3f), 9.999999974752427E-07);
            Assert.AreEqual(0.20000000298023224, DirtMaskMath.ApplyScrub(0.2f, 10f, 1f, 1f, 0.3f), 9.999999974752427E-07, "Dirt already under the floor must not be touched");
            Assert.AreEqual(0.800000011920929, DirtMaskMath.ApplyScrub(1f, 0.2f, 1f, 1f, 0.3f), 9.999999974752427E-07, "Above the floor it comes off at the usual rate");
        }
    }
}
