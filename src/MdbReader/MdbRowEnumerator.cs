// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.
//
// Based on code from libmdb (https://github.com/mdbtools/mdbtools)

using System.Collections;
using System.Collections.Immutable;

namespace MMKiwi.MdbReader;

/// <summary>
/// A collection of rows.
/// </summary>
public class MdbRows : IEnumerable<MdbDataRow>, IAsyncEnumerable<MdbDataRow>
{
    internal MdbRows(Jet3Reader reader, MdbTable table)
    {
        Reader = reader;
        Table = table;
    }

    private Jet3Reader Reader { get; }
    private MdbTable Table { get; }

    /// <inheritdoc/>
    public async IAsyncEnumerator<MdbDataRow> GetAsyncEnumerator(CancellationToken ct = default)
    {
        byte[] usageMap = Reader.ReadUsageMap(Table.UsedPagesPtr);

        int page = 0;
        while (true)
        {
            page = Reader.FindNextMap(usageMap, page);
            if (page == 0)
                break;
            await foreach (var row in Reader.ReadDataPageAsync(page, Table, new HashSet<string>(0), ct))
                yield return new(row.ToImmutableArray(), Reader.Options.TableNameComparison, 10);
        }
    }

    /// <inheritdoc/>
    public IEnumerator<MdbDataRow> GetEnumerator()
    {
        byte[] usageMap = Reader.ReadUsageMap(Table.UsedPagesPtr);

        int page = 0;
        while (true)
        {
            page = Reader.FindNextMap(usageMap, page);
            if (page == 0)
                break;
            foreach (var row in Reader.ReadDataPage(page, Table, new HashSet<string>(0)))
                yield return new(row.ToImmutableArray(), Reader.Options.TableNameComparison, 10);
        }
    }

    /// <inheritdoc/>
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
