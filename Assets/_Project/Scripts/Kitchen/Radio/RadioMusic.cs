using System;
using UnityEngine;

namespace Zmywak.Kitchen.Radio
{
    /// <summary>
    /// What the kitchen radio plays, synthesised in code like the rest of the kitchen's sounds - no audio files: a jig
    /// for the trad station (fiddle-ish melody over a drone and a bodhrán), a four-chord pop song (electric piano,
    /// bass, kick, snare and hats) and the news (a jingle, then a presenter's voice too far off to make out). Each is a
    /// seamless loop, and all of it goes through a small, cheap speaker (<see cref="SmallSpeaker"/>). Deterministic,
    /// and returned as raw samples so it can be tested; <see cref="KitchenRadio"/> turns it into AudioClips.
    /// </summary>
    public static class RadioMusic
    {
        public const int SampleRate = 22050;

        public static float[] Generate(RadioStation station)
        {
            switch (station)
            {
                case RadioStation.Trad: return SmallSpeaker(Jig());
                case RadioStation.Pop: return SmallSpeaker(Pop());
                case RadioStation.News: return SmallSpeaker(News());
                default: return Array.Empty<float>();
            }
        }

        // ------------------------------------------------------------------
        // Trad: a jig in D, 6/8, a dotted crotchet at 116
        // ------------------------------------------------------------------

        // Two four-bar phrases (A, then B), each bar six quavers; semitones above D (0), -1 is a rest.
        private static readonly int[] JigTune =
        {
            0, 4, 7, 12, 7, 4,     2, 5, 9, 14, 9, 5,     4, 7, 11, 16, 14, 12,   11, 9, 7, 4, 2, 0,
            7, 11, 14, 19, 14, 11, 9, 12, 16, 12, 9, 7,   11, 9, 7, 5, 4, 2,      0, 4, 7, 0, -1, -1
        };

        private static float[] Jig()
        {
            const float bpm = 116f; // dotted crotchets
            float quaver = 60f / bpm / 3f;
            int phraseRepeats = 2;
            float seconds = quaver * JigTune.Length * phraseRepeats;
            float[] s = new float[Samples(seconds)];
            float d = 293.66f; // D4
            float loop = s.Length / (float)SampleRate;

            // The drone: D and A, low and steady, breathing a little - each tuned to a whole number of cycles in the loop,
            // so it joins up without a click.
            float droneD = InLoop(d * 0.5f, loop);
            float droneA = InLoop(d * 0.75f, loop);
            float breath = InLoop(0.25f, loop);
            for (int i = 0; i < s.Length; i++)
            {
                float t = i / (float)SampleRate;
                s[i] += 0.12f * (Saw(droneD * t) + 0.6f * Saw(droneA * t)) * (0.85f + 0.15f * Mathf.Sin(2f * Mathf.PI * breath * t));
            }

            for (int r = 0; r < phraseRepeats; r++)
            {
                for (int n = 0; n < JigTune.Length; n++)
                {
                    float start = (r * JigTune.Length + n) * quaver;
                    if (JigTune[n] >= 0)
                    {
                        // Fiddle-ish: a bowed sawtooth with vibrato, a quick attack and a short release.
                        AddNote(s, start, quaver * 0.95f, d * Semitones(JigTune[n]), 0.32f, Fiddle);
                    }

                    // Bodhrán: a deep thump on each beat (every third quaver), a light tap on the others.
                    AddDrum(s, start, n % 3 == 0 ? 0.55f : 0.18f, n % 3 == 0 ? 85f : 140f, n % 3 == 0 ? 22f : 45f);
                }
            }

            return Normalize(s, 0.8f);
        }

        // ------------------------------------------------------------------
        // Pop: I - V - vi - IV in G at 100 bpm
        // ------------------------------------------------------------------

        private static readonly int[][] PopChords =
        {
            new[] { 0, 4, 7 },   // G
            new[] { 7, 11, 14 }, // D
            new[] { 9, 12, 16 }, // Em
            new[] { 5, 9, 12 }   // C
        };

        // A hook over the chords: one note a beat, semitones above G4; -1 holds.
        private static readonly int[] PopHook = { 12, 11, 7, -1, 14, 12, 11, -1, 16, 14, 12, 11, 9, 7, 9, -1 };

        private static float[] Pop()
        {
            const float bpm = 100f;
            float beat = 60f / bpm;
            int bars = 8;
            float seconds = beat * 4f * bars;
            float[] s = new float[Samples(seconds)];
            float g = 196f; // G3

            for (int bar = 0; bar < bars; bar++)
            {
                int[] chord = PopChords[bar % PopChords.Length];
                float barStart = bar * 4f * beat;
                foreach (int note in chord)
                {
                    AddNote(s, barStart, 4f * beat * 0.98f, g * Semitones(note), 0.16f, ElectricPiano); // the chord, held
                }

                for (int b = 0; b < 4; b++)
                {
                    float at = barStart + b * beat;
                    AddNote(s, at, beat * 0.8f, g * 0.5f * Semitones(chord[0]), 0.4f, Bass);   // root on every beat
                    int hook = PopHook[(bar * 4 + b) % PopHook.Length];
                    if (hook >= 0 && bar % 2 == 1)
                    {
                        AddNote(s, at, beat * 0.9f, g * 2f * Semitones(hook - 12), 0.22f, ElectricPiano);
                    }

                    AddDrum(s, at, b % 2 == 0 ? 0.7f : 0f, 60f, 18f);          // kick on 1 and 3
                    if (b % 2 == 1)
                    {
                        AddNoise(s, at, 0.16f, 0.4f, 18f, 0.6f);               // snare on 2 and 4
                    }

                    AddNoise(s, at, 0.05f, 0.12f, 60f, 0.95f);                 // hats on the beat...
                    AddNoise(s, at + beat * 0.5f, 0.05f, 0.08f, 60f, 0.95f);   // ...and between
                }
            }

            return Normalize(s, 0.8f);
        }

        // ------------------------------------------------------------------
        // News: a jingle, then a presenter too far off to make out
        // ------------------------------------------------------------------

        private static float[] News()
        {
            var random = new System.Random(71);
            const float seconds = 12f;
            float[] s = new float[Samples(seconds)];

            // The jingle: three rising notes and a chord.
            float[] jingle = { 523.25f, 659.25f, 783.99f };
            for (int n = 0; n < jingle.Length; n++)
            {
                AddNote(s, n * 0.16f, 0.15f, jingle[n], 0.35f, ElectricPiano);
                AddNote(s, 0.5f, 0.9f, jingle[n], 0.22f, ElectricPiano);
            }

            // The voice: syllables of filtered buzz shaped like speech - a falling pitch per phrase, formant-ish noise,
            // pauses between sentences - intelligible as a voice, not as words.
            float t0 = 1.6f;
            while (t0 < seconds - 1f)
            {
                int syllables = 6 + random.Next(9);
                float pitch = 125f + (float)random.NextDouble() * 20f;
                for (int k = 0; k < syllables && t0 < seconds - 0.6f; k++)
                {
                    float length = 0.11f + (float)random.NextDouble() * 0.12f;
                    float glide = 1f - 0.18f * k / syllables;
                    AddSyllable(s, t0, length, pitch * glide, random);
                    t0 += length + 0.02f + (float)random.NextDouble() * 0.05f;
                }

                t0 += 0.35f + (float)random.NextDouble() * 0.3f; // a breath between sentences
            }

            return Normalize(s, 0.75f);
        }

        private static void AddSyllable(float[] s, float start, float length, float pitch, System.Random random)
        {
            int from = Samples(start);
            int count = Samples(length);
            float formant = 600f + (float)random.NextDouble() * 900f; // the vowel
            float low = 0f;
            float band = 0f;
            for (int k = 0; k < count && from + k < s.Length; k++)
            {
                float t = k / (float)SampleRate;
                float env = Mathf.Sin(Mathf.PI * k / count);
                float buzz = Saw(pitch * t) * 0.6f + Noise(random) * 0.25f;
                // A crude resonance around the formant: two one-pole filters make a band-pass.
                float a = Mathf.Clamp01(formant * 2f / SampleRate);
                low += a * (buzz - low);
                band += a * (low - band);
                s[from + k] += (low - band) * env * 0.9f;
            }
        }

        // ------------------------------------------------------------------
        // Instruments
        // ------------------------------------------------------------------

        private delegate float Voice(float frequency, float t, float noteLength);

        private static float Fiddle(float f, float t, float length)
        {
            float vibrato = 1f + 0.006f * Mathf.Sin(2f * Mathf.PI * 5.5f * t) * Mathf.Clamp01(t * 6f);
            float env = Mathf.Min(1f, t / 0.02f) * Mathf.Min(1f, Mathf.Max(0f, length - t) / 0.03f);
            float saw = Saw(f * vibrato * t);
            return (saw * 0.7f + Mathf.Sin(2f * Mathf.PI * f * vibrato * t) * 0.3f) * env;
        }

        private static float ElectricPiano(float f, float t, float length)
        {
            // Two-operator FM, bell-like at the start and mellowing as it rings.
            float index = 2.2f * Mathf.Exp(-6f * t);
            float modulator = Mathf.Sin(2f * Mathf.PI * f * t) * index;
            float env = Mathf.Exp(-2.2f * t) * Mathf.Min(1f, t / 0.004f) * Mathf.Min(1f, Mathf.Max(0f, length - t) / 0.05f);
            return Mathf.Sin(2f * Mathf.PI * f * t + modulator) * env;
        }

        private static float Bass(float f, float t, float length)
        {
            float env = Mathf.Min(1f, t / 0.006f) * Mathf.Exp(-3f * t) * Mathf.Min(1f, Mathf.Max(0f, length - t) / 0.04f);
            return (Mathf.Sin(2f * Mathf.PI * f * t) + 0.3f * Mathf.Sin(4f * Mathf.PI * f * t)) * env;
        }

        private static void AddNote(float[] s, float start, float length, float frequency, float level, Voice voice)
        {
            int from = Samples(start);
            int count = Samples(length);
            for (int k = 0; k < count; k++)
            {
                int i = (from + k) % s.Length; // wraps: a note ringing past the end carries on at the start - a seamless loop
                s[i] += voice(frequency, k / (float)SampleRate, length) * level;
            }
        }

        private static void AddDrum(float[] s, float start, float level, float pitch, float decay)
        {
            if (level <= 0f)
            {
                return;
            }

            int from = Samples(start);
            int count = Samples(0.3f);
            for (int k = 0; k < count; k++)
            {
                float t = k / (float)SampleRate;
                float sweep = pitch * (1f + 1.5f * Mathf.Exp(-40f * t)); // the skin's pitch drops as it rings
                s[(from + k) % s.Length] += Mathf.Sin(2f * Mathf.PI * sweep * t) * Mathf.Exp(-decay * t) * level;
            }
        }

        private static void AddNoise(float[] s, float start, float seconds, float level, float decay, float brightness)
        {
            var random = new System.Random(Samples(start));
            int from = Samples(start);
            int count = Samples(seconds);
            float low = 0f;
            for (int k = 0; k < count; k++)
            {
                float t = k / (float)SampleRate;
                float white = Noise(random);
                low += (1f - brightness) * (white - low);
                s[(from + k) % s.Length] += (white - low * brightness) * Mathf.Exp(-decay * t) * level;
            }
        }

        // ------------------------------------------------------------------
        // The speaker, and plumbing
        // ------------------------------------------------------------------

        /// <summary>
        /// A small kitchen radio's speaker: no real bass (high-pass ~200 Hz), no air (low-pass ~4 kHz), and a little
        /// soft clipping when it is pushed. Run twice round the loop so the filters' state matches at the seam.
        /// </summary>
        public static float[] SmallSpeaker(float[] samples)
        {
            if (samples.Length == 0)
            {
                return samples;
            }

            float hpA = Mathf.Exp(-2f * Mathf.PI * 200f / SampleRate);
            float lpA = 1f - Mathf.Exp(-2f * Mathf.PI * 4000f / SampleRate);
            float[] output = new float[samples.Length];
            float prevIn = 0f, prevHp = 0f, lp = 0f;
            for (int pass = 0; pass < 2; pass++)
            {
                for (int i = 0; i < samples.Length; i++)
                {
                    float x = samples[i];
                    float hp = hpA * (prevHp + x - prevIn);
                    prevIn = x;
                    prevHp = hp;
                    lp += lpA * (hp - lp);
                    output[i] = (float)Math.Tanh(lp * 1.4f) / (float)Math.Tanh(1.4f);
                }
            }

            return Normalize(output, 0.7f);
        }

        private static float Saw(float phase) => 2f * (phase - Mathf.Floor(phase + 0.5f));

        private static float Semitones(int n) => Mathf.Pow(2f, n / 12f);

        /// <summary>The nearest frequency that fits a whole number of cycles into the loop.</summary>
        private static float InLoop(float frequency, float loopSeconds) => Mathf.Max(1f, Mathf.Round(frequency * loopSeconds)) / loopSeconds;

        private static int Samples(float seconds) => Mathf.RoundToInt(seconds * SampleRate);

        private static float Noise(System.Random random) => (float)(random.NextDouble() * 2.0 - 1.0);

        private static float[] Normalize(float[] samples, float peak)
        {
            float max = 0f;
            foreach (float v in samples)
            {
                max = Mathf.Max(max, Mathf.Abs(v));
            }

            if (max > 0f)
            {
                float scale = peak / max;
                for (int i = 0; i < samples.Length; i++)
                {
                    samples[i] *= scale;
                }
            }

            return samples;
        }
    }
}
