// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.
//
// Based on code from libmdb (https://github.com/mdbtools/mdbtools)

namespace MMKiwi.MdbTools.Mutable;
/// <summary>
/// /// A container class for mutable builders for the publicly visiable, immutable Mdb obejects
/// </summary>
internal static partial class MdbBuilder
{
    /// <summary>
    /// The mutable builder for a <see cref="MdbTable" /> class.
    /// </summary>

    internal class Table
    {
        public Table(int numRows, int nextAutoNum, TableType tableType, ushort maxCols, ushort numVarCols, ushort numCols, int numIndexes, int numRealIndexes, int usedPagesPtr, int freePagesPtr, int firstPage)
        {
            NumRows = numRows;
            NextAutoNum = nextAutoNum;
            TableType = tableType;
            MaxCols = maxCols;
            NumVarCols = numVarCols;
            NumCols = numCols;
            NumIndexes = numIndexes;
            NumRealIndexes = numRealIndexes;
            UsedPagesPtr = usedPagesPtr;
            FreePagesPtr = freePagesPtr;
            FirstPage = firstPage;

            Columns = new Column[numCols];
            RealIndices = new RealIndex[numRealIndexes];
            Indices = new Index[numIndexes];
        }

        public int NumRows { get; set; }
        public int NextAutoNum { get; set; }
        public TableType TableType { get; set; }
        public ushort MaxCols { get; set; }
        public ushort NumVarCols { get; set; }
        public ushort NumCols { get; set; }
        public int NumIndexes { get; set; }
        public int NumRealIndexes { get; set; }
        public int UsedPagesPtr { get; set; }
        public int FreePagesPtr { get; set; }
        public int FirstPage { get; set; }
        public Column[] Columns { get; }
        public RealIndex[] RealIndices { get; }
        public Index[] Indices { get; }
    }
}