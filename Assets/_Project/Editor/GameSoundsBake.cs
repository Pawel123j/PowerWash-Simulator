using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Zmywak.Core.Audio;

namespace Zmywak.EditorTools
{
    /// <summary>
    /// The game's own sounds (<see cref="GameSound"/>) as clips. <see cref="Bake"/> makes them once: CC0 recordings from
    /// the same Kenney and rubberduck packs as the kitchen's (CREDITS.md) - the interface's clicks, a page turning, coins,
    /// the kitchen's bell, the rain on the window, the city's traffic, the building's creaks, doors and feet - trimmed,
    /// levelled and, for the distant ones, muffled as if through a wall; and the jingles and the menu's music
    /// synthesised (<see cref="GameSynth"/>), so they are files like the rest. <see cref="EnsureBank"/> (part of Run All)
    /// gathers them into the <see cref="GameSoundBank"/> the game plays from.
    /// </summary>
    public static class GameSoundsBake
    {
        public const string Folder = "Assets/_Project/Audio/Game";
        public const string BankPath = "Assets/_Project/Resources/" + GameSoundBank.ResourceName + ".asset";

        private const int Rate = RecordedSounds.Rate;

        private sealed class Take
        {
            public GameSound Sound;
            public string[] Sources;   // empty: synthesised
            public float Speed = 1f;
            public float Peak = 0.5f;  // one-shots: their loudest point
            public float Rms;          // loops: their loudness instead
            public float LowpassHz;    // 0 = as recorded; else muffled, as through a wall or a window
            public float Skip;         // loops: seconds left out at the start
            public float Length;       // loops: seconds kept (0 = all)
            public float Seam = 0.5f;  // loops: crossfade
            public float Spacing;      // several sources: each starts this long after the last (0 = joined end to start)
        }

        private static Take Rec(GameSound sound, string source, float peak, float speed = 1f, float lowpass = 0f) =>
            new Take { Sound = sound, Sources = new[] { source }, Peak = peak, Speed = speed, LowpassHz = lowpass };

        private static Take Synth(GameSound sound) => new Take { Sound = sound, Sources = new string[0] };

        private static readonly Take[] Recipe =
        {
            // The interface (Kenney, Interface Sounds).
            Rec(GameSound.UiClick, "kenney_interface-sounds/click_002.ogg", 0.45f),
            Rec(GameSound.UiClick, "kenney_interface-sounds/click_003.ogg", 0.45f),
            Rec(GameSound.UiConfirm, "kenney_interface-sounds/confirmation_002.ogg", 0.5f),
            Rec(GameSound.UiBack, "kenney_interface-sounds/back_002.ogg", 0.45f),
            Rec(GameSound.UiError, "kenney_interface-sounds/error_003.ogg", 0.4f),
            Rec(GameSound.UiToggle, "kenney_interface-sounds/toggle_002.ogg", 0.45f),
            Rec(GameSound.UiTick, "kenney_interface-sounds/tick_002.ogg", 0.35f),
            Rec(GameSound.UiOpen, "kenney_interface-sounds/maximize_003.ogg", 0.35f),
            // The dialogue: a page turned (Kenney, RPG Audio); the typing blip is synthesised, very soft.
            Rec(GameSound.PageTurn, "kenney_rpg-audio/bookFlip2.ogg", 0.35f),
            Synth(GameSound.DialogueBlip),
            // Money: coins in the hand (Kenney, RPG Audio); the count-up's tick and the jingles are synthesised.
            Rec(GameSound.Reward, "kenney_rpg-audio/handleCoins.ogg", 0.55f),
            Rec(GameSound.Reward, "kenney_rpg-audio/handleCoins2.ogg", 0.55f),
            Synth(GameSound.MoneyTick),
            Synth(GameSound.GradeGood),
            Synth(GameSound.GradePoor),
            Synth(GameSound.Promotion),
            Synth(GameSound.FlowChime),
            Synth(GameSound.FullRack),
            Synth(GameSound.WrongTool),
            // Closing time: the pass's bell (rubberduck, 100 CC0 SFX).
            Rec(GameSound.ShiftEnd, "100-CC0-SFX_0/bell_01.ogg", 0.6f),
            // The menu's music.
            Synth(GameSound.MenuMusic),
            // The room: rain on the glass (rubberduck, CC0 loops), the city's traffic through a closed window (rubberduck,
            // 100 CC0 SFX #2 - a highway, muffled), the room's own hum (synthesised).
            new Take { Sound = GameSound.AmbRain, Sources = new[] { "sfx_loops/rain.ogg" }, Rms = 0.12f, LowpassHz = 6000f, Seam = 0.6f },
            new Take { Sound = GameSound.AmbCity, Sources = new[] { "sfx_100_v2/sfx100v2_loop_highway.ogg" }, Rms = 0.1f, LowpassHz = 600f, Seam = 0.8f },
            Synth(GameSound.AmbRoom),
            // The building: a floorboard (Kenney, RPG Audio, slowed), a door down the hall and feet upstairs (Kenney), thunder
            // (rubberduck) - all through walls.
            Rec(GameSound.Creak, "kenney_rpg-audio/creak2.ogg", 0.35f, 0.8f, 2500f),
            Rec(GameSound.Door, "kenney_rpg-audio/doorClose_1.ogg", 0.4f, 1f, 900f),
            Rec(GameSound.Door, "kenney_rpg-audio/doorClose_3.ogg", 0.4f, 1f, 900f),
            new Take
            {
                Sound = GameSound.Footsteps, Peak = 0.4f, LowpassHz = 500f, Spacing = 0.55f,
                Sources = new[] { "kenney_impact-sounds/footstep_wood_000.ogg", "kenney_impact-sounds/footstep_wood_001.ogg", "kenney_impact-sounds/footstep_wood_002.ogg" }
            },
            Rec(GameSound.Thunder, "sfx_100_v2/sfx100v2_thunder_01.ogg", 0.6f, 1f, 1500f)
        };

        /// <summary>The source of each baked file, for CREDITS.md: file name to "pack/file" (or "synthesised").</summary>
        public static IEnumerable<KeyValuePair<string, string>> Sources()
        {
            var counts = new Dictionary<GameSound, int>();
            foreach (Take take in Recipe)
            {
                counts[take.Sound] = counts.TryGetValue(take.Sound, out int n) ? n + 1 : 1;
                yield return new KeyValuePair<string, string>($"{take.Sound}_{counts[take.Sound]}.wav",
                    take.Sources.Length == 0 ? "synthesised (GameSynth)" : string.Join(" + ", take.Sources));
            }
        }

        [MenuItem("Zmywak/Audio/Bake Game Sounds")]
        public static void Bake()
        {
            bool haveRecordings = AssetDatabase.IsValidFolder(RecordedSounds.SourceFolder);
            if (!haveRecordings)
            {
                Debug.LogWarning($"Zmywak: no CC0 packs in {RecordedSounds.SourceFolder} - baking the synthesised sounds only.");
            }

            Directory.CreateDirectory(Folder);
            var counts = new Dictionary<GameSound, int>();
            foreach (Take take in Recipe)
            {
                counts[take.Sound] = counts.TryGetValue(take.Sound, out int n) ? n + 1 : 1;
                string path = $"{Folder}/{take.Sound}_{counts[take.Sound]}.wav";
                if (take.Sources.Length == 0)
                {
                    float[] synth = GameSynth.Generate(take.Sound);
                    RecordedSounds.WriteWav(path, synth, GameSynth.SampleRate);
                }
                else if (haveRecordings)
                {
                    RecordedSounds.WriteWav(path, GameSounds.IsLoop(take.Sound) ? BakeLoop(take) : BakeOneShot(take), Rate);
                }
            }

            AssetDatabase.Refresh();
            EnsureBank();
            Debug.Log("Zmywak: game sounds baked");
        }

        [MenuItem("Zmywak/Setup/0f. Game Sound Bank")]
        public static GameSoundBank EnsureBank()
        {
            var bank = AssetDatabase.LoadAssetAtPath<GameSoundBank>(BankPath);
            if (bank == null)
            {
                bank = ScriptableObject.CreateInstance<GameSoundBank>();
                AssetDatabase.CreateAsset(bank, BankPath);
            }

            var takes = new Dictionary<GameSound, List<AudioClip>>();
            if (AssetDatabase.IsValidFolder(Folder))
            {
                foreach (string path in AssetDatabase.FindAssets("t:AudioClip", new[] { Folder }).Select(AssetDatabase.GUIDToAssetPath).OrderBy(p => p))
                {
                    string name = Path.GetFileNameWithoutExtension(path);
                    int cut = name.LastIndexOf('_');
                    if (cut <= 0 || !Enum.TryParse(name.Substring(0, cut), out GameSound sound))
                    {
                        continue;
                    }

                    Configure(path, GameSounds.IsLoop(sound));
                    if (!takes.TryGetValue(sound, out List<AudioClip> list))
                    {
                        takes[sound] = list = new List<AudioClip>();
                    }

                    list.Add(AssetDatabase.LoadAssetAtPath<AudioClip>(path));
                }
            }

            foreach (GameSound sound in Enum.GetValues(typeof(GameSound)))
            {
                bank.SetTakes(sound, takes.TryGetValue(sound, out List<AudioClip> list) ? list : new List<AudioClip>());
            }

            EditorUtility.SetDirty(bank);
            AssetDatabase.SaveAssets();
            return bank;
        }

        /// <summary>Short sounds decompressed on load; the long loops (the music, the rain) kept compressed in memory.</summary>
        private static void Configure(string path, bool loop)
        {
            var importer = (AudioImporter)AssetImporter.GetAtPath(path);
            AudioImporterSampleSettings settings = importer.defaultSampleSettings;
            AudioClipLoadType load = loop ? AudioClipLoadType.CompressedInMemory : AudioClipLoadType.DecompressOnLoad;
            bool changed = !importer.forceToMono || importer.loadInBackground != loop || settings.loadType != load ||
                           settings.compressionFormat != AudioCompressionFormat.Vorbis || Mathf.Abs(settings.quality - 0.6f) > 1e-3f;
            if (!changed)
            {
                return;
            }

            importer.forceToMono = true;
            importer.loadInBackground = loop;
            settings.loadType = load;
            settings.compressionFormat = AudioCompressionFormat.Vorbis;
            settings.quality = 0.6f;
            importer.defaultSampleSettings = settings;
            importer.SaveAndReimport();
        }

        private static float[] BakeOneShot(Take take)
        {
            float[] x;
            if (take.Sources.Length == 1)
            {
                x = RecordedSounds.Trim(RecordedSounds.Resample(RecordedSounds.LoadMono(take.Sources[0], out int hz), hz, take.Speed));
            }
            else
            {
                // Steps one after another, each after the last's start by the spacing.
                int spacing = RecordedSounds.Ms(take.Spacing * 1000f);
                var parts = take.Sources.Select(s => RecordedSounds.Trim(RecordedSounds.Resample(RecordedSounds.LoadMono(s, out int hz), hz, take.Speed))).ToList();
                x = new float[spacing * (parts.Count - 1) + parts.Last().Length];
                for (int p = 0; p < parts.Count; p++)
                {
                    for (int i = 0; i < parts[p].Length && p * spacing + i < x.Length; i++)
                    {
                        x[p * spacing + i] += parts[p][i] * (p == 1 ? 0.8f : 1f);
                    }
                }
            }

            if (take.LowpassHz > 0f)
            {
                x = Lowpass(x, take.LowpassHz);
            }

            RecordedSounds.Fade(x, RecordedSounds.Ms(2), RecordedSounds.Ms(20));
            return RecordedSounds.Scale(x, take.Peak / Mathf.Max(RecordedSounds.Peak(x), 1e-6f));
        }

        private static float[] BakeLoop(Take take)
        {
            float[] x = RecordedSounds.Resample(RecordedSounds.LoadMono(take.Sources[0], out int hz), hz, take.Speed);
            if (take.LowpassHz > 0f)
            {
                x = Lowpass(x, take.LowpassHz);
            }

            int seam = RecordedSounds.Ms(take.Seam * 1000f);
            int skip = Mathf.Min(RecordedSounds.Ms(take.Skip * 1000f), x.Length / 4);
            int keep = take.Length > 0f ? Mathf.Min(RecordedSounds.Ms(take.Length * 1000f) + seam, x.Length - skip) : x.Length - skip;
            var raw = new float[keep];
            Array.Copy(x, skip, raw, 0, keep);
            float[] loop = RecordedSounds.CrossfadeLoop(raw, seam);
            float scale = Mathf.Min(take.Rms / Mathf.Max(RecordedSounds.Rms(loop), 1e-6f), 0.95f / Mathf.Max(RecordedSounds.Peak(loop), 1e-6f));
            return RecordedSounds.Scale(loop, scale);
        }

        /// <summary>Two one-pole low-passes in a row: the sound as heard through a wall, a window, a floor.</summary>
        private static float[] Lowpass(float[] x, float cutoffHz)
        {
            float a = 1f - Mathf.Exp(-2f * Mathf.PI * cutoffHz / Rate);
            float y1 = 0f, y2 = 0f;
            var y = new float[x.Length];
            for (int i = 0; i < x.Length; i++)
            {
                y1 += a * (x[i] - y1);
                y2 += a * (y1 - y2);
                y[i] = y2;
            }

            return y;
        }
    }
}
