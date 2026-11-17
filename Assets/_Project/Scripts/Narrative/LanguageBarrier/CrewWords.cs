using System.Collections.Generic;

namespace Zmywak.Narrative.LanguageBarrier
{
    /// <summary>
    /// The English Kuba picks up just by being one of the crew - the little words every sentence is made of, which no
    /// scene teaches because nobody explains "and" or "it's". The closer he is to the people he works with (Integration,
    /// and a pocket dictionary on top), the more of them he catches, so the story's English slowly comes into focus as he
    /// settles in. The scenes still teach their own words; these are only the glue between them.
    /// </summary>
    public static class CrewWords
    {
        /// <summary>The language level the dictionary adds to Integration.</summary>
        public const float DictionaryBoost = 20f;

        private static readonly (float level, string[] words)[] Tiers =
        {
            (25f, new[] { "and", "not", "is", "it", "it's", "for", "on", "in", "to", "at", "of", "my" }),
            (45f, new[] { "that", "that's", "all", "me", "now", "we", "can", "are", "from", "he", "him", "back", "this", "here" }),
            (65f, new[] { "you're", "i'm", "i'll", "don't", "get", "give", "right", "well", "then", "after", "same", "there", "said" }),
            (85f, new[] { "who", "what's", "did", "need", "work", "know", "tell", "every", "time", "week", "write" })
        };

        /// <summary>Every word a language level has brought in (the levels below included).</summary>
        public static IEnumerable<string> WordsFor(float level)
        {
            foreach ((float needed, string[] words) in Tiers)
            {
                if (level < needed)
                {
                    yield break;
                }

                foreach (string word in words)
                {
                    yield return word;
                }
            }
        }

        /// <summary>The level at which the next words come in (float.MaxValue once all are known).</summary>
        public static float NextLevel(float level)
        {
            foreach ((float needed, string[] _) in Tiers)
            {
                if (level < needed)
                {
                    return needed;
                }
            }

            return float.MaxValue;
        }
    }
}
