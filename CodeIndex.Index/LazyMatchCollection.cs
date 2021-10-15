namespace CodeIndex.Index
{
    using System;
    using System.Collections.Generic;
    using System.IO;

    public class LazyMatchCollection
    {
        public LazyMatchCollection(string fileName, IReadOnlyList<string> tokens)
        {
            this.FileName = fileName;
            this.Tokens = tokens;
        }

        public string FileName { get; }

        public IReadOnlyList<string> Tokens { get; }

        public IEnumerable<Match> GetMatches()
        {
            var lines = File.ReadAllLines(this.FileName);

            int i = 0;
            foreach (var line in lines)
            {
                // TODO: we can do this in a single pass.
                foreach (var token in this.Tokens)
                {
                    var index = line.IndexOf(token, StringComparison.OrdinalIgnoreCase);

                    if (index >= 0)
                    {
                        yield return new Match(token, i);
                    }
                }

                i++;
            }
        }
    }
}
