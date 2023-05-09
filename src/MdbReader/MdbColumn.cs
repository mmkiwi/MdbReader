// This file is part of MdbReader. Licensed under the LGPL version 2.0.
// You should have received a coy of the GNU LGPL version along with this
// program. If not, see https://www.gnu.org/licenses/old-licenses/lgpl-2.0.html
//
// Copyright Micah Makaiwi.
// Based on code from libmdb (https://github.com/mdbtools/mdbtools)

using MMKiwi.MdbReader.Helpers;
using MMKiwi.MdbReader.Schema;

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
    /// The SQL Type of this column.
    /// </summary>
    /// <remarks>
    /// See <seealso href="https://learn.microsoft.com/en-us/sql/odbc/microsoft/microsoft-access-data-types?view=sql-server-ver16" />
    /// for a list of all SQL data types and their equivalent names in the Access GUI.
    /// </remarks>
    public string SqlTypeName => (Type, Flags.HasFlag(MdbColumnFlags.FixedLength)) switch
    {
        (MdbColumnType.Boolean, _) => "BIT",
        (MdbColumnType.Byte, _) => "UNSIGNED BYTE",
        (MdbColumnType.Int, _) => "SMALLINT",
        (MdbColumnType.LongInt, _) => "INTEGER",
        (MdbColumnType.Currency, _) => "MONEY",
        (MdbColumnType.Single, _) => "REAL",
        (MdbColumnType.Double, _) => "FLOAT",
        (MdbColumnType.DateTime, _) => "DATETIME",
        (MdbColumnType.Binary, false) => "BINARY",
        (MdbColumnType.Binary, true) => "VARBINARY",
        (MdbColumnType.Text, false) => "CHAR",
        (MdbColumnType.Text, true) => "VARCHAR",
        (MdbColumnType.OLE,_) => "LONGBINARY",
        (MdbColumnType.Memo,_) => "LONGTEXT",
        (MdbColumnType.Guid,_) => "GUID",
        (MdbColumnType.Numeric,_) => "NUMERIC",
        _ => "UNKNOWN"
    };

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
    public int Length { get; }

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
        public byte[]? Misc { get; set; }
        public MdbColumnFlags Flags { get; set; }
        public ushort OffsetFixed { get; set; }
        public ushort Length { get; set; }
        public string? Name { get; set; }
        public int UsedPages { get; set; }
        public int FreePages { get; set; }
        public Encoding? OverrideEncoding { get; internal set; }

        public MdbColumn Build(Jet3Reader reader)
        {
            if (Name == null || Misc == null)
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
            if (Misc == null)
                throw new InvalidOperationException("All values must be initialized before building MdbColumn");
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
                        MdbBinary.ReadUInt16LittleEndian(Misc.AsSpan()[..2]), reader.Db.Encoding);
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
