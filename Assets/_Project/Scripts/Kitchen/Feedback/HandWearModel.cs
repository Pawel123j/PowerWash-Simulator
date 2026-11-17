using System;
using UnityEngine;

namespace Zmywak.Kitchen.Feedback
{
    /// <summary>How Kuba's arm looks at a moment of a shift, each 0-1 (the Zmywak/Hand shader draws it).</summary>
    public struct HandLook
    {
        /// <summary>Sweat on the skin and inside the glove, veins up the forearm.</summary>
        public float Tiredness;

        /// <summary>Hot water and scrubbing: the skin flushed red.</summary>
        public float Heat;

        /// <summary>Time at the sink this shift: a wet wrist, scuffed fingertips, a splashed sleeve.</summary>
        public float Soak;

        /// <summary>Years of wet work: dry, rough patches at the wrist.</summary>
        public float Weathering;

        /// <summary>A fine shake in the hand, only when he is worn out.</summary>
        public float Tremor;
    }

    /// <summary>
    /// What a shift does to the hands. Tiredness is mostly how tired he came in (the energy he started the shift
    /// with, as <see cref="Shifts.Fatigue"/> reads it) and partly the shift wearing on; the hot water soaks in as the
    /// shift goes; scrubbing hard flushes the skin; and every year at a sink leaves its mark. A rested Kuba ends a
    /// shift sweaty and wet to the wrist; only a worn-out one shakes.
    /// </summary>
    public static class HandWearModel
    {
        /// <summary>His first shift in Dublin.</summary>
        public static readonly DateTime FirstShift = new DateTime(2004, 5, 1);

        /// <summary>How long it takes wet work to give him a dishwasher's hands.</summary>
        public const float YearsToWeather = 4f;

        /// <summary>Tiredness from which the hand starts to shake.</summary>
        public const float TremorFrom = 0.6f;

        public static HandLook Evaluate(float energyAtStart, float shiftProgress01, float exertion01, DateTime today)
        {
            float tiredness = Tiredness(energyAtStart, shiftProgress01);
            float soak = Soak(shiftProgress01);
            return new HandLook
            {
                Tiredness = tiredness,
                Heat = Heat(soak, exertion01),
                Soak = soak,
                Weathering = Weathering(today),
                Tremor = Tremor(tiredness)
            };
        }

        public static float Tiredness(float energyAtStart, float shiftProgress01)
        {
            float cameIn = 1f - Mathf.Clamp01(energyAtStart / 100f);
            return Mathf.Clamp01(cameIn * 0.85f + Mathf.Clamp01(shiftProgress01) * 0.35f);
        }

        /// <summary>Wet to the wrist by four fifths of the way through.</summary>
        public static float Soak(float shiftProgress01) => Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(shiftProgress01 * 1.25f));

        /// <summary>A little from the start (the water is hot), more as it soaks in, more again while scrubbing.</summary>
        public static float Heat(float soak, float exertion01) => Mathf.Clamp01(0.1f + soak * 0.35f + Mathf.Clamp01(exertion01) * 0.4f);

        public static float Weathering(DateTime today)
        {
            double years = (today - FirstShift).TotalDays / 365.25;
            return Mathf.Clamp01((float)years / YearsToWeather);
        }

        public static float Tremor(float tiredness) => Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(TremorFrom, 1f, tiredness));
    }
}
