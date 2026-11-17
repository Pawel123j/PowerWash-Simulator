using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Zmywak.Core.Audio;
using Zmywak.Core.GameState;
using Zmywak.Core.Graphics;
using Zmywak.Core.Settings;
using Zmywak.Kitchen.Shifts;
using Zmywak.UI.Accessibility;

namespace Zmywak.Tests.PlayMode
{
    /// <summary>
    /// The way between the screens and what stays across them: two quick taps on a way out load the scene once; going back
    /// and forth leaves one of each of the game's persistent objects, one listener and no pile of sound sources; the menu and
    /// the hub run at the calm frame rate and the kitchen at its tier's, calming down again while paused; and every easing
    /// setting switched on at once (relaxed, no vibration, the largest text, no screen effects, the sound off) still plays.
    /// </summary>
    public class ScenesAndSettingsPlayModeTests
    {
        [SetUp]
        public void SetUp()
        {
            TestSaveSandbox.Enter();
        }

        [TearDown]
        public void TearDown()
        {
            Time.timeScale = 1f;
            TestSaveSandbox.DestroyPersistentObjects();
            TestSaveSandbox.Exit();
        }

        [UnityTest]
        public IEnumerator TwoQuickTaps_OnTheWayOut_LoadTheSceneOnce()
        {
            yield return Boot();
            int hubLoads = 0;
            void Count(string scene)
            {
                if (scene == "MetaHub")
                {
                    hubLoads++;
                }
            }

            SceneLoader.Loaded += Count;
            try
            {
                SceneLoader.Load("MetaHub");
                SceneLoader.Load("MetaHub");
                yield return null;
                SceneLoader.Load("MetaHub");
                yield return WaitForLoader();
                yield return new WaitForSecondsRealtime(1f); // a second load queued behind the first would be under way by now
                Assert.IsFalse(SceneLoader.IsLoading, "Another load followed the first");
                Assert.AreEqual(1, hubLoads, "The hub was loaded more than once for one way out");
                Assert.AreEqual("MetaHub", SceneManager.GetActiveScene().name);
            }
            finally
            {
                SceneLoader.Loaded -= Count;
            }
        }

        [UnityTest]
        public IEnumerator GoingBackAndForth_LeavesOneOfEachPersistentObject_AndTheSoundsDoNotPileUp()
        {
            yield return Boot();
            int sourcesAfterFirstRound = -1;
            for (int round = 0; round < 3; round++)
            {
                SceneLoader.Load("MetaHub");
                yield return WaitForLoader();
                SceneLoader.Load("MainMenu");
                yield return WaitForLoader();
                yield return new WaitForSecondsRealtime(3f); // the hub's loops fade out, the menu's in

                Assert.AreEqual(1, Object.FindObjectsByType<GameAudio>(FindObjectsInactive.Include).Length, "Two sound players");
                Assert.AreEqual(1, Object.FindObjectsByType<SceneLoader>(FindObjectsInactive.Include).Length, "Two scene loaders");
                Assert.AreEqual(1, Object.FindObjectsByType<QualityManager>(FindObjectsInactive.Include).Length, "Two quality managers");
                Assert.AreEqual(1, Object.FindObjectsByType<AudioListener>(FindObjectsInactive.Exclude).Count(listener => listener.enabled),
                    "Not exactly one ear in the scene");

                int sources = GameAudio.Instance.GetComponents<AudioSource>().Length;
                if (round == 0)
                {
                    sourcesAfterFirstRound = sources;
                }
                else
                {
                    Assert.LessOrEqual(sources, sourcesAfterFirstRound, $"Sound sources piled up: {sources} after round {round + 1}");
                }
            }

            Assert.IsTrue(GameAudio.IsLooping("music"), "The menu's music is not playing after coming back");
        }

        [UnityTest]
        public IEnumerator TheMenuAndHubRunCalm_TheKitchenAtItsTier_AndAPausedShiftCalmsDown()
        {
            yield return Boot();
            yield return null;
            Assert.AreEqual(QualityManager.CalmFrameRate, Application.targetFrameRate, "The menu does not run at the calm rate");

            SceneLoader.Load("MetaHub");
            yield return WaitForLoader();
            yield return null;
            Assert.AreEqual(QualityManager.CalmFrameRate, Application.targetFrameRate, "The hub does not run at the calm rate");

            SceneLoader.Load(QualityManager.KitchenScene);
            yield return WaitForLoader();
            yield return null;
            yield return null;
            Assert.AreEqual(QualityGovernor.TargetFrameRate(QualityManager.ActiveTier), Application.targetFrameRate,
                "The kitchen does not run at its tier's rate");
            Assert.GreaterOrEqual(Application.targetFrameRate, QualityManager.CalmFrameRate);

            Time.timeScale = 0f; // paused
            yield return null;
            yield return null;
            Assert.AreEqual(QualityManager.CalmFrameRate, Application.targetFrameRate, "A paused shift still runs at the kitchen's rate");
            Time.timeScale = 1f;
            yield return null;
            yield return null;
            Assert.AreEqual(QualityGovernor.TargetFrameRate(QualityManager.ActiveTier), Application.targetFrameRate);
        }

        [UnityTest]
        public IEnumerator EveryEasingSettingAtOnce_TheMenuHubAndKitchenStillPlay()
        {
            AccessibilitySettings.RelaxedMode = true;
            AccessibilitySettings.HapticsEnabled = false;
            AccessibilitySettings.TextScale = 1.5f;
            AccessibilitySettings.ScreenEffects = false;
            AccessibilitySettings.SoundEnabled = false;
            AccessibilitySettings.ColorBlindMode = true;
            AccessibilitySettings.ToolAssist = true;
            int oneShots = GameAudio.OneShotsPlayed;

            yield return Boot();
            SceneLoader.Load("MetaHub");
            yield return WaitForLoader();
            yield return null;
            TextScaler scaler = Object.FindObjectsByType<TextScaler>(FindObjectsInactive.Exclude).FirstOrDefault();
            Assert.IsNotNull(scaler, "The hub's texts do not follow the text size");

            SceneLoader.Load(QualityManager.KitchenScene);
            yield return WaitForLoader();
            var shift = Object.FindAnyObjectByType<ShiftController>();
            Assert.IsNotNull(shift);
            Time.timeScale = 5f;
            var deadline = new PlayDeadline(10f);
            while (shift.DishesSpawnedTotal < 3 && !deadline.Passed)
            {
                yield return null;
            }

            Time.timeScale = 1f;
            Assert.GreaterOrEqual(shift.DishesSpawnedTotal, 3, "The kitchen did not run with every easing setting on");
            Assert.AreEqual(oneShots, GameAudio.OneShotsPlayed, "A sound played with the sound switched off");
            Assert.IsTrue(AccessibilitySettings.RelaxedMode && !AccessibilitySettings.HapticsEnabled && !AccessibilitySettings.SoundEnabled,
                "A setting changed on its own along the way");
            Assert.AreEqual(1.5f, AccessibilitySettings.TextScale, 1e-4f);
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

            yield return WaitForLoader();
            Assert.AreEqual("MainMenu", SceneManager.GetActiveScene().name);
        }

        private static IEnumerator WaitForLoader()
        {
            yield return null;
            var deadline = new PlayDeadline(15f);
            while (SceneLoader.IsLoading && !deadline.Passed)
            {
                yield return null;
            }

            Assert.IsFalse(SceneLoader.IsLoading, "A load never finished");
            yield return null;
        }
    }
}
