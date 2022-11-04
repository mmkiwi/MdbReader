// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.
//
// Based on code from libmdb (https://github.com/mdbtools/mdbtools)

using System.Diagnostics;
using MMKiwi.MdbTools.Mutable;

namespace MMKiwi.MdbTools;

[DebuggerDisplay("MdbColumn {Name} {Type}")]
public sealed record class MdbColumn
{
    internal MdbColumn(MdbBuilder.Column column)
    {
        Type = column.Type;
        NumInclDeleted = column.NumInclDeleted;
        OffsetVariable = column.OffsetVariable;
        ColNum = column.ColNum;
        SortOrder = column.SortOrder;
        Misc = column.Misc;
        Flags = column.Flags;
        Offset = column.Offset;
        Length = column.Length;
        Name = column.Name!;
    }

    public ColumnType Type { get; }
    public ushort NumInclDeleted { get; }
    public ushort OffsetVariable { get; }
    public ushort ColNum { get; }
    public ushort SortOrder { get; }
    public ushort Misc { get; }
    public ColumnFlags Flags { get; }
    public ushort Offset { get; }
    public ushort Length { get; }
    public string Name { get; }
}