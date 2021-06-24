namespace CodeIndex.Paging
{
    using System;
    using System.Collections;
    using System.Collections.Generic;

    internal sealed class PagingListRange<TData>
        : IReadOnlyList<TData>
        where TData : IBinarySerializable, new()
    {
        private readonly PagingList<TData> subjectList;
        private readonly Range range;

        public PagingListRange(PagingList<TData> subjectList, Range range)
        {
            if (range.Start < 0 ||
                range.Start >= subjectList.Count ||
                (range.Start + range.Length) > subjectList.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(range));
            }

            this.subjectList = subjectList;
            this.range = range;
        }

        public TData this[int index] => this.subjectList[range.Start + index];

        public int Count => this.range.Length;

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
