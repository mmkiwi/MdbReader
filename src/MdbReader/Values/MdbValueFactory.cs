// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.
//
// Based on code from libmdb (https://github.com/mdbtools/mdbtools)

using System.Collections.Immutable;

namespace MMKiwi.MdbReader.Values;

internal static class MdbValueFactory
{
    /// <summary>
    /// Create the appropriate <see cref="IMdbValue" /> for a specific <see cref="MdbColumnType" />.
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
        return (column.Type, column.Flags.HasFlag(MdbColumnFlags.CanBeNull)) switch
        {
            (MdbColumnType.Boolean, true) => new MdbBoolValue(column, isNull),
            (MdbColumnType.Boolean, false) => new MdbBoolValue(column, isNull),
            (MdbColumnType.Byte, false) => new MdbByteValue(column, isNull, binaryValue),
            (MdbColumnType.Byte, true) => new MdbByteValue.Nullable(column, isNull, binaryValue),
            (MdbColumnType.Int, false) => new MdbIntValue(column, isNull, binaryValue),
            (MdbColumnType.Int, true) => new MdbIntValue.Nullable(column, isNull, binaryValue),
            (MdbColumnType.LongInt, false) => new MdbLongIntValue(column, isNull, binaryValue),
            (MdbColumnType.LongInt, true) => new MdbLongIntValue.Nullable(column, isNull, binaryValue),
            (MdbColumnType.Currency, false) => new MdbCurrencyValue(column, isNull, binaryValue),
            (MdbColumnType.Currency, true) => new MdbCurrencyValue.Nullable(column, isNull, binaryValue),
            (MdbColumnType.Single, false) => new MdbSingleValue(column, isNull, binaryValue),
            (MdbColumnType.Single, true) => new MdbSingleValue.Nullable(column, isNull, binaryValue),
            (MdbColumnType.Double, false) => new MdbDoubleValue(column, isNull, binaryValue),
            (MdbColumnType.Double, true) => new MdbDoubleValue.Nullable(column, isNull, binaryValue),
            (MdbColumnType.DateTime, false) => new MdbDateTimeValue(column, isNull, binaryValue),
            (MdbColumnType.DateTime, true) => new MdbDateTimeValue.Nullable(column, isNull, binaryValue),
            (MdbColumnType.Binary, false) => new MdbBinaryValue(column, isNull, binaryValue),
            (MdbColumnType.Binary, true) => new MdbBinaryValue.Nullable(column, isNull, binaryValue),
            (MdbColumnType.Text, false) => new MdbStringValue(column, isNull, binaryValue),
            (MdbColumnType.Text, true) => new MdbStringValue.Nullable(column, isNull, binaryValue),
            (MdbColumnType.OLE, false) => new MdbOleValue(reader, column, isNull, binaryValue),
            (MdbColumnType.OLE, true) => new MdbOleValue(reader, column, isNull, binaryValue),
            (MdbColumnType.Memo, false) => new MdbMemoValue(reader, column, isNull, binaryValue),
            (MdbColumnType.Memo, true) => new MdbMemoValue(reader, column, isNull, binaryValue),
            (MdbColumnType.Guid, false) => new MdbGuidValue(column, isNull, binaryValue),
            (MdbColumnType.Guid, true) => new MdbGuidValue.Nullable(column, isNull, binaryValue),
            _ => throw new NotImplementedException()
        };
    }
}
