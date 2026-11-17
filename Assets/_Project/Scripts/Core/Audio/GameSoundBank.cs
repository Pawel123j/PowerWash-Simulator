using System;
using System.Collections.Generic;
using UnityEngine;

namespace Zmywak.Core.Audio
{
    /// <summary>
    /// The clips of the game's own sounds (<see cref="GameSound"/>): CC0 recordings trimmed and levelled by the setup
    /// (CREDITS.md), the jingles and the menu's music synthesised there once, gathered from Assets/_Project/Audio/Game
    /// and loaded from Resources. A sound with no take here is synthesised when it is first played.
    /// </summary>
    [CreateAssetMenu(menuName = "Zmywak/Game Sound Bank")]
    public class GameSoundBank : ScriptableObject
    {
        public const string ResourceName = "GameSoundBank";

        [Serializable]
        public class Entry
        {
            public GameSound Sound;
            public List<AudioClip> Takes = new List<AudioClip>();
        }

        [SerializeField] private List<Entry> entries = new List<Entry>();

        private static readonly AudioClip[] None = new AudioClip[0];

        public IReadOnlyList<Entry> Entries => entries;

        public IReadOnlyList<AudioClip> TakesOf(GameSound sound)
        {
            foreach (Entry entry in entries)
            {
                if (entry.Sound == sound)
                {
                    return entry.Takes;
                }
            }

            return None;
        }

        public void SetTakes(GameSound sound, IEnumerable<AudioClip> takes)
        {
            entries.RemoveAll(e => e.Sound == sound);
            var entry = new Entry { Sound = sound };
            entry.Takes.AddRange(takes);
            if (entry.Takes.Count > 0)
            {
                entries.Add(entry);
            }
        }
    }
}
