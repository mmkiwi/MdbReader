// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.
//
// Based on code from libmdb (https://github.com/mdbtools/mdbtools)

using System.Collections.Immutable;

namespace MMKiwi.MdbTools.Fields;

public class MdbMemoField : MdbLongValField<StreamReader?>
{

    internal MdbMemoField(Jet3Reader reader, MdbColumn column, bool isNull, ImmutableArray<byte> binaryValue)
        : base(reader, column, isNull, binaryValue, AllowableTypes)
    { }

    public override StreamReader? Value => IsNull ? null : new StreamReader(new MdbLongValStream(new Jet3Reader.LvalStream(Reader, Column, BinaryValue)), Column.Encoding);
    public static ColumnType[] AllowableTypes => new ColumnType[] { ColumnType.Memo };

}
