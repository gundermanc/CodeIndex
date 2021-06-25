namespace CodeIndex.Paging
{
    using System;
    using System.Diagnostics.Contracts;
    using System.IO;
    using System.Text;

    public sealed class VarChar :
        IBinarySerializable,
        IStableHashable,
        IEquatable<VarChar>
    {
        public string Value { get; internal set; }

        public VarChar()
        {
        }

        public VarChar(string value)
        {
            this.Value = value;
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

            this.Value = builder.ToString();
        }

        public void Serialize(BinaryWriter writer, int rowSize)
        {
            int written = 0;

            // Write the string contents.
            foreach (var c in this.Value)
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

        // Forked from System.String, for stability.
        public unsafe int GetStableHashCode()
        {
            unsafe
            {
                fixed (char* src = this.Value)
                {
                    Contract.Assert(src[this.Value.Length] == '\0', "src[this.Length] == '\\0'");
                    Contract.Assert(((int)src) % 4 == 0, "Managed string should start at 4 bytes boundary");

#if WIN32
                    int hash1 = (5381<<16) + 5381;
#else
                    int hash1 = 5381;
#endif
                    int hash2 = hash1;

#if WIN32
                    // 32 bit machines.
                    int* pint = (int *)src;
                    int len = this.Length;
                    while (len > 2)
                    {
                        hash1 = ((hash1 << 5) + hash1 + (hash1 >> 27)) ^ pint[0];
                        hash2 = ((hash2 << 5) + hash2 + (hash2 >> 27)) ^ pint[1];
                        pint += 2;
                        len  -= 4;
                    }

                    if (len > 0)
                    {
                        hash1 = ((hash1 << 5) + hash1 + (hash1 >> 27)) ^ pint[0];
                    }
#else
                    int c;
                    char* s = src;
                    while ((c = s[0]) != 0)
                    {
                        hash1 = ((hash1 << 5) + hash1) ^ c;
                        c = s[1];
                        if (c == 0)
                            break;
                        hash2 = ((hash2 << 5) + hash2) ^ c;
                        s += 2;
                    }
#endif
                    return hash1 + (hash2 * 1566083941);
                }
            }
        }

        public bool Equals(VarChar other) => this.Value == other.Value;
    }
}
