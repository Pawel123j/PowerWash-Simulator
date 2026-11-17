using System;
using System.Collections.Generic;

namespace Zmywak.Narrative.LanguageBarrier
{
    /// <summary>Which English words the player currently recognizes. Plain C# so it is unit-testable.</summary>
    public class VocabularyTracker
    {
        private readonly HashSet<string> _known = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        public VocabularyTracker(IEnumerable<string> seedWords = null)
        {
            if (seedWords == null)
            {
                return;
            }

            foreach (string word in seedWords)
            {
                Learn(word);
            }
        }

        public IReadOnlyCollection<string> KnownWords => _known;

        public bool IsKnown(string word)
        {
            return !string.IsNullOrEmpty(word) && _known.Contains(Normalize(word));
        }

        public void Learn(string word)
        {
            if (!string.IsNullOrEmpty(word))
            {
                _known.Add(Normalize(word));
            }
        }

        private static string Normalize(string word)
        {
            return word.Trim().ToLowerInvariant();
        }
    }
}
