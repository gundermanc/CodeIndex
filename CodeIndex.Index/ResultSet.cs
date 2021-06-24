using System.Collections.Generic;

namespace CodeIndex.Index
{
    public class ResultSet
    {
        public ResultSet(string[] tokens, List<KeyValuePair<string, List<string>>> results)
        {
            this.Tokens = tokens;
            this.Results = results;
        }

        public string[] Tokens { get; }

        public List<KeyValuePair<string, List<string>>> Results { get; }
    }
}
