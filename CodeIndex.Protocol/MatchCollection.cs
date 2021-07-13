namespace CodeIndex.Protocol
{
    using System.Runtime.Serialization;

    [DataContract]
    public struct MatchCollection
    {
        public MatchCollection(string fileName, string word)
        {
            this.FileName = fileName;
            this.Word = word;
        }

        [DataMember(Order = 0)]
        public string FileName { get; set; }

        [DataMember(Order = 1)]
        public string Word { get; set; }
    }
}
