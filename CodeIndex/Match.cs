namespace CodeIndex
{
    internal sealed class Match
    {
        public Match(string word, int lineNumber)
        {
            this.Word = word;
            this.LineNumber = lineNumber;
        }

        public string Word { get; }

        public int LineNumber { get; }
    }
}
