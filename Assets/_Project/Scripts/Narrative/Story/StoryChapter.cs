using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using Zmywak.Core.Data;
using Zmywak.Narrative.Dialogue;

namespace Zmywak.Narrative.Story
{
    /// <summary>One scene of a chapter, played at home once its day has come.</summary>
    [Serializable]
    public class StoryBeat
    {
        public string beatId;

        [Tooltip("The day (1 = the first) from which the scene can play. It waits if the player is late.")]
        public int day = 1;

        [Tooltip("Only plays once this story flag is set. Empty = no condition.")]
        public string requiredFlag;

        public DialogueGraph graph;

        [Tooltip("Playing this scene completes the chapter.")]
        public bool endsChapter;
    }

    /// <summary>Where a chapter takes Kuba if a story flag says so (the first that holds wins).</summary>
    [Serializable]
    public class StoryRelocation
    {
        public string ifFlag;
        public Place place;
    }

    /// <summary>A chapter of Kuba's story: its title, its scenes in the order they play, and - for a chapter that
    /// begins years after the one before - the date the calendar moves on to when it starts, and where Kuba lives by
    /// then.</summary>
    [CreateAssetMenu(menuName = "Zmywak/Story Chapter", fileName = "Chapter_")]
    public class StoryChapter : ScriptableObject
    {
        [SerializeField] private string chapterId;
        [SerializeField] private string title;
        [SerializeField] private List<StoryBeat> beats = new List<StoryBeat>();

        [Tooltip("yyyy-MM-dd: the calendar jumps to this date as the chapter's first scene begins. Empty = the days run on.")]
        [SerializeField] private string startsOn;

        [Tooltip("With the jump in the calendar, Kuba moves: to the place of the first of these whose flag is set.")]
        [SerializeField] private List<StoryRelocation> relocations = new List<StoryRelocation>();

        public string ChapterId => chapterId;
        public string Title => title;
        public IReadOnlyList<StoryBeat> Beats => beats;
        public IReadOnlyList<StoryRelocation> Relocations => relocations;

        /// <summary>Where the chapter takes Kuba as it begins, by the story so far; false if he stays where he is.</summary>
        public bool TryGetRelocation(Func<string, bool> hasFlag, out Place place)
        {
            foreach (StoryRelocation relocation in relocations)
            {
                if (relocation != null && hasFlag(relocation.ifFlag))
                {
                    place = relocation.place;
                    return true;
                }
            }

            place = Place.Dublin;
            return false;
        }

        /// <summary>Story flag set when the chapter's closing scene has played.</summary>
        public string CompletedFlag => $"{chapterId}_complete";

        /// <summary>The date this chapter moves the calendar to, if it begins later than the last one ended.</summary>
        public bool TryGetStartDate(out DateTime date)
        {
            return DateTime.TryParseExact(startsOn, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out date);
        }

        /// <summary>Replaces the chapter - for content generators and tests; the game only reads chapters.</summary>
        public void SetContent(string id, string chapterTitle, List<StoryBeat> content, DateTime? startDate = null,
            List<StoryRelocation> moves = null)
        {
            chapterId = id;
            title = chapterTitle;
            beats = content ?? new List<StoryBeat>();
            startsOn = startDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? string.Empty;
            relocations = moves ?? new List<StoryRelocation>();
        }
    }

    /// <summary>Which scene plays next: the first one, in chapter order, not yet seen, whose day has come and whose condition holds.</summary>
    public static class StoryBeatScheduler
    {
        public static StoryBeat NextBeat(IReadOnlyList<StoryBeat> beats, int day, Func<string, bool> seen, Func<string, bool> hasFlag)
        {
            if (beats == null)
            {
                return null;
            }

            foreach (StoryBeat beat in beats)
            {
                if (beat == null || beat.graph == null || seen(beat.beatId))
                {
                    continue;
                }

                if (beat.day > day)
                {
                    return null; // scenes play in order: a later one never jumps ahead of one still waiting for its day
                }

                if (!string.IsNullOrEmpty(beat.requiredFlag) && !hasFlag(beat.requiredFlag))
                {
                    continue;
                }

                return beat;
            }

            return null;
        }
    }
}
