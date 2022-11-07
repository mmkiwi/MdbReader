// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.
//
// Based on code from libmdb (https://github.com/mdbtools/mdbtools)

using System.Collections.Immutable;

namespace MMKiwi.MdbTools.Fields;

public static class MdbFieldFactory
{
    public static IMdbField CreateField(Jet3Reader reader, MdbColumn column, bool isNull, ImmutableArray<byte> binaryValue)
    {
        return (column.Type, column.Flags.HasFlag(ColumnFlags.CanBeNull)) switch
        {
            (ColumnType.Boolean, true) => new MdbBoolField(column, isNull),
            (ColumnType.Boolean, false) => new MdbBoolField(column, isNull),
            (ColumnType.Byte, false) => new MdbByteField(column, isNull, binaryValue),
            (ColumnType.Byte, true) => new MdbByteField.Nullable(column, isNull, binaryValue),
            (ColumnType.Int, false) => new MdbIntField(column, isNull, binaryValue),
            (ColumnType.Int, true) => new MdbIntField.Nullable(column, isNull, binaryValue),
            (ColumnType.LongInt, false) => new MdbLongIntField(column, isNull, binaryValue),
            (ColumnType.LongInt, true) => new MdbLongIntField.Nullable(column, isNull, binaryValue),
            (ColumnType.Money, false) => new MdbMoneyField(column, isNull, binaryValue),
            (ColumnType.Money, true) => new MdbMoneyField.Nullable(column, isNull, binaryValue),
            (ColumnType.Float, false) => new MdbFloatField(column, isNull, binaryValue),
            (ColumnType.Float, true) => new MdbFloatField.Nullable(column, isNull, binaryValue),
            (ColumnType.Double, false) => new MdbDoubleField(column, isNull, binaryValue),
            (ColumnType.Double, true) => new MdbDoubleField.Nullable(column, isNull, binaryValue),
            (ColumnType.DateTime, false) => new MdbDateTimeField(column, isNull, binaryValue),
            (ColumnType.DateTime, true) => new MdbDateTimeField.Nullable(column, isNull, binaryValue),
            (ColumnType.Binary, false) => new MdbBinaryField(column, isNull, binaryValue),
            (ColumnType.Binary, true) => new MdbBinaryField.Nullable(column, isNull, binaryValue),
            (ColumnType.Text, false) => new MdbStringField(column, isNull, binaryValue),
            (ColumnType.Text, true) => new MdbStringField.Nullable(column, isNull, binaryValue),
            (ColumnType.OLE, false) => new MdbOleField(reader, column, isNull, binaryValue),
            (ColumnType.OLE, true) => new MdbOleField(reader, column, isNull, binaryValue),
            (ColumnType.Memo, false) => new MdbMemoField(reader, column, isNull, binaryValue),
            (ColumnType.Memo, true) => new MdbMemoField(reader, column, isNull, binaryValue),
            (ColumnType.Guid, false) => new MdbGuidField(column, isNull, binaryValue),
            (ColumnType.Guid, true) => new MdbGuidField.Nullable(column, isNull, binaryValue),
            _ => throw new NotImplementedException()
        };
    }
}
