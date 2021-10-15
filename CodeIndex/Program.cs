namespace CodeIndex
{
    using CodeIndex.Cli.Server;
    using CodeIndex.Index;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;

    class Program
    {
        static async Task Main(string[] args)
        {
            if (await ServerTarget.TryStartServerAsync(args))
            {
                return;
            }

            PrintAboutInfo();

            while (true)
            {
                PrintUsageInfo();
                WriteLineInColor("CodeIndex>> ", ConsoleColor.Green);

                var command = Console.ReadLine();

                if (command.StartsWith("index ", StringComparison.OrdinalIgnoreCase))
                {
                    var inputDirectory = command["index ".Length..];
                    WriteLineInColor("Indexing...", ConsoleColor.Cyan);
                    
                    var stopwatch = Stopwatch.StartNew();

                    await Index.Index.CreateAsync(inputDirectory);

                    Console.WriteLine($"Indexed {inputDirectory} in {stopwatch.ElapsedMilliseconds} milliseconds");
                }
                else if (command.StartsWith("load "))
                {
                    var inputDirectory = command["load ".Length..];
                    var index = Index.Index.Load(inputDirectory);

                    SearchLoop(index);
                }
            }
        }

        private static void SearchLoop(Index.Index index)
        {
            while (true)
            {
                WriteLineInColor("Search for >>", ConsoleColor.Green);

                var searchString = Console.ReadLine();

                var stopwatch = Stopwatch.StartNew();
                var results = index.FindMatches(searchString);
                stopwatch.Stop();

                Console.WriteLine($"Narrowed search to {results.FilesConsidered} of {results.TotalFiles} files ({(float)results.FilesConsidered / results.TotalFiles * 100}%), in {stopwatch.ElapsedMilliseconds} milliseconds.");

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Found matches in:");
                Console.ForegroundColor = ConsoleColor.White;
                int resultIndex = 0;
                foreach (var file in results.Results)
                {
                    Console.WriteLine($"#{resultIndex} - {file.Key}");
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
                        var selectedResult = results.Results[resultNumber];
                        FormatResult(selectedResult, results.Tokens);
                    }
                    else
                    {
                        break;
                    }
                }
            }
        }

        private static void PrintUsageInfo()
        {
            Console.WriteLine("- index [input_directory] - indexes a folder.");
            Console.WriteLine("- load [index_directory] - loads a pre-existing index");
            Console.WriteLine("- server - configures as a server for handling search requests");
            Console.WriteLine("- exit - exits the application.");
        }

        private static void PrintAboutInfo()
        {
            Console.WriteLine("CodeIndex by Christian Gunderman");
            Console.WriteLine("gundermanc@gmail.com");
            Console.WriteLine();
        }

        private static void FormatResult(KeyValuePair<string, LazyMatchCollection> selectedResult, string[] tokens)
        {
            const int ContextLines = 6;

            foreach (var result in selectedResult.Value.GetMatches().Take(10))
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
                            var candidateStartIndex = line.IndexOf(token, Math.Min(line.Length, k + 1), StringComparison.OrdinalIgnoreCase);
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
    }
}
