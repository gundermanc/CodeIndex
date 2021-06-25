namespace CodeIndex.Paging
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;

    public class PagingList2D<TData>
        : IReadOnlyList<IReadOnlyList<TData>>
        where TData : IBinarySerializable, new()
    {
        private readonly PagingList<Range> firstList;
        private readonly PagingList<TData> secondList;

        public static void Write(
            StorageContextWriter context,
            int rowSize,
            IReadOnlyList<IReadOnlyList<TData>> data)
        {
            var innerItems = data.SelectMany(data => data);

            PagingList<Range>.Write(context, 8, GenerateRanges(data));
            PagingList<TData>.Write(context, rowSize, innerItems);
        }

        public PagingList2D(
            PageCache pageCache,
            StorageContextReader reader)
        {
            this.firstList = new PagingList<Range>(pageCache, reader);
            this.secondList = new PagingList<TData>(pageCache, reader);
        }

        public IReadOnlyList<TData> this[int index]
            => new PagingListRange<TData>(this.secondList, this.firstList[index]);

        public int Count => this.firstList.Count;

        public IEnumerator<IReadOnlyList<TData>> GetEnumerator()
        {
            throw new NotImplementedException();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }

        private static IEnumerable<Range> GenerateRanges(IReadOnlyList<IReadOnlyList<TData>> data)
        {
            int i = 0;

            foreach (var sublist in data)
            {
                yield return new Range(i, sublist.Count);

                i += sublist.Count;
            }
        }
    }
}
