using System.Collections.Generic;
using UnityEngine;

namespace Zmywak.Core.Data
{
    /// <summary>
    /// One kind of shift (a calm start, a lunch rush, a hard night...): its dishes and their deal, its pace and its waves of
    /// service, the quota it asks for, how often the chef needs something urgently, and what the dishwasher may do.
    /// </summary>
    [CreateAssetMenu(menuName = "Zmywak/Shift Definition", fileName = "Shift_")]
    public class ShiftDefinition : ScriptableObject
    {
        [SerializeField] private string id;
        [SerializeField] private string displayName;

        [Tooltip("What Kuba is told about the night as it starts.")]
        [SerializeField] private string intro;

        [SerializeField] private float durationSeconds = 180f;

        [Tooltip("Energy the shift takes out of Kuba (0: the plain shift's, LifeRules.ShiftEnergyCost) - a calm one less, a hard night more.")]
        [SerializeField, Min(0f)] private float energyCost;

        [Tooltip("Clean dishes the shift asks for (before the story's pace and a busy night's grow it - ShiftSchedule.QuotaFor).")]
        [SerializeField, Min(1)] private int quota = 8;

        [Tooltip("Dishes waiting on the counter when the shift starts, so there is work from the first second.")]
        [SerializeField, Min(0)] private int openingDishes = 2;

        [Tooltip("Gap between dishes at the start of the shift; it shrinks linearly to the end value, so the pile " +
                 "pressure builds as the shift goes on instead of everything arriving at once.")]
        [SerializeField, Min(0.5f)] private float spawnIntervalStartSeconds = 18f;

        [SerializeField, Min(0.5f)] private float spawnIntervalEndSeconds = 9f;

        [Tooltip("Keep cycling through the queue until the shift ends. Off for scripted shifts with a fixed set of dishes.")]
        [SerializeField] private bool repeatQueue = true;

        [Tooltip("The shift's mix of dishes: as many of each as it should bring per round (the weights).")]
        [SerializeField] private List<DishDefinition> dishSpawnQueue = new List<DishDefinition>();

        [Tooltip("Deal each round of the mix in a shuffled order (ShuffleBag). Off: always the order above - the kitchen on its own, for the tests.")]
        [SerializeField] private bool shuffleQueue;

        [Tooltip("Dishes that come first, in this order, on the days that teach (ShiftSchedule.FixedOpeningDays).")]
        [SerializeField] private List<DishDefinition> fixedOpening = new List<DishDefinition>();

        [Tooltip("Rushes in the dining room: dishes, and the chef's orders, come faster inside them.")]
        [SerializeField] private List<ServiceWave> waves = new List<ServiceWave>();

        [Tooltip("How often a chef's order is urgent - the dish wanted on the rack now, not through the machine.")]
        [SerializeField, Range(0f, 1f)] private float urgentOrderChance;

        [Header("Dishwasher breakdowns")]
        [Tooltip("1-based dishwasher cycle that always breaks down, so the player is sure to meet the repair. 0 = none.")]
        [SerializeField, Min(0)] private int scriptedBreakdownCycle;

        [Tooltip("Chance that any later cycle breaks down (never the first one).")]
        [SerializeField, Range(0f, 1f)] private float breakdownChancePerCycle;

        [SerializeField, Min(0)] private int maxBreakdowns = 1;

        [Tooltip("Cycles that must run clean after a breakdown before another can come.")]
        [SerializeField, Min(0)] private int minCyclesBetweenBreakdowns;

        public string Id => id;
        public string DisplayName => displayName;
        public string Intro => intro;
        public float DurationSeconds => durationSeconds;
        public int Quota => quota;
        public float EnergyCost => energyCost;
        public int OpeningDishes => openingDishes;
        public bool RepeatQueue => repeatQueue;
        public IReadOnlyList<DishDefinition> DishSpawnQueue => dishSpawnQueue;
        public bool ShuffleQueue => shuffleQueue;
        public IReadOnlyList<DishDefinition> FixedOpening => fixedOpening;
        public IReadOnlyList<ServiceWave> Waves => waves;
        public float UrgentOrderChance => urgentOrderChance;
        public int ScriptedBreakdownCycle => scriptedBreakdownCycle;
        public float BreakdownChancePerCycle => breakdownChancePerCycle;
        public int MaxBreakdowns => maxBreakdowns;
        public int MinCyclesBetweenBreakdowns => minCyclesBetweenBreakdowns;

        /// <summary>Seconds until the next dish, given how far through the shift we are (0 = start, 1 = end).</summary>
        public float SpawnIntervalAt(float shiftProgress01)
        {
            return Mathf.Lerp(spawnIntervalStartSeconds, spawnIntervalEndSeconds, Mathf.Clamp01(shiftProgress01));
        }
    }
}
