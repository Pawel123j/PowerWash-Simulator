// Reconstructed on 2026-11-17 by decompiling Library/ScriptAssemblies/Zmywak.Tests.EditMode.dll (built 2026-10-11):
// the original source was lost from disk. Behaviour matches that build; original comments are gone.
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Zmywak.Core.Save;

namespace Zmywak.Tests
{
    public class SaveSystemTests
    {
        private string _tempDir;

        [SetUp]
        public void SetUp()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), "ZmywakSaveTests_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempDir);
            SaveSystem.DirectoryOverride = _tempDir;
            SaveSystem.ResetSlot();
        }

        [TearDown]
        public void TearDown()
        {
            SaveSystem.DirectoryOverride = null;
            SaveSystem.PreferAtomicReplace = true;
            SaveSystem.ResetSlot();
            if (Directory.Exists(_tempDir))
            {
                Directory.Delete(_tempDir, recursive: true);
            }
        }

        [Test]
        public void NoSaveFile_LoadReturnsNull()
        {
            Assert.IsFalse(SaveSystem.HasSave());
            Assert.IsNull(SaveSystem.Load());
        }

        [Test]
        public void SaveThenLoad_RoundTripsEveryField()
        {
            SaveData saveData = new SaveData
            {
                walletEuros = 123.45f,
                energy = 42f,
                homesickness = 77f,
                integration = 13f,
                dayNumber = 9,
                currentDateIso = "2004-05-09T00:00:00.0000000",
                knownVocabulary = new List<string> { "chef", "kitchen", "grand" },
                unlockedSlangIds = new List<string> { "slang_grand" },
                hasSeenIntro = true
            };
            SaveSystem.Save(saveData);
            SaveData loaded = SaveSystem.Load();
            Assert.IsNotNull(loaded);
            Assert.AreEqual(saveData.walletEuros, loaded.walletEuros, 9.999999747378752E-05);
            Assert.AreEqual(saveData.energy, loaded.energy, 9.999999747378752E-05);
            Assert.AreEqual(saveData.homesickness, loaded.homesickness, 9.999999747378752E-05);
            Assert.AreEqual(saveData.integration, loaded.integration, 9.999999747378752E-05);
            Assert.AreEqual(saveData.dayNumber, loaded.dayNumber);
            Assert.AreEqual(saveData.currentDateIso, loaded.currentDateIso);
            CollectionAssert.AreEqual(saveData.knownVocabulary, loaded.knownVocabulary);
            CollectionAssert.AreEqual(saveData.unlockedSlangIds, loaded.unlockedSlangIds);
            Assert.IsTrue(loaded.hasSeenIntro);
        }

        [Test]
        public void DeleteSave_RemovesFile()
        {
            SaveSystem.Save(new SaveData());
            SaveSystem.Save(new SaveData());
            Assert.IsTrue(SaveSystem.HasSave());
            SaveSystem.DeleteSave();
            Assert.IsFalse(SaveSystem.HasSave());
            Assert.IsFalse(File.Exists(SaveSystem.BackupPath));
        }

        [Test]
        public void Saving_KeepsThePreviousGoodSaveAsABackup_AndLeavesNoTempFile()
        {
            SaveSystem.Save(new SaveData
            {
                dayNumber = 3
            });
            SaveSystem.Save(new SaveData
            {
                dayNumber = 4
            });
            Assert.AreEqual(4, SaveSystem.Load().dayNumber);
            StringAssert.Contains("\"dayNumber\": 3", File.ReadAllText(SaveSystem.BackupPath));
            Assert.IsEmpty(Directory.GetFiles(_tempDir, "*.tmp"), "A temporary file was left behind");
        }

        [TestCase("")]
        [TestCase("{ \"walletEuros\": 12.5, \"dayNu")]
        [TestCase("\0\0\0\0")]
        public void ADamagedSave_FallsBackToTheLastGoodOne(string damaged)
        {
            SaveSystem.Save(new SaveData
            {
                dayNumber = 5,
                walletEuros = 40f
            });
            SaveSystem.Save(new SaveData
            {
                dayNumber = 6,
                walletEuros = 55f
            });
            File.WriteAllText(SaveSystem.FilePath, damaged);
            SaveData loaded = SaveSystem.Load();
            Assert.IsNotNull(loaded, "A damaged save lost all progress");
            Assert.AreEqual(5, loaded.dayNumber, "Should have restored the previous good save");
        }

        [Test]
        public void SavingOverADamagedFile_KeepsTheGoodBackup_InsteadOfTheBrokenOne()
        {
            SaveSystem.Save(new SaveData
            {
                dayNumber = 5
            });
            SaveSystem.Save(new SaveData
            {
                dayNumber = 6
            });
            File.WriteAllText(SaveSystem.FilePath, "{ \"dayNu");
            Assert.AreEqual(5, SaveSystem.Load().dayNumber, "The game should carry on from the backup");
            Assert.IsTrue(SaveSystem.Save(new SaveData
            {
                dayNumber = 7
            }));
            Assert.AreEqual(7, SaveSystem.Load().dayNumber);
            StringAssert.Contains("\"dayNumber\": 5", File.ReadAllText(SaveSystem.BackupPath), "The broken file was moved over the only good backup");
            Assert.IsEmpty(Directory.GetFiles(_tempDir, "*.tmp"), "A temporary file was left behind");
        }

        [Test]
        public void ASaveWrittenBeforeVersions_IsUpgraded()
        {
            File.WriteAllText(SaveSystem.FilePath, "{ \"walletEuros\": 20.0, \"dayNumber\": 2, \"hasSeenIntro\": true, \"knownVocabulary\": [\"chef\"] }");
            SaveData loaded = SaveSystem.Load();
            Assert.AreEqual(2, loaded.version);
            CollectionAssert.Contains(loaded.seenStoryBeats, "ch1_arrival", "The intro the player saw was forgotten");
            Assert.IsNotNull(loaded.storyFlags);
            Assert.AreEqual(2, loaded.dayNumber);
        }

        [Test]
        public void EverySave_IsStampedWithTheCurrentVersion()
        {
            SaveSystem.Save(new SaveData());
            StringAssert.Contains($"\"version\": {2}", File.ReadAllText(SaveSystem.FilePath));
        }

        [Test]
        public void WithoutFileReplace_TheCopyingSwapSavesJustAsWell()
        {
            SaveSystem.PreferAtomicReplace = false;
            Assert.IsTrue(SaveSystem.Save(new SaveData
            {
                dayNumber = 3
            }));
            Assert.IsTrue(SaveSystem.Save(new SaveData
            {
                dayNumber = 4
            }));
            Assert.IsTrue(SaveSystem.Save(new SaveData
            {
                dayNumber = 5
            }));
            Assert.AreEqual(5, SaveSystem.Load().dayNumber);
            StringAssert.Contains("\"dayNumber\": 4", File.ReadAllText(SaveSystem.BackupPath), "The previous save is the backup");
            Assert.IsEmpty(Directory.GetFiles(_tempDir, "*.tmp"), "A temporary file was left behind");
            File.WriteAllText(SaveSystem.FilePath, "{ \"dayNu");
            Assert.AreEqual(4, SaveSystem.Load().dayNumber);
        }

        [Test]
        public void ASaveThatCannotBeWritten_FailsQuietly_AndLeavesTheOldOneAlone()
        {
            SaveSystem.Save(new SaveData
            {
                dayNumber = 7
            });
            Directory.CreateDirectory(SaveSystem.FilePath + ".tmp");
            bool saved = true;
            LogAssert.Expect(LogType.Warning, new Regex("could not save"));
            Assert.DoesNotThrow(() =>
            {
                saved = SaveSystem.Save(new SaveData
                {
                    dayNumber = 8
                });
            }, "A failing save must not take the app down");
            Assert.IsFalse(saved);
            Directory.Delete(SaveSystem.FilePath + ".tmp");
            Assert.AreEqual(7, SaveSystem.Load().dayNumber, "The save already on disk was lost");
            Directory.Delete(_tempDir, recursive: true);
            LogAssert.Expect(LogType.Warning, new Regex("could not save"));
            Assert.DoesNotThrow(() =>
            {
                saved = SaveSystem.Save(new SaveData
                {
                    dayNumber = 9
                });
            });
            Assert.IsFalse(saved);
        }

        [Test]
        public void WhenNothingReadableIsLeft_LoadingStartsAFreshGame_NotACrash()
        {
            SaveSystem.Save(new SaveData
            {
                dayNumber = 5
            });
            SaveSystem.Save(new SaveData
            {
                dayNumber = 6
            });
            File.WriteAllText(SaveSystem.FilePath, "{ \"walletEuros\": 12.5, \"dayNu");
            File.WriteAllText(SaveSystem.BackupPath, "\0\0\0\0");
            SaveData loaded = null;
            Assert.DoesNotThrow(() =>
            {
                loaded = SaveSystem.Load();
            });
            Assert.IsNull(loaded, "Neither file could be read: the game starts over rather than guessing");
            Assert.IsTrue(SaveSystem.HasSave(), "The files are still there; they just cannot be read");
        }

        [Test]
        public void ASaveFromANewerGame_IsLoadedAsFarAsItGoes()
        {
            File.WriteAllText(SaveSystem.FilePath, $"{{ \"version\": {5}, \"dayNumber\": 12, \"walletEuros\": 80.0, \"somethingNew\": [1, 2] }}");
            LogAssert.Expect(LogType.Warning, new Regex("newer than this game"));
            SaveData loaded = SaveSystem.Load();
            Assert.IsNotNull(loaded, "A newer save must not be thrown away");
            Assert.AreEqual(12, loaded.dayNumber);
            Assert.AreEqual(80.0, loaded.walletEuros, 9.999999747378752E-05);
        }

        [Test]
        public void ThreeSlots_KeepTheirOwnSaves_AndTheFirstIsTheOldFile()
        {
            Assert.AreEqual("zmywak_save.json", Path.GetFileName(SaveSystem.PathOf(0)), "Old single saves stay the first slot");
            Assert.AreEqual(0, SaveSystem.Slot);
            SaveSystem.Save(new SaveData
            {
                dayNumber = 30
            });
            SaveSystem.Slot = 2;
            Assert.IsNull(SaveSystem.Load(), "A new slot starts empty");
            SaveSystem.Save(new SaveData
            {
                dayNumber = 3
            });
            Assert.AreEqual(3, SaveSystem.Load().dayNumber);
            Assert.AreEqual(30, SaveSystem.Peek(0).dayNumber, "Peeking at another slot reads it without switching");
            Assert.AreEqual(2, SaveSystem.Slot);
            Assert.IsFalse(SaveSystem.HasSave(1));
            SaveSystem.DeleteSlot(0);
            Assert.IsFalse(SaveSystem.HasSave(0));
            Assert.IsTrue(SaveSystem.HasSave(2), "Deleting one slot leaves the others");
        }
    }
}
