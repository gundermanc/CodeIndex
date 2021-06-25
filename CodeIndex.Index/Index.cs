namespace CodeIndex.Index
{
    using CodeIndex.Paging;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    public sealed class Index : IDisposable
    {
        private const string IndexFileName = "index.dat";
        private readonly StorageContextReader context;
        private readonly PagingList<VarChar> wordsList;
        private readonly PagingList<VarChar> filesList;
        private readonly PagingList<Paging.Range> filesRangesList;

        private Index(
            StorageContextReader context,
            PagingList<VarChar> wordsList,
            PagingList<VarChar> filesList,
            PagingList<Paging.Range> filesRangesList)
        {
            this.context = context;
            this.wordsList = wordsList;
            this.filesList = filesList;
            this.filesRangesList = filesRangesList;
        }

        public static async Task CreateAsync(string inputDirectory)
        {
            var index = await BuildIndexDictionaryAsync(inputDirectory);

            // Create sorted index.
            var sortedIndex = index.OrderBy(pair => pair.Key).ToList();

            // Find the max word size to allocate.
            var maxWordSize = sortedIndex.Max(entry => entry.Key.Length);

            using (var context = new StorageContextWriter(Path.Combine(inputDirectory, IndexFileName)))
            {
                // Write the sorted word list.
                PagingList<VarChar>.Write(
                    context,
                    maxWordSize,
                    sortedIndex.Select(entry => new VarChar(entry.Key)));

                var files = sortedIndex.SelectMany(entry => entry.Value).Select(entry => new VarChar(entry.Key));
                var maxFileSize = files.Max(files => files.Value.Length);

                // Write the files containing those words into a list.
                PagingList<VarChar>.Write(
                    context,
                    maxFileSize,
                    files);

                // Write the mappings from words to ranges into a list.
                PagingList<Paging.Range>.Write(
                    context,
                    8,
                    GenerateRangesForIndex(sortedIndex));
            }
        }

        private static IEnumerable<Paging.Range> GenerateRangesForIndex(List<KeyValuePair<string, Dictionary<string, List<Match>>>> index)
        {
            int start = 0;
            foreach (var word in index)
            {
                var filesContaining = word.Value.Keys.Count;
                yield return new Paging.Range(start, filesContaining);

                start += filesContaining;
            }
        }

        public static Index Load(string inputDirectory)
        {
            var context = new StorageContextReader(Path.Combine(inputDirectory, IndexFileName));

            var pageCache = new PageCache(maxCount: 15, recordsPerPage: 15);

            return new Index(
                context,
                new PagingList<VarChar>(pageCache, context),
                new PagingList<VarChar>(pageCache, context),
                new PagingList<Paging.Range>(pageCache, context));
        }

        public ResultSet FindMatches(string query)
        {
            var tokens = TokenizeString(query);

            // Search for matching tokens.
            var matchIndexes = new List<int>();
            foreach (var token in tokens)
            {
                // Binary search to find prefix matches.
                var currentMatch = FindFirstMatch(this.wordsList, query);
                while (currentMatch != -1 &&
                    this.wordsList[currentMatch].Value.StartsWith(token, StringComparison.OrdinalIgnoreCase))
                {
                    matchIndexes.Add(currentMatch++);
                }
            }

            // Assemble a mapping from file => contained keyword.
            var fileTokenMatches = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
            foreach (var matchIndex in matchIndexes)
            {
                var word = this.wordsList[matchIndex];
                var filesRange = this.filesRangesList[matchIndex];

                for (int i = filesRange.Start; i < filesRange.Start + filesRange.Length; i++)
                {
                    var fileName = this.filesList[i];

                    if (!fileTokenMatches.TryGetValue(fileName.Value, out var fileTokens))
                    {
                        fileTokens = fileTokenMatches[fileName.Value] = new List<string>();
                    }

                    fileTokens.Add(word.Value);
                }
            }

            // Rank by:
            // Exact file name matches first.
            // Then substring matches.
            // Then de-prioritize tests, which are noisy and rarely what we want.
            // Then score by term frequency.
            var results = fileTokenMatches
                .OrderByDescending(match => match.Value.Any(token => Path.GetFileNameWithoutExtension(match.Key).Equals(token, StringComparison.OrdinalIgnoreCase)))
                .ThenByDescending(match => tokens.Any(token => match.Key.Contains(token, StringComparison.OrdinalIgnoreCase)))
                .ThenBy(match => match.Key.Contains("Test"))
                .ThenByDescending(match => match.Value.Count).Take(10).ToList();

            return new ResultSet(tokens, results);
        }

        private static int FindFirstMatch(IReadOnlyList<VarChar> index, string token)
        {
            int l = 0;
            int r = index.Count - 1;
            int m = -1;

            while (l <= r)
            {
                m = (int)Math.Floor((l + r) / 2.0);
                var comparison = index[m].Value.CompareTo(token);
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

            return m;
        }

        private static async Task<Dictionary<string, Dictionary<string, List<Match>>>> BuildIndexDictionaryAsync(string path)
        {
            var tasks = new List<Task>();
            var dictionary = new Dictionary<string, Dictionary<string, List<Match>>>();

            foreach (var file in Directory.GetFiles(path, "*", SearchOption.AllDirectories))
            {
                if (file.Length != Encoding.UTF8.GetByteCount(file))
                {
                    continue;
                }

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
                            //
                            // Eliminate multi-byte characters as they seem
                            // to cause trouble with serialization.
                            if (segment.Length > 3 &&
                                segment.Length < 50 &&
                                char.IsLetter(segment[0]) &&
                                segment.Length == Encoding.UTF8.GetByteCount(segment))
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

        public void Dispose() => this.context.Dispose();
    }
}
