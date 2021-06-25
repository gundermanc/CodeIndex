namespace CodeIndex.Paging
{
    using System;
    using System.Collections.Generic;
    using System.IO;

    public class StorageContextWriter : IDisposable
    {
        private readonly BinaryWriter innerWriter;
        private long lastContextHeaderPosition = -1;
        private readonly List<long> contextStartOffsets = new List<long>();

        public StorageContextWriter(string fileName)
        {
            this.innerWriter = new BinaryWriter(File.Create(fileName));

            // Allocate space for the start of the footer.
            this.innerWriter.Write((long)0);
        }


        public BinaryWriter PushNewContext()
        {
            // Save current position so we can get back to it.
            var currentPosition = this.innerWriter.BaseStream.Position;

            if (this.lastContextHeaderPosition > -1)
            {
                // Backpatch the header from the last PushNewContext() call
                // with the length of the context (the length of the substream).
                this.innerWriter.BaseStream.Position = lastContextHeaderPosition;
                this.innerWriter.Write(currentPosition - lastContextHeaderPosition + sizeof(long));

                // Move back to original position.
                this.innerWriter.BaseStream.Position = currentPosition;

                // Save the section start offset so we can find it again.
                this.contextStartOffsets.Add(this.lastContextHeaderPosition);
            }

            this.lastContextHeaderPosition = currentPosition;

            // Allocate space for our own header.
            this.innerWriter.Write((long)0);

            return this.innerWriter;
        }

        public void Dispose()
        {
            // Finish up any unfinished contexts.
            this.PushNewContext();

            // Save start of footer.
            var footerPosition = this.innerWriter.BaseStream.Position;

            // Write offsets.
            this.innerWriter.Write(this.contextStartOffsets.Count);
            foreach (var offset in this.contextStartOffsets)
            {
                this.innerWriter.Write(offset);
            }

            // Move to start.
            this.innerWriter.BaseStream.Position = 0;

            // Update the footer offset record.
            this.innerWriter.Write(footerPosition);

            // Cleanup.
            this.innerWriter.Dispose();
        }
    }
}
