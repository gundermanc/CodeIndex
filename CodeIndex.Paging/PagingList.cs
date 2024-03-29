﻿namespace CodeIndex.Paging
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.IO;

    public sealed class PagingList<TData>
        : IReadOnlyList<TData>
        where TData : IBinarySerializable, new()
    {
        private readonly PageCache pageCache;
        private readonly BinaryReader reader;
        private readonly int rowSize;

        public static void Write(
            StorageContextWriter context,
            int rowSize,
            IEnumerable<TData> items)
        {
            var writer = context.PushNewContext();

            writer.Write(rowSize);

            foreach (var item in items)
            {
                var beforePosition = writer.BaseStream.Position;
                item.Serialize(writer, rowSize);
                var afterPosition = writer.BaseStream.Position;

                // Make sure all writes match the cell size.
                var bytesWritten = afterPosition - beforePosition;
                if (bytesWritten != rowSize && bytesWritten != 0)
                {
                    throw new InvalidDataException("Write does not conform to cell size");
                }
            }
        }

        public PagingList(
            PageCache pageCache,
            StorageContextReader reader)
        {
            this.pageCache = pageCache;

            // TODO: dispose.
            this.reader = reader.PushNewContext();

            this.rowSize = this.reader.ReadInt32();
        }

        public TData this[int index]
        {
            get
            {
                if (index < 0 || index > this.Count)
                {
                    throw new ArgumentOutOfRangeException(nameof(index));
                }

                var pageNumber = index / this.pageCache.RecordsPerPage;
                var indexInPage = index % this.pageCache.RecordsPerPage;

                // Cache hit?
                // TODO: pass this specific instance instead of a type.
                if (this.pageCache.TryGetValue<int, List<TData>>(this, pageNumber, out var cachedPage))
                {
                    return cachedPage[indexInPage];
                }

                // No? We'll need to read the page.
                var newPage = new List<TData>();
                for (int i = 0; i < this.pageCache.RecordsPerPage; i++)
                {
                    // Set position at next record...
                    // ...after the 4 byte size header
                    // ...after the n rows per page * page number
                    // ...after the i-th row.
                    this.reader.BaseStream.Position = sizeof(int) + (pageNumber * this.rowSize * this.pageCache.RecordsPerPage) + (this.rowSize * i);

                    // Make sure the target position isn't beyond EOF.
                    var beforePosition = this.reader.BaseStream.Position;
                    if (beforePosition >= this.reader.BaseStream.Length)
                    {
                        break;
                    }

                    var row = new TData();
                    row.Deserialize(this.reader, this.rowSize);
                    var afterPosition = this.reader.BaseStream.Position;

                    // Make sure all reads are less than the cell size.
                    var bytesRead = afterPosition - beforePosition;
                    if (bytesRead > this.rowSize)
                    {
                        throw new InvalidDataException("Read exceeds cell size");
                    }

                    newPage.Add(row);
                }

                // Add the read page to the page cache.
                this.pageCache.Add(this, pageNumber, newPage);
                return newPage[indexInPage];
            }
        }

        public int Count => (int)((this.reader.BaseStream.Length - sizeof(int)) / this.rowSize);

        public IEnumerator<TData> GetEnumerator()
        {
            throw new NotImplementedException();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }
    }
}
