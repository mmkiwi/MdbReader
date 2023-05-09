// This file is part of MdbReader. Licensed under the LGPL version 2.0.
// You should have received a coy of the GNU LGPL version along with this
// program. If not, see https://www.gnu.org/licenses/old-licenses/lgpl-2.0.html
//
// Copyright Micah Makaiwi.
// Based on code from libmdb (https://github.com/mdbtools/mdbtools)

using System.Collections;
using System.Diagnostics.CodeAnalysis;

using MMKiwi.Collections;

namespace MMKiwi.MdbReader;

/// <summary>
/// A collection of <see cref="MdbTable">MdbTables</see> associated with a given <see cref="MdbConnection" />
/// </summary>
public class MdbTables : IReadOnlyList<MdbTable>
{
    private TableCollection Tables { get; }

    internal MdbTables(ImmutableArray<MdbTable> tables, IEqualityComparer<string> comparer)
    {
        Tables = new(tables, comparer);
    }

    /// <inheritdoc/>
    public MdbTable this[int index] => Tables[index];

    /// <inheritdoc/>
    public MdbTable this[string key] => Tables[key];

    /// <inheritdoc/>
    public int Count => Tables.Count;

    /// <inheritdoc/>
    public IEnumerable<string> Keys => Tables.Select(t => t.Name);

    /// <inheritdoc/>
    public IEnumerable<MdbTable> Values => Tables;

    /// <inheritdoc/>
    public bool ContainsKey(string key) => Tables.Contains(key);
    /// <inheritdoc/>
    public IEnumerator<MdbTable> GetEnumerator() => Tables.GetEnumerator();

    /// <summary>
    /// Tries to get a specific table from the database.
    /// </summary>
    /// <param name="tableName">The case-sensitive name of the table to return</param>
    /// <returns>The specified table if it exists in the database, null otherwise.</returns>
    public MdbTable? TryGetValue(string tableName)
    {
        bool res = Tables.TryGetValue(tableName, out MdbTable? table);
        return res ? table : null;
    }

    /// <summary>
    /// Tries to get a specific table from the database.
    /// </summary>
    /// <param name="tableName">The case-sensitive name of the table to return</param>
    /// <param name="mdbTable">When this method returns, contains the specified <see cref="MdbTable" /> or null otherwise.</param>
    /// <returns>True if the table exists in the database, false otherwise.</returns>
    public bool TryGetValue([NotNullWhen(true)] string tableName, [MaybeNullWhen(false)] out MdbTable mdbTable)
    {
        bool res = Tables.TryGetValue(tableName, out MdbTable? table);

        mdbTable = res ? table : null;

        return res;
    }

    /// <inheritdoc/>
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    private class TableCollection : ImmutableKeyedCollection<string, MdbTable>
    {

        public TableCollection(ImmutableArray<MdbTable> baseCollection, IEqualityComparer<string>? comparer = null, int dictionaryCreationThreshold = 0) : base(baseCollection, comparer, dictionaryCreationThreshold)
        {
        }

        protected override string GetKeyForItem(MdbTable item) => item.Name;
    }
}
