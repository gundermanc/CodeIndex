namespace CodeIndex.Paging
{
    using System;
    using System.Collections.Generic;
    using System.IO;

    public class StorageContextReader : IDisposable
    {
        private readonly BinaryReader innerReader;
        private readonly List<long> contextsOffsets = new List<long>();
        private int currentContext = -1;

        public StorageContextReader(string filePath)
        {
            this.innerReader = new BinaryReader(File.OpenRead(filePath));

            // Read the offset of the footer and jump there.
            var footerOffset = this.innerReader.ReadInt64();
            this.innerReader.BaseStream.Position = footerOffset;

            // Read the contexts offsets.
            var contextsOffsets = this.innerReader.ReadInt32();
            for (int i = 0; i < contextsOffsets; i++)
            {
                this.contextsOffsets.Add(this.innerReader.ReadInt64());
            }
        }

        public void Dispose() => this.innerReader.Dispose();

        public BinaryReader PushNewContext()
        {
            this.innerReader.BaseStream.Position = this.contextsOffsets[++this.currentContext];
            var length = this.innerReader.ReadInt64();
            var start = this.innerReader.BaseStream.Position;

            return new BinaryReader(new StreamRange(this.innerReader.BaseStream, start, length));
        }
    }
}
