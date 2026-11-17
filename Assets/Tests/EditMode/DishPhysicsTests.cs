// Reconstructed on 2026-11-17 by decompiling Library/ScriptAssemblies/Zmywak.Tests.EditMode.dll (built 2026-10-11):
// the original source was lost from disk. Behaviour matches that build; original comments are gone.
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Zmywak.Core.Data;
using Zmywak.Kitchen.Dishes;

namespace Zmywak.Tests
{
    public class DishPhysicsTests
    {
        private const float CollapseHeight = 1.4f;

        [Test]
        public void Steel_NeverBreaks_GlassBreaksSoonerThanChina()
        {
            Assert.IsFalse(DishPhysics.CanBreak(DishMaterial.Steel));
            Assert.IsFalse(DishPhysics.Breaks(DishMaterial.Steel, 100f), "A pan dropped from the ceiling only rings");
            Assert.IsTrue(DishPhysics.CanBreak(DishMaterial.Ceramic));
            Assert.Less(DishPhysics.BreakSpeed(DishMaterial.Glass), DishPhysics.BreakSpeed(DishMaterial.Ceramic));
        }

        [Test]
        public void AKnockJustUnderTheThreshold_LeavesItWhole()
        {
            DishMaterial[] array = new DishMaterial[2]
            {
                DishMaterial.Glass,
                DishMaterial.Ceramic
            };
            foreach (DishMaterial material in array)
            {
                float speed = DishPhysics.BreakSpeed(material);
                Assert.IsFalse(DishPhysics.Breaks(material, speed * 0.95f), $"{material} broke under its threshold");
                Assert.IsTrue(DishPhysics.Breaks(material, speed), $"{material} survived its threshold");
            }
        }

        [Test]
        public void ThrownOffACollapsingPile_ChinaAndGlassBreak_ButASlipDownTheHeapDoesNot()
        {
            float fromTheTop = Mathf.Sqrt(2f * Mathf.Abs(Physics.gravity.y) * 1.4f);
            Assert.IsTrue(DishPhysics.Breaks(DishMaterial.Ceramic, fromTheTop), "China survived a fall off the top of the pile");
            Assert.IsTrue(DishPhysics.Breaks(DishMaterial.Glass, fromTheTop));
            Assert.Greater(DishPhysics.SafeDropHeight(DishMaterial.Glass), 0.5f);
            Assert.Greater(DishPhysics.SafeDropHeight(DishMaterial.Ceramic), DishPhysics.SafeDropHeight(DishMaterial.Glass));
            Assert.IsTrue(float.IsPositiveInfinity(DishPhysics.SafeDropHeight(DishMaterial.Steel)));
        }

        [Test]
        public void TheKnock_IsAlongTheContactNormal_NotTheSpeedOfAGrazingSlide()
        {
            List<Vector3> up = new List<Vector3> { Vector3.up };
            Assert.AreEqual(5.0, DishPhysics.NormalImpactSpeed(new Vector3(0f, -5f, 0f), up), 9.999999747378752E-05, "Landing flat counts in full");
            Assert.AreEqual(0.0, DishPhysics.NormalImpactSpeed(new Vector3(5f, 0f, 0f), up), 9.999999747378752E-05, "Sliding along the counter is no knock");
            Assert.AreEqual(3.0, DishPhysics.NormalImpactSpeed(new Vector3(4f, -3f, 0f), up), 9.999999747378752E-05);
            List<Vector3> twoPoints = new List<Vector3>
            {
                Vector3.up,
                Vector3.right * 2f
            };
            Assert.AreEqual(4.0, DishPhysics.NormalImpactSpeed(new Vector3(4f, -3f, 0f), twoPoints), 9.999999747378752E-05, "The hardest point counts");
        }

        [Test]
        public void Steel_SlidesMoreReadily_AndRingsOffABitMore()
        {
            (float, float, float) china = DishPhysics.Surface(DishMaterial.Ceramic);
            (float staticFriction, float dynamicFriction, float bounciness) tuple = DishPhysics.Surface(DishMaterial.Steel);
            Assert.Less(tuple.dynamicFriction, china.Item2);
            Assert.Greater(tuple.bounciness, china.Item3);
            Assert.AreSame(DishPhysics.SurfaceMaterial(DishMaterial.Steel), DishPhysics.SurfaceMaterial(DishMaterial.Steel), "Every steel dish shares one physics material");
        }
    }
}
