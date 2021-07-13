namespace CodeIndex.Cli.Server
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using CodeIndex.Protocol;
    using StreamJsonRpc;

    internal sealed class ServerTarget
    {
        private readonly Dictionary<string, CodeIndex.Index.Index> loadedIndexes
            = new Dictionary<string, CodeIndex.Index.Index>(StringComparer.OrdinalIgnoreCase);

        public static async Task<bool> TryStartServerAsync(string[] args)
        {
            if (args.Length == 1 &&
                string.Equals("server", args[0], StringComparison.OrdinalIgnoreCase))
            {
                await ServerTarget.RunServerAsync();
                return true;
            }

            return false;
        }

        public JsonRpc JsonRpc { get; private set; } = null!;

        private static Task RunServerAsync()
        {
            var serverTarget = new ServerTarget();

            // Use MessagePack instead of JSON for better throughput.
            serverTarget.JsonRpc = new JsonRpc(
                new LengthHeaderMessageHandler(
                    Console.OpenStandardOutput(),
                    Console.OpenStandardInput(),
                    new MessagePackFormatter()), serverTarget);

            serverTarget.JsonRpc.StartListening();
            return serverTarget.JsonRpc.Completion;
        }

        // Private constructor to disallow creation outside the static factory methods.
        private ServerTarget()
        {
        }

        [JsonRpcMethod(CodeIndexMethods.IndexDirectoryMethodName)]
        public Task IndexDirectoryAsync(string? directory)
        {
            if (directory is null)
            {
                throw new ArgumentNullException(nameof(directory));
            }

            return CodeIndex.Index.Index.CreateAsync(directory);
        }

        [JsonRpcMethod(CodeIndexMethods.SearchIndexMethodName)]
        public SearchIndexResponse SearchIndex(string? directory, string? searchQuery)
        {
            if (directory is null ||
                searchQuery is null)
            {
                throw new ArgumentException("No parameters can be null");
            }

            lock (this.loadedIndexes)
            {
                this.EnsureIndexLoaded(directory);

                if (!this.loadedIndexes.TryGetValue(directory, out var index))
                {
                    throw new InvalidOperationException("Index is not loaded");
                }

                var result = index.FindMatches(searchQuery);

                return new SearchIndexResponse
                {
                    Matches = result.Results.Select(
                        result => new KeyValuePair<string, MatchCollection>(
                            result.Key,
                            new MatchCollection()
                            {
                                FileName = result.Value.FileName,
                                Word = result.Value.Word
                            }))
                };
            }
        }

        private void EnsureIndexLoaded(string directory)
        {
            lock (this.loadedIndexes)
            {
                // No-op if already loaded.
                if (!this.loadedIndexes.ContainsKey(directory))
                {
                    var index = CodeIndex.Index.Index.Load(directory);

                    this.loadedIndexes[directory] = index;
                }
            }
        }
    }
}
