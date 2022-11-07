// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.
//
// Based on code from libmdb (https://github.com/mdbtools/mdbtools)

using System.Collections.Immutable;

namespace MMKiwi.MdbTools.Fields;

public sealed class MdbByteField : MdbField<byte>, IFieldTypes
{
    internal MdbByteField(MdbColumn column, bool isNull, ImmutableArray<byte> binaryValue) 
        : base(column, isNull, binaryValue, false, 1, 1, AllowableTypes) { }

    public static ColumnType[] AllowableTypes => new ColumnType[] { ColumnType.Byte };

    public override byte Value => ConversionFunctions.AsByte(BinaryValue.AsSpan());

    public sealed class Nullable : MdbField<byte?>
    {
        internal Nullable(MdbColumn column, bool isNull, ImmutableArray<byte> binaryValue) 
            : base(column, isNull, binaryValue, true, 1, 1, AllowableTypes) { }

        public override byte? Value => IsNull ? null : ConversionFunctions.AsByte(BinaryValue.AsSpan());
    }
}
