using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Zmywak.Core.GameState;
using Zmywak.Core.GameState.States;
using Zmywak.Core.Save;
using Zmywak.Core.Services;
using Zmywak.Core.Settings;
using Zmywak.Kitchen.Dishwasher;
using Zmywak.Kitchen.Shifts;
using Zmywak.Meta.Progress;
using Zmywak.Narrative.Progress;
using Zmywak.UI.HUD;
using Zmywak.UI.Screens;

namespace Zmywak.Tests.PlayMode
{
    /// <summary>
    /// QA probes through the real scenes: what the phone's lifecycle and a hurried thumb can do to the saves and the day.
    /// Each asserts what SHOULD happen; a failing one is a bug found (QA_REPORT.md). The short ones run with the regular suite; two stay
    /// Explicit for their length and are run by E:\Dev\scripts\final_regression.ps1: the monkey tester (PWS-022, 2 x 110 s) and the
    /// ten-minute soak started from the hub (PWS-024; the fifteen-minute one is SoakPlayModeTests).
    /// </summary>
    [Category("QA")]
    public class QaExploratoryPlayModeTests
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
            AudioListener.pause = false;
            KitchenPauseMenu.PauseOnInterruptionOverride = null;
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
        public IEnumerator PWS003_BackgroundingTheMenuOnAFirstStart_DoesNotMintASave()
        {
            yield return Boot();
            Assert.IsFalse(SaveSystem.HasSave(0), "Setup: a save before anyone played");

            // A first-time player on the main menu pulls down the notification shade / takes a call / switches apps.
            var meta = Object.FindAnyObjectByType<MetaProgressController>();
            var narrative = Object.FindAnyObjectByType<NarrativeProgressController>();
            meta.SendMessage("OnApplicationPause", true);
            meta.SendMessage("OnApplicationFocus", false);
            narrative.SendMessage("OnApplicationPause", true);
            narrative.SendMessage("OnApplicationFocus", false);
            yield return null;

            Assert.IsFalse(SaveSystem.HasSave(0), "A save file appeared although the player never started a game");
        }

        [UnityTest]
        public IEnumerator PWS004_ADeletedSlot_StaysDeleted_WhenTheAppGoesToTheBackground()
        {
            SaveSystem.Save(new SaveData
            {
                dayNumber = 12, walletEuros = 80f, energy = 70f, currentDateIso = "2004-05-12T00:00:00",
                storyFlags = new System.Collections.Generic.List<string> { "ch1_complete" },
                seenStoryBeats = new System.Collections.Generic.List<string> { "ch1_arrival" }
            });
            yield return Boot();

            Object.FindAnyObjectByType<MainMenuScreen>().Play(0);
            yield return WaitForScene("MetaHub");
            Assert.IsTrue(ServiceLocator.TryGet(out MetaProgressController progress));
            Assert.AreEqual(12, progress.DayNumber);

            // The hub's menu button: back to the main menu WITHOUT restarting the persistent services.
            Assert.IsTrue(ServiceLocator.TryGet(out GameStateMachine machine));
            machine.ChangeState<MainMenuState>();
            yield return WaitForScene("MainMenu");

            var menu = Object.FindAnyObjectByType<MainMenuScreen>();
            menu.Delete(0);
            menu.Delete(0);
            Assert.IsFalse(SaveSystem.HasSave(0), "Setup: the slot was not deleted");

            // The player now switches to another app (or the phone sleeps).
            progress.SendMessage("OnApplicationPause", true);
            progress.SendMessage("OnApplicationFocus", false);
            Object.FindAnyObjectByType<NarrativeProgressController>().SendMessage("OnApplicationPause", true);
            yield return null;

            Assert.IsFalse(SaveSystem.HasSave(0), "The deleted life came back by itself when the app was backgrounded");
        }

        [UnityTest]
        public IEnumerator PWS011_AHurriedDoubleTapOnTheMenuQuestion_ClocksOutOnce_AndLeavesNothingFrozen()
        {
            yield return Boot();
            Object.FindAnyObjectByType<MainMenuScreen>().Play(0);
            yield return WaitForScene("MetaHub");
            Assert.IsTrue(ServiceLocator.TryGet(out MetaProgressController meta));
            int day = meta.DayNumber;

            SceneManager.LoadScene("Kitchen_Shift", LoadSceneMode.Single);
            yield return new WaitForSeconds(2f);
            var shift = Object.FindAnyObjectByType<ShiftController>();
            var pause = Object.FindAnyObjectByType<KitchenPauseMenu>();
            Assert.IsFalse(shift.IsShiftOver);

            pause.Pause();
            pause.GoToMenu();
            pause.GoToMenu(); // the thumb lands twice before the first scene change has happened
            yield return WaitForScene("MainMenu");

            Assert.AreEqual(day + 1, meta.DayNumber, "Two taps on 'yes' turned over more than one day");
            Assert.AreEqual(1f, Time.timeScale, "The game was left frozen");
            Assert.IsFalse(AudioListener.pause, "The game was left silent");
        }

        [UnityTest]
        public IEnumerator PWS019_TheShiftEndingUnderTheRepairCard_StillLetsThePlayerGoHome()
        {
            Zmywak.Core.Settings.AccessibilitySettings.RelaxedMode = true;
            SceneManager.LoadScene("Kitchen_Shift", LoadSceneMode.Single);
            yield return null;
            yield return null;

            var dishwasher = Object.FindAnyObjectByType<Zmywak.Kitchen.Dishwasher.DishwasherController>();
            var panel = Object.FindAnyObjectByType<DishwasherRepairPanel>();
            var hud = Object.FindAnyObjectByType<ShiftHUD>();
            var startButton = (UnityEngine.UI.Button)typeof(ShiftHUD).GetField("startDishwasherButton",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).GetValue(hud);
            dishwasher.BreakDown(Zmywak.Kitchen.Dishwasher.DishwasherFault.CloggedFilter);
            yield return null;
            startButton.onClick.Invoke();
            Assert.IsTrue(panel.IsOpen, "Setup: the repair card did not open");

            // The shift's clock runs out (here: clocked out) while the card is up.
            Object.FindAnyObjectByType<ShiftController>().EndShiftNow();
            var result = Object.FindAnyObjectByType<ShiftResultScreen>(FindObjectsInactive.Include);
            var deadline = new PlayDeadline(8f);
            while (!result.IsShown && !deadline.Passed)
            {
                yield return null;
            }

            Assert.IsTrue(result.IsShown, "The result screen never came up");
            yield return new WaitForSecondsRealtime(4f); // the card counts up and the button comes in

            var home = (UnityEngine.UI.Button)typeof(ShiftResultScreen).GetField("homeButton",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).GetValue(result);
            Canvas canvas = home.GetComponentInParent<Canvas>().rootCanvas;
            Camera cam = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            Vector2 screen = RectTransformUtility.WorldToScreenPoint(cam, home.transform.position);
            var hits = new System.Collections.Generic.List<UnityEngine.EventSystems.RaycastResult>();
            var events = UnityEngine.EventSystems.EventSystem.current;
            events.RaycastAll(new UnityEngine.EventSystems.PointerEventData(events) { position = screen }, hits);

            string top = hits.Count > 0 ? hits[0].gameObject.name + " (" + hits[0].gameObject.transform.root.name + ")" : "nothing";
            Assert.IsTrue(hits.Count > 0 && hits[0].gameObject.transform.IsChildOf(home.transform),
                $"The topmost thing under the 'go home' button is {top}; the repair card is {(panel.IsOpen ? "still open" : "closed")}");
            Assert.IsFalse(panel.IsOpen, "The repair card is still open over the result screen");
        }

        [UnityTest, Explicit("PWS-022")]
        [Timeout(900_000)]
        public IEnumerator PWS022_MonkeyTester_RandomTouchesToolSpamAndPauses_BreakNothing([Values(11, 22)] int seed)
        {
            // Input from a virtual mouse reaches the game although the batch-mode Editor has no focus (as the soak test does).
            var savedBackground = UnityEngine.InputSystem.InputSystem.settings.backgroundBehavior;
            var savedEditorInput = UnityEngine.InputSystem.InputSystem.settings.editorInputBehaviorInPlayMode;
            UnityEngine.InputSystem.InputSystem.settings.backgroundBehavior = UnityEngine.InputSystem.InputSettings.BackgroundBehavior.IgnoreFocus;
            UnityEngine.InputSystem.InputSystem.settings.editorInputBehaviorInPlayMode =
                UnityEngine.InputSystem.InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            UnityEngine.InputSystem.Mouse mouse = UnityEngine.InputSystem.InputSystem.AddDevice<UnityEngine.InputSystem.Mouse>("QaMonkeyMouse");
            var rng = new System.Random(seed);
            MonkeyHeartbeat heartbeat = null;
            try
            {
                SceneManager.LoadScene("Kitchen_Shift", LoadSceneMode.Single);
                yield return null;
                yield return null;
                var shift = Object.FindAnyObjectByType<ShiftController>();
                var pause = Object.FindAnyObjectByType<KitchenPauseMenu>();
                var result = Object.FindAnyObjectByType<ShiftResultScreen>(FindObjectsInactive.Include);
                Assert.IsNotNull(shift);
                Time.timeScale = MonkeyTimeScale; // a 3-minute shift in about a minute

                Vector2 pointer = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
                bool down = false;
                void Set(Vector2 p, bool press)
                {
                    pointer = new Vector2(Mathf.Clamp(p.x, 0, Screen.width - 1), Mathf.Clamp(p.y, 0, Screen.height - 1));
                    var state = new UnityEngine.InputSystem.LowLevel.MouseState { position = pointer };
                    UnityEngine.InputSystem.InputSystem.QueueStateEvent(mouse,
                        press ? state.WithButton(UnityEngine.InputSystem.LowLevel.MouseButton.Left) : state);
                    down = press;
                }

                Vector2 Random2() => new Vector2((float)rng.NextDouble() * Screen.width, (float)rng.NextDouble() * Screen.height);

                var problems = new System.Collections.Generic.List<string>();
                var deadline = new PlayDeadline(110f);
                float nextCheck = 0f;
                int actions = 0;
                int lastCase = -1;
                // A stuck monkey is otherwise silent until the test's own timeout; this says what it was doing and what state the game was in.
                heartbeat = new GameObject("QaMonkeyHeartbeat").AddComponent<MonkeyHeartbeat>();
                heartbeat.Describe = () => $"seed {seed}, {actions} actions, last case {lastCase}, timeScale {Time.timeScale}, " +
                                           $"paused {(pause != null && pause.IsPaused)}, shift over {shift.IsShiftOver}, deadline passed {deadline.Passed}";
                while (!deadline.Passed)
                {
                    actions++;
                    lastCase = rng.Next(7);
                    switch (lastCase)
                    {
                        case 0: // a tap
                            Set(Random2(), true);
                            yield return null;
                            Set(pointer, false);
                            break;
                        case 1: // a drag
                        {
                            Vector2 from = Random2();
                            Vector2 to = Random2();
                            int frames = rng.Next(2, 40);
                            Set(from, true);
                            for (int f = 1; f <= frames; f++)
                            {
                                yield return null;
                                Set(Vector2.Lerp(from, to, f / (float)frames), true);
                            }

                            Set(pointer, false);
                            break;
                        }
                        case 2: // a scrub: back and forth on one spot
                        {
                            Vector2 centre = Random2();
                            Set(centre, true);
                            for (int f = 0; f < rng.Next(10, 70); f++)
                            {
                                yield return null;
                                Set(centre + new Vector2(Mathf.Sin(f * 0.9f) * 90f, Mathf.Cos(f * 0.7f) * 60f), true);
                            }

                            Set(pointer, false);
                            break;
                        }
                        case 3: // a long hold (rinse, soak, a finger left on the glass)
                            Set(Random2(), true);
                            yield return new WaitForSecondsRealtime((float)rng.NextDouble() * 1.5f / MonkeyTimeScale);
                            Set(pointer, false);
                            break;
                        case 4: // tool spam, several changes in a frame
                            if (Zmywak.Kitchen.Tools.ActiveToolSelector.Instance != null)
                            {
                                var tools = Zmywak.Kitchen.Tools.ActiveToolSelector.Instance.AllTools;
                                for (int k = 0; k < rng.Next(1, 8); k++)
                                {
                                    Zmywak.Kitchen.Tools.ActiveToolSelector.Instance.SelectTool(tools[rng.Next(tools.Count)]);
                                }
                            }

                            break;
                        case 5: // pause and resume, sometimes with the finger down
                            if (pause != null && !shift.IsShiftOver)
                            {
                                pause.Pause();
                                yield return null;
                                if (rng.Next(2) == 0)
                                {
                                    pause.Resume();
                                }
                                else
                                {
                                    pause.OnBack();
                                }
                            }

                            break;
                        default: // let the game run
                            yield return new WaitForSecondsRealtime((float)rng.NextDouble() * 0.4f / MonkeyTimeScale);
                            break;
                    }

                    yield return null;
                    if (Time.realtimeSinceStartup >= nextCheck)
                    {
                        nextCheck = Time.realtimeSinceStartup + 1f;
                        foreach (Zmywak.Kitchen.Dishes.DishController dish in Object.FindObjectsByType<Zmywak.Kitchen.Dishes.DishController>())
                        {
                            Vector3 p = dish.transform.position;
                            if (float.IsNaN(p.x + p.y + p.z) || float.IsInfinity(p.x + p.y + p.z) || p.y < -20f || Mathf.Abs(p.x) > 30f || Mathf.Abs(p.z) > 30f)
                            {
                                problems.Add($"t={Time.timeSinceLevelLoad:0}s a dish ({dish.Definition.Id}, {dish.CurrentStage}) is at {p}");
                            }
                        }
                    }
                }

                Set(pointer, false);
                yield return null;
                if (pause != null && pause.IsPaused)
                {
                    pause.Resume();
                }

                Time.timeScale = 1f;
                if (!shift.IsShiftOver)
                {
                    shift.EndShiftNow();
                }

                var closing = new PlayDeadline(8f);
                while (!result.IsShown && !closing.Passed)
                {
                    yield return null;
                }

                Debug.Log($"QA PWS-022 seed {seed}: {actions} random actions; {problems.Count} problems");
                Assert.IsTrue(result.IsShown, "After the monkey the result screen never showed");
                Assert.IsEmpty(problems, string.Join("\n", problems));
            }
            finally
            {
                if (heartbeat != null)
                {
                    Object.Destroy(heartbeat.gameObject);
                }

                if (mouse != null && mouse.added)
                {
                    UnityEngine.InputSystem.InputSystem.RemoveDevice(mouse);
                }

                UnityEngine.InputSystem.InputSystem.settings.backgroundBehavior = savedBackground;
                UnityEngine.InputSystem.InputSystem.settings.editorInputBehaviorInPlayMode = savedEditorInput;
            }
        }

        /// <summary>
        /// The speed the monkey plays at. Its waits are in real time, game seconds divided by this: the game can be paused while the monkey
        /// plays (Time.timeScale 0 - a tap on the pause button is one way), and a wait in game time would then never end - seed 22 stood
        /// still for 8 minutes at 510 actions, paused (2026-10-08, see QA_REPORT.md section 12.3), until the test's own timeout.
        /// </summary>
        private const float MonkeyTimeScale = 3f;

        /// <summary>One line every few real seconds while the monkey runs (PWS-022): what it was last doing, and the state of the game.</summary>
        private sealed class MonkeyHeartbeat : MonoBehaviour
        {
            public System.Func<string> Describe;
            private float _next;

            private void Update()
            {
                if (Describe == null || Time.realtimeSinceStartup < _next)
                {
                    return;
                }

                _next = Time.realtimeSinceStartup + 5f;
                Debug.Log("QA PWS-022 heartbeat: " + Describe());
            }
        }

        [UnityTest]
        [Timeout(300_000)]
        public IEnumerator PWS023_AFreshGame_CanBePlayedFromTheHubToTheKitchen_ByWhatEverIsOnScreen()
        {
            yield return Boot();
            Object.FindAnyObjectByType<MainMenuScreen>().Play(0);
            yield return WaitForScene("MetaHub");

            string Dump()
            {
                string Btn(string name)
                {
                    GameObject go = GameObject.Find(name);
                    var b = go != null ? go.GetComponent<UnityEngine.UI.Button>() : null;
                    return b == null ? $"{name}:-" : $"{name}:{(b.IsInteractable() ? "ok" : "locked")}";
                }

                bool dialogue = GameObject.Find("DialoguePanel") != null;
                var beat = Object.FindAnyObjectByType<StoryBeatTrigger>();
                return $"t={Time.timeSinceLevelLoad:0.0} dialogue={dialogue} takesTaps={(beat != null ? beat.ActionsTakeTaps.ToString() : "?")} " +
                       $"loading={SceneLoader.IsLoading} timeScale={Time.timeScale} {Btn("ContinueButton")} {Btn("Choice_0")} {Btn("GoToWorkButton")} {Btn("RestButton")}";
            }

            var log = new System.Text.StringBuilder();
            var deadline = new PlayDeadline(90f);
            float nextDump = 0f;
            bool reachedKitchen = false;
            while (!deadline.Passed)
            {
                if (Time.realtimeSinceStartup >= nextDump)
                {
                    nextDump = Time.realtimeSinceStartup + 2f;
                    log.AppendLine(Dump());
                }

                if (SceneManager.GetActiveScene().name == "Kitchen_Shift")
                {
                    reachedKitchen = true;
                    break;
                }

                // what a player does: Continue / the first choice while a scene is on, else rest and go to work
                var cont = GameObject.Find("ContinueButton")?.GetComponent<UnityEngine.UI.Button>();
                var choice = GameObject.Find("Choice_0")?.GetComponent<UnityEngine.UI.Button>();
                var work = GameObject.Find("GoToWorkButton")?.GetComponent<UnityEngine.UI.Button>();
                if (cont != null && cont.IsInteractable())
                {
                    cont.onClick.Invoke();
                }
                else if (choice != null && choice.IsInteractable())
                {
                    choice.onClick.Invoke();
                }
                else if (work != null && work.IsInteractable() && GameObject.Find("DialoguePanel") == null)
                {
                    work.onClick.Invoke();
                }

                yield return new WaitForSeconds(0.3f);
            }

            Debug.Log("QA PWS-023 state dump:\n" + log);
            Assert.IsTrue(reachedKitchen, "A fresh game never got from the hub to the kitchen in 90 s of tapping what is on screen. Last state: " + Dump());
        }

        [UnityTest, Explicit("PWS-024")]
        [Timeout(1_800_000)]
        public IEnumerator PWS024_TenMinuteSoak_StartedFromTheHub_ShiftAfterShift()
        {
            var savedBackground = UnityEngine.InputSystem.InputSystem.settings.backgroundBehavior;
            var savedEditorInput = UnityEngine.InputSystem.InputSystem.settings.editorInputBehaviorInPlayMode;
            UnityEngine.InputSystem.InputSystem.settings.backgroundBehavior = UnityEngine.InputSystem.InputSettings.BackgroundBehavior.IgnoreFocus;
            UnityEngine.InputSystem.InputSystem.settings.editorInputBehaviorInPlayMode =
                UnityEngine.InputSystem.InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            const float minutes = 10f;
            try
            {
                yield return Boot();
                // The menu's Play restarts the game (StartFromAnyScene) and would take the autoplay with it: start it in the hub.
                Object.FindAnyObjectByType<MainMenuScreen>().Play(0);
                yield return WaitForScene("MetaHub");
                Zmywak.UI.Dev.DevAutoplay autoplay = Zmywak.UI.Dev.DevAutoplay.Begin(minutes);
                var deadline = new PlayDeadline(minutes * 60f + 240f);
                while (!autoplay.Finished && !deadline.Passed)
                {
                    yield return null;
                }

                var samples = Zmywak.Core.Telemetry.GameplayLog.Read<Zmywak.Core.Telemetry.SoakLogEntry>("soak");
                var shifts = Zmywak.Core.Telemetry.GameplayLog.Read<Zmywak.Core.Telemetry.ShiftLogEntry>("shift");
                var table = new System.Text.StringBuilder($"Soak from the hub, {minutes:0} min: {autoplay.ShiftsPlayed} shifts, {autoplay.DishesRacked} dishes racked, {autoplay.RepairsDone} repairs\n");
                table.AppendLine("minute  scene          fps   worst ms  slow   alloc MB  managed MB  textures  meshes");
                foreach (var s in samples)
                {
                    table.AppendLine($"{s.minute,6:0.0}  {s.scene,-13} {s.averageFps,5:0} {s.worstFrameMs,9:0} {s.slowFrameShare,5:P0} {s.allocatedMB,9:0} {s.managedMB,11:0} {s.textures,9} {s.meshes,7}");
                }

                table.AppendLine("shift  minutes  arrived  cleaned  broken  cycles  breakdowns  repairs  fps");
                for (int i = 0; i < shifts.Count; i++)
                {
                    var s = shifts[i];
                    table.AppendLine($"{i + 1,5}  {s.elapsedSeconds / 60f,7:0.0}  {s.dishesArrived,7}  {s.dishesCleaned,7}  {s.dishesBroken,6}  {s.dishwasherCycles,6}  {s.breakdowns,10}  {s.repairs.Count,7}  {s.averageFps,3:0}");
                }

                Debug.Log("QA PWS-024\n" + table);
                Assert.IsTrue(autoplay.Finished, "The soak never finished\n" + table);
                Assert.IsNull(autoplay.LastProblem);
                Assert.Greater(autoplay.ShiftsPlayed, 0, "Not one shift was played\n" + table);
            }
            finally
            {
                if (Zmywak.UI.Dev.DevAutoplay.Instance != null)
                {
                    Object.Destroy(Zmywak.UI.Dev.DevAutoplay.Instance.gameObject);
                }

                UnityEngine.InputSystem.InputSystem.settings.backgroundBehavior = savedBackground;
                UnityEngine.InputSystem.InputSystem.settings.editorInputBehaviorInPlayMode = savedEditorInput;
            }
        }

        [UnityTest]
        public IEnumerator PWS025_DishesTakenOutOfTheBigDishwasher_AreNotLost_WhenTheAppIsKilledBeforeTheShiftIsPaid()
        {
            AccessibilitySettings.RelaxedMode = true;
            var now = new System.DateTime(2026, 10, 1, 18, 0, 0, System.DateTimeKind.Utc);
            BigDishwasherController.Clock = () => now;
            try
            {
                // Forty minutes into a half-hour cycle: three clean plates waiting in the machine.
                var save = new SaveData { bigDishwasherStartedUtcTicks = now.AddMinutes(-40).Ticks };
                for (int i = 0; i < 3; i++)
                {
                    save.bigDishwasherLoad.Add("plate");
                }

                SaveSystem.Save(save);
                SceneManager.LoadScene("Kitchen_Shift", LoadSceneMode.Single);
                yield return null;
                yield return null;
                var machine = Object.FindAnyObjectByType<BigDishwasherController>();
                var shift = Object.FindAnyObjectByType<ShiftController>();
                Assert.AreEqual(3, machine.Count, "Setup: the machine does not hold the plates");

                Assert.AreEqual(3, machine.TryUnload(), "Setup: the clean load did not come out");
                Assert.AreEqual(3, shift.DishesCleanedTotal, "Setup: the plates did not count as washed this shift");

                // The OS kills the app mid-shift: no end of shift, nothing paid. The game is opened again.
                yield return null;
                SceneManager.LoadScene("Kitchen_Shift", LoadSceneMode.Single);
                yield return null;
                yield return null;
                var machineAfter = Object.FindAnyObjectByType<BigDishwasherController>();
                var shiftAfter = Object.FindAnyObjectByType<ShiftController>();

                bool stillInTheMachine = machineAfter.Count == 3;
                bool creditedToTheNewShift = shiftAfter.DishesCleanedTotal >= 3;
                Assert.IsTrue(stillInTheMachine || creditedToTheNewShift,
                    $"Three washed plates vanished: the machine holds {machineAfter.Count}, the new shift counts {shiftAfter.DishesCleanedTotal} washed, " +
                    "and the shift they were taken out in never paid for them");
            }
            finally
            {
                BigDishwasherController.Clock = () => System.DateTime.UtcNow;
            }
        }

        [UnityTest]
        [Timeout(300_000)]
        public IEnumerator PWS026_TheSoakAutoplay_StartedOnTheMenu_SurvivesPlayRestartingTheGame_AndReachesTheKitchen()
        {
            var savedBackground = UnityEngine.InputSystem.InputSystem.settings.backgroundBehavior;
            var savedEditorInput = UnityEngine.InputSystem.InputSystem.settings.editorInputBehaviorInPlayMode;
            UnityEngine.InputSystem.InputSystem.settings.backgroundBehavior = UnityEngine.InputSystem.InputSettings.BackgroundBehavior.IgnoreFocus;
            UnityEngine.InputSystem.InputSystem.settings.editorInputBehaviorInPlayMode =
                UnityEngine.InputSystem.InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            try
            {
                yield return Boot();
                Zmywak.UI.Dev.DevAutoplay autoplay = Zmywak.UI.Dev.DevAutoplay.Begin(3f);
                bool reachedTheKitchen = false;
                bool diedOnTheWay = false;
                var deadline = new PlayDeadline(90f);
                while (!deadline.Passed)
                {
                    if (Zmywak.UI.Dev.DevAutoplay.Instance == null || autoplay == null)
                    {
                        diedOnTheWay = true; // the menu's Play restarted the game and took the autoplay with it
                        break;
                    }

                    if (SceneManager.GetActiveScene().name == "Kitchen_Shift")
                    {
                        reachedTheKitchen = true;
                        break;
                    }

                    yield return null;
                }

                Assert.IsFalse(diedOnTheWay, "The autoplay was destroyed when the menu's Play restarted the game");
                Assert.IsTrue(reachedTheKitchen, "The autoplay never got from the menu to the kitchen in 90 s");
                Assert.IsNotNull(Zmywak.UI.Dev.DevAutoplay.Instance.Monitor, "The performance monitor did not survive the restart");
                yield return new WaitForSeconds(2f);
                Assert.IsNotNull(Zmywak.UI.Dev.DevAutoplay.Instance, "The autoplay is gone after reaching the kitchen");
            }
            finally
            {
                if (Zmywak.UI.Dev.DevAutoplay.Instance != null)
                {
                    Object.Destroy(Zmywak.UI.Dev.DevAutoplay.Instance.gameObject);
                }

                UnityEngine.InputSystem.InputSystem.settings.backgroundBehavior = savedBackground;
                UnityEngine.InputSystem.InputSystem.settings.editorInputBehaviorInPlayMode = savedEditorInput;
            }
        }

        [UnityTest]
        public IEnumerator PWS012_SpammingPauseResumeAndBack_AlwaysEndsRunning()
        {
            yield return Boot();
            Object.FindAnyObjectByType<MainMenuScreen>().Play(0);
            yield return WaitForScene("MetaHub");

            SceneManager.LoadScene("Kitchen_Shift", LoadSceneMode.Single);
            yield return new WaitForSeconds(2f);
            var pause = Object.FindAnyObjectByType<KitchenPauseMenu>();
            KitchenPauseMenu.PauseOnInterruptionOverride = true;

            for (int i = 0; i < 300; i++)
            {
                switch (i % 6)
                {
                    case 0: pause.Pause(); break;
                    case 1: pause.Pause(); break;
                    case 2: pause.OnBack(); break;
                    case 3: pause.SendMessage("OnApplicationPause", true); break;
                    case 4: pause.SendMessage("OnApplicationFocus", false); break;
                    default: pause.Resume(); break;
                }
            }

            pause.Resume();
            yield return null;
            Assert.AreEqual(1f, Time.timeScale, "Time did not come back after pause/resume spam");
            Assert.IsFalse(AudioListener.pause);
            Assert.IsFalse(pause.IsPaused);
            float before = Time.timeSinceLevelLoad;
            yield return new WaitForSeconds(0.5f);
            Assert.Greater(Time.timeSinceLevelLoad - before, 0.3f, "The clock is not running");
        }
    }
}
