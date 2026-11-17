// Reconstructed on 2026-11-17 by decompiling Library/ScriptAssemblies/Zmywak.Tests.EditMode.dll (built 2026-10-11):
// the original source was lost from disk. Behaviour matches that build; original comments are gone.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using Zmywak.Core.Audio;
using Zmywak.Core.Settings;

namespace Zmywak.Tests
{
    public class GameSoundsTests
    {
        private static IEnumerable<GameSound> AllSounds => Enum.GetValues(typeof(GameSound)).Cast<GameSound>();

        private static string Folder => Path.Combine(Application.dataPath, "_Project/Audio/Game");

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

        [TestCaseSource("AllSounds")]
        public void EverySound_HasAClipInTheBank(GameSound sound)
        {
            GameSoundBank gameSoundBank = Resources.Load<GameSoundBank>("GameSoundBank");
            Assert.IsNotNull(gameSoundBank, "No game sound bank in Resources (Zmywak > Setup > Run All)");
            Assert.Greater(gameSoundBank.TakesOf(sound).Count, 0, $"{sound} has no clip");
            Assert.IsTrue(gameSoundBank.TakesOf(sound).All((AudioClip c) => c != null), $"{sound} has a missing clip");
        }

        [Test]
        public void EveryFile_IsCredited()
        {
            string credits = File.ReadAllText(Path.Combine(Application.dataPath, "../CREDITS.md"));
            string[] array = (Directory.Exists(Folder) ? Directory.GetFiles(Folder, "*.wav").Select(Path.GetFileName).ToArray() : new string[0]);
            Assert.IsNotEmpty(array, "No game sounds in the project");
            string[] array2 = array;
            foreach (string file in array2)
            {
                StringAssert.Contains(file, credits, file + " is not in CREDITS.md");
            }
        }

        [TestCaseSource("AllSounds")]
        public void Synthesised_IsAudible_AndNeverClips(GameSound sound)
        {
            float[] array = GameSynth.Generate(sound);
            Assert.Greater(array.Length, 100, "Empty");
            float arg = ((IEnumerable<float>)array).Max((Func<float, float>)Mathf.Abs);
            Assert.Greater(arg, 0.1f, "Too quiet to hear");
            Assert.LessOrEqual(arg, 0.99f, "Clips");
            Assert.IsFalse(array.Any(float.IsNaN), "NaN in the samples");
        }

        [TestCaseSource("AllSounds")]
        public void Loops_JoinUpWithoutAClick_AndOneShotsDieAway(GameSound sound)
        {
            float[] s = GameSynth.Generate(sound);
            float peak = ((IEnumerable<float>)s).Max((Func<float, float>)Mathf.Abs);
            if (GameSounds.IsLoop(sound))
            {
                float seam = Mathf.Abs(s[^1] - s[0]);
                float typical = 0f;
                for (int i = 1; i < s.Length; i++)
                {
                    typical = Mathf.Max(typical, Mathf.Abs(s[i] - s[i - 1]));
                }
                Assert.LessOrEqual(seam, typical + 0.0001f, "The loop clicks where it wraps");
                Assert.GreaterOrEqual((float)s.Length / 22050f, 3f, "A loop that short is heard repeating");
            }
            else
            {
                Assert.Less(Mathf.Abs(s[^1]), peak * 0.05f + 0.001f, "A one-shot is cut off instead of dying away");
            }
        }

        [Test]
        public void TheMenuMusic_IsLongEnough_NotToNag()
        {
            Assert.Greater((float)GameSynth.MenuMusic().Length / 22050f, 40f, "The menu's piece repeats too soon");
        }

        [Test]
        public void TheLevels_AreSaved_Clamped_AndTheSwitchSilencesAll()
        {
            Assert.AreEqual(1f, AccessibilitySettings.MasterVolume);
            AccessibilitySettings.MasterVolume = 0.5f;
            AccessibilitySettings.MusicVolume = 2f;
            AccessibilitySettings.SfxVolume = -1f;
            Assert.AreEqual(1f, AccessibilitySettings.MusicVolume, "Not clamped");
            Assert.AreEqual(0f, AccessibilitySettings.SfxVolume, "Not clamped");
            Assert.AreEqual(0.5, AccessibilitySettings.MusicGain, 9.999999747378752E-05);
            Assert.AreEqual(0.0, AccessibilitySettings.SfxGain, 9.999999747378752E-05);
            AccessibilitySettings.KeyPrefix = "test_";
            Assert.AreEqual(0.5, AccessibilitySettings.MasterVolume, 9.999999747378752E-05, "The level was not saved");
            AccessibilitySettings.SfxVolume = 1f;
            AccessibilitySettings.SoundEnabled = false;
            Assert.AreEqual(0f, AccessibilitySettings.MusicGain);
            Assert.AreEqual(0f, AccessibilitySettings.SfxGain);
        }
    }
}
