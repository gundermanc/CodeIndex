using System.Collections.Generic;

namespace CodeIndex.Index
{
    public class ResultSet
    {
        public ResultSet(string[] tokens, List<KeyValuePair<string, List<Match>>> results)
        {
            this.Tokens = tokens;
            this.Results = results;
        }

        public string[] Tokens { get; }

        public List<KeyValuePair<string, List<Match>>> Results { get; }
    }
}
