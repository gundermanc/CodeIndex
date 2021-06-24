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
            string firstListFilePath,
            string secondListFilePath,
            int secondListRowSize,
            IReadOnlyList<IReadOnlyList<TData>> data)
        {
            var innerItems = data.SelectMany(data => data);

            PagingList<TData>.Write(secondListFilePath, secondListRowSize, innerItems);
            PagingList<Range>.Write(firstListFilePath, 8, GenerateRanges(data));
        }

        public PagingList2D(
            PageCache pageCache,
            string firstListFilePath,
            string secondListFilePath)
        {
            this.firstList = new PagingList<Range>(pageCache, firstListFilePath);
            this.secondList = new PagingList<TData>(pageCache, secondListFilePath);
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
