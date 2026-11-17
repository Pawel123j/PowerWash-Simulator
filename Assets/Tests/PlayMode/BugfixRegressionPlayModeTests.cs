// Reconstructed on 2026-11-17 by decompiling Library/ScriptAssemblies/Zmywak.Tests.PlayMode.dll (built 2026-10-11):
// the original source was lost from disk. Behaviour matches that build; original comments are gone.
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Zmywak.Core.Data;
using Zmywak.Core.Diagnostics;
using Zmywak.Core.GameState;
using Zmywak.Core.Save;
using Zmywak.Core.Services;
using Zmywak.Core.Settings;
using Zmywak.Kitchen.Dishes;
using Zmywak.Kitchen.Dishwasher;
using Zmywak.Kitchen.Input;
using Zmywak.Kitchen.Rack;
using Zmywak.Kitchen.Shifts;
using Zmywak.Kitchen.Tools;
using Zmywak.Meta.Progress;
using Zmywak.Narrative.Progress;
using Zmywak.UI.Dev;
using Zmywak.UI.Screens;

namespace Zmywak.Tests.PlayMode
{
    [Category("QARegression")]
    public class BugfixRegressionPlayModeTests
    {
        private static readonly DateTime BigDishwasherNow = new DateTime(2026, 10, 1, 18, 0, 0, DateTimeKind.Utc);

        [SetUp]
        public void SetUp()
        {
            TestSaveSandbox.Enter();
        }

        [TearDown]
        public void TearDown()
        {
            TestSaveSandbox.DestroyPersistentObjects();
            TestSaveSandbox.Exit();
        }

        private static SaveData Life(int day)
        {
            return new SaveData
            {
                dayNumber = day,
                walletEuros = 80f,
                energy = 70f,
                currentDateIso = "2004-05-12T00:00:00.0000000",
                storyFlags = new List<string> { "ch1_complete" },
                seenStoryBeats = new List<string> { "ch1_arrival" }
            };
        }

        private static (MetaProgressController meta, NarrativeProgressController narrative) LiveLife()
        {
            MetaProgressController item = new GameObject("Meta (QA)").AddComponent<MetaProgressController>();
            NarrativeProgressController narrative = new GameObject("Narrative (QA)").AddComponent<NarrativeProgressController>();
            return (meta: item, narrative: narrative);
        }

        private static void TheAppGoesToTheBackground(MetaProgressController meta, NarrativeProgressController narrative)
        {
            meta.SendMessage("OnApplicationPause", true);
            meta.SendMessage("OnApplicationFocus", false);
            narrative.SendMessage("OnApplicationPause", true);
            narrative.SendMessage("OnApplicationFocus", false);
        }

        private static Rect ScreenFootprint(DishController dish, Camera camera)
        {
            Bounds b = dish.GetComponent<Renderer>().bounds;
            Vector2 min = new Vector2(float.MaxValue, float.MaxValue);
            Vector2 max = new Vector2(float.MinValue, float.MinValue);
            for (int i = 0; i < 8; i++)
            {
                Vector3 corner = new Vector3(((i & 1) == 0) ? b.min.x : b.max.x, ((i & 2) == 0) ? b.min.y : b.max.y, ((i & 4) == 0) ? b.min.z : b.max.z);
                Vector2 p = camera.WorldToScreenPoint(corner);
                min = Vector2.Min(min, p);
                max = Vector2.Max(max, p);
            }
            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }

        private static IEnumerator Sweep(DishController dish, Camera camera, TouchGestureRecognizer gesture, Func<bool> stop, float seconds)
        {
            Rect footprint = ScreenFootprint(dish, camera);
            float rowStep = Mathf.Max(2f, Mathf.Min(footprint.height / 12f, footprint.width / 12f));
            float columnStep = Mathf.Max(2f, footprint.width / 10f);
            bool leftToRight = true;
            PlayDeadline deadline = new PlayDeadline(seconds);
            Time.timeScale = 3f;
            gesture.PressBegin(new Vector2(footprint.xMin, footprint.yMax));
            while (!stop() && !deadline.Passed)
            {
                float y = footprint.yMax;
                while (y >= footprint.yMin && !stop())
                {
                    for (float t = 0f; t <= footprint.width; t += columnStep)
                    {
                        if (stop())
                        {
                            break;
                        }
                        Vector2 p = new Vector2(leftToRight ? (footprint.xMin + t) : (footprint.xMax - t), y);
                        gesture.PressStay(p, Time.deltaTime);
                        dish.OnGestureUpdate(gesture, p);
                        yield return null;
                    }
                    leftToRight = !leftToRight;
                    y -= rowStep;
                }
            }
            Time.timeScale = 1f;
        }

        [UnityTest]
        public IEnumerator PWS020_InTheKitchen_AScraperRubbedDownToTheFilm_MakesTheToolBarSuggestAnotherTool_WhichFinishesTheDish()
        {
            AccessibilitySettings.RelaxedMode = true;
            AccessibilitySettings.ToolAssist = false;
            yield return LoadKitchen();
            WashStation station = UnityEngine.Object.FindAnyObjectByType<WashStation>();
            RackGrid rack = UnityEngine.Object.FindAnyObjectByType<RackGrid>();
            ActiveToolSelector selector = ActiveToolSelector.Instance;
            Camera camera = Camera.main;
            DishDefinition pan = null;
            DishDefinition[] array = Resources.FindObjectsOfTypeAll<DishDefinition>();
            foreach (DishDefinition d in array)
            {
                if (d.Id == "pan")
                {
                    pan = d;
                }
            }
            ToolDefinition scraper = null;
            foreach (ToolDefinition t in selector.AllTools)
            {
                if (t.Id == "scraper")
                {
                    scraper = t;
                }
            }
            Assert.IsNotNull(pan, "Setup: no pan");
            Assert.IsNotNull(scraper, "Setup: no scraper");
            Assert.IsTrue(selector.IsOnTheBar(scraper), "Setup: the scraper is not on the bar");
            DishController dish = DishController.Create(pan, station.transform.position + Vector3.up * 2f, null, rack, station);
            Assert.IsTrue(dish.TryClaim(), "Setup: the sink would not take the pan");
            PlayDeadline arrival = new PlayDeadline(8f);
            while (dish.IsMoving && !arrival.Passed)
            {
                yield return null;
            }
            TouchGestureRecognizer gesture = new TouchGestureRecognizer();
            Vector2 centre = camera.WorldToScreenPoint(dish.transform.position);
            gesture.PressBegin(centre);
            gesture.PressStay(centre + new Vector2(90f, 0f), 0.1f);
            gesture.PressEnd(centre + new Vector2(90f, 0f));
            dish.OnGestureEnd(gesture, centre + new Vector2(90f, 0f));
            Assert.AreEqual(WashStage.AwaitingRinse, dish.CurrentStage, "Setup: the swipe did not scrape the pan");
            gesture.PressBegin(centre);
            PlayDeadline rinse = new PlayDeadline(10f);
            while (dish.CurrentStage == WashStage.AwaitingRinse && !rinse.Passed)
            {
                gesture.PressStay(centre, Time.deltaTime);
                dish.OnGestureUpdate(gesture, centre);
                yield return null;
            }
            Assert.AreEqual(WashStage.AwaitingScrub, dish.CurrentStage, "Setup: the pan did not come to the scrubbing stage");
            selector.SelectTool(scraper);
            yield return null;
            Assert.AreSame(scraper, selector.CurrentTool);
            Assert.IsTrue(dish.ToolSuits(scraper), "Setup: the scraper should suit a pan of burnt food");
            Assert.IsFalse(selector.WrongToolInHand, "Setup: the bar already calls the scraper wrong");
            yield return Sweep(dish, camera, gesture, () => selector.WrongToolInHand || dish.CurrentStage != WashStage.AwaitingScrub, 60f);
            Assert.AreEqual(WashStage.AwaitingScrub, dish.CurrentStage, "The scraper alone is not meant to finish the pan");
            Assert.IsFalse(dish.ToolSuits(scraper), "The scraper has taken off all it can, and still 'suits'");
            Assert.IsTrue(selector.WrongToolInHand, "The tool bar never called the exhausted scraper the wrong tool");
            Assert.IsNotNull(selector.Suggested, "The bar suggests nothing");
            Assert.AreEqual(0f, selector.Suggested.ResidueFloor, "It suggested another tool that leaves a film");
            Assert.AreNotSame(scraper, selector.Suggested);
            yield return Sweep(dish, camera, gesture, () =>
            {
                Pick();
                return dish.CurrentStage != WashStage.AwaitingScrub;
            }, 60f);
            Assert.AreEqual(WashStage.ReadyForRack, dish.CurrentStage, "Following the bar's suggestions did not finish the pan");
            ToolDefinition Pick()
            {
                ToolDefinition want = dish.SuitingTool(selector.AvailableTools);
                if (want != null && !dish.ToolSuits(selector.CurrentTool))
                {
                    selector.SelectTool(want);
                }
                return want;
            }
        }

        [UnityTest]
        [Timeout(300000)]
        public IEnumerator PWS026_TheSoakAutoplay_StartedOnTheMenu_SurvivesTheRestartThatPlayMakes_AndReachesTheKitchen()
        {
            InputSettings.BackgroundBehavior savedBackground = InputSystem.settings.backgroundBehavior;
            InputSettings.EditorInputBehaviorInPlayMode savedEditorInput = InputSystem.settings.editorInputBehaviorInPlayMode;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            try
            {
                Scene previouslyActive = SceneManager.GetActiveScene();
                SceneManager.LoadScene("Boot", LoadSceneMode.Single);
                PlayDeadline bootDeadline = new PlayDeadline(10f);
                while ((SceneManager.GetActiveScene() == previouslyActive || SceneManager.GetActiveScene().name != "MainMenu") && !bootDeadline.Passed)
                {
                    yield return null;
                }
                yield return null;
                Assert.AreEqual("MainMenu", SceneManager.GetActiveScene().name);
                DevAutoplay autoplay = DevAutoplay.Begin(3f);
                bool reachedTheKitchen = false;
                PlayDeadline deadline = new PlayDeadline(90f);
                while (!deadline.Passed)
                {
                    Assert.IsTrue(DevAutoplay.Instance != null && autoplay != null, "The autoplay was destroyed when the menu's Play restarted the game");
                    if (SceneManager.GetActiveScene().name == "Kitchen_Shift")
                    {
                        reachedTheKitchen = true;
                        break;
                    }
                    yield return null;
                }
                Assert.IsTrue(reachedTheKitchen, "The autoplay never got from the menu to the kitchen in 90 s");
                Assert.IsNotNull(autoplay.Monitor, "The performance monitor did not survive the restart");
                Assert.IsTrue(autoplay.gameObject.GetComponent<SoakMonitor>() != null);
            }
            finally
            {
                if (DevAutoplay.Instance != null)
                {
                    UnityEngine.Object.Destroy(DevAutoplay.Instance.gameObject);
                }
                InputSystem.settings.backgroundBehavior = savedBackground;
                InputSystem.settings.editorInputBehaviorInPlayMode = savedEditorInput;
            }
        }

        private static void SeedADoneBigDishwasher()
        {
            BigDishwasherController.Clock = () => BigDishwasherNow;
            SaveData save = Life(5);
            save.bigDishwasherStartedUtcTicks = BigDishwasherNow.AddMinutes(-40.0).Ticks;
            for (int i = 0; i < 3; i++)
            {
                save.bigDishwasherLoad.Add("plate");
            }
            SaveSystem.Save(save);
        }

        private static IEnumerator LoadKitchen()
        {
            SceneManager.LoadScene("Kitchen_Shift", LoadSceneMode.Single);
            yield return null;
            yield return null;
        }

        [UnityTest]
        public IEnumerator PWS025_TheOwedCredit_IsSavedWithTheEmptiedMachine_AndSurvivesBeingKilledAnyNumberOfTimes()
        {
            AccessibilitySettings.RelaxedMode = true;
            try
            {
                SeedADoneBigDishwasher();
                yield return LoadKitchen();
                Assert.AreEqual(3, UnityEngine.Object.FindAnyObjectByType<BigDishwasherController>().TryUnload());
                Assert.AreEqual(3, UnityEngine.Object.FindAnyObjectByType<ShiftController>().DishesCleanedTotal);
                SaveData onDisk = SaveSystem.Load();
                Assert.AreEqual(0, onDisk.bigDishwasherLoad.Count, "The emptied machine was not saved");
                Assert.AreEqual(3, onDisk.bigDishwasherPendingDishes, "The credit was not saved with it");
                for (int kill = 1; kill <= 2; kill++)
                {
                    yield return LoadKitchen();
                    ShiftController shift = UnityEngine.Object.FindAnyObjectByType<ShiftController>();
                    Assert.AreEqual(3, shift.DishesCleanedTotal, $"After kill {kill} the three washed plates were lost");
                    Assert.AreEqual(3, shift.BigDishwasherUnloaded);
                    Assert.AreEqual(0, UnityEngine.Object.FindAnyObjectByType<BigDishwasherController>().Count, "They are also still in the machine");
                    Assert.AreEqual(3, SaveSystem.Load().bigDishwasherPendingDishes, "The credit must stay until a shift is paid");
                }
            }
            finally
            {
                BigDishwasherController.Clock = () => DateTime.UtcNow;
            }
        }

        [UnityTest]
        public IEnumerator PWS025_APaidShift_ClearsTheCredit_AndTheNextShiftDoesNotCountItAgain()
        {
            AccessibilitySettings.RelaxedMode = true;
            try
            {
                SeedADoneBigDishwasher();
                Scene previouslyActive = SceneManager.GetActiveScene();
                SceneManager.LoadScene("Boot", LoadSceneMode.Single);
                PlayDeadline deadline = new PlayDeadline(10f);
                while ((SceneManager.GetActiveScene() == previouslyActive || SceneManager.GetActiveScene().name != "MainMenu") && !deadline.Passed)
                {
                    yield return null;
                }
                yield return null;
                UnityEngine.Object.FindAnyObjectByType<MainMenuScreen>().Play(0);
                deadline = new PlayDeadline(12f);
                while ((SceneManager.GetActiveScene().name != "MetaHub" || SceneLoader.IsLoading) && !deadline.Passed)
                {
                    yield return null;
                }
                yield return null;
                Assert.IsTrue(ServiceLocator.TryGet<MetaProgressController>(out var meta));
                float walletBefore = meta.Wallet;
                yield return LoadKitchen();
                Assert.AreEqual(3, UnityEngine.Object.FindAnyObjectByType<BigDishwasherController>().TryUnload());
                Assert.AreEqual(3, SaveSystem.Load().bigDishwasherPendingDishes);
                UnityEngine.Object.FindAnyObjectByType<ShiftController>().EndShiftNow();
                yield return null;
                Assert.Greater(meta.Wallet, walletBefore + 0.5f, "Setup: the shift did not pay (three plates are worth their tips)");
                Assert.AreEqual(0, SaveSystem.Load().bigDishwasherPendingDishes, "The paid shift did not clear the credit it carried");
                yield return LoadKitchen();
                Assert.AreEqual(0, UnityEngine.Object.FindAnyObjectByType<ShiftController>().DishesCleanedTotal, "The next shift counted the paid plates again");
            }
            finally
            {
                BigDishwasherController.Clock = () => DateTime.UtcNow;
            }
        }

        [UnityTest]
        public IEnumerator PWS025_DishesTakenOutAfterTheShiftEnded_AreCreditedToTheNextOne()
        {
            AccessibilitySettings.RelaxedMode = true;
            try
            {
                SeedADoneBigDishwasher();
                yield return LoadKitchen();
                UnityEngine.Object.FindAnyObjectByType<ShiftController>().EndShiftNow();
                yield return null;
                Assert.AreEqual(3, UnityEngine.Object.FindAnyObjectByType<BigDishwasherController>().TryUnload(), "Setup: nothing came out");
                Assert.AreEqual(0, UnityEngine.Object.FindAnyObjectByType<ShiftController>().DishesCleanedTotal - UnityEngine.Object.FindAnyObjectByType<ShiftController>().ClosingLoad, "Setup: a shift that is over counted the plates");
                yield return LoadKitchen();
                Assert.AreEqual(3, UnityEngine.Object.FindAnyObjectByType<ShiftController>().DishesCleanedTotal, "The plates taken out after the shift were lost");
            }
            finally
            {
                BigDishwasherController.Clock = () => DateTime.UtcNow;
            }
        }

        [UnityTest]
        public IEnumerator PWS003_ANewGameNobodyStarted_IsNotSaved_ByThePhoneSendingTheAppAway_OrByItClosing()
        {
            Assert.IsFalse(SaveSystem.HasSave(), "Setup: a save before anyone played");
            var (meta, narrative) = LiveLife();
            yield return null;
            TheAppGoesToTheBackground(meta, narrative);
            meta.SendMessage("OnApplicationQuit", SendMessageOptions.DontRequireReceiver);
            narrative.SendMessage("OnApplicationQuit", SendMessageOptions.DontRequireReceiver);
            yield return null;
            Assert.IsFalse(SaveSystem.HasSave(), "Sending the app away made a save of a game nobody started");
            UnityEngine.Object.Destroy(meta.gameObject);
            UnityEngine.Object.Destroy(narrative.gameObject);
            yield return null;
            Assert.IsFalse(SaveSystem.HasSave(), "Closing the app made a save of a game nobody started");
            Assert.IsFalse(File.Exists(SaveSystem.FilePath));
            Assert.IsFalse(File.Exists(SaveSystem.BackupPath));
            Assert.IsNull(SaveSystem.Load(), "The menu would offer CONTINUE");
        }

        [UnityTest]
        public IEnumerator PWS003_ALifeThatExists_StillSavesWhenTheAppIsSentAway()
        {
            SaveSystem.Save(Life(12));
            var (meta, narrative) = LiveLife();
            yield return null;
            meta.Ledger.Stats.ChangeEnergy(-10f);
            float energy = meta.Energy;
            TheAppGoesToTheBackground(meta, narrative);
            Assert.AreEqual(energy, SaveSystem.Load().energy, 0.0010000000474974513, "The life in play was not flushed to disk");
        }

        [UnityTest]
        public IEnumerator PWS003_ReachingTheHub_MakesTheLife_AndFromThenOnItSavesInTheBackground()
        {
            var (meta, narrative) = LiveLife();
            yield return null;
            Assert.IsFalse(SaveSystem.HasSave());
            meta.EnsureOnDisk();
            Assert.IsTrue(SaveSystem.HasSave());
            Assert.AreEqual(1, SaveSystem.Load().dayNumber);
            meta.Ledger.Stats.ChangeEnergy(-10f);
            TheAppGoesToTheBackground(meta, narrative);
            Assert.AreEqual(meta.Energy, SaveSystem.Load().energy, 0.0010000000474974513);
        }

        [UnityTest]
        public IEnumerator PWS003_AnythingDoneInTheWeek_BeginsTheLife()
        {
            var (meta, _) = LiveLife();
            yield return null;
            Assert.IsFalse(SaveSystem.HasSave());
            Assert.IsTrue(meta.CallMom());
            Assert.IsTrue(SaveSystem.HasSave(), "A call home did not save");
            Assert.AreEqual(1, SaveSystem.Load().lastCallMomDay);
        }

        [UnityTest]
        public IEnumerator PWS003_AStoryChoice_BeginsTheLife()
        {
            NarrativeProgressController narrative = new GameObject("Narrative (QA)").AddComponent<NarrativeProgressController>();
            yield return null;
            Assert.IsFalse(SaveSystem.HasSave());
            narrative.SetFlag("ch1_first_choice");
            Assert.IsTrue(SaveSystem.HasSave(), "A story choice did not save");
            CollectionAssert.Contains(SaveSystem.Load().storyFlags, "ch1_first_choice");
        }

        [UnityTest]
        public IEnumerator PWS004_ADeletedSlotInPlay_IsNotWrittenBackByTheLifeInMemory_ByAnyCallback()
        {
            SaveSystem.Save(Life(12));
            var (meta, narrative) = LiveLife();
            yield return null;
            Assert.AreEqual(12, meta.DayNumber, "Setup: the life was not loaded");
            SaveSystem.DeleteSave();
            Assert.IsFalse(SaveSystem.HasSave(), "Setup: the slot was not deleted");
            TheAppGoesToTheBackground(meta, narrative);
            meta.CallMom();
            meta.TryRest();
            narrative.SetFlag("ch1_after_the_delete");
            narrative.LearnWord("zebra");
            yield return null;
            Assert.IsFalse(SaveSystem.HasSave(), "The deleted life came back");
            Assert.IsFalse(File.Exists(SaveSystem.FilePath), "A main file was written");
            Assert.IsFalse(File.Exists(SaveSystem.BackupPath), "A backup was written");
        }

        [UnityTest]
        public IEnumerator PWS004_DeletingAnotherSlot_DoesNotSilenceTheLifeInPlay()
        {
            SaveSystem.Save(Life(12));
            SaveSystem.Slot = 1;
            SaveSystem.Save(Life(30));
            SaveSystem.Slot = 0;
            var (meta, narrative) = LiveLife();
            yield return null;
            Assert.AreEqual(12, meta.DayNumber);
            SaveSystem.DeleteSlot(1);
            Assert.IsTrue(meta.CallMom(), "Setup: the call home did not happen");
            narrative.SetFlag("ch1_still_saving");
            SaveData saved = SaveSystem.Peek(0);
            Assert.AreEqual(12, saved.lastCallMomDay, "The life in play stopped saving after another slot was deleted");
            CollectionAssert.Contains(saved.storyFlags, "ch1_still_saving");
            Assert.IsFalse(SaveSystem.HasSave(1));
        }

        [UnityTest]
        public IEnumerator PWS004_PickingAnotherSlot_StopsTheOldLifeFromSavingIntoIt()
        {
            SaveSystem.Save(Life(12));
            var (meta, narrative) = LiveLife();
            yield return null;
            SaveSystem.Slot = 1;
            TheAppGoesToTheBackground(meta, narrative);
            meta.CallMom();
            narrative.SetFlag("ch1_wrong_slot");
            yield return null;
            Assert.IsFalse(SaveSystem.HasSave(1), "The old life wrote itself into the slot just picked");
            Assert.AreEqual(12, SaveSystem.Peek(0).dayNumber, "The first slot was touched");
            Assert.AreEqual(0, SaveSystem.Peek(0).lastCallMomDay, "The first slot got the old life's later changes");
        }

        [UnityTest]
        public IEnumerator PWS004_AfterARestartIntoTheDeletedSlot_TheNewLifeSavesNormally()
        {
            SaveSystem.Save(Life(12));
            LiveLife();
            yield return null;
            SaveSystem.DeleteSave();
            TestSaveSandbox.DestroyPersistentObjects();
            yield return null;
            SaveSystem.Slot = 0;
            var (meta, narrative) = LiveLife();
            yield return null;
            Assert.AreEqual(1, meta.DayNumber, "A deleted slot should start a new life");
            meta.EnsureOnDisk();
            narrative.SetFlag("ch1_new_life");
            meta.CallMom();
            SaveData saved = SaveSystem.Load();
            Assert.IsNotNull(saved, "The new life never saved");
            Assert.AreEqual(1, saved.dayNumber);
            Assert.AreEqual(1, saved.lastCallMomDay);
            CollectionAssert.Contains(saved.storyFlags, "ch1_new_life");
            CollectionAssert.DoesNotContain(saved.storyFlags, "ch1_complete", "The deleted life's story came back");
        }
    }
}
