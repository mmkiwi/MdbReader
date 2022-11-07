// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.
//
// Based on code from libmdb (https://github.com/mdbtools/mdbtools)

using System.Collections.Immutable;

namespace MMKiwi.MdbTools.Fields;

public sealed class MdbDoubleField : MdbField<double>, IFieldTypes
{
    internal MdbDoubleField(MdbColumn column, bool isNull, ImmutableArray<byte> binaryValue)
        : base(column, isNull, binaryValue, false, 8, 8, AllowableTypes) { }

    public static ColumnType[] AllowableTypes => new ColumnType[] { ColumnType.Double };

    public override double Value => ConversionFunctions.AsDouble(BinaryValue.AsSpan());

    public sealed class Nullable : MdbField<double?>
    {
        internal Nullable(MdbColumn column, bool isNull, ImmutableArray<byte> binaryValue)
            : base(column, isNull, binaryValue, true, 8, 8, AllowableTypes) { }

        public override double? Value => IsNull ? null : ConversionFunctions.AsDouble(BinaryValue.AsSpan());
    }
}
