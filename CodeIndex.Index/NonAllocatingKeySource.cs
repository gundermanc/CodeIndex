namespace CodeIndex.Index
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Threading;

    using CodeIndex.Paging;
    using static CodeIndex.Index.NonAllocatingKey;

    internal sealed class NonAllocatingKeySource
    {
        public NonAllocatingKeySource(NonAllocatingKeySourceCache cache)
        {
            this.Cache = cache;
        }

        public int ActiveKeyId { get; private set;}

        internal NonAllocatingKeySourceCache Cache { get; }


        public NonAllocatingKey GetTransientKey(ReadOnlyMemory<char> memory)
        {
            return new NonAllocatingKey(
                this,
                realizedString: null,
                memory,
                ++this.ActiveKeyId);
        }
    }

    internal readonly struct NonAllocatingKey : IEquatable<NonAllocatingKey>
    {
        private readonly NonAllocatingKeySource source;
        private readonly string? realizedString;
        private readonly ReadOnlyMemory<char> stringMemory;
        private readonly int keyId;

        public NonAllocatingKey(
            NonAllocatingKeySource source,
            string? realizedString,
            ReadOnlyMemory<char> unrealizedString,
            int keyId)
        {
            this.source = source;
            this.realizedString = realizedString;
            this.stringMemory = unrealizedString;
            this.keyId = keyId;
        }

        public ReadOnlyMemory<char> Memory => this.stringMemory;

        public string String
        {
            get
            {
                return this.realizedString ?? throw new InvalidOperationException("Cannot get string from transient key");
            }
        }

        public bool Equals(NonAllocatingKey other)
        {
            this.ThrowIfInvalid();

            if (this.Memory.Length != other.Memory.Length)
            {
                return false;
            }

            var thisSpan = this.Memory.Span;
            var otherSpan = other.Memory.Span;

            for (int i = 0; i < this.Memory.Length; i++)
            {
                if (thisSpan[i] != otherSpan[i])
                {
                    return false;
                }
            }

            return true;
        }

        public override bool Equals([NotNullWhen(true)] object? obj)
        {
            return obj is NonAllocatingKey other ?
                this.Equals(other) :
                false;
        }

        private void ThrowIfInvalid()
        {
            if (source is null ||
                (this.realizedString is null && source.ActiveKeyId != this.keyId))
            {
                throw new InvalidOperationException("Using an expired key");
            }
        }

        public override int GetHashCode() => StableStringHash.Hash(this.Memory.Span);

        public NonAllocatingKey Realize()
        {
            return this.source.Cache.Realize(this);
        }

        internal sealed class NonAllocatingKeySourceCache
        {
            private readonly Dictionary<NonAllocatingKey, NonAllocatingKey> realizedKeyCache = new();

            // Cache is shared across all threads, use reader-writer lock to avoid lock contention.
            // private readonly ReaderWriterLock readerWriterLock = new();

            public NonAllocatingKey Realize(NonAllocatingKey key)
            {
                try
                {
                    // this.readerWriterLock.AcquireReaderLock(int.MaxValue);

                    if (!this.realizedKeyCache.TryGetValue(key, out var realizedKey))
                    {
                        // this.readerWriterLock.UpgradeToWriterLock(int.MaxValue);
                        var realizedString = key.stringMemory.ToString().ToUpperInvariant();

                        // Realize into an actual string key.
                        realizedKey = new NonAllocatingKey(
                            key.source,
                            realizedString,
                            realizedString.AsMemory(),
                            key.keyId);

                        // Add to the cache so we can reuse the same string for subsequent occurrences
                        // of this word.
                        //
                        // From the dictionary perspective the two keys are equal, so we'll have to delete
                        // and remove it to ensure it's replaced.
                        this.realizedKeyCache.Remove(realizedKey);
                        this.realizedKeyCache[realizedKey] = realizedKey;
                    }

                    return realizedKey;
                }
                finally
                {
                    // this.readerWriterLock.ReleaseLock();
                }
            }
        }
    }
}
