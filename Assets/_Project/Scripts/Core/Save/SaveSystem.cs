using System;
using System.IO;
using UnityEngine;
using Zmywak.Core.Diagnostics;

namespace Zmywak.Core.Save
{
    /// <summary>
    /// The game's saves: <see cref="SlotCount"/> slots, each a JSON file in persistentDataPath, written so that a
    /// phone killing the app at any moment cannot lose progress: the new state goes to a temporary file first and
    /// then replaces the save in one step, keeping the previous good save as a backup. Loading falls back to that
    /// backup when the main file is missing, empty or unreadable, and upgrades older formats (see
    /// <see cref="SaveMigrations"/>). Everything reads and writes the <see cref="Slot"/> in play; the main menu
    /// picks it (and remembers it for the next start). The first slot keeps the old single save's file name.
    /// </summary>
    public static class SaveSystem
    {
        public const int SlotCount = 3;

        private const string FileName = "zmywak_save.json";
        private const string TempSuffix = ".tmp";
        private const string BackupSuffix = ".bak";
        /// <summary>The save of a newer game, kept once beside the file an older game is about to overwrite (see <see cref="SwapIn"/>).</summary>
        private const string NewerSuffix = ".newer";
        private const string SlotKey = "zmywak_save_slot";

        private static int? _slot;

        /// <summary>Folder the save file lives in; null means Application.persistentDataPath. Tests point
        /// this at a temp folder so they never read or overwrite the developer's real save.</summary>
        public static string DirectoryOverride { get; set; }

        /// <summary>The slot in play (0-based). Remembered between starts - except under a test's
        /// <see cref="DirectoryOverride"/>, where it stays in memory.</summary>
        public static int Slot
        {
            get
            {
                if (_slot == null)
                {
                    _slot = DirectoryOverride == null ? Mathf.Clamp(PlayerPrefs.GetInt(SlotKey, 0), 0, SlotCount - 1) : 0;
                }

                return _slot.Value;
            }
            set
            {
                LifeGeneration++; // whatever life the running game holds in memory is no longer the one in this slot
                _slot = Mathf.Clamp(value, 0, SlotCount - 1);
                if (DirectoryOverride == null)
                {
                    PlayerPrefs.SetInt(SlotKey, _slot.Value);
                    PlayerPrefs.Save();
                }
            }
        }

        /// <summary>
        /// Goes up whenever the slot in play stops being the life the running game holds in memory: another slot is picked
        /// (<see cref="Slot"/>), or the one in play is deleted (<see cref="DeleteSlot"/>). The persistent controllers that keep a
        /// life in memory note it when they start and save only while it is unchanged - otherwise the app going to the background
        /// would write the old life straight back into the slot just deleted (or into the one just picked). A restart into a
        /// slot makes them afresh, with the new number.
        /// </summary>
        public static int LifeGeneration { get; private set; }

        public static string FilePath => PathOf(Slot);
        public static string BackupPath => FilePath + BackupSuffix;

        /// <summary>A slot's file: the first one the old single save's, the others numbered from 2.</summary>
        public static string PathOf(int slot)
        {
            string name = slot <= 0 ? FileName : $"zmywak_save_{slot + 1}.json";
            return Path.Combine(DirectoryOverride ?? Application.persistentDataPath, name);
        }

        /// <summary>Forgets the slot in play (a test sandbox starting over), so the next read takes it afresh.</summary>
        public static void ResetSlot()
        {
            _slot = null;
        }

        /// <summary>
        /// Swap the new save in with <see cref="File.Replace"/> (one step, so a kill cannot leave half a save). Where
        /// that is refused - it has not been tried on a phone's IL2CPP runtime - the plain copy below does the same
        /// job in two steps, which the backup still covers. Tests turn this off to exercise the copy.
        /// </summary>
        public static bool PreferAtomicReplace { get; set; } = true;

#if UNITY_EDITOR
        /// <summary>Editor-only crash injection at IO boundaries. Throwing a non-IO exception simulates process death;
        /// tests recreate the controllers from disk afterwards. Never present in a player build.</summary>
        public static Action<string> WriteBoundaryForTests { get; set; }
#endif

        private static void WriteBoundary(string stage)
        {
#if UNITY_EDITOR
            WriteBoundaryForTests?.Invoke(stage);
#endif
        }

        /// <summary>
        /// Writes the slot in play. Never throws: a phone with a full disk or a folder it may not write to cannot be
        /// allowed to break the moment that saves (the app going to the background, a shift paid out). It says so in
        /// the log and returns false; the previous save and its backup stay as they were.
        /// </summary>
        public static bool Save(SaveData data)
        {
            using var _write = StartupTimeline.Measure("save: write");
            // A save from a newer game keeps its version: relabelling it would have that game migrate it again on top of itself.
            data.version = Math.Max(data.version, SaveMigrations.CurrentVersion);
            SaveValidation.MakeWritable(data);
            string json = JsonUtility.ToJson(data, true);
            string path = FilePath;
            string temp = path + TempSuffix;

            // Write the whole new state beside the save, then swap it in. A kill mid-write leaves a broken
            // .tmp and the old save untouched; a kill mid-swap still leaves the backup.
            try
            {
                WriteBoundary("before-temp");
                File.WriteAllText(temp, json);
                WriteBoundary("after-temp");
                SwapIn(temp, path, path + BackupSuffix);
                WriteBoundary("after-commit");
                return true;
            }
            catch (Exception e) when (IsDiskProblem(e))
            {
                Debug.LogWarning($"SaveSystem: could not save ({e.GetType().Name}: {e.Message}).");
                TryDelete(temp);
                return false;
            }
        }

        private static void SwapIn(string temp, string path, string backup)
        {
            if (!File.Exists(path))
            {
                File.Move(temp, path);
                return;
            }

            // A damaged main file (the game was loaded from the backup) must never become the backup: that would put a
            // broken save over the only good one. The new save goes over the damaged file and the backup stays as it is.
            SaveData existing = TryRead(path);
            if (existing == null)
            {
                File.Copy(temp, path, true);
                WriteBoundary("after-main-copy");
                TryDelete(temp); // a cleanup failure must not report a committed snapshot as failed
                return;
            }

            if (existing.version > SaveMigrations.CurrentVersion)
            {
                KeepNewerGamesSave(path); // this game cannot write what a newer one added: the original is kept, once
            }

            if (PreferAtomicReplace)
            {
                try
                {
                    File.Replace(temp, path, backup);
                    return;
                }
                catch (Exception e) when (IsDiskProblem(e) || e is NotImplementedException)
                {
                    Debug.LogWarning($"SaveSystem: File.Replace was refused ({e.GetType().Name}); copying over instead.");
                }
            }

            // The previous good save first, then the new one over the main file; a kill in between leaves a damaged
            // main file and the backup, which Load falls back to.
            File.Copy(path, backup, true);
            WriteBoundary("after-backup-copy");
            File.Copy(temp, path, true);
            WriteBoundary("after-main-copy");
            TryDelete(temp);
        }

        /// <summary>
        /// The file is from a newer game and this one is about to save over it: JsonUtility drops the fields it does not know, so the
        /// newer game's own file is copied aside first (once - never over the first copy), for that game to find again. Not being
        /// able to copy must not stop the save.
        /// </summary>
        private static void KeepNewerGamesSave(string path)
        {
            string kept = path + NewerSuffix;
            try
            {
                if (!File.Exists(kept))
                {
                    File.Copy(path, kept);
                    Debug.LogWarning($"SaveSystem: {Path.GetFileName(path)} is from a newer version of the game; its original is kept as {Path.GetFileName(kept)}.");
                }
            }
            catch (Exception e) when (IsDiskProblem(e))
            {
                Debug.LogWarning($"SaveSystem: could not keep the newer game's save ({e.Message}).");
            }
        }

        private static bool IsDiskProblem(Exception e) =>
            e is IOException || e is UnauthorizedAccessException || e is NotSupportedException || e is System.Security.SecurityException;

        private static void TryDelete(string file)
        {
            try
            {
                if (File.Exists(file))
                {
                    File.Delete(file);
                }
            }
            catch (Exception e) when (IsDiskProblem(e))
            {
                // Nothing more to do: it is only a leftover, and the next save writes over it.
            }
        }

        public static SaveData Load() => Peek(Slot);

        /// <summary>Reads any slot, without switching to it (the main menu's list of saves); null for an empty one.</summary>
        public static SaveData Peek(int slot)
        {
            using var _read = StartupTimeline.Measure("save: read");
            string path = PathOf(slot);
            SaveData data = TryRead(path);
            if (data == null && File.Exists(path + BackupSuffix))
            {
                data = TryRead(path + BackupSuffix);
                if (data != null)
                {
                    Debug.LogWarning("SaveSystem: the save file was missing or damaged - restored the last good backup.");
                }
            }

            if (data == null)
            {
                return null;
            }

            data = SaveMigrations.Upgrade(data);
            SaveValidation.Repair(data);
            return data;
        }

        public static bool HasSave() => HasSave(Slot);

        public static bool HasSave(int slot)
        {
            string path = PathOf(slot);
            return File.Exists(path) || File.Exists(path + BackupSuffix);
        }

        public static bool HasReadableSave() => HasReadableSave(Slot);

        /// <summary>
        /// Whether the slot holds a save that can be read - what the menu goes by: files that are only wreckage (<see cref="HasSave"/>
        /// still says they are there) are no life to continue, and the slot is a free one for a new game.
        /// </summary>
        public static bool HasReadableSave(int slot) => HasSave(slot) && Peek(slot) != null;

        public static void DeleteSave() => DeleteSlot(Slot);

        /// <summary>Empties a slot for good: its save, the backup and any half-written file.</summary>
        public static void DeleteSlot(int slot)
        {
            if (slot == Slot)
            {
                LifeGeneration++; // the life in memory (if any) must not save itself back over the empty slot
            }

            string path = PathOf(slot);
            foreach (string file in new[] { path, path + BackupSuffix, path + TempSuffix, path + NewerSuffix })
            {
                TryDelete(file); // a locked or vanished file must not throw out of the main menu's delete button
            }
        }

        private static SaveData TryRead(string path)
        {
            if (!File.Exists(path))
            {
                return null;
            }

            try
            {
                string json = File.ReadAllText(path);
                SaveData data = string.IsNullOrWhiteSpace(json) ? null : JsonUtility.FromJson<SaveData>(json);
                if (data != null && !SaveValidation.IsReadable(data, out string problem))
                {
                    Debug.LogWarning($"SaveSystem: {Path.GetFileName(path)} is not a usable save ({problem}).");
                    return null;
                }

                return data;
            }
            catch (Exception e) when (e is ArgumentException || IsDiskProblem(e))
            {
                Debug.LogWarning($"SaveSystem: could not read {Path.GetFileName(path)} ({e.Message}).");
                return null;
            }
        }
    }
}
