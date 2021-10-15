namespace CodeIndex.Index
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using static CodeIndex.Index.NonAllocatingKey;

    internal static class FileIndexer
    {
        public static async Task<IEnumerable<string>> QuickIndexFile(
            NonAllocatingKeySourceCache cache,
            string file)
        {
            var source = new NonAllocatingKeySource(cache);
            HashSet<NonAllocatingKey> words = new HashSet<NonAllocatingKey>();

            char [] buffer = new char[1024];
            int read;

            using (var stream = new StreamReader(File.OpenRead(file)))
            {
                do
                {
                    read = await stream.ReadBlockAsync(
                        buffer,
                        0,
                        buffer.Length);

                    ScrapeWords(buffer.AsMemory().Slice(0, read), words, source);
                }
                while (read > 0);
            }

            return words.Select(words => words.String);
        }

        private static void ScrapeWords(ReadOnlyMemory<char> buffer, HashSet<NonAllocatingKey> words, NonAllocatingKeySource source)
        {
            var wordStart = 0;

            var span = buffer.Span;

            for (int i = 0; i < buffer.Length - 3; i++)
            {
                // Check if the trigram has been discovered before, without allocating anything.
                var key = source.GetTransientKey(buffer[wordStart..(wordStart + 3)]);
                var keySpan = key.Memory.Span;
                if (keySpan.Length == Encoding.UTF8.GetByteCount(keySpan) &&
                    words.Add(key))
                {
                    // No, it hasn't. Allocate a string for this (freeing our buffer for reuse)
                    // remove the transient key, and re-add.
                    words.Remove(key);
                    words.Add(key.Realize());
                }

                wordStart = i + 1;
            }
        }

        private static void AddMatch(
            Dictionary<string, List<Match>> dictionary,
            string segment,
            int lineNumber)
        {
            if (!dictionary.TryGetValue(segment, out var matchesList))
            {
                matchesList = dictionary[segment] = new List<Match>();
            }

            matchesList.Add(new Match(segment, lineNumber));
        }

        public static string[] TokenizeString(string str)
        {
            return str.Split(' ', '.', '{', '}', '<', '>', '(', ')', '[', ']', ':', ';', '+', '-', '*', '/', ' ', '\0', ',', '\t', '_', '/', '|', '!', '-', '@', '#', '$', '%', '^', '&', '?', '~');
        }

        internal static IEnumerable<string> TrigramString(string query)
        {
            for (int i = 0; i < query.Length; i++)
            {
                if (i + 3 < query.Length)
                {
                    yield return query.Substring(i, 3).ToUpper();
                }
                else
                {
                    yield return query.Substring(i, query.Length - i).ToUpper();
                    yield break;
                }
            }
        }
    }
}
