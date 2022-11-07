// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.
//
// Based on code from libmdb (https://github.com/mdbtools/mdbtools)

using System.Collections.Immutable;

namespace MMKiwi.MdbTools.Fields;

public sealed class MdbGuidField : MdbField<Guid>, IFieldTypes
{
    internal MdbGuidField(MdbColumn column, bool isNull, ImmutableArray<byte> binaryValue)
        : base(column, isNull, binaryValue, false, 16, 16, AllowableTypes) { }

    public static ColumnType[] AllowableTypes => new ColumnType[] { ColumnType.Guid };

    public override Guid Value => ConversionFunctions.AsGuid(BinaryValue.AsSpan());

    public sealed class Nullable : MdbField<Guid?>
    {
        internal Nullable(MdbColumn column, bool isNull, ImmutableArray<byte> binaryValue)
            : base(column, isNull, binaryValue, true, 16, 16, AllowableTypes) { }

        public override Guid? Value => IsNull ? null : ConversionFunctions.AsGuid(BinaryValue.AsSpan());
    }
}
