using System;
using UnityEngine;

namespace Zmywak.Kitchen.Flow
{
    /// <summary>
    /// Tracks the player's "flawless streak": fulfilling a chef order or sending a full rack through extends
    /// it, letting an order expire or breaking a dish resets it. The HUD reacts to <see cref="OnStreakChanged"/> (and, later, so will the radio).
    /// </summary>
    public class FlowStreakController : MonoBehaviour
    {
        public int CurrentStreak { get; private set; }

        /// <summary>The longest streak this shift (it counts towards promotions).</summary>
        public int BestStreak { get; private set; }
        public event Action<int> OnStreakChanged;

        public void RegisterOrderFulfilled() => Extend();

        /// <summary>A tightly packed, full rack went into the machine: efficient work keeps the flow going.</summary>
        public void RegisterFullRack() => Extend();

        private void Extend()
        {
            CurrentStreak++;
            BestStreak = Mathf.Max(BestStreak, CurrentStreak);
            OnStreakChanged?.Invoke(CurrentStreak);
        }

        public void RegisterStreakBroken()
        {
            if (CurrentStreak == 0)
            {
                return;
            }

            CurrentStreak = 0;
            OnStreakChanged?.Invoke(CurrentStreak);
        }
    }
}
