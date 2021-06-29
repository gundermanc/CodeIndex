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
        private readonly PagingList2D<Integer> wordIndexToFileIndiciesMapping;

        private Index(
            StorageContextReader context,
            PagingList<VarChar> wordsList,
            PagingList<VarChar> filesList,
            PagingList2D<Integer> wordIndexToFileIndiciesMapping)
        {
            this.context = context;
            this.wordsList = wordsList;
            this.filesList = filesList;
            this.wordIndexToFileIndiciesMapping = wordIndexToFileIndiciesMapping;
        }

        public static async Task CreateAsync(string inputDirectory)
        {
            var index = await BuildIndexDictionaryAsync(inputDirectory);

            // Create sorted index.
            List<KeyValuePair<string, Dictionary<string, List<Match>>>> sortedIndex = index.OrderBy(pair => pair.Key).ToList();

            // Find the max word size to allocate.
            var maxWordSize = sortedIndex.Max(entry => entry.Key.Length);

            using (var context = new StorageContextWriter(Path.Combine(inputDirectory, IndexFileName)))
            {
                // Write the sorted word list.
                PagingList<VarChar>.Write(
                    context,
                    maxWordSize,
                    sortedIndex.Select(entry => new VarChar(entry.Key)));

                // Write the sorted files list.
                var sortedFiles = sortedIndex.SelectMany(entry => entry.Value.Keys).Distinct().OrderBy(entry => entry).ToArray();
                var maxFileSize = sortedFiles.Max(files => files.Length);
                PagingList<VarChar>.Write(
                    context,
                    maxFileSize,
                    sortedFiles.Select(file => new VarChar(file)));

                // Write the mapping from the sorted words list indexes to the sorted files that contain them.
                var containingFileEntries = new List<List<Integer>>();
                foreach (var wordEntry in sortedIndex)
                {
                    if (index.TryGetValue(wordEntry.Key, out var wordMatches))
                    {
                        var entries = new List<Integer>();

                        foreach (var file in wordMatches.Keys)
                        {
                            // TODO: binary search in tight inner loop is very slow.
                            var entryIndex = Array.BinarySearch(sortedFiles, file);
                            if (entryIndex != -1)
                            {
                                entries.Add(new Integer(entryIndex));
                            }
                        }

                        containingFileEntries.Add(entries);
                    }
                }
                PagingList2D<Integer>.Write(context, 4, containingFileEntries);
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
                new PagingList2D<Integer>(pageCache, context));
        }

        public ResultSet FindMatches(string query)
        {
            var tokens = FileIndexer.TokenizeString(query);

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
            var fileTokenMatches = new Dictionary<string, LazyMatchCollection>(StringComparer.OrdinalIgnoreCase);
            foreach (var matchIndex in matchIndexes)
            {
                var word = this.wordsList[matchIndex];
                var containingFileIndicies = this.wordIndexToFileIndiciesMapping[matchIndex];

                for (int i = 0; i < containingFileIndicies.Count; i++)
                {
                    var fileName = this.filesList[containingFileIndicies[i].Value];

                    if (!fileTokenMatches.TryGetValue(fileName.Value, out var fileTokens))
                    {
                        fileTokens = fileTokenMatches[fileName.Value] = new LazyMatchCollection(fileName.Value, word.Value);
                    }
                }
            }

            // Rank by:
            // Exact file name matches first.
            // Then substring matches.
            // Then de-prioritize tests, which are noisy and rarely what we want.
            // Then score by term frequency.
            var results = fileTokenMatches
                //.OrderByDescending(match => match.Value.Any(token => Path.GetFileNameWithoutExtension(match.Key).Equals(token, StringComparison.OrdinalIgnoreCase)))
                .OrderByDescending(match => tokens.Any(token => match.Key.Contains(token, StringComparison.OrdinalIgnoreCase)))
                .ThenBy(match => match.Key.Contains("Test"))
                //.ThenByDescending(match => match.Value.Count)
                .Take(10).ToList();

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
                    Dictionary<string, List<Match>> fileDictionary = FileIndexer.IndexFile(file);

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
                                if (!filesDictionary.TryGetValue(file, out var matchList))
                                {
                                    matchList = filesDictionary[file] = new List<Match>();
                                }

                                matchList.Add(fileMatches);
                            }
                        }
                    }
                }));
            }

            await Task.WhenAll(tasks);

            return dictionary;
        }

        public void Dispose() => this.context.Dispose();
    }
}
