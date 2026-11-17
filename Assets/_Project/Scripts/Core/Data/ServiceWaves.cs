using System;
using System.Collections.Generic;
using UnityEngine;

namespace Zmywak.Core.Data
{
    /// <summary>Where a shift is in its rhythm: between rushes, the room filling up, the rush itself, the room emptying.</summary>
    public enum ServicePhase
    {
        Steady,
        Rising,
        Peak,
        Easing
    }

    /// <summary>
    /// A rush in the dining room: from <see cref="start01"/> to <see cref="end01"/> of the shift (0-1), dishes come in up to
    /// 1 + <see cref="intensity"/> times as fast - building up over the first fifth of it, dying away over the last fifth.
    /// </summary>
    [Serializable]
    public struct ServiceWave
    {
        public float start01;
        public float end01;
        public float intensity;

        /// <summary>What the kitchen hears as the rush peaks ("Lunch: sala pełna!"); a plain line when empty.</summary>
        public string callout;

        public ServiceWave(float start01, float end01, float intensity, string callout = null)
        {
            this.start01 = start01;
            this.end01 = end01;
            this.intensity = intensity;
            this.callout = callout;
        }
    }

    /// <summary>
    /// A shift's service in waves instead of one even pace: quiet, the room filling, a rush, a breather, another rush, the
    /// end. The waves speed up what already runs the shift - how often a dish arrives, how soon the chef calls for the
    /// next one - rather than adding a game of their own. Pure, so the shape of a shift can be tested.
    /// </summary>
    public static class ServiceWaves
    {
        /// <summary>The share of a wave spent building up at its start and dying away at its end.</summary>
        public const float RampShare = 0.2f;

        /// <summary>How busy the room is at this point of the shift: 1 between rushes, more inside one.</summary>
        public static float PaceAt(IReadOnlyList<ServiceWave> waves, float progress01)
        {
            float pace = 1f;
            if (waves == null)
            {
                return pace;
            }

            foreach (ServiceWave wave in waves)
            {
                pace += Mathf.Max(0f, wave.intensity) * Envelope(wave, progress01);
            }

            return pace;
        }

        /// <summary>0 outside the wave, rising smoothly to 1 over its first fifth, 1 through its middle, falling over its last fifth.</summary>
        public static float Envelope(ServiceWave wave, float progress01)
        {
            float length = wave.end01 - wave.start01;
            if (length <= 0f || progress01 < wave.start01 || progress01 > wave.end01)
            {
                return 0f;
            }

            float ramp = length * RampShare;
            if (progress01 < wave.start01 + ramp)
            {
                return Mathf.SmoothStep(0f, 1f, (progress01 - wave.start01) / ramp);
            }

            if (progress01 > wave.end01 - ramp)
            {
                return Mathf.SmoothStep(0f, 1f, (wave.end01 - progress01) / ramp);
            }

            return 1f;
        }

        /// <summary>The phase of the strongest wave at this point (<see cref="ServicePhase.Steady"/> outside them all), and which one it is.</summary>
        public static ServicePhase PhaseAt(IReadOnlyList<ServiceWave> waves, float progress01, out int waveIndex)
        {
            waveIndex = -1;
            if (waves == null)
            {
                return ServicePhase.Steady;
            }

            float strongest = 0f;
            for (int i = 0; i < waves.Count; i++)
            {
                float strength = Envelope(waves[i], progress01) * waves[i].intensity;
                if (strength > strongest)
                {
                    strongest = strength;
                    waveIndex = i;
                }
            }

            if (waveIndex < 0)
            {
                return ServicePhase.Steady;
            }

            ServiceWave wave = waves[waveIndex];
            float ramp = (wave.end01 - wave.start01) * RampShare;
            if (progress01 < wave.start01 + ramp)
            {
                return ServicePhase.Rising;
            }

            return progress01 > wave.end01 - ramp ? ServicePhase.Easing : ServicePhase.Peak;
        }
    }
}
