using System.Collections;

namespace MMKiwi.MdbTools;

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
                yield return new(row);
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
                yield return new(row);
        }
    }

    /// <inheritdoc/>
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
