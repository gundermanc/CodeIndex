namespace CodeIndex.Index
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using CodeIndex.Paging;

    internal sealed class NonAllocatingKeySource
    {
        public int ActiveKeyId { get; private set;}

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
        private readonly ReadOnlyMemory<char> unrealizedString;
        private readonly int keyId;

        public NonAllocatingKey(
            NonAllocatingKeySource source,
            string? realizedString,
            ReadOnlyMemory<char> unrealizedString,
            int keyId)
        {
            this.source = source;
            this.realizedString = realizedString;
            this.unrealizedString = unrealizedString;
            this.keyId = keyId;
        }

        public ReadOnlyMemory<char> Memory => this.realizedString?.AsMemory()
            ?? this.unrealizedString;

        public string String
        {
            get
            {
                return this.realizedString ?? this.Memory.ToString(); //throw new InvalidOperationException("Cannot get string from transient key");
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
            return new NonAllocatingKey(
                this.source,
                this.Memory.ToString(),
                Array.Empty<char>().AsMemory(),
                this.keyId);
        }
    }
}
