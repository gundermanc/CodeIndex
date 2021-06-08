namespace CodeIndex.Paging
{
    using System;
    using System.Collections.Generic;

    public sealed class PageCache
    {
        private readonly int maxCount;
        private readonly Dictionary<(Type, object), (object, DateTime)> cache
            = new Dictionary<(Type, object), (object, DateTime)>();
        private readonly object syncRoot = new object();

        public PageCache(int maxCount = 1000, int recordsPerPage = 15)
        {
            this.maxCount = maxCount;
            this.RecordsPerPage = recordsPerPage;
        }

        public int Lookups { get; private set; }

        public int CacheHits { get; private set; }

        public int CacheMisses { get; private set; }

        public int Evictions { get; private set; }

        public int EvictionSearches { get; private set; }

        public int RecordsPerPage { get; }

        public bool TryGetValue<TCachingObject, TKey, TValue>(TKey key, out TValue value)
        {
            lock (this.syncRoot)
            {
                this.Lookups++;

                if (this.cache.TryGetValue((typeof(TCachingObject), key), out var valueObj) &&
                    valueObj.Item1 is TValue concreteValue)
                {
                    this.CacheHits++;
                    value = concreteValue;
                    return true;
                }
            }

            this.CacheMisses++;
            value = default;
            return false;
        }

        public void Add<TCachingObject, TKey, TValue>(TKey key, TValue value)
        {
            lock (this.syncRoot)
            {
                var oldestTimeStamp = DateTime.MaxValue;
                (Type, object)? oldestRecordKey = null;

                // If cache is at max count, do an eviction.
                if (this.cache.Count + 1 > this.maxCount)
                {
                    this.EvictionSearches++;

                    // Find oldest item.
                    foreach (var item in this.cache)
                    {
                        if (item.Value.Item2 < oldestTimeStamp)
                        {
                            oldestTimeStamp = item.Value.Item2;
                            oldestRecordKey = item.Key;
                        }
                    }

                    // Remove oldest item.
                    if (oldestRecordKey is not null)
                    {
                        this.Evictions++;
                        this.cache.Remove(oldestRecordKey.Value);
                    }
                }

                // Add new item.
                this.cache.Add((typeof(TCachingObject), key), (value, DateTime.UtcNow));
            }
        }
    }
}
