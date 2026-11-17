using UnityEngine;

namespace Zmywak.Kitchen.Shifts
{
    /// <summary>
    /// A tired Kuba scrubs and rinses slower, and his hand lags the finger: full strength from
    /// <see cref="RestedEnergy"/> up, easing down to <see cref="ExhaustedStrength"/> with nothing left. It is what makes the evening count - a night in the pub
    /// or a call home shows up at the sink the next day. Most weeks energy sits around 80, so it only bites
    /// after hard days (and the economy simulator plays with it on).
    /// </summary>
    public static class Fatigue
    {
        public const float RestedEnergy = 60f;
        public const float ExhaustedStrength = 0.7f;

        public static float ScrubStrength(float energy)
        {
            return Mathf.Lerp(ExhaustedStrength, 1f, Mathf.Clamp01(energy / RestedEnergy));
        }

        public static bool IsTired(float energy) => energy < RestedEnergy;
    }
}
