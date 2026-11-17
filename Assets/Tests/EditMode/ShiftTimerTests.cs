// Reconstructed on 2026-11-17 by decompiling Library/ScriptAssemblies/Zmywak.Tests.EditMode.dll (built 2026-10-11):
// the original source was lost from disk. Behaviour matches that build; original comments are gone.
using NUnit.Framework;
using UnityEngine;
using Zmywak.Core.Data;
using Zmywak.Kitchen.Shifts;

namespace Zmywak.Tests
{
    public class ShiftTimerTests
    {
        [Test]
        public void TicksDownAndExpires()
        {
            ShiftTimer timer = new ShiftTimer(5f);
            timer.Tick(3f);
            Assert.IsFalse(timer.IsExpired);
            Assert.AreEqual(2.0, timer.Remaining, 9.999999747378752E-06);
            timer.Tick(10f);
            Assert.IsTrue(timer.IsExpired);
            Assert.AreEqual(0f, timer.Remaining);
        }

        [Test]
        public void Progress_RunsFromZeroToOne()
        {
            ShiftTimer timer = new ShiftTimer(180f);
            Assert.AreEqual(0.0, timer.Progress01, 9.999999747378752E-06);
            timer.Tick(45f);
            Assert.AreEqual(0.25, timer.Progress01, 9.999999747378752E-06);
            timer.Tick(500f);
            Assert.AreEqual(1.0, timer.Progress01, 9.999999747378752E-06);
        }

        [Test]
        public void SpawnInterval_TightensOverTheShift()
        {
            ShiftDefinition shift = ScriptableObject.CreateInstance<ShiftDefinition>();
            try
            {
                float num = shift.SpawnIntervalAt(0f);
                float middle = shift.SpawnIntervalAt(0.5f);
                float end = shift.SpawnIntervalAt(1f);
                Assert.Greater(num, middle);
                Assert.Greater(middle, end);
                Assert.AreEqual((num + end) / 2f, middle, 9.999999747378752E-05, "The gap should shrink steadily");
                Assert.AreEqual(end, shift.SpawnIntervalAt(3f), "Overtime must not keep shrinking the gap");
                Assert.AreEqual(num, shift.SpawnIntervalAt(-1f));
            }
            finally
            {
                Object.DestroyImmediate(shift);
            }
        }
    }
}
