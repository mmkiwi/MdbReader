// This file is part of MdbReader. Licensed under the LGPL version 2.0.
// You should have received a coy of the GNU LGPL version along with this
// program. If not, see https://www.gnu.org/licenses/old-licenses/lgpl-2.0.html
//
// Copyright Micah Makaiwi.
// Based on code from libmdb (https://github.com/mdbtools/mdbtools)

using System.Buffers.Binary;
using System.Runtime.CompilerServices;
#if !NET5_0_OR_GREATER
using System.Runtime.InteropServices;
#endif

namespace MMKiwi.MdbReader.Helpers;

/// <summary>
/// This class wraps <see cref="BinaryPrimitives" /> for two reasons:
/// 1) to polyfill for earlier .NET versions, and 
/// 2) to ensure that we don't accidentally call any big-endian functoins
/// </summary>
internal static class MdbBinary
{
    /// <inheritdoc cref="BinaryPrimitives.ReadInt16LittleEndian(ReadOnlySpan{byte}) "/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static short ReadInt16LittleEndian(ReadOnlySpan<byte> source)
    => BinaryPrimitives.ReadInt16LittleEndian(source);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static byte ReadByte(ReadOnlySpan<byte> source)
    => source[0];

    public static ushort ReadByteOrUInt16(ReadOnlySpan<byte> source)
        => source.Length switch
        {
            1 => source[0],
            _ => ReadUInt16LittleEndian(source)
        };

    /// <inheritdoc cref="BinaryPrimitives.ReadUInt16LittleEndian(ReadOnlySpan{byte}) "/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static ushort ReadUInt16LittleEndian(ReadOnlySpan<byte> source)
        => BinaryPrimitives.ReadUInt16LittleEndian(source);

    /// <inheritdoc cref="BinaryPrimitives.ReadInt32LittleEndian(ReadOnlySpan{byte}) "/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int ReadInt32LittleEndian(ReadOnlySpan<byte> source)
        => BinaryPrimitives.ReadInt32LittleEndian(source);

    /// <inheritdoc cref="BinaryPrimitives.ReadUInt32LittleEndian(ReadOnlySpan{byte}) "/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static uint ReadUInt32LittleEndian(ReadOnlySpan<byte> source)
        => BinaryPrimitives.ReadUInt32LittleEndian(source);

    /// <inheritdoc cref="BinaryPrimitives.ReadInt64LittleEndian(ReadOnlySpan{byte}) "/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static long ReadInt64LittleEndian(ReadOnlySpan<byte> source)
        => BinaryPrimitives.ReadInt64LittleEndian(source);

    /// <inheritdoc cref="BinaryPrimitives.WriteInt32LittleEndian(Span{byte}, int) "/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void WriteInt32LittleEndian(Span<byte> destination, int value)
        => BinaryPrimitives.WriteInt32LittleEndian(destination, value);
    /// <inheritdoc cref="BinaryPrimitives.WriteInt32LittleEndian(Span{byte}, int) "/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void WriteUInt32LittleEndian(Span<byte> destination, uint value)
        => BinaryPrimitives.WriteUInt32LittleEndian(destination, value);

#if NET5_0_OR_GREATER
    /// <inheritdoc cref="BinaryPrimitives.ReadSingleLittleEndian(ReadOnlySpan{byte}) "/>
    internal static float ReadSingleLittleEndian(ReadOnlySpan<byte> source)
        => BinaryPrimitives.ReadSingleLittleEndian(source);

    /// <inheritdoc cref="BinaryPrimitives.ReadDoubleLittleEndian(ReadOnlySpan{byte}) "/>
    internal static double ReadDoubleLittleEndian(ReadOnlySpan<byte> source)
        => BinaryPrimitives.ReadDoubleLittleEndian(source);
#else
    // Need to polyfill before .net 5
    /// <summary>
    /// Reads a <see cref="double" /> from the beginning of a read-only span of bytes, as little endian.
    /// </summary>
    /// <param name="source">The read-only span to read.</param>
    /// <returns>The little endian value.</returns>
    /// <remarks>Reads exactly 8 bytes from the beginning of the span.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="source"/> is too small to contain a <see cref="double" />.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double ReadDoubleLittleEndian(ReadOnlySpan<byte> source)
    {
        return !BitConverter.IsLittleEndian ?
            BitConverter.Int64BitsToDouble(BinaryPrimitives.ReverseEndianness(MemoryMarshal.Read<long>(source))) :
            MemoryMarshal.Read<double>(source);
    }

    /// <summary>
    /// Reads a <see cref="float" /> from the beginning of a read-only span of bytes, as little endian.
    /// </summary>
    /// <param name="source">The read-only span to read.</param>
    /// <returns>The little endian value.</returns>
    /// <remarks>Reads exactly 4 bytes from the beginning of the span.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="source"/> is too small to contain a <see cref="float" />.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float ReadSingleLittleEndian(ReadOnlySpan<byte> source)
    {
        return !BitConverter.IsLittleEndian ?
            BitConverter.Int32BitsToSingle(BinaryPrimitives.ReverseEndianness(MemoryMarshal.Read<int>(source))) :
            MemoryMarshal.Read<float>(source);
    }
#endif
}
