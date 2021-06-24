namespace CodeIndex.Index
{
    using CodeIndex.Paging;
    using System.IO;

    public sealed class Match : IBinarySerializable
    {
        public Match()
        {

        }

        public Match(string word, int lineNumber)
        {
            this.Word = new VarChar(word);
            this.LineNumber = lineNumber;
        }

        public VarChar Word { get; private set; }

        public int LineNumber { get; private set; }

        public void Deserialize(BinaryReader reader, int rowSize)
        {
            this.Word.Deserialize(reader, rowSize - sizeof(int));
            this.LineNumber = reader.ReadInt32();
        }

        public void Serialize(BinaryWriter writer, int rowSize)
        {
            this.Word.Serialize(writer, rowSize - sizeof(int));
            writer.Write(this.LineNumber);
        }
    }
}
