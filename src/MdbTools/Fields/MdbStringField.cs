// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.
//
// Based on code from libmdb (https://github.com/mdbtools/mdbtools)

using System.Collections.Immutable;

namespace MMKiwi.MdbTools.Fields;

public sealed class MdbStringField : MdbField<string>, IFieldTypes
{
    //Not saving the string to the BinaryValue buffer in order to not duplicate it
    internal MdbStringField(MdbColumn column, bool isNull, ImmutableArray<byte> binaryValue)
        : base(column, isNull, ImmutableArray<byte>.Empty, false, 0, column.Length, AllowableTypes)
    {
        Value = ConversionFunctions.AsString(Column.Encoding, binaryValue.AsSpan());
    }

    public static ColumnType[] AllowableTypes => new ColumnType[] { ColumnType.Text };

    public override string Value { get; }

    public sealed class Nullable : MdbField<string?>
    {
        internal Nullable(MdbColumn column, bool isNull, ImmutableArray<byte> binaryValue)
            : base(column, isNull, ImmutableArray<byte>.Empty, true, 0, column.Length, AllowableTypes)
        {
            Value = IsNull ? null : ConversionFunctions.AsString(Column.Encoding, binaryValue.AsSpan());
        }

        public override string? Value { get; }
    }
}
