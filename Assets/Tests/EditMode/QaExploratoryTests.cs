using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using Zmywak.Core.Data;
using Zmywak.Core.Save;
using Zmywak.Kitchen.Dishes;
using Zmywak.Kitchen.Dishwasher;
using Zmywak.Kitchen.Shifts;
using Zmywak.Meta.Economy;
using Zmywak.Meta.Progress;

namespace Zmywak.Tests
{
    /// <summary>
    /// QA probes: each test tries to break one thing a player (or a phone) can do, and asserts what SHOULD happen. A failing
    /// one is a bug found - see QA_REPORT.md. They began as Explicit probes (red = an open bug); every bug they found is fixed now, so
    /// they run with the regular suite. Category "QA".
    /// </summary>
    [Category("QA")]
    public class QaExploratoryTests
    {
        private static readonly LifeRules Rules = LifeRules.Default;
        private string _dir;

        [SetUp]
        public void SetUp()
        {
            _dir = Path.Combine(Path.GetTempPath(), "ZmywakQa_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_dir);
            SaveSystem.DirectoryOverride = _dir;
            SaveSystem.ResetSlot();
        }

        [TearDown]
        public void TearDown()
        {
            SaveSystem.DirectoryOverride = null;
            SaveSystem.PreferAtomicReplace = true;
            SaveSystem.ResetSlot();
            if (Directory.Exists(_dir))
            {
                Directory.Delete(_dir, true);
            }
        }

        private static ShiftResult Paid(float euros) => new ShiftResult { Wage = euros };

        /// <summary>A life that earned nothing until a week's bills left it in the red.</summary>
        private static LifeLedger InDebt()
        {
            var ledger = new LifeLedger(Rules, null);
            for (int day = 0; day < 40 && ledger.Wallet >= 0f; day++)
            {
                ledger.CompleteShift(Paid(0f));
            }

            Assert.Less(ledger.Wallet, 0f, "Setup: the life never fell into debt");
            return ledger;
        }

        // ---- Economy ---------------------------------------------------------------------------------------------

        [Test]
        public void PWS001_AStoryMoment_WhileInDebt_DoesNotWipeTheDebt([Values(-1f, +1f)] float euros)
        {
            LifeLedger ledger = InDebt();
            float before = ledger.Wallet;
            float expected = before + ledger.FromEuros(euros);

            ledger.ApplyStoryOutcome(new StoryOutcome { moneyEuros = euros });

            Assert.AreEqual(expected, ledger.Wallet, 0.011f,
                $"In debt ({before:0.00}), a story moment worth {euros:+0;-0} EUR left the wallet at {ledger.Wallet:0.00} instead of {expected:0.00}");
        }

        [Test]
        public void PWS005_SkippingEveryShift_NeverBreaksTheNumbers()
        {
            // The pause menu's "back to the menu" clocks out for what is done: tapped at second one it pays nothing but
            // still turns the day over. Thirty such days.
            var ledger = new LifeLedger(Rules, null);
            var early = new ShiftResult { Wage = 0f, ElapsedSeconds = 1f, PlannedSeconds = 180f };
            for (int day = 0; day < 60; day++)
            {
                ledger.CompleteShift(early);
                Assert.IsFalse(float.IsNaN(ledger.Wallet) || float.IsInfinity(ledger.Wallet), $"Wallet is {ledger.Wallet} on day {ledger.DayNumber}");
                Assert.That(ledger.Stats.Energy, Is.InRange(0f, 100f));
                Assert.That(ledger.Stats.Homesickness, Is.InRange(0f, 100f));
            }

            Debug.Log($"QA PWS-005: after 60 skipped days: wallet {ledger.Wallet:0.00}, energy {ledger.Stats.Energy:0}, " +
                      $"homesick {ledger.Stats.Homesickness:0}, hostel {ledger.InHostel}, arrears {ledger.WeeksInArrears}, day {ledger.DayNumber}");
        }

        [Test]
        public void PWS006_AShiftWithNoTimeWorked_PaysNothing_AndNeverNaN()
        {
            ShiftResult r = ShiftResultCalculator.Calculate(new List<WashStage>(), 0, 0, 0f, 180f, 0, 7f, Place.Dublin, SpecialShift.None, 12,
                ShiftGrade.None);
            Assert.AreEqual(0f, r.Wage, 1e-4f);
            r = ShiftResultCalculator.Calculate(new List<WashStage>(), 0, 5, 0f, 0f, 0, 7f, Place.Dublin, SpecialShift.None, 0, ShiftGrade.None);
            Assert.IsFalse(float.IsNaN(r.Wage));
            r = ShiftResultCalculator.Calculate(new List<WashStage>(), 3, 0, 500f, 180f, 0, 7f, Place.Dublin, SpecialShift.None, 3, ShiftGrade.S);
            Assert.IsFalse(float.IsNaN(r.Wage) || float.IsInfinity(r.Wage));
        }

        // ---- Saves -----------------------------------------------------------------------------------------------

        private static SaveData RichSave(int day, float wallet)
        {
            return new SaveData
            {
                dayNumber = day,
                walletEuros = wallet,
                energy = 61f,
                homesickness = 33f,
                integration = 12f,
                currentDateIso = "2004-05-09T00:00:00.0000000",
                storyFlags = new List<string> { "ch1_a", "ch1_b", "ch1_c", "ch1_d", "ch1_e" },
                seenStoryBeats = new List<string> { "ch1_arrival", "ch1_house" },
                purchases = new List<string> { "Mattress", "Phone" },
                knownVocabulary = new List<string> { "chef", "kitchen", "grand", "table" }
            };
        }

        [Test]
        public void PWS002_AMainSaveCutShortAnywhere_NeverLoadsAsHalfASave()
        {
            SaveSystem.Save(RichSave(7, 111f));
            SaveSystem.Save(RichSave(8, 222f)); // main = day 8, backup = day 7
            Assert.IsTrue(File.Exists(SaveSystem.BackupPath), "Setup: no backup");
            string full = File.ReadAllText(SaveSystem.FilePath);

            var halfSaves = new List<string>();
            for (int cut = 1; cut < full.Length; cut += Math.Max(1, full.Length / 60))
            {
                File.WriteAllText(SaveSystem.FilePath, full.Substring(0, cut));
                SaveData loaded = SaveSystem.Load();
                bool backup = loaded != null && loaded.dayNumber == 7 && loaded.storyFlags.Count == 5 && loaded.purchases.Count == 2;
                bool whole = loaded != null && loaded.dayNumber == 8 && loaded.storyFlags.Count == 5 && loaded.purchases.Count == 2;
                if (!backup && !whole)
                {
                    halfSaves.Add($"cut at {cut}/{full.Length}: " + (loaded == null
                        ? "null"
                        : $"day {loaded.dayNumber}, wallet {loaded.walletEuros}, flags {loaded.storyFlags?.Count}, purchases {loaded.purchases?.Count}"));
                }
            }

            Assert.IsEmpty(halfSaves, "A save cut short loaded as:\n" + string.Join("\n", halfSaves));
        }

        [Test]
        public void PWS002_AGarbageMainSave_FallsBackToTheBackup([Values("{}", "null", "[]", "{\"dayNumber\":", "\0\0\0\0")] string garbage)
        {
            SaveSystem.Save(RichSave(7, 111f));
            SaveSystem.Save(RichSave(8, 222f));
            File.WriteAllText(SaveSystem.FilePath, garbage);

            SaveData loaded = SaveSystem.Load();

            Assert.IsNotNull(loaded);
            Assert.AreEqual(7, loaded.dayNumber, $"Main file {garbage.Replace("\0", "\\0")}: the backup (day 7) was not used");
        }

        [Test]
        public void PWS007_ABigDishwasherSaveWithAbsurdTicks_DoesNotThrowOnLoad([Values(long.MaxValue, 3155378975999999999L + 1)] long ticks)
        {
            var machine = new LongCycleDishwasher(6, TimeSpan.FromMinutes(30));
            var data = new SaveData { bigDishwasherLoad = new List<string> { "plate" }, bigDishwasherStartedUtcTicks = ticks };

            Assert.DoesNotThrow(() => machine.ReadFrom(data), "A damaged save made loading the kitchen throw");
        }

        [Test]
        public void PWS008_ASaveWithNaNOrInfiniteNumbers_LoadsAsASaneLife()
        {
            var data = RichSave(5, float.NaN);
            data.energy = float.PositiveInfinity;
            data.homesickness = float.NegativeInfinity;
            SaveSystem.Save(data);
            SaveData loaded = SaveSystem.Load();

            var ledger = new LifeLedger(Rules, null);
            if (loaded != null)
            {
                ledger.LoadFrom(loaded);
            }

            Assert.IsFalse(float.IsNaN(ledger.Wallet) || float.IsInfinity(ledger.Wallet), "Wallet is " + ledger.Wallet);
            Assert.That(ledger.Stats.Energy, Is.InRange(0f, 100f), "Energy is " + ledger.Stats.Energy);
            Assert.That(ledger.Stats.Homesickness, Is.InRange(0f, 100f), "Homesickness is " + ledger.Stats.Homesickness);
        }

        [Test]
        public void PWS009_ASaveFromANewerGame_IsNotDowngradedByASave()
        {
            var data = RichSave(5, 50f);
            data.version = SaveMigrations.CurrentVersion + 3;
            File.WriteAllText(SaveSystem.FilePath, JsonUtility.ToJson(data));

            SaveData loaded = SaveSystem.Load();
            SaveSystem.Save(loaded);

            Assert.AreEqual(data.version, SaveSystem.Load().version, "An older game relabelled a newer save as its own version");
        }

        [Test]
        public void PWS010_TwoSaves_ThroughTheCopyFallback_KeepAGoodBackup()
        {
            SaveSystem.PreferAtomicReplace = false; // what a phone does where File.Replace is refused
            for (int day = 1; day <= 4; day++)
            {
                SaveSystem.Save(RichSave(day, day * 10f));
            }

            Assert.AreEqual(4, SaveSystem.Load().dayNumber);
            File.Delete(SaveSystem.FilePath);
            Assert.AreEqual(3, SaveSystem.Load().dayNumber, "With the main file gone the backup should be the save before the last");
            Assert.IsFalse(File.Exists(SaveSystem.FilePath + ".tmp"), "A temporary file was left behind");
        }
    }
}
