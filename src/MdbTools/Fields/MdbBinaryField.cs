// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.
//
// Based on code from libmdb (https://github.com/mdbtools/mdbtools)

using System.Collections.Immutable;

namespace MMKiwi.MdbTools.Fields;

public sealed class MdbBinaryField : MdbField<ImmutableArray<byte>>, IFieldTypes
{
    internal MdbBinaryField(MdbColumn column, bool isNull, ImmutableArray<byte> binaryValue)
        : base(column, isNull, binaryValue, false, 0, column.Length, AllowableTypes) { }

    public static ColumnType[] AllowableTypes => new ColumnType[] { ColumnType.Binary };

    public override ImmutableArray<byte> Value => BinaryValue;

    public sealed class Nullable : MdbField<ImmutableArray<byte>?>
    {
        internal Nullable(MdbColumn column, bool isNull, ImmutableArray<byte> binaryValue)
            : base(column, isNull, binaryValue, true, 0, column.Length, AllowableTypes) { }

        public override ImmutableArray<byte>? Value => IsNull ? null : BinaryValue;
    }
}