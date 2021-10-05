namespace CodeIndex.Paging.Disk
{
    using System;
    using System.IO;

    public sealed class PageView
    {
        private readonly long offset;
        private readonly long length;
        private readonly PageFile pageFile;

        internal PageView(long offset, long length, PageFile pageFile)
        {
            this.offset = offset;
            this.length = length;
            this.pageFile = pageFile;
        }

        public void DoPageAction(Action<Stream> action)
        {
            using (var stream = )
        }
    }
}
