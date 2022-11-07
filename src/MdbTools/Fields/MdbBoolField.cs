// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.
//
// Based on code from libmdb (https://github.com/mdbtools/mdbtools)

using System.Collections.Immutable;

namespace MMKiwi.MdbTools.Fields;

public sealed class MdbBoolField : MdbField<bool>, IFieldTypes
{
    internal MdbBoolField(MdbColumn column, bool isNull) : base(column, isNull, ImmutableArray<byte>.Empty, true, 0, 0, AllowableTypes) { }

    public override bool Value => ConversionFunctions.AsBool(IsNull);

    public static ColumnType[] AllowableTypes => new ColumnType[] { ColumnType.Boolean };
}
