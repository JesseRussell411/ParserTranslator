using System.Collections.Generic;
using System.Linq;

namespace ParsingTools
{
    /// <summary>
    /// This class is capable of finding multi-character sequences in strings. example: "a := x + 7" -> ":=", "+".
    /// This is done using the Look(c, next) method. The standard use is in a for loop, looping through a string such that the current character and the next character are provided.
    /// Like so:
    /// 
    /// SequenceFinder finder = new SequenceFinder(COLLECTION OF SEQUENCES TO FIND);
    /// 
    /// char c;
    /// char? next;
    /// int text_lastIndex = text.Length - 1;
    /// string sequence;
    /// for(int i = 0; i < text.Length; i++)
    /// {
    ///     c = text[i];
    ///     next = i == text_lastIndex ? null : (char?) text[i + 1];
    ///     
    ///     sequence = finder.Look(c, next);
    /// }
    ///
    ///
    /// 
    /// Feel free to use this as boiler-plate code.
    /// </summary>
    public class SequenceFinder
    {
        public SequenceFinder(IEnumerable<string> sequences) => Reset(sequences);
        public SequenceFinder(string symbol) : this(new string[] { symbol }) { }
        public SequenceFinder() : this(new string[0]) { }

        /// <summary>
        /// Resets the finder. Good to be used if you're moving back to the start of the string.
        /// </summary>
        public void Reset()
        {
            // Reset progress.
            progress = 0;
            // Rebuild possible:
            possible.Clear();
            foreach(string s in sequences) { possible.Add(s); }
        }

        /// <summary>
        /// Resets the finder. Good to be used if you're moving back to the start of the string.
        /// </summary>
        /// <param name="replacementSequences">The sequences to replace the current sequences with.</param>
        public void Reset(IEnumerable<string> replacementSequences)
        {
            sequences = replacementSequences.Distinct().ToArray();
            Reset();
        }

        /// <summary>
        /// Returns the found sequence or an empty string if no sequence was found.
        /// </summary>
        /// <returns>The sequence that was found if one was found or null if no sequence was found.</returns>
        public string Look(char c, char? next)
        {
            if (sequences.Length == 0) return null; // * If we're not looking for anything, we won't find anything.

            string possiblyFound = null;
            // Check if each possible sequence is still possible with the current character(variable: c) and if a perfect match is found, record it in possiblyFound...
            for (int i = possible.Count - 1; i >= 0; i--)
            {
                //string op = sequences[possible[i]];
                string p = possible[i];

                if (p.Length <= progress || p[progress] != c)
                {
                    possible.RemoveAt(i);
                }
                else if (p.Length == progress + 1)
                {
                    possiblyFound = p; // possible match found
                }
            }


            // Give up if there are no possible sequences...
            if (possible.Count == 0)
            {
                Reset();
                return null;
            }


            bool nextPossible = false;
            // Find out if any of the possible sequences will still be possible with the next character...
            foreach (string p in possible)
            {
                if ((progress + 1) < p.Length && p[progress + 1] == next)
                {
                    nextPossible = true;
                    break;
                }
            }


            // If there are no sequences possible with the next character, return whatever sequence was possible if any. Otherwise, prepare for the next call.
            if (!nextPossible)
            {
                Reset();
                return possiblyFound;
            }
            else
            {
                progress++;
                return null;
            }
        }

        #region private Fields
        private string[]              sequences;                     // The sequences to look for.
        private readonly List<string> possible = new List<string>(); // All sequences that are currently possible.
        private int                   progress = 0;                  // The index of the possible sequence so far.
        #endregion
    }
}
