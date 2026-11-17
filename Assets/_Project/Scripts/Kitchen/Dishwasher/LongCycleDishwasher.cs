using System;
using System.Collections.Generic;
using Zmywak.Core.Save;

namespace Zmywak.Kitchen.Dishwasher
{
    /// <summary>
    /// The big dishwasher's state, kept in real time: which dishes are in it (by id) and when it was started. It is
    /// never ticked by the game, only asked what time it is - so it carries on when the shift ends, through the
    /// evening at home and while the app is closed: half an hour after the start the load is clean, whenever anyone
    /// next looks. Dirty dishes go in as they are (no scrubbing); the price is the wait. Plain C#, for tests.
    /// </summary>
    public class LongCycleDishwasher
    {
        private readonly List<string> _load = new List<string>();

        public LongCycleDishwasher(int capacity, TimeSpan cycle)
        {
            Capacity = Math.Max(1, capacity);
            Cycle = cycle > TimeSpan.Zero ? cycle : TimeSpan.FromMinutes(30);
        }

        public int Capacity { get; }
        public TimeSpan Cycle { get; }
        public IReadOnlyList<string> Load => _load;
        public int Count => _load.Count;

        /// <summary>When the running (or finished) cycle was started; null while it is being loaded.</summary>
        public DateTime? StartedUtc { get; private set; }

        public bool IsStarted => StartedUtc.HasValue;
        public bool IsFull => _load.Count >= Capacity;

        /// <summary>Open for loading: not started, and room left.</summary>
        public bool CanLoad => !IsStarted && !IsFull;

        public bool CanStart => !IsStarted && _load.Count > 0;

        /// <summary>Started and still washing. A clock set back before the start counts as just started, not as done.</summary>
        public bool IsRunning(DateTime nowUtc) => IsStarted && nowUtc - StartedUtc.Value < Cycle;

        public bool IsDone(DateTime nowUtc) => IsStarted && nowUtc - StartedUtc.Value >= Cycle;

        /// <summary>Time left of the cycle (the whole cycle before the start, zero once done).</summary>
        public TimeSpan Remaining(DateTime nowUtc)
        {
            if (!IsStarted)
            {
                return Cycle;
            }

            TimeSpan elapsed = nowUtc - StartedUtc.Value;
            if (elapsed < TimeSpan.Zero)
            {
                elapsed = TimeSpan.Zero;
            }

            return elapsed >= Cycle ? TimeSpan.Zero : Cycle - elapsed;
        }

        /// <summary>0 at the start, 1 when done (0 while loading).</summary>
        public float Progress01(DateTime nowUtc) =>
            IsStarted ? (float)(1.0 - Remaining(nowUtc).TotalSeconds / Cycle.TotalSeconds) : 0f;

        public bool TryLoad(string dishId)
        {
            if (!CanLoad || string.IsNullOrEmpty(dishId))
            {
                return false;
            }

            _load.Add(dishId);
            return true;
        }

        public bool TryStart(DateTime nowUtc)
        {
            if (!CanStart)
            {
                return false;
            }

            StartedUtc = nowUtc;
            return true;
        }

        /// <summary>Takes the clean load out (only once the cycle is done) and leaves the machine empty for the next.</summary>
        public IReadOnlyList<string> Unload(DateTime nowUtc)
        {
            if (!IsDone(nowUtc))
            {
                return Array.Empty<string>();
            }

            var unloaded = new List<string>(_load);
            _load.Clear();
            StartedUtc = null;
            return unloaded;
        }

        /// <summary>
        /// The machine as the save left it. A start time no clock can hold (outside <see cref="DateTime"/>'s range) is no start: the
        /// load waits to be started again. With <paramref name="nowUtc"/>, a start more than a week ahead of it - far more than a
        /// clock set back by hand explains - counts as started just now, so a damaged time cannot jam the machine for years.
        /// </summary>
        public void ReadFrom(SaveData data, DateTime? nowUtc = null)
        {
            _load.Clear();
            StartedUtc = null;
            if (data == null)
            {
                return;
            }

            if (data.bigDishwasherLoad != null)
            {
                foreach (string id in data.bigDishwasherLoad)
                {
                    if (!string.IsNullOrEmpty(id) && _load.Count < Capacity)
                    {
                        _load.Add(id);
                    }
                }
            }

            if (data.bigDishwasherStartedUtcTicks > 0 && data.bigDishwasherStartedUtcTicks <= DateTime.MaxValue.Ticks && _load.Count > 0)
            {
                var started = new DateTime(data.bigDishwasherStartedUtcTicks, DateTimeKind.Utc);
                if (nowUtc.HasValue && started > nowUtc.Value.AddDays(7))
                {
                    started = nowUtc.Value;
                }

                StartedUtc = started;
            }
        }

        public void WriteTo(SaveData data)
        {
            data.bigDishwasherLoad = new List<string>(_load);
            data.bigDishwasherStartedUtcTicks = StartedUtc?.Ticks ?? 0;
        }
    }
}
