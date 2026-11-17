using System.Collections.Generic;
using UnityEngine;

namespace Zmywak.Kitchen.Feedback
{
    /// <summary>
    /// Where the kitchen gets a sound: a recorded take from the <see cref="KitchenSoundBank"/> when there is one - a
    /// different take each time, never the same one twice running, so a rack of plates does not rattle like a machine
    /// gun - and otherwise the synthesised clip from <see cref="ProceduralSounds"/>.
    /// </summary>
    public static class KitchenSounds
    {
        private static KitchenSoundBank _bank;
        private static bool _looked;
        private static readonly Dictionary<ProceduralSounds.Sound, int> LastTake = new Dictionary<ProceduralSounds.Sound, int>();

        /// <summary>The bank in Resources, looked up once; null when the project has none.</summary>
        public static KitchenSoundBank Bank
        {
            get
            {
                if (!_looked)
                {
                    _bank = Resources.Load<KitchenSoundBank>(KitchenSoundBank.ResourceName);
                    _looked = true;
                }

                return _bank;
            }
        }

        /// <summary>Plays from this bank instead (null: synthesised only) - for tests.</summary>
        public static void Use(KitchenSoundBank bank)
        {
            _bank = bank;
            _looked = true;
            LastTake.Clear();
        }

        /// <summary>Back to the bank in Resources.</summary>
        public static void Reset()
        {
            _bank = null;
            _looked = false;
            LastTake.Clear();
        }

        public static bool IsRecorded(ProceduralSounds.Sound sound) => Bank != null && Bank.TakesOf(sound).Count > 0;

        public static AudioClip Clip(ProceduralSounds.Sound sound)
        {
            IReadOnlyList<AudioClip> takes = Bank != null ? Bank.TakesOf(sound) : null;
            if (takes == null || takes.Count == 0)
            {
                return ProceduralSounds.Clip(sound);
            }

            int last = LastTake.TryGetValue(sound, out int previous) ? previous : -1;
            int take = NextTake(last, takes.Count, Random.Range(0, int.MaxValue));
            LastTake[sound] = take;
            return takes[take] != null ? takes[take] : ProceduralSounds.Clip(sound);
        }

        /// <summary>A take out of <paramref name="count"/> from a random <paramref name="roll"/>, never <paramref name="last"/> again.</summary>
        public static int NextTake(int last, int count, int roll)
        {
            if (count <= 1)
            {
                return 0;
            }

            roll &= int.MaxValue;
            if (last < 0 || last >= count)
            {
                return roll % count;
            }

            int pick = roll % (count - 1);
            return pick >= last ? pick + 1 : pick;
        }
    }
}
