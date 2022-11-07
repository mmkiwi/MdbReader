// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.
//
// Based on code from libmdb (https://github.com/mdbtools/mdbtools)

using System.Collections.Immutable;

namespace MMKiwi.MdbTools.Fields;

public class MdbOleField : MdbLongValField<MdbLongValStream?>, IFieldTypes
{
    internal MdbOleField(Jet3Reader reader, MdbColumn column, bool isNull, ImmutableArray<byte> binaryValue)
        : base(reader, column, isNull, binaryValue, AllowableTypes)
    {
    }

    public override MdbLongValStream? Value => IsNull ? null : new MdbLongValStream(new Jet3Reader.LvalStream(Reader, Column, BinaryValue));

    public static ColumnType[] AllowableTypes => new ColumnType[] { ColumnType.OLE };
}
