﻿namespace CodeIndex.Index
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    using static CodeIndex.Index.NonAllocatingKey;

    internal sealed class CompoundIndexer
    {
        private readonly HashSet<string> words = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        private readonly Dictionary<string, HashSet<string>> wordToContainingFileMapping = new(StringComparer.OrdinalIgnoreCase);

        private int nextFileToIndex = -1;

        public IReadOnlyList<string> Files { get; }

        public CompoundIndexer(IEnumerable<string> files)
        {
            var filesToIndex = new List<string>();

            foreach (var file in files)
            {
                // HACK: Drop files with paths that can't be encoded at one char
                // per byte until we fix the bug in PagingList<> that prevents
                // these from being serialized correctly.
                if (file.Length == Encoding.UTF8.GetByteCount(file))
                {
                    filesToIndex.Add(file);
                }
            }

            this.Files = filesToIndex;
        }

        public async Task<(HashSet<string> words, Dictionary<string, HashSet<string>> wordsToFilesMap)> IndexAsync()
        {
            var consumerTasks = new List<Task>();

            for (int i = 0; i <  Environment.ProcessorCount; i++)
            {
                consumerTasks.Add(Task.Run(this.ConsumeAsync));
            }

            consumerTasks.Add(Task.Run(this.PrintProgressAsync));

            await Task.WhenAll(consumerTasks);

            return (this.words, this.wordToContainingFileMapping);
        }

        private async Task PrintProgressAsync()
        {
            int nextFileToIndex = 0;

            while (true)
            {
                lock (this.Files)
                {
                    if (this.nextFileToIndex >= this.Files.Count)
                    {
                        return;
                    }

                    nextFileToIndex = this.nextFileToIndex;
                }

                await Task.Delay(1000);

                Console.WriteLine($"Indexing progress: {nextFileToIndex} of {this.Files.Count} ({(float)nextFileToIndex / this.Files.Count * 100}%)");
            }
        }

        private async Task ConsumeAsync()
        {
            NonAllocatingKeySourceCache cache = new();
            string? file = null;

            while (true)
            {
                var nextFileToIndex = Interlocked.Increment(ref this.nextFileToIndex);

                if (nextFileToIndex >= this.Files.Count)
                {
                    return;
                }

                file = this.Files[nextFileToIndex];

                if (file is null)
                {
                    return;
                }

                // Ignore large (probably non-code files).
                if (new FileInfo(file).Length < 500_000)
                {
                    var words = await FileIndexer.QuickIndexFile(cache, file);

                    foreach (var word in words)
                    {
                        this.AddWord(file, word);
                    }
                }
            }
        }

        private void AddWord(string file, string word)
        {
            // Add to words list.
            lock (this.words)
            {
                this.words.Add(word);
            }

            lock (this.wordToContainingFileMapping)
            {
                // Get or create the containing files set.
                if (!this.wordToContainingFileMapping.TryGetValue(word, out var containingFiles))
                {
                    this.wordToContainingFileMapping[word] = containingFiles = new HashSet<string>();
                }

                containingFiles.Add(file);
            }
        }
    }
}
