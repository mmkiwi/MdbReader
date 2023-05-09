// This file is part of MdbReader. Licensed under the LGPL version 2.0.
// You should have received a coy of the GNU LGPL version along with this
// program. If not, see https://www.gnu.org/licenses/old-licenses/lgpl-2.0.html
//
// Copyright Micah Makaiwi.
// Based on code from libmdb (https://github.com/mdbtools/mdbtools)

namespace MMKiwi.MdbReader.Schema;

/// <summary>
/// Information about a database index.
/// </summary>
public record class MdbIndex
{
    private MdbIndex(int indexNum, int indexNum2, byte relTableType, int rlIndexNum, int relTablePage, bool cascadeUpdates, bool cascadeDeletes, MdbIndexType indexType, string? name)
    {
        IndexNum = indexNum;
        IndexNum2 = indexNum2;
        RelTableType = relTableType;
        RlIndexNum = rlIndexNum;
        RelTablePage = relTablePage;
        CascadeUpdates = cascadeUpdates;
        CascadeDeletes = cascadeDeletes;
        IndexType = indexType;
        Name = name;
    }

    /// <summary>
    /// The index number
    /// </summary>
    /// <value></value>
    public int IndexNum { get; }
    internal int IndexNum2 { get; }
    internal byte RelTableType { get; }
    internal int RlIndexNum { get; }
    internal int RelTablePage { get; }

    /// <summary>
    /// If true, updates cascade through the relationship
    /// </summary>
    public bool CascadeUpdates { get; }
    /// <summary>
    /// If true, deletes cascade through the relationship
    /// </summary>
    public bool CascadeDeletes { get; }

    /// <summary>
    /// The type of the index
    /// </summary>
    public MdbIndexType IndexType { get; }

    /// <summary>
    /// The index name
    /// </summary>
    public string? Name { get; }

    internal class Builder
    {
        public int IndexNum { get; set; }
        public int IndexNum2 { get; set; }
        public byte RelTableType { get; set; }
        public int RlIndexNum { get; set; }
        public int RelTablePage { get; set; }
        public bool CascadeUpdates { get; set; }
        public bool CascadeDeletes { get; set; }
        public MdbIndexType IndexType { get; set; }
        public string? Name { get; set; }

        public MdbIndex Build() =>
            new(IndexNum, IndexNum2, RelTableType, RlIndexNum, RelTablePage, CascadeUpdates, CascadeDeletes, IndexType, Name);
    }
}