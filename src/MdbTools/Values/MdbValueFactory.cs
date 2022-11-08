// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.
//
// Based on code from libmdb (https://github.com/mdbtools/mdbtools)

using System.Collections.Immutable;

namespace MMKiwi.MdbTools.Values;

internal static class MdbValueFactory
{
    /// <summary>
    /// Create the appropriate <see cref="IMdbValue" /> for a specific <see cref="ColumnType" />.
    /// </summary>
    /// <param name="reader">
    /// The <see cref="Jet3Reader" /> instance that is creating this field. (Required for 
    /// <see cref="MdbMemoValue" /> and <see cref="MdbOleValue" />). 
    /// </param>
    /// <param name="column"></param>
    /// <param name="isNull"></param>
    /// <param name="binaryValue"></param>
    /// <returns></returns>
    public static IMdbValue CreateValue(Jet3Reader reader, MdbColumn column, bool isNull, ImmutableArray<byte> binaryValue)
    {
        return (column.Type, column.Flags.HasFlag(ColumnFlags.CanBeNull)) switch
        {
            (ColumnType.Boolean, true) => new MdbBoolValue(column, isNull),
            (ColumnType.Boolean, false) => new MdbBoolValue(column, isNull),
            (ColumnType.Byte, false) => new MdbByteValue(column, isNull, binaryValue),
            (ColumnType.Byte, true) => new MdbByteValue.Nullable(column, isNull, binaryValue),
            (ColumnType.Int, false) => new MdbIntValue(column, isNull, binaryValue),
            (ColumnType.Int, true) => new MdbIntValue.Nullable(column, isNull, binaryValue),
            (ColumnType.LongInt, false) => new MdbLongIntValue(column, isNull, binaryValue),
            (ColumnType.LongInt, true) => new MdbLongIntValue.Nullable(column, isNull, binaryValue),
            (ColumnType.Currency, false) => new MdbCurrencyValue(column, isNull, binaryValue),
            (ColumnType.Currency, true) => new MdbCurrencyValue.Nullable(column, isNull, binaryValue),
            (ColumnType.Single, false) => new MdbSingleValue(column, isNull, binaryValue),
            (ColumnType.Single, true) => new MdbSingleValue.Nullable(column, isNull, binaryValue),
            (ColumnType.Double, false) => new MdbDoubleValue(column, isNull, binaryValue),
            (ColumnType.Double, true) => new MdbDoubleValue.Nullable(column, isNull, binaryValue),
            (ColumnType.DateTime, false) => new MdbDateTimeValue(column, isNull, binaryValue),
            (ColumnType.DateTime, true) => new MdbDateTimeValue.Nullable(column, isNull, binaryValue),
            (ColumnType.Binary, false) => new MdbBinaryValue(column, isNull, binaryValue),
            (ColumnType.Binary, true) => new MdbBinaryValue.Nullable(column, isNull, binaryValue),
            (ColumnType.Text, false) => new MdbStringValue(column, isNull, binaryValue),
            (ColumnType.Text, true) => new MdbStringValue.Nullable(column, isNull, binaryValue),
            (ColumnType.OLE, false) => new MdbOleValue(reader, column, isNull, binaryValue),
            (ColumnType.OLE, true) => new MdbOleValue(reader, column, isNull, binaryValue),
            (ColumnType.Memo, false) => new MdbMemoValue(reader, column, isNull, binaryValue),
            (ColumnType.Memo, true) => new MdbMemoValue(reader, column, isNull, binaryValue),
            (ColumnType.Guid, false) => new MdbGuidValue(column, isNull, binaryValue),
            (ColumnType.Guid, true) => new MdbGuidValue.Nullable(column, isNull, binaryValue),
            _ => throw new NotImplementedException()
        };
    }
}
