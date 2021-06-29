namespace CodeIndex.Index
{
    using System.Collections.Generic;
    using System.IO;
    using System.Text;

    internal static class FileIndexer
    {
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
                    break;
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
