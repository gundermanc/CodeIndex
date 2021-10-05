namespace CodeIndex.Paging.Disk
{
    using System;
    using System.IO;
    using System.Runtime.InteropServices;

    public sealed class PageFile
    {
        // Magic bytes for validating the file type.
        private static readonly byte[] MagicBytes = new byte[] { (byte)'C', (byte)'D', (byte)'G' };
        private const int CurrentVersion = 1;

        // Page file layout:
        //   [byte[3] Magic bytes]
        //   [int Version]
        //   [long Footer Offset]

        private readonly FileStream fileStream;
        private long footerOffset;

        public PageFile(string filePath)
        {
            this.fileStream = File.Open(filePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);

            // If file previously existed, we can read the header.
            if (this.fileStream.Length > 0)
            {
                this.ReadHeader();
            }
            else
            {
                WriteHeader();
            }
        }

        private void ReadHeader()
        {
            // Just in case.
            this.fileStream.Position = 0;

            Span<byte> header = stackalloc byte[MagicBytes.Length + sizeof(int) + sizeof(long)];
            if (this.fileStream.Read(header) < header.Length)
            {
                throw new InvalidDataException("Page file cannot be opened for reading.");
            }

            // Make sure the magic bytes are present (this IS a pagefile).
            if (MagicBytes.AsSpan() != header.Slice(0, MagicBytes.Length))
            {
                throw new InvalidDataException("Page file cannot be opened for reading. Invalid magic bytes.");
            }

            // Make sure the file header is versioned correctly.
            var versionSpan = MemoryMarshal.Cast<byte, int>(header.Slice(MagicBytes.Length, sizeof(int)));
            if (versionSpan.Length < 1 ||
                versionSpan[0] != CurrentVersion)
            {
                throw new InvalidDataException("Page file cannot be opened for reading. Unsupported version.");
            }

            // Find the offset of the footer.
            var footerSpan = MemoryMarshal.Cast<byte, long>(header.Slice(MagicBytes.Length + sizeof(int), sizeof(long)));
            if (footerSpan.Length < 1 ||
                footerSpan[0] < 0 ||
                footerSpan[0] > this.fileStream.Length)
            {
                throw new InvalidDataException("Page file cannot be opened for reading. Cannot find footer.");
            }

            this.footerOffset = footerSpan[0];
        }

        private void WriteHeader()
        {
            // Just in case.
            this.fileStream.Position = 0;

            // Write magic bytes.
            this.fileStream.Write(MagicBytes);

            // Write version.
            Span<byte> versionBytes = stackalloc byte[sizeof(int)];
            MemoryMarshal.Cast<byte, int>(versionBytes);
            this.fileStream.Write(versionBytes);

            // Writer the footer bytes.
            Span<byte> offsetBytes = stackalloc byte[sizeof(long)];
            this.fileStream.Write(offsetBytes);
        }

        public PageView ReadPage(long offset)
        {
            throw new NotImplementedException();

        }

        public PageView AllocatePage(long offset, long size)
        {
            throw new NotImplementedException();
        }

        public PageView FreePage(long offset)
        {
            throw new NotImplementedException();
        }
    }
}
