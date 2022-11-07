// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.
//
// Based on code from libmdb (https://github.com/mdbtools/mdbtools)

using System.Collections.Immutable;

namespace MMKiwi.MdbTools.Fields;

public sealed class MdbMoneyField : MdbField<decimal>, IFieldTypes
{
    internal MdbMoneyField(MdbColumn column, bool isNull, ImmutableArray<byte> binaryValue)
        : base(column, isNull, binaryValue, false, 8, 8, AllowableTypes) { }

    public static ColumnType[] AllowableTypes => new ColumnType[] { ColumnType.Money };

    public override decimal Value => ConversionFunctions.AsMoney(BinaryValue.AsSpan());

    public sealed class Nullable : MdbField<decimal?>
    {
        internal Nullable(MdbColumn column, bool isNull, ImmutableArray<byte> binaryValue)
            : base(column, isNull, binaryValue, true, 8, 8, AllowableTypes) { }

        public override decimal? Value => IsNull ? null : ConversionFunctions.AsMoney(BinaryValue.AsSpan());
    }
}