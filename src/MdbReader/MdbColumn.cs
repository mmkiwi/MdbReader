// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.
//
// Based on code from libmdb (https://github.com/mdbtools/mdbtools)

using System.Collections.Immutable;
using System.Diagnostics;

using MMKiwi.MdbReader.Helpers;

namespace MMKiwi.MdbReader;

/// <summary>
/// Represents a column in an Access Database
/// </summary>
[DebuggerDisplay("MdbColumn {Name} {Type}")]
public sealed record class MdbColumn
{
    private MdbColumn(MdbColumnType type, ushort indexIncludingDeleted, ushort offsetVariable, int index, MdbMiscColumnInfo columnInfo, MdbColumnFlags flags, ushort offsetFixed, ushort length, string name)
    {
        Type = type;
        IndexIncludingDeleted = indexIncludingDeleted;
        OffsetVariable = offsetVariable;
        Index = index;
        ColumnInfo = columnInfo;
        Flags = flags;
        OffsetFixed = offsetFixed;
        Length = length;
        Name = name;
    }

    /// <summary>
    /// The data type of the column.
    /// </summary>
    public MdbColumnType Type { get; }

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
    /// Miscellaneous column info. This will be a <see cref="MdbDecimalColumnInfo"/>, <see cref="MdbTextColumnInfo"/>, or <see cref="MdbComplexColumnInfo"/>.
    /// </summary>
    public MdbMiscColumnInfo ColumnInfo { get; }

    /// <summary>
    /// The flags set on the column. Use <see cref="Enum.HasFlag(Enum)" /> to see if the flag is set.
    /// </summary>
    public MdbColumnFlags Flags { get; }

    /// <summary>
    /// The offset for the fixed-length columns
    /// </summary>
    internal ushort OffsetFixed { get; }

    /// <summary>
    /// The length of the column for fixed-length columns, or the max length for variable-length columns. For long
    /// columns (<see cref="MdbColumnType.Memo" /> or <see cref="MdbColumnType.OLE" />), the length is zero.
    /// </summary>
    public ushort Length { get; }

    /// <summary>
    /// The name of the column
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// The mutable builder for <see cref="MdbColumn" />
    /// </summary>
    internal class Builder
    {

        public MdbColumnType Type { get; set; }
        public ushort NumInclDeleted { get; set; }
        public ushort OffsetVariable { get; set; }
        public ushort ColNum { get; set; }
        public ImmutableArray<byte> Misc { get; set; }
        public MdbColumnFlags Flags { get; set; }
        public ushort OffsetFixed { get; set; }
        public ushort Length { get; set; }
        public string? Name { get; set; }
        public int UsedPages { get; set; }
        public int FreePages { get; set; }

        public MdbColumn Build(Jet3Reader reader)
        {
            if (Name == null)
                throw new InvalidOperationException("All values must be initialized before building MdbColumn");

            return new MdbColumn(
                type: Type,
                indexIncludingDeleted: NumInclDeleted,
                offsetVariable: OffsetVariable,
                index: ColNum,
                columnInfo: BuildColumnInfo(reader),
                flags: Flags,
                offsetFixed: OffsetFixed,
                length: Length,
                name: Name);
        }

        private MdbMiscColumnInfo BuildColumnInfo(Jet3Reader reader)
        {
            if (reader.Db.JetVersion == JetVersion.Jet3)
            {
                if (Type is MdbColumnType.Text or MdbColumnType.Memo)
                {
                    return new MdbTextColumnInfo(reader, 
                        MdbBinary.ReadUInt16LittleEndian(Misc.AsSpan()[..2]),
                        MdbBinary.ReadUInt16LittleEndian(Misc.AsSpan()[2..4]));
                }
                else
                {
                    return new MdbDecimalColumnInfo(Misc[1], Misc[2]);
                }
            }
            else
            {
                if (Type is MdbColumnType.Text or MdbColumnType.Memo)
                {
                    return new MdbTextColumnInfo(reader, 
                        MdbBinary.ReadUInt16LittleEndian(Misc.AsSpan()[..2]));
                }
                else if (Type is MdbColumnType.Complex)
                {
                    return new MdbComplexColumnInfo(MdbBinary.ReadUInt32LittleEndian(Misc.AsSpan()));
                }
                else
                {
                    return new MdbDecimalColumnInfo(Misc[0], Misc[1]);
                }
            }
        }
    }
}
