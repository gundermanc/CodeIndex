namespace CodeIndex.Paging.Disk
{
    using System;
    using System.Collections;

    internal sealed class BTreeNode : IList
    {
        private readonly PageView pageView;

        public BTreeNode(PageView pageView)
        {
            this.pageView = pageView;
        }

        public object this[int index] { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public bool IsFixedSize => false;

        public bool IsReadOnly => false;

        public int Count { get; }

        public bool IsSynchronized => false;

        public object SyncRoot => throw new NotImplementedException();

        public int Add(object value)
        {
            throw new NotImplementedException();
        }

        public void Clear()
        {
            throw new NotImplementedException();
        }

        public bool Contains(object value)
        {
            throw new NotImplementedException();
        }

        public void CopyTo(Array array, int index)
        {
            throw new NotImplementedException();
        }

        public IEnumerator GetEnumerator()
        {
            throw new NotImplementedException();
        }

        public int IndexOf(object value)
        {
            throw new NotImplementedException();
        }

        public void Insert(int index, object value)
        {
            throw new NotImplementedException();
        }

        public void Remove(object value)
        {
            throw new NotImplementedException();
        }

        public void RemoveAt(int index)
        {
            throw new NotImplementedException();
        }
    }
}
