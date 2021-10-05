namespace CodeIndex.Paging.Disk
{
    using System;
    using System.Runtime.InteropServices;

    [StructLayout(LayoutKind.Sequential)]
    internal ref struct PageFileHeader
    {
        public Span<byte> MagicBytes { get; }

        public int Version { get; }
    }
}
