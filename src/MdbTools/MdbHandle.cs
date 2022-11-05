// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.
//
// Based on code from libmdb (https://github.com/mdbtools/mdbtools)

using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Text;

namespace MMKiwi.MdbTools;
public sealed class MdbHandle : IDisposable, IAsyncDisposable
{
    private MdbHandle(Jet3Reader reader)
    {
        Reader = reader;
        _userTables = new(GetUserTables, true);
    }

    public static MdbHandle Open(string fileName)
    {
        using FileStream mdbFileStream = File.OpenRead(fileName);

        (var jetVersion, var encoding) = GetJetVersion(mdbFileStream);

        return new(jetVersion == 0 ? new Jet3FileReader(fileName, encoding) : throw new Exception());
    }

    public static MdbHandle Open(Stream stream, bool disableAsyncForThreadSafety)
    {
        if (!stream.CanSeek || !stream.CanRead)
            throw new ArgumentException($"MdbHandle requires a stream that is readable and seekable");

        (var jetVersion, var encoding) = GetJetVersion(stream);

        return new(jetVersion == 0 ? new Jet3StreamReader(stream, encoding, disableAsyncForThreadSafety) : throw new Exception());
    }

    private static (int jetVersion, Encoding encoding) GetJetVersion(Stream mdbFileStream)
    {
        byte[] header = new byte[19];
        mdbFileStream.ReadExactly(header);

        if (!header.ByteArrayCompare("\0\u0001\0\0Standard Jet DB"u8))
            throw new FormatException("File is not JET database");

        mdbFileStream.Seek(0x14, SeekOrigin.Begin);
        int jetVersion = mdbFileStream.ReadByte();

        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        var encoding = Encoding.GetEncoding(1252);

        return (jetVersion, encoding);
    }

    public ImmutableArray<MdbTable> GetUserTables()
    {
        MdbTable catalogTable = new("MSysObjects", Reader.ReadTableDef(2));
        return EnumerateRows(catalogTable)
            .Where(r => r.Fields.First(f => f.Column.Name == "Type").AsInt16() == 1 &&
                        r.Fields.First(f => f.Column.Name == "Flags").AsInt32() == 0)
            .Select(CreateMdbTableFromRecord)
            .ToImmutableArray();
    }

    private async ValueTask<MdbTable> CreateMdbTableFromRecordAsync(MdbDataRow row, CancellationToken ct)
    {
        var result = row.Fields.ToDictionary(field => field.Column!.Name);
        var id = result["Id"].AsInt32();
        string name = result["Name"].AsStringNotNull();
        return new MdbTable(name, await Reader.ReadTableDefAsync(id, ct).ConfigureAwait(false));
    }

    private MdbTable CreateMdbTableFromRecord(MdbDataRow row)
    {
        var result = row.Fields.ToDictionary(field => field.Column!.Name);
        var id = result["Id"].AsInt32();
        string name = result["Name"].AsStringNotNull();
        return new MdbTable(name, Reader.ReadTableDef(id));
    }

    internal async IAsyncEnumerable<MdbDataRow> EnumerateRowsAsync(MdbTable table, [EnumeratorCancellation] CancellationToken ct)
    {
        byte[] usageMap = await Reader.ReadUsageMapAsync(table.UsedPagesPtr, ct).ConfigureAwait(false);
        byte[] freeMap = await Reader.ReadUsageMapAsync(table.FreePagesPtr, ct).ConfigureAwait(false);

        List<MdbField[]> results = new();
        int page = 0;
        while (true)
        {
            page = await Reader.FindNextMapAsync(usageMap, page, ct).ConfigureAwait(false);
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

    internal IEnumerable<MdbDataRow> EnumerateRows(MdbTable table)
    {
        byte[] usageMap = Reader.ReadUsageMap(table.UsedPagesPtr);
        byte[] freeMap = Reader.ReadUsageMap(table.FreePagesPtr);

        List<MdbField[]> results = new();
        int page = 0;
        while (true)
        {
            page = Reader.FindNextMap(usageMap, page);
            if (page == 0)
                break;
            foreach (var row in Reader.ReadDataPage(page, table))
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

    private readonly Lazy<ImmutableArray<MdbTable>> _userTables;

    public ImmutableArray<MdbTable> Tables => _userTables.Value;

    public ValueTask DisposeAsync() => Reader.DisposeAsync();

    public void Dispose() => Reader.Dispose();

    internal Jet3Reader Reader { get; }
}
