// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.
//
// Based on code from libmdb (https://github.com/mdbtools/mdbtools)

using System.Runtime.InteropServices;

namespace MMKiwi.MdbTools;

public static partial class BinaryExtensions
{
    [LibraryImport("msvcrt.dll")]
    private static partial int memcmp(ReadOnlySpan<byte> b1, ReadOnlySpan<byte> b2, long count);

    public static bool ByteArrayCompare(this ReadOnlySpan<byte> b1, ReadOnlySpan<byte> b2)
    {
        // Validate buffers are the same length.
        // This also ensures that the count does not exceed the length of either buffer.  
        return b1.Length == b2.Length && memcmp(b1, b2, b1.Length) == 0;
    }

    public static bool ByteArrayCompare(this Span<byte> b1, ReadOnlySpan<byte> b2)
    {
        // Validate buffers are the same length.
        // This also ensures that the count does not exceed the length of either buffer.  
        return b1.Length == b2.Length && memcmp(b1, b2, b1.Length) == 0;
    }
}