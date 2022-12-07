using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

using Nito.AsyncEx;

namespace MMKiwi.MdbTools;
public class MdbTables : IReadOnlyList<MdbTable>
{

    private ImmutableArray<MdbTable> Tables { get; }
    private ImmutableDictionary<string, int> Indexes { get; }

    internal MdbTables(ImmutableArray<MdbTable> tables)
    {
        Tables = tables;
        Indexes = tables.Select((t, i) => (Table: t, Index: i)).ToImmutableDictionary(kvp => kvp.Table.Name, kvp => kvp.Index);
    }



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

    /// <inheritdoc/>
    public bool TryGetValue(string key, out MdbTable value)
    {
        var res = Indexes.TryGetValue(key, out int index);
        value = Tables[index];
        return res;
    }

    public MdbTable? TryGetTable(string key)
    {
        var res = Indexes.TryGetValue(key, out int index);
        var value = Tables[index];
        return res ? value : null;
    } 

    /// <inheritdoc/>
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

}
