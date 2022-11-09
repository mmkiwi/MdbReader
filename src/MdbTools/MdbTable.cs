// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.
//
// Based on code from libmdb (https://github.com/mdbtools/mdbtools)

using System.Collections.Immutable;
using System.Diagnostics;

using MMKiwi.MdbTools.Mutable;

namespace MMKiwi.MdbTools;

/// <summary>
/// Represents a table in the Access database.
/// </summary>
[DebuggerDisplay("MdbTable {Name}")]
public sealed record class MdbTable
{
    internal MdbTable(string name, MdbBuilder.Table tableBuilder, MdbHandle handle)
    {
        Name = name;
        NumRows = tableBuilder.NumRows;
        NextAutoNum = tableBuilder.NextAutoNum;
        TableType = tableBuilder.TableType;
        MaxCols = tableBuilder.MaxCols;
        NumVarCols = tableBuilder.NumVarCols;
        NumColumns = tableBuilder.NumCols;
        NumIndexes = tableBuilder.NumIndexes;
        NumRealIndexes = tableBuilder.NumRealIndexes;
        UsedPagesPtr = tableBuilder.UsedPagesPtr;
        FreePagesPtr = tableBuilder.FreePagesPtr;
        FirstPage = tableBuilder.FirstPage;
        Handle = handle;
        Columns = tableBuilder.Columns?.Select(c => new MdbColumn(c)).ToImmutableArray() ?? ImmutableArray<MdbColumn>.Empty;
    }

    /// <summary>
    /// Get all the rows in the database asynchronously.
    /// </summary>
    /// <param name="ct">A <see cref="CancellationToken" /> to cancel the enumeration</param>
    public IAsyncEnumerable<MdbDataRow> GetRowsAsync(CancellationToken ct = default)
        => Handle.EnumerateRowsAsync(this, null, ct);


    /// <summary>
    /// An enumerator for all the rows in the database.
    /// </summary>
    public IEnumerable<MdbDataRow> Rows => Handle.EnumerateRows(this, null);

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
    public ushort NumColumns { get; }

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
    /// The parent MdbHandle
    /// </summary>
    public MdbHandle Handle { get; }

    /// <summary>
    /// The columns in the table
    /// </summary>
    public ImmutableArray<MdbColumn> Columns { get; }
}
