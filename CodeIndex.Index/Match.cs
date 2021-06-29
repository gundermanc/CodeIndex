namespace CodeIndex.Index
{
    using CodeIndex.Paging;
    using System.IO;

    public sealed class Match
    {
        public Match()
        {

        }

        public Match(string word, int lineNumber)
        {
            this.Word = word;
            this.LineNumber = lineNumber;
        }

        public string Word { get; private set; }

        public int LineNumber { get; private set; }
    }
}
