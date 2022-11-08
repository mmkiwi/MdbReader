// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.
//
// Based on code from libmdb (https://github.com/mdbtools/mdbtools)

using System.Runtime.InteropServices;

namespace MMKiwi.MdbTools.Helpers;

internal static partial class BinaryExtensions
{
    #if NET7_0_OR_GREATER
    [LibraryImport("msvcrt.dll")]
    private static partial int memcmp(ReadOnlySpan<byte> b1, ReadOnlySpan<byte> b2, long count);
    #else
    [DllImport("msvcrt.dll")]
    private static extern int memcmp(ReadOnlySpan<byte> b1, ReadOnlySpan<byte> b2, long count);
    #endif
    private static bool Compare(this ReadOnlySpan<byte> b1, ReadOnlySpan<byte> b2)
    {
        // Validate buffers are the same length.
        // This also ensures that the count does not exceed the length of either buffer.  
        return b1.Length == b2.Length && memcmp(b1, b2, b1.Length) == 0;
    }

    /// <summary>
    /// Efficiently compare two Spans byte for byte
    /// </summary>
    /// <param name="b1">The first span to compare</param>
    /// <param name="b2">The second span to compare</param>
    /// <returns>true if each byte in <c>b1</c> is identical to the same byte on <c>b2</c></returns>
    public static bool ByteArrayCompare(this Span<byte> b1, ReadOnlySpan<byte> b2)
        => Compare(b1, b2);

    /// <summary>
    /// Efficiently compare two byte arrays byte for byte
    /// </summary>
    /// <param name="b1">The first span to compare</param>
    /// <param name="b2">The second span to compare</param>
    /// <returns>true if each byte in <c>b1</c> is identical to the same byte on <c>b2</c></returns>
    public static bool ByteArrayCompare(this byte[] b1, ReadOnlySpan<byte> b2)
        => Compare(b1, b2);

    /// <summary>
    /// Efficiently compare two ReadOnlySpans byte for byte
    /// </summary>
    /// <param name="b1">The first span to compare</param>
    /// <param name="b2">The second span to compare</param>
    /// <returns>true if each byte in <c>b1</c> is identical to the same byte on <c>b2</c></returns>
    public static bool ByteArrayCompare(this ReadOnlySpan<byte> b1, ReadOnlySpan<byte> b2)
        => Compare(b1, b2);
}

