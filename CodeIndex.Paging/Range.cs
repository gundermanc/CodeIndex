namespace CodeIndex.Paging
{
    using System.IO;

    public sealed class Range : IBinarySerializable
    {
        public Range()
        {
        }

        public Range(int start, int length)
        {
            this.Start = start;
            this.Length = length;
        }

        public int Start { get; internal set; }

        public int Length { get; internal set; }

        public void Deserialize(BinaryReader reader, int rowSize)
        {
            if (rowSize != (sizeof(int) * 2))
            {
                throw new InvalidDataException("Expected an exact match");
            }

            this.Start = reader.ReadInt32();
            this.Length = reader.ReadInt32();
        }

        public void Serialize(BinaryWriter writer, int rowSize)
        {
            if (rowSize != (sizeof(int) * 2))
            {
                throw new InvalidDataException("Expected an exact match");
            }

            writer.Write(this.Start);
            writer.Write(this.Length);
        }
    }
}
