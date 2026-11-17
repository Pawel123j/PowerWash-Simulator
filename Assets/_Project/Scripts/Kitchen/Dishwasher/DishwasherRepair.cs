using System;
using UnityEngine;

namespace Zmywak.Kitchen.Dishwasher
{
    /// <summary>What went wrong with the machine. Each fault is fixed with one of the sink's own gestures.</summary>
    public enum DishwasherFault
    {
        /// <summary>Scraps block the drain filter - flick them off (swipe, like scraping a plate).</summary>
        CloggedFilter,

        /// <summary>The spray arm has seized - turn it round until it runs free (circling, like scrubbing).</summary>
        StuckSprayArm,

        /// <summary>The safety cut-out tripped - hold the reset button in (press and hold, like rinsing).</summary>
        TrippedReset
    }

    /// <summary>
    /// The state of one repair, independent of how it is drawn or touched: the UI feeds it gestures in
    /// "play area" coordinates (a unit disc, centre 0,0, rim at radius 1) and reads progress back.
    /// </summary>
    public abstract class DishwasherRepair
    {
        public abstract DishwasherFault Fault { get; }

        /// <summary>0 = still broken, 1 = fixed.</summary>
        public abstract float Progress01 { get; }

        public bool IsDone => Progress01 >= 1f;

        /// <summary>Raised on each tangible step (a scrap flicked away, a quarter turn, the cut-out clicking in).</summary>
        public event Action Stepped;

        protected void RaiseStepped() => Stepped?.Invoke();

        public static DishwasherRepair Create(DishwasherFault fault, int seed)
        {
            switch (fault)
            {
                case DishwasherFault.CloggedFilter: return new CloggedFilterRepair(seed);
                case DishwasherFault.StuckSprayArm: return new StuckSprayArmRepair();
                case DishwasherFault.TrippedReset: return new TrippedResetRepair();
                default: throw new ArgumentOutOfRangeException(nameof(fault), fault, null);
            }
        }
    }

    /// <summary>A handful of food scraps stuck on the round drain filter; each one has to be flicked off.</summary>
    public sealed class CloggedFilterRepair : DishwasherRepair
    {
        /// <summary>How far (in play-area radii) a scrap has to be dragged before it comes loose.</summary>
        public const float FlickDistance = 0.3f;

        private const int Scraps = 6;
        private const float SpreadRadius = 0.62f;
        private const float MinSize = 0.13f;
        private const float MaxSize = 0.2f;

        private readonly Vector2[] _positions = new Vector2[Scraps];
        private readonly float[] _sizes = new float[Scraps];
        private readonly bool[] _cleared = new bool[Scraps];
        private int _clearedCount;

        public CloggedFilterRepair(int seed)
        {
            var random = new System.Random(seed);
            for (int i = 0; i < Scraps; i++)
            {
                _sizes[i] = MinSize + (float)random.NextDouble() * (MaxSize - MinSize);
                // Spread them out so no scrap hides another; fall back to any spot after a few tries.
                Vector2 best = Vector2.zero;
                float bestGap = -1f;
                for (int attempt = 0; attempt < 12; attempt++)
                {
                    double angle = random.NextDouble() * Math.PI * 2.0;
                    float radius = SpreadRadius * Mathf.Sqrt((float)random.NextDouble());
                    var candidate = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle)) * radius;
                    float gap = float.MaxValue;
                    for (int j = 0; j < i; j++)
                    {
                        gap = Mathf.Min(gap, Vector2.Distance(candidate, _positions[j]) - _sizes[j] - _sizes[i]);
                    }

                    if (gap > bestGap)
                    {
                        bestGap = gap;
                        best = candidate;
                    }

                    if (gap > 0.04f)
                    {
                        break;
                    }
                }

                _positions[i] = best;
            }
        }

        public override DishwasherFault Fault => DishwasherFault.CloggedFilter;
        public override float Progress01 => _clearedCount / (float)Scraps;

        public int ScrapCount => Scraps;
        public Vector2 ScrapPosition(int index) => _positions[index];
        public float ScrapSize(int index) => _sizes[index];
        public bool IsCleared(int index) => _cleared[index];

        /// <summary>The scrap still on the filter under <paramref name="point"/>, or -1. Later scraps sit on top.</summary>
        public int ScrapAt(Vector2 point)
        {
            for (int i = Scraps - 1; i >= 0; i--)
            {
                if (!_cleared[i] && Vector2.Distance(point, _positions[i]) <= _sizes[i] + 0.04f)
                {
                    return i;
                }
            }

            return -1;
        }

        /// <summary>Drag a scrap by <paramref name="drag"/>; far enough and it comes off. True if it did.</summary>
        public bool Flick(int index, Vector2 drag)
        {
            if (index < 0 || index >= Scraps || _cleared[index] || drag.magnitude < FlickDistance)
            {
                return false;
            }

            _cleared[index] = true;
            _clearedCount++;
            RaiseStepped();
            return true;
        }
    }

    /// <summary>
    /// A seized spray arm. It only follows part of the finger's movement at first and loosens as it turns;
    /// either direction counts, so rocking it back and forth works as well as spinning it.
    /// </summary>
    public sealed class StuckSprayArmRepair : DishwasherRepair
    {
        /// <summary>Degrees the arm itself has to travel before it runs free.</summary>
        public const float DegreesToFree = 540f;

        private const float StiffFollow = 0.35f;

        private float _travelled;

        public override DishwasherFault Fault => DishwasherFault.StuckSprayArm;
        public override float Progress01 => Mathf.Clamp01(_travelled / DegreesToFree);

        /// <summary>Where the arm points, in degrees - for drawing it.</summary>
        public float ArmAngle { get; private set; }

        /// <summary>The finger went round the hub by <paramref name="fingerDegrees"/> (signed).</summary>
        public void Turn(float fingerDegrees)
        {
            if (IsDone)
            {
                ArmAngle += fingerDegrees; // free now: it just spins with the finger
                return;
            }

            float armDegrees = fingerDegrees * Mathf.Lerp(StiffFollow, 1f, Progress01);
            ArmAngle += armDegrees;

            int quartersBefore = Mathf.FloorToInt(_travelled / 90f);
            _travelled = Mathf.Min(DegreesToFree, _travelled + Mathf.Abs(armDegrees));
            if (Mathf.FloorToInt(_travelled / 90f) > quartersBefore)
            {
                RaiseStepped();
            }
        }
    }

    /// <summary>The cut-out's reset button: it has to be held in; let go too early and it springs back out.</summary>
    public sealed class TrippedResetRepair : DishwasherRepair
    {
        public const float HoldSeconds = 1.5f;

        private const float SpringBackSpeed = 2f; // held-seconds lost per second after letting go

        private float _held;

        public override DishwasherFault Fault => DishwasherFault.TrippedReset;
        public override float Progress01 => Mathf.Clamp01(_held / HoldSeconds);

        public bool IsHeld { get; private set; }

        public void Hold(float deltaTime)
        {
            if (IsDone)
            {
                return;
            }

            IsHeld = true;
            _held = Mathf.Min(HoldSeconds, _held + deltaTime);
            if (IsDone)
            {
                RaiseStepped(); // the click of the breaker going back in
            }
        }

        public void Release(float deltaTime)
        {
            IsHeld = false;
            if (!IsDone)
            {
                _held = Mathf.Max(0f, _held - deltaTime * SpringBackSpeed);
            }
        }
    }
}
