using System;
using System.Collections.Generic;
using UnityEngine;
using Zmywak.Core.Diagnostics;

namespace Zmywak.Kitchen.Feedback
{
    /// <summary>
    /// Kitchen sounds synthesised in code - running water, sponge and steel-wool scrubbing, the squeak of a
    /// clean plate, china clinks, a smash, the dishwasher's hum and ding. The game plays recorded takes of these
    /// (<see cref="KitchenSounds"/>); this is the fallback when a sound has none, and the reference level a
    /// recording is matched to. Plain sample math (noise, one-pole filters, decaying partials), deterministic per
    /// seed. The Generate* methods return raw samples so they can be unit tested; <see cref="Clip"/> wraps and
    /// caches them as AudioClips.
    /// </summary>
    public static class ProceduralSounds
    {
        public const int SampleRate = 22050;

        public enum Sound
        {
            Water,
            ScrubSoft,
            ScrubHarsh,
            Squeak,
            Clink,
            Crash,
            Scrape,
            Pickup,
            Hum,
            Ding,
            Fault,
            PowerUp,
            Clang,
            Chop,
            GlassClink,
            Splash,
            Click
        }

        /// <summary>Loops are built to wrap seamlessly (crossfaded ends, or whole periods).</summary>
        public static bool IsLoop(Sound sound) =>
            sound == Sound.Water || sound == Sound.ScrubSoft || sound == Sound.ScrubHarsh || sound == Sound.Hum;

        private static readonly Dictionary<Sound, AudioClip> Cache = new Dictionary<Sound, AudioClip>();

        public static AudioClip Clip(Sound sound)
        {
            if (!Cache.TryGetValue(sound, out AudioClip clip) || clip == null)
            {
                using var _synth = StartupTimeline.Measure("audio: kitchen sound synthesised (first use)");
                float[] samples = Generate(sound);
                clip = AudioClip.Create(sound.ToString(), samples.Length, 1, SampleRate, false);
                clip.SetData(samples, 0);
                Cache[sound] = clip;
            }

            return clip;
        }

        public static float[] Generate(Sound sound)
        {
            switch (sound)
            {
                case Sound.Water: return Water();
                case Sound.ScrubSoft: return Scrub(soft: true);
                case Sound.ScrubHarsh: return Scrub(soft: false);
                case Sound.Squeak: return Squeak();
                case Sound.Clink: return Clink(1f, 1f);
                case Sound.Crash: return Crash();
                case Sound.Scrape: return Scrape();
                case Sound.Pickup: return Pickup();
                case Sound.Hum: return Hum();
                case Sound.Ding: return Ding();
                case Sound.Fault: return Fault();
                case Sound.PowerUp: return PowerUp();
                case Sound.Clang: return Clang();
                case Sound.Chop: return Chop();
                case Sound.GlassClink: return Clink(1.45f, 0.8f); // thinner than china, so it rings higher
                case Sound.Splash: return Splash();
                case Sound.Click: return Click();
                default: throw new ArgumentOutOfRangeException(nameof(sound), sound, null);
            }
        }

        // ------------------------------------------------------------------
        // Sounds
        // ------------------------------------------------------------------

        private static float[] Water()
        {
            var random = new System.Random(11);
            const float seconds = 2f;
            const float fade = 0.15f;
            float[] raw = new float[Samples(seconds + fade)];
            float low = 0f;
            float lower = 0f;
            for (int i = 0; i < raw.Length; i++)
            {
                float white = Noise(random);
                low += 0.35f * (white - low);
                lower += 0.05f * (white - lower);
                // Rush of the stream (band between the two low-passes) plus a little hiss on top.
                raw[i] = (low - lower) * 0.9f + (white - low) * 0.15f;
            }

            // Bubbles: short rising sine chirps scattered through the loop.
            int bubbles = (int)(seconds * 28f);
            for (int b = 0; b < bubbles; b++)
            {
                int start = random.Next(0, raw.Length - Samples(0.03f));
                float freq = 350f + (float)random.NextDouble() * 600f;
                float amp = 0.08f + (float)random.NextDouble() * 0.12f;
                int length = Samples(0.012f + (float)random.NextDouble() * 0.02f);
                for (int k = 0; k < length; k++)
                {
                    float t = k / (float)SampleRate;
                    float env = Mathf.Sin(Mathf.PI * k / length);
                    raw[start + k] += amp * env * Mathf.Sin(2f * Mathf.PI * freq * (1f + t * 20f) * t);
                }
            }

            return Normalize(CrossfadeLoop(raw, Samples(fade)), 0.8f);
        }

        private static float[] Scrub(bool soft)
        {
            var random = new System.Random(soft ? 21 : 22);
            const float seconds = 0.8f;
            const float fade = 0.08f;
            float[] raw = new float[Samples(seconds + fade)];
            float low = 0f;
            float lower = 0f;
            float strokeRate = soft ? 7f : 9f;
            for (int i = 0; i < raw.Length; i++)
            {
                float t = i / (float)SampleRate;
                float white = Noise(random);
                low += (soft ? 0.25f : 0.6f) * (white - low);
                lower += (soft ? 0.03f : 0.2f) * (white - lower);
                float band = soft ? (low - lower) : (white - lower); // steel wool keeps the harsh highs
                // Back-and-forth strokes: loudness pulses with each rub, never fully silent.
                float stroke = 0.35f + 0.65f * Mathf.Pow(Mathf.Abs(Mathf.Sin(Mathf.PI * strokeRate * t)), 0.6f);
                float grit = soft ? 1f : 1f + 0.6f * (random.NextDouble() < 0.02 ? 1f : 0f); // wire snags
                raw[i] = band * stroke * grit;
            }

            if (soft)
            {
                // Foam: a quieter bubbly layer.
                for (int b = 0; b < 40; b++)
                {
                    int start = random.Next(0, raw.Length - Samples(0.02f));
                    float freq = 900f + (float)random.NextDouble() * 900f;
                    int length = Samples(0.008f);
                    for (int k = 0; k < length; k++)
                    {
                        raw[start + k] += 0.05f * Mathf.Sin(Mathf.PI * k / length) * Mathf.Sin(2f * Mathf.PI * freq * k / SampleRate);
                    }
                }
            }

            return Normalize(CrossfadeLoop(raw, Samples(fade)), soft ? 0.7f : 0.6f);
        }

        private static float[] Squeak()
        {
            const float seconds = 0.38f;
            float[] s = new float[Samples(seconds)];
            float phase = 0f;
            for (int i = 0; i < s.Length; i++)
            {
                float t = i / (float)SampleRate;
                float progress = t / seconds;
                float freq = 1300f + 1100f * progress * progress + 70f * Mathf.Sin(2f * Mathf.PI * 32f * t);
                phase += 2f * Mathf.PI * freq / SampleRate;
                float env = Mathf.Min(1f, t / 0.02f) * Mathf.Pow(1f - progress, 1.5f);
                s[i] = env * (Mathf.Sin(phase) + 0.3f * Mathf.Sin(2f * phase) + 0.12f * Mathf.Sin(3f * phase));
            }

            return Normalize(s, 0.5f);
        }

        /// <summary>China on china: a few inharmonic partials with fast, uneven decays.</summary>
        private static float[] Clink(float pitch, float level)
        {
            const float seconds = 0.6f;
            float[] s = new float[Samples(seconds)];
            float[] partials = { 2350f, 3710f, 5230f, 7140f };
            float[] amps = { 1f, 0.6f, 0.4f, 0.22f };
            float[] decays = { 9f, 13f, 19f, 27f };
            for (int i = 0; i < s.Length; i++)
            {
                float t = i / (float)SampleRate;
                float v = 0f;
                for (int p = 0; p < partials.Length; p++)
                {
                    v += amps[p] * Mathf.Exp(-decays[p] * t) * Mathf.Sin(2f * Mathf.PI * partials[p] * pitch * t);
                }

                s[i] = v;
            }

            return Normalize(s, 0.6f * level);
        }

        /// <summary>
        /// Steel knocked against something: a pan or a pot rings low and long - inharmonic partials, as a struck
        /// metal plate has, decaying slowly - after a short dull thud of the hit itself. China clinks; steel clangs.
        /// </summary>
        private static float[] Clang()
        {
            var random = new System.Random(47);
            const float seconds = 1.2f;
            float[] s = new float[Samples(seconds)];
            float[] partials = { 410f, 1095f, 1870f, 2960f, 4230f };
            float[] amps = { 1f, 0.7f, 0.45f, 0.3f, 0.16f };
            float[] decays = { 3.2f, 4.5f, 6.5f, 9f, 13f };
            float low = 0f;
            for (int i = 0; i < s.Length; i++)
            {
                float t = i / (float)SampleRate;
                float v = 0f;
                for (int p = 0; p < partials.Length; p++)
                {
                    v += amps[p] * Mathf.Exp(-decays[p] * t) * Mathf.Sin(2f * Mathf.PI * partials[p] * t);
                }

                low += 0.08f * (Noise(random) - low); // the thud: low-passed noise over the first few milliseconds
                v += low * 3f * Mathf.Exp(-60f * t);
                s[i] = v * Mathf.Min(1f, t / 0.002f);
            }

            return Normalize(s, 0.6f);
        }

        /// <summary>A knife through an onion onto a wooden board: the crunch of the cut, then the knock of the wood.</summary>
        private static float[] Chop()
        {
            var random = new System.Random(59);
            const float seconds = 0.25f;
            float[] s = new float[Samples(seconds)];
            float low = 0f;
            for (int i = 0; i < s.Length; i++)
            {
                float t = i / (float)SampleRate;
                float white = Noise(random);
                low += 0.35f * (white - low);
                float crunch = (white - low) * Mathf.Exp(-140f * t);                     // the bright, crisp cut
                float knock = (Mathf.Sin(2f * Mathf.PI * 210f * t) + 0.5f * Mathf.Sin(2f * Mathf.PI * 470f * t)) *
                              Mathf.Exp(-38f * t);                                           // the board, a hollow wooden thock
                s[i] = (crunch * 0.8f + knock) * Mathf.Min(1f, t / 0.0015f);
            }

            return Normalize(s, 0.7f);
        }

        /// <summary>A dish going under: a dull slap of water, then a few bubbles rising off it.</summary>
        private static float[] Splash()
        {
            var random = new System.Random(71);
            const float seconds = 0.45f;
            float[] s = new float[Samples(seconds)];
            float low = 0f;
            for (int i = 0; i < s.Length; i++)
            {
                float t = i / (float)SampleRate;
                low += 0.2f * (Noise(random) - low);
                s[i] = low * Mathf.Min(1f, t / 0.005f) * Mathf.Exp(-14f * t);
            }

            for (int b = 0; b < 4; b++)
            {
                int start = Samples(0.05f + b * 0.07f);
                int length = Samples(0.03f);
                float freq = 500f + b * 120f;
                for (int k = 0; k < length; k++)
                {
                    float t = k / (float)SampleRate;
                    s[start + k] += 0.25f * Mathf.Sin(Mathf.PI * k / length) * Mathf.Sin(2f * Mathf.PI * freq * (1f + t * 15f) * t);
                }
            }

            return Normalize(s, 0.5f);
        }

        /// <summary>A breaker snapping back in: a hard tick, a second smaller one, and the ring of the little box.</summary>
        private static float[] Click()
        {
            var random = new System.Random(73);
            const float seconds = 0.12f;
            float[] s = new float[Samples(seconds)];
            float second = 0.035f;
            for (int i = 0; i < s.Length; i++)
            {
                float t = i / (float)SampleRate;
                float tick = Noise(random) * Mathf.Exp(-300f * t);
                if (t >= second)
                {
                    tick += 0.5f * Noise(random) * Mathf.Exp(-300f * (t - second));
                }

                float body = Mathf.Sin(2f * Mathf.PI * 1800f * t) * Mathf.Exp(-45f * t);
                s[i] = tick + 0.6f * body;
            }

            return Normalize(s, 0.6f);
        }

        private static float[] Crash()
        {
            var random = new System.Random(31);
            const float seconds = 1f;
            float[] s = new float[Samples(seconds)];
            float low = 0f;
            for (int i = 0; i < s.Length; i++)
            {
                float t = i / (float)SampleRate;
                float white = Noise(random);
                low += 0.2f * (white - low);
                s[i] = (white - low) * Mathf.Exp(-7f * t) * 0.8f;
            }

            // Shards: a scatter of clinks at different pitches over the first quarter second.
            for (int c = 0; c < 7; c++)
            {
                float[] clink = Clink(0.75f + (float)random.NextDouble() * 0.8f, 0.5f + (float)random.NextDouble() * 0.5f);
                int offset = Samples((float)random.NextDouble() * 0.25f);
                for (int k = 0; k < clink.Length && offset + k < s.Length; k++)
                {
                    s[offset + k] += clink[k];
                }
            }

            return Normalize(s, 0.85f);
        }

        private static float[] Scrape()
        {
            var random = new System.Random(41);
            const float seconds = 0.24f;
            float[] s = new float[Samples(seconds)];
            float low = 0f;
            for (int i = 0; i < s.Length; i++)
            {
                float progress = i / (float)s.Length;
                float white = Noise(random);
                low += Mathf.Lerp(0.6f, 0.08f, progress) * (white - low); // swoosh closing down
                float env = Mathf.Min(1f, progress / 0.08f) * (1f - progress);
                s[i] = low * env;
            }

            return Normalize(s, 0.55f);
        }

        private static float[] Pickup()
        {
            const float seconds = 0.14f;
            float[] s = new float[Samples(seconds)];
            float phase = 0f;
            for (int i = 0; i < s.Length; i++)
            {
                float progress = i / (float)s.Length;
                phase += 2f * Mathf.PI * Mathf.Lerp(260f, 620f, progress) / SampleRate;
                s[i] = Mathf.Sin(Mathf.PI * progress) * Mathf.Sin(phase);
            }

            return Normalize(s, 0.25f);
        }

        /// <summary>
        /// Dishwasher: mains hum plus water sloshing. The tonal parts complete whole cycles in the one-second
        /// loop; the slosh is noise, so the loop is crossfaded like the others (which leaves the periodic
        /// hum untouched).
        /// </summary>
        private static float[] Hum()
        {
            var random = new System.Random(51);
            const float fade = 0.1f;
            float[] raw = new float[SampleRate + Samples(fade)];
            float low = 0f;
            for (int i = 0; i < raw.Length; i++)
            {
                float t = i / (float)SampleRate;
                float hum = Mathf.Sin(2f * Mathf.PI * 50f * t) + 0.5f * Mathf.Sin(2f * Mathf.PI * 100f * t) + 0.25f * Mathf.Sin(2f * Mathf.PI * 150f * t);
                low += 0.08f * (Noise(random) - low);
                float slosh = low * (0.6f + 0.4f * Mathf.Sin(2f * Mathf.PI * 2f * t));
                raw[i] = hum * 0.35f + slosh * 2.5f;
            }

            return Normalize(CrossfadeLoop(raw, Samples(fade)), 0.5f);
        }

        private static float[] Ding()
        {
            const float seconds = 1.4f;
            float[] s = new float[Samples(seconds)];
            float[] partials = { 1046.5f, 2093f, 3155f, 4190f };
            float[] amps = { 1f, 0.45f, 0.25f, 0.12f };
            float[] decays = { 2.6f, 4f, 6.5f, 9f };
            for (int i = 0; i < s.Length; i++)
            {
                float t = i / (float)SampleRate;
                float v = 0f;
                for (int p = 0; p < partials.Length; p++)
                {
                    v += amps[p] * Mathf.Exp(-decays[p] * t) * Mathf.Sin(2f * Mathf.PI * partials[p] * t);
                }

                s[i] = v * Mathf.Min(1f, t / 0.004f);
            }

            return Normalize(s, 0.55f);
        }

        /// <summary>The machine dying: a thump as it stops, a rattle, then the pump motor winding down.</summary>
        private static float[] Fault()
        {
            var random = new System.Random(61);
            const float seconds = 1.1f;
            float[] s = new float[Samples(seconds)];
            float phase = 0f;
            for (int i = 0; i < s.Length; i++)
            {
                float t = i / (float)SampleRate;
                float progress = t / seconds;
                phase += 2f * Mathf.PI * Mathf.Lerp(190f, 38f, Mathf.Sqrt(progress)) / SampleRate;
                // Buzzy odd harmonics, like a motor struggling against a jam.
                float motor = Mathf.Sin(phase) + 0.45f * Mathf.Sin(3f * phase) + 0.25f * Mathf.Sin(5f * phase);
                float motorEnv = Mathf.Min(1f, t / 0.01f) * (1f - progress) * (1f - progress);
                float thump = Mathf.Sin(2f * Mathf.PI * 55f * t) * Mathf.Exp(-18f * t) * 1.6f;
                float rattle = Noise(random) * Mathf.Exp(-30f * t) * 0.5f;
                s[i] = motor * motorEnv * 0.6f + thump + rattle;
            }

            return Normalize(s, 0.75f);
        }

        /// <summary>Back in business: the pump spinning up, with a bright "ready" blip on top.</summary>
        private static float[] PowerUp()
        {
            const float seconds = 0.9f;
            float[] s = new float[Samples(seconds)];
            float phase = 0f;
            float blipPhase = 0f;
            int blipStart = Samples(0.55f);
            int blipLength = Samples(0.09f);
            for (int i = 0; i < s.Length; i++)
            {
                float t = i / (float)SampleRate;
                float progress = t / seconds;
                phase += 2f * Mathf.PI * Mathf.Lerp(45f, 150f, 1f - (1f - progress) * (1f - progress)) / SampleRate;
                float tone = Mathf.Sin(phase) + 0.35f * Mathf.Sin(2f * phase) + 0.2f * Mathf.Sin(3f * phase);
                float env = Mathf.Min(1f, t / 0.05f) * Mathf.Min(1f, (seconds - t) / 0.25f);
                float v = tone * env;

                int k = i - blipStart;
                if (k >= 0 && k < blipLength)
                {
                    blipPhase += 2f * Mathf.PI * 1320f / SampleRate;
                    v += 0.5f * Mathf.Sin(Mathf.PI * k / blipLength) * Mathf.Sin(blipPhase);
                }

                s[i] = v;
            }

            return Normalize(s, 0.6f);
        }

        // ------------------------------------------------------------------
        // Helpers
        // ------------------------------------------------------------------

        private static int Samples(float seconds) => Mathf.CeilToInt(seconds * SampleRate);

        private static float Noise(System.Random random) => (float)(random.NextDouble() * 2.0 - 1.0);

        /// <summary>
        /// Loopable buffer: the extra tail beyond the loop length is faded into the head, so the last sample
        /// flows into the first when the clip repeats.
        /// </summary>
        private static float[] CrossfadeLoop(float[] raw, int fade)
        {
            int length = raw.Length - fade;
            var loop = new float[length];
            Array.Copy(raw, loop, length);
            for (int i = 0; i < fade; i++)
            {
                float t = i / (float)fade;
                loop[i] = raw[i] * t + raw[length + i] * (1f - t);
            }

            return loop;
        }

        private static float[] Normalize(float[] samples, float peak)
        {
            float max = 0f;
            foreach (float v in samples)
            {
                max = Mathf.Max(max, Mathf.Abs(v));
            }

            if (max <= 0f)
            {
                return samples;
            }

            float scale = peak / max;
            for (int i = 0; i < samples.Length; i++)
            {
                samples[i] *= scale;
            }

            return samples;
        }
    }
}
