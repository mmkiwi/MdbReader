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
        _reader = reader;
        _table = table;
    }

    private readonly Jet3Reader _reader;
    private readonly MdbTable _table;

    /// <inheritdoc/>
    public IAsyncEnumerator<MdbDataRow> GetAsyncEnumerator(CancellationToken ct = default)
    {
        return _reader.EnumerateRowsAsync(_table, null, ct).GetAsyncEnumerator(ct);
    }

    /// <inheritdoc/>
    public IEnumerator<MdbDataRow> GetEnumerator()
    {
        return _reader.EnumerateRows(_table, null).GetEnumerator();
    }

    /// <inheritdoc/>
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
