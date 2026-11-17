using System.Collections.Generic;
using UnityEngine;

namespace Zmywak.Core.Save
{
    /// <summary>
    /// Brings a save written by an older version of the game up to the current format, one version at a
    /// time. Add a step here whenever <see cref="SaveData"/> changes in a way old files cannot simply be
    /// read into (renamed or re-purposed fields), and bump <see cref="CurrentVersion"/>.
    /// </summary>
    public static class SaveMigrations
    {
        /// <summary>
        /// 1 - the first saves (no version number in the file).
        /// 2 - story scenes: "intro seen" became the list of seen scenes, plus story flags.
        /// 3 - atomic dialogue consequences and a node/choice checkpoint.
        /// </summary>
        public const int CurrentVersion = 3;

        public static SaveData Upgrade(SaveData data)
        {
            if (data.version <= 0)
            {
                data.version = 1; // written before saves carried a version
            }

            if (data.version > CurrentVersion)
            {
                Debug.LogWarning($"SaveMigrations: save version {data.version} is newer than this game ({CurrentVersion}); loading what it can.");
                return data;
            }

            if (data.version == 1)
            {
                data.storyFlags ??= new List<string>();
                data.seenStoryBeats ??= new List<string>();
                if (data.hasSeenIntro && !data.seenStoryBeats.Contains(ArrivalBeatId))
                {
                    data.seenStoryBeats.Add(ArrivalBeatId);
                }

                data.version = 2;
            }

            if (data.version == 2)
            {
                // Old files cannot tell whether a partially played node's money was paid before a crash.
                // Preserve every existing balance/flag; never invent a payment or debit during migration.
                data.appliedDialogueNodes ??= new List<string>();
                data.dialogueCheckpoint = null;
                data.version = 3;
            }

            return data;
        }

        // Narrative's id for the Chapter 1 opening (Core cannot reference Narrative); the one intro there was.
        private const string ArrivalBeatId = "ch1_arrival";
    }
}
