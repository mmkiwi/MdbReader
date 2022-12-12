using System.Collections;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace MMKiwi.MdbTools;

/// <summary>
/// A collection of <see cref="MdbTable">MdbTables</see> associated with a given <see cref="MdbHandle" />
/// </summary>
public class MdbTables : IReadOnlyList<MdbTable>
{

    private ImmutableArray<MdbTable> Tables { get; }
    private ImmutableDictionary<string, int> Indexes { get; }

    internal MdbTables(ImmutableArray<MdbTable> tables)
    {
        Tables = tables;
        Indexes = tables.Select((t, i) => (Table: t, Index: i)).ToImmutableDictionary(kvp => kvp.Table.Name, kvp => kvp.Index);
    }

/// <summary>
/// Determines whether the table exists in the database
/// </summary>
/// <param name="tableName">The name of the table (case-sensitive)</param>
/// <returns>true if the table exists, false otherwise</returns>
    public bool TableExists(string tableName) => Indexes.ContainsKey(tableName);

    /// <inheritdoc/>
    public MdbTable this[int index] => Tables[index];

    /// <inheritdoc/>
    public MdbTable this[string key] => Tables[Indexes[key]];

    /// <inheritdoc/>
    public int Count => Tables.Length;

    /// <inheritdoc/>
    public bool ContainsTable(string key) => Indexes.ContainsKey(key);
    /// <inheritdoc/>
    public IEnumerator<MdbTable> GetEnumerator() => (Tables as IEnumerable<MdbTable>).GetEnumerator();

    /// <summary>
    /// Tries to get a specific table from the database.
    /// </summary>
    /// <param name="tableName">The case-sensitive name of the table to return</param>
    /// <returns>The specified table if it exists in the database, null otherwise.</returns>
    public MdbTable? TryGetValue(string tableName)
    {
        bool res = Indexes.TryGetValue(tableName, out int index);
        return res ? Tables[index] : null;
    }

    /// <summary>
    /// Tries to get a specific table from the database.
    /// </summary>
    /// <param name="tableName">The case-sensitive name of the table to return</param>
    /// <param name="mdbTable">When this method returns, contains the specified <see cref="MdbTable" /> or null otherwise.</param>
    /// <returns>True if the table exists in the database, false otherwise.</returns>
    public bool TryGetValue([NotNullWhen(true)] string tableName, [MaybeNullWhen(false)] out MdbTable? mdbTable)
    {
        bool res = Indexes.TryGetValue(tableName, out int index);
        if(res)
        {
            mdbTable = Tables[index];
        }
        else
        {
            mdbTable = null!;
        }

        return res;
    } 

    /// <inheritdoc/>
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

}
