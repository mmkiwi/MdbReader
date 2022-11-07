// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.
//
// Based on code from libmdb (https://github.com/mdbtools/mdbtools)

using System.Buffers.Binary;
using System.Collections.Immutable;
using System.Text;

namespace MMKiwi.MdbTools.Fields;

internal static class ConversionFunctions
{
    public static bool AsBool(bool isNull) => isNull;
    public static byte AsByte(ReadOnlySpan<byte> bytes) => bytes[0];
    public static short AsShort(ReadOnlySpan<byte> bytes)
        => BinaryPrimitives.ReadInt16LittleEndian(bytes);

    public static int AsInt(ReadOnlySpan<byte> bytes)
        => BinaryPrimitives.ReadInt32LittleEndian(bytes);
    public static decimal AsMoney(ReadOnlySpan<byte> bytes)
        => new decimal(BinaryPrimitives.ReadInt64LittleEndian(bytes)) / 10000;

    public static float AsFloat(ReadOnlySpan<byte> bytes)
        => BinaryPrimitives.ReadSingleLittleEndian(bytes);

    public static double AsDouble(ReadOnlySpan<byte> bytes)
        => BinaryPrimitives.ReadDoubleLittleEndian(bytes);

    public static DateTime AsDateTime(ReadOnlySpan<byte> bytes)
        // stored as fractional days since 1/1/1900
        => new DateTime(1900, 1, 1).AddDays(BinaryPrimitives.ReadDoubleLittleEndian(bytes));

    public static ReadOnlySpan<byte> AsBinary(ReadOnlySpan<byte> bytes)
        // stored as fractional days since 1/1/1900
        => bytes;

    public static string AsString(Encoding encoding, ReadOnlySpan<byte> bytes)
        // stored as fractional days since 1/1/1900
        => encoding.GetString(bytes);

    public static Guid AsGuid(ReadOnlySpan<byte> b)
    => BitConverter.IsLittleEndian ? new Guid(b) : 
        //Parse manually for big endian computers
        new Guid(
        BinaryPrimitives.ReadInt32LittleEndian(b),
        BinaryPrimitives.ReadInt16LittleEndian(b[4..]),
        b[6], b[7], b[8], b[9], b[10], b[12], b[13], b[14], b[15]);
}
