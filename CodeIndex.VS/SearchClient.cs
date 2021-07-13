namespace CodeIndex.VS
{
    using System;
    using System.Diagnostics;
    using System.Threading;
    using System.Threading.Tasks;
    using CodeIndex.Protocol;
    using Microsoft.VisualStudio.Threading;
    using StreamJsonRpc;

    internal sealed class SearchClient : IDisposable
    {
        private readonly Lazy<JsonRpc> serverRpc = new Lazy<JsonRpc>(CreateServer, isThreadSafe: true);
        private string? workspaceDirectory;

        public string? WorkspaceDirectory
        {
            get => this.workspaceDirectory;
            set
            {
                if (value != this.workspaceDirectory)
                {
                    this.workspaceDirectory = value;

                    if (value is not null)
                    {
                        this.IndexDirectoryAsync(value, CancellationToken.None).Forget();
                    }
                }
            }
        }

        public void Dispose()
        {
            if (this.serverRpc.IsValueCreated)
            {
                this.serverRpc.Value.Dispose();
            }
        }

        public Task IndexDirectoryAsync(string directory, CancellationToken cancellationToken)
        {
            return this.serverRpc.Value.InvokeWithCancellationAsync(
                CodeIndexMethods.IndexDirectoryMethodName,
                new[] { directory },
                cancellationToken);
        }

        public Task<SearchIndexResponse> SearchAsync(string searchQuery, CancellationToken cancellationToken)
        {
            return this.serverRpc.Value.InvokeWithCancellationAsync<SearchIndexResponse>(
                CodeIndexMethods.SearchIndexMethodName,
                new[]
                {
                    this.WorkspaceDirectory ?? throw new InvalidOperationException($"{WorkspaceDirectory} is uninitialized"),
                    searchQuery,
                },
                cancellationToken);
        }

        private static JsonRpc CreateServer()
        {
            // TODO: package CLI with extension.
            var startInfo = new ProcessStartInfo
            {
                FileName = @"D:\Repos\CodeIndex\CodeIndex\bin\Debug\net6.0\CodeIndex.Cli.exe",
                Arguments = "server",
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            var p = new Process();
            p.StartInfo = startInfo;
            p.EnableRaisingEvents = true;
            p.Start();

            // Use MessagePack instead of JSON for better throughput.
            var rpc = new JsonRpc(
                new LengthHeaderMessageHandler(
                    p.StandardInput.BaseStream,
                    p.StandardOutput.BaseStream,
                    new MessagePackFormatter()));
            rpc.StartListening();

            return rpc;
        }
    }
}
