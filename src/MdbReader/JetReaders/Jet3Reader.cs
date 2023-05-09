// This file is part of MdbReader. Licensed under the LGPL version 2.0.
// You should have received a coy of the GNU LGPL version along with this
// program. If not, see https://www.gnu.org/licenses/old-licenses/lgpl-2.0.html
//
// Copyright Micah Makaiwi.
// Based on code from libmdb (https://github.com/mdbtools/mdbtools)

using MMKiwi.MdbReader.Values;
using System.Runtime.CompilerServices;
using MMKiwi.MdbReader.Helpers;
using MMKiwi.MdbReader.Schema;

namespace MMKiwi.MdbReader;

internal abstract partial class Jet3Reader : IDisposable, IAsyncDisposable
{
    public MdbReaderOptions Options { get; }
    protected Jet3Reader(MdbHeaderInfo db, MdbReaderOptions options)
    {
        Options = options;
        Db = db;
        if (db.DbKey == 0)
            DbKey = null;
        else
            MdbBinary.WriteInt32LittleEndian(DbKey, db.DbKey);
    }

    public static MdbHeaderInfo GetDatabaseInfo(Stream mdbStream)
    {
        if (mdbStream.Length < JetConstants.Jet3.PageSize * 3)
            throw new InvalidDataException("File is not JET database. File too short.");

        byte[] header = new byte[JetConstants.Jet3.PageSize];
        mdbStream.Seek(0, SeekOrigin.Begin);
        mdbStream.Read(header);

        return ReadFirstPage(header);
    }

    public static async Task<MdbHeaderInfo> GetDatabaseInfoAsync(Stream mdbStream, CancellationToken ct)
    {
        if (mdbStream.Length < JetConstants.Jet3.PageSize * 3)
            throw new InvalidDataException("File is not JET database. File too short.");

        byte[] header = new byte[JetConstants.Jet3.PageSize];
        mdbStream.Seek(0, SeekOrigin.Begin);

        await mdbStream.ReadAsync(header.AsMemory(), ct).ConfigureAwait(false);

        return ReadFirstPage(header);
    }

    private static MdbHeaderInfo ReadFirstPage(Span<byte> header)
    {
        if (MdbBinary.ReadUInt32LittleEndian(header) != JetConstants.DbPageCosntants.DbMagicNumber)
            throw new InvalidDataException("File is not JET database. Missing magic number.");

        JetVersion jetVersion = (JetVersion)header[0x14];

        if (!Enum.IsDefined(typeof(JetVersion), jetVersion))
            Debug.WriteLine($"Version {jetVersion} is unknown. Continuing assuming JET4+");

        JetConstants constants = new(jetVersion);

        // "Standard JET Db" (*.mdb, Jet 3 or 4) or "Standard ACE DB" (*.accdb) as ASCII
        var fileFormatId = constants.DbPage.DbFileFormatId;

        if (!header.Slice(0x04, fileFormatId.Length).SequenceEqual(fileFormatId))
            throw new InvalidDataException("File is not JET database. Invalid file format ID");

        byte[] tempRc4Key = new byte[] { 0xC7, 0xDA, 0x39, 0x6B };
        var encryptedHeader = header.Slice(0x18, constants.DbPage.DbHeaderSize);
        // The next 126 or 128 bytes are RC4 encoded, decode in memory
        RC4.ApplyInPlace(encryptedHeader, tempRc4Key);

        ushort collation = MdbBinary.ReadUInt16LittleEndian(encryptedHeader[constants.DbPage.DbCollationOffset..]);
        ushort codePage = MdbBinary.ReadUInt16LittleEndian(encryptedHeader[constants.DbPage.DbCodePageOffset..]);
        int dbKey = MdbBinary.ReadInt32LittleEndian(encryptedHeader[constants.DbPage.DbKeyOffset..]);

        // ignoring database passwords for now; not needed to read
        DateTime creationDate = ConversionFunctions.AsDateTime(encryptedHeader[constants.DbPage.DbCreationDateOffset..]);

        return new(jetVersion, collation, codePage, dbKey, creationDate, constants);
    }

    public MdbHeaderInfo Db { get; }
    public byte[]? DbKey { get; }

    public ushort PageSize => Db.Constants.PageSize;

    protected abstract Task ReadPartialPageToBufferAsync(int pageNo, Memory<byte> buffer, int start, CancellationToken ct);

    protected abstract void ReadPartialPageToBuffer(int pageNo, Span<byte> buffer, int start);

    protected async Task ReadPageToBufferAsync(int pageNo, Memory<byte> buffer, MdbPageType pageType, CancellationToken ct)
    {
        await ReadPartialPageToBufferAsync(pageNo, buffer, 0, ct).ConfigureAwait(false);

        byte header = buffer.Span[0];
        if (header != (byte)pageType)
            throw new FormatException($"Incorrect page type on page {pageNo}. (Expected {(byte)pageType}, observed {header})");
    }

    protected void ReadPageToBuffer(int pageNo, Span<byte> buffer, MdbPageType pageType)
    {
        ReadPartialPageToBuffer(pageNo, buffer, 0);

        byte header = buffer[0];
        if (header != (byte)pageType)
            throw new FormatException($"Incorrect page type on page {pageNo}. (Expected {(byte)pageType}, observed {header})");
    }

    internal async Task<byte[]> ReadUsageMapAsync(int mapPtr, CancellationToken ct)
    {
        var c = Db.Constants.UsageMap;
        int pageNo = mapPtr >> 8;
        byte[] x = new byte[4];
        System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(x, mapPtr);
        byte[] buffer = new byte[PageSize];

        await ReadPageToBufferAsync(pageNo, buffer, MdbPageType.Data, ct).ConfigureAwait(false);

        ushort rowLocation = MdbBinary.ReadUInt16LittleEndian(buffer.AsSpan(c.RowCount));
        int mapSize = PageSize - rowLocation;

        return buffer.AsSpan(rowLocation, mapSize).ToArray();
    }

    protected virtual void DecryptPage(int pageNo, Span<byte> buffer)
    {
        if (pageNo == 0 || DbKey is null)
            return; // not encypted, do nothing
        byte[] pageKey = new byte[4];
        MdbBinary.WriteInt32LittleEndian(pageKey, pageNo);
        for (int i = 0; i < 4; i++)
            pageKey[i] ^= DbKey[i];
        RC4.ApplyInPlace(buffer, pageKey);
    }

    internal byte[] ReadUsageMap(int mapPtr)
    {
        var c = Db.Constants.UsageMap;
        int pageNo = mapPtr >> 8;
        byte[] x = new byte[4];
        System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(x, mapPtr);
        byte[] buffer = new byte[PageSize];

        ReadPageToBuffer(pageNo, buffer, MdbPageType.Data);

        ushort rowLocation = MdbBinary.ReadUInt16LittleEndian(buffer.AsSpan(c.RowCount));
        int mapSize = PageSize - rowLocation;

        return buffer.AsSpan(rowLocation, mapSize).ToArray();
    }

    internal async Task<MdbTable.Builder> ReadTableDefAsync(int startPage, CancellationToken ct)
    {
        (int nextPage, int dataLength) = await ReadNextTablePageAsync(startPage, null, ct).ConfigureAwait(false);

        int usedDataLength = PageSize - 8;

        byte[] buffer = new byte[Math.Max(dataLength, PageSize - 8)];
        await ReadPartialPageToBufferAsync(startPage, buffer.AsMemory(0, Math.Min(dataLength, PageSize - 8)), 8, ct).ConfigureAwait(false);

        while (dataLength > usedDataLength)
        {
            (nextPage, _) = await ReadNextTablePageAsync(nextPage, buffer.AsMemory(usedDataLength), ct).ConfigureAwait(false);
            usedDataLength += PageSize - 8;
        }
        return ParseTable(startPage, buffer);
    }

    internal MdbTable.Builder ReadTableDef(int startPage)
    {
        (int nextPage, int dataLength) = ReadNextTablePage(startPage, null);

        int usedDataLength = PageSize - 8;

        byte[] buffer = new byte[Math.Max(dataLength, PageSize - 8)];
        ReadPartialPageToBuffer(startPage, buffer.AsSpan(0, Math.Min(dataLength, PageSize - 8)), 8);

        while (dataLength > usedDataLength)
        {
            (nextPage, _) = ReadNextTablePage(nextPage, buffer.AsSpan(usedDataLength));
            usedDataLength += PageSize - 8;
        }
        return ParseTable(startPage, buffer);
    }

    private MdbTable.Builder ParseTable(int startPage, ReadOnlySpan<byte> buffer)
    {
        var c = Db.Constants.TablePage;
        // This will always be on the first page of the table def
        MdbTable.Builder table = new()
        {
            FirstPage = startPage,
            NumRows = MdbBinary.ReadInt32LittleEndian(buffer[c.NumRows]),
            NextAutoNum = MdbBinary.ReadInt32LittleEndian(buffer[c.NextAutoNum]),
            AutoNumIncrement = Db.JetVersion == JetVersion.Jet3 ? null :
               MdbBinary.ReadInt32LittleEndian(buffer[c.AutonumIncrement]),
            TableType = (MdbTableType)MdbBinary.ReadByte(buffer[c.TableType]),
            MaxCols = MdbBinary.ReadUInt16LittleEndian(buffer[c.MaxCols]),
            NumVarCols = MdbBinary.ReadUInt16LittleEndian(buffer[c.NumVarCols]),
            NumColumns = MdbBinary.ReadUInt16LittleEndian(buffer[c.NumCols]),
            NumIndexes = MdbBinary.ReadInt32LittleEndian(buffer[c.NumIndexes]),
            NumRealIndexes = MdbBinary.ReadInt32LittleEndian(buffer[c.NumRealIndexes]),
            UsedPagesPtr = MdbBinary.ReadInt32LittleEndian(buffer[c.UsedPages]),
            FreePagesPtr = MdbBinary.ReadInt32LittleEndian(buffer[c.FreePages])
        };

        table.InitializeArrays();

        int cursor = c.TableCursorStartPoint;

        for (int i = 0; i < table.NumRealIndexes; i++)
        {
            var realIndexSlice = buffer.Slice(cursor, c.RealIndexSlice1Length);

            table.RealIndices[i] = new MdbRealIndex.Builder
            {
                NumIndexRows = MdbBinary.ReadInt32LittleEndian(realIndexSlice[c.RealIndexRows])
            };
            cursor += c.RealIndexSlice1Length;
        }

        for (int i = 0; i < table.NumColumns; i++)
        {
            var columnSlice = buffer.Slice(cursor, c.ColumnSliceLength);
            table.Columns[i] = ProcessColumn(columnSlice);
            cursor += c.ColumnSliceLength;
        }
        for (int i = 0; i < table.NumColumns; i++)
        {
            if (Db.JetVersion == JetVersion.Jet3)
            {
                byte colNameLen = buffer[cursor];
                table.Columns[i].Name = Db.Encoding.GetString(buffer.Slice(cursor + 1, colNameLen));
                cursor += 1 + colNameLen;
            }
            else
            {
                ushort colNameLen = MdbBinary.ReadUInt16LittleEndian(buffer.Slice(cursor, 2));
                // UCS-2, UTF-16 is close enough for reading purposes
                table.Columns[i].Name = Encoding.Unicode.GetString(buffer.Slice(cursor + 2, colNameLen));
                cursor += 2 + colNameLen;
            }
        }

        for (int i = 0; i < table.NumRealIndexes; i++)
        {
            var realIndexSlice = buffer.Slice(cursor, c.RealIndexSlice2Length);
            ProcessRealIndex(table.RealIndices[i], realIndexSlice);
            cursor += c.RealIndexSlice2Length;
        }

        for (int i = 0; i < table.NumIndexes; i++)
        {
            var indexSlice = buffer.Slice(cursor, c.IndexSliceLength);
            table.Indices[i] = ProcessIndex(indexSlice);
            cursor += c.IndexSliceLength;
        }

        for (int i = 0; i < table.NumIndexes; i++)
        {
            if (Db.JetVersion == JetVersion.Jet3)
            {
                byte indexNameLength = buffer[cursor];
                table.Indices[i].Name = Db.Encoding.GetString(buffer.Slice(cursor + 1, indexNameLength));
                cursor += 1 + indexNameLength;
            }
            else
            {
                ushort indexNameLength = MdbBinary.ReadUInt16LittleEndian(buffer.Slice(cursor, 2));
                // UCS-2, UTF-16 is close enough for reading purposes
                table.Indices[i].Name = Encoding.Unicode.GetString(buffer.Slice(cursor + 2, indexNameLength));
                cursor += 2 + indexNameLength;
            }
        }

        while (true)
        {
            var varColSlice = buffer.Slice(cursor, c.VarColSliceSize);
            if (!ProcessVarCol(table.Columns, varColSlice))
                break;
            cursor += c.VarColSliceSize;
        }

        return table;
    }

    private bool ProcessVarCol(MdbColumn.Builder[] columns, ReadOnlySpan<byte> varColSlice)
    {
        var c = Db.Constants.TablePage;
        ushort colNum = MdbBinary.ReadUInt16LittleEndian(varColSlice[c.VarColNum]);
        if (colNum == ushort.MaxValue)
        {
            return false;
        }
        else
        {
            columns[colNum].UsedPages = MdbBinary.ReadInt32LittleEndian(varColSlice[c.VarColUsedPagePtr]);
            columns[colNum].FreePages = MdbBinary.ReadInt32LittleEndian(varColSlice[c.VarColFreePagePtr]);
            return true;
        }
    }

    internal MdbColumn.Builder ProcessColumn(ReadOnlySpan<byte> columnSlice)
    {
        var c = Db.Constants.TablePage;
        return new()
        {
            Type = (MdbColumnType)MdbBinary.ReadByte(columnSlice[c.ColType]),
            NumInclDeleted = MdbBinary.ReadUInt16LittleEndian(columnSlice[c.ColNumInclDeleted]),
            OffsetVariable = MdbBinary.ReadUInt16LittleEndian(columnSlice[c.OffsetVariable]),
            ColNum = MdbBinary.ReadUInt16LittleEndian(columnSlice[c.ColNum]),
            Misc = columnSlice[c.Misc].ToArray(),
            //skip next 2 bytes
            Flags = (MdbColumnFlags)MdbBinary.ReadByte(columnSlice[c.ColFlags]),
            OffsetFixed = MdbBinary.ReadUInt16LittleEndian(columnSlice[c.OffsetFixed]),
            Length = MdbBinary.ReadUInt16LittleEndian(columnSlice[c.ColLength])
        };
    }

    internal MdbIndex.Builder ProcessIndex(ReadOnlySpan<byte> indexSlice)
    {
        var c = Db.Constants.TablePage;
        MdbIndex.Builder index = new()
        {
            IndexNum = MdbBinary.ReadInt32LittleEndian(indexSlice[c.IndexNum]),
            IndexNum2 = MdbBinary.ReadInt32LittleEndian(indexSlice[c.IndexNum2]),
            RelTableType = MdbBinary.ReadByte(indexSlice[c.IndexRelTableType]),
            RlIndexNum = MdbBinary.ReadInt32LittleEndian(indexSlice[c.IndexRelIndexNum]),
            RelTablePage = MdbBinary.ReadInt32LittleEndian(indexSlice[c.IndexRelTablePage]),
            CascadeUpdates = MdbBinary.ReadByte(indexSlice[c.IndexCascadeUpdates]) == 1,
            CascadeDeletes = MdbBinary.ReadByte(indexSlice[c.IndexCascadeDeletes]) == 1,
            IndexType = (MdbIndexType)MdbBinary.ReadByte(indexSlice[c.IndexType])
        };
        return index;
    }

    internal string? ProcessIndexName(ref int cursor, ReadOnlySpan<byte> buffer)
    {
        byte indexNameLen = buffer[cursor];
        ReadOnlySpan<byte> indexNameEntry = buffer.Slice(cursor, indexNameLen + 1);
        cursor += 1 + indexNameLen;
        return Db.Encoding.GetString(indexNameEntry[1..]);
    }

    internal void ProcessRealIndex(in MdbRealIndex.Builder realIndex, ReadOnlySpan<byte> realIndexSlice)
    {
        var c = Db.Constants.TablePage;
        var columnsSlice = realIndexSlice[c.RealIndexColsSlice];
        for (int i = 0; i < 10; i++)
        {
            var colSlice = columnsSlice.Slice(i * c.RealIndexColSize, c.RealIndexColSize);
            realIndex.Columns[i] = new MdbRealIndexColumn.Builder
            {
                ColNum = MdbBinary.ReadUInt16LittleEndian(colSlice[c.RealIndexColNum]),
                ColOrder = MdbBinary.ReadByte(colSlice[c.RealIndexColOrder])
            };
        }
        realIndex.UsedPages = MdbBinary.ReadInt32LittleEndian(realIndexSlice[c.RealIndexUsagePointer]);
        realIndex.FirstDataPointer = MdbBinary.ReadInt32LittleEndian(realIndexSlice[c.RealIndexFirstData]);
        realIndex.Flags = MdbBinary.ReadByteOrUInt16(realIndexSlice[c.RealIndexFlags]);
    }
    private async Task<(int nextPage, int dataLength)> ReadNextTablePageAsync(int nextPage, Memory<byte> buffer, CancellationToken ct)
    {
        //Header is the first 8 bytes. The first byte is the PageType == 0x02
        byte[] header = new byte[16];
        await ReadPageToBufferAsync(nextPage, header, MdbPageType.TableDefinition, ct).ConfigureAwait(false);

        (int newNextPage, int dataLength, _) = ReadTablePageHeader(header);

        if (buffer.Length > 0) // first time around, we won't 
            await ReadPartialPageToBufferAsync(nextPage, buffer[..Math.Min(buffer.Length, PageSize - 8)], 8, ct).ConfigureAwait(false);

        return (newNextPage, dataLength);
    }

    private (int nextPage, int dataLength) ReadNextTablePage(int nextPage, Span<byte> buffer)
    {
        //Header is the first 8 bytes. The first byte is the PageType == 0x02
        byte[] header = new byte[16];
        ReadPageToBuffer(nextPage, header, MdbPageType.TableDefinition);

        (int newNextPage, int dataLength, _) = ReadTablePageHeader(header);

        if (buffer.Length > 0) // first time around, we won't 
            ReadPartialPageToBuffer(nextPage, buffer[..Math.Min(buffer.Length, PageSize - 8)], 8);

        return (newNextPage, dataLength);
    }

    private (int NewNextPage, int DataLength, ushort? FreeSpace) ReadTablePageHeader(Span<byte> header)
    {

        // Skip the second byte
        // Third and fourth bytes is the word "VC"
        ushort? freeSpace = null;
        if (Db.JetVersion != JetVersion.Jet3)
        {
            freeSpace = MdbBinary.ReadUInt16LittleEndian(header[2..]);
        }
        else if (!header.Slice(2, 2).SequenceEqual(Db.Constants.TablePage.TdefId))
            throw new FormatException("Invalid JET3 table (missing 'VC' in header)");

        // Bytes 4-7 are the pointer to the next page if the table definiton spans
        // multiple pages.
        return (MdbBinary.ReadInt32LittleEndian(header[4..]),
                MdbBinary.ReadInt32LittleEndian(header[8..]),
                freeSpace);
    }

    internal async IAsyncEnumerable<int> GetUsageMapAsync(byte[] map, [EnumeratorCancellation] CancellationToken ct)
    {
        var c = Db.Constants.UsageMap;
        if (map[0] == 0) // Type 0 usage map
        {
            int pageStart = MdbBinary.ReadInt32LittleEndian(map.AsSpan(1));
            BitArray usageBitmap = new(map[5..]);
            for (int j = 0; j < usageBitmap.Length; j++)
                if (usageBitmap[j])
                    yield return pageStart + j;
        }
        else if (map[0] == 1)
        {
            int maxPages = (map.Length - 1) / 4;
            for (int i = 0; i < maxPages; i++)
            {
                int mapPage = MdbBinary.ReadInt32LittleEndian(map.AsSpan(i * 4 + 1));
                if (mapPage == 0)
                    continue;
                byte[] buffer = new byte[PageSize];
                await ReadPageToBufferAsync(mapPage, buffer, MdbPageType.PageUseageBitmap, ct);

                BitArray usageBitmap = new(buffer[4..]);

                for (int j = 1; j < usageBitmap.Length; j++)
                    if (usageBitmap[j])
                        yield return i * c.UsageMapLen + j;
            }
        }
        else
            throw new FormatException();
    }

    internal IEnumerable<int> GetUsageMap(byte[] map)
    {
        var c = Db.Constants.UsageMap;
        if (map[0] == 0) // Type 0 usage map
        {
            int pageStart = MdbBinary.ReadInt32LittleEndian(map.AsSpan(1));
            BitArray usageBitmap = new(map[5..]);
            for (int j = 0; j < usageBitmap.Length; j++)
                if (usageBitmap[j])
                    yield return pageStart + j;
        }
        else if (map[0] == 1)
        {
            int maxPages = (map.Length - 1) / 4;
            for (int i = 0; i < maxPages; i++)
            {
                int mapPage = MdbBinary.ReadInt32LittleEndian(map.AsSpan(i * 4 + 1));
                if (mapPage == 0)
                    continue;
                byte[] buffer = new byte[PageSize];
                ReadPageToBuffer(mapPage, buffer, MdbPageType.PageUseageBitmap);

                BitArray usageBitmap = new(buffer[4..]);

                for (int j = 1; j < usageBitmap.Length; j++)
                    if (usageBitmap[j])
                        yield return i * c.UsageMapLen + j;
            }
        }
        else
            throw new FormatException();
    }

    internal async IAsyncEnumerable<ImmutableArray<IMdbValue>> ReadDataPageAsync(int page, MdbTable table, HashSet<string> columnsToTake, [EnumeratorCancellation] CancellationToken ct)
    {
        // +--------------------------------------------------------------------------+
        // | Jet3 Data Page Definition                                                |
        // +------+---------+---------------------------------------------------------+
        // | data | length  | name       | description                                |
        // +------+---------+---------------------------------------------------------+
        // | 0x01 | 1 byte  | page_type  | 0x01 indicates a data page.                |
        // | 0x01 | 1 byte  | unknown    |                                            |
        // | ???? | 2 bytes | free_space | Free space in this page                    |
        // | ???? | 4 bytes | tdef_pg    | Page pointer to table definition           |
        // | ???? | 2 bytes | num_rows   | number of records on this page             |
        // +--------------------------------------------------------------------------+
        // | Iterate for the number of records                                        |
        // +--------------------------------------------------------------------------+
        // | ???? | 2 bytes | offset_row | The record's location on this page         |
        // +--------------------------------------------------------------------------+

        byte[] buffer = new byte[PageSize];

        await ReadPageToBufferAsync(page, buffer, MdbPageType.Data, ct).ConfigureAwait(false);
        //Header is the first 8 bytes. The first byte is the PageType == 0x02

        ushort numRows = MdbBinary.ReadUInt16LittleEndian(buffer.AsSpan(Db.Constants.DataPage.RecordCount));

        var rowOffsets = GetRowOffsets(numRows, buffer);

        foreach (var rowOffset in rowOffsets)
        {
            if (rowOffset.IsDeleted)
                continue;

            yield return rowOffset.IsLookup
                ? CrackRow(table, rowOffset, buffer, columnsToTake)
                : CrackRow(table, rowOffset, buffer, columnsToTake);
        }
    }

    internal IEnumerable<ImmutableArray<IMdbValue>> ReadDataPage(int page, MdbTable table, HashSet<string> columnsToTake)
    {
        // +--------------------------------------------------------------------------+
        // | Jet3 Data Page Definition                                                |
        // +------+---------+---------------------------------------------------------+
        // | data | length  | name       | description                                |
        // +------+---------+---------------------------------------------------------+
        // | 0x01 | 1 byte  | page_type  | 0x01 indicates a data page.                |
        // | 0x01 | 1 byte  | unknown    |                                            |
        // | ???? | 2 bytes | free_space | Free space in this page                    |
        // | ???? | 4 bytes | tdef_pg    | Page pointer to table definition           |
        // | ???? | 2 bytes | num_rows   | number of records on this page             |
        // +--------------------------------------------------------------------------+
        // | Iterate for the number of records                                        |
        // +--------------------------------------------------------------------------+
        // | ???? | 2 bytes | offset_row | The record's location on this page         |
        // +--------------------------------------------------------------------------+

        byte[] buffer = new byte[PageSize];

        ReadPageToBuffer(page, buffer, MdbPageType.Data);
        //Header is the first 8 bytes. The first byte is the PageType == 0x02

        ushort numRows = MdbBinary.ReadUInt16LittleEndian(buffer.AsSpan(Db.Constants.DataPage.RecordCount));

        var rowOffsets = GetRowOffsets(numRows, buffer);

        foreach (var rowOffset in rowOffsets)
        {
            if (rowOffset.IsDeleted)
                continue;
            yield return rowOffset.IsLookup
                ? CrackRow(table, rowOffset, buffer, columnsToTake)
                : CrackRow(table, rowOffset, buffer, columnsToTake);
        }
    }

    private ImmutableArray<IMdbValue> CrackRow(MdbTable table, RowOffset row, ReadOnlySpan<byte> buffer, HashSet<string> columnsToTake)
    {
        int numCols = columnsToTake.Count == 0 ? table.Columns.Length : columnsToTake.Count;
        ImmutableArray<IMdbValue>.Builder fields = ImmutableArray.CreateBuilder<IMdbValue>(numCols);
        ReadOnlySpan<byte> region = buffer[row.StartOffset..row.EndOffset];

        short numFixedCols = Db.JetVersion == JetVersion.Jet3 ? region[0] : MdbBinary.ReadInt16LittleEndian(region);

        int nullBitmaskSize = (numFixedCols + 7) / 8;
        BitArray nullBitmask = new(region[^nullBitmaskSize..].ToArray());
        short numVarCols;
        int[] varColOffsets;
        if (table.NumVarCols > 0)
        {
            if (Db.JetVersion == JetVersion.Jet3)
            {
                numVarCols = region[^(nullBitmaskSize + 1)];
                varColOffsets = GetVarColOffsetsJet3(region, nullBitmaskSize, numVarCols);
            }
            else
            {
                numVarCols = MdbBinary.ReadInt16LittleEndian(region[^(nullBitmaskSize + 2)..]);
                varColOffsets = GetVarColOffsetsJet4(region, nullBitmaskSize, numVarCols);

            }
        }
        else
        {
            numVarCols = 0;
            varColOffsets = Array.Empty<int>();
        }
        int fixedStartOffset = Db.JetVersion == JetVersion.Jet3 ? 1 : 2;
        int fixedColsFound = 0;

        for (int i = 0; i < table.Columns.Length; i++)
        {
            MdbColumn? column = table.Columns[i];
            var rowFields = ProcessRow(region, numFixedCols, !nullBitmask[i], numVarCols, varColOffsets, ref fixedColsFound, column, fixedStartOffset);
            if (!columnsToTake.Any() || columnsToTake.Contains(column.Name))
                fields.Add(rowFields);
        }

        return fields.ToImmutable();
    }

    private IMdbValue ProcessRow(ReadOnlySpan<byte> region, short numFixedCols, bool isNull, short numVarCols, int[] varColOffsets, ref int fixedColsFound, MdbColumn column, int fixedStartOffset)
    {
        try
        {
            if (column.Flags.HasFlag(MdbColumnFlags.FixedLength) && fixedColsFound < numFixedCols)
            {
                var field = MdbValueFactory.CreateValue(this, column, isNull, region.Slice(column.OffsetFixed + fixedStartOffset, column.Length).ToImmutableArray());
                fixedColsFound++;
                return field;
            }
            else if (!column.Flags.HasFlag(MdbColumnFlags.FixedLength) && column.OffsetVariable < numVarCols)
            {
                int startOffset = varColOffsets[column.OffsetVariable];
                int endOffset = varColOffsets[column.OffsetVariable + 1];
                ReadOnlySpan<byte> dataRegion = region[startOffset..endOffset];
                return MdbValueFactory.CreateValue(this, column, isNull, dataRegion.ToImmutableArray());

            }
            return MdbValueFactory.CreateValue(this, column, true, ImmutableArray<byte>.Empty);
        }
        catch
        {
            throw;
        }
    }

    private int ReadRowFromLvalPage(int pageNo, int rowNo, Span<byte> oleBuffer)
    {
        Span<byte> buffer = new byte[PageSize];
        ReadPageToBuffer(pageNo, buffer, MdbPageType.Data);

        if (!buffer.Slice(4, 4).SequenceEqual(JetConstants.LvalString))
            throw new FormatException($"Error trying to read LVAL page {pageNo}. (Expected \"LVAL\" at byte 4)");

        int start = MdbBinary.ReadUInt16LittleEndian(buffer.Slice(2 + Db.Constants.TablePage.RowCountOffset + rowNo * 2, 2));

        int nextStart = rowNo == 0 ? PageSize : MdbBinary.ReadUInt16LittleEndian(buffer.Slice(Db.Constants.TablePage.RowCountOffset + rowNo * 2, 2));

        int length = nextStart - start & JetConstants.OffsetMask;
        int startOffset = start & JetConstants.OffsetMask;

        if (startOffset > PageSize || startOffset > nextStart || nextStart > PageSize)
            throw new FormatException($"Error reading long value (page {pageNo}, row {rowNo})");

        buffer.Slice(startOffset, length).CopyTo(oleBuffer);
        return length;
    }

    private (int numRead, int nextPage) ReadRowFromLvalPage2(int pageNo, int rowNo, Span<byte> oleBuffer)
    {
        Span<byte> buffer = new byte[PageSize];
        ReadPageToBuffer(pageNo, buffer, MdbPageType.Data);

        if (!buffer.Slice(4, 4).SequenceEqual(JetConstants.LvalString))
            throw new FormatException($"Error trying to read LVAL page {pageNo}. (Expected \"LVAL\" at byte 4)");

        int start = MdbBinary.ReadUInt16LittleEndian(buffer.Slice(2 + Db.Constants.TablePage.RowCountOffset + rowNo * 2, 2));

        int nextStart = rowNo == 0 ? PageSize : MdbBinary.ReadUInt16LittleEndian(buffer.Slice(Db.Constants.TablePage.RowCountOffset + rowNo * 2, 2));

        int length = nextStart - start & JetConstants.OffsetMask;
        int startOffset = start & JetConstants.OffsetMask;

        ReadOnlySpan<byte> recordBuffer = buffer.Slice(startOffset, length);
        int nextPage = MdbBinary.ReadInt32LittleEndian(recordBuffer);
        if (startOffset > PageSize || startOffset > nextStart || nextStart > PageSize)
            throw new FormatException($"Error reading long value (page {pageNo}, row {rowNo})");

        recordBuffer[4..].CopyTo(oleBuffer);

        return (length - 4, nextPage);
    }

    private static int[] GetVarColOffsetsJet4(ReadOnlySpan<byte> rowRegion, int nullBitmaskSize, short numVarCols)
    {
        var colPtr = rowRegion.Length - nullBitmaskSize - 2;

        if (nullBitmaskSize + 2 > rowRegion.Length)
            return Array.Empty<int>();

        int[] varColOffsets = new int[numVarCols + 1];

        ReadOnlySpan<byte> colTable = rowRegion[(colPtr - numVarCols * 2 - 2)..(colPtr)];
        for (int i = 0; i < numVarCols + 1; i++)
        {
            ReadOnlySpan<byte> offsetSpan = colTable[^(2 * (i + 1))..];
            varColOffsets[i] = MdbBinary.ReadInt16LittleEndian(offsetSpan);
        }
        return varColOffsets;
    }

    private static int[] GetVarColOffsetsJet3(ReadOnlySpan<byte> rowRegion, int nullBitmaskSize, short numVarCols)
    {
        var numJumps = (rowRegion.Length - 1) / 256;
        var colPtr = rowRegion.Length - nullBitmaskSize - numJumps - 2;

        /* If last jump is a dummy value, ignore it */
        if ((colPtr - numVarCols) / 256 < numJumps)
            numJumps--;

        if (nullBitmaskSize + numJumps + 1 > rowRegion.Length)
            return Array.Empty<int>();

        int[] varColOffsets = new int[numVarCols + 1];

        var jumpsUsed = 0;
        ReadOnlySpan<byte> jumpTable = rowRegion.Slice(Index.FromEnd(nullBitmaskSize + numJumps + 1).GetOffset(rowRegion.Length), numJumps);
        ReadOnlySpan<byte> colTable = rowRegion[(colPtr - numVarCols)..(colPtr + 1)];
        for (int i = 0; i < numVarCols + 1; i++)
        {
            while (jumpsUsed < numJumps && i == jumpTable[^(jumpsUsed + 1)])
            {
                jumpsUsed++;
            }
            varColOffsets[i] = colTable[^(i + 1)] + (jumpsUsed * 256);
        }
        return varColOffsets;
    }

    private RowOffset[] GetRowOffsets(ushort numRows, ReadOnlySpan<byte> buffer)
    {
        // The high byte is not used to get a row offset (the max row offset in Jet4 is 4096 = 2^12)
        // Offsets that have 0x40 in the high order byte point to a location within the page where a
        // Data Pointer (4 bytes) to another data page (also known as an overflow page) is stored.
        // Offsets that have 0x80 in the high order byte are deleted rows.
        ReadOnlySpan<byte> region = buffer.Slice(Db.Constants.DataPage.HeaderSize, numRows * 2);
        RowOffset[] rowOffsets = new RowOffset[numRows];
        for (int i = 0; i < numRows; i++)
        {
            ushort offset = MdbBinary.ReadUInt16LittleEndian(region[(i * 2)..]);
            rowOffsets[i].StartOffset = unchecked((ushort)(
                                       offset & 0b0001_1111_1111_1111));
            rowOffsets[i].IsLookup = (offset & 0b1000_0000_0000_0000) > 0;
            rowOffsets[i].IsDeleted = (offset & 0b0100_0000_0000_0000) > 0;

            rowOffsets[i].EndOffset = i == 0
                ? Db.Constants.PageSize
                : rowOffsets[i - 1].StartOffset;
        }

        return rowOffsets;
    }

    public bool IsDisposed { get; protected set; }

    public abstract void Dispose();
    public abstract ValueTask DisposeAsync();

    [DebuggerDisplay("MdbOffset {StartOffset} - {EndOffset} ({Length} bytes)")]
    internal record struct RowOffset(ushort StartOffset, ushort EndOffset, bool IsLookup, bool IsDeleted)
    {
        public int Length => EndOffset - StartOffset;
    }

    internal MdbTables GetUserTables(IEqualityComparer<string> tableNameComparison)
    {
        MdbTable.Builder tableDef = ReadTableDef(2);
        foreach (var col in tableDef.Columns!)
        {
            if (col.Name == "Name")
                col.OverrideEncoding = Encoding.Unicode;
        }
        MdbTable catalogTable = tableDef.Build("MSysObjects", this);

        var catRows = EnumerateRows(catalogTable, new HashSet<string>()
            {
                "Id",
                "Name",
                 "Type",
                 "Flags"
            });

        var x = catRows.Where(FilterUserTables);

        ImmutableArray<MdbTable> tables = catRows
            .Where(FilterUserTables)
            .Select(CreateMdbTableFromRecord)
            .ToImmutableArray();
        return new MdbTables(tables, tableNameComparison);

    }

    static bool FilterUserTables(MdbDataRow row)
    {
        short type = row.GetNullableInt16("Type") ?? 0;
        int? flags = row.GetNullableInt32("Flags");

        return (type & 0x7F) is 1 && flags.HasValue && flags.Value == 0;
    }

    internal async Task<MdbTables> GetUserTablesAsync(IEqualityComparer<string> tableNameComparison, CancellationToken ct)
    {
        var tableDef = await ReadTableDefAsync(2, ct).ConfigureAwait(false);
        MdbTable catalogTable = tableDef.Build("MSysObjects", this);

        MdbTable[] tables = await EnumerateRowsAsync(catalogTable, new HashSet<string>()
            {
                "Id",
                "Name",
                 "Type",
                 "Flags"
            }, ct)
            .Where(r => r.GetInt16("Type") == 1 && r.GetInt32("Flags") == 0)
            .Select(CreateMdbTableFromRecord)
            .ToArrayAsync(cancellationToken: ct).ConfigureAwait(false);
        // There's no ToImmutableArrayAsync, so use ToArrayAsync and then use Unsafe.As to cast it to an immutable array struct
        ImmutableArray<MdbTable> immutTables = Unsafe.As<MdbTable[], ImmutableArray<MdbTable>>(ref tables);
        return new MdbTables(immutTables, tableNameComparison);
    }

    internal async IAsyncEnumerable<MdbDataRow> EnumerateRowsAsync(MdbTable table, HashSet<string>? columnsToTake, [EnumeratorCancellation] CancellationToken ct)
    {
        byte[] usageMap = await ReadUsageMapAsync(table.UsedPagesPtr, ct).ConfigureAwait(false);

        await foreach (int page in GetUsageMapAsync(usageMap, ct))
        {
            await foreach (ImmutableArray<IMdbValue> row in ReadDataPageAsync(page, table, columnsToTake ?? new(0), ct))
                yield return new(row, Options.TableNameComparison, 10);
        }
    }

    internal IEnumerable<MdbDataRow> EnumerateRows(MdbTable table, HashSet<string>? columnsToTake)
    {
        byte[] usageMap = ReadUsageMap(table.UsedPagesPtr);
        //byte[] freeMap = Reader.ReadUsageMap(table.FreePagesPtr);

        foreach (int page in GetUsageMap(usageMap))
        {
            foreach (ImmutableArray<IMdbValue> row in ReadDataPage(page, table, columnsToTake ?? new(0)))
                yield return new(row, Options.TableNameComparison, 10);
        }
    }

    private MdbTable CreateMdbTableFromRecord(MdbDataRow row)
    {
        int id = row.GetNullableInt32("Id") ?? throw new FormatException("Could not get ID of table");
        string name = row.GetString("Name") ?? throw new FormatException("Could not get Name of table");
        return ReadTableDef(id).Build(name, this);
    }

    [Conditional("DEBUG")]
    protected static void WriteDebug(string message)
    {
        Debug.WriteLine(message);
#if DEBUG
        DebugCallback?.Invoke(message);
#endif
    }

#if DEBUG
    internal static void SetDebugCallback(Action<string> callback) => DebugCallback = callback;

    [ThreadStatic]
    internal static Action<string>? DebugCallback;
#endif

}
