namespace CodeIndex.Index
{
    using CodeIndex.Paging;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
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
            var files = Directory.GetFiles(inputDirectory, "*", SearchOption.AllDirectories)
                .Where(file => !file.EndsWith(".pdb") && !file.EndsWith(".exe") && !file.EndsWith(".lib") && !file.EndsWith(".vsix") && !file.EndsWith(".dll") && !file.EndsWith(".zip") && !file.EndsWith(".nupkg") && !file.EndsWith(".log") && !file.EndsWith(".winmd") && !file.EndsWith(".png") && !file.EndsWith(".so") && !file.EndsWith(".dat")).ToArray();
            var indexer = new CompoundIndexer(files);

            (var words, var wordToFileMap) = await indexer.IndexAsync();

            // Find the max word size to allocate.
            var maxWordSize = words.Max(entry => entry.Length);
            var sortedWords = words.OrderBy(word => word).ToArray();

            using (var context = new StorageContextWriter(Path.Combine(inputDirectory, IndexFileName)))
            {
                // Write the sorted word list.
                PagingList<VarChar>.Write(
                    context,
                    maxWordSize,
                    sortedWords.Select(entry => new VarChar(entry)));

                // Write the sorted files list.
                var sortedFiles = indexer.Files.OrderBy(entry => entry).ToArray();
                var maxFileSize = sortedFiles.Max(files => files.Length);
                PagingList<VarChar>.Write(
                    context,
                    maxFileSize,
                    sortedFiles.Select(file => new VarChar(file)));

                // Dictionary of files for quick lookup.
                var filesDictionary = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                int i = 0;
                foreach (var file in sortedFiles)
                {
                    filesDictionary.Add(file, i++);
                }

                // Write the mapping from the sorted words list indexes to the sorted files that contain them.
                var containingFileEntries = new List<List<Integer>>();
                foreach (var word in sortedWords)
                {
                    if (wordToFileMap.TryGetValue(word, out var containingFiles))
                    {
                        var entries = new List<Integer>();

                        foreach (var file in containingFiles)
                        {
                            if (filesDictionary.TryGetValue(file, out var entryIndex))
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

            var pageCache = new PageCache(maxCount: 1000, recordsPerPage: 15);

            return new Index(
                context,
                new PagingList<VarChar>(pageCache, context),
                new PagingList<VarChar>(pageCache, context),
                new PagingList2D<Integer>(pageCache, context));
        }

        public ResultSet FindMatches(string query)
        {
            var trigrams = FileIndexer.TrigramString(query).ToArray();
            var tokens = FileIndexer.TokenizeString(query).ToArray();

            // Search for matching tokens.
            var matchIndexes = new List<int>();
            foreach (var token in trigrams)
            {
                // Binary search to find prefix matches.
                var currentMatch = FindFirstMatch(this.wordsList, token);
                if (currentMatch != -1 &&
                    this.wordsList[currentMatch].Value.StartsWith(token, StringComparison.OrdinalIgnoreCase))
                {
                    matchIndexes.Add(currentMatch);
                }
            }

            // Count trigrams matched by each file.
            var fileTokenMatches = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var matchIndex in matchIndexes)
            {
                var word = this.wordsList[matchIndex];
                var containingFileIndicies = this.wordIndexToFileIndiciesMapping[matchIndex];

                for (int i = 0; i < containingFileIndicies.Count; i++)
                {
                    var fileName = this.filesList[containingFileIndicies[i].Value];

                    if (!fileTokenMatches.TryGetValue(fileName.Value, out var trigramCount))
                    {
                        fileTokenMatches[fileName.Value] = trigramCount = 0;
                    }

                    fileTokenMatches[fileName.Value]++;
                }
            }

            var fileTokenMatches2 = new Dictionary<string, LazyMatchCollection>(StringComparer.OrdinalIgnoreCase);
            foreach (var file in fileTokenMatches)
            {
                // Only add full matches.
                if (file.Value == trigrams.Length)
                {
                    // TODO: can probably make an option to return partial matches too.
                    fileTokenMatches2[file.Key] = new LazyMatchCollection(file.Key, tokens);
                }
            }

            // Rank by:
            // Exact file name matches first.
            // Then substring matches.
            // Then de-prioritize tests, which are noisy and rarely what we want.
            // Then score by term frequency.
            List<KeyValuePair<string, LazyMatchCollection>>? results = fileTokenMatches2
                .OrderByDescending(match => trigrams.Any(token => Path.GetFileNameWithoutExtension(match.Value.FileName).Equals(token, StringComparison.OrdinalIgnoreCase)))
                .OrderByDescending(match => trigrams.Any(token => match.Key.Contains(token, StringComparison.OrdinalIgnoreCase)))
                .ThenBy(match => match.Key.Contains("Test"))
                .Take(10).ToList();

            results = results.Take(10).ToList();

            return new ResultSet(FileIndexer.TokenizeString(query), results, filesConsidered: fileTokenMatches2.Count, totalFiles: this.filesList.Count);
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

            return m - 1;
        }

        public void Dispose() => this.context.Dispose();
    }
}
