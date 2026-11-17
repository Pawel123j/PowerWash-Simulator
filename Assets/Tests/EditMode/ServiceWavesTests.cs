// Reconstructed on 2026-11-17 by decompiling Library/ScriptAssemblies/Zmywak.Tests.EditMode.dll (built 2026-10-11):
// the original source was lost from disk. Behaviour matches that build; original comments are gone.
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Zmywak.Core.Data;

namespace Zmywak.Tests
{
    public class ServiceWavesTests
    {
        private static readonly ServiceWave[] TwoRushes = new ServiceWave[2]
        {
            new ServiceWave(0.2f, 0.45f, 0.4f),
            new ServiceWave(0.6f, 0.9f, 0.8f, "Kolacja!")
        };

        [Test]
        public void WithoutWaves_TheShiftKeepsOnePace()
        {
            for (float p = 0f; p <= 1f; p += 0.05f)
            {
                Assert.AreEqual(1.0, ServiceWaves.PaceAt(null, p), 9.999999747378752E-06);
                Assert.AreEqual(1.0, ServiceWaves.PaceAt(new ServiceWave[0], p), 9.999999747378752E-06);
                Assert.AreEqual(ServicePhase.Steady, ServiceWaves.PhaseAt(new ServiceWave[0], p, out var _));
            }
        }

        [Test]
        public void InsideARush_DishesComeFaster_AndNeverSlowerThanBetween()
        {
            Assert.AreEqual(1.0, ServiceWaves.PaceAt(TwoRushes, 0.1f), 9.999999747378752E-06, "Before the first rush");
            Assert.AreEqual(1.399999976158142, ServiceWaves.PaceAt(TwoRushes, 0.33f), 9.999999747378752E-06, "The first rush's middle");
            Assert.AreEqual(1.0, ServiceWaves.PaceAt(TwoRushes, 0.52f), 9.999999747378752E-06, "The breather");
            Assert.AreEqual(1.7999999523162842, ServiceWaves.PaceAt(TwoRushes, 0.75f), 9.999999747378752E-06, "The second rush's middle");
            Assert.AreEqual(1.0, ServiceWaves.PaceAt(TwoRushes, 0.97f), 9.999999747378752E-06, "The end of the night");
            for (float p = 0f; p <= 1f; p += 0.01f)
            {
                Assert.GreaterOrEqual(ServiceWaves.PaceAt(TwoRushes, p), 1f);
            }
        }

        [Test]
        public void ARush_BuildsUpAndDiesAway_WithoutAJump()
        {
            float previous = ServiceWaves.PaceAt(TwoRushes, 0f);
            for (float p = 0.005f; p <= 1f; p += 0.005f)
            {
                float num = ServiceWaves.PaceAt(TwoRushes, p);
                Assert.Less(Mathf.Abs(num - previous), 0.15f, $"The pace jumps at {p:0.000}");
                previous = num;
            }
        }

        [Test]
        public void ThePhases_GoQuiet_FillingUp_Rush_Breather_Rush_End()
        {
            List<ServicePhase> phases = new List<ServicePhase>();
            for (float p = 0f; p <= 1f; p += 0.01f)
            {
                ServicePhase phase = ServiceWaves.PhaseAt(TwoRushes, p, out var _);
                if (phases.Count == 0 || phases[phases.Count - 1] != phase)
                {
                    phases.Add(phase);
                }
            }
            CollectionAssert.AreEqual(new ServicePhase[9]
            {
                ServicePhase.Steady,
                ServicePhase.Rising,
                ServicePhase.Peak,
                ServicePhase.Easing,
                ServicePhase.Steady,
                ServicePhase.Rising,
                ServicePhase.Peak,
                ServicePhase.Easing,
                ServicePhase.Steady
            }, phases);
            ServiceWaves.PhaseAt(TwoRushes, 0.75f, out var wave);
            Assert.AreEqual(1, wave, "The rush at 0.75 is the second one (its callout is the HUD's)");
        }
    }
}
