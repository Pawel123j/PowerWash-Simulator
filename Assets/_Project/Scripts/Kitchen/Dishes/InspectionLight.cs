using UnityEngine;

namespace Zmywak.Kitchen.Dishes
{
    /// <summary>
    /// The reflection of the kitchen light on a glass held up to it: a bright vertical stripe, the way a
    /// lamp shows on the side of a tumbler. The dish texture is projected from above (UV = local x/z), so a
    /// band at one UV x lights the glass's walls at that x from rim to base. Tilting the phone left or right
    /// slides it across (the tilt held when the check began counts as level, so it works however the
    /// phone is held). Without tilt the finger can drag it; left alone it sweeps back and forth by itself.
    /// Hidden streaks only show inside it.
    /// </summary>
    public sealed class InspectionLight
    {
        /// <summary>Half the stripe's width, in dish UV units (the dish is the UV disc of radius 0.5).</summary>
        public const float HalfWidth = 0.1f;

        /// <summary>UV the stripe moves per unit of sideways tilt (gravity in g): ~20 degrees crosses half the glass.</summary>
        public const float TiltGain = 1.15f;

        private const float MaxOffset = 0.45f; // the stripe can reach the glass's edge, not beyond
        private const float Follow = 12f; // smoothing rate towards the tilt target
        private const float SweepSpeed = 0.45f; // UV per second when sweeping by itself
        private const float Center = 0.5f;

        private float _restX;
        private bool _hasRest;
        private float _sweepDirection = 1f;

        /// <summary>Where the stripe is across the dish (UV x): 0.5 is the middle.</summary>
        public float StripeX { get; private set; } = Center;

        /// <summary>Start a fresh check: the next tilt reading becomes "level" and the stripe starts centred.</summary>
        public void Reset()
        {
            _hasRest = false;
            _sweepDirection = 1f;
            StripeX = Center;
        }

        /// <summary>
        /// Follow the phone's tilt. <paramref name="gravity"/> is the accelerometer reading in device space
        /// (x to the right of the screen): tipping the phone's right edge down slides the stripe right.
        /// </summary>
        public void Tilt(Vector3 gravity, float deltaTime)
        {
            if (!_hasRest)
            {
                _restX = gravity.x;
                _hasRest = true;
            }

            float target = Center + Mathf.Clamp((gravity.x - _restX) * TiltGain, -MaxOffset, MaxOffset);
            StripeX = Mathf.Lerp(StripeX, target, 1f - Mathf.Exp(-Follow * deltaTime));
        }

        /// <summary>Slide the stripe by <paramref name="uvDelta"/> - a finger dragging beside the glass.</summary>
        public void Nudge(float uvDelta)
        {
            StripeX = Mathf.Clamp(StripeX + uvDelta, Center - MaxOffset, Center + MaxOffset);
            if (uvDelta != 0f)
            {
                _sweepDirection = Mathf.Sign(uvDelta); // if left alone, it carries on the way it was pushed
            }
        }

        /// <summary>Nothing steering it: the reflection sweeps from side to side by itself, from where it is.</summary>
        public void Drift(float deltaTime)
        {
            float limit = MaxOffset * 0.9f;
            StripeX += _sweepDirection * SweepSpeed * deltaTime;
            if (StripeX >= Center + limit || StripeX <= Center - limit)
            {
                StripeX = Mathf.Clamp(StripeX, Center - limit, Center + limit);
                _sweepDirection = -_sweepDirection;
            }
        }

        /// <summary>How strongly the reflection lights the dish at <paramref name="uv"/>: 1 along its middle, 0 past its edge.</summary>
        public float Intensity(Vector2 uv)
        {
            float t = 1f - Mathf.Abs(uv.x - StripeX) / HalfWidth;
            if (t <= 0f)
            {
                return 0f;
            }

            return t * t * (3f - 2f * t);
        }
    }
}
