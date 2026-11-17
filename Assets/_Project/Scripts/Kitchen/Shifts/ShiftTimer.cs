using UnityEngine;

namespace Zmywak.Kitchen.Shifts
{
    /// <summary>Simple countdown, kept as a plain class so it is testable without a running scene.</summary>
    public class ShiftTimer
    {
        public float Duration { get; }
        public float Remaining { get; private set; }
        public bool IsExpired => Remaining <= 0f;

        /// <summary>How far through the shift we are: 0 at the start, 1 once expired.</summary>
        public float Progress01 => Duration > 0f ? 1f - Remaining / Duration : 1f;

        public ShiftTimer(float duration)
        {
            Duration = duration;
            Remaining = duration;
        }

        public void Tick(float deltaTime)
        {
            Remaining = Mathf.Max(0f, Remaining - deltaTime);
        }
    }
}
