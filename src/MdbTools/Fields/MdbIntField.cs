// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.
//
// Based on code from libmdb (https://github.com/mdbtools/mdbtools)

using System.Collections.Immutable;

namespace MMKiwi.MdbTools.Fields;

public sealed class MdbIntField : MdbField<short>, IFieldTypes
{
    internal MdbIntField(MdbColumn column, bool isNull, ImmutableArray<byte> binaryValue) 
        : base(column, isNull, binaryValue, false, 2, 2, AllowableTypes) { }

    public static ColumnType[] AllowableTypes => new ColumnType[] { ColumnType.Int };

    public override short Value => ConversionFunctions.AsShort(BinaryValue.AsSpan());

    public sealed class Nullable : MdbField<int?>
    {
        internal Nullable(MdbColumn column, bool isNull, ImmutableArray<byte> binaryValue) 
            : base(column, isNull, binaryValue, true, 2, 2, AllowableTypes) { }

        public override int? Value => IsNull ? null : ConversionFunctions.AsShort(BinaryValue.AsSpan());
    }
}
