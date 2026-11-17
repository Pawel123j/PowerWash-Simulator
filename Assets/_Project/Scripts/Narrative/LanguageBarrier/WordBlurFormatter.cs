namespace Zmywak.Narrative.LanguageBarrier
{
    /// <summary>
    /// Blurs the English words the player doesn't know yet: each stays where it is, its length and outline there
    /// (<see cref="Silhouette"/> - the letters swapped for stand-ins of the same height), set in <see cref="BlurFont"/>,
    /// whose letters are soft smudges (a TextMeshPro font tag). Surrounding punctuation is left sharp. Pure and
    /// testable - no Unity/TMP dependency.
    /// </summary>
    public static class WordBlurFormatter
    {
        /// <summary>The blurred font (the dialogue font's face, only the stand-in letters, with a wide distance field and its
        /// edges softened all the way), under TextMeshPro's "Fonts &amp; Materials" resources; the setup makes it.</summary>
        public const string BlurFont = "LiberationSans SDF - Blur";

        /// <summary>Every letter <see cref="Silhouette"/> ever writes - all the blurred font needs to hold.</summary>
        public const string StandIns = "lIjJrPmMhHqOon0";

        public const string Open = "<font=\"" + BlurFont + "\">";
        public const string Close = "</font>";

        public static string Format(string text, VocabularyTracker vocabulary)
        {
            if (string.IsNullOrEmpty(text) || vocabulary == null)
            {
                return text;
            }

            string[] tokens = text.Split(' ');
            for (int i = 0; i < tokens.Length; i++)
            {
                tokens[i] = FormatToken(tokens[i], vocabulary);
            }

            return string.Join(" ", tokens);
        }

        private static string FormatToken(string token, VocabularyTracker vocabulary)
        {
            if (string.IsNullOrEmpty(token))
            {
                return token;
            }

            int start = 0;
            int end = token.Length;
            while (start < end && !char.IsLetter(token[start]))
            {
                start++;
            }

            while (end > start && !char.IsLetter(token[end - 1]))
            {
                end--;
            }

            string prefix = token.Substring(0, start);
            string core = token.Substring(start, end - start);
            string suffix = token.Substring(end);

            if (core.Length == 0 || vocabulary.IsKnown(core))
            {
                return token;
            }

            return prefix + Open + Silhouette(core) + Close + suffix;
        }

        /// <summary>
        /// A word's shape without its letters: each letter becomes a stand-in of the same height - ascenders stay tall,
        /// descenders hang, capitals stay capitals, narrow letters narrow - so under the blur it reads as a word out of
        /// focus, its length and outline there, and nothing to make out even where the blur is thin.
        /// </summary>
        public static string Silhouette(string word)
        {
            var shape = new char[word.Length];
            for (int i = 0; i < word.Length; i++)
            {
                shape[i] = StandIn(word[i], i);
            }

            return new string(shape);
        }

        private static char StandIn(char c, int position)
        {
            bool alternate = position % 2 == 1;
            switch (char.ToLowerInvariant(c))
            {
                case 'i':
                case 'l':
                case 't':
                case 'f':
                    return char.IsUpper(c) ? 'I' : 'l';
                case 'j':
                    return char.IsUpper(c) ? 'J' : 'j';
                case 'r':
                    return char.IsUpper(c) ? 'P' : 'r';
                case 'm':
                case 'w':
                    return char.IsUpper(c) ? 'M' : 'm';
                case 'b':
                case 'd':
                case 'h':
                case 'k':
                    return char.IsUpper(c) ? 'H' : 'h';
                case 'g':
                case 'p':
                case 'q':
                case 'y':
                    return char.IsUpper(c) ? 'O' : 'q';
            }

            if (char.IsDigit(c))
            {
                return '0';
            }

            if (char.IsUpper(c))
            {
                return alternate ? 'H' : 'O';
            }

            return alternate ? 'n' : 'o';
        }
    }
}
