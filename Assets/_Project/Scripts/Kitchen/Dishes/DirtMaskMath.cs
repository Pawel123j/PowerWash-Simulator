using System.Collections.Generic;
using UnityEngine;

namespace Zmywak.Kitchen.Dishes
{
    /// <summary>Pure math for dirt-layer cleanliness, kept free of MonoBehaviour/ScriptableObject so it is unit-testable.</summary>
    public static class DirtMaskMath
    {
        public static float ComputeCleanliness01(IReadOnlyList<float> remainingPerLayer)
        {
            if (remainingPerLayer == null || remainingPerLayer.Count == 0)
            {
                return 1f;
            }

            float sum = 0f;
            for (int i = 0; i < remainingPerLayer.Count; i++)
            {
                sum += Mathf.Clamp01(remainingPerLayer[i]);
            }

            float averageRemaining = sum / remainingPerLayer.Count;
            return 1f - averageRemaining;
        }

        /// <param name="floor">How thin this tool can get the dirt: it never goes below this (nor touches dirt that is
        /// already thinner).</param>
        public static float ApplyScrub(float remaining, float toolPower, float deltaTime, float scrubDifficulty, float floor = 0f)
        {
            if (remaining <= floor)
            {
                return remaining;
            }

            float rate = toolPower / Mathf.Max(0.01f, scrubDifficulty);
            return Mathf.Max(floor, remaining - rate * deltaTime);
        }
    }
}
