using System;
using UnityEngine;
using Zmywak.Core.Diagnostics;

namespace Zmywak.Core.Audio
{
    /// <summary>
    /// The game's own sounds synthesised in code: the interface's clicks and chimes, the jingles of a shift's result,
    /// the menu's music - a slow, rainy-evening piano piece - and stand-ins for the room's recorded ambience. The setup
    /// bakes the jingles and the music into clips once (deterministic: the same samples every run); anything the
    /// <see cref="GameSoundBank"/> has no take of is made here when first played. Raw samples, so they can be tested.
    /// </summary>
    public static class GameSynth
    {
        public const int SampleRate = 22050;

        public static float[] Generate(GameSound sound)
        {
            switch (sound)
            {
                case GameSound.UiClick: return Pluck(1650f, 0.05f, 0.5f);
                case GameSound.UiConfirm: return Notes(0.55f, 0.11f, 0.35f, 659.3f, 987.8f);
                case GameSound.UiBack: return Notes(0.45f, 0.09f, 0.25f, 880f, 659.3f);
                case GameSound.UiError: return Buzz();
                case GameSound.UiToggle: return Pluck(1250f, 0.035f, 0.45f);
                case GameSound.UiTick: return Pluck(2600f, 0.018f, 0.3f);
                case GameSound.UiOpen: return Whoosh(0.22f, 0.3f);
                case GameSound.DialogueBlip: return Pluck(740f, 0.03f, 0.22f);
                case GameSound.PageTurn: return Whoosh(0.12f, 0.35f);
                case GameSound.Reward: return Notes(0.5f, 0.07f, 0.45f, 1568f, 2093f, 2637f);
                case GameSound.MoneyTick: return Pluck(3150f, 0.03f, 0.28f);
                case GameSound.GradeGood: return Notes(0.55f, 0.11f, 0.9f, 523.3f, 659.3f, 784f, 1046.5f);
                case GameSound.GradePoor: return Notes(0.45f, 0.28f, 0.7f, 392f, 311.1f);
                case GameSound.Promotion: return Fanfare();
                case GameSound.ShiftEnd: return Bell(880f, 1.6f, 0.6f);
                case GameSound.FlowChime: return Notes(0.4f, 0.06f, 0.6f, 1318.5f, 1568f, 1975.5f);
                case GameSound.FullRack: return Notes(0.45f, 0.1f, 0.45f, 784f, 1174.7f);
                case GameSound.WrongTool: return Thunk(150f, 0.16f, 0.45f);
                case GameSound.MenuMusic: return MenuMusic();
                case GameSound.AmbRain: return Rain();
                case GameSound.AmbCity: return City();
                case GameSound.AmbRoom: return Room();
                case GameSound.Creak: return Creak();
                case GameSound.Door: return Thunk(70f, 0.45f, 0.7f);
                case GameSound.Footsteps: return Footsteps();
                case GameSound.Thunder: return Thunder();
                default: throw new ArgumentOutOfRangeException(nameof(sound), sound, null);
            }
        }

        public static AudioClip Clip(GameSound sound)
        {
            using var _synth = StartupTimeline.Measure("audio: game sound synthesised");
            float[] samples = Generate(sound);
            AudioClip clip = AudioClip.Create(sound.ToString(), Mathf.Max(1, samples.Length), 1, SampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        // ------------------------------------------------------------------ the interface

        /// <summary>A soft plucked tick: a sine with a click of noise on its front, dying fast.</summary>
        private static float[] Pluck(float frequency, float seconds, float level)
        {
            var s = new float[Samples(seconds + 0.02f)];
            var random = new System.Random(Mathf.RoundToInt(frequency));
            for (int i = 0; i < s.Length; i++)
            {
                float t = i / (float)SampleRate;
                float env = Mathf.Exp(-t / (seconds * 0.35f));
                float transient = t < 0.004f ? Noise(random) * (1f - t / 0.004f) * 0.4f : 0f;
                s[i] = level * env * (Mathf.Sin(2f * Mathf.PI * frequency * t) + 0.3f * Mathf.Sin(4f * Mathf.PI * frequency * t) + transient);
            }

            return Normalize(Fade(s, 0.001f, 0.01f), level);
        }

        /// <summary>Little bells in a row, each ringing on under the next.</summary>
        private static float[] Notes(float level, float gap, float ring, params float[] frequencies)
        {
            var s = new float[Samples(gap * frequencies.Length + ring + 0.05f)];
            for (int n = 0; n < frequencies.Length; n++)
            {
                AddBell(s, n * gap, ring, frequencies[n], n == frequencies.Length - 1 ? 1f : 0.75f);
            }

            return Normalize(Fade(s, 0.002f, 0.05f), level);
        }

        private static void AddBell(float[] s, float start, float ring, float frequency, float level)
        {
            int from = Samples(start);
            int length = Samples(ring);
            for (int i = 0; i < length && from + i < s.Length; i++)
            {
                float t = i / (float)SampleRate;
                float attack = Mathf.Clamp01(t / 0.004f);
                float env = attack * Mathf.Exp(-t / (ring * 0.32f));
                float tone = Mathf.Sin(2f * Mathf.PI * frequency * t) + 0.35f * Mathf.Sin(2f * Mathf.PI * frequency * 2.01f * t) * Mathf.Exp(-t * 9f) +
                             0.12f * Mathf.Sin(2f * Mathf.PI * frequency * 3.02f * t) * Mathf.Exp(-t * 16f);
                s[from + i] += level * env * tone;
            }
        }

        /// <summary>A soft, low "no": two close tones beating against each other.</summary>
        private static float[] Buzz()
        {
            var s = new float[Samples(0.26f)];
            for (int i = 0; i < s.Length; i++)
            {
                float t = i / (float)SampleRate;
                float env = Mathf.Clamp01(t / 0.01f) * Mathf.Exp(-t / 0.12f);
                s[i] = env * (Mathf.Sin(2f * Mathf.PI * 196f * t) + Mathf.Sin(2f * Mathf.PI * 207.7f * t) +
                              0.25f * Mathf.Sin(2f * Mathf.PI * 392f * t));
            }

            return Normalize(Fade(s, 0.002f, 0.03f), 0.45f);
        }

        private static float[] Thunk(float frequency, float seconds, float level)
        {
            var s = new float[Samples(seconds)];
            var random = new System.Random(7);
            float lowpass = 0f;
            for (int i = 0; i < s.Length; i++)
            {
                float t = i / (float)SampleRate;
                float env = Mathf.Exp(-t / (seconds * 0.25f));
                float pitch = frequency * (1f + 0.6f * Mathf.Exp(-t * 40f));
                lowpass += 0.08f * (Noise(random) - lowpass);
                s[i] = env * (Mathf.Sin(2f * Mathf.PI * pitch * t) + 0.5f * lowpass);
            }

            return Normalize(Fade(s, 0.001f, 0.02f), level);
        }

        /// <summary>Noise swept through a band that opens and closes: air moving, a page turning.</summary>
        private static float[] Whoosh(float seconds, float level)
        {
            var s = new float[Samples(seconds)];
            var random = new System.Random(11);
            float low = 0f, band = 0f;
            for (int i = 0; i < s.Length; i++)
            {
                float p = i / (float)s.Length;
                float env = Mathf.Sin(Mathf.PI * p);
                float cut = Mathf.Lerp(0.04f, 0.35f, p);
                low += cut * (Noise(random) - low);
                band += cut * 0.5f * (low - band);
                s[i] = env * (low - band);
            }

            return Normalize(s, level);
        }

        private static float[] Fanfare()
        {
            var s = new float[Samples(2f)];
            float[] arpeggio = { 523.3f, 659.3f, 784f, 1046.5f };
            for (int n = 0; n < arpeggio.Length; n++)
            {
                AddBell(s, n * 0.09f, 0.6f, arpeggio[n], 0.8f);
            }

            // Then the chord held, with a shimmer on top.
            foreach (float f in new[] { 523.3f, 659.3f, 784f, 1046.5f, 1568f })
            {
                AddBell(s, 0.42f, 1.5f, f, 0.45f);
            }

            return Normalize(Fade(s, 0.002f, 0.2f), 0.6f);
        }

        /// <summary>A struck bell: inharmonic partials, the high ones dying first - the kitchen's bell at closing time.</summary>
        private static float[] Bell(float frequency, float seconds, float level)
        {
            var s = new float[Samples(seconds)];
            float[] ratios = { 1f, 2.0f, 2.76f, 5.4f, 8.93f };
            float[] levels = { 1f, 0.55f, 0.4f, 0.22f, 0.12f };
            for (int i = 0; i < s.Length; i++)
            {
                float t = i / (float)SampleRate;
                float sum = 0f;
                for (int p = 0; p < ratios.Length; p++)
                {
                    sum += levels[p] * Mathf.Exp(-t * (1.6f + p * 1.8f)) * Mathf.Sin(2f * Mathf.PI * frequency * ratios[p] * t);
                }

                s[i] = sum * Mathf.Clamp01(t / 0.002f);
            }

            return Normalize(Fade(s, 0f, 0.1f), level);
        }

        // ------------------------------------------------------------------ the menu's music

        // A slow nocturne in A minor, 66 bpm, two bars a chord: Am - F - C - G - Am - F - Dm - E.
        private static readonly int[][] Chords =
        {
            new[] { -12, 0, 3, 7 }, new[] { -16, -4, 0, 3 }, new[] { -9, 3, 7, 10 }, new[] { -14, -2, 2, 5 },
            new[] { -12, 0, 3, 7 }, new[] { -16, -4, 0, 3 }, new[] { -19, -7, -4, 0 }, new[] { -17, -5, -1, 2 }
        };

        // A few notes of melody over the second half of the progression (semitones above A4; -99 = rest), a quaver each.
        private static readonly int[] Melody =
        {
            -99, -99, -99, -99, 7, 5, 3, -99,   0, -99, -99, -99, -2, 0, 3, -99,
            -99, -99, 5, -99, 3, 2, 0, -99,     -1, -99, -99, -99, -99, -99, -99, -99
        };

        /// <summary>
        /// Soft electric piano chords broken into slow arpeggios over a warm pad, a melody coming in halfway, all in a small
        /// room's reverb - and wrapped, so the tails at the end ring on into the start: a loop with no seam.
        /// </summary>
        public static float[] MenuMusic()
        {
            const float bpm = 66f;
            float beat = 60f / bpm;
            int bars = Chords.Length * 2;
            float seconds = bars * 4 * beat;
            int length = Samples(seconds);
            var s = new float[length + Samples(4f)]; // the tails, folded back onto the start below
            const float a4 = 440f;

            for (int c = 0; c < Chords.Length; c++)
            {
                float chordStart = c * 8 * beat;
                int[] chord = Chords[c];

                // The pad: the chord's upper notes held across both bars, slowly swelling.
                for (int n = 1; n < chord.Length; n++)
                {
                    AddPad(s, chordStart, 8 * beat, a4 * Semitones(chord[n] - 12), 0.06f);
                }

                // The piano: the root low, then the chord broken upwards, a note on each beat.
                for (int b = 0; b < 8; b++)
                {
                    int note = b % 4 == 0 ? chord[0] - 12 : chord[(b % 4)];
                    float level = b % 4 == 0 ? 0.42f : 0.26f;
                    AddPiano(s, chordStart + b * beat, beat * 2.4f, a4 * Semitones(note), level);
                }
            }

            // The melody, the second time round only.
            float melodyStart = Chords.Length / 2 * 8 * beat;
            for (int i = 0; i < Melody.Length; i++)
            {
                if (Melody[i] > -99)
                {
                    AddPiano(s, melodyStart + i * beat, beat * 2.8f, a4 * Semitones(Melody[i]), 0.34f);
                }
            }

            // Softened before it is wrapped, so the filter runs on through the seam like everywhere else.
            float[] wet = Lowpass(Reverb(s, 0.32f), 0.45f);
            var loop = new float[length];
            for (int i = 0; i < wet.Length; i++)
            {
                loop[i % length] += wet[i];
            }

            return Normalize(loop, 0.55f);
        }

        /// <summary>An FM electric piano: a bell-ish attack mellowing into a warm tone.</summary>
        private static void AddPiano(float[] s, float start, float length, float frequency, float level)
        {
            int from = Samples(start);
            int count = Samples(length);
            for (int i = 0; i < count && from + i < s.Length; i++)
            {
                float t = i / (float)SampleRate;
                float env = Mathf.Clamp01(t / 0.006f) * Mathf.Exp(-t / (length * 0.38f));
                float index = 1.6f * Mathf.Exp(-t * 5f);
                float modulator = Mathf.Sin(2f * Mathf.PI * frequency * t);
                float tone = Mathf.Sin(2f * Mathf.PI * frequency * t + index * modulator) + 0.15f * Mathf.Sin(4f * Mathf.PI * frequency * t);
                s[from + i] += level * env * tone;
            }
        }

        private static void AddPad(float[] s, float start, float length, float frequency, float level)
        {
            int from = Samples(start);
            int count = Samples(length);
            for (int i = 0; i < count && from + i < s.Length; i++)
            {
                float t = i / (float)SampleRate;
                float p = t / length;
                float env = Mathf.Sin(Mathf.PI * Mathf.Clamp01(p)) * (0.8f + 0.2f * Mathf.Sin(2f * Mathf.PI * 0.3f * t));
                float tone = Mathf.Sin(2f * Mathf.PI * frequency * t) + 0.5f * Mathf.Sin(2f * Mathf.PI * frequency * 1.003f * t + 1f) +
                             0.25f * Mathf.Sin(2f * Mathf.PI * frequency * 2.002f * t);
                s[from + i] += level * env * tone;
            }
        }

        /// <summary>A small room: four combs in parallel, two allpasses after (Schroeder), mixed under the dry sound.</summary>
        private static float[] Reverb(float[] dry, float mix)
        {
            int[] combs = { 1116, 1188, 1277, 1356 };
            int[] allpasses = { 225, 556 };
            var wet = new float[dry.Length];
            foreach (int delay in combs)
            {
                int d = delay * SampleRate / 44100;
                var buffer = new float[d];
                int at = 0;
                for (int i = 0; i < dry.Length; i++)
                {
                    float y = buffer[at];
                    buffer[at] = dry[i] + y * 0.8f;
                    at = (at + 1) % d;
                    wet[i] += y * 0.25f;
                }
            }

            foreach (int delay in allpasses)
            {
                int d = delay * SampleRate / 44100;
                var buffer = new float[d];
                int at = 0;
                for (int i = 0; i < wet.Length; i++)
                {
                    float b = buffer[at];
                    float y = -wet[i] + b;
                    buffer[at] = wet[i] + b * 0.5f;
                    at = (at + 1) % d;
                    wet[i] = y;
                }
            }

            var mixed = new float[dry.Length];
            for (int i = 0; i < dry.Length; i++)
            {
                mixed[i] = dry[i] + wet[i] * mix;
            }

            return mixed;
        }

        // ------------------------------------------------------------------ the room

        /// <summary>Rain on the window: a hiss of fine drops and a patter of bigger ones, crossfaded into a loop.</summary>
        private static float[] Rain()
        {
            var random = new System.Random(3);
            var s = new float[Samples(6.5f)];
            float low = 0f, high = 0f;
            for (int i = 0; i < s.Length; i++)
            {
                float n = Noise(random);
                low += 0.25f * (n - low);
                high = n - low;
                s[i] = 0.35f * low + 0.12f * high;
                if (random.NextDouble() < 0.0016)
                {
                    AddDrop(s, i, 0.15f + (float)random.NextDouble() * 0.25f, 1800f + (float)random.NextDouble() * 2400f);
                }
            }

            return Normalize(LoopCrossfade(s, Samples(0.5f)), 0.5f);
        }

        private static void AddDrop(float[] s, int from, float level, float frequency)
        {
            int length = Samples(0.02f);
            for (int i = 0; i < length && from + i < s.Length; i++)
            {
                float t = i / (float)SampleRate;
                s[from + i] += level * Mathf.Exp(-t * 260f) * Mathf.Sin(2f * Mathf.PI * frequency * t * (1f - t * 8f));
            }
        }

        /// <summary>The city through a closed window: a low rumble of traffic, swelling as cars pass far off.</summary>
        private static float[] City()
        {
            var random = new System.Random(5);
            var s = new float[Samples(10f)];
            float brown = 0f, low = 0f;
            for (int i = 0; i < s.Length; i++)
            {
                float t = i / (float)SampleRate;
                brown = Mathf.Clamp(brown + Noise(random) * 0.02f, -1f, 1f) * 0.999f;
                low += 0.02f * (brown - low);
                float swell = 0.7f + 0.3f * Mathf.Sin(2f * Mathf.PI * t / 10f * 2f) * Mathf.Sin(2f * Mathf.PI * t / 10f * 3f);
                s[i] = low * swell;
            }

            return Normalize(LoopCrossfade(s, Samples(1f)), 0.5f);
        }

        /// <summary>A room's tone: the fridge's faint hum and the building's air.</summary>
        private static float[] Room()
        {
            var random = new System.Random(9);
            float loop = 4f;
            var s = new float[Samples(loop)];
            float brown = 0f;
            float hum = Mathf.Round(50f * loop) / loop;
            for (int i = 0; i < s.Length; i++)
            {
                float t = i / (float)SampleRate;
                brown = Mathf.Clamp(brown + Noise(random) * 0.015f, -1f, 1f) * 0.998f;
                s[i] = 0.5f * brown + 0.08f * Mathf.Sin(2f * Mathf.PI * hum * t) + 0.04f * Mathf.Sin(4f * Mathf.PI * hum * t);
            }

            return Normalize(LoopCrossfade(s, Samples(0.4f)), 0.4f);
        }

        private static float[] Creak()
        {
            var s = new float[Samples(0.7f)];
            var random = new System.Random(13);
            float phase = 0f;
            for (int i = 0; i < s.Length; i++)
            {
                float t = i / (float)SampleRate;
                float p = t / 0.7f;
                float f = 180f + 70f * Mathf.Sin(Mathf.PI * p) + 15f * Noise(random);
                phase += f / SampleRate;
                float stick = Mathf.Abs(Mathf.Sin(2f * Mathf.PI * 23f * t)) > 0.6f ? 1f : 0.4f;
                s[i] = Mathf.Sin(Mathf.PI * p) * stick * (2f * (phase - Mathf.Floor(phase + 0.5f)));
            }

            return Normalize(Fade(Lowpass(s, 0.3f), 0f, 0.05f), 0.35f);
        }

        private static float[] Footsteps()
        {
            var s = new float[Samples(1.6f)];
            float[] step = Thunk(90f, 0.18f, 1f);
            for (int n = 0; n < 3; n++)
            {
                int from = Samples(n * 0.55f);
                for (int i = 0; i < step.Length && from + i < s.Length; i++)
                {
                    s[from + i] += step[i] * (n == 1 ? 0.8f : 1f);
                }
            }

            return Normalize(Fade(Lowpass(s, 0.15f), 0f, 0.05f), 0.4f);
        }

        private static float[] Thunder()
        {
            var random = new System.Random(17);
            var s = new float[Samples(4f)];
            float brown = 0f;
            for (int i = 0; i < s.Length; i++)
            {
                float t = i / (float)SampleRate;
                brown = Mathf.Clamp(brown + Noise(random) * 0.05f, -1f, 1f) * 0.995f;
                float env = Mathf.Clamp01(t / 0.3f) * Mathf.Exp(-t / 1.4f) * (0.7f + 0.3f * Mathf.Sin(t * 9f));
                s[i] = brown * env;
            }

            return Normalize(Fade(Lowpass(s, 0.1f), 0f, 0.4f), 0.6f);
        }

        // ------------------------------------------------------------------ helpers

        private static float[] LoopCrossfade(float[] raw, int seam)
        {
            seam = Mathf.Clamp(seam, 1, raw.Length / 3);
            int length = raw.Length - seam;
            var loop = new float[length];
            Array.Copy(raw, loop, length);
            for (int i = 0; i < seam; i++)
            {
                float t = i / (float)seam * Mathf.PI / 2f;
                loop[i] = raw[i] * Mathf.Sin(t) + raw[length + i] * Mathf.Cos(t);
            }

            return loop;
        }

        private static float[] Lowpass(float[] x, float amount)
        {
            float y = 0f;
            for (int i = 0; i < x.Length; i++)
            {
                y += amount * (x[i] - y);
                x[i] = y;
            }

            return x;
        }

        private static float[] Fade(float[] x, float inSeconds, float outSeconds)
        {
            int fadeIn = Samples(inSeconds), fadeOut = Samples(outSeconds);
            for (int i = 0; i < fadeIn && i < x.Length; i++)
            {
                x[i] *= i / (float)fadeIn;
            }

            for (int i = 0; i < fadeOut && i < x.Length; i++)
            {
                x[x.Length - 1 - i] *= i / (float)fadeOut;
            }

            return x;
        }

        private static float[] Normalize(float[] x, float peak)
        {
            float max = 0f;
            foreach (float v in x)
            {
                max = Mathf.Max(max, Mathf.Abs(v));
            }

            if (max < 1e-6f)
            {
                return x;
            }

            float scale = peak / max;
            for (int i = 0; i < x.Length; i++)
            {
                x[i] *= scale;
            }

            return x;
        }

        private static float Semitones(int n) => Mathf.Pow(2f, n / 12f);

        private static int Samples(float seconds) => Mathf.Max(1, Mathf.RoundToInt(seconds * SampleRate));

        private static float Noise(System.Random random) => (float)(random.NextDouble() * 2.0 - 1.0);
    }
}
