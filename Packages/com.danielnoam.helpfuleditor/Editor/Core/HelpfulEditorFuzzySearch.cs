using System.Collections.Generic;

namespace DNExtensions.HelpfulEditor
{
    /// <summary>
    /// Subsequence matching that understands where words begin, so "mvspd" finds "Move Speed" and
    /// "rbmass" finds "Rigidbody Mass". Plain substring matching finds neither, which is the usual
    /// reason a search box feels like it is refusing to help.
    ///
    /// Matches carry a cost so results can be ranked: consecutive characters are free, landing on the
    /// start of a word is nearly free, and skipping through the middle of one is not.
    /// </summary>
    internal static class HelpfulEditorFuzzySearch
    {
        private static readonly char[] Separators = { ' ', '-', '_', '.', '(', ')', '[', ']', '/' };

        // Reused across calls: this runs for every property of every component on each keystroke.
        private static readonly List<int> WordStarts = new List<int>();
        private static readonly List<int> NextWordStart = new List<int>();

        /// <summary>Lower cost is a better match. False when the query does not match at all.</summary>
        public static bool TryMatch(string name, string query, out float cost)
        {
            cost = 0f;

            if (string.IsNullOrEmpty(query)) return true;
            if (string.IsNullOrEmpty(name)) return false;

            BuildWordStarts(name);

            int nameIndex = 0;
            int queryIndex = 0;
            int previousMatch = -1;

            while (nameIndex < name.Length && queryIndex < query.Length)
            {
                char wanted = char.ToLowerInvariant(query[queryIndex]);

                // Straight ahead. A run of consecutive characters costs nothing, so an exact
                // substring always beats a scattered match.
                if (char.ToLowerInvariant(name[nameIndex]) == wanted)
                {
                    cost += nameIndex - previousMatch - 1;

                    previousMatch = nameIndex;
                    nameIndex++;
                    queryIndex++;
                    continue;
                }

                // Otherwise try skipping to the next word. Cheaper than the gap it crosses, because
                // jumping between words is what the user meant by typing initials — but never free,
                // so it loses to a straight run.
                int wordStart = NextWordStart[nameIndex];

                if (wordStart > nameIndex && char.ToLowerInvariant(name[wordStart]) == wanted)
                {
                    float gap = wordStart - previousMatch - 1;
                    cost += gap * 0.01f < 0.9f ? gap * 0.01f : 0.9f;

                    previousMatch = wordStart;
                    nameIndex = wordStart + 1;
                    queryIndex++;
                    continue;
                }

                nameIndex++;
            }

            return queryIndex >= query.Length;
        }

        /// <summary>
        /// Where each word begins: after a separator, at a capital that starts one, and at the edges
        /// of a number. Then, for every position, the next such index — walked once so the matcher
        /// does not rescan the name looking for it.
        /// </summary>
        private static void BuildWordStarts(string name)
        {
            WordStarts.Clear();
            WordStarts.Add(0);

            for (int i = 1; i < name.Length; i++)
            {
                char previous = name[i - 1];
                char current = name[i];
                char next = i + 1 < name.Length ? name[i + 1] : '\0';

                bool afterSeparator = IsSeparator(previous) && !IsSeparator(current);

                // The second test catches the last capital of a run, as in the "R" of "URPAsset".
                bool camelHump = char.IsUpper(current) && (char.IsLower(previous) || char.IsLower(next));

                bool numberStart = char.IsDigit(current) && !char.IsDigit(previous);
                bool afterNumber = char.IsDigit(previous) && !char.IsDigit(current);

                if (afterSeparator || camelHump || numberStart || afterNumber) WordStarts.Add(i);
            }

            NextWordStart.Clear();
            for (int i = 0; i < name.Length; i++) NextWordStart.Add(i);

            int wordIndex = 0;

            for (int i = 0; i < name.Length; i++)
            {
                // Advance past any word start at or before this position, so the answer is always
                // the next one rather than the current one.
                while (wordIndex < WordStarts.Count && WordStarts[wordIndex] <= i) wordIndex++;

                NextWordStart[i] = wordIndex < WordStarts.Count ? WordStarts[wordIndex] : i;
            }
        }

        private static bool IsSeparator(char character)
        {
            foreach (char separator in Separators)
            {
                if (character == separator) return true;
            }

            return false;
        }
    }
}
