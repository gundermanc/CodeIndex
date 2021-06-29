namespace CodeIndex.Index
{
    using System.Collections.Generic;
    using System.Linq;

    public class LazyMatchCollection
    {
        public LazyMatchCollection(string fileName, string word)
        {
            this.FileName = fileName;
            this.Word = word;
        }

        public string FileName { get; }

        public string Word { get; }

        public IEnumerable<Match> GetMatches()
            => FileIndexer.IndexFile(this.FileName)
            .Where(pair => pair.Key == this.Word)
            .SelectMany(pair => pair.Value);
    }
}
