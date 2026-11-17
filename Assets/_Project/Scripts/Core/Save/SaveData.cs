using System;
using System.Collections.Generic;

namespace Zmywak.Core.Save
{
    /// <summary>
    /// Everything that survives between play sessions. Flat on purpose - JsonUtility can't do
    /// polymorphism. Multiple persistent controllers share this one file, each reading the current
    /// disk state, touching only their own fields, and writing the whole thing back (see MetaProgressController
    /// and NarrativeProgressController's Save() methods) so nobody clobbers anybody else's progress.
    /// </summary>
    [Serializable]
    public class SaveData
    {
        /// <summary>Format version (see SaveMigrations); 0 in files written before it existed.</summary>
        public int version;
        /// <summary>Where Kuba lives (Core.Data.Place: 0 Dublin, 1 Kraków, 2 the Pilbara).</summary>
        public int place;
        /// <summary>The wallet, in the money of <see cref="place"/> (named from when that was always euros).</summary>
        public float walletEuros;
        public float energy;
        public float homesickness;
        public float integration;
        public int dayNumber;
        public string currentDateIso;
        /// <summary>The day of the last call home / night out (0 = never): once an evening, even across a restart.</summary>
        public int lastCallMomDay;
        public int lastPubDay;
        public float sentHomeEuros;
        public float sentHomePln;
        /// <summary>Weeks in a row ended in debt, and whether that has put Kuba in a hostel (LifeLedger).</summary>
        public int weeksInArrears;
        public bool inHostel;
        /// <summary>Kuba's work at the sink over all his shifts, and the rank it has earned him (Core.Data.Career).</summary>
        public Zmywak.Core.Data.CareerRecord career = new Zmywak.Core.Data.CareerRecord();
        public int rank;
        public List<string> knownVocabulary = new List<string>();
        public List<string> unlockedSlangIds = new List<string>();
        public bool hasSeenIntro;
        public List<string> storyFlags = new List<string>();
        public List<string> seenStoryBeats = new List<string>();
        /// <summary>Stable beat/node keys whose entire consequences were committed in this same snapshot.</summary>
        public List<string> appliedDialogueNodes = new List<string>();
        /// <summary>Resume at a node boundary (or its choices). Null when no conversation is pending.</summary>
        public DialogueCheckpoint dialogueCheckpoint;
        /// <summary>The big dishwasher in the kitchen (LongCycleDishwasher): the ids of the dishes in it, and when it was
        /// started, in UTC ticks (0 = not started). It runs in real time, so it is saved, not just the shift.</summary>
        public List<string> bigDishwasherLoad = new List<string>();
        public long bigDishwasherStartedUtcTicks;
        /// <summary>Dishes taken out of the big dishwasher whose credit (they count as washed, with their tips) is not paid yet.
        /// Written in the same save as the emptied machine, and cleared only in the save that pays the shift: a kill in between
        /// leaves the credit here for the next shift to carry, instead of losing the half hour's wait.</summary>
        public int bigDishwasherPendingDishes;
        /// <summary>The dishwasher's story so far (Core.Data.ShiftSchedule): Kuba has met - and fixed - a breakdown, and whether
        /// the machine broke down on the last shift (the next is spared another, mostly).</summary>
        public bool breakdownTutorialDone;
        public bool lastShiftBrokeDown;
        /// <summary>The evening's plan (Meta.Progress.EveningActivity) and the day it was made for: one plan an evening.</summary>
        public int lastEveningDay;
        public int lastEveningActivity;
        /// <summary>What Kuba has bought in the evenings (Meta.Progress.Purchase names), once each.</summary>
        public List<string> purchases = new List<string>();
        /// <summary>The kitchen's guided first plate and first-time cards already shown (Core.Save.TutorialProgress).</summary>
        public List<string> seenTips = new List<string>();
    }

    [Serializable]
    public sealed class DialogueCheckpoint
    {
        // The authored StoryBeat.beatId is the stable identity of the graph in the campaign; asset names may change.
        public string beatId;
        public string nodeId;
        public bool atChoices;
    }
}
