// Reconstructed on 2026-11-17 by decompiling Library/ScriptAssemblies/Zmywak.Tests.EditMode.dll (built 2026-10-11):
// the original source was lost from disk. Behaviour matches that build; original comments are gone.
using NUnit.Framework;
using UnityEngine;
using Zmywak.Core.Settings;

namespace Zmywak.Tests
{
    public class ColorBlindPaletteTests
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
        public void ColorBlindOff_ReturnsOriginalColor()
        {
            AccessibilitySettings.ColorBlindMode = false;
            Color original = new Color(0.5f, 0.3f, 0.1f);
            Assert.AreEqual(original, ColorBlindPalette.Adjust(original, "grease"));
        }

        [Test]
        public void ColorBlindOn_SwapsKnownDirtTypes()
        {
            AccessibilitySettings.ColorBlindMode = true;
            Color original = new Color(0.5f, 0.3f, 0.1f);
            Assert.AreNotEqual(original, ColorBlindPalette.Adjust(original, "grease"));
            Assert.AreNotEqual(original, ColorBlindPalette.Adjust(original, "dried_sauce"));
            Assert.AreEqual(Color.black, ColorBlindPalette.Adjust(original, "burnt_food"));
        }

        [Test]
        public void ColorBlindOn_UnknownDirtType_ReturnsOriginal()
        {
            AccessibilitySettings.ColorBlindMode = true;
            Color original = new Color(0.5f, 0.3f, 0.1f);
            Assert.AreEqual(original, ColorBlindPalette.Adjust(original, "unknown_type"));
        }
    }
}
