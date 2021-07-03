namespace CodeIndex.Index
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    internal static class FileIndexer
    {
        public static async Task<IEnumerable<string>> QuickIndexFile(string file)
        {
            var source = new NonAllocatingKeySource();
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

            for (int i = 0; i < buffer.Length; i++)
            {
                switch (span[i])
                {
                    case ' ':
                    case '!':
                    case '#':
                    case '$':
                    case '%':
                    case '&':
                    case '(':
                    case ')':
                    case '*':
                    case '.':
                    case '/':
                    case '-':
                    case ':':
                    case ';':
                    case '?':
                    case '@':
                    case '[':
                    case '\0':
                    case '\t':
                    case ']':
                    case '^':
                    case '_':
                    case '{':
                    case '|':
                    case '}':
                    case '~':
                    case '+':
                    case '<':
                    case '>':
                    case '\r':
                    case '\n':

                        // Make sure key isn't zero length (minus one for the split char).
                        if (i - 1 > wordStart)
                        {
                            // Check if the word has been discovered before, without allocating anything.
                            var key = source.GetTransientKey(buffer[wordStart..i]);
                            var keySpan = key.Memory.Span;
                            if (keySpan.Length > 3 &&
                                keySpan.Length < 50 &&
                                char.IsLetter(keySpan[0]) &&
                                keySpan.Length == Encoding.UTF8.GetByteCount(keySpan) &&
                                words.Add(key))
                            {
                                // No, it hasn't. Allocate a string for this (freeing our buffer for reuse)
                                // remove the transient key, and re-add.
                                words.Remove(key);
                                words.Add(key.Realize());
                            }

                            wordStart = i + 1;
                        }
                        break;
                }
            }
        }

        public static Dictionary<string, List<Match>> IndexFile(string file)
        {
            var fileDictionary = new Dictionary<string, List<Match>>();

            var lines = File.ReadAllLines(file);

            int lineNumber = 1;
            foreach (var line in lines)
            {
                // Reject binary files.
                if (line.Contains("\0\0\0"))
                {
                    fileDictionary.Clear();
                }

                foreach (var segment in TokenizeString(line))
                {
                    // Trim tokens that don't look strictly relevant.
                    //
                    // Eliminate multi-byte characters as they seem
                    // to cause trouble with serialization.
                    if (segment.Length > 3 &&
                        segment.Length < 50 &&
                        char.IsLetter(segment[0]) &&
                        segment.Length == Encoding.UTF8.GetByteCount(segment))
                    {
                        AddMatch(fileDictionary, segment, lineNumber);
                    }
                }

                lineNumber++;
            }

            return fileDictionary;
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
    }
}
