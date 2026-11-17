using UnityEngine;
using Zmywak.Core.Data;
using Zmywak.Core.Events.Channels;
using Zmywak.Core.Settings;
using Zmywak.Core.Telemetry;
using Zmywak.Kitchen.Dishes;
using Zmywak.Kitchen.Dishwasher;
using Zmywak.Kitchen.Feedback;
using Zmywak.Kitchen.Flow;
using Zmywak.Kitchen.Orders;
using Zmywak.Kitchen.Pile;
using Zmywak.Core.Diagnostics;

namespace Zmywak.Kitchen.Shifts
{
    /// <summary>
    /// Watches a shift being played and, when it ends, writes it to the local <see cref="GameplayLog"/>: how
    /// long each step in the sink takes, the mistakes (breakages, collapses, drops beside the rack), the
    /// machine's breakdowns and their repairs, the chef's orders, the pay, and the frame rate. Only watches -
    /// nothing in the kitchen depends on it.
    /// </summary>
    public class ShiftDiary : MonoBehaviour
    {
        // Clearly under 30 fps: a frame held to a 30 fps cap lasts 33.4 ms and is not slow.
        private const float SlowFrameSeconds = 1.1f / 30f;

        [SerializeField] private ShiftController shiftController;
        [SerializeField] private WashStation washStation;
        [SerializeField] private DishwasherController dishwasher;
        [SerializeField] private DishPileController pileController;
        [SerializeField] private ChefOrderController chefOrders;
        [SerializeField] private FlowStreakController flow;
        [SerializeField] private ShiftCompletedEventChannel shiftCompletedChannel;

        private readonly ShiftLogEntry _entry = new ShiftLogEntry();

        // The dish in the sink and the time it has spent at each step so far.
        private DishController _dishInSink;
        private float _scrape, _rinse, _scrub, _inspect, _toRack;

        private DishwasherFault _fault;
        private float _brokeDownAt = -1f;
        private bool _wasPaused;
        private int _frames;
        private int _slowFrames;
        private float _playedSeconds;

        private void Awake()
        {
            using var _startup = StartupTimeline.Measure("ShiftDiary.Awake");
            shiftController = shiftController != null ? shiftController : FindAnyObjectByType<ShiftController>();
            washStation = washStation != null ? washStation : FindAnyObjectByType<WashStation>();
            dishwasher = dishwasher != null ? dishwasher : FindAnyObjectByType<DishwasherController>();
            pileController = pileController != null ? pileController : FindAnyObjectByType<DishPileController>();
            chefOrders = chefOrders != null ? chefOrders : FindAnyObjectByType<ChefOrderController>();
            flow = flow != null ? flow : FindAnyObjectByType<FlowStreakController>();
        }

        private void OnEnable()
        {
            KitchenFeedbackEvents.HeldToLight += OnHeldToLight;
            KitchenFeedbackEvents.DropMissed += OnDropMissed;
            if (dishwasher != null)
            {
                dishwasher.OnCycleStarted += OnCycleStarted;
                dishwasher.OnBrokeDown += OnBrokeDown;
                dishwasher.OnRepaired += OnRepaired;
            }

            if (pileController != null)
            {
                pileController.OnCollapsed += OnPileCollapsed;
            }

            if (chefOrders != null)
            {
                chefOrders.OnOrderIssued += OnOrderIssued;
            }

            if (flow != null)
            {
                flow.OnStreakChanged += OnStreakChanged;
            }

            if (shiftCompletedChannel != null)
            {
                shiftCompletedChannel.OnRaised += OnShiftCompleted;
            }
        }

        private void OnDisable()
        {
            KitchenFeedbackEvents.HeldToLight -= OnHeldToLight;
            KitchenFeedbackEvents.DropMissed -= OnDropMissed;
            if (dishwasher != null)
            {
                dishwasher.OnCycleStarted -= OnCycleStarted;
                dishwasher.OnBrokeDown -= OnBrokeDown;
                dishwasher.OnRepaired -= OnRepaired;
            }

            if (pileController != null)
            {
                pileController.OnCollapsed -= OnPileCollapsed;
            }

            if (chefOrders != null)
            {
                chefOrders.OnOrderIssued -= OnOrderIssued;
            }

            if (flow != null)
            {
                flow.OnStreakChanged -= OnStreakChanged;
            }

            if (shiftCompletedChannel != null)
            {
                shiftCompletedChannel.OnRaised -= OnShiftCompleted;
            }
        }

        private void Update()
        {
            if (shiftController != null && shiftController.IsShiftOver)
            {
                return;
            }

            bool paused = Time.timeScale == 0f;
            if (paused && !_wasPaused)
            {
                _entry.pauses++;
            }

            _wasPaused = paused;
            if (paused)
            {
                return;
            }

            _frames++;
            _playedSeconds += Time.unscaledDeltaTime;
            if (Time.unscaledDeltaTime > SlowFrameSeconds)
            {
                _slowFrames++;
            }

            TimeTheSink();
        }

        private void TimeTheSink()
        {
            DishController occupant = washStation != null ? washStation.Occupant : null;
            if (occupant != _dishInSink)
            {
                FinishTiming(_dishInSink);
                _dishInSink = occupant;
                _scrape = _rinse = _scrub = _inspect = _toRack = 0f;
            }

            if (occupant == null)
            {
                return;
            }

            float dt = Time.deltaTime;
            switch (occupant.CurrentStage)
            {
                case WashStage.AwaitingScrape:
                    _scrape += dt;
                    break;
                case WashStage.AwaitingRinse:
                    _rinse += dt;
                    break;
                case WashStage.AwaitingScrub:
                    if (occupant.IsInspecting)
                    {
                        _inspect += dt;
                    }
                    else
                    {
                        _scrub += dt;
                    }

                    break;
                case WashStage.ReadyForRack:
                    _toRack += dt;
                    break;
            }
        }

        /// <summary>A dish that left the sink for the rack counts towards the averages (one that broke does not).</summary>
        private void FinishTiming(DishController dish)
        {
            if (dish == null || (dish.CurrentStage != WashStage.Racked && dish.CurrentStage != WashStage.Done))
            {
                return;
            }

            _entry.dishesTimed++;
            _entry.scrapeSeconds += _scrape;
            _entry.rinseSeconds += _rinse;
            _entry.scrubSeconds += _scrub;
            _entry.inspectSeconds += _inspect;
            _entry.toRackSeconds += _toRack;
        }

        private void OnHeldToLight(DishController dish) => _entry.glassesHeldToLight++;
        private void OnDropMissed(DishController dish) => _entry.rackDropsMissed++;
        private void OnCycleStarted(System.Collections.Generic.IReadOnlyList<DishDefinition> load, float fill) => _entry.dishwasherCycles++;
        private void OnPileCollapsed(int shoved) => _entry.pileCollapses++;
        private void OnOrderIssued(DishDefinition dish) => _entry.chefOrders++;
        private void OnStreakChanged(int streak) => _entry.bestFlowStreak = Mathf.Max(_entry.bestFlowStreak, streak);

        private void OnBrokeDown(DishwasherFault fault)
        {
            _entry.breakdowns++;
            _fault = fault;
            _brokeDownAt = Time.time;
        }

        private void OnRepaired()
        {
            if (_brokeDownAt >= 0f)
            {
                _entry.repairs.Add(new RepairLogEntry { fault = _fault.ToString(), seconds = Time.time - _brokeDownAt });
                _brokeDownAt = -1f;
            }
        }

        private void OnShiftCompleted(ShiftResult result)
        {
            FinishTiming(_dishInSink);
            _dishInSink = null;

            ShiftLogEntry e = _entry;
            e.shiftId = shiftController != null && shiftController.Definition != null ? shiftController.Definition.Id : string.Empty;
            e.relaxedMode = AccessibilitySettings.RelaxedMode;
            e.plannedSeconds = result.PlannedSeconds;
            e.elapsedSeconds = result.ElapsedSeconds;
            e.clockedOutEarly = result.PlannedSeconds > 0f && result.ElapsedSeconds < result.PlannedSeconds - 0.5f;
            e.dishesArrived = shiftController != null ? shiftController.DishesSpawnedTotal : 0;
            e.dishesCleaned = result.DishesCleaned;
            e.dishesMissed = result.DishesMissed;
            e.dishesBroken = result.DishesBroken;
            e.fullRackLoads = result.FullRackLoads;
            e.bigDishwasherLoaded = shiftController != null ? shiftController.BigDishwasherLoaded : 0;
            e.bigDishwasherUnloaded = shiftController != null ? shiftController.BigDishwasherUnloaded : 0;
            e.chefOrdersMet = chefOrders != null ? chefOrders.OrdersFulfilled : 0;
            e.wageEuros = result.Wage;
            e.tipsEuros = result.Tips;
            e.deductionsEuros = result.Deductions;
            e.quota = result.Quota;
            e.score = result.Score.Total;
            e.grade = ShiftScoring.Letter(result.Grade);
            e.quotaBonusEuros = result.QuotaBonus;
            e.gradeBonusEuros = result.GradeBonus;
            e.urgentOrders = result.UrgentOrders;
            e.urgentOrdersMet = result.UrgentOrdersMet;
            e.washAccuracy = result.WashAccuracy;
            if (e.dishesTimed > 0)
            {
                e.scrapeSeconds /= e.dishesTimed;
                e.rinseSeconds /= e.dishesTimed;
                e.scrubSeconds /= e.dishesTimed;
                e.inspectSeconds /= e.dishesTimed;
                e.toRackSeconds /= e.dishesTimed;
            }

            e.averageFps = _playedSeconds > 0f ? _frames / _playedSeconds : 0f;
            e.slowFrameShare = _frames > 0 ? (float)_slowFrames / _frames : 0f;

            GameplayLog.Append(e);
        }
    }
}
