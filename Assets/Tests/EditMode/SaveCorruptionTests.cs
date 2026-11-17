// Reconstructed on 2026-11-17 by decompiling Library/ScriptAssemblies/Zmywak.Tests.EditMode.dll (built 2026-10-11):
// the original source was lost from disk. Behaviour matches that build; original comments are gone.
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using Zmywak.Core.Data;
using Zmywak.Core.Save;
using Zmywak.Kitchen.Dishwasher;
using Zmywak.Meta.Economy;
using Zmywak.Meta.Progress;

namespace Zmywak.Tests
{
    [Category("QARegression")]
    public class SaveCorruptionTests
    {
        private static readonly LifeRules Rules = LifeRules.Default;

        private string _dir;

        [SetUp]
        public void SetUp()
        {
            _dir = Path.Combine(Path.GetTempPath(), "ZmywakCorruption_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_dir);
            SaveSystem.DirectoryOverride = _dir;
            SaveSystem.PreferAtomicReplace = true;
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
                Directory.Delete(_dir, recursive: true);
            }
        }

        private static SaveData Life(int day, float wallet = 80f)
        {
            return new SaveData
            {
                dayNumber = day,
                walletEuros = wallet,
                energy = 61f,
                homesickness = 33f,
                integration = 12f,
                currentDateIso = "2004-05-09T00:00:00.0000000",
                storyFlags = new List<string> { "ch1_a", "ch1_b" },
                seenStoryBeats = new List<string> { "ch1_arrival" },
                purchases = new List<string> { "Mattress" }
            };
        }

        private static void MainDay8_BackupDay7()
        {
            SaveSystem.Save(Life(7));
            SaveSystem.Save(Life(8));
            Assert.IsTrue(File.Exists(SaveSystem.BackupPath), "Setup: no backup");
        }

        private static string WithBom(string json)
        {
            return "\ufeff" + json;
        }

        public static IEnumerable DamagedFiles()
        {
            yield return new TestCaseData("").SetName("{m}_ZeroLength");
            yield return new TestCaseData("  \n\t ").SetName("{m}_Whitespace");
            yield return new TestCaseData("\0\0\0\0\0\0").SetName("{m}_NulBytes");
            yield return new TestCaseData("{\"version\":2,\"dayNumber\":").SetName("{m}_TruncatedMidValue");
            yield return new TestCaseData("{\"version\":2,\"dayNumber\":8").SetName("{m}_TruncatedBeforeBrace");
            yield return new TestCaseData("{\"version\":2,\"dayNumber\":8,}").SetName("{m}_TrailingComma");
            yield return new TestCaseData("{\"version\":2,\"dayNumber\":8}garbage").SetName("{m}_TrailingGarbage");
            yield return new TestCaseData("{\"version\":2,\"dayNumber\":8}{\"version\":2,\"dayNumber\":9}").SetName("{m}_TwoObjects");
            yield return new TestCaseData("[]").SetName("{m}_Array");
            yield return new TestCaseData("null").SetName("{m}_NullLiteral");
            yield return new TestCaseData("\"text\"").SetName("{m}_StringLiteral");
            yield return new TestCaseData("42").SetName("{m}_NumberLiteral");
            yield return new TestCaseData("{}").SetName("{m}_EmptyObject");
            yield return new TestCaseData("{\"foo\":1}").SetName("{m}_ObjectOfSomethingElse");
            yield return new TestCaseData("{\"version\":0,\"dayNumber\":0}").SetName("{m}_EmptyLife_NoVersionNoDay");
            yield return new TestCaseData("{\"version\":2,\"dayNumber\":8,\"walletEuros\":NaN}").SetName("{m}_WalletNaN");
            yield return new TestCaseData("{\"version\":2,\"dayNumber\":8,\"walletEuros\":Infinity}").SetName("{m}_WalletInfinity");
            yield return new TestCaseData("{\"version\":2,\"dayNumber\":8,\"walletEuros\":-Infinity}").SetName("{m}_WalletMinusInfinity");
            yield return new TestCaseData("{\"version\":2,\"dayNumber\":8,\"walletEuros\":1e39}").SetName("{m}_WalletOverflowsFloat");
            yield return new TestCaseData("{\"version\":2,\"dayNumber\":8,\"energy\":NaN}").SetName("{m}_EnergyNaN");
            yield return new TestCaseData("{\"version\":2,\"dayNumber\":8,\"homesickness\":Infinity}").SetName("{m}_HomesicknessInfinity");
            yield return new TestCaseData("{\"version\":2,\"dayNumber\":8,\"integration\":NaN}").SetName("{m}_IntegrationNaN");
            yield return new TestCaseData("{\"version\":2,\"dayNumber\":8,\"sentHomeEuros\":NaN}").SetName("{m}_SentHomeNaN");
            yield return new TestCaseData("{\"version\":2,\"dayNumber\":8,\"sentHomePln\":Infinity}").SetName("{m}_SentHomePlnInfinity");
        }

        [TestCaseSource("DamagedFiles")]
        public void AnUnusableMainSave_FallsBackToTheGoodBackup(string damaged)
        {
            MainDay8_BackupDay7();
            File.WriteAllText(SaveSystem.FilePath, damaged);
            SaveData loaded = null;
            Assert.DoesNotThrow(() =>
            {
                loaded = SaveSystem.Load();
            }, "Loading threw");
            Assert.IsNotNull(loaded, "Nothing came back although the backup is good");
            Assert.AreEqual(7, loaded.dayNumber, "The good backup (day 7) was not the one used");
            Assert.IsTrue(SaveSystem.HasReadableSave());
        }

        [TestCaseSource("DamagedFiles")]
        public void AnUnusableMainSave_AndNoBackup_IsNoSaveAtAll(string damaged)
        {
            File.WriteAllText(SaveSystem.FilePath, damaged);
            Assert.DoesNotThrow(() =>
            {
                SaveSystem.Load();
            });
            Assert.IsNull(SaveSystem.Load(), "A damaged file came back as a save");
            Assert.IsNull(SaveSystem.Peek(0));
            Assert.IsFalse(SaveSystem.HasReadableSave(), "The menu would offer to continue a save that cannot be read");
        }

        [TestCaseSource("DamagedFiles")]
        public void BothFilesUnusable_IsNoSave_AndANewLifeCanBeSavedOverThem(string damaged)
        {
            File.WriteAllText(SaveSystem.FilePath, damaged);
            File.WriteAllText(SaveSystem.BackupPath, damaged);
            Assert.IsNull(SaveSystem.Load());
            Assert.IsFalse(SaveSystem.HasReadableSave());
            Assert.IsTrue(SaveSystem.Save(Life(1)), "A new life could not be saved over the wreck");
            Assert.AreEqual(1, SaveSystem.Load().dayNumber);
            Assert.IsTrue(SaveSystem.Save(Life(2)));
            Assert.AreEqual(2, SaveSystem.Load().dayNumber);
            File.WriteAllText(SaveSystem.FilePath, damaged);
            Assert.AreEqual(1, SaveSystem.Load().dayNumber);
        }

        [Test]
        public void AnUnusableBackup_WithAGoodMainSave_ChangesNothing_AndIsReplacedByTheNextSave()
        {
            MainDay8_BackupDay7();
            File.WriteAllText(SaveSystem.BackupPath, "{broken");
            Assert.AreEqual(8, SaveSystem.Load().dayNumber);
            Assert.IsTrue(SaveSystem.Save(Life(9)));
            File.WriteAllText(SaveSystem.FilePath, "{broken");
            Assert.AreEqual(8, SaveSystem.Load().dayNumber, "The backup was not renewed by the save after it was damaged");
        }

        [Test]
        public void PWS002_AMainSaveCutShortAnywhere_NeverLoadsAsHalfASave()
        {
            MainDay8_BackupDay7();
            string full = File.ReadAllText(SaveSystem.FilePath);
            List<string> halfSaves = new List<string>();
            for (int cut = 0; cut < full.Length; cut++)
            {
                File.WriteAllText(SaveSystem.FilePath, full.Substring(0, cut));
                SaveData loaded = SaveSystem.Load();
                if (loaded == null || loaded.dayNumber != 7 || loaded.storyFlags.Count != 2)
                {
                    halfSaves.Add($"cut at {cut}/{full.Length}: " + ((loaded == null) ? "null" : $"day {loaded.dayNumber}, wallet {loaded.walletEuros}"));
                }
            }
            Assert.IsEmpty(halfSaves, "Cut at every single character, some loaded as something else than the backup:\n" + string.Join("\n", halfSaves));
        }

        [Test]
        public void AFileWithABom_StillLoads()
        {
            File.WriteAllText(SaveSystem.FilePath, WithBom("{\"version\":2,\"dayNumber\":8,\"walletEuros\":50}"));
            SaveData loaded = SaveSystem.Load();
            Assert.IsNotNull(loaded);
            Assert.AreEqual(8, loaded.dayNumber);
        }

        [Test]
        public void MissingFields_LoadAsDefaults()
        {
            File.WriteAllText(SaveSystem.FilePath, "{\"version\":2,\"dayNumber\":8}");
            SaveData loaded = SaveSystem.Load();
            Assert.AreEqual(8, loaded.dayNumber);
            Assert.IsNotNull(loaded.storyFlags);
            Assert.IsNotNull(loaded.career);
            LifeLedger ledger = new LifeLedger(Rules, null);
            Assert.DoesNotThrow(() =>
            {
                ledger.LoadFrom(loaded);
            });
            Assert.AreEqual(8, ledger.DayNumber);
        }

        [Test]
        public void ExtraFields_AreIgnored()
        {
            File.WriteAllText(SaveSystem.FilePath, "{\"version\":2,\"dayNumber\":8,\"unknown\":{\"a\":[1,2,{\"b\":3}]},\"extra\":\"x\"}");
            Assert.AreEqual(8, SaveSystem.Load().dayNumber);
        }

        [Test]
        public void NullListsAndNullObjects_LoadAsEmpty()
        {
            File.WriteAllText(SaveSystem.FilePath, "{\"version\":2,\"dayNumber\":8,\"storyFlags\":null,\"seenStoryBeats\":null,\"purchases\":null,\"knownVocabulary\":null,\"career\":null,\"bigDishwasherLoad\":null}");
            SaveData loaded = SaveSystem.Load();
            Assert.IsNotNull(loaded);
            LifeLedger ledger = new LifeLedger(Rules, null);
            Assert.DoesNotThrow(() =>
            {
                ledger.LoadFrom(loaded);
            });
            Assert.IsNotNull(ledger.WorkRecord);
        }

        [Test]
        public void AnOldSaveWithoutAVersion_IsUpgraded_AndKeepsItsData()
        {
            File.WriteAllText(SaveSystem.FilePath, "{\"dayNumber\":8,\"walletEuros\":50,\"hasSeenIntro\":true}");
            SaveData loaded = SaveSystem.Load();
            Assert.IsNotNull(loaded, "A save from before versions was refused");
            Assert.AreEqual(2, loaded.version);
            Assert.AreEqual(8, loaded.dayNumber);
            Assert.AreEqual(50.0, loaded.walletEuros, 0.0010000000474974513);
            CollectionAssert.Contains(loaded.seenStoryBeats, "ch1_arrival");
        }

        [Test]
        public void AValueOfTheWrongType_LoadsAsItsDefault_NotACrash()
        {
            File.WriteAllText(SaveSystem.FilePath, "{\"version\":2,\"dayNumber\":8,\"walletEuros\":\"abc\",\"storyFlags\":\"x\",\"career\":5}");
            SaveData loaded = null;
            Assert.DoesNotThrow(() =>
            {
                loaded = SaveSystem.Load();
            });
            Assert.AreEqual(8, loaded.dayNumber);
            Assert.AreEqual(0f, loaded.walletEuros);
        }

        public static IEnumerable ExtremeSaves()
        {
            yield return new TestCaseData("{\"version\":2,\"dayNumber\":2147483647,\"walletEuros\":50}").SetName("{m}_DayIntMax");
            yield return new TestCaseData("{\"version\":2,\"dayNumber\":99999999999,\"walletEuros\":50}").SetName("{m}_DayWrapsAround");
            yield return new TestCaseData("{\"version\":2,\"dayNumber\":-5,\"walletEuros\":50}").SetName("{m}_DayNegative");
            yield return new TestCaseData("{\"version\":2,\"dayNumber\":8,\"walletEuros\":3.4e38}").SetName("{m}_WalletFloatMax");
            yield return new TestCaseData("{\"version\":2,\"dayNumber\":8,\"walletEuros\":-3.4e38}").SetName("{m}_WalletMinusFloatMax");
            yield return new TestCaseData("{\"version\":2,\"dayNumber\":8,\"sentHomeEuros\":-5,\"sentHomePln\":3.4e38}").SetName("{m}_SentHomeOutOfRange");
            yield return new TestCaseData("{\"version\":2,\"dayNumber\":8,\"energy\":9999,\"homesickness\":-9999,\"integration\":1e30}").SetName("{m}_StatsOutOfRange");
            yield return new TestCaseData("{\"version\":2,\"dayNumber\":8,\"currentDateIso\":\"9999-12-31T23:59:59.9999999\"}").SetName("{m}_DateAtTheEndOfTime");
            yield return new TestCaseData("{\"version\":2,\"dayNumber\":8,\"currentDateIso\":\"0001-01-01T00:00:00.0000000\"}").SetName("{m}_DateAtTheStartOfTime");
            yield return new TestCaseData("{\"version\":2,\"dayNumber\":8,\"currentDateIso\":\"garbage\"}").SetName("{m}_DateGarbage");
            yield return new TestCaseData("{\"version\":2,\"dayNumber\":8,\"place\":7}").SetName("{m}_PlaceUndefined");
            yield return new TestCaseData("{\"version\":2,\"dayNumber\":8,\"place\":-1}").SetName("{m}_PlaceNegative");
            yield return new TestCaseData("{\"version\":2,\"dayNumber\":8,\"weeksInArrears\":-40,\"rank\":99,\"lastEveningActivity\":77}").SetName("{m}_CountersOutOfRange");
            yield return new TestCaseData("{\"version\":2,\"dayNumber\":8,\"bigDishwasherLoad\":[\"plate\"],\"bigDishwasherStartedUtcTicks\":9223372036854775807}").SetName("{m}_DishwasherTicksLongMax");
            yield return new TestCaseData("{\"version\":2,\"dayNumber\":8,\"bigDishwasherLoad\":[\"plate\"],\"bigDishwasherStartedUtcTicks\":-9223372036854775808}").SetName("{m}_DishwasherTicksLongMin");
        }

        [TestCaseSource("ExtremeSaves")]
        public void AnExtremeSave_LoadsIntoALifeAndAShiftAndAWeek_WithoutThrowing_AndStaysInRange(string json)
        {
            File.WriteAllText(SaveSystem.FilePath, json);
            SaveData loaded = null;
            Assert.DoesNotThrow(() =>
            {
                loaded = SaveSystem.Load();
            });
            Assert.IsNotNull(loaded);
            LifeLedger ledger = new LifeLedger(Rules, null);
            Assert.DoesNotThrow(() =>
            {
                ledger.LoadFrom(loaded);
            }, "Reading the save into a life threw");
            for (int day = 0; day < 20; day++)
            {
                Assert.DoesNotThrow(() =>
                {
                    ledger.CompleteShift(new ShiftResult
                    {
                        Wage = 12f,
                        ElapsedSeconds = 90f,
                        PlannedSeconds = 180f
                    });
                }, $"Shift {day + 1} after loading threw (day {ledger.DayNumber}, date {ledger.CurrentDate:yyyy-MM-dd})");
                Assert.IsFalse(float.IsNaN(ledger.Wallet) || float.IsInfinity(ledger.Wallet), $"Wallet is {ledger.Wallet}");
                Assert.That(ledger.DayNumber, Is.InRange(1, 200000), "The day number left its range");
                Assert.That(ledger.CurrentDate.Year, Is.InRange(1990, 2200), "The date left its range");
                Assert.That(ledger.Stats.Energy, Is.InRange(0f, 100f));
                Assert.That(ledger.Stats.Homesickness, Is.InRange(0f, 100f));
                Assert.That(ledger.Stats.Integration, Is.InRange(0f, 100f));
                Assert.GreaterOrEqual(ledger.SentHomeEuros, 0f);
                Assert.GreaterOrEqual(ledger.BillsReserve, 0f, "The week's reserve went negative");
                Assert.LessOrEqual(ledger.Sendable, Mathf.Max(0f, ledger.Wallet) + 1f, "More could be sent home than the wallet holds");
            }
            SaveData written = new SaveData();
            ledger.WriteTo(written);
            Assert.IsTrue(SaveSystem.Save(written));
            Assert.AreEqual(Mathf.Min(ledger.DayNumber, 100000), SaveSystem.Load().dayNumber);
        }

        [Test]
        public void ThePlaceOfASave_IsAlwaysOneTheGameKnows()
        {
            File.WriteAllText(SaveSystem.FilePath, "{\"version\":2,\"dayNumber\":8,\"place\":7}");
            Assert.IsTrue(Enum.IsDefined(typeof(Place), SaveSystem.Load().place), "A place the game has never heard of was passed on");
        }

        [Test]
        public void PWS009_ASaveFromANewerGame_KeepsItsVersion_WhenThisGameSavesIt_AndTheOriginalIsKept()
        {
            string original = "{\"version\":" + 5 + ",\"dayNumber\":5,\"walletEuros\":50,\"futureField\":[1,2,3],\"futureObject\":{\"a\":1}}";
            File.WriteAllText(SaveSystem.FilePath, original);
            SaveData loaded = SaveSystem.Load();
            Assert.AreEqual(5, loaded.version);
            Assert.IsTrue(SaveSystem.Save(loaded));
            Assert.AreEqual(5, SaveSystem.Load().version, "An older game relabelled a newer save as its own version");
            string kept = SaveSystem.FilePath + ".newer";
            Assert.IsTrue(File.Exists(kept), "The newer game's own file was not kept before being overwritten");
            Assert.AreEqual(original, File.ReadAllText(kept), "The kept copy is not the original file");
            loaded.walletEuros = 99f;
            Assert.IsTrue(SaveSystem.Save(loaded));
            Assert.AreEqual(original, File.ReadAllText(kept), "The kept copy was overwritten by a later save");
        }

        [Test]
        public void PWS009_ASaveOfThisVersionOrOlder_LeavesNoNewerCopy()
        {
            SaveSystem.Save(Life(3));
            SaveSystem.Save(Life(4));
            File.WriteAllText(SaveSystem.FilePath, "{\"dayNumber\":8,\"walletEuros\":50}");
            SaveSystem.Save(SaveSystem.Load());
            Assert.IsFalse(File.Exists(SaveSystem.FilePath + ".newer"));
            Assert.AreEqual(2, SaveSystem.Load().version);
        }

        [Test]
        public void PWS009_DeletingASlot_AlsoRemovesTheNewerCopy()
        {
            File.WriteAllText(SaveSystem.FilePath, "{\"version\":" + 3 + ",\"dayNumber\":5}");
            SaveSystem.Save(SaveSystem.Load());
            Assert.IsTrue(File.Exists(SaveSystem.FilePath + ".newer"), "Setup");
            SaveSystem.DeleteSlot(0);
            Assert.IsFalse(File.Exists(SaveSystem.FilePath));
            Assert.IsFalse(File.Exists(SaveSystem.BackupPath));
            Assert.IsFalse(File.Exists(SaveSystem.FilePath + ".newer"));
            Assert.IsFalse(File.Exists(SaveSystem.FilePath + ".tmp"));
        }

        [TestCase(long.MaxValue)]
        [TestCase(long.MinValue)]
        [TestCase(3155378976000000000L)]
        [TestCase(-1L)]
        [TestCase(0L)]
        public void PWS007_AStartTimeNoClockCanHold_IsNotAStart_AndDoesNotThrow(long ticks)
        {
            LongCycleDishwasher machine = new LongCycleDishwasher(6, TimeSpan.FromMinutes(30.0));
            SaveData data = new SaveData
            {
                bigDishwasherLoad = new List<string> { "plate", "pan" },
                bigDishwasherStartedUtcTicks = ticks
            };
            Assert.DoesNotThrow(() =>
            {
                machine.ReadFrom(data);
            });
            Assert.AreEqual(2, machine.Count, "The load was lost with the bad start time");
            Assert.IsFalse(machine.IsStarted, "A time nobody can hold became a start");
            Assert.IsTrue(machine.CanStart, "The machine cannot be started again");
        }

        [Test]
        public void PWS007_AValidStartTime_StillLoads()
        {
            DateTime started = new DateTime(2026, 10, 1, 18, 0, 0, DateTimeKind.Utc);
            LongCycleDishwasher machine = new LongCycleDishwasher(6, TimeSpan.FromMinutes(30.0));
            machine.ReadFrom(new SaveData
            {
                bigDishwasherLoad = new List<string> { "plate" },
                bigDishwasherStartedUtcTicks = started.Ticks
            });
            Assert.IsTrue(machine.IsStarted);
            Assert.AreEqual(started, machine.StartedUtc.Value);
            Assert.IsTrue(machine.IsDone(started.AddMinutes(31.0)));
        }
    }
}
