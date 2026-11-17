using System.Threading;
using NUnit.Framework;
using Zmywak.Core.Haptics;
using Zmywak.Core.Settings;

namespace Zmywak.Tests
{
    /// <summary>
    /// The vibration follows the setting, and never buzzes: a burst of the same pulse sends one, a tick right after any
    /// pulse is swallowed, and once the gap has passed the next one goes through.
    /// </summary>
    public class HapticsTests
    {
        [SetUp]
        public void SetUp()
        {
            AccessibilitySettings.KeyPrefix = "test_";
            AccessibilitySettings.DeleteAll();
        }

        [TearDown]
        public void TearDown()
        {
            AccessibilitySettings.DeleteAll();
            AccessibilitySettings.KeyPrefix = string.Empty;
        }

        [Test]
        public void SwitchedOff_NothingIsSent()
        {
            AccessibilitySettings.HapticsEnabled = false;
            Thread.Sleep(350); // past any earlier test's pulses
            int before = HapticsService.PulsesSent;
            HapticsService.Pulse(HapticPulse.Heavy);
            HapticsService.Pulse(HapticPulse.Light);
            HapticsService.Pulse(HapticPulse.Tick);
            Assert.AreEqual(before, HapticsService.PulsesSent, "A pulse went out with the vibration switched off");
        }

        [Test]
        public void ABurst_SendsOnePulse_ATickRightAfterIsSwallowed_AndAfterTheGapTheNextGoesThrough()
        {
            AccessibilitySettings.HapticsEnabled = true;
            Thread.Sleep(350);
            int before = HapticsService.PulsesSent;
            for (int i = 0; i < 10; i++)
            {
                HapticsService.Pulse(HapticPulse.Medium);
            }

            Assert.AreEqual(before + 1, HapticsService.PulsesSent, "Ten pulses at once buzzed instead of sending one");
            HapticsService.Pulse(HapticPulse.Tick);
            Assert.AreEqual(before + 1, HapticsService.PulsesSent, "A scrubbing tick went out on top of a pulse");

            Thread.Sleep(350);
            HapticsService.Pulse(HapticPulse.Medium);
            Assert.AreEqual(before + 2, HapticsService.PulsesSent, "After the gap the next pulse did not go through");
        }
    }
}
