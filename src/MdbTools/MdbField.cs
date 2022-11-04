// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.
//
// Based on code from libmdb (https://github.com/mdbtools/mdbtools)

using System.Buffers.Binary;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;

namespace MMKiwi.MdbTools;

[DebuggerDisplay("{Column.Name}: {Value} ({Column.Type})")]
public sealed record class MdbField
{
    public MdbField(MdbColumn column, bool isNull, ImmutableArray<byte> binaryValue)
    {
        Column = column;
        IsNull = isNull;
        BinaryValue = binaryValue;
    }

    public MdbColumn Column { get; }
    public bool IsNull { get; }
    public ImmutableArray<byte> BinaryValue { get; }

    public object? Value => (Column.Type) switch
    {
        ColumnType.Boolean => AsBoolean(),
        ColumnType.Byte => IsNull ? null : AsByte(),
        ColumnType.Int => IsNull ? null : AsInt16(),
        ColumnType.LongInt => IsNull ? null : AsInt32(),
        ColumnType.Money => IsNull ? null : AsDecimal(),
        ColumnType.Float => IsNull ? null : AsFloat(),
        ColumnType.Double => IsNull ? null : AsDouble(),
        ColumnType.DateTime => IsNull ? null : AsDateTime(),
        ColumnType.Binary => IsNull ? null : AsBinary(),
        ColumnType.Text => IsNull ? null : AsStringNotNull(),
        ColumnType.OLE => IsNull ? null : AsBinary(),
        ColumnType.Memo => IsNull ? null : AsStringNotNull(),
        ColumnType.Guid => IsNull ? null : AsGuid(),
        ColumnType.Numeric => throw new NotImplementedException(), //Jet4 and newer
        _ => throw new NotImplementedException(),
    };

    public decimal AsDecimal()
        // Money is a long int times 10,000 (i.e. 100.50 is 1005000)
        => ConfirmTypeAndCastNotNull(ColumnType.Money, (b) => new Decimal(BinaryPrimitives.ReadInt64LittleEndian(b)) / 10000);

    public Guid AsGuid()
        => ConfirmTypeAndCastNotNull(ColumnType.Guid, (b) => new Guid(
            BinaryPrimitives.ReadInt32LittleEndian(b),
            BinaryPrimitives.ReadInt16LittleEndian(b[4..]),
            b[6], b[7], b[8], b[9], b[10], b[12], b[13], b[14], b[15]));

    public DateTime AsDateTime()
         => ConfirmTypeAndCastNotNull(ColumnType.DateTime, ToDateTime);

    public DateTime? AsNullableDateTime()
        => ConfirmTypeAndCast(ColumnType.DateTime, ToDateTime);

    public bool AsBoolean()
    {
        // Bool is implemented using the null bitmask in the row and not by data
        return IsNull;
    }

    public byte AsByte()
        => ConfirmTypeAndCastNotNull(ColumnType.DateTime, (b) => b[0]);

    public byte? AsNullableByte()
        => ConfirmTypeAndCast(ColumnType.DateTime, (b) => b[0]);

    public short AsInt16()
        => ConfirmTypeAndCastNotNull(ColumnType.Int, BinaryPrimitives.ReadInt16LittleEndian);

    public short? AsNullableInt16()
        => ConfirmTypeAndCast(ColumnType.Int, BinaryPrimitives.ReadInt16LittleEndian);

    public int AsInt32()
        => ConfirmTypeAndCastNotNull(ColumnType.LongInt, BinaryPrimitives.ReadInt32LittleEndian);

    public int? AsNullableInt32()
        => ConfirmTypeAndCast(ColumnType.LongInt, BinaryPrimitives.ReadInt32LittleEndian);

    public float AsFloat()
        => ConfirmTypeAndCastNotNull(ColumnType.Float, BinaryPrimitives.ReadSingleLittleEndian);

    public float? AsNullableFloat()
        => ConfirmTypeAndCast(ColumnType.Float, BinaryPrimitives.ReadSingleLittleEndian);

    public double AsDouble()
        => ConfirmTypeAndCastNotNull(ColumnType.Double, BinaryPrimitives.ReadDoubleLittleEndian);

    public double? AsNullableDouble()
        => ConfirmTypeAndCast(ColumnType.Double, BinaryPrimitives.ReadDoubleLittleEndian);

    public string? AsString()
        => Column.Type switch
        {
            ColumnType.Text => ConfirmTypeAndCast(ColumnType.Text, Encoding.GetEncoding(1252).GetString),
            _ => ConfirmTypeAndCast(ColumnType.Memo, Encoding.GetEncoding(1252).GetString)
        };

    public string AsStringNotNull()
        => Column.Type switch
        {
            ColumnType.Text => ConfirmTypeAndCastNotNull(ColumnType.Text, Encoding.GetEncoding(1252).GetString),
            _ => ConfirmTypeAndCastNotNull(ColumnType.Memo, Encoding.GetEncoding(1252).GetString)
        };

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private T ConfirmTypeAndCastNotNull<T>(ColumnType type, ConversionFunctionStruct<T> conversionFunc)
        where T : struct
    {
        if (Column == null || BinaryValue == null)
            throw new InvalidOperationException("Cannot get value of initialized field");
        if (Column.Type != type)
            throw new InvalidCastException($"Could not convert field value type {Column.Type} to {type}");
        if (IsNull)
            throw new InvalidOperationException("Cannot convert null db value to non-nullable type");

        return conversionFunc(BinaryValue.AsSpan());
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private T? ConfirmTypeAndCast<T>(ColumnType type, ConversionFunctionStruct<T> conversionFunc)
        where T : struct
    {
        if (Column == null || BinaryValue == null)
            throw new InvalidOperationException("Cannot get value of initialized field");
        if (Column.Type != type)
            throw new InvalidCastException($"Could not convert field value type {Column.Type} to {type}");

        return IsNull ? default : conversionFunc(BinaryValue.AsSpan());
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private T? ConfirmTypeAndCast<T>(ColumnType type, ConversionFunction<T> conversionFunc)
        where T : class
    {
        if (Column == null || BinaryValue == null)
            throw new InvalidOperationException("Cannot get value of initialized field");
        if (Column.Type != type)
            throw new InvalidCastException($"Could not convert field value type {Column.Type} to {type}");

        return IsNull ? default : conversionFunc(BinaryValue.AsSpan());
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private T ConfirmTypeAndCastNotNull<T>(ColumnType type, ConversionFunction<T> conversionFunc)
        where T : class
    {
        if (Column == null || BinaryValue == null)
            throw new InvalidOperationException("Cannot get value of initialized field");
        if (Column.Type != type)
            throw new InvalidCastException($"Could not convert field value type {Column.Type} to {type}");
        if (IsNull)
            throw new InvalidOperationException("Cannot convert null db value to non-nullable type");

        return conversionFunc(BinaryValue.AsSpan());
    }


    private delegate T ConversionFunction<T>(ReadOnlySpan<byte> inArray)
        where T : class;

    private delegate T ConversionFunctionStruct<T>(ReadOnlySpan<byte> inArray)
        where T : struct;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static DateTime ToDateTime(ReadOnlySpan<byte> binaryValue) =>
        // stored as fractional days since 1/1/1900
        new DateTime(1900, 1, 1).AddDays(BinaryPrimitives.ReadDoubleLittleEndian(binaryValue));

    public byte[] AsBinary()
        => Column.Type switch
        {
            ColumnType.Binary => ConfirmTypeAndCast(ColumnType.Binary, v => v.ToArray())!,
            _ => ConfirmTypeAndCast(ColumnType.OLE, v => v.ToArray())!
        };
}