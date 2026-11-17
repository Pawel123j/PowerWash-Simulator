using System;
using System.Collections.Generic;
using UnityEngine;
using Zmywak.Core.Data;
using Zmywak.Kitchen.Dishes;
using Zmywak.Kitchen.Dishwasher;
using Zmywak.Kitchen.Feedback;
using Zmywak.Kitchen.Flow;

namespace Zmywak.Kitchen.Orders
{
    /// <summary>
    /// Periodically calls out for a specific dish type ("PLATES!"). Delivering one through the dishwasher in time extends
    /// the Flow streak; letting the order time out breaks it. Some calls are urgent ("a glass, NOW!"): the chef takes the
    /// dish straight off the rack, so it counts the moment one is racked clean - with a short window, which is a choice
    /// for the porter: finish the dish in hand, or put it in to soak and fetch what the pass needs. A missed urgent order
    /// costs the shift's score as well as the Flow. The chef only asks for what the porter can act on right now: a dish
    /// in the sink or one lying uncovered on the pile. Orders come sooner in a rush (the shift's waves), and are more
    /// often urgent at its peak, but never two urgent ones in a row.
    /// </summary>
    public class ChefOrderController : MonoBehaviour
    {
        // Nothing within reach yet (the pile is still falling in, or everything is washed): ask again soon.
        private const float RetrySeconds = 4f;
        private const float PeakUrgency = 1.5f;
        private const float MostUrgentShare = 0.9f;

        [SerializeField] private DishwasherController dishwasher;
        [SerializeField] private FlowStreakController flowController;
        [SerializeField] private float minIntervalSeconds = 20f;
        [SerializeField] private float maxIntervalSeconds = 35f;
        // An order only counts once a matching dish comes OUT of the dishwasher, so the window must cover a
        // full cycle on top of the time to pick, wash and rack the dish. It used to be a flat 20s - exactly
        // the length of the 20s cycle - which made reacting to an order impossible (only a dish that happened
        // to be mid-cycle already could ever satisfy it). Tuning value: seconds the player gets to react.
        [SerializeField] private float reactionSeconds = 35f;

        [Tooltip("An urgent order's window: time to fetch, wash and rack the dish (no machine cycle - it counts on the rack).")]
        [SerializeField] private float urgentReactionSeconds = 30f;

        private readonly List<DishDefinition> _inReach = new List<DishDefinition>();
        private float _nextOrderCountdown;
        private float _urgentChance;
        private Func<float> _pace = () => 1f;
        private Func<bool> _atPeak = () => false;
        private bool _lastWasUrgent;
        private float _nextFillableCheck;

        public DishDefinition ActiveOrder { get; private set; }
        public float OrderTimeRemaining { get; private set; }

        /// <summary>The active order is urgent: wanted on the rack now.</summary>
        public bool IsUrgent { get; private set; }

        /// <summary>Orders met this shift (the washed dish came out of the machine, or an urgent one was racked, in time).</summary>
        public int OrdersFulfilled { get; private set; }

        public int OrdersIssued { get; private set; }
        public int UrgentIssued { get; private set; }
        public int UrgentMet { get; private set; }
        public int UrgentMissed { get; private set; }

        /// <summary>Orders called off because nothing was left to fill them (for tests).</summary>
        public int OrdersWithdrawn { get; private set; }

        /// <summary>Whether the chef calls at all: not on the first day (the sink comes first), nor while the guided first plate
        /// holds the kitchen still. An order already called runs on.</summary>
        public bool OrdersOn { get; set; } = true;

        public event Action<DishDefinition> OnOrderIssued;
        public event Action OnOrderCleared;

        private void Awake()
        {
            _nextOrderCountdown = UnityEngine.Random.Range(minIntervalSeconds, maxIntervalSeconds);
        }

        private void OnEnable()
        {
            if (dishwasher != null)
            {
                dishwasher.OnCycleCompleted += HandleCycleCompleted;
            }

            KitchenFeedbackEvents.Racked += HandleRacked;
        }

        private void OnDisable()
        {
            if (dishwasher != null)
            {
                dishwasher.OnCycleCompleted -= HandleCycleCompleted;
            }

            KitchenFeedbackEvents.Racked -= HandleRacked;
        }

        /// <summary>
        /// Set by the shift as it starts: the share of orders that are urgent (0: none), and the service's pace and peak
        /// (<see cref="ServiceWaves"/>) - orders come that much sooner in a rush.
        /// </summary>
        public void Configure(float urgentChance, Func<float> pace, Func<bool> atPeak)
        {
            _urgentChance = Mathf.Clamp01(urgentChance);
            _pace = pace ?? (() => 1f);
            _atPeak = atPeak ?? (() => false);
        }

        /// <summary>The chance the next order is urgent: the shift's, half as much again at a rush's peak, never after another urgent one.</summary>
        public float UrgentChanceNow => _lastWasUrgent ? 0f : Mathf.Min(MostUrgentShare, _urgentChance * (_atPeak() ? PeakUrgency : 1f));

        private void Update()
        {
            if (ActiveOrder == null)
            {
                if (!OrdersOn)
                {
                    return;
                }

                _nextOrderCountdown -= Time.deltaTime * Mathf.Max(1f, _pace());
                if (_nextOrderCountdown <= 0f)
                {
                    IssueOrder();
                }
            }
            else
            {
                OrderTimeRemaining -= Time.deltaTime;
                if (OrderTimeRemaining <= 0f)
                {
                    ExpireOrder();
                }
                else if (Time.time >= _nextFillableCheck)
                {
                    // Once a second: the dish called for may have broken (the pile came down) and none of its kind be left.
                    _nextFillableCheck = Time.time + 1f;
                    if (!CanStillBeFilled(ActiveOrder, IsUrgent))
                    {
                        WithdrawOrder();
                    }
                }
            }
        }

        /// <summary>
        /// Whether any dish could still fill the order: an urgent one, a dish of the kind not yet racked; any other, one not
        /// yet out of the dishwasher (a racked one, or one washing inside it, still counts).
        /// </summary>
        private static bool CanStillBeFilled(DishDefinition order, bool urgent)
        {
            FindObjectsInactive inactive = urgent ? FindObjectsInactive.Exclude : FindObjectsInactive.Include;
            foreach (DishController dish in FindObjectsByType<DishController>(inactive))
            {
                if (dish.Definition == order && (urgent ? dish.CurrentStage < WashStage.Racked : dish.CurrentStage < WashStage.Done))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Nothing left to fill the order (the only one broke): the chef calls it off. No Flow lost and, for an urgent one,
        /// not counted against the shift - Kuba could not have met it.
        /// </summary>
        private void WithdrawOrder()
        {
            OrdersIssued = Mathf.Max(0, OrdersIssued - 1);
            if (IsUrgent)
            {
                UrgentIssued = Mathf.Max(0, UrgentIssued - 1);
            }

            OrdersWithdrawn++;
            ClearOrder();
        }

        private void IssueOrder()
        {
            DishDefinition order = PickOrder();
            if (order == null)
            {
                _nextOrderCountdown = RetrySeconds;
                return;
            }

            Issue(order, UnityEngine.Random.value < UrgentChanceNow);
        }

        /// <summary>Calls for a dish now (the shift's own calls, story events and tests); an urgent one counts when it is racked.</summary>
        public void Issue(DishDefinition dish, bool urgent)
        {
            if (dish == null)
            {
                return;
            }

            ActiveOrder = dish;
            IsUrgent = urgent;
            _lastWasUrgent = urgent;
            _nextFillableCheck = Time.time + 1f;
            OrdersIssued++;
            OrderTimeRemaining = urgent ? urgentReactionSeconds : reactionSeconds + (dishwasher != null ? dishwasher.CycleSeconds : 0f);
            if (urgent)
            {
                UrgentIssued++;
            }

            OnOrderIssued?.Invoke(ActiveOrder);
            if (urgent)
            {
                KitchenFeedbackEvents.RaiseUrgentOrder(dish);
            }
        }

        /// <summary>
        /// A random type among the dishes in reach, weighted by how many of each there are. Picking from every
        /// type in the shift used to call for glasses before any had arrived, or for the one plate buried under
        /// a pan - orders nobody could fill in time.
        /// </summary>
        private DishDefinition PickOrder()
        {
            _inReach.Clear();
            Camera view = Camera.main;
            foreach (DishController dish in FindObjectsByType<DishController>(FindObjectsInactive.Exclude))
            {
                if (dish.CurrentStage >= WashStage.Racked)
                {
                    continue; // on the rack or already washed - nothing left for the porter to do
                }

                if (dish.IsClaimed || (dish.IsSettledInPile && dish.IsTappableFrom(view)))
                {
                    _inReach.Add(dish.Definition);
                }
            }

            return _inReach.Count > 0 ? _inReach[UnityEngine.Random.Range(0, _inReach.Count)] : null;
        }

        private void HandleCycleCompleted(IReadOnlyList<DishDefinition> washed)
        {
            if (ActiveOrder == null)
            {
                return;
            }

            foreach (DishDefinition dish in washed)
            {
                if (dish == ActiveOrder)
                {
                    Fulfil();
                    return;
                }
            }
        }

        /// <summary>An urgent order is met the moment a matching dish is racked clean: the chef takes it from there.</summary>
        private void HandleRacked(DishController dish, Vector3 position)
        {
            if (ActiveOrder != null && IsUrgent && dish != null && dish.Definition == ActiveOrder)
            {
                Fulfil();
            }
        }

        private void Fulfil()
        {
            OrdersFulfilled++;
            if (IsUrgent)
            {
                UrgentMet++;
            }

            flowController?.RegisterOrderFulfilled();
            ClearOrder();
        }

        private void ExpireOrder()
        {
            if (IsUrgent)
            {
                UrgentMissed++;
            }

            flowController?.RegisterStreakBroken();
            ClearOrder();
        }

        private void ClearOrder()
        {
            ActiveOrder = null;
            IsUrgent = false;
            _nextOrderCountdown = UnityEngine.Random.Range(minIntervalSeconds, maxIntervalSeconds);
            OnOrderCleared?.Invoke();
        }
    }
}
