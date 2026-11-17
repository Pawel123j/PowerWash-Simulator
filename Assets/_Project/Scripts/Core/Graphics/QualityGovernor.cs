using System.Collections.Generic;
using UnityEngine;

namespace Zmywak.Core.Graphics
{
    /// <summary>
    /// Decides when the graphics tier should change, frame by frame - plain C#, so it is tested without a device.
    /// The soak test showed what the old loop (a 3 s average against a fixed 22 ms) did: one 1 s hitch - a scene
    /// load, a GC pause, a notification - pushed the average over and dropped quality for good, because at Low's
    /// 30 fps cap no frame could ever look fast enough to climb back. Now:
    /// <list type="bullet">
    /// <item>each 3 s window is judged by its median frame, which a hitch cannot move;</item>
    /// <item>a window is slow when that median misses the tier's frame-rate cap by more than a quarter;</item>
    /// <item>two slow windows in a row step down one tier;</item>
    /// <item>below the starting tier it tries one step up again after a while; a try that proves slow comes
    /// back down and the next one waits twice as long (at most 8 minutes); one that holds resets the wait.</item>
    /// </list>
    /// </summary>
    public sealed class QualityGovernor
    {
        public const float WindowSeconds = 3f;
        public const int SlowWindowsToStepDown = 2;
        public const float FirstRetrySeconds = 60f;
        public const float LongestRetrySeconds = 480f;
        public const float TryHeldSeconds = 30f;

        private readonly QualityTier _ceiling;
        private readonly List<float> _window = new List<float>(256);
        private float _windowElapsed;
        private int _slowWindows;
        private float _sinceChange;
        private bool _trying;

        public QualityGovernor(QualityTier start, QualityTier ceiling)
        {
            Tier = start;
            _ceiling = ceiling;
            RetryAfterSeconds = FirstRetrySeconds;
        }

        public QualityTier Tier { get; private set; }

        /// <summary>How long it waits below the ceiling before trying a step up.</summary>
        public float RetryAfterSeconds { get; private set; }

        public static int TargetFrameRate(QualityTier tier) => tier == QualityTier.Low ? 30 : 60;

        /// <summary>The longest a window's median frame may take at this tier: the frame-rate cap's frame plus a quarter.</summary>
        public static float BudgetSeconds(QualityTier tier) => 1.25f / TargetFrameRate(tier);

        /// <summary>Somebody set the tier directly (a settings screen, a test): judge from there.</summary>
        public void Reset(QualityTier tier)
        {
            Change(tier, trying: false);
        }

        /// <summary>Feeds one frame's real duration; true when <see cref="Tier"/> changed.</summary>
        public bool Frame(float seconds)
        {
            _window.Add(seconds);
            _windowElapsed += seconds;
            _sinceChange += seconds;

            if (_trying && _sinceChange >= TryHeldSeconds)
            {
                _trying = false; // the step up held
                RetryAfterSeconds = FirstRetrySeconds;
            }

            if (_windowElapsed < WindowSeconds)
            {
                return false;
            }

            float median = Median(_window);
            _window.Clear();
            _windowElapsed = 0f;

            bool slow = median > BudgetSeconds(Tier);
            _slowWindows = slow ? _slowWindows + 1 : 0;

            if (_slowWindows >= SlowWindowsToStepDown && Tier > QualityTier.Low)
            {
                if (_trying)
                {
                    RetryAfterSeconds = Mathf.Min(RetryAfterSeconds * 2f, LongestRetrySeconds);
                }

                return Change(Tier - 1, trying: false);
            }

            if (!slow && Tier < _ceiling && _sinceChange >= RetryAfterSeconds)
            {
                return Change(Tier + 1, trying: true);
            }

            return false;
        }

        private bool Change(QualityTier tier, bool trying)
        {
            bool changed = tier != Tier;
            Tier = tier;
            _trying = trying;
            _sinceChange = 0f;
            _slowWindows = 0;
            _window.Clear();
            _windowElapsed = 0f;
            return changed;
        }

        private static float Median(List<float> values)
        {
            values.Sort();
            int middle = values.Count / 2;
            return values.Count % 2 == 1 ? values[middle] : (values[middle - 1] + values[middle]) * 0.5f;
        }
    }
}
