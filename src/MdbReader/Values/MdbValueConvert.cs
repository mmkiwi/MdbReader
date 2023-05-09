// This file is part of MdbReader. Licensed under the LGPL version 2.0.
// You should have received a coy of the GNU LGPL version along with this
// program. If not, see https://www.gnu.org/licenses/old-licenses/lgpl-2.0.html
//
// Copyright Micah Makaiwi.
// Based on code from libmdb (https://github.com/mdbtools/mdbtools)

using MMKiwi.MdbReader.Helpers;

namespace MMKiwi.MdbReader.Values;

/// <summary>
/// Static functions to convert raw Access bytes into a CLR type. Has not been tested on big-endian 
/// systems but should work on them.
/// </summary>
internal static class ConversionFunctions
{
    public static bool AsBool(bool isNull) => isNull;
    public static byte AsByte(ReadOnlySpan<byte> bytes) => bytes[0];
    public static short AsShort(ReadOnlySpan<byte> bytes)
        => MdbBinary.ReadInt16LittleEndian(bytes);

    public static int AsInt(ReadOnlySpan<byte> bytes)
        => MdbBinary.ReadInt32LittleEndian(bytes);
    public static decimal AsCurrency(ReadOnlySpan<byte> bytes)
        => new decimal(MdbBinary.ReadInt64LittleEndian(bytes)) / 10000;

    public static float AsSingle(ReadOnlySpan<byte> bytes)
        => MdbBinary.ReadSingleLittleEndian(bytes);

    public static double AsDouble(ReadOnlySpan<byte> bytes)
        => MdbBinary.ReadDoubleLittleEndian(bytes);

    public static DateTime AsDateTime(ReadOnlySpan<byte> bytes)
        // stored as fractional days since 1/1/1900
        => DateTime.FromOADate(MdbBinary.ReadDoubleLittleEndian(bytes));

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
        MdbBinary.ReadInt32LittleEndian(b),
        MdbBinary.ReadInt16LittleEndian(b[4..]),
        b[6], b[7], b[8], b[9], b[10], b[12], b[13], b[14], b[15]);
}
