using System.Collections.Generic;

namespace CodeIndex.Index
{
    public class ResultSet
    {
        public ResultSet(string[] tokens, List<KeyValuePair<string, LazyMatchCollection>> results, int filesConsidered, int totalFiles)
        {
            this.Tokens = tokens;
            this.Results = results;
            this.FilesConsidered = filesConsidered;
            this.TotalFiles = totalFiles;
        }

        public string[] Tokens { get; }

        public List<KeyValuePair<string, LazyMatchCollection>> Results { get; }

        public int FilesConsidered { get; }

        public int TotalFiles { get; }
    }
}
