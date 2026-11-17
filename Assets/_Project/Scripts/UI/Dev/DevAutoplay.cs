using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Zmywak.Core.Diagnostics;
using Zmywak.Core.Data;
using Zmywak.Core.GameState;
using Zmywak.Kitchen.Dishes;
using Zmywak.Kitchen.Dishwasher;
using Zmywak.Kitchen.Prep;
using Zmywak.Kitchen.Rack;
using Zmywak.Kitchen.Shifts;
using Zmywak.Kitchen.Tools;
using Zmywak.UI.HUD;

namespace Zmywak.UI.Dev
{
    /// <summary>
    /// Development builds only: the game plays itself for a soak test - shift after shift with the evening in
    /// between - through the real input path (a virtual mouse fed to the Input System, the way a finger's touches
    /// arrive), while a <see cref="SoakMonitor"/> records how it runs. Started on a phone by the launch intent
    /// (<c>--ez zmywak_autoplay true --ei zmywak_soak_minutes 15</c>, see Tools/phone_soak.ps1), elsewhere by
    /// <c>-zmywakAutoplay [minutes]</c> on the command line or <see cref="Begin"/>. A release build ignores all of it.
    /// </summary>
    public class DevAutoplay : MonoBehaviour, IKeepAcrossRestart // the menu's Play restarts the game; the soak (and its monitor, on this object) must go on through it
    {
        public const string IntentFlag = "zmywak_autoplay";
        public const string IntentMinutes = "zmywak_soak_minutes";
        public const string CommandLineFlag = "-zmywakAutoplay";
        private const float DefaultMinutes = 15f;
        private const float SwipePixels = 120f;

        private Mouse _mouse;
        private float _endsAt;
        private Vector2 _pointer;

        public static DevAutoplay Instance { get; private set; }
        public SoakMonitor Monitor { get; private set; }
        public bool Finished { get; private set; }
        public int ShiftsPlayed { get; private set; }
        public int DishesRacked { get; private set; }
        public int RepairsDone { get; private set; }
        public string LastProblem { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void StartIfRequested()
        {
            if (Debug.isDebugBuild && Application.isPlaying && IsRequested(out float minutes))
            {
                Begin(minutes);
            }
        }

        public static DevAutoplay Begin(float minutes)
        {
            if (Instance != null)
            {
                return Instance;
            }

            var go = new GameObject("DevAutoplay");
            DontDestroyOnLoad(go);
            Instance = go.AddComponent<DevAutoplay>();
            Instance.Monitor = go.AddComponent<SoakMonitor>();
            Instance._endsAt = Time.realtimeSinceStartup + minutes * 60f;
            Debug.Log($"[Soak] Autoplay for {minutes:0.#} min on {SystemInfo.deviceModel}, {SystemInfo.operatingSystem}, " +
                      $"{SystemInfo.graphicsDeviceName}, {Screen.width}x{Screen.height} @ {Screen.dpi:0} dpi");
            return Instance;
        }

        private static bool IsRequested(out float minutes)
        {
            minutes = DefaultMinutes;
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == CommandLineFlag)
                {
                    if (i + 1 < args.Length && float.TryParse(args[i + 1], System.Globalization.NumberStyles.Float,
                            System.Globalization.CultureInfo.InvariantCulture, out float parsed))
                    {
                        minutes = parsed;
                    }

                    return true;
                }
            }

#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (AndroidJavaObject activity = player.GetStatic<AndroidJavaObject>("currentActivity"))
                using (AndroidJavaObject intent = activity.Call<AndroidJavaObject>("getIntent"))
                {
                    if (intent != null && intent.Call<bool>("getBooleanExtra", IntentFlag, false))
                    {
                        minutes = intent.Call<int>("getIntExtra", IntentMinutes, (int)DefaultMinutes);
                        return true;
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Soak] Could not read the launch intent: {e.Message}");
            }
#endif
            return false;
        }

        private void Start()
        {
            _mouse = InputSystem.AddDevice<Mouse>("DevAutoplayMouse");
            // A phone lying on the table cannot tilt: let the reflection sweep by itself, as without a sensor.
            DishController.SuspendTilt = true;
            StartCoroutine(Run());
        }

        private void OnDestroy()
        {
            DishController.SuspendTilt = false;
            if (_mouse != null && _mouse.added)
            {
                InputSystem.RemoveDevice(_mouse);
            }

            if (Instance == this)
            {
                Instance = null;
            }
        }

        private bool TimeIsUp => Time.realtimeSinceStartup >= _endsAt;
        private static bool Paused => Time.timeScale == 0f;

        private IEnumerator Run()
        {
            while (!TimeIsUp)
            {
                string scene = SceneManager.GetActiveScene().name;
                if (scene == "Kitchen_Shift")
                {
                    yield return PlayShift();
                }
                else if (scene == "MetaHub")
                {
                    yield return SpendTheEvening();
                }
                else if (scene == "MainMenu")
                {
                    // CONTINUE when there is a life to go on with, else a new one.
                    if (!Click("MenuContinueButton"))
                    {
                        Click("NewGameButton");
                    }

                    yield return WaitForSceneChange(scene);
                }
                else
                {
                    yield return null;
                }
            }

            Release();
            Monitor.Sample();
            Finished = true;
            Debug.Log($"[Soak] Done: {ShiftsPlayed} shifts, {DishesRacked} dishes racked, {RepairsDone} repairs, " +
                      $"{Monitor.First?.averageFps:0.0} -> {Monitor.Last?.averageFps:0.0} fps, " +
                      $"{Monitor.First?.allocatedMB:0} -> {Monitor.Last?.allocatedMB:0} MB, textures {Monitor.First?.textures} -> {Monitor.Last?.textures}");
        }

        // ------------------------------------------------------------------ the evening

        private IEnumerator SpendTheEvening()
        {
            // Today's story scene first: continue, or the first choice on offer.
            for (int guard = 0; guard < 200 && IsActive("DialoguePanel"); guard++)
            {
                if (!Click("ContinueButton"))
                {
                    Click("Choice_0");
                }

                yield return Pause(0.3f);
            }

            // One plan for the evening: the pub when it is on and Kuba is not tired, else rest, else a call home.
            if (!Click("PubButton") && !Click("RestButton"))
            {
                Click("CallMomButton");
            }

            yield return Pause(0.5f);
            Click("SendMoneyButton");
            yield return Pause(0.5f);
            Click("GoToWorkButton");
            yield return WaitForSceneChange("MetaHub");
        }

        // ------------------------------------------------------------------ the shift

        private IEnumerator PlayShift()
        {
            yield return Pause(1f);
            var shift = FindAnyObjectByType<ShiftController>();
            var station = FindAnyObjectByType<WashStation>();
            var repairPanel = FindAnyObjectByType<DishwasherRepairPanel>(FindObjectsInactive.Include);
            var prepPanel = FindAnyObjectByType<PrepBoardPanel>(FindObjectsInactive.Include);
            RackGrid rack = shift != null ? shift.Rack : null;
            if (shift == null || station == null || rack == null)
            {
                LastProblem = "The kitchen is missing its shift controller, sink or rack";
                yield break;
            }

            if (prepPanel != null)
            {
                yield return Prep(prepPanel, shift);
            }

            while (!shift.IsShiftOver && !TimeIsUp)
            {
                if (Paused)
                {
                    // A call or a notification paused the shift (on a phone): back to the sink.
                    Release();
                    Click("ResumeButton");
                    yield return Pause(0.5f);
                    continue;
                }

                Camera camera = Camera.main;
                if (repairPanel != null && repairPanel.Repair != null && !repairPanel.Repair.IsDone)
                {
                    yield return Repair(repairPanel);
                    continue;
                }

                if (shift.IsDishwasherBroken && (station.Occupant == null || station.Occupant.CurrentStage != WashStage.AwaitingScrub))
                {
                    // The machine stopped: the red button opens the repair card (between dishes, like a player).
                    Click("StartDishwasherButton");
                    yield return Pause(0.3f);
                    continue;
                }

                if (shift.CanStartDishwasher && (rack.FillRatio >= RackGrid.FullLoadFill || station.Occupant == null && FindWashableDish(camera) == null))
                {
                    Click("StartDishwasherButton");
                }

                if (station.Occupant != null)
                {
                    yield return FinishInTheSink(station.Occupant, shift, rack);
                    continue;
                }

                DishController dish = FindWashableDish(camera);
                if (dish != null)
                {
                    yield return ClaimAndScrape(dish, camera);
                }
                else
                {
                    yield return Pause(0.25f);
                }
            }

            Release();
            ShiftsPlayed += shift.IsShiftOver ? 1 : 0;
            if (shift.IsShiftOver)
            {
                yield return Pause(1.5f); // a look at the result card
                Click("BackToMenuButton");
                yield return WaitForSceneChange("Kitchen_Shift");
            }
        }

        private IEnumerator ClaimAndScrape(DishController dish, Camera camera)
        {
            Vector2? point = VisiblePoint(dish, camera);
            if (!point.HasValue)
            {
                yield break;
            }

            for (int frame = 0; frame < 5 && !dish.IsClaimed; frame++)
            {
                Hold(point.Value);
                yield return null;
            }

            // The swipe that claimed it scrapes it too.
            for (int step = 1; step <= 6; step++)
            {
                Hold(point.Value + new Vector2(SwipePixels * step / 6f, 0f));
                yield return null;
            }

            Release();
            yield return null;
        }

        private IEnumerator FinishInTheSink(DishController dish, ShiftController shift, RackGrid rack)
        {
            for (int guard = 0; guard < 300 && dish.IsMoving; guard++)
            {
                yield return null;
            }

            Camera camera = Camera.main;
            switch (dish.CurrentStage)
            {
                case WashStage.AwaitingScrape:
                    yield return ClaimAndScrape(dish, camera);
                    break;
                case WashStage.AwaitingRinse:
                    yield return HoldOnDish(dish, camera, WashStage.AwaitingRinse, 8f);
                    break;
                case WashStage.AwaitingScrub:
                    yield return Scrub(dish, camera);
                    break;
                case WashStage.ReadyForRack:
                    yield return Rack(dish, camera, shift, rack);
                    break;
                default:
                    yield return null;
                    break;
            }
        }

        private IEnumerator HoldOnDish(DishController dish, Camera camera, WashStage until, float seconds)
        {
            float deadline = Time.realtimeSinceStartup + seconds;
            while (dish != null && dish.CurrentStage == until && Time.realtimeSinceStartup < deadline && !TimeIsUp && !Paused)
            {
                Vector2? point = VisiblePoint(dish, camera);
                if (point.HasValue)
                {
                    Hold(point.Value);
                }

                yield return null;
            }

            Release();
            yield return null;
        }

        /// <summary>
        /// Rubs the dish's whole footprint row by row with whichever tool shifts the dirt that is left. The
        /// finger goes down on the dish itself and stays down: a press beside a glass held to the light would
        /// steer its reflection instead of scrubbing.
        /// </summary>
        private IEnumerator Scrub(DishController dish, Camera camera)
        {
            Vector2? onDish = VisiblePoint(dish, camera);
            if (!onDish.HasValue)
            {
                yield return null;
                yield break;
            }

            for (int frame = 0; frame < 3; frame++)
            {
                Hold(onDish.Value);
                yield return null;
            }

            float deadline = Time.realtimeSinceStartup + 40f;
            bool leftToRight = true;
            while (dish != null && dish.CurrentStage == WashStage.AwaitingScrub && Time.realtimeSinceStartup < deadline && !TimeIsUp && !Paused)
            {
                Rect footprint = ScreenFootprint(dish, camera);
                float rowStep = Mathf.Max(2f, Mathf.Min(footprint.height, footprint.width) / 12f);
                float columnStep = Mathf.Max(2f, footprint.width / 10f);
                for (float y = footprint.yMax; y >= footprint.yMin && dish.CurrentStage == WashStage.AwaitingScrub && !Paused; y -= rowStep)
                {
                    for (float t = 0f; t <= footprint.width && dish.CurrentStage == WashStage.AwaitingScrub && !Paused; t += columnStep)
                    {
                        SelectToolForRemainingDirt(dish);
                        Hold(new Vector2(leftToRight ? footprint.xMin + t : footprint.xMax - t, y));
                        yield return null;
                    }

                    leftToRight = !leftToRight;
                }
            }

            Release();
            yield return null;
        }

        private IEnumerator Rack(DishController dish, Camera camera, ShiftController shift, RackGrid rack)
        {
            RackPlacement? placement = rack.FindFreePlacement(dish.Definition);
            if (!placement.HasValue)
            {
                // Full: send this load through the machine (if it is free) and try again.
                Click("StartDishwasherButton");
                yield return Pause(0.5f);
                yield break;
            }

            Vector2? start = VisiblePoint(dish, camera);
            if (!start.HasValue)
            {
                yield return null;
                yield break;
            }

            Vector2 target = camera.WorldToScreenPoint(rack.BodyWorldPosition(dish.Definition, placement.Value));
            for (int step = 0; step <= 20; step++)
            {
                Hold(Vector2.Lerp(start.Value, target, step / 20f));
                yield return null;
            }

            for (int frame = 0; frame < 10; frame++)
            {
                Hold(target);
                yield return null;
            }

            Release();
            yield return Pause(0.3f);
            if (dish != null && dish.CurrentStage == WashStage.Racked)
            {
                DishesRacked++;
            }
        }

        /// <summary>The repair card, done by hand the way a player would: flick the scraps off, turn the arm, hold RESET.</summary>
        /// <summary>The board, when the shift opens with it: a cut on the beat - whenever the vegetable reaches the mark.</summary>
        private IEnumerator Prep(PrepBoardPanel panel, ShiftController shift)
        {
            yield return null; // the card opens on the kitchen's first frame
            while (shift.AwaitingPrep && !TimeIsUp)
            {
                PrepRound round = panel.Round;
                if (round != null && round.IsFinished)
                {
                    yield return Pause(1f); // a look at the verdict
                    panel.Continue();
                }
                else if (round != null && round.FedMm >= PrepRound.TargetSliceMm - 0.1f)
                {
                    panel.TryCut();
                }

                yield return null;
            }
        }

        private IEnumerator Repair(DishwasherRepairPanel panel)
        {
            switch (panel.Repair)
            {
                case CloggedFilterRepair filter:
                    for (int pass = 0; pass < 3 && !filter.IsDone; pass++)
                    {
                        for (int i = 0; i < filter.ScrapCount; i++)
                        {
                            if (filter.IsCleared(i))
                            {
                                continue;
                            }

                            Vector2 from = filter.ScrapPosition(i);
                            Vector2 away = from.sqrMagnitude > 1e-4f ? from.normalized : Vector2.right;
                            for (int step = 0; step <= 8; step++)
                            {
                                Hold(panel.PlayAreaToScreen(from + away * (0.06f * step)));
                                yield return null;
                            }

                            Release();
                            yield return null;
                        }
                    }

                    break;
                case StuckSprayArmRepair arm:
                    for (int frame = 0; frame < 400 && !arm.IsDone; frame++)
                    {
                        float angle = frame * 25f * Mathf.Deg2Rad;
                        Hold(panel.PlayAreaToScreen(new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 0.6f));
                        yield return null;
                    }

                    break;
                case TrippedResetRepair reset:
                    float deadline = Time.realtimeSinceStartup + 6f;
                    while (!reset.IsDone && Time.realtimeSinceStartup < deadline && !Paused)
                    {
                        Hold(panel.PlayAreaToScreen(Vector2.zero));
                        yield return null;
                    }

                    break;
            }

            Release();
            yield return Pause(0.5f);
            if (panel.Repair == null || panel.Repair.IsDone)
            {
                RepairsDone++;
            }
        }

        // ------------------------------------------------------------------ fingers and eyes

        private void Hold(Vector2 point)
        {
            _pointer = point;
            InputSystem.QueueStateEvent(_mouse, new MouseState { position = point }.WithButton(MouseButton.Left));
        }

        private void Release()
        {
            if (_mouse != null && _mouse.added)
            {
                InputSystem.QueueStateEvent(_mouse, new MouseState { position = _pointer });
            }
        }

        private static IEnumerator Pause(float seconds)
        {
            float until = Time.realtimeSinceStartup + seconds;
            while (Time.realtimeSinceStartup < until)
            {
                yield return null;
            }
        }

        private IEnumerator WaitForSceneChange(string from)
        {
            float until = Time.realtimeSinceStartup + 15f;
            while (SceneManager.GetActiveScene().name == from && Time.realtimeSinceStartup < until)
            {
                yield return null;
            }

            yield return null;
        }

        private static bool IsActive(string objectName)
        {
            GameObject go = GameObject.Find(objectName);
            return go != null && go.activeInHierarchy;
        }

        /// <summary>Presses a visible, enabled button by its name in the generated scenes.</summary>
        private static bool Click(string buttonName)
        {
            GameObject go = GameObject.Find(buttonName);
            Button button = go != null ? go.GetComponent<Button>() : null;
            if (button == null || !button.IsActive() || !button.IsInteractable())
            {
                return false;
            }

            button.onClick.Invoke();
            return true;
        }

        /// <summary>A still, reachable dish in the pile - the first thing a tap on it would hit.</summary>
        private static DishController FindWashableDish(Camera camera)
        {
            foreach (DishController dish in FindObjectsByType<DishController>())
            {
                if (!dish.IsClaimed && dish.CurrentStage == WashStage.AwaitingScrape && dish.IsSettledInPile && dish.IsTappableFrom(camera))
                {
                    return dish;
                }
            }

            return null;
        }

        private static Vector2? VisiblePoint(DishController dish, Camera camera)
        {
            if (dish == null || camera == null)
            {
                return null;
            }

            Bounds bounds = dish.GetComponent<Collider>().bounds;
            Vector3 e = bounds.extents * 0.6f;
            Vector3[] candidates =
            {
                bounds.center, bounds.center + new Vector3(0f, bounds.extents.y, 0f),
                bounds.center + new Vector3(e.x, 0f, 0f), bounds.center - new Vector3(e.x, 0f, 0f),
                bounds.center + new Vector3(0f, 0f, e.z), bounds.center - new Vector3(0f, 0f, e.z)
            };

            foreach (Vector3 candidate in candidates)
            {
                Vector2 screenPoint = camera.WorldToScreenPoint(candidate);
                if (Physics.Raycast(camera.ScreenPointToRay(screenPoint), out RaycastHit hit, 100f) &&
                    hit.collider.GetComponentInParent<DishController>() == dish)
                {
                    return screenPoint;
                }
            }

            return null;
        }

        private static Rect ScreenFootprint(DishController dish, Camera camera)
        {
            Bounds b = dish.GetComponent<Renderer>().bounds;
            Vector2 min = new Vector2(float.MaxValue, float.MaxValue);
            Vector2 max = new Vector2(float.MinValue, float.MinValue);
            for (int i = 0; i < 8; i++)
            {
                var corner = new Vector3((i & 1) == 0 ? b.min.x : b.max.x, (i & 2) == 0 ? b.min.y : b.max.y, (i & 4) == 0 ? b.min.z : b.max.z);
                Vector2 p = camera.WorldToScreenPoint(corner);
                min = Vector2.Min(min, p);
                max = Vector2.Max(max, p);
            }

            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }

        private static void SelectToolForRemainingDirt(DishController dish)
        {
            ActiveToolSelector selector = ActiveToolSelector.Instance;
            if (selector == null)
            {
                return;
            }

            foreach (DirtLayerState layer in dish.DirtLayers)
            {
                if (layer.Remaining <= 0f || layer.Type == null)
                {
                    continue;
                }

                foreach (ToolDefinition tool in selector.AvailableTools)
                {
                    // One that gets it right off: the scraper and the spray stop at a film.
                    if (tool.IsEffectiveAgainst(layer.Type) && tool.ResidueFloor <= 0f)
                    {
                        if (selector.CurrentTool != tool)
                        {
                            selector.SelectTool(tool);
                        }

                        return;
                    }
                }
            }
        }
    }
}
