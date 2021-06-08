namespace CodeIndex
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;

    class Program
    {
        // Search index:
        //  - Ingestion:
        //    - Check file timestamps
        //    - If timestamps changed, update that file in the index.
        //  - In memory part:
        //    - Sorted Word list
        //    - Each word has a file on disk { hash => list of files containing that word }.
        //    - How do we avoid searching everything? Prioritization?
        //  - On disk part

        static async Task Main(string[] args)
        {
            Console.BackgroundColor = ConsoleColor.Black;
            Console.ForegroundColor = ConsoleColor.White;
            Console.Clear();

            WriteLineInColor("Indexing...", ConsoleColor.Cyan);
            var index = await BuildIndexAsync();

            while (true)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Find >> ");
                Console.ForegroundColor = ConsoleColor.White;

                var searchString = Console.ReadLine();

                var tokens = TokenizeString(searchString);

                // Search for matches for each token.
                var matches = new List<KeyValuePair<string, Dictionary<string, List<Match>>>>();
                foreach (var token in tokens)
                {
                    // Binary search to find prefix matches.
                    var currentMatch = FindFirstMatch(index, token);
                    while (currentMatch != -1 && index[currentMatch].Key.StartsWith(token, StringComparison.OrdinalIgnoreCase))
                    {
                        matches.Add(index[currentMatch++]);
                    }
                }

                // Merge match results from different tokens into a mapping from { file } => { matches }.
                var matchDictionary = new Dictionary<string, List<Match>>();
                foreach (var match in matches)
                {
                    foreach (var fileMatch in match.Value)
                    {
                        if (!matchDictionary.TryGetValue(fileMatch.Key, out var fileMatches))
                        {
                            fileMatches = matchDictionary[fileMatch.Key] = new List<Match>();
                        }

                        fileMatches.AddRange(fileMatch.Value);
                    }
                }

                // Rank by:
                // Exact file name matches first.
                // Then substring matches.
                // Then de-prioritize tests, which are noisy and rarely what we want.
                // Then score by term frequency.
                var results = matchDictionary
                    .OrderByDescending(match => tokens.Any(token => Path.GetFileNameWithoutExtension(match.Key).Equals(token, StringComparison.OrdinalIgnoreCase)))
                    .ThenByDescending(match => tokens.Any(token => match.Key.Contains(token, StringComparison.OrdinalIgnoreCase)))
                    .ThenBy(match => match.Key.Contains("Test"))
                    .ThenByDescending(match => match.Value.Count).Take(10).ToList();

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Found matches in:");
                Console.ForegroundColor = ConsoleColor.White;
                int resultIndex = 0;
                foreach (var file in results)
                {
                    Console.WriteLine($"#{resultIndex} - {file.Value.Count} - {file.Key}");
                    resultIndex++;
                }

                while (true)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("Display>>");
                    Console.ForegroundColor = ConsoleColor.White;
                    var number = Console.ReadLine();
                    if (int.TryParse(number, out var resultNumber))
                    {
                        var selectedResult = results[resultNumber];
                        FormatResult(selectedResult, tokens);
                    }
                    else
                    {
                        break;
                    }
                }
            }
        }

        private static void FormatResult(KeyValuePair<string, List<Match>> selectedResult, string[] tokens)
        {
            const int ContextLines = 6;

            foreach (var result in selectedResult.Value.Take(10))
            {
                WriteLineInColor($"{result.LineNumber}:{selectedResult.Key}", ConsoleColor.Cyan);

                foreach (var line in File.ReadAllLines(selectedResult.Key).Skip(Math.Max(0, result.LineNumber - (ContextLines / 2))).Take(ContextLines))
                {
                    Console.Write("      ");
                    int i = 0;
                    int j = 0;
                    int k = 0;
                    while (i < line.Length && i != -1)
                    {
                        // Find the index of the first token.
                        int newStartIndex = line.Length - 1;
                        int newEndIndex = line.Length - 1;
                        foreach (var token in tokens)
                        {
                            var candidateStartIndex = line.IndexOf(token, k + 1);
                            if (candidateStartIndex < newStartIndex)
                            {
                                newStartIndex = candidateStartIndex;
                                if (newStartIndex > -1)
                                {
                                    newEndIndex = candidateStartIndex + token.Length;
                                }
                            }
                        }

                        if (//i == -1 ||
                            newStartIndex == -1 ||
                            k == -1)
                        {
                            if (i == 0 && j == -1)
                            {
                                Console.WriteLine(line);
                            }
                            else
                            {
                                Console.WriteLine(line[k..]);
                            }

                            break;
                        }

                        i = j;
                        j = newStartIndex;
                        k = newEndIndex;

                        Console.Write(line[i..j]);
                        Console.BackgroundColor = ConsoleColor.Yellow;
                        Console.ForegroundColor = ConsoleColor.Black;
                        Console.Write(line[j..k]);
                        Console.BackgroundColor = ConsoleColor.Black;
                        Console.ForegroundColor = ConsoleColor.White;
                    }
                }

                Console.WriteLine();
            }
        }

        private static void WriteLineInColor(string line, ConsoleColor color)
        {
            Console.BackgroundColor = ConsoleColor.Black;
            Console.ForegroundColor = color;
            Console.WriteLine(line);
            Console.ForegroundColor = ConsoleColor.White;
        }

        private static int FindFirstMatch(List<KeyValuePair<string, Dictionary<string, List<Match>>>> index, string token)
        {
            int l = 0;
            int r = index.Count - 1;
            int m = -1;

            while (l <= r)
            {
                m = (int)Math.Floor((l + r) / 2.0);
                var comparison = index[m].Key.CompareTo(token);
                if (comparison < 0)
                {
                    l = m + 1;
                }
                else if (comparison > 0)
                {
                    r = m - 1;
                }
                else
                {
                    return m;
                }
            }

            // Update one last time and return the closest match.
            return m;
        }

        private static async Task<List<KeyValuePair<string, Dictionary<string, List<Match>>>>> BuildIndexAsync()
        {
            var index = await BuildIndexDictionaryAsync(@"C:\VSP\src\Editor");

            // Create sorted index.
            return index.OrderBy(pair => pair.Key).ToList();
        }

        private static async Task<Dictionary<string, Dictionary<string, List<Match>>>> BuildIndexDictionaryAsync(string path)
        {
            var tasks = new List<Task>();
            var dictionary = new Dictionary<string, Dictionary<string, List<Match>>>();

            foreach (var file in Directory.GetFiles(path, "*", SearchOption.AllDirectories))
            {
                tasks.Add(Task.Run(() =>
                {
                    var fileDictionary = new Dictionary<string, Dictionary<string, List<Match>>>();

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
                            if (segment.Length > 3 &&
                                segment.Length < 50 &&
                                char.IsLetter(segment[0]) &&
                                segment.All(c => char.IsLetterOrDigit(c)))
                            {
                                AddMatch(fileDictionary, segment, file, lineNumber, line);
                            }
                        }

                        lineNumber++;
                    }

                    lock (dictionary)
                    {
                        foreach (var item in fileDictionary)
                        {
                            if (!dictionary.TryGetValue(item.Key, out var filesDictionary))
                            {
                                filesDictionary = dictionary[item.Key] = new Dictionary<string, List<Match>>();
                            }

                            foreach (var fileMatches in item.Value)
                            {
                                if (!filesDictionary.TryGetValue(fileMatches.Key, out var matchList))
                                {
                                    matchList = filesDictionary[fileMatches.Key] = new List<Match>();
                                }

                                matchList.AddRange(fileMatches.Value);
                            }
                        }
                    }
                }));
            }

            await Task.WhenAll(tasks);

            return dictionary;
        }

        private static void AddMatch(
            Dictionary<string, Dictionary<string, List<Match>>> dictionary,
            string segment,
            string filePath,
            int lineNumber,
            string lineText)
        {
            if (!dictionary.TryGetValue(segment, out var filesDictionary))
            {
                filesDictionary = dictionary[segment] = new Dictionary<string, List<Match>>();
            }

            if (!filesDictionary.TryGetValue(filePath, out var matchList))
            {
                matchList = filesDictionary[filePath] = new List<Match>();
            }

            matchList.Add(new Match(segment, lineNumber));
        }

        private static string[] TokenizeString(string str)
        {
            return str.Split(' ', '.', '{', '}', '<', '>', '(', ')', '[', ']', ':', ';', '+', '-', '*', '/', ' ', '\0', ',', '\t', '_', '/', '|', '!', '-', '@', '#', '$', '%', '^', '&', '?', '~');
        }
    }
}
