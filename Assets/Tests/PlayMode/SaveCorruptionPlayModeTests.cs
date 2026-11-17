using System.Collections;
using System.IO;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Zmywak.Core.GameState;
using Zmywak.Core.Save;
using Zmywak.Core.Services;
using Zmywak.Kitchen.Dishwasher;
using Zmywak.Meta.Progress;
using Zmywak.UI.Screens;

namespace Zmywak.Tests.PlayMode
{
    /// <summary>
    /// What the real scenes do with a save that is wreckage or odd: the menu, the persistent controllers that read it first, and the
    /// kitchen's big dishwasher. The unit side is SaveCorruptionTests (EditMode); PWS-002 / 007 / 008 / 009 in QA_REPORT.md.
    /// </summary>
    [Category("QARegression")]
    public class SaveCorruptionPlayModeTests
    {
        [SetUp]
        public void SetUp()
        {
            TestSaveSandbox.Enter();
        }

        [TearDown]
        public void TearDown()
        {
            BigDishwasherController.Clock = () => System.DateTime.UtcNow;
            TestSaveSandbox.DestroyPersistentObjects();
            TestSaveSandbox.Exit();
        }

        private static IEnumerator Boot()
        {
            Scene previouslyActive = SceneManager.GetActiveScene();
            SceneManager.LoadScene("Boot", LoadSceneMode.Single);
            var deadline = new PlayDeadline(10f);
            while ((SceneManager.GetActiveScene() == previouslyActive || SceneManager.GetActiveScene().name != "MainMenu") && !deadline.Passed)
            {
                yield return null;
            }

            yield return null;
            Assert.AreEqual("MainMenu", SceneManager.GetActiveScene().name);
        }

        private static IEnumerator WaitForScene(string scene, float seconds = 12f)
        {
            var deadline = new PlayDeadline(seconds);
            while ((SceneManager.GetActiveScene().name != scene || SceneLoader.IsLoading) && !deadline.Passed)
            {
                yield return null;
            }

            yield return null;
            Assert.AreEqual(scene, SceneManager.GetActiveScene().name, $"Never arrived at {scene}");
        }

        [UnityTest]
        public IEnumerator AWreckedSlot_IsNotOfferedAsALifeToContinue_AndANewGameStartsInIt()
        {
            File.WriteAllText(SaveSystem.FilePath, "{ \"version\": 2, \"dayNumber\": 12, \"walletEu"); // cut off mid-write
            File.WriteAllText(SaveSystem.BackupPath, "\0\0\0\0"); // and the backup is no better
            Assert.IsTrue(SaveSystem.HasSave(0), "Setup: the files should be there");

            yield return Boot();

            Assert.IsNull(GameObject.Find("MenuContinueButton"), "The menu offers to continue a save that cannot be read");
            Assert.IsNull(GameObject.Find("LoadGameButton"), "The menu offers to load saves, and there is none");
            var start = GameObject.Find("NewGameButton").GetComponentInChildren<TMP_Text>();
            StringAssert.Contains("Rozpocznij", start.text, "A first start is what this is");

            Object.FindAnyObjectByType<MainMenuScreen>().Play(0);
            yield return WaitForScene("MetaHub");
            Assert.IsTrue(ServiceLocator.TryGet(out MetaProgressController meta));
            Assert.AreEqual(1, meta.DayNumber, "A wrecked slot should be a new life");
            meta.EnsureOnDisk();
            Assert.IsNotNull(SaveSystem.Load(), "The new life could not be saved over the wreckage");
            Assert.AreEqual(1, SaveSystem.Load().dayNumber);
        }

        [UnityTest]
        public IEnumerator AWreckedSlot_NextToAGoodOne_LeavesTheGoodOneToContinue()
        {
            SaveSystem.Save(new SaveData { dayNumber = 9, walletEuros = 40f, energy = 70f, currentDateIso = "2004-05-09T00:00:00.0000000" });
            SaveSystem.Slot = 1;
            File.WriteAllText(SaveSystem.FilePath, "{ \"version\": 2, \"dayNum");
            SaveSystem.Slot = 0;

            yield return Boot();

            Assert.IsNotNull(GameObject.Find("MenuContinueButton"), "The good slot is not offered");
            StringAssert.Contains("Dzień 9", GameObject.Find("ContinueDetails").GetComponent<TMP_Text>().text);
            Assert.AreEqual(0, Object.FindAnyObjectByType<MainMenuScreen>().ContinueSlot);
        }

        [UnityTest]
        public IEnumerator AnExtremeButReadableSave_GoesFromTheMenuToTheHubAndThroughAShift_WithoutAnError(
            [Values(
                "{\"version\":2,\"dayNumber\":2147483647,\"walletEuros\":50,\"energy\":80,\"currentDateIso\":\"2004-05-09T00:00:00.0000000\"}",
                "{\"version\":2,\"dayNumber\":8,\"walletEuros\":3.4e38,\"energy\":80}",
                "{\"version\":2,\"dayNumber\":8,\"energy\":80,\"currentDateIso\":\"9999-12-31T23:59:59.9999999\"}",
                "{\"version\":2,\"dayNumber\":8,\"energy\":80,\"place\":7}",
                "{\"version\":2,\"dayNumber\":8,\"energy\":80,\"storyFlags\":null,\"knownVocabulary\":null,\"purchases\":[\"Nothing\",\"99\"]}")] string json)
        {
            File.WriteAllText(SaveSystem.FilePath, json);

            yield return Boot();
            Object.FindAnyObjectByType<MainMenuScreen>().Play(0);
            yield return WaitForScene("MetaHub");
            Assert.IsTrue(ServiceLocator.TryGet(out MetaProgressController meta));
            Assert.That(meta.DayNumber, Is.InRange(1, SaveValidation.MaxDay));
            Assert.IsFalse(float.IsNaN(meta.Wallet) || float.IsInfinity(meta.Wallet));

            // a shift paid and the evening after it: the day turns over (AddDays on the date, the week's bills)
            SceneManager.LoadScene("Kitchen_Shift", LoadSceneMode.Single);
            yield return new WaitForSeconds(1f);
            Object.FindAnyObjectByType<Zmywak.Kitchen.Shifts.ShiftController>().EndShiftNow();
            yield return null;
            meta.CallMom();
            Assert.That(meta.CurrentDate.Year, Is.InRange(SaveValidation.EarliestYear, SaveValidation.LatestYear));
            Assert.IsNotNull(SaveSystem.Load(), "What it wrote could not be read back");
        }

        [UnityTest]
        public IEnumerator TheKitchen_WithABigDishwasherStartTimeNoClockCanHold_Loads_WithTheLoadStillInTheMachine()
        {
            var now = new System.DateTime(2026, 10, 1, 18, 0, 0, System.DateTimeKind.Utc);
            BigDishwasherController.Clock = () => now;
            File.WriteAllText(SaveSystem.FilePath,
                "{\"version\":2,\"dayNumber\":8,\"bigDishwasherLoad\":[\"plate\",\"pan\"],\"bigDishwasherStartedUtcTicks\":9223372036854775807}");

            SceneManager.LoadScene("Kitchen_Shift", LoadSceneMode.Single);
            yield return null;
            yield return null;

            var machine = Object.FindAnyObjectByType<BigDishwasherController>();
            Assert.IsNotNull(machine, "The kitchen did not come up");
            Assert.AreEqual(2, machine.Count, "The load was lost");
            Assert.IsFalse(machine.IsStarted, "A start time that cannot exist became a start");
            Assert.IsTrue(machine.CanStart, "The machine cannot be started again");
        }

        [UnityTest]
        public IEnumerator TheKitchen_WithABigDishwasherStartedYearsAhead_CountsItAsStartedNow_NotJammedForYears()
        {
            var now = new System.DateTime(2026, 10, 1, 18, 0, 0, System.DateTimeKind.Utc);
            BigDishwasherController.Clock = () => now;
            var save = new SaveData { dayNumber = 8, bigDishwasherStartedUtcTicks = now.AddYears(40).Ticks };
            save.bigDishwasherLoad.Add("plate");
            SaveSystem.Save(save);

            SceneManager.LoadScene("Kitchen_Shift", LoadSceneMode.Single);
            yield return null;
            yield return null;

            var machine = Object.FindAnyObjectByType<BigDishwasherController>();
            Assert.IsTrue(machine.IsRunning, "It should be running (started just now)");
            Assert.LessOrEqual(machine.Remaining, machine.Cycle, "It would take longer than a cycle");
            now = now.AddMinutes(31);
            Assert.IsTrue(machine.IsDone, "A cycle later it should be done, not still jammed by the damaged time");
        }
    }
}
