using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Zmywak.Core.Data;
using Zmywak.Core.Events.Channels;
using Zmywak.Core.Save;
using Zmywak.Core.Settings;
using Zmywak.Core.Telemetry;
using Zmywak.Kitchen.Dishes;
using Zmywak.Kitchen.Dishwasher;
using Zmywak.Kitchen.Feedback;
using Zmywak.Kitchen.Flow;
using Zmywak.Kitchen.Input;
using Zmywak.Kitchen.Orders;
using Zmywak.Kitchen.Pile;
using Zmywak.Kitchen.Rack;
using Zmywak.Kitchen.Shifts;
using Zmywak.Kitchen.Tools;
using Zmywak.UI.HUD;

namespace Zmywak.Tests.PlayMode
{
    /// <summary>
    /// Integration tests that load the real, editor-generated Kitchen_Shift scene and drive it through
    /// Unity's Input System test tools via a simulated <see cref="Mouse"/> rather than a Touchscreen:
    /// InputTestFixture.Press/Release write directly into a control's backing state, and Touchscreen's
    /// "press" is a synthetic TouchPressControl derived from touch phase that rejects direct writes
    /// (NotSupportedException). Mouse.leftButton is a plain, directly-writable ButtonControl. Since
    /// DishInputRouter reads everything through the device-agnostic Pointer.current - exactly the design
    /// point of building it that way - this exercises the identical code path a real touch would.
    /// </summary>
    public class KitchenShiftPlayModeTests : InputTestFixture
    {
        private const string SceneName = "Kitchen_Shift";
        private Mouse _mouse;

        public override void Setup()
        {
            base.Setup();
            // A headless Editor never has focus: don't let that reset or hold back the simulated devices.
            // The fixture's settings are per test, so this is test-only.
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            MoveTestInputClockPastPlayModeEntry();
            _mouse = InputSystem.AddDevice<Mouse>();
            // A finished shift autosaves progress; tests also flip relaxed mode, sound and tilt. The sandbox keeps
            // both off the real save and settings, and starts the settings from their defaults (relaxed mode off,
            // sound and tilt on).
            TestSaveSandbox.Enter();
        }

        public override void TearDown()
        {
            Time.timeScale = 1f;
            AudioListener.pause = false;
            KitchenPauseMenu.PauseOnInterruptionOverride = null; // a test that played the phone's interruptions
            ShiftPlan.Schedule = null; // a scheduled shift whose kitchen never started
            ShiftPlan.Special = SpecialShift.None; // a preview that never started its kitchen leaves no special behind
            TestSaveSandbox.Exit();
            base.TearDown();
        }

        [UnityTest]
        public IEnumerator WashingAim_EveryDishHitsTheVisibleBottomInsteadOfTheConvexLid()
        {
            AccessibilitySettings.RelaxedMode = true;
            yield return LoadKitchen();
            var shift = Object.FindAnyObjectByType<ShiftController>();
            var station = Object.FindAnyObjectByType<WashStation>();
            var rack = Object.FindAnyObjectByType<RackGrid>();
            Camera camera = Camera.main;
            foreach (string id in new[] { "plate", "pan", "glass", "bowl", "mug", "pot", "roaster" })
            {
                DishController dish = DishController.Create(DishFromShift(shift, id), station.HeldPosition, shift.transform, rack, station);
                Assert.IsTrue(((MeshCollider)dish.Collider).convex, "A loose dish still needs its physics hull");
                Assert.IsTrue(dish.TryClaim());
                yield return WaitUntilOrFail(() => !dish.IsMoving, 3f, $"{id} did not settle");
                Physics.SyncTransforms();
                Vector2 floor = DishMeshFactory.Floor(dish.Definition.Shape);
                foreach (float x in new[] { -floor.x * 0.5f, 0f, floor.x * 0.5f })
                {
                    Vector3 expected = dish.transform.TransformPoint(new Vector3(x, floor.y, 0f));
                    Vector2 pointer = camera.WorldToScreenPoint(expected);
                    Assert.IsTrue(dish.TryGetWashContact(pointer, out Vector2 uv, out Vector3 contact), $"Cannot reach {id}'s bottom");
                    Assert.Less(Vector3.Distance(expected, contact), 0.01f, $"{id}: the brush hit an invisible lid");
                    Assert.Less(Vector2.Distance(uv, new Vector2(x + 0.5f, 0.5f)), 0.01f, $"{id}: dirt UV does not match the pointer");
                }
                if (id == "plate" || id == "pot")
                {
                    TestScreenshot.Capture(camera, $"washing_aim_{id}.png", 900, 1600);
                }
                Object.Destroy(dish.gameObject);
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator WashingAim_NearTheRimStartsWashing_ButControlsKeepTheirTouch()
        {
            AccessibilitySettings.RelaxedMode = true;
            yield return LoadKitchen();
            var shift = Object.FindAnyObjectByType<ShiftController>();
            var station = Object.FindAnyObjectByType<WashStation>();
            var rack = Object.FindAnyObjectByType<RackGrid>();
            var router = Object.FindAnyObjectByType<DishInputRouter>();
            Camera camera = Camera.main;
            DishController dish = DishController.Create(DishFromShift(shift, "plate"), station.HeldPosition, shift.transform, rack, station);
            Assert.IsTrue(dish.TryClaim());
            yield return WaitUntilOrFail(() => !dish.IsMoving, 3f, "Plate did not settle");
            Physics.SyncTransforms();
            Vector2 centre = camera.WorldToScreenPoint(dish.transform.position);
            Vector2 edge = centre;
            for (int i = 0; i < camera.pixelWidth; i++)
            {
                edge += Vector2.right;
                if (!dish.Collider.Raycast(camera.ScreenPointToRay(edge), out _, 100f)) break;
            }
            Vector2 near = edge + Vector2.right * 2f;
            Assert.IsTrue(dish.TryGetWashContact(near, out _, out _), "A slight miss at the rim should still work");
            Assert.IsFalse(dish.TryGetWashContact(edge + Vector2.right * 60f, out _, out _), "Assistance reached across the counter");
            HoldAt(near);
            yield return null;
            yield return null;
            Assert.AreSame(dish, router.ActiveDish, "The router ignored the forgiving rim contact");
            Release(_mouse.leftButton);
            yield return null;
            System.Func<Vector2, bool> previous = DishInputRouter.IsOverControl;
            try
            {
                DishInputRouter.IsOverControl = _ => true;
                HoldAt(near);
                yield return null;
                yield return null;
                Assert.IsNull(router.ActiveDish, "The rim allowance stole a control's press");
            }
            finally
            {
                DishInputRouter.IsOverControl = previous;
                Release(_mouse.leftButton);
            }
        }

        [UnityTest]
        public IEnumerator WashingAim_TheFirstFastStrokeCleansItsWholePath()
        {
            AccessibilitySettings.RelaxedMode = true;
            yield return LoadKitchen();
            var shift = Object.FindAnyObjectByType<ShiftController>();
            var station = Object.FindAnyObjectByType<WashStation>();
            var rack = Object.FindAnyObjectByType<RackGrid>();
            Camera camera = Camera.main;
            DishDefinition definition = Object.Instantiate(DishFromShift(shift, "plate"));
            var layers = GetPrivateField<List<DirtLayer>>(definition, "dirtLayers");
            DirtTypeDefinition dirt = layers[0].dirtType;
            layers.Clear();
            layers.Add(new DirtLayer { dirtType = dirt, coverage = 1f });
            DishController dish = DishController.Create(definition, station.HeldPosition, shift.transform, rack, station);
            try
            {
                Assert.IsTrue(dish.TryClaim());
                yield return WaitUntilOrFail(() => !dish.IsMoving, 3f, "Plate did not settle");
                Physics.SyncTransforms();
                DishInstance instance = GetPrivateField<DishInstance>(dish, "_instance");
                instance.Scrape();
                instance.Rinse(1f);
                SelectToolForRemainingDirt(dish);
                float floor = DishMeshFactory.Floor(definition.Shape).y;
                Vector2 start = camera.WorldToScreenPoint(dish.transform.TransformPoint(new Vector3(-0.22f, floor, 0f)));
                Vector2 end = camera.WorldToScreenPoint(dish.transform.TransformPoint(new Vector3(0.22f, floor, 0f)));
                var centre = new Vector2(0.5f, 0.5f);
                float before = dish.DirtAmountAt(0, centre);
                var gesture = new TouchGestureRecognizer();
                gesture.PressBegin(start);
                gesture.PressStay(end, 1f / 30f);
                dish.OnGestureUpdate(gesture, end);
                Assert.Less(dish.DirtAmountAt(0, centre), before, "The initial movement only cleaned its endpoint");
                float after = dish.DirtAmountAt(0, centre);
                gesture.PressStay(end, 1f / 30f);
                dish.OnGestureUpdate(gesture, end);
                Assert.AreEqual(after, dish.DirtAmountAt(0, centre), "Holding a sponge still should not scrub");
                HoldAt(start);
                yield return null;
                HoldAt(end);
                yield return null;
                yield return null; // the hand follows in LateUpdate
                var hand = Object.FindAnyObjectByType<GlovedHand>();
                Assert.IsNotNull(hand);
                Assert.IsTrue(dish.TryGetWashContact(end, out _, out Vector3 expectedContact));
                Assert.Less(Vector3.Distance(hand.Contact, expectedContact), 0.002f, "The visible tool lags behind the actual brush");
                Assert.Greater(Vector3.Dot(hand.transform.up, dish.transform.up), 0.999f, "The tool should lie against the tilted dish");
                yield return new WaitForSeconds(0.25f); // let the hand finish appearing before the visual check
                TestScreenshot.Capture(camera, "washing_aim_hand.png", 900, 1600);
            }
            finally
            {
                Release(_mouse.leftButton);
                Object.Destroy(dish.gameObject);
                Object.Destroy(definition);
            }
        }

        [UnityTest]
        public IEnumerator WashingAim_FinishingTheRubKeepsTheDishStillUntilTheFingerLifts()
        {
            AccessibilitySettings.RelaxedMode = true;
            yield return LoadKitchen();
            var shift = Object.FindAnyObjectByType<ShiftController>();
            var station = Object.FindAnyObjectByType<WashStation>();
            var rack = Object.FindAnyObjectByType<RackGrid>();
            Camera camera = Camera.main;
            DishController dish = null;
            yield return WaitUntilOrFail(() => (dish = FindWashableDish(camera, null)) != null, 10f, "No dish to wash");
            yield return ClaimScrapeAndRinse(dish, camera);
            yield return ScrubOnly(dish, camera);
            Vector3 position = dish.transform.position;
            Vector3 scale = dish.transform.localScale;
            Vector3 target = rack.BestSpotFor(dish.Definition).Value;
            // Even continuing the final rub away from the sink cannot turn it into a drop or throw.
            for (int i = 0; i < 5; i++)
            {
                HoldAt(camera.WorldToScreenPoint(target));
                yield return null;
            }
            Assert.Less(Vector3.Distance(position, dish.transform.position), 0.01f, "The final rub grabbed the clean dish");
            Assert.Less(Vector3.Distance(scale, dish.transform.localScale), 0.01f, "The clean dish shrank while still scrubbing");
            Release(_mouse.leftButton);
            yield return null;
            Assert.AreEqual(WashStage.ReadyForRack, dish.CurrentStage);
            HoldAt(VisiblePointOrFail(dish, camera));
            yield return null;
            for (int i = 0; i < 5; i++)
            {
                HoldAt(camera.WorldToScreenPoint(target));
                yield return null;
            }
            Release(_mouse.leftButton);
            yield return null;
            Assert.AreEqual(WashStage.Racked, dish.CurrentStage, "A fresh gesture should still carry it onto the rack");
        }

        [UnityTest]
        public IEnumerator KitchenShiftScene_Loads_AndSpawnsDishes()
        {
            yield return LoadKitchen();

            var shiftController = Object.FindAnyObjectByType<ShiftController>();
            Assert.IsNotNull(shiftController, "ShiftController not found in Kitchen_Shift scene");

            yield return WaitUntilOrFail(() => shiftController.DishesSpawnedTotal >= 1, 10f,
                "No dish spawned within timeout");
        }

        [UnityTest]
        public IEnumerator FullWashLoop_SimulatedTouch_ProducesCleanedDish()
        {
            yield return LoadKitchen();

            var shiftController = Object.FindAnyObjectByType<ShiftController>();
            var rackGrid = Object.FindAnyObjectByType<RackGrid>();
            var washStation = Object.FindAnyObjectByType<WashStation>();
            Camera camera = Camera.main;
            Assert.IsNotNull(shiftController);
            Assert.IsNotNull(rackGrid);
            Assert.IsNotNull(washStation, "Kitchen scene has no sink (WashStation)");
            Assert.IsNotNull(camera);

            DishController dish = null;
            yield return WaitUntilOrFail(() => (dish = FindWashableDish(camera, null)) != null, 10f, "No dish spawned to wash");
            TestScreenshot.Capture(camera, "01_kitchen_shift_initial.png");

            var fx = Object.FindAnyObjectByType<KitchenFx>();
            var audio = Object.FindAnyObjectByType<KitchenAudio>();
            Assert.IsNotNull(fx, "Kitchen scene has no particle feedback (KitchenFx)");
            Assert.IsNotNull(audio, "Kitchen scene has no procedural audio (KitchenAudio)");
            int oneShotsBefore = audio.OneShotsPlayed;
            int particlesWhileScrubbing = 0;
            int particlesWhenClean = 0;
            using var feedback = new FeedbackRecorder();

            yield return WashAndRack(dish, camera, rackGrid, stage =>
            {
                switch (stage)
                {
                    case WashStage.AwaitingRinse:
                        Assert.AreSame(dish, washStation.Occupant, "Claimed dish was not lifted onto the sink");
                        break;
                    case WashStage.AwaitingScrub:
                        particlesWhileScrubbing = fx.LiveParticleCount; // a second into scrubbing: foam and grime flecks
                        TestScreenshot.Capture(camera, "02_kitchen_shift_scrubbing.png");
                        break;
                    case WashStage.ReadyForRack:
                        particlesWhenClean = fx.LiveParticleCount; // stars and the ripple ring on the clean dish
                        TestScreenshot.Capture(camera, "03_kitchen_shift_dish_clean.png");
                        break;
                    case WashStage.Racked:
                        Assert.IsNull(washStation.Occupant, "Racking the dish did not free the sink");
                        TestScreenshot.Capture(camera, "04_kitchen_shift_dish_racked.png");
                        break;
                }
            });

            // Every step of the wash fed the juice layer: events raised, particles alive, sounds started.
            Assert.That(feedback.Claimed, Is.GreaterThanOrEqualTo(1), feedback.ToString());
            Assert.That(feedback.Scraped, Is.GreaterThanOrEqualTo(1), feedback.ToString());
            Assert.That(feedback.Rinsing, Is.GreaterThanOrEqualTo(1), feedback.ToString());
            Assert.That(feedback.Scrubbing, Is.GreaterThanOrEqualTo(1), feedback.ToString());
            Assert.That(feedback.Cleaned, Is.EqualTo(1), feedback.ToString());
            Assert.That(feedback.Racked, Is.EqualTo(1), feedback.ToString());
            Assert.Greater(particlesWhileScrubbing, 0, "Scrubbing threw no foam or grime particles");
            Assert.Greater(particlesWhenClean, 0, "The dish came clean without stars or a ripple");
            Assert.GreaterOrEqual(audio.OneShotsPlayed - oneShotsBefore, 4,
                "Pickup, scrape, squeak and clink should each have played a one-shot");

            shiftController.TryStartDishwasherCycle();
            yield return WaitUntilOrFail(() => shiftController.DishesCleanedTotal >= 1, 25f,
                "Dishwasher cycle did not report a cleaned dish in time");

            TestScreenshot.Capture(camera, "05_kitchen_shift_after_dishwasher.png");
        }

        [UnityTest]
        public IEnumerator Sink_HoldsOneDishAtATime()
        {
            // Relaxed mode: no collapse can destroy the dishes this test is poking at.
            AccessibilitySettings.RelaxedMode = true;
            yield return LoadKitchen();

            var washStation = Object.FindAnyObjectByType<WashStation>();
            var shiftController = Object.FindAnyObjectByType<ShiftController>();
            Camera camera = Camera.main;

            DishController first = null;
            yield return WaitUntilOrFail(() => (first = FindWashableDish(camera, null)) != null, 10f, "No first dish");
            yield return Tap(VisiblePointOrFail(first, camera));
            Assert.AreSame(first, washStation.Occupant);

            // Speed up spawning until a second dish is waiting in the pile.
            Time.timeScale = 6f;
            DishController second = null;
            yield return WaitUntilOrFail(() => (second = FindWashableDish(camera, null)) != null, 10f, "No second dish");
            Time.timeScale = 1f;

            yield return Tap(VisiblePointOrFail(second, camera));

            Assert.IsFalse(second.IsClaimed, "A second dish was taken while the sink was busy");
            Assert.AreSame(first, washStation.Occupant);
            Assert.Greater(shiftController.DishesSpawnedTotal, 1);
        }

        [UnityTest]
        public IEnumerator Shift_OpensWithAFewDishes_ThenKeepsThemComing()
        {
            // Relaxed mode: no collapse, so nothing interferes with counting what arrives.
            AccessibilitySettings.RelaxedMode = true;
            yield return LoadKitchen();

            var shiftController = Object.FindAnyObjectByType<ShiftController>();
            ShiftDefinition shift = GetPrivateField<ShiftDefinition>(shiftController, "shiftDefinition");
            Assert.Greater(shift.OpeningDishes, 0);

            // "At the start" in game time: a stalled run (the laptop dozing off in Modern Standby froze one for two minutes) must not count
            // against the shift, whose clock only advances a capped step per frame.
            yield return WaitUntilOrFail(() => Time.timeSinceLevelLoad >= 1f, 30f, "The shift clock never got going");
            Assert.GreaterOrEqual(shiftController.DishesSpawnedTotal, shift.OpeningDishes,
                "The opening dishes were not on the counter at the start of the shift");

            // Nothing more until the first gap is (nearly) up - no flood at the start.
            Time.timeScale = 5f;
            yield return new WaitForSeconds(shift.SpawnIntervalAt(0f) * 0.8f);
            Assert.AreEqual(shift.OpeningDishes, shiftController.DishesSpawnedTotal, "A dish arrived before the first gap was up");

            // ...and the queue starts over instead of drying up for the rest of the shift.
            Time.timeScale = 10f;
            yield return WaitUntilOrFail(() => shiftController.DishesSpawnedTotal > shift.DishSpawnQueue.Count, 30f,
                $"Only {shiftController.DishesSpawnedTotal} dishes came; the {shift.DishSpawnQueue.Count}-dish queue did not repeat");
            Time.timeScale = 1f;
        }

        [UnityTest]
        public IEnumerator APileAtRest_FallsAsleep()
        {
            // Dishes nobody touches have to stop costing physics work - and stop creeping, or they never count as
            // still enough to tap. Only the opening dishes are out before the first gap is up.
            AccessibilitySettings.RelaxedMode = true;
            yield return LoadKitchen();

            var shiftController = Object.FindAnyObjectByType<ShiftController>();
            ShiftDefinition shift = GetPrivateField<ShiftDefinition>(shiftController, "shiftDefinition");
            yield return WaitUntilOrFail(() => shiftController.DishesSpawnedTotal >= shift.OpeningDishes, 30f, "The opening dishes never came");

            float asleepBy = Time.timeSinceLevelLoad + Mathf.Min(8f, shift.SpawnIntervalAt(0f) * 0.8f);
            string awake = null;
            while (Time.timeSinceLevelLoad < asleepBy)
            {
                awake = null;
                foreach (DishController dish in Object.FindObjectsByType<DishController>())
                {
                    var body = dish.GetComponent<Rigidbody>();
                    if (!body.isKinematic && !body.IsSleeping())
                    {
                        awake = $"{dish.name} at {dish.transform.position} v {body.linearVelocity.magnitude:0.000} w {body.angularVelocity.magnitude:0.000}";
                        break;
                    }
                }

                if (awake == null)
                {
                    break;
                }

                yield return null;
            }

            Assert.IsNull(awake, "The pile never fell asleep; still moving: " + awake);
            Assert.AreEqual(shift.OpeningDishes, shiftController.DishesSpawnedTotal, "A new dish arrived and woke the pile - the check ran too long");
        }

        [UnityTest]
        public IEnumerator DishwasherBreakdown_EachFault_IsFixedThroughTheRepairCard()
        {
            AccessibilitySettings.RelaxedMode = true; // nothing collapses while the card is up
            yield return LoadKitchen();

            var dishwasher = Object.FindAnyObjectByType<DishwasherController>();
            var panel = Object.FindAnyObjectByType<DishwasherRepairPanel>();
            Button dishwasherButton = GetPrivateField<Button>(Object.FindAnyObjectByType<ShiftHUD>(), "startDishwasherButton");
            Camera camera = Camera.main;
            Assert.IsNotNull(panel, "Kitchen HUD has no dishwasher repair card");

            int shot = 10;
            foreach (DishwasherFault fault in System.Enum.GetValues(typeof(DishwasherFault)))
            {
                dishwasher.BreakDown(fault);
                yield return null;
                Assert.IsTrue(dishwasherButton.interactable, $"{fault}: the dishwasher button cannot be pressed to reach the repair");
                Assert.AreEqual("Napraw zmywarkę!", dishwasherButton.GetComponentInChildren<TMP_Text>().text);

                dishwasherButton.onClick.Invoke();
                Assert.IsTrue(panel.IsOpen, $"{fault}: the red dishwasher button did not open the repair card");
                Assert.AreEqual(fault, panel.Repair.Fault);
                yield return null;
                TestScreenshot.Capture(camera, $"{shot++}_repair_{fault}.png");

                DishwasherRepair repair = panel.Repair;
                yield return PerformRepair(panel);
                yield return WaitUntilOrFail(() => !dishwasher.IsBroken, 3f,
                    $"{fault}: the gestures did not fix the machine (progress {repair.Progress01:P0})");
                yield return WaitUntilOrFail(() => !panel.IsOpen, 3f, $"{fault}: the repair card stayed up after the fix");
            }
        }

        [UnityTest]
        public IEnumerator DishwasherBreakdown_MidCycle_HoldsTheCycle_UntilRepaired()
        {
            AccessibilitySettings.RelaxedMode = true;
            yield return LoadKitchen();

            var shiftController = Object.FindAnyObjectByType<ShiftController>();
            var rackGrid = Object.FindAnyObjectByType<RackGrid>();
            var dishwasher = Object.FindAnyObjectByType<DishwasherController>();
            var panel = Object.FindAnyObjectByType<DishwasherRepairPanel>();
            var audio = Object.FindAnyObjectByType<KitchenAudio>();
            Button dishwasherButton = GetPrivateField<Button>(Object.FindAnyObjectByType<ShiftHUD>(), "startDishwasherButton");
            Camera camera = Camera.main;

            DishController dish = null;
            yield return WaitUntilOrFail(() => (dish = FindWashableDish(camera, null)) != null, 10f, "No dish to wash");
            yield return WashAndRack(dish, camera, rackGrid);

            dishwasherButton.onClick.Invoke();
            Assert.IsTrue(dishwasher.IsRunning, "The dishwasher button did not start a cycle");
            yield return WaitUntilOrFail(() => dishwasher.Progress01 > 0.2f, 10f, "The cycle did not get going");

            int soundsBefore = audio.OneShotsPlayed;
            dishwasher.BreakDown(DishwasherFault.TrippedReset);
            Assert.Greater(audio.OneShotsPlayed, soundsBefore, "The breakdown made no sound");
            float stalledAt = dishwasher.Progress01;
            yield return new WaitForSeconds(1f);
            Assert.AreEqual(stalledAt, dishwasher.Progress01, 1e-4f, "The cycle kept going on a broken machine");
            TestScreenshot.Capture(camera, "13_dishwasher_broken.png");
            Assert.IsFalse(shiftController.CanStartDishwasher);

            dishwasherButton.onClick.Invoke();
            Assert.IsTrue(panel.IsOpen);

            // The kitchen behind the card must not take touches.
            DishController pileDish = FindWashableDish(camera, null);
            if (pileDish != null)
            {
                yield return Tap(VisiblePointOrFail(pileDish, camera));
                Assert.IsFalse(pileDish.IsClaimed, "A dish in the pile was grabbed through the repair card");
            }

            yield return PerformRepair(panel);
            yield return WaitUntilOrFail(() => !dishwasher.IsBroken, 3f, "Holding RESET did not fix the machine");
            yield return WaitUntilOrFail(() => shiftController.DishesCleanedTotal >= 1, 25f,
                "The cycle did not pick up again and finish after the repair");
        }

        [UnityTest]
        public IEnumerator Rack_DishCoversItsFootprint_AndTheLoadGoesIntoTheMachine()
        {
            AccessibilitySettings.RelaxedMode = true;
            yield return LoadKitchen();

            var shiftController = Object.FindAnyObjectByType<ShiftController>();
            var rackGrid = Object.FindAnyObjectByType<RackGrid>();
            var dishwasher = Object.FindAnyObjectByType<DishwasherController>();
            var hud = Object.FindAnyObjectByType<ShiftHUD>();
            TMP_Text rackText = GetPrivateField<TMP_Text>(hud, "rackFillText");
            Button dishwasherButton = GetPrivateField<Button>(hud, "startDishwasherButton");
            Camera camera = Camera.main;

            DishController dish = null;
            yield return WaitUntilOrFail(() => (dish = FindWashableDish(camera, null)) != null, 10f, "No dish to wash");
            yield return WashAndRack(dish, camera, rackGrid);

            Vector2Int footprint = dish.Definition.RackFootprint;
            Assert.AreEqual(footprint.x * footprint.y, rackGrid.OccupiedCells,
                $"{dish.name} should cover {footprint.x}x{footprint.y} cells of the rack");
            yield return null;
            Assert.AreEqual($"Kosz: {rackGrid.OccupiedCells}/{rackGrid.CellCount}", rackText.text);

            dishwasherButton.onClick.Invoke();
            Assert.IsTrue(dishwasher.IsRunning, "The dishwasher button did not start a cycle");
            Assert.AreEqual(0, rackGrid.DishCount, "The load stayed on the rack instead of going into the machine");
            Assert.AreEqual(0, rackGrid.OccupiedCells, "The rack is not free for the next load while this one washes");
            Assert.AreEqual(1, dishwasher.LoadCount);
            yield return WaitUntilOrFail(() => !dish.gameObject.activeSelf, 2f, "The dish never slid into the machine");

            yield return WaitUntilOrFail(() => shiftController.DishesCleanedTotal >= 1, 25f, "The load did not come out washed");
            Assert.AreEqual(0, shiftController.FullRackLoads, "One dish on its own is not a full rack");
        }

        [UnityTest]
        public IEnumerator ALoadStillWashing_AtClosingTime_CountsOnce()
        {
            AccessibilitySettings.RelaxedMode = true;
            yield return LoadKitchen();

            var shiftController = Object.FindAnyObjectByType<ShiftController>();
            var rackGrid = Object.FindAnyObjectByType<RackGrid>();
            var dishwasher = Object.FindAnyObjectByType<DishwasherController>();
            var hud = Object.FindAnyObjectByType<ShiftHUD>();
            TMP_Text dishesText = GetPrivateField<TMP_Text>(hud, "dishesText");
            Camera camera = Camera.main;

            DishController dish = null;
            yield return WaitUntilOrFail(() => (dish = FindWashableDish(camera, null)) != null, 10f, "No dish to wash");
            yield return WashAndRack(dish, camera, rackGrid);
            shiftController.TryStartDishwasherCycle();
            Assert.IsTrue(dishwasher.IsRunning, "The dishwasher did not start");

            shiftController.EndShiftNow(); // the clock runs out mid-cycle: the closing load counts the dish
            Assert.AreEqual(1, shiftController.DishesCleanedTotal, "The load in the machine at closing time did not count");

            // Seen on the emulator: the machine finished its cycle after the result card and counted the dish again,
            // so the HUD behind the card said 2 where the card said 1.
            yield return WaitUntilOrFail(() => !dishwasher.IsRunning, 25f, "The closing cycle never finished");
            yield return null;
            Assert.AreEqual(1, shiftController.DishesCleanedTotal, "The closing load was counted twice");
            StringAssert.StartsWith("Umyte: 1", dishesText.text);
        }

        [UnityTest]
        public IEnumerator ATapShorterThanAFrame_StillTakesTheDishToTheSink()
        {
            AccessibilitySettings.RelaxedMode = true;
            yield return LoadKitchen();
            Camera camera = Camera.main;

            DishController dish = null;
            yield return WaitUntilOrFail(() => (dish = FindWashableDish(camera, null)) != null, 10f, "No dish to wash");
            var settle = new PlayDeadline(5f);
            while (!FindVisiblePoint(dish, camera).HasValue && !settle.Passed)
            {
                yield return null;
            }

            Set(_mouse.position, VisiblePointOrFail(dish, camera));
            yield return null;
            // Down and up between two frames, as on the emulator at 30 fps: the button is never seen held.
            Press(_mouse.leftButton, queueEventOnly: true);
            Release(_mouse.leftButton, queueEventOnly: true);
            yield return null;
            yield return null;
            Assert.IsTrue(dish.IsClaimed, "A tap shorter than a frame did not take the dish to the sink");
        }

        [UnityTest]
        public IEnumerator LettingGoOfACleanDish_OverTheSink_IsNoMissedDrop_DroppingItOnTheCounterIs()
        {
            AccessibilitySettings.RelaxedMode = true;
            yield return LoadKitchen();
            Camera camera = Camera.main;
            var station = Object.FindAnyObjectByType<WashStation>();
            int missed = 0;
            void OnMissed(DishController _) => missed++;
            KitchenFeedbackEvents.DropMissed += OnMissed;
            try
            {
                DishController dish = null;
                yield return WaitUntilOrFail(() => (dish = FindWashableDish(camera, null)) != null, 10f, "No dish to wash");
                yield return ClaimScrapeAndRinse(dish, camera);
                yield return ScrubClean(dish, camera, null, 3f, null);

                // The finger that scrubbed it clean lifts where it is, over the sink: no throw, no miss (it cost 2 points).
                Release(_mouse.leftButton);
                yield return null;
                yield return WaitUntilOrFail(() => !dish.IsMoving, 3f, $"{dish.name} did not settle back in the sink");
                Assert.AreEqual(0, missed, "Letting go of a clean dish over the sink counted as a drop that missed the rack");

                yield return Tap(VisiblePointOrFail(dish, camera));
                yield return WaitUntilOrFail(() => !dish.IsMoving, 3f, $"{dish.name} did not settle back in the sink");
                Assert.AreEqual(0, missed, "A tap on a clean dish counted as a drop that missed the rack");

                // Carried off across the counter and dropped there, away from the rack: that one missed.
                Vector2 counter = new Vector2(Screen.width * 0.2f, Screen.height * 0.3f);
                Ray ray = camera.ScreenPointToRay(counter);
                Assert.IsTrue(new Plane(Vector3.up, station.HeldPosition).Raycast(ray, out float along), "The counter point is off the table");
                Vector3 onCounter = ray.GetPoint(along) - station.HeldPosition;
                Assert.Greater(new Vector2(onCounter.x, onCounter.z).magnitude, DishController.MissedDropDistance,
                    "The test's counter point is too close to the sink to be a carried-off drop");

                HoldAt(VisiblePointOrFail(dish, camera));
                yield return null;
                for (int frame = 0; frame < 20; frame++)
                {
                    HoldAt(counter);
                    yield return null;
                }

                Release(_mouse.leftButton);
                yield return null;
                Assert.AreEqual(1, missed, "A clean dish dropped on the counter, off the rack, did not count as a missed drop");
            }
            finally
            {
                KitchenFeedbackEvents.DropMissed -= OnMissed;
            }
        }

        [UnityTest]
        public IEnumerator GlassInspection_TiltRollsTheReflection_AndStreaksMustComeOff()
        {
            AccessibilitySettings.RelaxedMode = true;
            var tilt = InputSystem.AddDevice<Accelerometer>();
            var heldLevel = new Vector3(0f, -0.7f, -0.7f); // phone held at ~45 degrees, as over a sink
            Set(tilt.acceleration, heldLevel);
            yield return LoadKitchen();

            var shiftController = Object.FindAnyObjectByType<ShiftController>();
            var rackGrid = Object.FindAnyObjectByType<RackGrid>();
            TMP_Text hint = GetPrivateField<TMP_Text>(Object.FindAnyObjectByType<ShiftHUD>(), "stageHintText");
            Camera camera = Camera.main;
            DishDefinition glassType = null;
            foreach (DishDefinition candidate in GetPrivateField<ShiftDefinition>(shiftController, "shiftDefinition").DishSpawnQueue)
            {
                if (candidate.Id == "glass")
                {
                    glassType = candidate;
                }
            }

            Assert.IsNotNull(glassType, "The shift has no glasses");
            DishController glass = null;
            Time.timeScale = 8f; // the first glass is the third dish of the shift
            yield return WaitUntilOrFail(() => (glass = FindWashableDish(camera, glassType)) != null, 15f, "No glass came to wash");
            Time.timeScale = 1f;

            bool inspected = false;
            yield return WashAndRack(glass, camera, rackGrid, onInspection: () => CheckGlassUnderTheLight());
            Assert.IsTrue(inspected, "The glass was never held to the light");

            IEnumerator CheckGlassUnderTheLight()
            {
                Assert.AreEqual(WashStage.AwaitingScrub, glass.CurrentStage, "A glass with streaks on it counted as clean");
                Assert.IsTrue(glass.NeedsInspection);
                yield return null;
                StringAssert.StartsWith("Pod światło!", hint.text);

                Set(tilt.acceleration, heldLevel);
                yield return new WaitForSeconds(0.3f);
                float level = glass.InspectionStripeX;
                Assert.AreEqual(0.5f, level, 0.05f, "Held as it was when the check began, the reflection should sit mid-glass");

                Set(tilt.acceleration, heldLevel + new Vector3(0.25f, 0f, 0f)); // tip the right edge down
                yield return new WaitForSeconds(0.5f);
                Assert.Greater(glass.InspectionStripeX, level + 0.15f, "Tipping the phone did not slide the reflection");
                TestScreenshot.Capture(camera, "14_glass_under_the_light.png");
                inspected = true;
            }
        }

        [UnityTest]
        public IEnumerator GlassInspection_WithoutATiltSensor_ADragBesideTheGlassSlidesTheReflection()
        {
            AccessibilitySettings.RelaxedMode = true; // no accelerometer on this "phone"
            yield return LoadKitchen();

            var shiftController = Object.FindAnyObjectByType<ShiftController>();
            var rackGrid = Object.FindAnyObjectByType<RackGrid>();
            TMP_Text hint = GetPrivateField<TMP_Text>(Object.FindAnyObjectByType<ShiftHUD>(), "stageHintText");
            Camera camera = Camera.main;
            DishDefinition glassType = null;
            foreach (DishDefinition candidate in GetPrivateField<ShiftDefinition>(shiftController, "shiftDefinition").DishSpawnQueue)
            {
                if (candidate.Id == "glass")
                {
                    glassType = candidate;
                }
            }

            DishController glass = null;
            Time.timeScale = 8f;
            yield return WaitUntilOrFail(() => (glass = FindWashableDish(camera, glassType)) != null, 15f, "No glass came to wash");
            Time.timeScale = 1f;

            bool steered = false;
            yield return WashAndRack(glass, camera, rackGrid, onInspection: () => DragBesideTheGlass());
            Assert.IsTrue(steered, "The glass was never held to the light");

            IEnumerator DragBesideTheGlass()
            {
                Assert.IsFalse(DishController.TiltSteersReflection);
                yield return null;
                StringAssert.Contains("Przeciągnij palcem obok szklanki", hint.text);

                // Start on bare counter to the left of the glass, then drag right a quarter of the screen.
                Rect footprint = ScreenFootprint(glass, camera);
                var start = new Vector2(footprint.xMin - Screen.width * 0.12f, footprint.center.y);
                Physics.Raycast(camera.ScreenPointToRay(start), out RaycastHit hit, 100f);
                Assert.IsTrue(hit.collider == null || hit.collider.GetComponentInParent<DishController>() == null,
                    $"The drag would start on a dish ({hit.collider?.name})");

                float before = glass.InspectionStripeX;
                for (int step = 0; step <= 10; step++)
                {
                    HoldAt(start + new Vector2(Screen.width * 0.025f * step, 0f));
                    yield return null;
                }

                Release(_mouse.leftButton);
                yield return null;
                Assert.Greater(glass.InspectionStripeX, before + 0.2f, "Dragging beside the glass did not slide the reflection");
                Assert.AreEqual(WashStage.AwaitingScrub, glass.CurrentStage, "The drag did something else to the glass");
                steered = true;
            }
        }

        [UnityTest]
        public IEnumerator Pause_TheBackGesture_StopsTheShift_UntilThePlayerComesBack()
        {
            AccessibilitySettings.RelaxedMode = true;
            var keyboard = InputSystem.AddDevice<Keyboard>(); // Android's back gesture arrives as Escape
            yield return LoadKitchen();

            var shiftController = Object.FindAnyObjectByType<ShiftController>();
            var pause = Object.FindAnyObjectByType<KitchenPauseMenu>();
            Camera camera = Camera.main;
            Assert.IsNotNull(pause, "The kitchen has no pause menu");
            DishController dish = null;
            yield return WaitUntilOrFail(() => (dish = FindWashableDish(camera, null)) != null, 10f, "No dish to try touching");
            Vector2 dishPoint = VisiblePointOrFail(dish, camera);

            Press(keyboard.escapeKey);
            yield return null;
            Release(keyboard.escapeKey);
            yield return null;

            Assert.IsTrue(pause.IsPaused, "Back did not pause the shift");
            Assert.AreEqual(0f, Time.timeScale, "The shift clock and physics kept running");
            Assert.IsTrue(AudioListener.pause, "The kitchen kept making noise while paused");
            float remaining = shiftController.RemainingSeconds;
            yield return new WaitForSecondsRealtime(0.5f);
            Assert.AreEqual(remaining, shiftController.RemainingSeconds, "The shift timer ran during the pause");
            TestScreenshot.Capture(camera, "17_kitchen_paused.png");

            yield return Tap(dishPoint);
            Assert.IsFalse(dish.IsClaimed, "A dish was picked up through the pause menu");

            Press(keyboard.escapeKey);
            yield return null;
            Release(keyboard.escapeKey);
            yield return null;
            Assert.IsFalse(pause.IsPaused, "Back did not resume the shift");
            Assert.AreEqual(1f, Time.timeScale);
            Assert.IsFalse(AudioListener.pause);
            yield return new WaitForSeconds(0.5f);
            Assert.Less(shiftController.RemainingSeconds, remaining, "The shift did not carry on after the pause");

            // Clocking out from the pause menu ends the shift there and then, and unfreezes the game.
            pause.Pause();
            GetPrivateField<Button>(pause, "endShiftButton").onClick.Invoke();
            Assert.IsTrue(shiftController.IsShiftOver, "Ending the shift from the pause menu did nothing");
            Assert.AreEqual(1f, Time.timeScale);
            Assert.IsFalse(AudioListener.pause);
        }

        [UnityTest]
        public IEnumerator Pause_TheAppGoingToTheBackground_StopsTheShift_AndComingBackWaitsForThePlayer()
        {
            AccessibilitySettings.RelaxedMode = true;
            KitchenPauseMenu.PauseOnInterruptionOverride = true; // the Editor does not react to focus by itself; a phone does
            yield return LoadKitchen();

            var shiftController = Object.FindAnyObjectByType<ShiftController>();
            var pause = Object.FindAnyObjectByType<KitchenPauseMenu>();
            Camera camera = Camera.main;
            Assert.IsNotNull(pause, "The kitchen has no pause menu");
            DishController dish = null;
            yield return WaitUntilOrFail(() => (dish = FindWashableDish(camera, null)) != null, 10f, "No dish to try touching");
            Vector2 dishPoint = VisiblePointOrFail(dish, camera);

            // The home button, a call, the notification shade: the same messages Android sends the game.
            pause.SendMessage("OnApplicationPause", true);
            Assert.IsTrue(pause.IsPaused, "Sending the app to the background did not pause the shift");
            Assert.AreEqual(0f, Time.timeScale, "The shift clock and physics kept running in the background");
            Assert.IsTrue(AudioListener.pause, "The kitchen kept making noise in the background");
            float remaining = shiftController.RemainingSeconds;

            // Coming back: focus returns, but the shift waits for the player ("back to the sink") - the clock never starts
            // under a thumb that has not touched the screen yet.
            pause.SendMessage("OnApplicationPause", false);
            pause.SendMessage("OnApplicationFocus", true);
            yield return new WaitForSecondsRealtime(0.4f);
            Assert.IsTrue(pause.IsPaused, "Coming back resumed the shift behind the player's back");
            Assert.AreEqual(remaining, shiftController.RemainingSeconds, "The shift timer ran while the card was up");
            yield return Tap(dishPoint);
            Assert.IsFalse(dish.IsClaimed, "A dish was picked up through the pause card");

            GetPrivateField<Button>(pause, "resumeButton").onClick.Invoke();
            Assert.IsFalse(pause.IsPaused);
            Assert.AreEqual(1f, Time.timeScale, "The game did not go back to its speed");
            Assert.IsFalse(AudioListener.pause, "The kitchen stayed silent after coming back");
            yield return new WaitForSeconds(0.5f);
            Assert.Less(shiftController.RemainingSeconds, remaining, "The shift did not carry on after the player came back");

            // Losing focus alone (split screen, a system dialog) pauses as well, and twice in a row loses nothing.
            pause.SendMessage("OnApplicationFocus", false);
            pause.SendMessage("OnApplicationFocus", false);
            Assert.IsTrue(pause.IsPaused);
            Assert.AreEqual(0f, Time.timeScale);
            GetPrivateField<Button>(pause, "resumeButton").onClick.Invoke();
            Assert.AreEqual(1f, Time.timeScale, "Pausing twice lost the speed the game ran at");
        }

        [UnityTest]
        public IEnumerator AScheduledDay_PlaysItsKindOfShift_DealtForThatDay_InWaves()
        {
            AccessibilitySettings.RelaxedMode = true; // nothing breaks while the dishes pile up at speed
            ScheduledShift day = ShiftSchedule.For(10, SpecialShift.None, tutorialBreakdownDone: true, lastShiftBrokeDown: false);
            ShiftPlan.Schedule = day;
            yield return LoadKitchen();

            var shiftController = Object.FindAnyObjectByType<ShiftController>();
            Assert.IsNull(ShiftPlan.Schedule, "The kitchen should take the plan, not leave it for the next shift");
            Assert.IsTrue(shiftController.IsScheduled, "The day's kind of shift was not found in the catalog");
            Assert.AreEqual(day.ArchetypeId, shiftController.Definition.Id);
            Assert.AreEqual(ShiftSchedule.QuotaFor(shiftController.Definition.Quota, day.Pace, 1f), shiftController.Quota);

            // The night's card tells what kind of shift it is.
            var badge = Object.FindAnyObjectByType<SpecialShiftBadge>(FindObjectsInactive.Include);
            yield return null;
            Assert.IsTrue(GetPrivateField<GameObject>(badge, "card").activeSelf, "No card for the day's kind of shift");
            Assert.AreEqual(shiftController.Title, GetPrivateField<TMP_Text>(badge, "titleText").text);

            var phases = new List<ServicePhase>();
            shiftController.OnServicePhaseChanged += (phase, callout) => phases.Add(phase);
            Time.timeScale = 10f;
            yield return WaitUntilOrFail(() => shiftController.DishesSpawnedTotal >= 9 && shiftController.Progress01 > 0.5f, 40f,
                "The shift did not bring nine dishes by half-time");
            Time.timeScale = 1f;

            // The dishes came in the day's own deal - and not in the order of the mix.
            ShiftDefinition kind = shiftController.Definition;
            var expected = new ShuffleBag<DishDefinition>(null, kind.DishSpawnQueue, dish => dish.Id, day.Seed);
            var spawned = GetPrivateField<List<DishController>>(shiftController, "_spawnedDishes");
            var dealt = new List<string>();
            var dealtExpected = new List<string>();
            for (int i = 0; i < 9; i++)
            {
                Assert.IsTrue(spawned[i] != null, "A dish vanished in relaxed mode");
                dealt.Add(spawned[i].Definition.Id);
                dealtExpected.Add(expected.Next().Id);
            }

            CollectionAssert.AreEqual(dealtExpected, dealt, "The dishes did not come in the day's deal");
            CollectionAssert.AreNotEqual(kind.DishSpawnQueue.Take(9).Select(dish => dish.Id).ToList(), dealt, "The dishes came in the mix's own order");

            // The service came in a wave: the room filled up, then the rush.
            CollectionAssert.Contains(phases, ServicePhase.Rising);
            CollectionAssert.Contains(phases, ServicePhase.Peak);
        }

        [UnityTest]
        public IEnumerator AnUrgentOrder_IsMetTheMomentTheDishIsRacked_AndTheResultCardSaysHowItWent()
        {
            AccessibilitySettings.RelaxedMode = true; // the pile must not smash the one dish the order needs
            yield return LoadKitchen();

            var shiftController = Object.FindAnyObjectByType<ShiftController>();
            var orders = Object.FindAnyObjectByType<ChefOrderController>();
            var flow = Object.FindAnyObjectByType<FlowStreakController>();
            var rackGrid = Object.FindAnyObjectByType<RackGrid>();
            Camera camera = Camera.main;
            DishController dish = null;
            yield return WaitUntilOrFail(() => (dish = FindWashableDish(camera, null)) != null, 10f, "No dish to wash");

            // "A plate, NOW!": the chef takes it straight off the rack, no machine cycle.
            orders.Issue(dish.Definition, urgent: true);
            Assert.IsTrue(orders.IsUrgent);
            yield return WashAndRack(dish, camera, rackGrid, scrubTimeScale: 1f);

            Assert.AreEqual(1, orders.UrgentMet, "Racking the ordered dish did not meet the urgent order");
            Assert.IsNull(orders.ActiveOrder);
            Assert.AreEqual(1, flow.CurrentStreak, "Meeting the order did not extend Flow");

            ShiftResult result = null;
            var channel = GetPrivateField<ShiftCompletedEventChannel>(shiftController, "shiftCompletedChannel");
            System.Action<ShiftResult> capture = r => result = r;
            channel.OnRaised += capture;
            shiftController.EndShiftNow();
            channel.OnRaised -= capture;

            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.UrgentOrders);
            Assert.AreEqual(1, result.UrgentOrdersMet);
            Assert.AreEqual(ShiftScoring.OrderPoints, result.Score.Orders, 1e-3f);
            Assert.AreNotEqual(ShiftGrade.None, result.Grade, "The shift was not graded");
            Assert.AreEqual(shiftController.Quota, result.Quota, "The card's quota is not the night's");
            Assert.Less(result.Score.Work, ShiftScoring.WorkPoints, "Walking out after one dish met the whole night's quota");
            Assert.Less(result.QuotaBonus, ShiftResultCalculator.QuotaBonusHours * ShiftResultCalculator.HourlyWageEuros * 0.5f,
                "Walking out early earned the quota's whole hour");

            // The closing bell, then the result screen: skipped to its end, it says how the night went.
            var hud = Object.FindAnyObjectByType<ShiftHUD>();
            yield return WaitUntilOrFail(() => hud.ResultScreen.IsShown, 5f, "The result screen did not come up");
            hud.ResultScreen.Complete();
            string card = hud.ResultScreen.Describe();
            StringAssert.Contains(ShiftScoring.Letter(result.Grade), card, "The result screen does not show the grade");
            StringAssert.Contains("Pilne zamówienia 1 / 1", card);
            StringAssert.Contains($"{result.Score.Total}/100", card);
            StringAssert.Contains(PlaceMoney.Format(result.Wage, result.Place), card, "The result screen does not say what the night paid");
        }

        [UnityTest]
        public IEnumerator AMissedUrgentOrder_BreaksTheFlow_AndCostsTheShiftsScore()
        {
            AccessibilitySettings.RelaxedMode = true;
            yield return LoadKitchen();

            var shiftController = Object.FindAnyObjectByType<ShiftController>();
            var orders = Object.FindAnyObjectByType<ChefOrderController>();
            Camera camera = Camera.main;
            DishController dish = null;
            yield return WaitUntilOrFail(() => (dish = FindWashableDish(camera, null)) != null, 10f, "No dish to order");

            orders.Issue(dish.Definition, urgent: true);
            Time.timeScale = 10f;
            yield return WaitUntilOrFail(() => orders.ActiveOrder == null, 10f, "The urgent order never ran out");
            Time.timeScale = 1f;
            Assert.AreEqual(1, orders.UrgentMissed);
            Assert.AreEqual(0, orders.UrgentMet);

            ShiftResult result = null;
            var channel = GetPrivateField<ShiftCompletedEventChannel>(shiftController, "shiftCompletedChannel");
            System.Action<ShiftResult> capture = r => result = r;
            channel.OnRaised += capture;
            shiftController.EndShiftNow();
            channel.OnRaised -= capture;

            Assert.AreEqual(1, result.UrgentOrders);
            Assert.AreEqual(0, result.UrgentOrdersMet);
            Assert.AreEqual(0f, result.Score.Orders, 1e-3f, "A missed urgent order cost nothing");
        }

        /// <summary>
        /// An urgent order nobody can fill any more - every dish of the kind broke - is called off by the chef: it is not
        /// counted as missed, the Flow is kept and the shift's order points are not lost.
        /// </summary>
        [UnityTest]
        public IEnumerator AnUrgentOrderForADishThatBroke_IsCalledOff_WithoutBlame()
        {
            AccessibilitySettings.RelaxedMode = true;
            yield return LoadKitchen();

            var orders = Object.FindAnyObjectByType<ChefOrderController>();
            Camera camera = Camera.main;
            DishController dish = null;
            yield return WaitUntilOrFail(() => (dish = FindWashableDish(camera, null)) != null, 10f, "No dish to order");
            DishDefinition kind = dish.Definition;

            orders.Issue(kind, urgent: true);
            Assert.AreEqual(1, orders.UrgentIssued);
            // Every dish of the kind breaks - and any more of it that arrive meanwhile.
            var deadline = new PlayDeadline(4f);
            while (orders.ActiveOrder != null && !deadline.Passed)
            {
                foreach (DishController other in Object.FindObjectsByType<DishController>(FindObjectsInactive.Exclude))
                {
                    if (other.Definition == kind)
                    {
                        other.Shatter(other.transform.position);
                    }
                }

                yield return null;
            }

            Assert.IsNull(orders.ActiveOrder, "The order for a dish that is gone was never called off");
            Assert.AreEqual(1, orders.OrdersWithdrawn);
            Assert.AreEqual(0, orders.UrgentMissed, "An order nobody could fill was counted as missed");
            Assert.AreEqual(0, orders.UrgentIssued, "An order called off still counts against the shift");
        }

        [UnityTest]
        public IEnumerator AKnockThatBreaksAPlate_OnlyMakesAPanRing()
        {
            yield return LoadKitchen();

            var shiftController = Object.FindAnyObjectByType<ShiftController>();
            var pileController = Object.FindAnyObjectByType<DishPileController>();
            var rackGrid = Object.FindAnyObjectByType<RackGrid>();
            Vector3 plateSpot = FindClearCounter(shiftController, avoid: null);
            Vector3 panSpot = FindClearCounter(shiftController, avoid: plateSpot);
            DishController plate = DishController.Create(DishFromShift(shiftController, "plate"), plateSpot + Vector3.up * 0.2f, shiftController.transform, rackGrid);
            DishController pan = DishController.Create(DishFromShift(shiftController, "pan"), panSpot + Vector3.up * 0.3f, shiftController.transform, rackGrid);
            pileController.Register(plate);
            pileController.Register(pan);

            int broken = 0;
            int steelRang = 0;
            pileController.OnDishesBroken += count => broken += count;
            System.Action<Vector3, float, DishMaterial> onImpact = (point, speed, material) =>
            {
                if (material == DishMaterial.Steel)
                {
                    steelRang++;
                }
            };
            KitchenFeedbackEvents.Impact += onImpact;
            try
            {
                // Freshly dropped in, nothing breaks: only a dish that has come to rest in the pile can be knocked over.
                yield return WaitUntilOrFail(() => plate.HasLanded && pan.HasLanded, 8f, "The plate and the pan never came to rest");
                Assert.AreEqual(0, broken, "Something broke just landing on the counter");
                Assert.AreEqual(DishMaterial.Steel, pan.Definition.Material);
                Assert.Greater(pan.GetComponent<Rigidbody>().mass, plate.GetComponent<Rigidbody>().mass, "A pan should outweigh a plate");

                // Both thrown up the same way: they come down on the counter at about 6 units a second.
                plate.Knock(Vector3.up * 6f, Vector3.zero);
                pan.Knock(Vector3.up * 6f, Vector3.zero);
                yield return WaitUntilOrFail(() => plate == null, 5f, "The plate came down hard on the counter and did not break");
                yield return new WaitForSeconds(0.5f);
            }
            finally
            {
                KitchenFeedbackEvents.Impact -= onImpact;
            }

            Assert.AreEqual(1, broken, "Exactly the plate should have broken");
            Assert.IsTrue(pan != null, "The steel pan broke");
            Assert.Greater(steelRang, 0, "The pan landed without a sound");
        }

        [UnityTest]
        public IEnumerator OverloadedPile_Collapses_AndBreaksSomeDishes()
        {
            yield return LoadKitchen();

            var shiftController = Object.FindAnyObjectByType<ShiftController>();
            var pileController = Object.FindAnyObjectByType<DishPileController>();
            var rackGrid = Object.FindAnyObjectByType<RackGrid>();
            Assert.IsNotNull(shiftController);
            Assert.IsNotNull(pileController);

            yield return WaitUntilOrFail(() => shiftController.DishesSpawnedTotal >= 1, 10f, "No dish spawned");

            int brokenReported = 0;
            pileController.OnDishesBroken += count => brokenReported += count;

            OverloadPile(shiftController, pileController, rackGrid);

            yield return WaitUntilOrFail(() => brokenReported > 0, 10f,
                $"Pile did not report any broken dishes after overload (height {pileController.CurrentHeight:0.00}, " +
                $"collapse at {pileController.CollapseHeight:0.00})");

            Assert.Greater(shiftController.DishesBrokenTotal, 0,
                "ShiftController did not record the breakage reported by the pile");
        }

        [UnityTest]
        public IEnumerator Breakages_LowerTheShiftWage()
        {
            yield return LoadKitchen();

            var shiftController = Object.FindAnyObjectByType<ShiftController>();
            var pileController = Object.FindAnyObjectByType<DishPileController>();
            var rackGrid = Object.FindAnyObjectByType<RackGrid>();
            Camera camera = Camera.main;

            ShiftResult result = null;
            ShiftCompletedEventChannel channel = GetPrivateField<ShiftCompletedEventChannel>(shiftController, "shiftCompletedChannel");
            System.Action<ShiftResult> onCompleted = r => result = r;
            channel.OnRaised += onCompleted;

            int brokenReported = 0;
            pileController.OnDishesBroken += count => brokenReported += count;

            try
            {
                // Earn something first, so there is a wage for breakages to reduce.
                DishController first = null;
                yield return WaitUntilOrFail(() => (first = FindWashableDish(camera, null)) != null, 10f, "No dish to wash");
                yield return WashAndRack(first, camera, rackGrid);
                shiftController.TryStartDishwasherCycle();
                Time.timeScale = 5f;
                yield return WaitUntilOrFail(() => shiftController.DishesCleanedTotal >= 1, 15f, "Dishwasher cycle did not finish");
                Time.timeScale = 1f;

                // The ignored pile may well have collapsed on its own by now (every dish spawned while the
                // clock ran 5x); if not, overload it.
                if (brokenReported == 0)
                {
                    OverloadPile(shiftController, pileController, rackGrid);
                }

                yield return WaitUntilOrFail(() => brokenReported > 0, 10f,
                    $"Pile did not collapse (height {pileController.CurrentHeight:0.00}, collapse at " +
                    $"{pileController.CollapseHeight:0.00})");

                // Fast-forward to the end of the shift (3 min of game time).
                Time.timeScale = 20f;
                yield return WaitUntilOrFail(() => result != null, 40f, "Shift never ended / ShiftCompleted was not raised");
                Time.timeScale = 1f;
            }
            finally
            {
                channel.OnRaised -= onCompleted;
            }

            Assert.GreaterOrEqual(result.DishesCleaned, 1);
            Assert.Greater(result.DishesBroken, 0, "Result did not carry the breakages");
            Assert.AreEqual(shiftController.DishesBrokenTotal, result.DishesBroken);

            float wageWithoutBreakages = ShiftResultCalculator.Calculate(new List<WashStage>(), result.DishesCleaned, 0,
                result.ElapsedSeconds, result.PlannedSeconds, result.FullRackLoads).Wage;
            Assert.Less(result.Wage, wageWithoutBreakages,
                $"Wage {result.Wage} EUR with {result.DishesBroken} broken should be below {wageWithoutBreakages} EUR");
            // 3 EUR a dish, but never more than 30% of the base left after any docking for a night with little washed (at 20x
            // the ignored pile can collapse a dozen times).
            float deductions = Mathf.Min(result.DishesBroken * ShiftResultCalculator.DeductionPerDishBroken,
                ShiftResultCalculator.MaxDeductionShare * (result.BaseWage - result.IdleDocked));
            Assert.AreEqual(deductions, result.Deductions, 1e-3f);
        }

        [UnityTest]
        public IEnumerator AFinishedShift_IsWrittenToTheLocalGameplayDiary()
        {
            AccessibilitySettings.RelaxedMode = true;
            yield return LoadKitchen();

            var shiftController = Object.FindAnyObjectByType<ShiftController>();
            var rackGrid = Object.FindAnyObjectByType<RackGrid>();
            Camera camera = Camera.main;

            ShiftResult result = null;
            ShiftCompletedEventChannel channel = GetPrivateField<ShiftCompletedEventChannel>(shiftController, "shiftCompletedChannel");
            System.Action<ShiftResult> onCompleted = r => result = r;
            channel.OnRaised += onCompleted;
            try
            {
                DishController dish = null;
                yield return WaitUntilOrFail(() => (dish = FindWashableDish(camera, null)) != null, 10f, "No dish to wash");
                yield return WashAndRack(dish, camera, rackGrid);
                shiftController.EndShiftNow(); // "Kończę zmianę" from the pause menu
                yield return WaitUntilOrFail(() => result != null, 5f, "ShiftCompleted was not raised");
                // The closing bell first, then the result card: shown at its end, with the pay broken down.
                var hud = Object.FindAnyObjectByType<ShiftHUD>();
                yield return WaitUntilOrFail(() => hud.ResultScreen.IsShown, 5f, "The result screen did not come up");
                hud.ResultScreen.Complete();
                yield return null;
                TestScreenshot.Capture(camera, "23_shift_result.png");
            }
            finally
            {
                channel.OnRaised -= onCompleted;
            }

            List<ShiftLogEntry> shifts = GameplayLog.Read<ShiftLogEntry>("shift");
            Assert.AreEqual(1, shifts.Count, $"Expected one shift in {GameplayLog.FilePath}");
            ShiftLogEntry entry = shifts[0];

            Assert.AreEqual(shiftController.Definition.Id, entry.shiftId);
            Assert.IsTrue(entry.relaxedMode);
            Assert.IsTrue(entry.clockedOutEarly, "Ended from the pause menu, well before the clock ran out");
            Assert.AreEqual(result.PlannedSeconds, entry.plannedSeconds, 1e-3f);
            Assert.AreEqual(result.Wage, entry.wageEuros, 1e-3f);
            Assert.Less(result.BaseWage, ShiftResultCalculator.FullShiftBaseWage, "Clocking out early pays the hours worked");
            Assert.AreEqual(shiftController.DishesSpawnedTotal, entry.dishesArrived);

            // The washed dish went to the rack: its time at each step in the sink is in the diary.
            Assert.AreEqual(1, entry.dishesTimed);
            Assert.Greater(entry.scrapeSeconds, 0f, "scrape");
            Assert.Greater(entry.rinseSeconds, 0f, "rinse");
            Assert.Greater(entry.scrubSeconds + entry.inspectSeconds, 0f, "scrub");
            Assert.Greater(entry.toRackSeconds, 0f, "to the rack");
            Assert.Greater(entry.averageFps, 0f);
        }

        [UnityTest]
        public IEnumerator FulfillingChefOrder_ExtendsFlowStreak()
        {
            // The pile collapsing mid-test could smash the one dish the order needs; relaxed mode disables it.
            AccessibilitySettings.RelaxedMode = true;
            yield return LoadKitchen();

            var shiftController = Object.FindAnyObjectByType<ShiftController>();
            var orders = Object.FindAnyObjectByType<ChefOrderController>();
            var flow = Object.FindAnyObjectByType<FlowStreakController>();
            var rackGrid = Object.FindAnyObjectByType<RackGrid>();
            Camera camera = Camera.main;
            Assert.IsNotNull(orders);
            Assert.IsNotNull(flow);
            Assert.AreEqual(0, flow.CurrentStreak);

            // What the chef calls for is a dice roll over everything in reach, and that ranges from a plate (about 8 s of scrubbing
            // at best) to a roasting dish or a pan (over a minute): washing whatever he called for within a fixed budget passed or
            // failed with his dice (measured 2026-10-08; see QA_REPORT.md). So the test makes the call itself - Issue is the shift's
            // own entry for exactly this - for a plate, and nothing else calls meanwhile. Everything after the call is as it was:
            // washed, racked, through the machine, and the Flow streak must grow. (That the chef calls on his own, for a dish in
            // reach, is ChefOrder_IsCalledOnItsOwn_ForADishThatIsInReach.)
            orders.OrdersOn = false;
            Time.timeScale = 8f; // dishes keep arriving: don't sit through that in real time
            yield return WaitUntilOrFail(() => FindWashableDishWithId(camera, "plate") != null, 20f, "No plate came within reach");
            Time.timeScale = 1f;

            DishController target = null;
            var findDeadline = new PlayDeadline(10f);
            while ((target = FindWashableDishWithId(camera, "plate")) == null && !findDeadline.Passed)
            {
                yield return null;
            }

            DishDefinition plateKind = target != null ? target.Definition : null;
            Assert.IsNotNull(target, "No reachable plate");
            DishDefinition ordered = plateKind;
            orders.Issue(ordered, urgent: false);
            Assert.AreSame(ordered, orders.ActiveOrder);

            // Real-time scrubbing here: speeding the clock up would also eat into the order's time window.
            yield return WashAndRack(target, camera, rackGrid, scrubTimeScale: 1f);

            yield return WaitUntilOrFail(() => !Object.FindAnyObjectByType<DishwasherController>().IsRunning, 25f,
                "Dishwasher stayed busy");
            shiftController.TryStartDishwasherCycle();

            Time.timeScale = 5f;
            yield return WaitUntilOrFail(() => flow.CurrentStreak >= 1 || orders.ActiveOrder != ordered, 15f,
                "Order neither fulfilled nor cleared");
            Time.timeScale = 1f;

            Assert.AreEqual(1, flow.CurrentStreak, "Delivering the ordered dish through the dishwasher did not extend Flow");
        }

        [UnityTest]
        public IEnumerator ChefOrder_IsCalledOnItsOwn_ForADishThatIsInReach()
        {
            AccessibilitySettings.RelaxedMode = true;
            yield return LoadKitchen();

            var orders = Object.FindAnyObjectByType<ChefOrderController>();
            Assert.IsNotNull(orders);
            Assert.IsNull(orders.ActiveOrder);

            // First order comes 20-35s into the shift; don't sit through that in real time.
            Time.timeScale = 8f;
            yield return WaitUntilOrFail(() => orders.ActiveOrder != null, 10f, "Chef never issued an order");
            Time.timeScale = 1f;

            // He calls only for what the porter can act on: a dish of that kind that is still to be washed is there.
            DishDefinition ordered = orders.ActiveOrder;
            bool inReach = false;
            foreach (DishController dish in Object.FindObjectsByType<DishController>())
            {
                inReach |= dish.Definition == ordered && dish.CurrentStage < WashStage.Racked;
            }

            Assert.IsTrue(inReach, $"The chef called for {ordered.name}, and there is none to wash");
            Assert.Greater(orders.OrderTimeRemaining, 0f);
            Assert.AreEqual(1, orders.OrdersIssued);
        }

        [UnityTest]
        public IEnumerator BigDishwasher_TakesADirtyDish_WashesForHalfAnHourWhileTheGameIsClosed_AndPaysOnUnload()
        {
            AccessibilitySettings.RelaxedMode = true;
            var now = new System.DateTime(2026, 10, 1, 18, 0, 0, System.DateTimeKind.Utc);
            BigDishwasherController.Clock = () => now;
            try
            {
                yield return LoadKitchen();
                var machine = Object.FindAnyObjectByType<BigDishwasherController>();
                var panel = Object.FindAnyObjectByType<BigDishwasherPanel>();
                var station = Object.FindAnyObjectByType<WashStation>();
                Camera camera = Camera.main;
                Assert.IsNotNull(machine, "The kitchen has no big dishwasher");
                Assert.IsNotNull(panel, "The big dishwasher has no controls");
                Assert.AreEqual(0, machine.Count);

                // A dish tapped in the pile lands in the sink, dirty - and the machine offers to take it as it is.
                DishController dish = null;
                yield return WaitUntilOrFail(() => (dish = FindWashableDish(camera, null)) != null, 10f, "No dish to load");
                string dishId = dish.Definition.Id;
                yield return Tap(VisiblePointOrFail(dish, camera));
                Assert.AreSame(dish, station.Occupant, "The tapped dish did not go to the sink");
                yield return WaitUntilOrFail(() => !dish.IsMoving, 3f, "The dish never arrived in the sink");
                yield return null;
                Assert.IsTrue(panel.LoadButton.gameObject.activeInHierarchy, "No button to put the dirty dish into the big dishwasher");

                panel.LoadButton.onClick.Invoke();
                Assert.AreEqual(1, machine.Count);
                Assert.IsNull(station.Occupant, "Loading the machine did not free the sink");
                yield return WaitUntilOrFail(() => dish == null, 2f, "The dish never went into the machine");
                Assert.AreEqual(dishId, SaveSystem.Load().bigDishwasherLoad[0], "The load was not saved with the game");

                // The card's button sits over the counter: a touch on it is the button's, never a grab at a dish.
                yield return null;
                Assert.IsTrue(panel.ActionButton.gameObject.activeInHierarchy, "No start button for a loaded machine");
                var actionRect = (RectTransform)panel.ActionButton.transform;
                Vector2 onButton = RectTransformUtility.WorldToScreenPoint(null, actionRect.TransformPoint(actionRect.rect.center));
                Assert.IsTrue(DishInputRouter.IsOverControl != null && DishInputRouter.IsOverControl(onButton),
                    "A touch on the big dishwasher's button would reach the dishes under it");
                TestScreenshot.Capture(camera, "24_big_dishwasher_loaded.png");

                panel.ActionButton.onClick.Invoke();
                Assert.IsTrue(machine.IsRunning, "The start button did not start the machine");
                yield return null;
                Assert.IsFalse(panel.ActionButton.gameObject.activeInHierarchy, "The running machine still offers a button");

                // The game is closed mid-cycle and opened again forty minutes later: the load is clean and waiting.
                now = now.AddMinutes(40);
                yield return LoadKitchen();
                machine = Object.FindAnyObjectByType<BigDishwasherController>();
                panel = Object.FindAnyObjectByType<BigDishwasherPanel>();
                var shiftController = Object.FindAnyObjectByType<ShiftController>();
                yield return null;
                Assert.AreEqual(1, machine.Count, "The machine forgot its load across the restart");
                Assert.IsTrue(machine.IsDone, "Forty minutes later the half-hour cycle was not done");
                Assert.IsTrue(panel.ActionButton.gameObject.activeInHierarchy, "No button to unload the clean dishes");
                TestScreenshot.Capture(Camera.main, "25_big_dishwasher_done.png");

                panel.ActionButton.onClick.Invoke();
                Assert.AreEqual(0, machine.Count, "Unloading left dishes in the machine");
                Assert.AreEqual(1, shiftController.DishesCleanedTotal, "The unloaded dish did not count as washed this shift");
                Assert.AreEqual(1, shiftController.BigDishwasherUnloaded);
                Assert.AreEqual(0, SaveSystem.Load().bigDishwasherLoad.Count, "The emptied machine was not saved");
            }
            finally
            {
                BigDishwasherController.Clock = () => System.DateTime.UtcNow;
            }
        }

        [UnityTest]
        public IEnumerator Soaking_FreesTheHands_SoftensTheDirt_AndATouchTakesTheDishBackOut()
        {
            AccessibilitySettings.RelaxedMode = true;
            yield return LoadKitchen();
            var panel = Object.FindAnyObjectByType<SoakPanel>();
            var station = Object.FindAnyObjectByType<WashStation>();
            var bigDishwasher = Object.FindAnyObjectByType<BigDishwasherPanel>();
            Camera camera = Camera.main;
            Assert.IsNotNull(panel, "The kitchen has no soaking button");

            // A scraped, rinsed dish in the sink can go down into the water to soak.
            DishController dish = null;
            yield return WaitUntilOrFail(() => (dish = FindWashableDish(camera, null)) != null, 10f, "No dish to soak");
            yield return ClaimScrapeAndRinse(dish, camera);
            Release(_mouse.leftButton);
            yield return null;
            yield return null;
            Assert.IsTrue(panel.Button.gameObject.activeInHierarchy && panel.Button.interactable, "No button to put the dish in to soak");

            panel.Button.onClick.Invoke();
            Assert.IsTrue(dish.IsSoaking, "The button did not put the dish in to soak");
            Assert.AreSame(dish, station.Soaking);
            Assert.IsNull(station.Occupant, "Soaking a dish did not free the hands");
            yield return WaitUntilOrFail(() => !dish.IsMoving, 3f, "The dish never settled in the water");
            Assert.Less(dish.Collider.bounds.center.y, station.WaterLevelY, "The soaking dish is not down in the water");
            yield return new WaitForSeconds(0.5f);
            TestScreenshot.Capture(camera, "26_kitchen_soaking.png");

            // Meanwhile the next dish comes to the sink, over the soaking one, which waits under it.
            DishController next = null;
            yield return WaitUntilOrFail(() => (next = FindWashableDish(camera, null)) != null, 10f, "No second dish");
            yield return Tap(VisiblePointOrFail(next, camera));
            Assert.AreSame(next, station.Occupant, "With one dish soaking, the next one could not come to the sink");
            yield return null;
            Assert.IsTrue(panel.Button.gameObject.activeInHierarchy && !panel.Button.interactable,
                "While both hands are busy, the soak should show its progress but offer nothing");

            Time.timeScale = 8f;
            yield return WaitUntilOrFail(() => dish.SoakProgress01 >= 1f, 6f, "The soak never finished");
            Time.timeScale = 1f;
            Assert.IsTrue(dish.IsSoaking, "The soaking dish came out while the hands were full");

            // Hands free again (the second dish goes into the big dishwasher): a touch on the soaking dish lifts it out.
            yield return WaitUntilOrFail(() => !next.IsMoving, 3f, "The second dish never arrived in the sink");
            yield return null;
            bigDishwasher.LoadButton.onClick.Invoke();
            Assert.IsNull(station.Occupant);
            yield return WaitUntilOrFail(() => next == null, 2f, "The second dish never went into the machine");

            Bounds bounds = dish.Collider.bounds;
            yield return Tap(camera.WorldToScreenPoint(bounds.center + Vector3.up * bounds.extents.y * 0.5f));
            Assert.IsFalse(dish.IsSoaking, "Touching the soaking dish did not take it out of the water");
            Assert.AreSame(dish, station.Occupant, "The dish taken out of the water is not in the hands");
            Assert.IsNull(station.Soaking);
            Assert.AreEqual(WashStage.AwaitingScrub, dish.CurrentStage);
            Assert.AreEqual(1f, dish.SoakProgress01, 1e-5f, "Coming out of the water lost the soak");
        }

        /// <summary>
        /// The first day of a new life opens on the guided plate: the kitchen holds still (no clock, one dish, no chef) while
        /// the card walks the player through every step, each step moving on only once it is done; starting the dishwasher
        /// ends the lesson, the clock starts and the lesson is kept in the save - the next first shift does not teach again.
        /// </summary>
        [UnityTest]
        public IEnumerator DayOne_TheGuidedPlate_TeachesEachStep_HoldsTheKitchen_AndIsTaughtOnce()
        {
            AccessibilitySettings.RelaxedMode = true;
            ShiftPlan.Schedule = ShiftSchedule.For(1, SpecialShift.None, tutorialBreakdownDone: false, lastShiftBrokeDown: false);
            yield return LoadKitchen();

            var shiftController = Object.FindAnyObjectByType<ShiftController>();
            var tutorial = Object.FindAnyObjectByType<KitchenTutorial>();
            var rackGrid = Object.FindAnyObjectByType<RackGrid>();
            var hud = Object.FindAnyObjectByType<ShiftHUD>();
            Camera camera = Camera.main;
            Assert.IsNotNull(tutorial, "The kitchen has no guided plate");
            Assert.IsTrue(shiftController.TutorialActive && shiftController.TutorialHold, "Day one did not open on the guided plate");

            yield return WaitUntilOrFail(() => tutorial.Step == TutorialStep.PickDish && tutorial.CardShown, 5f, "The first card never came up");
            DishController plate = null;
            yield return WaitUntilOrFail(() => (plate = FindWashableDish(camera, null)) != null, 10f, "The guided plate never came");
            Assert.IsFalse(tutorial.ShowsDishwasherButton, "The dishwasher's button was up before its step");
            TestScreenshot.Capture(camera, "tutorial_01_pick.png");

            // Every step the lesson goes through, frame by frame.
            var recorder = new GameObject("TutorialStepRecorder").AddComponent<TutorialStepRecorder>();
            recorder.Tutorial = tutorial;
            List<TutorialStep> steps = recorder.Seen;
            yield return WashAndRack(plate, camera, rackGrid, stage =>
            {
                Assert.AreEqual(1, shiftController.DishesSpawnedTotal, "More dishes came while the plate was being taught");
                Assert.AreEqual(0f, shiftController.Progress01, 1e-4f, "The clock ran during the lesson");
            });

            yield return WaitUntilOrFail(() => tutorial.Step == TutorialStep.Dishwasher, 3f, $"The lesson did not get to the dishwasher (it is at {tutorial.Step})");
            Assert.IsTrue(tutorial.ShowsDishwasherButton);
            CollectionAssert.IsOrdered(steps, $"The lesson's steps went backwards: {string.Join(", ", steps)}");
            CollectionAssert.Contains(steps, TutorialStep.Tool, $"The guided plate skipped the tool lesson: {string.Join(", ", steps)}");
            CollectionAssert.Contains(steps, TutorialStep.Scrub, "The guided plate skipped the scrubbing lesson");
            CollectionAssert.Contains(steps, TutorialStep.Rack, "The guided plate skipped the rack lesson");
            TestScreenshot.Capture(camera, "tutorial_02_dishwasher.png");

            GetPrivateField<Button>(hud, "startDishwasherButton").onClick.Invoke();
            yield return WaitUntilOrFail(() => tutorial.Step == TutorialStep.Done || tutorial.Step == TutorialStep.Off, 3f, "Starting the machine did not end the lesson");
            Assert.IsFalse(shiftController.TutorialHold, "The kitchen still holds after the lesson");
            Assert.IsTrue(TutorialProgress.Has(TutorialProgress.FirstDish), "The lesson was not kept in the save");
            yield return WaitUntilOrFail(() => shiftController.Progress01 > 0f, 5f, "The clock did not start after the lesson");

            // The same first day again (a restart): no second lesson.
            ShiftPlan.Schedule = ShiftSchedule.For(1, SpecialShift.None, tutorialBreakdownDone: false, lastShiftBrokeDown: false);
            yield return LoadKitchen();
            shiftController = Object.FindAnyObjectByType<ShiftController>();
            yield return null;
            Assert.IsFalse(shiftController.TutorialActive, "The guided plate was taught a second time");
        }

        /// <summary>
        /// The quick way onto the rack: a clean dish flicked up from the sink flies into the spot it packs into best; a quick
        /// sideways swipe is not a throw - the dish stays in the sink.
        /// </summary>
        [UnityTest]
        public IEnumerator AFlickUpFromTheSink_ThrowsTheDishOntoTheRack_ASidewaysSwipeDoesNot()
        {
            AccessibilitySettings.RelaxedMode = true;
            yield return LoadKitchen();

            var rackGrid = Object.FindAnyObjectByType<RackGrid>();
            var washStation = Object.FindAnyObjectByType<WashStation>();
            Camera camera = Camera.main;
            DishController dish = null;
            yield return WaitUntilOrFail(() => (dish = FindWashableDish(camera, null)) != null, 10f, "No dish to wash");
            yield return ClaimScrapeAndRinse(dish, camera);
            if (dish.CurrentStage == WashStage.AwaitingScrub)
            {
                // Scrubbed clean the same way, but not dragged anywhere: the finger lifts over the sink.
                yield return ScrubOnly(dish, camera);
            }

            Release(_mouse.leftButton);
            yield return WaitUntilOrFail(() => !dish.IsMoving, 2f, "The clean dish did not settle in the sink");
            Assert.AreEqual(WashStage.ReadyForRack, dish.CurrentStage);

            // Sideways away from the rack, so the swipe cannot simply drop it there.
            Vector3? best = rackGrid.BestSpotFor(dish.Definition);
            Assert.IsTrue(best.HasValue);
            float awayFromRack = camera.WorldToScreenPoint(best.Value).x > VisiblePointOrFail(dish, camera).x ? -1f : 1f;
            yield return Flick(dish, camera, new Vector2(110f * awayFromRack, 0f));
            yield return WaitUntilOrFail(() => !dish.IsMoving, 2f, "The dish did not come back to the sink");
            Assert.AreEqual(WashStage.ReadyForRack, dish.CurrentStage, "A sideways swipe threw the dish onto the rack");
            Assert.AreSame(dish, washStation.Occupant, "A sideways swipe took the dish off the sink");

            yield return Flick(dish, camera, new Vector2(0f, 90f));
            Assert.AreEqual(WashStage.Racked, dish.CurrentStage, "A flick up did not throw the dish onto the rack");
            Assert.AreEqual(1, rackGrid.DishCount);
            Assert.IsNull(washStation.Occupant, "The thrown dish still holds the sink");
            yield return new WaitForSeconds(0.4f); // its flight
            Vector3 flat = dish.transform.position - best.Value;
            flat.y = 0f;
            Assert.Less(flat.magnitude, 0.2f, "The dish did not land in the spot it packs into best");
        }

        /// <summary>
        /// A full rack starts the machine by itself after a short count; a tap on its button stops the count (to pack more
        /// in) and nothing starts while the rack stays as it is; the next dish racked counts again and the machine starts.
        /// </summary>
        [UnityTest]
        public IEnumerator AFullRack_CountsDownAndStartsTheMachine_ATapStopsTheCount()
        {
            AccessibilitySettings.RelaxedMode = true;
            AccessibilitySettings.AutoDishwasher = true;
            yield return LoadKitchen();

            var shiftController = Object.FindAnyObjectByType<ShiftController>();
            var rackGrid = Object.FindAnyObjectByType<RackGrid>();
            var dishwasher = Object.FindAnyObjectByType<DishwasherController>();
            Button dishwasherButton = GetPrivateField<Button>(Object.FindAnyObjectByType<ShiftHUD>(), "startDishwasherButton");
            DishDefinition glass = DishFromShift(shiftController, "glass");
            Vector3 aside = shiftController.transform.position + Vector3.right * 40f;

            bool RackOne()
            {
                DishController dish = DishController.Create(glass, aside, shiftController.transform, rackGrid);
                dish.GetComponent<Rigidbody>().isKinematic = true;
                if (rackGrid.TryPlaceBest(dish))
                {
                    return true;
                }

                Object.Destroy(dish.gameObject);
                return false;
            }

            int cells = glass.RackFootprint.x * glass.RackFootprint.y;
            while (rackGrid.OccupiedCells + cells < rackGrid.FullLoadCells)
            {
                Assert.IsTrue(RackOne(), "The rack filled up before a full load");
            }

            yield return null;
            yield return null;
            Assert.Less(shiftController.AutoStartIn, 0f, "A rack short of a full load started counting");

            Assert.IsTrue(RackOne());
            Assert.IsTrue(rackGrid.IsFullLoad);
            yield return null;
            Assert.Greater(shiftController.AutoStartIn, 0f, "A full rack did not count down");
            Assert.LessOrEqual(shiftController.AutoStartIn, ShiftController.AutoStartSeconds);

            dishwasherButton.onClick.Invoke();
            Assert.Less(shiftController.AutoStartIn, 0f, "The tap did not stop the count");
            Assert.IsFalse(dishwasher.IsRunning, "The tap that stops the count started the machine");
            yield return new WaitForSeconds(ShiftController.AutoStartSeconds + 0.5f);
            Assert.IsFalse(dishwasher.IsRunning, "The machine started after the count was stopped");

            Assert.IsTrue(RackOne(), "No room on the full rack for one more glass");
            yield return null;
            Assert.Greater(shiftController.AutoStartIn, 0f, "Another dish on the rack did not count again");
            yield return WaitUntilOrFail(() => dishwasher.IsRunning, ShiftController.AutoStartSeconds + 2f, "The full rack never started the machine");
            Assert.Less(shiftController.AutoStartIn, 0f);
            Assert.AreEqual(0, rackGrid.DishCount, "The load stayed on the rack");
        }

        /// <summary>Notes each step of the guided plate as the lesson comes to it.</summary>
        private sealed class TutorialStepRecorder : MonoBehaviour
        {
            public KitchenTutorial Tutorial;
            public readonly List<TutorialStep> Seen = new List<TutorialStep>();

            private void Update()
            {
                if (Tutorial != null && (Seen.Count == 0 || Seen[Seen.Count - 1] != Tutorial.Step))
                {
                    Seen.Add(Tutorial.Step);
                }
            }
        }

        /// <summary>A quick swipe from the dish in the sink, <paramref name="by"/> pixels, released at its end.</summary>
        private IEnumerator Flick(DishController dish, Camera camera, Vector2 by)
        {
            Vector2 from = VisiblePointOrFail(dish, camera);
            Time.timeScale = 0.25f; // three frames well inside a swipe's time, however slow the editor's frames are
            HoldAt(from);
            yield return null;
            for (int i = 1; i <= 3; i++)
            {
                HoldAt(from + by * (i / 3f));
                yield return null;
            }

            Release(_mouse.leftButton);
            yield return null;
            Time.timeScale = 1f;
        }

        /// <summary>The scrub of <see cref="ScrubAndRack"/> without the drag to the rack: the finger stays down on the clean dish.</summary>
        private IEnumerator ScrubOnly(DishController dish, Camera camera)
        {
            Rect footprint = ScreenFootprint(dish, camera);
            float rowStep = Mathf.Max(2f, Mathf.Min(footprint.height / 12f, footprint.width / 12f));
            float columnStep = Mathf.Max(2f, footprint.width / 10f);
            bool leftToRight = true;
            var deadline = new PlayDeadline(30f);
            Time.timeScale = 3f;
            while (dish.CurrentStage == WashStage.AwaitingScrub && !deadline.Passed)
            {
                for (float y = footprint.yMax; y >= footprint.yMin && dish.CurrentStage == WashStage.AwaitingScrub; y -= rowStep)
                {
                    for (float t = 0f; t <= footprint.width && dish.CurrentStage == WashStage.AwaitingScrub; t += columnStep)
                    {
                        SelectToolForRemainingDirt(dish);
                        HoldAt(new Vector2(leftToRight ? footprint.xMin + t : footprint.xMax - t, y));
                        yield return null;
                    }

                    leftToRight = !leftToRight;
                }
            }

            Time.timeScale = 1f;
            Assert.AreEqual(WashStage.ReadyForRack, dish.CurrentStage, $"Scrubbing did not finish {dish.name} (dirt left: {DescribeDirt(dish)})");
        }

        /// <summary>
        /// Not a check - a picture for judging the art: the big dishwasher full (forty dishes of every kind) and ten
        /// minutes into its cycle, steaming. Explicit (Logs/Screenshots/art_big_dishwasher_full.png).
        /// </summary>
        [UnityTest, Explicit]
        public IEnumerator ArtPreview_BigDishwasher_Full()
        {
            var now = new System.DateTime(2026, 10, 1, 18, 0, 0, System.DateTimeKind.Utc);
            BigDishwasherController.Clock = () => now;
            try
            {
                string[] kinds = { "plate", "plate", "pan", "glass", "plate" };
                var save = new SaveData { bigDishwasherStartedUtcTicks = now.AddMinutes(-10).Ticks };
                for (int i = 0; i < 40; i++)
                {
                    save.bigDishwasherLoad.Add(kinds[i % kinds.Length]);
                }

                SaveSystem.Save(save);
                yield return LoadKitchen();
                yield return new WaitForSeconds(1.5f); // the steam gets going
                TestScreenshot.Capture(Camera.main, "art_big_dishwasher_full.png", 1080, 2340);
            }
            finally
            {
                BigDishwasherController.Clock = () => System.DateTime.UtcNow;
            }
        }

        /// <summary>
        /// Not a check - pictures for judging the art, at a tall phone's shape (1080x2340, 19.5:9): the kitchen with
        /// dirty dishes in the crate, then each kind of dish in the sink as it arrives, mid-scrub with the hand on
        /// it, held to the light (the glass), clean, and finally the loaded rack. Explicit, so a normal run skips it;
        /// the images are Logs/Screenshots/art_*.png.
        /// </summary>
        [UnityTest, Explicit]
        public IEnumerator ArtPreview_EachDish_DirtyScrubbedAndClean()
        {
            AccessibilitySettings.RelaxedMode = true; // a collapse would smash the dishes being photographed
            yield return LoadKitchen();

            var shiftController = Object.FindAnyObjectByType<ShiftController>();
            var rackGrid = Object.FindAnyObjectByType<RackGrid>();
            Camera camera = Camera.main;
            const int width = 1080;
            const int height = 2340;

            // Plate, pan and glass all in the crate: the glass is the third dish of the shift, and a fourth on top
            // makes a pile worth looking at.
            Time.timeScale = 8f;
            yield return WaitUntilOrFail(() => shiftController.DishesSpawnedTotal >= 4, 20f, "The shift never brought four dishes");
            Time.timeScale = 1f;
            yield return WaitUntilOrFail(() => FindWashableDish(camera, null) != null, 10f, "The pile never settled");
            TestScreenshot.Capture(camera, "art_00_kitchen.png", width, height);

            // The roaster before the pot: the rack then needs emptying at most once (a second cycle is the one that
            // always breaks down).
            string[] ids = { "plate", "pan", "glass", "bowl", "mug", "roaster", "pot" };
            for (int i = 0; i < ids.Length; i++)
            {
                string id = ids[i];
                DishController dish = null;
                Time.timeScale = 4f; // the later dishes are further down the shift's queue
                yield return WaitUntilOrFail(() => (dish = FindWashableDishWithId(camera, id)) != null, 30f, $"No {id} to photograph");
                Time.timeScale = 1f;
                if (!rackGrid.HasRoomFor(dish.Definition))
                {
                    // Six kinds of dish fill the rack (the pot alone takes 3x3): send the load off first, as a porter would.
                    Button startButton = GameObject.Find("StartDishwasherButton").GetComponent<Button>();
                    Debug.Log($"ArtPreview {id}: rack full ({rackGrid.DishCount} dishes, {rackGrid.OccupiedCells}/{rackGrid.CellCount} cells); " +
                              $"start button active {startButton.gameObject.activeInHierarchy}, interactable {startButton.IsInteractable()}");
                    var machine = Object.FindAnyObjectByType<DishwasherController>();
                    startButton.onClick.Invoke();
                    if (!machine.IsRunning)
                    {
                        // A full rack counts down to starting itself, and the first tap on the button only stops that count (the HUD
                        // says "add something more first"): the second tap starts the cycle. Tapping once left the machine idle for good.
                        startButton.onClick.Invoke();
                    }

                    Time.timeScale = 4f;
                    DishDefinition kind = dish.Definition;
                    string lastState = null;
                    yield return WaitUntilOrFail(() =>
                    {
                        // Says what the machine was doing while the porter waited (a failed wait is otherwise only "never made room").
                        string state = $"running {machine.IsRunning}, broken {machine.IsBroken}, breakdowns this shift {machine.Breakdowns}, load {machine.LoadCount}, " +
                                       $"rack {rackGrid.DishCount} dishes {rackGrid.OccupiedCells}/{rackGrid.CellCount} cells";
                        if (state != lastState)
                        {
                            lastState = state;
                            Debug.Log($"ArtPreview {id}: waiting for room in the rack - dishwasher {state}");
                        }

                        return rackGrid.HasRoomFor(kind);
                    }, 20f, $"The rack never made room for the {id}");
                    Time.timeScale = 1f;
                }

                string prefix = $"art_{i + 1}_{id}";
                yield return WashAndRack(dish, camera, rackGrid, stage =>
                {
                    switch (stage)
                    {
                        case WashStage.AwaitingRinse:
                            TestScreenshot.Capture(camera, $"{prefix}_a_dirty.png", width, height);
                            break;
                        case WashStage.AwaitingScrub:
                            TestScreenshot.Capture(camera, $"{prefix}_b_scrubbing.png", width, height);
                            break;
                        case WashStage.ReadyForRack:
                            TestScreenshot.Capture(camera, $"{prefix}_d_clean.png", width, height);
                            break;
                    }
                }, onInspection: () => UnderTheLight(prefix));
            }

            TestScreenshot.Capture(camera, "art_9_rack.png", width, height);

            IEnumerator UnderTheLight(string prefix)
            {
                yield return new WaitForSeconds(0.6f);
                TestScreenshot.Capture(camera, $"{prefix}_c_under_the_light.png", width, height);
            }
        }

        /// <summary>
        /// Not a check - pictures of what is left on each kind of dish, held in the sink before it is scraped (the
        /// moment a player sees it closest): Logs/Screenshots/art_leftovers_*.png. Explicit.
        /// </summary>
        [UnityTest, Explicit]
        public IEnumerator ArtPreview_Leftovers()
        {
            AccessibilitySettings.RelaxedMode = true;
            yield return LoadKitchen();
            Camera camera = Camera.main;
            var station = Object.FindAnyObjectByType<WashStation>();
            var bigDishwasher = Object.FindAnyObjectByType<BigDishwasherPanel>();
            foreach (string id in new[] { "plate", "pan", "glass", "bowl", "mug", "pot", "roaster" })
            {
                DishController dish = null;
                Time.timeScale = 8f;
                yield return WaitUntilOrFail(() => (dish = FindWashableDishWithId(camera, id)) != null, 30f, $"No {id} to photograph");
                Time.timeScale = 1f;
                yield return Tap(VisiblePointOrFail(dish, camera));
                Assert.AreSame(dish, station.Occupant, $"The {id} did not go to the sink");
                yield return WaitUntilOrFail(() => !dish.IsMoving, 3f, $"The {id} never arrived in the sink");
                yield return null;
                TestScreenshot.Capture(camera, $"art_leftovers_{id}.png", 1080, 2340);

                // Out of the way for the next one, as it is - into the big dishwasher.
                bigDishwasher.LoadButton.onClick.Invoke();
                yield return WaitUntilOrFail(() => dish == null, 2f, $"The {id} never went into the machine");
            }
        }

        /// <summary>
        /// Not a check - pictures of the scraper and the spray at work on a pan (burnt crust, then grease), mid-rub with
        /// the hand holding each: Logs/Screenshots/art_tools_*.png. Explicit, so a normal run skips it.
        /// </summary>
        [UnityTest, Explicit]
        public IEnumerator ArtPreview_ScraperAndSpray()
        {
            AccessibilitySettings.RelaxedMode = true;
            yield return LoadKitchen();
            Camera camera = Camera.main;
            const int width = 1080;
            const int height = 2340;

            DishController pan = null;
            Time.timeScale = 8f;
            yield return WaitUntilOrFail(() => (pan = FindWashableDishWithId(camera, "pan")) != null, 20f, "No pan to photograph");
            Time.timeScale = 1f;
            yield return ClaimScrapeAndRinse(pan, camera);

            Time.timeScale = 3f;
            ActiveToolSelector.Instance.SelectTool(Tool("scraper"));
            yield return RubRows(pan, camera, 0f, 0.55f);
            Time.timeScale = 1f;
            yield return null;
            TestScreenshot.Capture(camera, "art_tools_a_scraper.png", width, height);

            Time.timeScale = 3f;
            ActiveToolSelector.Instance.SelectTool(Tool("shower"));
            yield return RubRows(pan, camera, 0.5f, 1f);
            Time.timeScale = 1f;
            for (int i = 0; i < 8; i++)
            {
                HoldAt(VisiblePointOrFail(pan, camera)); // the jets need a few frames to reach the pan
                yield return null;
            }

            TestScreenshot.Capture(camera, "art_tools_b_spray.png", width, height);
            Release(_mouse.leftButton);

            static ToolDefinition Tool(string id)
            {
                foreach (ToolDefinition tool in ActiveToolSelector.Instance.AvailableTools)
                {
                    if (tool.Id == id)
                    {
                        return tool;
                    }
                }

                Assert.Fail($"No {id} on the toolbar");
                return null;
            }
        }

        /// <summary>
        /// Not a check - Sunday's legendary lasagne tray, on the pile as it lands and then in the sink before it is
        /// scraped: Logs/Screenshots/art_special_tray_*.png. Explicit.
        /// </summary>
        [UnityTest, Explicit]
        public IEnumerator ArtPreview_LegendaryTray()
        {
            AccessibilitySettings.RelaxedMode = true;
            ShiftPlan.Special = SpecialShift.LasagneTray;
            yield return LoadKitchen();
            Camera camera = Camera.main;
            var station = Object.FindAnyObjectByType<WashStation>();

            DishController tray = null;
            Time.timeScale = 8f;
            yield return WaitUntilOrFail(() => (tray = FindWashableDishWithId(camera, "lasagne_tray")) != null, 30f, "The tray never came");
            Time.timeScale = 1f;
            yield return new WaitForSeconds(1f);
            TestScreenshot.Capture(camera, "art_special_tray_a_pile.png", 1080, 2340);

            yield return Tap(VisiblePointOrFail(tray, camera));
            Assert.AreSame(tray, station.Occupant, "The tray did not go to the sink");
            yield return WaitUntilOrFail(() => !tray.IsMoving, 3f, "The tray never arrived in the sink");
            yield return null;
            TestScreenshot.Capture(camera, "art_special_tray_b_sink.png", 1080, 2340);
        }

        /// <summary>
        /// Not a check - the arm mid-scrub on a plate as the shift wears on it (Zmywak/Hand): fresh at the start of a
        /// rested first shift, the end of a hard one in the autumn of 2008, and everything at full. Explicit; the
        /// images are Logs/Screenshots/art_hands_*.png.
        /// </summary>
        [UnityTest, Explicit]
        public IEnumerator ArtPreview_HandsFreshAndWorn()
        {
            AccessibilitySettings.RelaxedMode = true;
            yield return LoadKitchen();
            Camera camera = Camera.main;
            var wear = Object.FindAnyObjectByType<HandWear>();
            Assert.IsNotNull(wear, "The hand has no HandWear");

            DishController plate = null;
            Time.timeScale = 8f;
            yield return WaitUntilOrFail(() => (plate = FindWashableDishWithId(camera, "plate")) != null, 20f, "No plate to photograph");
            Time.timeScale = 1f;
            yield return ClaimScrapeAndRinse(plate, camera);
            foreach (ToolDefinition tool in ActiveToolSelector.Instance.AvailableTools)
            {
                if (tool.Id == "sponge")
                {
                    ActiveToolSelector.Instance.SelectTool(tool); // the hand flat on the plate, the back of the glove up
                }
            }

            var looks = new[]
            {
                ("a_fresh", HandWearModel.Evaluate(100f, 0f, 0f, HandWearModel.FirstShift)),
                ("b_hard_shift_2008", HandWearModel.Evaluate(35f, 0.9f, 1f, new System.DateTime(2008, 10, 1))),
                ("c_everything", new HandLook { Tiredness = 1f, Heat = 1f, Soak = 1f, Weathering = 1f })
            };
            foreach ((string name, HandLook look) in looks)
            {
                wear.Show(look);
                Vector2 centre = VisiblePointOrFail(plate, camera);
                for (int i = 0; i < 24; i++)
                {
                    // Small circles, as a hand works at a spot.
                    HoldAt(centre + new Vector2(Mathf.Cos(i * 0.6f), Mathf.Sin(i * 0.6f)) * 14f);
                    yield return null;
                }

                TestScreenshot.Capture(camera, $"art_hands_{name}.png", 1080, 2340);
            }

            wear.Release();
            Release(_mouse.leftButton);
        }

        // ------------------------------------------------------------------
        // Helpers
        // ------------------------------------------------------------------

        private static IEnumerator LoadKitchen()
        {
            SceneManager.LoadScene(SceneName, LoadSceneMode.Single);
            yield return null;
            yield return null;
        }

        /// <summary>
        /// Full wash of one dish through real (simulated) pointer input: tap-swipe in the pile to claim it
        /// and scrape it, wait for it to land in the sink, hold to rinse, rub to scrub - cycling through every
        /// tool, so it works for any dish/dirt combination - then drag it onto a free rack slot.
        /// <paramref name="onStage"/> is called as each stage is reached (for assertions/screenshots).
        /// </summary>
        private IEnumerator WashAndRack(DishController dish, Camera camera, RackGrid rackGrid, System.Action<WashStage> onStage = null, float scrubTimeScale = 3f,
            System.Func<IEnumerator> onInspection = null)
        {
            yield return ClaimScrapeAndRinse(dish, camera, onStage);
            yield return ScrubAndRack(dish, camera, rackGrid, onStage, scrubTimeScale, onInspection);
        }

        /// <summary>Tap-swipe a pile dish to claim and scrape it, wait for it in the sink, hold until it is rinsed (finger left down).</summary>
        private IEnumerator ClaimScrapeAndRinse(DishController dish, Camera camera, System.Action<WashStage> onStage = null)
        {
            // A dish landing on the pile can cover the one just picked for a moment (a pot buries a glass): give it
            // a few seconds to settle before calling it covered.
            var settle = new PlayDeadline(5f);
            while (!FindVisiblePoint(dish, camera).HasValue && !settle.Passed)
            {
                yield return null;
            }

            Vector2 point = VisiblePointOrFail(dish, camera);
            Vector3 positionBeforeTap = dish.transform.position;
            UnityEngine.InputSystem.LowLevel.InputMetrics metricsBeforeTap = InputSystem.metrics;
            int frameBeforeTap = Time.frameCount;
            Set(_mouse.position, point);
            yield return null;
            // A finger rests on the dish for a few frames; the dish should be picked up within them.
            for (int attempt = 0; attempt < 5 && !dish.IsClaimed; attempt++)
            {
                HoldAt(point);
                yield return null;
            }

            if (!dish.IsClaimed)
            {
                var station = Object.FindAnyObjectByType<WashStation>();
                Physics.Raycast(camera.ScreenPointToRay(point), out RaycastHit hit, 100f);
                Assert.Fail($"Tapping {dish.name} did not claim it. Sink holds: " +
                            $"{(station != null && station.Occupant != null ? station.Occupant.name : "nothing")}; " +
                            $"ray now hits: {(hit.collider != null ? hit.collider.name : "nothing")}; " +
                            $"dish moved {Vector3.Distance(positionBeforeTap, dish.transform.position):0.000} m; " +
                            $"tap at {point}, pointer reads {_mouse.position.ReadValue()}, " +
                            $"pressed {_mouse.leftButton.isPressed}, mouse enabled {_mouse.enabled}, " +
                            $"is Pointer.current {UnityEngine.InputSystem.Pointer.current == _mouse}, Editor focused {Application.isFocused}; " +
                            $"input updates {metricsBeforeTap.totalUpdateCount}->{InputSystem.metrics.totalUpdateCount}, " +
                            $"events {metricsBeforeTap.totalEventCount}->{InputSystem.metrics.totalEventCount} over {Time.frameCount - frameBeforeTap} frames, " +
                            $"mouse last updated {_mouse.lastUpdateTime:F2} (input time {UnityEngine.InputSystem.LowLevel.InputState.currentTime:F2}); " +
                            $"test runtime: {DescribeTestRuntime()}, " +
                            $"settings: background {InputSystem.settings.backgroundBehavior}, editor {InputSystem.settings.editorInputBehaviorInPlayMode}, " +
                            $"update mode {InputSystem.settings.updateMode}");
            }
            Set(_mouse.position, point + new Vector2(120f, 0f));
            yield return null;
            Release(_mouse.leftButton);
            yield return null;
            Assert.AreEqual(WashStage.AwaitingRinse, dish.CurrentStage, $"Swipe did not scrape {dish.name}");

            yield return WaitUntilOrFail(() => !dish.IsMoving, 3f, $"{dish.name} never arrived in the sink");
            onStage?.Invoke(WashStage.AwaitingRinse);

            point = VisiblePointOrFail(dish, camera);
            Set(_mouse.position, point);
            yield return null;
            Press(_mouse.leftButton);
            var rinseDeadline = new PlayDeadline(5f);
            while (dish.CurrentStage == WashStage.AwaitingRinse && !rinseDeadline.Passed)
            {
                HoldAt(point);
                yield return null;
            }

            if (dish.CurrentStage == WashStage.AwaitingRinse)
            {
                Physics.Raycast(camera.ScreenPointToRay(point), out RaycastHit rinseHit, 100f);
                Assert.Fail($"{dish.name} did not rinse: hold at {point} hits {(rinseHit.collider != null ? rinseHit.collider.name : "nothing")}, " +
                            $"pointer reads {_mouse.position.ReadValue()} pressed {_mouse.leftButton.isPressed}, dish moving {dish.IsMoving}, " +
                            $"timeScale {Time.timeScale}");
            }
        }

        /// <summary>Rubs rows of the dish's on-screen footprint, from <paramref name="fromShare"/> to <paramref name="toShare"/> of
        /// its height (top first), with the finger left down on the last point.</summary>
        private IEnumerator RubRows(DishController dish, Camera camera, float fromShare, float toShare)
        {
            Rect footprint = ScreenFootprint(dish, camera);
            float rowStep = Mathf.Max(2f, Mathf.Min(footprint.height / 12f, footprint.width / 12f));
            float columnStep = Mathf.Max(2f, footprint.width / 10f);
            bool leftToRight = true;
            for (float y = footprint.yMax - footprint.height * fromShare; y >= footprint.yMax - footprint.height * toShare; y -= rowStep)
            {
                for (float t = 0f; t <= footprint.width && dish.CurrentStage == WashStage.AwaitingScrub; t += columnStep)
                {
                    HoldAt(new Vector2(leftToRight ? footprint.xMin + t : footprint.xMax - t, y));
                    yield return null;
                }

                leftToRight = !leftToRight;
            }
        }

        private IEnumerator ScrubAndRack(DishController dish, Camera camera, RackGrid rackGrid, System.Action<WashStage> onStage, float scrubTimeScale,
            System.Func<IEnumerator> onInspection)
        {
            yield return ScrubClean(dish, camera, onStage, scrubTimeScale, onInspection);
            // Washing and carrying are separate gestures: finishing the last rub leaves it in the sink.
            Release(_mouse.leftButton);
            yield return null;
            HoldAt(VisiblePointOrFail(dish, camera));
            yield return null;

            RackPlacement? placement = rackGrid.FindFreePlacement(dish.Definition);
            Assert.IsTrue(placement.HasValue, $"No room on the rack for {dish.name}");
            Vector3 target = rackGrid.BodyWorldPosition(dish.Definition, placement.Value);
            Vector2 targetPoint = camera.WorldToScreenPoint(target);
            var dragDeadline = new PlayDeadline(2f);
            while (!dragDeadline.Passed && Vector3.Distance(dish.transform.position, target) >= 0.1f)
            {
                HoldAt(targetPoint);
                yield return null;
            }

            Release(_mouse.leftButton);
            yield return WaitUntilOrFail(() => dish.CurrentStage == WashStage.Racked, 2f, $"{dish.name} was not racked");
            onStage?.Invoke(WashStage.Racked);
        }

        /// <summary>Rubs the dish in the sink clean with the right tools; returns with it ready for the rack and the finger still down on it.</summary>
        private IEnumerator ScrubClean(DishController dish, Camera camera, System.Action<WashStage> onStage, float scrubTimeScale,
            System.Func<IEnumerator> onInspection)
        {
            // Dirt only comes off under the finger, so rub the whole dish: sweep its on-screen footprint
            // row by row (boustrophedon), with the tool that works on whatever dirt is left. Run the clock
            // 3x meanwhile - only coverage matters here, not wall-clock time.
            // Steps relative to the dish's size on screen: the batch-mode screen is small (the dish can be
            // under 100 px), and rows must stay well under a brush width apart or stripes stay dirty.
            // Rows are also bounded by the width: a tall glass's footprint is mostly its side, and rows a
            // twelfth of that apart skipped a strip along the far rim of its (much smaller) top.
            Rect footprint = ScreenFootprint(dish, camera);
            float rowStep = Mathf.Max(2f, Mathf.Min(footprint.height / 12f, footprint.width / 12f));
            float columnStep = Mathf.Max(2f, footprint.width / 10f);
            int frame = 0;
            bool reportedScrub = false;
            bool reportedInspection = false;
            bool leftToRight = true;
            var deadline = new PlayDeadline(30f);
            Time.timeScale = scrubTimeScale;
            while (dish.CurrentStage == WashStage.AwaitingScrub && !deadline.Passed)
            {
                for (float y = footprint.yMax; y >= footprint.yMin && dish.CurrentStage == WashStage.AwaitingScrub; y -= rowStep)
                {
                    for (float t = 0f; t <= footprint.width && dish.CurrentStage == WashStage.AwaitingScrub; t += columnStep)
                    {
                        SelectToolForRemainingDirt(dish);
                        float x = leftToRight ? footprint.xMin + t : footprint.xMax - t;
                        HoldAt(new Vector2(x, y));
                        frame++;
                        yield return null;

                        if (!reportedScrub && frame == 60)
                        {
                            reportedScrub = true;
                            onStage?.Invoke(WashStage.AwaitingScrub);
                        }

                        if (!reportedInspection && onInspection != null && dish.IsInspecting)
                        {
                            // The glass went up to the light: let the caller look before rubbing on.
                            reportedInspection = true;
                            Release(_mouse.leftButton);
                            Time.timeScale = 1f;
                            yield return onInspection();
                            // The finger goes back down on the glass itself, as a player's would: a press that
                            // starts beside a glass held to the light steers the reflection instead of scrubbing,
                            // and the sweep's next point (a corner of the footprint) can well be beside it.
                            HoldAt(VisiblePointOrFail(dish, camera));
                            yield return null;
                            Time.timeScale = scrubTimeScale;
                        }
                    }

                    leftToRight = !leftToRight;
                }
            }

            Time.timeScale = 1f;
            Assert.AreEqual(WashStage.ReadyForRack, dish.CurrentStage,
                $"Scrubbing did not finish {dish.name} (dirt left: {DescribeDirt(dish)})");
            onStage?.Invoke(WashStage.ReadyForRack);
        }

        private static void SelectToolForRemainingDirt(DishController dish)
        {
            foreach (DirtLayerState layer in dish.DirtLayers)
            {
                if (layer.Remaining <= 0f || layer.Type == null)
                {
                    continue;
                }

                foreach (ToolDefinition tool in ActiveToolSelector.Instance.AvailableTools)
                {
                    if (tool.IsEffectiveAgainst(layer.Type) && tool.ResidueFloor <= 0f)
                    {
                        if (ActiveToolSelector.Instance.CurrentTool != tool)
                        {
                            ActiveToolSelector.Instance.SelectTool(tool);
                        }

                        return;
                    }
                }
            }
        }

        private static string DescribeDishes(Camera camera, DishDefinition ofType)
        {
            var parts = new List<string>();
            foreach (DishController dish in Object.FindObjectsByType<DishController>())
            {
                if (dish.Definition != ofType)
                {
                    continue;
                }

                var body = dish.GetComponent<Rigidbody>();
                parts.Add($"{dish.name} at {dish.transform.position} stage {dish.CurrentStage} claimed {dish.IsClaimed} " +
                          $"settled {dish.IsSettledInPile} v {body.linearVelocity.magnitude:0.000} w {body.angularVelocity.magnitude:0.000} " +
                          $"visible {FindVisiblePoint(dish, camera).HasValue}");
            }

            return parts.Count > 0 ? string.Join(" | ", parts) : "none exist";
        }

        private static string DescribeDirt(DishController dish)
        {
            var parts = new List<string>();
            for (int layer = 0; layer < dish.DirtLayers.Count; layer++)
            {
                DirtLayerState state = dish.DirtLayers[layer];
                var dirtySpots = new List<string>();
                if (state.Remaining > 0f)
                {
                    // Where the leftover dirt sits, on a coarse 10x10 grid of the dish's UV disc.
                    for (int y = 0; y < 10; y++)
                    {
                        for (int x = 0; x < 10; x++)
                        {
                            var uv = new Vector2((x + 0.5f) / 10f, (y + 0.5f) / 10f);
                            if (dish.DirtAmountAt(layer, uv) > 0.05f)
                            {
                                dirtySpots.Add($"({uv.x:0.00},{uv.y:0.00})");
                            }
                        }
                    }
                }

                parts.Add($"{(state.Type != null ? state.Type.Id : "?")}={state.Remaining:0.000} at [{string.Join(" ", dirtySpots)}]");
            }

            return string.Join(", ", parts);
        }

        /// <summary>Screen-space rectangle covering the dish (its renderer bounds projected).</summary>
        private static Rect ScreenFootprint(DishController dish, Camera camera)
        {
            Bounds b = dish.GetComponent<Renderer>().bounds;
            Vector2 min = new Vector2(float.MaxValue, float.MaxValue);
            Vector2 max = new Vector2(float.MinValue, float.MinValue);
            for (int i = 0; i < 8; i++)
            {
                var corner = new Vector3(
                    (i & 1) == 0 ? b.min.x : b.max.x,
                    (i & 2) == 0 ? b.min.y : b.max.y,
                    (i & 4) == 0 ? b.min.z : b.max.z);
                Vector2 p = camera.WorldToScreenPoint(corner);
                min = Vector2.Min(min, p);
                max = Vector2.Max(max, p);
            }

            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }

        /// <summary>
        /// Keeps the simulated finger down at <paramref name="point"/>. Re-asserts the button every frame:
        /// in a headless Editor the test mouse occasionally loses its queued state (reads back its old
        /// position, button up), which would otherwise end a hold for good. If the game saw such a blip it
        /// simply gets a release and a new press on the same dish in the sink - same code path as a player
        /// lifting a finger for a moment.
        /// </summary>
        private void HoldAt(Vector2 point)
        {
            Set(_mouse.position, point);
            if (!_mouse.leftButton.isPressed)
            {
                Press(_mouse.leftButton);
            }
        }

        /// <summary>
        /// Fixes whatever is on the repair card with the simulated finger, the way a player would: flick
        /// each scrap outwards off the filter, circle round the spray arm's hub, or hold RESET in.
        /// </summary>
        private IEnumerator PerformRepair(DishwasherRepairPanel panel)
        {
            switch (panel.Repair)
            {
                case CloggedFilterRepair filter:
                    // A few passes: where scraps overlap, pressing one may pick up the one lying on top of it.
                    for (int pass = 0; pass < 3 && !filter.IsDone; pass++)
                    {
                        for (int i = 0; i < filter.ScrapCount; i++)
                        {
                            if (filter.IsCleared(i))
                            {
                                continue;
                            }

                            Vector2 start = filter.ScrapPosition(i);
                            Vector2 away = start.sqrMagnitude > 1e-4f ? start.normalized : Vector2.right;
                            for (int step = 0; step <= 8; step++)
                            {
                                HoldAt(panel.PlayAreaToScreen(start + away * (0.06f * step)));
                                yield return null;
                            }

                            Release(_mouse.leftButton);
                            yield return null;
                        }
                    }

                    break;

                case StuckSprayArmRepair arm:
                    for (int frame = 0; frame < 400 && !arm.IsDone; frame++)
                    {
                        float angle = frame * 25f * Mathf.Deg2Rad;
                        HoldAt(panel.PlayAreaToScreen(new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 0.6f));
                        yield return null;
                    }

                    Release(_mouse.leftButton);
                    yield return null;
                    break;

                case TrippedResetRepair reset:
                    var deadline = new PlayDeadline(6f);
                    while (!reset.IsDone && !deadline.Passed)
                    {
                        HoldAt(panel.PlayAreaToScreen(Vector2.zero));
                        yield return null;
                    }

                    Release(_mouse.leftButton);
                    yield return null;
                    break;
            }
        }

        /// <summary>Diagnostics only: what the fixture's (internal) input runtime has queued, and whether it thinks the game has focus.</summary>
        private string DescribeTestRuntime()
        {
            object testRuntime = typeof(InputTestFixture).GetProperty("runtime", BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(this);
            if (testRuntime == null)
            {
                return "unavailable";
            }

            System.Type type = testRuntime.GetType();
            return $"{type.GetProperty("eventCount")?.GetValue(testRuntime)} events queued, focused {type.GetProperty("isPlayerFocused")?.GetValue(testRuntime)}, " +
                   $"runs in background {type.GetProperty("runInBackground")?.GetValue(testRuntime)}";
        }

        /// <summary>
        /// The fixture stamps simulated input with its own clock, which starts near zero and moves a fixed step
        /// per update - in batch mode, at thousands of frames a second, far faster than real time. The Input
        /// System meanwhile drops state events stamped between the Editor's exit-edit-mode and enter-play-mode
        /// times, which come from the real clock: a window a few seconds wide some 30-40 s after launch. Taps
        /// that fell into it vanished (the mouse read (0,0), no event ever processed). Starting the fixture's
        /// clock far beyond any such time keeps every simulated event out of that window.
        /// </summary>
        private void MoveTestInputClockPastPlayModeEntry()
        {
            object testRuntime = typeof(InputTestFixture).GetProperty("runtime", BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(this);
            PropertyInfo clock = testRuntime?.GetType().GetProperty("currentTime");
            Assert.IsNotNull(clock, "InputTestFixture no longer exposes its runtime clock - revisit this workaround");
            clock.SetValue(testRuntime, 1_000_000.0);
        }

        private IEnumerator Tap(Vector2 point)
        {
            Set(_mouse.position, point);
            yield return null;
            Press(_mouse.leftButton);
            yield return null;
            yield return null;
            Release(_mouse.leftButton);
            yield return null;
        }

        /// <summary>
        /// Adds a tall column of pans to the pile. Stacked on a clear patch of counter next to the spawn
        /// point rather than on top of the existing heap: dropped onto an uneven heap they slide off each
        /// other and the column never gets tall. (The pile is every unwashed dish, wherever it lies.)
        /// </summary>
        private static void OverloadPile(ShiftController shiftController, DishPileController pileController, RackGrid rackGrid)
        {
            // China, not pans: when the pile gives way, its top is thrown off and breaks on the counter - steel would
            // only ring. Fourteen plates stack flat and steady to well above the collapse height.
            DishDefinition plate = DishFromShift(shiftController, "plate");
            Vector3 clearSpot = FindClearCounter(shiftController, avoid: null);
            float thickness = plate.PlaceholderScale.y * 2f; // the dish mesh is 2 units tall before scaling
            for (int i = 0; i < 14; i++)
            {
                Vector3 pos = clearSpot + Vector3.up * (thickness * 0.5f + 0.05f + i * (thickness + 0.05f));
                pileController.Register(DishController.Create(plate, pos, shiftController.transform, rackGrid));
            }
        }

        private static DishDefinition DishFromShift(ShiftController shiftController, string id)
        {
            ShiftDefinition shift = GetPrivateField<ShiftDefinition>(shiftController, "shiftDefinition");
            foreach (DishDefinition candidate in shift.DishSpawnQueue)
            {
                if (candidate.Id == id)
                {
                    return candidate;
                }
            }

            Assert.Fail($"The shift has no {id}");
            return null;
        }

        /// <summary>A patch of counter near the pile with nothing on it (and not the one given).</summary>
        private static Vector3 FindClearCounter(ShiftController shiftController, Vector3? avoid)
        {
            Transform spawnPoint = GetPrivateField<Transform>(shiftController, "spawnPoint");
            Vector3[] offsets =
            {
                new Vector3(0f, 0f, 1.3f), new Vector3(1.5f, 0f, 0.9f), new Vector3(2.8f, 0f, 1.0f), new Vector3(-1.2f, 0f, -1.6f)
            };

            foreach (Vector3 offset in offsets)
            {
                Vector3 candidate = spawnPoint.position + offset;
                if (avoid.HasValue && Vector3.Distance(candidate, avoid.Value) < 1.2f)
                {
                    continue;
                }

                // Counter under it (not the sink's well or past the edge), and nothing on it - the box starts just
                // above the counter top (y = 0) so the counter itself doesn't count.
                bool counter = Physics.Raycast(candidate + Vector3.up * 3f, Vector3.down, out RaycastHit hit, 5f) &&
                               Mathf.Abs(hit.point.y) < 0.05f;
                if (counter && !Physics.CheckBox(candidate + Vector3.up * 0.4f, new Vector3(0.55f, 0.34f, 0.55f)))
                {
                    return candidate;
                }
            }

            Assert.Fail("No clear patch of counter near the pile");
            return Vector3.zero;
        }

        /// <summary>An untouched, settled dish (of the given type, if any) with some part visible to a tap.</summary>
        private static DishController FindWashableDish(Camera camera, DishDefinition ofType)
        {
            DishController[] dishes = Object.FindObjectsByType<DishController>();
            // Wait for anything still dropping into the pile: a dish falling through the tap's ray at the
            // wrong moment is the one that gets grabbed (new dishes keep arriving during a shift).
            foreach (DishController other in dishes)
            {
                if (!other.IsClaimed && !other.IsSettledInPile)
                {
                    return null;
                }
            }

            foreach (DishController dish in dishes)
            {
                if (dish.IsClaimed || dish.CurrentStage != WashStage.AwaitingScrape || (ofType != null && dish.Definition != ofType))
                {
                    continue;
                }

                // A freshly spawned dish has zero velocity for a frame while hanging in the air above the
                // pile - IsSettledInPile includes a grace period for that. The stricter velocity check
                // on top makes sure it isn't still sliding: by the time the tap lands it could be elsewhere.
                var body = dish.GetComponent<Rigidbody>();
                if (!dish.IsSettledInPile ||
                    (body != null && (body.linearVelocity.sqrMagnitude > 0.0004f || body.angularVelocity.sqrMagnitude > 0.01f)))
                {
                    continue;
                }

                if (FindVisiblePoint(dish, camera).HasValue)
                {
                    return dish;
                }
            }

            return null;
        }

        private static DishController FindWashableDishWithId(Camera camera, string id)
        {
            foreach (DishController dish in Object.FindObjectsByType<DishController>())
            {
                if (dish.Definition.Id == id && !dish.IsClaimed)
                {
                    DishController washable = FindWashableDish(camera, dish.Definition);
                    if (washable != null)
                    {
                        return washable;
                    }
                }
            }

            return null;
        }

        private static Vector2 VisiblePointOrFail(DishController dish, Camera camera)
        {
            Vector2? point = FindVisiblePoint(dish, camera);
            Assert.IsTrue(point.HasValue, $"{dish.name} is completely covered by other dishes");
            return point.Value;
        }

        /// <summary>A screen point where a tap would hit this dish rather than whatever lies on top of it -
        /// the same raycast DishInputRouter does.</summary>
        private static Vector2? FindVisiblePoint(DishController dish, Camera camera)
        {
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

        private static T GetPrivateField<T>(object owner, string fieldName) where T : class
        {
            FieldInfo field = owner.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(field, $"{owner.GetType().Name}.{fieldName} field not found");
            var value = field.GetValue(owner) as T;
            Assert.IsNotNull(value, $"{owner.GetType().Name}.{fieldName} is not assigned");
            return value;
        }

        private static IEnumerator WaitUntilOrFail(System.Func<bool> condition, float timeoutSeconds, string failureMessage)
        {
            var deadline = new PlayDeadline(timeoutSeconds);
            while (!condition() && !deadline.Passed)
            {
                yield return null;
            }

            Assert.IsTrue(condition(), failureMessage);
        }
    }
}
