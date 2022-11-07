// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.
//
// Based on code from libmdb (https://github.com/mdbtools/mdbtools)

using System.Collections.Immutable;
using System.Configuration;

namespace MMKiwi.MdbTools.Fields;

public sealed class MdbLongIntField : MdbField<int>, IFieldTypes
{
    internal MdbLongIntField(MdbColumn column, bool isNull, ImmutableArray<byte> binaryValue)
        : base(column, isNull, binaryValue, false, 4, 4, AllowableTypes) { }

    public static ColumnType[] AllowableTypes => new ColumnType[] { ColumnType.LongInt };

    public override int Value => ConversionFunctions.AsInt(BinaryValue.AsSpan());

    public sealed class Nullable : MdbField<int?>
    {
        internal Nullable(MdbColumn column, bool isNull, ImmutableArray<byte> binaryValue) 
            : base(column, isNull, binaryValue, true, 4, 4, AllowableTypes) { }

        public override int? Value => IsNull ? null : ConversionFunctions.AsInt(BinaryValue.AsSpan());
    }
}
