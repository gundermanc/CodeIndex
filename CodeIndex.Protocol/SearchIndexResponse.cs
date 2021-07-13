namespace CodeIndex.Protocol
{
    using System.Collections.Generic;
    using System.Runtime.Serialization;

    [DataContract]
    public class SearchIndexResponse
    {
        [DataMember(Order = 1)]
        public IEnumerable<KeyValuePair<string, MatchCollection>>? Matches { get; set; }
    }
}
