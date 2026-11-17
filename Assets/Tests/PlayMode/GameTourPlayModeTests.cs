using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Zmywak.UI.Dev;

namespace Zmywak.Tests.PlayMode
{
    /// <summary>
    /// Not a check - the whole game in pictures, as a player sees it. From the start screen on a fresh save, the game
    /// plays itself (DevAutoplay, the soak test's driver, through the real input path): the menu, the first evening's
    /// story, the evening's choices, a whole shift in the kitchen with whatever it brings (the chef's orders, a
    /// breakdown, the rack, the dishwasher), the result, and on into the next evening. A phone-shaped screenshot is
    /// taken every few seconds - more often while the story is on. Explicit; Logs/Screenshots/tour_*.png.
    /// </summary>
    public class GameTourPlayModeTests
    {
        private const float StoryEvery = 1.5f;
        private const float KitchenEvery = 4f;
        private const float OtherEvery = 2.5f;
        private const float SettleAfterSceneChange = 1.2f;
        private const float KeepWatchingAtHome = 20f;

        [SetUp]
        public void SetUp()
        {
            TestSaveSandbox.Enter(); // day one, the story from its start; the real save untouched
        }

        [TearDown]
        public void TearDown()
        {
            if (DevAutoplay.Instance != null)
            {
                Object.Destroy(DevAutoplay.Instance.gameObject);
            }

            TestSaveSandbox.DestroyPersistentObjects();
            TestSaveSandbox.Exit();
        }

        [UnityTest, Explicit, Timeout(1200000)]
        public IEnumerator Tour_TheWholeGame_InPictures()
        {
            // A headless Editor never has focus: let the autoplay's virtual mouse through anyway.
            InputSettings.BackgroundBehavior background = InputSystem.settings.backgroundBehavior;
            InputSettings.EditorInputBehaviorInPlayMode editorInput = InputSystem.settings.editorInputBehaviorInPlayMode;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            try
            {
                SceneManager.LoadScene("Boot", LoadSceneMode.Single);
                float started = Time.realtimeSinceStartup;
                float nextShot = started + SettleAfterSceneChange;
                string lastScene = string.Empty;
                int shot = 0;
                float homeSince = -1f;
                DevAutoplay autoplay = null;

                while (Time.realtimeSinceStartup - started < 1100f)
                {
                    yield return null;
                    float now = Time.realtimeSinceStartup;
                    string scene = SceneManager.GetActiveScene().name;
                    if (autoplay == null && now - started > 2.5f)
                    {
                        autoplay = DevAutoplay.Begin(16f);
                    }

                    if (scene != lastScene)
                    {
                        lastScene = scene;
                        nextShot = now + SettleAfterSceneChange; // let the new scene come in first
                    }

                    if (now >= nextShot && Camera.main != null)
                    {
                        TestScreenshot.Capture(Camera.main, $"tour_{shot++:000}_{scene}.png", 1080, 2340);
                        bool story = GameObject.Find("DialoguePanel") != null;
                        nextShot = now + (story ? StoryEvery : scene == "Kitchen_Shift" ? KitchenEvery : OtherEvery);
                    }

                    // After a shift, a while of the next evening, then stop.
                    if (autoplay != null && autoplay.ShiftsPlayed >= 1 && scene == "MetaHub")
                    {
                        homeSince = homeSince < 0f ? now : homeSince;
                        if (now - homeSince > KeepWatchingAtHome)
                        {
                            break;
                        }
                    }
                }

                Assert.Greater(shot, 0, "No pictures were taken");
                Debug.Log($"Tour: {shot} pictures, {autoplay?.ShiftsPlayed} shift(s), {autoplay?.DishesRacked} dishes racked, " +
                          $"{autoplay?.RepairsDone} repairs; last problem: {autoplay?.LastProblem ?? "none"}");
            }
            finally
            {
                InputSystem.settings.backgroundBehavior = background;
                InputSystem.settings.editorInputBehaviorInPlayMode = editorInput;
            }
        }
    }
}
