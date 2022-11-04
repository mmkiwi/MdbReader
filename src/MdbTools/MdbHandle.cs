// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.
//
// Based on code from libmdb (https://github.com/mdbtools/mdbtools)

using System.Runtime.CompilerServices;

namespace MMKiwi.MdbTools;
public sealed class MdbHandle : IDisposable, IAsyncDisposable
{
    private MdbHandle(Jet3Reader reader)
    {
        Reader = reader;
    }

    public async static Task<MdbHandle> Open(string fileName)
    {
        FileStream mdbFileStream = File.OpenRead(fileName);
        byte[] header = new byte[19];
        await mdbFileStream.ReadExactlyAsync(header.AsMemory());

        if (!header.ByteArrayCompare("\0\u0001\0\0Standard Jet DB"u8))
            throw new FormatException("File is not JET database");

        mdbFileStream.Seek(0x14, SeekOrigin.Begin);
        int jetVersion = mdbFileStream.ReadByte();
        return new(jetVersion == 0 ? new Jet3Reader(mdbFileStream) : throw new Exception());

    }

    private async Task<MdbTable> GetCatalogTableAsync(CancellationToken ct)
    {
        return _catalogTable ??= new("MSysObjects", await Reader.ReadTableDefAsync(2, ct));
    }

    public async Task<IEnumerable<MdbTable>> GetUserTablesAsync(CancellationToken ct = default)
    {
        if (UserTables == null)
        {
            MdbTable catalogTable = await GetCatalogTableAsync(ct);
            UserTables = await EnumerateRows(catalogTable, ct)
                .Where(r => r.Fields.First(f => f.Column.Name == "Type").AsInt16() == 1 &&
                            r.Fields.First(f => f.Column.Name == "Flags").AsInt32() == 0)
                .SelectAwaitWithCancellation(CreateMdbTableFromRecord)
                .ToListAsync();
                
        }
        return UserTables;
    }

    private async ValueTask<MdbTable> CreateMdbTableFromRecord(MdbDataRow row, CancellationToken ct)
    {
        var result = row.Fields.ToDictionary(field => field.Column!.Name);
        var id = result["Id"].AsInt32();
        string name = result["Name"].AsStringNotNull();
        return new MdbTable(name, await Reader.ReadTableDefAsync(id, ct));
    }

    internal async IAsyncEnumerable<MdbDataRow> EnumerateRows(MdbTable table, [EnumeratorCancellation] CancellationToken ct)
    {
        byte[] usageMap = await Reader.ReadUsageMap(table.UsedPagesPtr, ct);
        byte[] freeMap = await Reader.ReadUsageMap(table.FreePagesPtr, ct);

        List<MdbField[]> results = new();
        int page = 0;
        while (true)
        {
            page = await Reader.FindNextMap(usageMap, page, ct);
            if (page == 0)
                break;
            await foreach (var row in Reader.ReadDataPageAsync(page, table, ct))
                yield return new(row);
        }

        for (int i = 4; i < results.Count; i++)
        {
            var result = results[i].ToDictionary(field => field.Column!.Name);
            var id = result["Id"].AsInt32();
            string name = result["Name"].AsStringNotNull();
            if (result["Type"].AsInt16() != 1 || result["Flags"].AsInt32() == -2147483648)
                continue;
        }

    }

    private MdbTable? _catalogTable;
    private List<MdbTable>? UserTables { get; set; }

    public ValueTask DisposeAsync() => Reader.DisposeAsync();

    public void Dispose() => Reader.Dispose();

    internal Jet3Reader Reader { get; }
}
