using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Zmywak.Kitchen.Feedback;
using Sound = Zmywak.Kitchen.Feedback.ProceduralSounds.Sound;

namespace Zmywak.EditorTools
{
    /// <summary>
    /// The kitchen's recorded sounds. <see cref="Bake"/> turns CC0 recordings (Kenney and rubberduck packs - see
    /// CREDITS.md) into the game's clips: mono 44.1 kHz WAVs in <see cref="Folder"/>, each trimmed, faded, levelled to
    /// its synthesised counterpart (peak for one-shots, loudness for loops) and, for loops, crossfaded so the seam does
    /// not tick. The recipe below is the record of what came from where. <see cref="EnsureBank"/> (part of Run All)
    /// sets the clips' import settings and gathers them into the <see cref="KitchenSoundBank"/> the game plays from.
    /// </summary>
    public static class RecordedSounds
    {
        public const string Folder = "Assets/_Project/Audio/Kitchen";
        public const string BankPath = "Assets/_Project/Resources/" + KitchenSoundBank.ResourceName + ".asset";

        /// <summary>Where the unpacked CC0 packs go for a bake, one folder per pack (not kept in the project).</summary>
        public const string SourceFolder = "Assets/_Audition";

        public const int Rate = 44100;

        private sealed class Take
        {
            public Sound Sound;
            public string[] Sources;
            public float Speed = 1f; // played faster - and so higher - than recorded
            public float Skip;       // loops: seconds left out at the start
            public float Length;     // loops: seconds kept (0 = all)
            public float Seam;       // loops: crossfade where the end runs into the start
        }

        private static Take One(Sound sound, string source, float speed = 1f) =>
            new Take { Sound = sound, Sources = new[] { source }, Speed = speed };

        private static Take Loop(Sound sound, float seam, float skip, float length, params string[] sources) =>
            new Take { Sound = sound, Sources = sources, Seam = seam, Skip = skip, Length = length };

        private static readonly Take[] Recipe =
        {
            // The tap: a steady stream (rubberduck, 100 CC0 SFX #2).
            Loop(Sound.Water, 0.25f, 0.5f, 4f, "sfx_100_v2/sfx100v2_loop_water_01.ogg"),
            // The sponge: three strokes of rubbing, one after another (rubberduck, 100 CC0 SFX - paper).
            Loop(Sound.ScrubSoft, 0.06f, 0f, 0f, "100-CC0-SFX_0/paper_02.ogg", "100-CC0-SFX_0/paper_04.ogg", "100-CC0-SFX_0/paper_03.ogg"),
            // Steel wool: a rasp, stroke after stroke (rubberduck, 30 CC0 SFX loops - saw).
            Loop(Sound.ScrubHarsh, 0.08f, 0f, 0f, "sfx_loops/saw.ogg"),
            // The dishwasher running: a low machine drone (rubberduck, 100 CC0 SFX #2).
            Loop(Sound.Hum, 0.5f, 0.3f, 4f, "sfx_100_v2/sfx100v2_loop_machine_01.ogg"),

            One(Sound.Squeak, "kenney_rpg-audio/creak3.ogg", 1.25f),
            One(Sound.Squeak, "kenney_rpg-audio/creak1.ogg", 1.25f),
            One(Sound.Clink, "kenney_impact-sounds/impactPlate_light_000.ogg"),
            One(Sound.Clink, "kenney_impact-sounds/impactPlate_light_001.ogg"),
            One(Sound.Clink, "kenney_impact-sounds/impactPlate_light_002.ogg"),
            One(Sound.Clink, "kenney_impact-sounds/impactPlate_light_003.ogg"),
            One(Sound.Clink, "kenney_impact-sounds/impactPlate_light_004.ogg"),
            One(Sound.GlassClink, "kenney_impact-sounds/impactGlass_light_000.ogg"),
            One(Sound.GlassClink, "kenney_impact-sounds/impactGlass_light_001.ogg"),
            One(Sound.GlassClink, "kenney_impact-sounds/impactGlass_light_002.ogg"),
            One(Sound.GlassClink, "kenney_impact-sounds/impactGlass_light_003.ogg"),
            One(Sound.GlassClink, "kenney_impact-sounds/impactGlass_light_004.ogg"),
            One(Sound.Crash, "100-CC0-SFX_0/glass_01.ogg"),
            One(Sound.Crash, "100-CC0-SFX_0/glass_02.ogg"),
            One(Sound.Crash, "sfx_100_v2/sfx100v2_glass_02.ogg"),
            One(Sound.Crash, "sfx_100_v2/sfx100v2_glass_03.ogg"),
            One(Sound.Scrape, "100-CC0-SFX_0/tools_01.ogg"),
            One(Sound.Scrape, "100-CC0-SFX_0/tools_04.ogg"),
            One(Sound.Pickup, "kenney_impact-sounds/impactGeneric_light_000.ogg"),
            One(Sound.Pickup, "kenney_impact-sounds/impactGeneric_light_001.ogg"),
            One(Sound.Pickup, "kenney_impact-sounds/impactGeneric_light_002.ogg"),
            One(Sound.Ding, "100-CC0-SFX_0/bell_02.ogg"),
            One(Sound.Fault, "kenney_interface-sounds/error_006.ogg"),
            One(Sound.PowerUp, "kenney_interface-sounds/maximize_008.ogg"),
            One(Sound.Clang, "100-CC0-SFX_0/metal_05.ogg"),
            One(Sound.Clang, "100-CC0-SFX_0/metal_08.ogg"),
            One(Sound.Clang, "100-CC0-SFX_0/metal_09.ogg"),
            One(Sound.Chop, "kenney_rpg-audio/chop.ogg"),
            One(Sound.Chop, "kenney_impact-sounds/impactWood_light_000.ogg"),
            One(Sound.Chop, "kenney_impact-sounds/impactWood_light_002.ogg"),
            One(Sound.Splash, "water-splash-slime-sfx/splash_09.ogg"),
            One(Sound.Splash, "water-splash-slime-sfx/splash_10.ogg"),
            One(Sound.Click, "100-CC0-SFX_0/switch_01.ogg")
        };

        /// <summary>The source of each baked file, for CREDITS.md: file name to "pack/file".</summary>
        public static IEnumerable<KeyValuePair<string, string>> Sources()
        {
            var counts = new Dictionary<Sound, int>();
            foreach (Take take in Recipe)
            {
                counts[take.Sound] = counts.TryGetValue(take.Sound, out int n) ? n + 1 : 1;
                yield return new KeyValuePair<string, string>($"{take.Sound}_{counts[take.Sound]}.wav", string.Join(" + ", take.Sources));
            }
        }

        [MenuItem("Zmywak/Audio/Bake Recorded Sounds")]
        public static void Bake()
        {
            if (!AssetDatabase.IsValidFolder(SourceFolder))
            {
                Debug.LogError($"Zmywak: unpack the CC0 packs (CREDITS.md) into {SourceFolder}/<pack> first.");
                return;
            }

            Directory.CreateDirectory(Folder);
            var counts = new Dictionary<Sound, int>();
            foreach (Take take in Recipe)
            {
                counts[take.Sound] = counts.TryGetValue(take.Sound, out int n) ? n + 1 : 1;
                float[] samples = ProceduralSounds.IsLoop(take.Sound) ? BakeLoop(take) : BakeOneShot(take);
                WriteWav($"{Folder}/{take.Sound}_{counts[take.Sound]}.wav", samples, Rate);
            }

            AssetDatabase.Refresh();
            EnsureBank();
            Debug.Log("Zmywak: recorded sounds baked");
        }

        [MenuItem("Zmywak/Setup/0e. Kitchen Sound Bank")]
        public static KitchenSoundBank EnsureBank()
        {
            var bank = AssetDatabase.LoadAssetAtPath<KitchenSoundBank>(BankPath);
            if (bank == null)
            {
                bank = ScriptableObject.CreateInstance<KitchenSoundBank>();
                AssetDatabase.CreateAsset(bank, BankPath);
            }

            var takes = new Dictionary<Sound, List<AudioClip>>();
            if (AssetDatabase.IsValidFolder(Folder))
            {
                foreach (string path in AssetDatabase.FindAssets("t:AudioClip", new[] { Folder }).Select(AssetDatabase.GUIDToAssetPath).OrderBy(p => p))
                {
                    string name = Path.GetFileNameWithoutExtension(path);
                    int cut = name.LastIndexOf('_');
                    if (cut <= 0 || !Enum.TryParse(name.Substring(0, cut), out Sound sound))
                    {
                        continue;
                    }

                    Configure(path);
                    if (!takes.TryGetValue(sound, out List<AudioClip> list))
                    {
                        takes[sound] = list = new List<AudioClip>();
                    }

                    list.Add(AssetDatabase.LoadAssetAtPath<AudioClip>(path));
                }
            }

            foreach (Sound sound in Enum.GetValues(typeof(Sound)))
            {
                bank.SetTakes(sound, takes.TryGetValue(sound, out List<AudioClip> list) ? list : new List<AudioClip>());
            }

            EditorUtility.SetDirty(bank);
            AssetDatabase.SaveAssets();
            return bank;
        }

        /// <summary>Small clips, played often and at once: decompressed when the scene loads, Vorbis on disk.</summary>
        internal static void Configure(string path)
        {
            var importer = (AudioImporter)AssetImporter.GetAtPath(path);
            AudioImporterSampleSettings settings = importer.defaultSampleSettings;
            bool changed = !importer.forceToMono || importer.loadInBackground ||
                           settings.loadType != AudioClipLoadType.DecompressOnLoad ||
                           settings.compressionFormat != AudioCompressionFormat.Vorbis ||
                           Mathf.Abs(settings.quality - 0.6f) > 1e-3f;
            if (!changed)
            {
                return;
            }

            importer.forceToMono = true;
            importer.loadInBackground = false;
            settings.loadType = AudioClipLoadType.DecompressOnLoad;
            settings.compressionFormat = AudioCompressionFormat.Vorbis;
            settings.quality = 0.6f;
            importer.defaultSampleSettings = settings;
            importer.SaveAndReimport();
        }

        // ------------------------------------------------------------------
        // Processing
        // ------------------------------------------------------------------

        private static float[] BakeOneShot(Take take)
        {
            float[] x = Resample(LoadMono(take.Sources[0], out int hz), hz, take.Speed);
            x = Trim(x);
            Fade(x, Ms(2), Ms(15));
            float[] reference = ProceduralSounds.Generate(take.Sound);
            return Scale(x, Peak(reference) / Mathf.Max(Peak(x), 1e-6f));
        }

        private static float[] BakeLoop(Take take)
        {
            int seam = Ms(take.Seam * 1000f);
            float[] raw;
            if (take.Sources.Length == 1)
            {
                float[] x = Resample(LoadMono(take.Sources[0], out int hz), hz, take.Speed);
                int skip = Mathf.Min(Ms(take.Skip * 1000f), x.Length / 4);
                int keep = take.Length > 0f ? Mathf.Min(Ms(take.Length * 1000f) + seam, x.Length - skip) : x.Length - skip;
                raw = new float[keep];
                Array.Copy(x, skip, raw, 0, keep);
            }
            else
            {
                // Strokes joined end to start with a short overlap, so the rubbing never stops between them.
                raw = new float[0];
                foreach (string source in take.Sources)
                {
                    float[] stroke = Trim(Resample(LoadMono(source, out int hz), hz, take.Speed));
                    raw = Join(raw, stroke, Ms(20));
                }
            }

            float[] loop = CrossfadeLoop(raw, seam);
            float[] reference = ProceduralSounds.Generate(take.Sound);
            float scale = Mathf.Min(Rms(reference) / Mathf.Max(Rms(loop), 1e-6f), 0.95f / Mathf.Max(Peak(loop), 1e-6f));
            return Scale(loop, scale);
        }

        internal static float[] LoadMono(string source, out int hz)
        {
            string path = $"{SourceFolder}/{source}";
            var importer = (AudioImporter)AssetImporter.GetAtPath(path);
            if (importer == null)
            {
                throw new FileNotFoundException($"Zmywak: no recording at {path}");
            }

            AudioImporterSampleSettings settings = importer.defaultSampleSettings;
            if (settings.loadType != AudioClipLoadType.DecompressOnLoad)
            {
                settings.loadType = AudioClipLoadType.DecompressOnLoad;
                importer.defaultSampleSettings = settings;
                importer.SaveAndReimport();
            }

            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            clip.LoadAudioData();
            hz = clip.frequency;
            int channels = clip.channels;
            var data = new float[clip.samples * channels];
            clip.GetData(data, 0);
            var mono = new float[clip.samples];
            for (int i = 0; i < mono.Length; i++)
            {
                float sum = 0f;
                for (int c = 0; c < channels; c++)
                {
                    sum += data[i * channels + c];
                }

                mono[i] = sum / channels;
            }

            return mono;
        }

        /// <summary>To <see cref="Rate"/>, <paramref name="speed"/> times faster (linear interpolation).</summary>
        internal static float[] Resample(float[] x, int hz, float speed)
        {
            double step = speed * hz / (double)Rate;
            var y = new float[Mathf.Max(1, (int)((x.Length - 1) / step))];
            for (int i = 0; i < y.Length; i++)
            {
                double at = i * step;
                int a = (int)at;
                float f = (float)(at - a);
                y[i] = a + 1 < x.Length ? Mathf.Lerp(x[a], x[a + 1], f) : x[x.Length - 1];
            }

            return y;
        }

        /// <summary>From just before the sound starts to where it has died 50 dB down.</summary>
        internal static float[] Trim(float[] x)
        {
            float peak = Peak(x);
            int start = 0;
            while (start < x.Length && Mathf.Abs(x[start]) < peak * 0.02f)
            {
                start++;
            }

            start = Mathf.Max(0, start - Ms(2));
            int window = Ms(5), end = start;
            for (int w = start; w + window <= x.Length; w += window)
            {
                if (Rms(x, w, window) > peak * 0.003f)
                {
                    end = w + window;
                }
            }

            end = Mathf.Min(x.Length, end + window);
            var y = new float[Mathf.Max(1, end - start)];
            Array.Copy(x, start, y, 0, y.Length);
            return y;
        }

        internal static void Fade(float[] x, int fadeIn, int fadeOut)
        {
            for (int i = 0; i < fadeIn && i < x.Length; i++)
            {
                x[i] *= i / (float)fadeIn;
            }

            for (int i = 0; i < fadeOut && i < x.Length; i++)
            {
                x[x.Length - 1 - i] *= i / (float)fadeOut;
            }
        }

        internal static float[] Join(float[] a, float[] b, int overlap)
        {
            overlap = Mathf.Min(overlap, Mathf.Min(a.Length, b.Length));
            var y = new float[a.Length + b.Length - overlap];
            Array.Copy(a, y, a.Length);
            for (int i = 0; i < b.Length; i++)
            {
                int at = a.Length - overlap + i;
                float t = i < overlap ? i / (float)overlap : 1f;
                y[at] = (i < overlap ? y[at] * Mathf.Cos(t * Mathf.PI / 2f) : 0f) + b[i] * Mathf.Sin(t * Mathf.PI / 2f);
            }

            return y;
        }

        /// <summary>The extra tail is faded into the head (equal power, as for noise), so the end flows into the start.</summary>
        internal static float[] CrossfadeLoop(float[] raw, int seam)
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

        internal static float[] Scale(float[] x, float scale)
        {
            for (int i = 0; i < x.Length; i++)
            {
                x[i] = Mathf.Clamp(x[i] * scale, -1f, 1f);
            }

            return x;
        }

        internal static int Ms(float ms) => Mathf.RoundToInt(ms * Rate / 1000f);

        internal static float Peak(float[] x) => x.Length == 0 ? 0f : x.Max(Mathf.Abs);

        internal static float Rms(float[] x) => Rms(x, 0, x.Length);

        internal static float Rms(float[] x, int start, int count)
        {
            double e = 0;
            for (int i = start; i < start + count && i < x.Length; i++)
            {
                e += x[i] * x[i];
            }

            return Mathf.Sqrt((float)(e / Mathf.Max(1, count)));
        }

        /// <summary>16-bit PCM, mono.</summary>
        internal static void WriteWav(string path, float[] samples, int rate)
        {
            using (var w = new BinaryWriter(File.Create(path)))
            {
                int bytes = samples.Length * 2;
                w.Write("RIFF".ToCharArray());
                w.Write(36 + bytes);
                w.Write("WAVE".ToCharArray());
                w.Write("fmt ".ToCharArray());
                w.Write(16);
                w.Write((short)1);
                w.Write((short)1);
                w.Write(rate);
                w.Write(rate * 2);
                w.Write((short)2);
                w.Write((short)16);
                w.Write("data".ToCharArray());
                w.Write(bytes);
                foreach (float s in samples)
                {
                    w.Write((short)Mathf.RoundToInt(Mathf.Clamp(s, -1f, 1f) * 32767f));
                }
            }
        }
    }
}
