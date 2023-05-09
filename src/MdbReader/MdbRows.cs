// This file is part of MdbReader. Licensed under the LGPL version 2.0.
// You should have received a coy of the GNU LGPL version along with this
// program. If not, see https://www.gnu.org/licenses/old-licenses/lgpl-2.0.html
//
// Copyright Micah Makaiwi.
// Based on code from libmdb (https://github.com/mdbtools/mdbtools)

using System.Collections;

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

        foreach (int page in Reader.GetUsageMap(usageMap))
        {
            await foreach (var row in Reader.ReadDataPageAsync(page, Table, new HashSet<string>(0), ct))
                yield return new(row, Reader.Options.TableNameComparison, 10);
        }
    }

    /// <inheritdoc/>
    public IEnumerator<MdbDataRow> GetEnumerator()
    {
        byte[] usageMap = Reader.ReadUsageMap(Table.UsedPagesPtr);

        foreach (int page in Reader.GetUsageMap(usageMap))
        {
            foreach (var row in Reader.ReadDataPage(page, Table, new HashSet<string>(0)))
                yield return new(row, Reader.Options.TableNameComparison, 10);
        }
    }

    /// <inheritdoc/>
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
