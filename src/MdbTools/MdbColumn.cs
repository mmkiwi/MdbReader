// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.
//
// Based on code from libmdb (https://github.com/mdbtools/mdbtools)

using System.Diagnostics;
using System.Text;

using MMKiwi.MdbTools.Mutable;

namespace MMKiwi.MdbTools;

/// <summary>
/// Represents a column in an Access Database
/// </summary>
[DebuggerDisplay("MdbColumn {Name} {Type}")]
public sealed record class MdbColumn
{
    internal MdbColumn(MdbBuilder.Column column)
    {
        Type = column.Type;
        IndexIncludingDeleted = column.NumInclDeleted;
        OffsetVariable = column.OffsetVariable;
        Index = column.ColNum;
        SortOrder = column.SortOrder;
        Locale = column.Locale;
        Flags = column.Flags;
        OffsetFixed = column.OffsetFixed;
        Length = column.Length;
        Name = column.Name!;
        Encoding = column.Encoding;
    }

    /// <summary>
    /// The data type of the column.
    /// </summary>
    public ColumnType Type { get; }

    /// <summary>
    /// The index of the column (including deleted columns)
    /// </summary>
    internal ushort IndexIncludingDeleted { get; }
    /// <summary>
    /// The offset from the start of the row to get variable-width columns
    /// </summary>
    internal ushort OffsetVariable { get; }

    /// <summary>
    /// The index of the column in the underlying database (0 = the first column)
    /// </summary>
    public int Index { get; }

    /// <summary>
    /// The column sort order (may not match the order in the underlying database file)
    /// </summary>
    public ushort SortOrder { get; }

    /// <summary>
    /// The locale for the column
    /// </summary>
    public ushort Locale { get; }

    /// <summary>
    /// The flags set on the column. Use <see cref="Enum.HasFlag(Enum)" /> to see if the flag is set.
    /// </summary>
    public ColumnFlags Flags { get; }

    /// <summary>
    /// The offset for the fixed-length columns
    /// </summary>
    internal ushort OffsetFixed { get; }

    /// <summary>
    /// The length of the column for fixed-length columns, or the max length for variable-length columns. For long
    /// columns (<see cref="ColumnType.Memo" /> or <see cref="ColumnType.OLE" />), the length is zero.
    /// </summary>
    public ushort Length { get; }

    /// <summary>
    /// The name of the column
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// The encoding of the column.
    /// </summary>
    public Encoding Encoding { get; }
}