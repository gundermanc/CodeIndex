namespace CodeIndex.Paging
{
    using System;
    using System.IO;

    internal sealed class StreamRange : Stream
    {
        private readonly Stream stream;
        private readonly long start;
        private readonly long length;

        public StreamRange(Stream stream, long start, long length)
        {
            this.stream = stream;
            this.start = start;
            this.length = length;
        }

        public override bool CanRead => this.Position < this.Length &&
            this.stream.CanRead;

        public override bool CanSeek => this.stream.CanSeek;

        public override bool CanWrite => this.Position < this.Length
            && this.stream.CanWrite;

        public override long Length => this.length;

        public override long Position
        {
            get => this.stream.Position - this.start;
            set => this.stream.Position = this.start + value;
        }

        public override void Flush() => this.stream.Flush();

        public override int Read(byte[] buffer, int offset, int count) => this.stream.Read(buffer, offset, count);

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotImplementedException();
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        public override void Write(byte[] buffer, int offset, int count) => this.stream.Write(buffer, offset, count);
    }
}
