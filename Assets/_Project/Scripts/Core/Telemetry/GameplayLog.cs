using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Zmywak.Core.Telemetry
{
    /// <summary>
    /// A local diary of how the game is actually played, for balancing: one JSON object per line in
    /// <c>persistentDataPath/gameplay_log.jsonl</c> - finished shifts (<see cref="ShiftLogEntry"/>) and the
    /// life between them (<see cref="LifeLogEntry"/>). It never leaves the device: nothing here touches the
    /// network. Pull it off a test phone with adb (README, section 9). Capped: past <see cref="MaxBytes"/>
    /// the file moves to <c>gameplay_log.old.jsonl</c> (replacing the previous one) and starts afresh.
    /// A failed write is a warning, never an error - the diary must not break the game.
    /// </summary>
    public static class GameplayLog
    {
        public const string FileName = "gameplay_log.jsonl";
        public const string PreviousFileName = "gameplay_log.old.jsonl";
        public const long MaxBytes = 1024 * 1024;

        /// <summary>Tests point this at a throwaway folder (as they do SaveSystem's).</summary>
        public static string DirectoryOverride { get; set; }

        public static string FilePath => Path.Combine(DirectoryOverride ?? Application.persistentDataPath, FileName);
        public static string PreviousFilePath => Path.Combine(DirectoryOverride ?? Application.persistentDataPath, PreviousFileName);

        public static void Append(LogEntry entry)
        {
            entry.time = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss");
            string line = JsonUtility.ToJson(entry) + "\n";
            try
            {
                var file = new FileInfo(FilePath);
                if (file.Exists && file.Length + line.Length > MaxBytes)
                {
                    File.Copy(FilePath, PreviousFilePath, true);
                    File.Delete(FilePath);
                }

                File.AppendAllText(FilePath, line);
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
            {
                Debug.LogWarning($"GameplayLog: could not write {FilePath}: {e.Message}");
            }
        }

        /// <summary>The current file's entries of one type, oldest first (for tests and tools).</summary>
        public static List<T> Read<T>(string type) where T : LogEntry
        {
            var entries = new List<T>();
            if (!File.Exists(FilePath))
            {
                return entries;
            }

            foreach (string line in File.ReadAllLines(FilePath))
            {
                if (line.Contains($"\"type\":\"{type}\""))
                {
                    entries.Add(JsonUtility.FromJson<T>(line));
                }
            }

            return entries;
        }
    }

    [Serializable]
    public class LogEntry
    {
        public string type;
        public string time;
    }

    /// <summary>One finished shift. Seconds are game seconds (a paused shift does not count).</summary>
    [Serializable]
    public class ShiftLogEntry : LogEntry
    {
        public string shiftId;
        public bool relaxedMode;
        public float plannedSeconds;
        public float elapsedSeconds;
        public bool clockedOutEarly;
        public int dishesArrived;
        public int dishesCleaned;
        public int dishesMissed;
        public int dishesBroken;
        public int pileCollapses;
        public int rackDropsMissed;
        public int fullRackLoads;
        public int dishwasherCycles;
        /// <summary>Dirty dishes put into the big half-hour dishwasher, and clean ones taken out of it (in dishesCleaned too).</summary>
        public int bigDishwasherLoaded;
        public int bigDishwasherUnloaded;
        public int chefOrders;
        public int chefOrdersMet;
        public int bestFlowStreak;
        public int glassesHeldToLight;
        public int pauses;
        public float wageEuros;
        public float tipsEuros;
        public float deductionsEuros;

        /// <summary>The quota (for the time worked), the score out of 100 and its grade, and what the quota and the grade paid.</summary>
        public int quota;
        public int score;
        public string grade;
        public float quotaBonusEuros;
        public float gradeBonusEuros;

        /// <summary>The chef's urgent orders and how many were on the rack in time; the share of scrubbing with the right tool.</summary>
        public int urgentOrders;
        public int urgentOrdersMet;
        public float washAccuracy;

        /// <summary>Average seconds a finished dish spent at each step in the sink.</summary>
        public int dishesTimed;
        public float scrapeSeconds;
        public float rinseSeconds;
        public float scrubSeconds;
        public float inspectSeconds;
        public float toRackSeconds;

        /// <summary>Times the machine broke down; each one fixed is in <see cref="repairs"/>.</summary>
        public int breakdowns;
        public List<RepairLogEntry> repairs = new List<RepairLogEntry>();

        /// <summary>Frame rate while playing (not paused): the average and the share of frames slower than 30 fps.</summary>
        public float averageFps;
        public float slowFrameShare;

        public ShiftLogEntry()
        {
            type = "shift";
        }
    }

    [Serializable]
    public class RepairLogEntry
    {
        public string fault;
        public float seconds;
    }

    /// <summary>A soak-test sample (development autoplay): how the game ran over the last stretch.</summary>
    [Serializable]
    public class SoakLogEntry : LogEntry
    {
        public float minute;
        public string scene;
        public float averageFps;
        public float worstFrameMs;
        public float slowFrameShare;
        public float allocatedMB;
        public float reservedMB;
        public float managedMB;
        public int textures;
        public int meshes;
        public float batteryLevel;
        public string batteryStatus;
        public string qualityTier;

        public SoakLogEntry()
        {
            type = "soak";
        }
    }

    /// <summary>Something in the life between shifts: the pay and the bills, a call, the pub, money home, the story.</summary>
    [Serializable]
    public class LifeLogEntry : LogEntry
    {
        public string action;
        public int day;
        /// <summary>What <see cref="amountEuros"/> and <see cref="walletEuros"/> are in: EUR, PLN or AUD (where Kuba lived;
        /// the names are from when it was always euros).</summary>
        public string currency;
        public float amountEuros;
        public float walletEuros;
        public float energy;
        public float homesickness;
        public float integration;
        public float sentHomeEuros;

        public LifeLogEntry()
        {
            type = "life";
        }
    }
}
