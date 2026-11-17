using System;
using System.Collections.Generic;
using UnityEngine;

namespace Zmywak.Kitchen.Feedback
{
    /// <summary>
    /// Recorded takes of the kitchen's sounds - CC0 recordings (CREDITS.md), trimmed and levelled to the synthesised
    /// ones - built by the setup from the clips in Assets/_Project/Audio/Kitchen and loaded from Resources. A sound
    /// with no take here is played synthesised.
    /// </summary>
    [CreateAssetMenu(menuName = "Zmywak/Kitchen Sound Bank")]
    public class KitchenSoundBank : ScriptableObject
    {
        public const string ResourceName = "KitchenSoundBank";

        [Serializable]
        public class Entry
        {
            public ProceduralSounds.Sound Sound;
            public List<AudioClip> Takes = new List<AudioClip>();
        }

        [SerializeField] private List<Entry> entries = new List<Entry>();

        private static readonly AudioClip[] None = new AudioClip[0];

        public IReadOnlyList<Entry> Entries => entries;

        public IReadOnlyList<AudioClip> TakesOf(ProceduralSounds.Sound sound)
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

        public void SetTakes(ProceduralSounds.Sound sound, IEnumerable<AudioClip> takes)
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
