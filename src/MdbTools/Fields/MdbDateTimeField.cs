// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.
//
// Based on code from libmdb (https://github.com/mdbtools/mdbtools)

using System.Collections.Immutable;

namespace MMKiwi.MdbTools.Fields;

public sealed class MdbDateTimeField : MdbField<DateTime>, IFieldTypes
{
    internal MdbDateTimeField(MdbColumn column, bool isNull, ImmutableArray<byte> binaryValue)
        : base(column, isNull, binaryValue, false, 8, 8, AllowableTypes) { }

    public static ColumnType[] AllowableTypes => new ColumnType[] { ColumnType.DateTime };

    public override DateTime Value => ConversionFunctions.AsDateTime(BinaryValue.AsSpan());

    public sealed class Nullable : MdbField<DateTime?>
    {
        internal Nullable(MdbColumn column, bool isNull, ImmutableArray<byte> binaryValue)
            : base(column, isNull, binaryValue, true, 8, 8, AllowableTypes) { }

        public override DateTime? Value => IsNull ? null : ConversionFunctions.AsDateTime(BinaryValue.AsSpan());
    }
}
