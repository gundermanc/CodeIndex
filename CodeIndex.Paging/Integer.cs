namespace CodeIndex.Paging
{
    using System.IO;

    public sealed class Integer : IBinarySerializable
    {
        public Integer()
        {
        }

        public Integer(int value)
        {
            this.Value = value;
        }

        public int Value { get; internal set; }

        public void Deserialize(BinaryReader reader, int rowSize)
        {
            if (rowSize != (sizeof(int)))
            {
                throw new InvalidDataException("Expected an exact match");
            }

            this.Value = reader.ReadInt32();
        }

        public void Serialize(BinaryWriter writer, int rowSize)
        {
            if (rowSize != (sizeof(int)))
            {
                throw new InvalidDataException("Expected an exact match");
            }

            writer.Write(this.Value);
        }
    }
}
