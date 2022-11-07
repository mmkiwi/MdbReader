// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.
//
// Based on code from libmdb (https://github.com/mdbtools/mdbtools)

using System.Collections.Immutable;

namespace MMKiwi.MdbTools.Fields;

public sealed class MdbFloatField : MdbField<float>, IFieldTypes
{
    internal MdbFloatField(MdbColumn column, bool isNull, ImmutableArray<byte> binaryValue)
        : base(column, isNull, binaryValue, false, 4, 4, AllowableTypes) { }

    public static ColumnType[] AllowableTypes => new ColumnType[] { ColumnType.Float };

    public override float Value => ConversionFunctions.AsFloat(BinaryValue.AsSpan());

    public sealed class Nullable : MdbField<float?>
    {
        internal Nullable(MdbColumn column, bool isNull, ImmutableArray<byte> binaryValue)
            : base(column, isNull, binaryValue, true, 4, 4, AllowableTypes) { }

        public override float? Value => IsNull ? null : ConversionFunctions.AsFloat(BinaryValue.AsSpan());
    }
}
