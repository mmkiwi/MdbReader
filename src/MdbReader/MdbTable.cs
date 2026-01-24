// This file is part of MdbReader. Licensed under the LGPL version 2.0.
// You should have received a coy of the GNU LGPL version along with this
// program. If not, see https://www.gnu.org/licenses/old-licenses/lgpl-2.0.html
//
// Copyright Micah Makaiwi.
// Based on code from libmdb (https://github.com/mdbtools/mdbtools)

using System.Diagnostics.CodeAnalysis;

using MMKiwi.MdbReader.Schema;

namespace MMKiwi.MdbReader;

/// <summary>
/// Represents a table in the Access database.
/// </summary>
[DebuggerDisplay("MdbTable {Name}")]
public sealed record class MdbTable
{
    private MdbTable(string name, int numRows, int nextAutoNum, MdbTableType tableType, ushort maxCols, ushort numVarCols, ushort numColumns, int numIndexes, int numRealIndexes, int usedPagesPtr, int freePagesPtr, int firstPage, ImmutableArray<MdbColumn> columns, Jet3Reader reader, ImmutableArray<MdbIndex> indexes, ImmutableArray<MdbRealIndex> realIndices)
    {
        Name = name;
        NumRows = numRows;
        NextAutoNum = nextAutoNum;
        TableType = tableType;
        MaxCols = maxCols;
        NumVarCols = numVarCols;
        NumColumns = numColumns;
        NumIndexes = numIndexes;
        NumRealIndexes = numRealIndexes;
        UsedPagesPtr = usedPagesPtr;
        FreePagesPtr = freePagesPtr;
        FirstPage = firstPage;
        Columns = columns;
        Indexes = indexes;
        _reader = reader;
        RealIndices = realIndices;
    }

    /// <summary>
    /// An enumerator for all the rows in the database.
    /// </summary>
    public MdbRows Rows => _rows ??= new MdbRows(_reader, this);

    internal ImmutableArray<MdbRealIndex> RealIndices { get; }

    /// <summary>
    /// The name of the table
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// The number of rows in the table
    /// </summary>
    public int NumRows { get; }

    /// <summary>
    /// The next number for AutoNumber IDs
    /// </summary>
    public int NextAutoNum { get; }

    /// <summary>
    /// The type of the table
    /// </summary>
    public MdbTableType TableType { get; }

    /// <summary>
    /// Maximum number of columns a row will have (includes deleted columns)
    /// </summary>
    internal ushort MaxCols { get; }

    /// <summary>
    /// The number of variable-width columns
    /// </summary>
    /// <value></value>
    internal ushort NumVarCols { get; }

    /// <summary>
    /// The number of columns
    /// </summary>
    public int NumColumns { get; }

    /// <summary>
    /// The number of indexes on the table.
    /// </summary>
    public int NumIndexes { get; }

    /// <summary>
    /// The number of real indexes (may be larger than NumIndexed when they are used for 
    /// relations)
    /// </summary>
    internal int NumRealIndexes { get; }

    /// <summary>
    /// A pointer to the pages used by the table
    /// </summary>s
    internal int UsedPagesPtr { get; }

    /// <summary>
    /// A pointer to the pages that can be written for this table.
    /// </summary>
    internal int FreePagesPtr { get; }

    /// <summary>
    /// The page number for the first page
    /// </summary>
    internal int FirstPage { get; }

    /// <summary>
    /// The columns in the table
    /// </summary>
    public ImmutableArray<MdbColumn> Columns { get; }

    /// <summary>
    /// The indexes on the table
    /// </summary>
    public ImmutableArray<MdbIndex> Indexes { get; }

    private readonly Jet3Reader _reader;
    private MdbRows? _rows;

    internal class Builder
    {
        internal Builder() { }

        public int NumRows { get; set; }
        public int NextAutoNum { get; set; }
        public int? AutoNumIncrement { get; set; }
        public MdbTableType TableType { get; set; }
        public ushort MaxCols { get; set; }
        public ushort NumVarCols { get; set; }
        public ushort NumColumns { get; set; }
        public int NumIndexes { get; set; }
        public int NumRealIndexes { get; set; }
        public int UsedPagesPtr { get; set; }
        public int FreePagesPtr { get; set; }
        public int FirstPage { get; set; }
        public MdbColumn.Builder[]? Columns { get; set; }
        public MdbRealIndex.Builder[]? RealIndices { get; set; }
        public MdbIndex.Builder[]? Indices { get; set; }
        [MemberNotNull(nameof(Columns))]
        [MemberNotNull(nameof(RealIndices))]
        [MemberNotNull(nameof(Indices))]
        public void InitializeArrays()
        {
            Columns = new MdbColumn.Builder[NumColumns];
            Indices = new MdbIndex.Builder[NumIndexes];
            RealIndices = new MdbRealIndex.Builder[NumRealIndexes];
        }

        public MdbTable Build(string name, Jet3Reader reader)
        {
            if (Columns == null || RealIndices == null || Indices == null)
                throw new InvalidOperationException("All objects must be initialized before building table");

            return new MdbTable(
                name: name,
                numRows: NumRows,
                nextAutoNum: NextAutoNum,
                tableType: TableType,
                maxCols: MaxCols,
                numVarCols: NumVarCols,
                numColumns: NumColumns,
                numIndexes: NumIndexes,
                numRealIndexes: NumRealIndexes,
                usedPagesPtr: UsedPagesPtr,
                freePagesPtr: FreePagesPtr,
                firstPage: FirstPage,
                columns: Columns.Select(c => c.Build(reader)).OrderBy(c => c.IndexIncludingDeleted).ToImmutableArray(),
                indexes: Indices.Select(i => i.Build()).ToImmutableArray(),
                reader: reader,
                realIndices: RealIndices.Select(i => i.Build()).ToImmutableArray());
        }
    }
}
