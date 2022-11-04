// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.
//
// Based on code from libmdb (https://github.com/mdbtools/mdbtools)

using System.Collections.Immutable;
using System.Diagnostics;
using MMKiwi.MdbTools.Mutable;

namespace MMKiwi.MdbTools;

[DebuggerDisplay("MdbTable {Name}")]
public sealed record class MdbTable
{
    internal MdbTable(string name, MdbBuilder.Table tableBuilder)
    {
        Name = name;
        NumRows = tableBuilder.NumRows;
        NextAutoNum = tableBuilder.NextAutoNum;
        TableType = tableBuilder.TableType;
        MaxCols = tableBuilder.MaxCols;
        NumVarCols = tableBuilder.NumVarCols;
        NumCols = tableBuilder.NumCols;
        NumIndexes = tableBuilder.NumIndexes;
        NumRealIndexes = tableBuilder.NumRealIndexes;
        UsedPagesPtr = tableBuilder.UsedPagesPtr;
        FreePagesPtr = tableBuilder.FreePagesPtr;
        FirstPage = tableBuilder.FirstPage;
        Columns = tableBuilder.Columns?.Select(c => new MdbColumn(c)).ToImmutableArray() ?? ImmutableArray<MdbColumn>.Empty;
    }

    public IAsyncEnumerable<MdbDataRow> GetRows(MdbHandle handle, CancellationToken ct = default)
    {
        return handle.EnumerateRows(this,ct);
    }

    public string Name { get; }
    public int NumRows { get; }
    public int NextAutoNum { get; }
    public TableType TableType { get; }
    public ushort MaxCols { get; }
    public ushort NumVarCols { get; }
    public ushort NumCols { get; }
    public int NumIndexes { get; }
    public int NumRealIndexes { get; }
    public int UsedPagesPtr { get; }
    public int FreePagesPtr { get; }
    public int FirstPage { get; }
    public ImmutableArray<MdbColumn> Columns { get; }
}
