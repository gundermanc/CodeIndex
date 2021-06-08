namespace CodeIndex.Paging
{
    using System;
    using System.IO;
    using System.Text;

    public sealed class VarChar : IBinarySerializable
    {
        private string value;

        public VarChar()
        {
        }

        public VarChar(string value)
        {
            this.value = value;
        }

        public void Deserialize(BinaryReader reader, int rowSize)
        {
            var builder = new StringBuilder();

            for (int i = 0; i < rowSize && i < rowSize; i++)
            {
                var c = reader.ReadChar();
                if (c == '\0')
                {
                    break;
                }

                builder.Append(c);
            }

            this.value = builder.ToString();
        }

        public void Serialize(BinaryWriter writer, int rowSize)
        {
            int written = 0;

            // Write the string contents.
            foreach (var c in this.value)
            {
                writer.Write(c);
                written++;
            }

            // Fill in to the rest of the row.
            for (int i = written; i < rowSize; i++)
            {
                writer.Write('\0');
            }
        }
    }
}
