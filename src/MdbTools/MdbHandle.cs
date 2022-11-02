// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.
//
// Based on code from libmdb (https://github.com/mdbtools/mdbtools)

namespace MMKiwi.MdbTools;
public sealed class MdbHandle : IDisposable, IAsyncDisposable
{
    private MdbHandle(Jet3Reader reader)
    {
        Reader = reader;
    }

    public static MdbHandle Open(string fileName)
    {
        FileStream mdbFileStream = File.OpenRead(fileName);
        Span<byte> header = stackalloc byte[19];
        mdbFileStream.ReadExactly(header);

        if (!header.ByteArrayCompare("\0\u0001\0\0Standard Jet DB"u8))
            throw new FormatException("File is not JET database");

        mdbFileStream.Seek(0x14, SeekOrigin.Begin);
        int jetVersion = mdbFileStream.ReadByte();
        return new(jetVersion == 0 ? new Jet3Reader(mdbFileStream) : throw new Exception());

    }

    private async Task<MdbTable> GetCatalogTableAsync(CancellationToken ct)
    {
        return _catalogTable ??= new("MSysObjects", await Reader.ReadTableDef(2, ct));
    }

    public async Task<IEnumerable<MdbTable>> GetTablesAsync(CancellationToken ct = default)
    {
        if (Tables != null)
            return Tables;
        else
        {
            var catalogTable = await GetCatalogTableAsync(ct);
            var usageMap = await Reader.ReadUsageMap(catalogTable.UsedPagesPtr, ct);
            var freeMap = await Reader.ReadUsageMap(catalogTable.FreePagesPtr, ct);

            int page = await Reader.FindNextMap(usageMap, 1, ct);
            await Reader.ReadDataPageAsync(page, ct);
            return null!;
        }
    }



    private MdbTable? _catalogTable;
    private MdbTable[]? Tables { get; set; }

    public ValueTask DisposeAsync() => Reader.DisposeAsync();

    public void Dispose() => Reader.Dispose();

    internal Jet3Reader Reader { get; init; }
}
