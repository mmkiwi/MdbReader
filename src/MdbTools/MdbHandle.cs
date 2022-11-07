// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.
//
// Based on code from libmdb (https://github.com/mdbtools/mdbtools)

using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Text;

using MMKiwi.MdbTools.Fields;

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
        return EnumerateRows(catalogTable, new HashSet<string>()
            {
                "Id",
                "Name",
                 "Type",
                 "Flags"
            })
            .Where(r => ((MdbIntField.Nullable)r.Fields.First(f => f.Column.Name == "Type")).Value == 1 &&
                        ((MdbLongIntField.Nullable)r.Fields.First(f => f.Column.Name == "Flags")).Value == 0)
            .Select(CreateMdbTableFromRecord)
            .ToImmutableArray();
    }

    private MdbTable CreateMdbTableFromRecord(MdbDataRow row)
    {
        var result = row.Fields.ToDictionary(field => field.Column!.Name);
        int id = ((MdbLongIntField.Nullable)result["Id"]).Value ?? throw new FormatException("Could not get ID of table");
        string name = ((MdbStringField.Nullable)result["Name"]).Value ?? throw new FormatException("Could not get Name of table"); ;
        return new MdbTable(name, Reader.ReadTableDef(id));
    }

    internal async IAsyncEnumerable<MdbDataRow> EnumerateRowsAsync(MdbTable table, HashSet<string>? columnsToTake, [EnumeratorCancellation] CancellationToken ct)
    {
        byte[] usageMap = await Reader.ReadUsageMapAsync(table.UsedPagesPtr, ct).ConfigureAwait(false);
        byte[] freeMap = await Reader.ReadUsageMapAsync(table.FreePagesPtr, ct).ConfigureAwait(false);

        int page = 0;
        while (true)
        {
            page = await Reader.FindNextMapAsync(usageMap, page, ct).ConfigureAwait(false);
            if (page == 0)
                break;
            await foreach (var row in Reader.ReadDataPageAsync(page, table, columnsToTake ?? new(0), ct))
                yield return new(row);
        }
    }

    internal IEnumerable<MdbDataRow> EnumerateRows(MdbTable table, HashSet<string>? columnsToTake)
    {
        byte[] usageMap = Reader.ReadUsageMap(table.UsedPagesPtr);
        byte[] freeMap = Reader.ReadUsageMap(table.FreePagesPtr);

        int page = 0;
        while (true)
        {
            page = Reader.FindNextMap(usageMap, page);
            if (page == 0)
                break;
            foreach (var row in Reader.ReadDataPage(page, table, columnsToTake ?? new(0)))
                yield return new(row);
        }
    }

    private readonly Lazy<ImmutableArray<MdbTable>> _userTables;

    public ImmutableArray<MdbTable> Tables => _userTables.Value;

    public ValueTask DisposeAsync() => Reader.DisposeAsync();

    public void Dispose() => Reader.Dispose();

    internal Jet3Reader Reader { get; }
}
