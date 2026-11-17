using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Zmywak.Core.Data;
using Zmywak.Core.Events.Channels;
using Zmywak.Core.Graphics;
using Zmywak.Core.Save;
using Zmywak.Core.Services;
using Zmywak.Core.Settings;
using Zmywak.Kitchen.Dishes;
using Zmywak.Kitchen.Dishwasher;
using Zmywak.Kitchen.Feedback;
using Zmywak.Kitchen.Flow;
using Zmywak.Kitchen.Orders;
using Zmywak.Kitchen.Pile;
using Zmywak.Kitchen.Rack;
using Zmywak.Kitchen.Tools;
using Zmywak.Core.Diagnostics;

namespace Zmywak.Kitchen.Shifts
{
    /// <summary>
    /// Orchestrates one shift: the day's kind of shift (from the evening's schedule, <see cref="ShiftPlan"/>, else the kitchen's
    /// own default), the dishes dealt from its mix, the pace of its waves of service, the dishwasher's breakdowns and the
    /// chef's orders; the timer; and at the end the score and the pay.
    /// </summary>
    public class ShiftController : MonoBehaviour
    {
        private const float OpeningStaggerSeconds = 0.6f;

        [Tooltip("The kitchen's own shift: played when no schedule comes from the evening (the scene on its own, the tests).")]
        [SerializeField] private ShiftDefinition shiftDefinition;
        [SerializeField] private Transform spawnPoint;
        [SerializeField] private RackGrid rackGrid;
        [SerializeField] private WashStation washStation;
        [SerializeField] private DishwasherController dishwasher;
        [Tooltip("The big, half-hour machine: what comes out of it counts as washed in the shift it is unloaded in.")]
        [SerializeField] private BigDishwasherController bigDishwasher;
        [SerializeField] private DishPileController pileController;
        [SerializeField] private FlowStreakController flowController;
        [Tooltip("The chef's orders (found in the scene when not set).")]
        [SerializeField] private ChefOrderController chefOrders;
        [SerializeField] private ShiftCompletedEventChannel shiftCompletedChannel;
        [Tooltip("Sunday's legendary lasagne tray (SpecialShift.LasagneTray): it comes in a quarter of the way through.")]
        [SerializeField] private DishDefinition legendaryTray;

        /// <summary>How far through a Sunday shift the lasagne tray comes in.</summary>
        public const float TrayArrivesAt = 0.25f;

        private ShuffleBag<DishDefinition> _bag;
        private float _storyPace = 1f;

        private readonly List<DishController> _spawnedDishes = new List<DishController>();
        private ShiftTimer _timer;
        private int _dishesCleanedTotal;
        private int _dishesBrokenTotal;
        private bool _shiftEnded;
        private int _fullRackLoads;

        // How the work was done, for the score.
        private float _rightToolSeconds;
        private float _wrongToolSeconds;
        private int _rackDropsMissed;
        private int _pileCollapses;

        public ShiftDefinition Definition => shiftDefinition;

        /// <summary>The story day the shift is worked on (<see cref="MechanicUnlocks.Everything"/> for the kitchen on its own: all of
        /// it there from the start).</summary>
        public int Day { get; private set; } = MechanicUnlocks.Everything;

        /// <summary>Whether a part of the game is in the kitchen yet, on this shift's day.</summary>
        public bool IsUnlocked(Mechanic mechanic) => MechanicUnlocks.IsUnlocked(mechanic, Day);

        /// <summary>The first shift opens on the guided first plate (<see cref="TutorialProgress.FirstDish"/>, once per life).</summary>
        public bool TutorialActive { get; private set; }

        /// <summary>While the guided first plate is being taught, the kitchen holds still: no clock, no more dishes, no chef.</summary>
        public bool TutorialHold { get; private set; }

        /// <summary>The guided plate is done: the clock starts, the dishes come, the chef calls (when his day has come).</summary>
        public void EndTutorialHold()
        {
            if (!TutorialHold)
            {
                return;
            }

            TutorialHold = false;
            TutorialProgress.Mark(TutorialProgress.FirstDish);
            RefreshChef();
        }

        /// <summary>A friend from the crew helps on a hard night once Kuba is one of them (this Integration).</summary>
        public const float CrewHelpIntegration = 50f;

        /// <summary>The pile this close to giving way is when the friend steps in (once a shift).</summary>
        public const float CrewHelpAt = 0.72f;

        public const int CrewHelpDishes = 2;

        /// <summary>A friend can lend a hand this shift (close enough to the crew, the evening's schedule).</summary>
        public bool CrewHelpAvailable { get; private set; }

        /// <summary>Dishes a friend from the crew took off the pile (counted as washed).</summary>
        public int CrewHelped { get; private set; }

        /// <summary>The friend who stepped in, and how many dishes he took: the HUD says so.</summary>
        public event Action<string, int> OnCrewHelped;

        /// <summary>Who stands at the next sink: by where Kuba works, and when.</summary>
        public static string CrewMate(Place place, int day) =>
            place == Place.Krakow ? "Ołeh" : place == Place.Pilbara ? "Liam" : day < 9 ? "Siobhán" : "Tunde";

        private Place _crewPlace;

        /// <summary>The full rack's automatic start: seconds left, or below zero when none is counting down.</summary>
        public float AutoStartIn { get; private set; } = -1f;

        /// <summary>How long a full rack waits before the machine starts by itself.</summary>
        public const float AutoStartSeconds = 3f;

        private int _autoStartCancelledAt = -1;
        private int _rackCellsSeen = -1;

        /// <summary>The player stopped the countdown (to pack more in): it waits until the rack changes again.</summary>
        public void CancelAutoStart()
        {
            if (AutoStartIn >= 0f && rackGrid != null)
            {
                _autoStartCancelledAt = rackGrid.OccupiedCells;
                AutoStartIn = -1f;
            }
        }

        /// <summary>The shift came from the evening's schedule (a kind of shift for the day), not the kitchen's own default.</summary>
        public bool IsScheduled { get; private set; }

        /// <summary>The kind of shift's name and what Kuba is told about it.</summary>
        public string Title => shiftDefinition != null ? shiftDefinition.DisplayName : string.Empty;
        public string Intro => shiftDefinition != null ? shiftDefinition.Intro : string.Empty;

        /// <summary>The clean dishes the shift asks for (the kind's quota grown with the pace of the day and of a busy night).</summary>
        public int Quota { get; private set; }

        /// <summary>Where the service is: between rushes, filling up, a rush, a breather (<see cref="ServiceWaves"/>).</summary>
        public ServicePhase Phase { get; private set; }

        /// <summary>The rush's own call as it peaks, when it has one.</summary>
        public string PhaseCallout { get; private set; }

        /// <summary>The service moved on to another phase (the HUD calls it out).</summary>
        public event Action<ServicePhase, string> OnServicePhaseChanged;

        /// <summary>How busy the room is right now: 1 between rushes, more inside one (1 before the clock starts).</summary>
        public float ServicePace => _timer == null || AwaitingPrep || _shiftEnded ? 1f : ServiceWaves.PaceAt(shiftDefinition.Waves, _timer.Progress01);

        /// <summary>The share of the scrubbing so far done with a tool that works on the dirt (1 with no scrubbing yet).</summary>
        public float WashAccuracy
        {
            get
            {
                float total = _rightToolSeconds + _wrongToolSeconds;
                return total > 0f ? _rightToolSeconds / total : 1f;
            }
        }

        public int RackDropsMissed => _rackDropsMissed;

        /// <summary>Kuba's energy as the shift began (100 without the life around it); tired, he scrubs slower.</summary>
        public float EnergyAtStart { get; private set; } = 100f;

        private float _hourlyWage = ShiftResultCalculator.HourlyWageEuros; // the wage of the day the shift began, where Kuba is
        private float _prepBonus;

        /// <summary>Where the shift is worked (Dublin without the life around it): what it pays, and in what money.</summary>
        public Place Place { get; private set; } = Place.Dublin;

        /// <summary>Kuba's rank at the sink (Career) as the shift began: each one adds to the hourly wage.</summary>
        public int Rank { get; private set; }

        /// <summary>A special shift (ShiftPlan, from the calendar): a wedding, an inspection, the lasagne tray, St. Patrick's.</summary>
        public SpecialShift Special { get; private set; }

        public SpecialShiftRules SpecialRules => SpecialShifts.Rules(Special);

        /// <summary>The dishes the closing load washed as the shift ended (racked, or still in the machine).</summary>
        public int ClosingLoad { get; private set; }

        /// <summary>The pile has come down this shift.</summary>
        public bool PileCollapsed { get; private set; }

        /// <summary>An inspection shift gone wrong: something broke, or the pile came down.</summary>
        public bool InspectionFailed => Special == SpecialShift.Inspection && !SpecialShifts.Passed(_dishesBrokenTotal, PileCollapsed);

        /// <summary>The lasagne tray once it has come in (null before, and after it leaves the kitchen).</summary>
        public DishController LegendaryTray { get; private set; }

        public bool TrayArrived { get; private set; }

        /// <summary>The tray has been scrubbed clean by hand (it reached the rack stage at some point).</summary>
        public bool TrayScrubbed { get; private set; }

        /// <summary>The shift opens at the board (ShiftPlan): the clock and the dishes wait until the prep is done.</summary>
        public bool AwaitingPrep { get; private set; }

        /// <summary>What the prep earned, paid with the shift.</summary>
        public float PrepBonus => _prepBonus;
        public float RemainingSeconds => _timer?.Remaining ?? 0f;

        /// <summary>How far through the shift's time it is, 0-1 (0 at the prep board, before the clock starts).</summary>
        public float Progress01 => _timer?.Progress01 ?? 0f;
        public int DishesCleanedTotal => _dishesCleanedTotal;
        public int DishesBrokenTotal => _dishesBrokenTotal;
        public int DishesSpawnedTotal => _spawnedDishes.Count;
        public int FullRackLoads => _fullRackLoads;

        /// <summary>Dishes put into the big dishwasher this shift, and clean ones taken out of it (counted as washed).</summary>
        public int BigDishwasherLoaded { get; private set; }
        public int BigDishwasherUnloaded { get; private set; }
        public RackGrid Rack => rackGrid;
        public bool IsShiftOver => _shiftEnded;

        /// <summary>A full rack just went into the machine (Flow has already been extended).</summary>
        public event System.Action OnFullRackLoaded;

        /// <summary>Something is racked and the machine is free - what the HUD's start button reflects.</summary>
        public bool CanStartDishwasher => dishwasher != null && !dishwasher.IsRunning && !dishwasher.IsBroken && rackGrid != null &&
                                          rackGrid.DishCount > 0;
        public bool IsDishwasherRunning => dishwasher != null && dishwasher.IsRunning;
        public bool IsDishwasherBroken => dishwasher != null && dishwasher.IsBroken;

        private void Awake()
        {
            if (chefOrders == null)
            {
                chefOrders = FindAnyObjectByType<ChefOrderController>();
            }

            if (dishwasher != null)
            {
                dishwasher.OnCycleCompleted += OnDishwasherCycleCompleted;
                dishwasher.OnCycleStarted += OnDishwasherCycleStarted;
            }

            if (pileController != null)
            {
                pileController.OnDishesBroken += OnDishesBroken;
                pileController.OnCollapsed += OnPileCollapsed;
            }

            if (bigDishwasher != null)
            {
                bigDishwasher.OnLoaded += OnBigDishwasherLoaded;
                bigDishwasher.OnUnloaded += OnBigDishwasherUnloaded;
            }

            KitchenFeedbackEvents.Scrubbing += OnScrubbing;
            KitchenFeedbackEvents.DropMissed += OnDropMissed;
        }

        private void OnDestroy()
        {
            DishController.ScrubStrength = 1f; // the next scene starts rested unless told otherwise
            KitchenFeedbackEvents.Scrubbing -= OnScrubbing;
            KitchenFeedbackEvents.DropMissed -= OnDropMissed;
            if (dishwasher != null)
            {
                dishwasher.OnCycleCompleted -= OnDishwasherCycleCompleted;
                dishwasher.OnCycleStarted -= OnDishwasherCycleStarted;
            }

            if (bigDishwasher != null)
            {
                bigDishwasher.OnLoaded -= OnBigDishwasherLoaded;
                bigDishwasher.OnUnloaded -= OnBigDishwasherUnloaded;
            }

            if (pileController != null)
            {
                pileController.OnDishesBroken -= OnDishesBroken;
                pileController.OnCollapsed -= OnPileCollapsed;
            }
        }

        private void Start()
        {
            using var _start = StartupTimeline.Measure("ShiftController.Start");
            // The evening's plan for the day: a kind of shift from the catalog, else the kitchen's own.
            ScheduledShift? schedule = ShiftPlan.TakeSchedule();
            ShiftDefinition planned = schedule.HasValue ? ShiftCatalog.Load()?.Find(schedule.Value.ArchetypeId) : null;
            if (planned != null)
            {
                shiftDefinition = planned;
                IsScheduled = true;
            }
            else if (schedule.HasValue)
            {
                Debug.LogWarning($"ShiftController: no shift '{schedule.Value.ArchetypeId}' in the catalog - playing the kitchen's own.");
            }

            if (shiftDefinition == null)
            {
                Debug.LogError("ShiftController has no ShiftDefinition assigned.");
                enabled = false;
                return;
            }

            float duration = shiftDefinition.DurationSeconds;
            if (AccessibilitySettings.RelaxedMode)
            {
                duration *= 1.5f;
            }

            _timer = new ShiftTimer(duration);

            // How the evening before shows at the sink (no life around it - a test, the scene on its own - means rested).
            bool living = ServiceLocator.TryGet(out IPlayerCondition condition);
            EnergyAtStart = living ? condition.Energy : 100f;
            Place = living ? condition.Place : Place.Dublin;
            _hourlyWage = living ? ShiftResultCalculator.HourlyWageOn(condition.Today, Place) : ShiftResultCalculator.HourlyWageEuros;
            // A promoted porter earns a little more an hour (Career: the rank's bonus, in the place's small sums).
            Rank = living ? condition.Rank : 0;
            _hourlyWage += Career.HourlyBonusEuros(Rank) * PlaceMoney.SmallSums(Place);
            DishController.ScrubStrength = Fatigue.ScrubStrength(EnergyAtStart);

            // The day the shift is on: what the kitchen has by then, and whether it opens on the guided first plate.
            Day = schedule.HasValue && IsScheduled ? schedule.Value.DayNumber : MechanicUnlocks.Everything;
            TutorialActive = IsScheduled && Day == MechanicUnlocks.DayOf(Mechanic.GuidedFirstDish) && !TutorialProgress.Has(TutorialProgress.FirstDish);
            TutorialHold = TutorialActive;
            ActiveToolSelector selector = ActiveToolSelector.Instance;
            selector?.SetUnlocked(tool => tool != null && MechanicUnlocks.IsToolUnlocked(tool.Id, Day));
            CrewHelpAvailable = living && IsScheduled && condition.Integration >= CrewHelpIntegration;
            _crewPlace = Place;

            // A wedding or St. Patrick's brings its own dishes, faster; a Sunday, the lasagne tray.
            Special = ShiftPlan.TakeSpecial();
            _storyPace = schedule.HasValue ? schedule.Value.Pace : 1f;
            Quota = ShiftSchedule.QuotaFor(shiftDefinition.Quota, _storyPace, SpecialRules.PaceScale);

            using (StartupTimeline.Measure("shift: unpaid big dishwasher credit (save read)")) CreditUnpaidBigDishwasherLoad();
            using (StartupTimeline.Measure("shift: ConfigureBreakdowns")) ConfigureBreakdowns(schedule);
            using (StartupTimeline.Measure("shift: BuildBag")) _bag = BuildBag(schedule);
            using (StartupTimeline.Measure("shift: PrewarmDirt (starts the worker)")) PrewarmDirt();
            Phase = ServicePhase.Steady;
            chefOrders?.Configure(IsUnlocked(Mechanic.UrgentOrders) ? shiftDefinition.UrgentOrderChance : 0f, () => ServicePace,
                () => Phase == ServicePhase.Peak);
            RefreshChef();

            // Kuba on the board does the prep first (PrepBoardPanel); the sink, and the clock, come after.
            AwaitingPrep = ShiftPlan.TakePrepBeforeShift();
            if (!AwaitingPrep)
            {
                BeginSpawning();
            }
        }

        /// <summary>The chef calls from his day on, and not while the kitchen holds still for the guided plate or the board.</summary>
        private void RefreshChef()
        {
            if (chefOrders != null)
            {
                chefOrders.OrdersOn = IsUnlocked(Mechanic.ChefOrders) && !TutorialHold && !AwaitingPrep;
            }
        }

        /// <summary>
        /// What the machine may do this shift. Scheduled: the one scripted breakdown while Kuba has not yet met one, then only
        /// by chance - none at all before the scripted one, less the day after a breakdown (the schedule's scale) - and never
        /// two within a few cycles. The kitchen's own shift keeps its definition as it is.
        /// </summary>
        private void ConfigureBreakdowns(ScheduledShift? schedule)
        {
            if (dishwasher == null)
            {
                return;
            }

            int scripted = schedule.HasValue
                ? (schedule.Value.TutorialBreakdown ? ShiftSchedule.TutorialBreakdownCycle : 0)
                : shiftDefinition.ScriptedBreakdownCycle;
            if (schedule.HasValue && !MechanicUnlocks.IsUnlocked(Mechanic.Breakdowns, schedule.Value.DayNumber))
            {
                dishwasher.ConfigureBreakdowns(new DishwasherBreakdownSchedule(0, 0f, 0, 0)); // the machine just works, the first days
                return;
            }

            float chance = shiftDefinition.BreakdownChancePerCycle * (schedule.HasValue ? schedule.Value.BreakdownChanceScale : 1f);
            int most = scripted > 0 ? Mathf.Max(1, shiftDefinition.MaxBreakdowns) : shiftDefinition.MaxBreakdowns;
            dishwasher.ConfigureBreakdowns(new DishwasherBreakdownSchedule(scripted, chance, most, shiftDefinition.MinCyclesBetweenBreakdowns));
        }

        /// <summary>
        /// The deal of the night: the kind's mix (or a busy night's own, by dish id), shuffled for the day's seed when the kind
        /// says so - opening, on the days that teach, with the dishes the hints teach on.
        /// </summary>
        private ShuffleBag<DishDefinition> BuildBag(ScheduledShift? schedule)
        {
            var mix = new List<DishDefinition>(shiftDefinition.DishSpawnQueue);
            List<DishDefinition> busyNight = SpecialMix(mix);
            // Only the dishes whose day has come (the glass on day two, the bowl with the soak, the pots with the busy shifts).
            mix.RemoveAll(dish => dish == null || !MechanicUnlocks.IsDishUnlocked(dish.Id, Day));
            busyNight?.RemoveAll(dish => dish == null || !MechanicUnlocks.IsDishUnlocked(dish.Id, Day));
            if (busyNight != null && busyNight.Count == 0)
            {
                busyNight = null;
            }

            bool teaching = !schedule.HasValue || schedule.Value.DayNumber <= ShiftSchedule.FixedOpeningDays;
            List<DishDefinition> opening = busyNight == null && teaching
                ? new List<DishDefinition>(shiftDefinition.FixedOpening).FindAll(dish => dish != null && MechanicUnlocks.IsDishUnlocked(dish.Id, Day))
                : null;
            int seed = schedule.HasValue ? schedule.Value.Seed : 0;
            return new ShuffleBag<DishDefinition>(opening, busyNight ?? mix, dish => dish.Id, seed, shiftDefinition.ShuffleQueue,
                shiftDefinition.RepeatQueue);
        }

        /// <summary>The dirt of one dish of every kind the night brings, made ahead on a worker thread (DirtPrewarm).</summary>
        private void PrewarmDirt()
        {
            DirtPrewarm.Clear();
            int resolution = DeviceProfile.DirtResolution(QualityManager.ActiveTier);
            foreach (DishDefinition dish in shiftDefinition.DishSpawnQueue)
            {
                DirtPrewarm.Prepare(dish, resolution);
            }

            foreach (DishDefinition dish in shiftDefinition.FixedOpening)
            {
                DirtPrewarm.Prepare(dish, resolution);
            }
        }

        /// <summary>A busy night's dishes, by id, from the kinds the shift has (null on any other night).</summary>
        private List<DishDefinition> SpecialMix(List<DishDefinition> mix)
        {
            string[] ids = SpecialShifts.Queue(Special);
            if (ids == null)
            {
                return null;
            }

            var byId = new Dictionary<string, DishDefinition>();
            foreach (DishDefinition dish in mix)
            {
                if (dish != null)
                {
                    byId[dish.Id] = dish;
                }
            }

            foreach (DishDefinition dish in shiftDefinition.FixedOpening)
            {
                if (dish != null)
                {
                    byId[dish.Id] = dish;
                }
            }

            var busy = new List<DishDefinition>();
            foreach (string id in ids)
            {
                if (byId.TryGetValue(id, out DishDefinition dish))
                {
                    busy.Add(dish);
                }
            }

            return busy.Count > 0 ? busy : null;
        }

        private void BeginSpawning()
        {
            StartCoroutine(SpawnRoutine());
            if (Special == SpecialShift.LasagneTray && legendaryTray != null)
            {
                StartCoroutine(TrayRoutine());
            }
        }

        /// <summary>Sunday: a quarter of the way in, the legendary tray lands on the pile.</summary>
        private IEnumerator TrayRoutine()
        {
            while (!_shiftEnded && _timer.Progress01 < TrayArrivesAt)
            {
                yield return null;
            }

            if (!_shiftEnded)
            {
                LegendaryTray = SpawnDish(legendaryTray);
                TrayArrived = true;
            }
        }

        /// <summary>The prep is done: the shift begins, and what the board earned (in Dublin euros' worth: the place's
        /// small sums, <see cref="PlaceMoney.SmallSums"/>) is paid with it.</summary>
        public void FinishPrep(float bonusEuros)
        {
            if (!AwaitingPrep)
            {
                return;
            }

            AwaitingPrep = false;
            _prepBonus = Mathf.Max(0f, bonusEuros) * PlaceMoney.SmallSums(Place);
            RefreshChef();
            BeginSpawning();
        }

        private void Update()
        {
            if (_shiftEnded || _timer == null || AwaitingPrep)
            {
                return;
            }

            UpdateAutoStart();
            if (TutorialHold)
            {
                return; // the clock waits for the guided plate
            }

            _timer.Tick(Time.deltaTime);
            UpdateCrewHelp();
            if (LegendaryTray != null && LegendaryTray.CurrentStage >= WashStage.ReadyForRack)
            {
                TrayScrubbed = true; // clean by hand - wherever it goes from here
            }

            UpdateServicePhase();
            if (_timer.IsExpired)
            {
                EndShift();
            }
        }

        /// <summary>
        /// A hard night with the pile about to come down: a friend from the crew - once Kuba is one of them - takes a couple of
        /// dishes off the top to his own sink. Once a shift; what he takes counts as washed.
        /// </summary>
        private void UpdateCrewHelp()
        {
            if (!CrewHelpAvailable || CrewHelped > 0 || pileController == null || pileController.Fullness < CrewHelpAt ||
                AccessibilitySettings.RelaxedMode)
            {
                return;
            }

            Vector3 away = (spawnPoint != null ? spawnPoint.position : transform.position) + new Vector3(-3f, 1.5f, 0f);
            List<DishDefinition> taken = pileController.TakeFromTop(CrewHelpDishes, away);
            if (taken.Count == 0)
            {
                return;
            }

            CrewHelped = taken.Count;
            _dishesCleanedTotal += taken.Count;
            OnCrewHelped?.Invoke(CrewMate(_crewPlace, Day == MechanicUnlocks.Everything ? 99 : Day), taken.Count);
        }

        /// <summary>
        /// A full rack (and a free machine) counts down and starts itself - unless the setting is off, the guided plate is on,
        /// or the player tapped to cancel (then it waits until the rack changes). Another dish racked restarts the count.
        /// </summary>
        private void UpdateAutoStart()
        {
            int cells = rackGrid != null ? rackGrid.OccupiedCells : 0;
            bool ready = AccessibilitySettings.AutoDishwasher && !TutorialHold && rackGrid != null && rackGrid.IsFullLoad && CanStartDishwasher &&
                         cells != _autoStartCancelledAt;
            if (!ready)
            {
                AutoStartIn = -1f;
                _rackCellsSeen = cells;
                return;
            }

            if (AutoStartIn < 0f || cells != _rackCellsSeen)
            {
                AutoStartIn = AutoStartSeconds;
                _rackCellsSeen = cells;
            }

            AutoStartIn -= Time.deltaTime;
            if (AutoStartIn <= 0f)
            {
                AutoStartIn = -1f;
                TryStartDishwasherCycle();
            }
        }

        private void UpdateServicePhase()
        {
            ServicePhase phase = ServiceWaves.PhaseAt(shiftDefinition.Waves, _timer.Progress01, out int wave);
            if (phase == Phase)
            {
                return;
            }

            Phase = phase;
            PhaseCallout = wave >= 0 ? shiftDefinition.Waves[wave].callout : null;
            OnServicePhaseChanged?.Invoke(Phase, PhaseCallout);
        }

        /// <summary>
        /// A couple of dishes are waiting at the start, then the deal trickles in with a gap that shrinks as the shift goes on -
        /// pressure builds instead of arriving all at once - and closes up further inside a rush (the waves) and on a busy
        /// night, and a little more with every week of the story.
        /// </summary>
        private IEnumerator SpawnRoutine()
        {
            SpecialShiftRules rules = SpecialRules;
            while (!_shiftEnded)
            {
                while (TutorialHold && _spawnedDishes.Count >= 1 && !_shiftEnded)
                {
                    yield return null; // one plate to learn on; the rest come once it is done
                }

                DishDefinition next = _bag.Next();
                if (next == null)
                {
                    yield break;
                }

                SpawnDish(next);

                // Opening dishes are only staggered enough not to collide in mid-air.
                float progress = _timer.Progress01;
                yield return _spawnedDishes.Count < shiftDefinition.OpeningDishes + rules.ExtraOpeningDishes
                    ? new WaitForSeconds(OpeningStaggerSeconds)
                    : new WaitForSeconds(shiftDefinition.SpawnIntervalAt(progress) * rules.PaceScale * _storyPace /
                                         ServiceWaves.PaceAt(shiftDefinition.Waves, progress));
            }
        }

        private DishController SpawnDish(DishDefinition definition)
        {
            using var _spawn = StartupTimeline.Measure("shift: SpawnDish");
            Vector3 basePos = spawnPoint != null ? spawnPoint.position : transform.position;
            // Drop from above the current pile, so a new dish never spawns inside it.
            float pileTop = pileController != null ? pileController.CurrentHeight : 0f;
            Vector3 offset = new Vector3(UnityEngine.Random.Range(-0.3f, 0.3f), Mathf.Max(1.2f, pileTop + 0.5f), UnityEngine.Random.Range(-0.3f, 0.3f));
            DishController dish = DishController.Create(definition, basePos + offset, transform, rackGrid, washStation);
            _spawnedDishes.Add(dish);
            pileController?.Register(dish);
            return dish;
        }

        public void TryStartDishwasherCycle()
        {
            dishwasher?.StartCycle();
        }

        /// <summary>
        /// Dishes taken out of the big dishwasher in an earlier shift that never got paid (the app was killed, or they came out
        /// after the shift ended) are still owed: they count as washed in this one. They stay on the save until a paid shift
        /// clears them (MetaProgressController), so being killed again does not lose them either.
        /// </summary>
        private void CreditUnpaidBigDishwasherLoad()
        {
            int owed = SaveSystem.Load()?.bigDishwasherPendingDishes ?? 0;
            if (owed > 0)
            {
                BigDishwasherUnloaded += owed;
                _dishesCleanedTotal += owed;
            }
        }

        private void OnDishwasherCycleCompleted(IReadOnlyList<DishDefinition> washed)
        {
            if (_shiftEnded)
            {
                return; // a cycle finishing after closing time: the closing load already counted it
            }

            _dishesCleanedTotal += washed.Count;
        }

        /// <summary>Packing the rack tight pays: a full load extends Flow and adds to the wage.</summary>
        private void OnDishwasherCycleStarted(IReadOnlyList<DishDefinition> load, float fill)
        {
            if (fill < RackGrid.FullLoadFill - 1e-4f)
            {
                return;
            }

            _fullRackLoads++;
            flowController?.RegisterFullRack();
            OnFullRackLoaded?.Invoke();
        }

        private void OnBigDishwasherLoaded(int countInMachine)
        {
            BigDishwasherLoaded++;
        }

        /// <summary>The big machine's clean load: washed this shift (whenever it went in), and it pays its tips now.</summary>
        private void OnBigDishwasherUnloaded(IReadOnlyList<string> dishIds)
        {
            if (_shiftEnded)
            {
                return;
            }

            BigDishwasherUnloaded += dishIds.Count;
            _dishesCleanedTotal += dishIds.Count;
        }

        private void OnDishesBroken(int count)
        {
            _dishesBrokenTotal += count;
            flowController?.RegisterStreakBroken();
        }

        private void OnPileCollapsed(int dishesShoved)
        {
            PileCollapsed = true;
            _pileCollapses++;
        }

        /// <summary>Scrubbing with the tool for the dirt in front of it is careful work; with another, it still works - slowly.</summary>
        private void OnScrubbing(DishController dish, Vector3 point, ToolDefinition tool, float intensity)
        {
            if (_shiftEnded || AwaitingPrep || dish == null)
            {
                return;
            }

            if (dish.ToolSuits(tool))
            {
                _rightToolSeconds += Time.deltaTime;
            }
            else
            {
                _wrongToolSeconds += Time.deltaTime;
            }
        }

        private void OnDropMissed(DishController dish)
        {
            if (!_shiftEnded)
            {
                _rackDropsMissed++;
            }
        }

        /// <summary>Clock out now (the pause menu's "end the shift"): paid for what is done so far.</summary>
        public void EndShiftNow()
        {
            if (!_shiftEnded && _timer != null)
            {
                EndShift();
            }
        }

        private void EndShift()
        {
            _shiftEnded = true;
            StopAllCoroutines();

            // Closing time: the last load goes through before the lights go off - what is racked and what is still in
            // the machine comes out clean and counts. A machine broken down keeps its load for the morning.
            bool closingLoad = dishwasher != null && !dishwasher.IsBroken;
            if (closingLoad)
            {
                ClosingLoad = (dishwasher.IsRunning ? dishwasher.LoadCount : 0) + (rackGrid != null ? rackGrid.DishCount : 0);
                _dishesCleanedTotal += ClosingLoad;
            }

            var finalStages = new List<WashStage>();
            foreach (DishController dish in _spawnedDishes)
            {
                if (dish != null)
                {
                    finalStages.Add(closingLoad && dish.CurrentStage == WashStage.Racked ? WashStage.Done : dish.CurrentStage);
                }
            }

            float elapsed = _timer.Duration - _timer.Remaining;
            float shareWorked = _timer.Duration > 0f ? Mathf.Clamp01(elapsed / _timer.Duration) : 1f;

            // How it went: the whole shift's quota (walking out early leaves it unmet - the night is graded, not the minutes),
            // the care taken, the Flow, the chef's urgent orders, the breakages.
            ShiftScore score = ShiftScoring.Score(new ShiftPerformance
            {
                DishesCleaned = _dishesCleanedTotal,
                Quota = Quota,
                DishesBroken = _dishesBrokenTotal,
                PileCollapses = _pileCollapses,
                RackDropsMissed = _rackDropsMissed,
                WashAccuracy = WashAccuracy,
                BestFlow = flowController != null ? flowController.BestStreak : 0,
                UrgentOrders = chefOrders != null ? chefOrders.UrgentIssued : 0,
                UrgentOrdersMet = chefOrders != null ? chefOrders.UrgentMet : 0
            });

            ShiftResult result = ShiftResultCalculator.Calculate(finalStages, _dishesCleanedTotal, _dishesBrokenTotal, elapsed,
                _timer.Duration, _fullRackLoads, _hourlyWage, Place, Special, Quota, score.Grade);
            result.PrepBonus = _prepBonus;
            result.Wage += _prepBonus;
            result.ShiftTitle = Title;
            result.Quota = Quota; // the night's quota, as the HUD showed it (the pay for it is for the share worked)
            result.Score = score;
            result.UrgentOrders = chefOrders != null ? chefOrders.UrgentIssued : 0;
            result.UrgentOrdersMet = chefOrders != null ? chefOrders.UrgentMet : 0;
            result.WashAccuracy = WashAccuracy;
            result.RackDropsMissed = _rackDropsMissed;
            result.PileCollapses = _pileCollapses;
            result.Breakdowns = dishwasher != null ? dishwasher.Breakdowns : 0;
            result.EnergyCost = shiftDefinition.EnergyCost;
            result.CrewHelped = CrewHelped;

            // A special shift's envelope, thanks or whip-round, in the place's small sums - for the share of the night
            // worked (walking out of an inspection in the first minute passes nothing).
            result.InspectionPassed = !InspectionFailed;
            result.BestFlow = flowController != null ? flowController.BestStreak : 0;
            result.TrayScrubbed = TrayScrubbed;
            result.SpecialBonus = SpecialShifts.Bonus(Special, _dishesBrokenTotal, PileCollapsed, TrayScrubbed) * PlaceMoney.SmallSums(Place) * shareWorked;
            result.Wage += result.SpecialBonus;

            shiftCompletedChannel?.Raise(result);
        }
    }
}
