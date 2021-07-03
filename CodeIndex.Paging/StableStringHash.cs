namespace CodeIndex.Paging
{
    using System;

    public static class StableStringHash
    {
        // Forked from System.String, for stability.
        public static unsafe int Hash(ReadOnlySpan<char> span)
        {
            unsafe
            {
                fixed (char* src = span)
                {
                    // Disabled in fork since we're no longer using null terminator as the loop condition.
                    // Contract.Assert(src[span.Length] == '\0', "src[this.Length] == '\\0'");

                    // Disabled in fork because we're reading from streams, not just managed strings,
                    // which may not be aligned.
                    // Contract.Assert(((int)src) % 4 == 0, "Managed string should start at 4 bytes boundary");

#if WIN32
                    int hash1 = (5381<<16) + 5381;
#else
                    int hash1 = 5381;
#endif
                    int hash2 = hash1;

#if WIN32
                    // 32 bit machines.
                    int* pint = (int *)src;
                    int len = this.Length;
                    while (len > 2)
                    {
                        hash1 = ((hash1 << 5) + hash1 + (hash1 >> 27)) ^ pint[0];
                        hash2 = ((hash2 << 5) + hash2 + (hash2 >> 27)) ^ pint[1];
                        pint += 2;
                        len  -= 4;
                    }

                    if (len > 0)
                    {
                        hash1 = ((hash1 << 5) + hash1 + (hash1 >> 27)) ^ pint[0];
                    }
#else
                    int c;
                    char* s = src;
                    // Differences from the original
                    // 1) stop after a certain length, since streams we're operating on
                    //    aren't null terminated.
                    // 2) initialize c in the body of the loop, since loop condition has changed.

                    //while ((c = s[0]) != 0)

                    while (s < src + span.Length)
                    {
                        // Moved from loop condition in fork.
                        c = s[0];

                        hash1 = ((hash1 << 5) + hash1) ^ c;
                        c = s[1];
                        if (c == 0)
                            break;
                        hash2 = ((hash2 << 5) + hash2) ^ c;
                        s += 2;
                    }
#endif
                    return hash1 + (hash2 * 1566083941);
                }
            }
        }
    }
}
