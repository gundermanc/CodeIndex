namespace CodeIndex.Paging
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;

    public sealed class PagingDictionary<TKey, TValue>
        : IReadOnlyDictionary<TKey, TValue>
        where TKey : IBinarySerializable, IEquatable<TKey>, IStableHashable, new()
        where TValue : IBinarySerializable, new()
    {
        private readonly PageCache pageCache;
        private readonly PagingList2D<DictionaryEntry<TKey, TValue>> itemBuckets;
        private readonly int keySize;
        private readonly int valueSize;

        public static void Write(
            StorageContextWriter context,
            int keySize,
            int valueSize,
            IReadOnlyList<KeyValuePair<TKey, TValue>> items)
        {
            var writer = context.PushNewContext();

            // Write the number of items in the collection.
            writer.Write(items.Count);
            writer.Write(keySize);
            writer.Write(valueSize);

            // Create a list of lists. This will be an in memory hash table.
            var itemBuckets = new List<List<DictionaryEntry<TKey, TValue>>>();
            for (int i = 0; i < items.Count; i++)
            {
                itemBuckets.Add(new List<DictionaryEntry<TKey, TValue>>());
            }

            // Assign each item to its bucket.
            foreach (var item in items)
            {
                var bucket = ComputeBucketIndex(item.Key, items.Count);

                itemBuckets[bucket].Add(
                    new DictionaryEntry<TKey, TValue>(
                        item.Key,
                        item.Value,
                        keySize,
                        valueSize));
            }

            // Serialize in-memory hash table as a list of lists of dictionary entries.
            PagingList2D<DictionaryEntry<TKey, TValue>>.Write(context, keySize + valueSize, itemBuckets);
        }

        public PagingDictionary(PageCache pageCache, StorageContextReader context)
        {
            this.pageCache = pageCache;

            var reader = context.PushNewContext();

            this.Count = reader.ReadInt32();
            this.keySize = reader.ReadInt32();
            this.valueSize = reader.ReadInt32();

            this.itemBuckets = new PagingList2D<DictionaryEntry<TKey, TValue>>(pageCache, context);
        }

        public TValue this[TKey key]
        {
            get
            {
                var bucket = ComputeBucketIndex(key, this.Count);
                var bucketEntries = this.itemBuckets[bucket];

                for (int i = 0; i < bucketEntries.Count; i++)
                {
                    if (bucketEntries[i].GetKey(this.keySize).Equals(key))
                    {
                        return bucketEntries[i].GetValue(this.keySize, this.valueSize);
                    }
                }

                throw new KeyNotFoundException();
            }
        }

        public IEnumerable<TKey> Keys => throw new NotImplementedException();

        public IEnumerable<TValue> Values => throw new NotImplementedException();

        public int Count { get; }

        public bool ContainsKey(TKey key)
        {
            throw new NotImplementedException();
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            throw new NotImplementedException();
        }

        public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value)
        {
            throw new NotImplementedException();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }

        private static int ComputeBucketIndex(TKey key, int count)
        {
            var hashCode = key.GetStableHashCode() & 0x7FFFFFFF;
            return hashCode % count;
        }
    }

    internal sealed class DictionaryEntry<TKey, TValue> : IBinarySerializable
        where TKey : IBinarySerializable, new()
        where TValue : IBinarySerializable, new()
    {
        private readonly int keySize;
        private readonly int valueSize;
        private TKey key;
        private TValue value;
        private BinaryReader reader;
        private long? readOrigin;

        public DictionaryEntry()
        {
        }

        public DictionaryEntry(TKey key, TValue value, int keySize, int valueSize)
        {
            this.key = key;
            this.value = value;
            this.keySize = keySize;
            this.valueSize = valueSize;
        }

        public TKey GetKey(int keySize)
        {
            if (this.key is null)
            {
                this.key = new TKey();
                this.reader.BaseStream.Position = this.readOrigin.Value;
                this.key.Deserialize(this.reader, keySize);
            }

            return this.key;
        }

        public TValue GetValue(int keySize, int valueSize)
        {
            if (value is null)
            {
                this.value = new TValue();
                this.reader.BaseStream.Position = this.readOrigin.Value + keySize;
                this.value.Deserialize(this.reader, valueSize);
            }

            return this.value;
        }

        public void Deserialize(BinaryReader reader, int rowSize)
        {
            this.reader = reader;
            this.readOrigin = reader.BaseStream.Position;
            reader.BaseStream.Position += rowSize;
        }

        public void Serialize(BinaryWriter writer, int rowSize)
        {
            this.key.Serialize(writer, this.keySize);
            this.value.Serialize(writer, this.valueSize);
        }
    }
}
