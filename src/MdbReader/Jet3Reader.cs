// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.
//
// Based on code from libmdb (https://github.com/mdbtools/mdbtools)

using MMKiwi.MdbReader.Values;
using System.Collections;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using MMKiwi.MdbReader.Helpers;

namespace MMKiwi.MdbReader;

internal abstract partial class Jet3Reader : IDisposable, IAsyncDisposable
{
    protected Jet3Reader(MdbHeaderInfo db)
    {
        Db = db;
        if (db.DbKey == 0)
            DbKey = null;
        else
            MdbBinary.WriteUInt32LittleEndian(DbKey, db.DbKey);
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

    public async static Task<MdbHeaderInfo> GetDatabaseInfoAsync(Stream mdbStream, CancellationToken ct)
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
        uint dbKey = MdbBinary.ReadUInt32LittleEndian(encryptedHeader[constants.DbPage.DbKeyOffset..]);

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
        int pageNo = mapPtr >> 8;

        byte[] buffer = new byte[PageSize];

        await ReadPageToBufferAsync(pageNo, buffer, MdbPageType.Data, ct).ConfigureAwait(false);

        ushort rowLocation = MdbBinary.ReadUInt16LittleEndian(buffer.AsSpan(10));
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
            Misc = columnSlice[c.Misc].ToImmutableArray(),
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

    internal async Task<int> FindNextMapAsync(byte[] map, int startPage, CancellationToken ct)
    {
        var c = Db.Constants.UsageMap;
        if (map[0] == 0) // Type 0 usage map
        {
            int pageStart = MdbBinary.ReadInt32LittleEndian(map.AsSpan(1));
            BitArray usageBitmap = new(map[5..]);
            for (int j = startPage > pageStart ? startPage - pageStart + 1 : 0; j < usageBitmap.Length; j++)
                if (usageBitmap[j])
                    return pageStart + j;
            return 0;
        }
        else if (map[0] == 1)
        {

            int maxPages = (map.Length - 1) / 4;
            for (int i = (startPage + 1) / c.UsageMapLen; i < maxPages; i++)
            {
                int mapPage = MdbBinary.ReadInt32LittleEndian(map.AsSpan(i * 4 + 1));
                if (mapPage == 0)
                    continue;
                byte[] buffer = new byte[PageSize];
                await ReadPageToBufferAsync(mapPage, buffer, MdbPageType.PageUseageBitmap, ct).ConfigureAwait(false);

                BitArray usageBitmap = new(buffer[4..]);
                for (int j = startPage + 1; j < usageBitmap.Length; j++)
                    if (usageBitmap[j])
                        return j;
            }
            return 0;
        }
        throw new FormatException();
    }

    internal int FindNextMap(byte[] map, int startPage)
    {
        var c = Db.Constants.UsageMap;
        if (map[0] == 0) // Type 0 usage map
        {
            int pageStart = MdbBinary.ReadInt32LittleEndian(map.AsSpan(1));
            BitArray usageBitmap = new(map[5..]);
            for (int j = startPage > pageStart ? startPage - pageStart + 1 : 0; j < usageBitmap.Length; j++)
                if (usageBitmap[j])
                    return pageStart + j;
            return 0;
        }
        else if (map[0] == 1)
        {

            int maxPages = (map.Length - 1) / 4;
            for (int i = (startPage + 1) / c.UsageMapLen; i < maxPages; i++)
            {
                int mapPage = MdbBinary.ReadInt32LittleEndian(map.AsSpan(i * 4 + 1));
                if (mapPage == 0)
                    continue;
                byte[] buffer = new byte[PageSize];
                ReadPageToBuffer(mapPage, buffer, MdbPageType.PageUseageBitmap);

                BitArray usageBitmap = new(buffer[4..]);
                for (int j = startPage + 1; j < usageBitmap.Length; j++)
                    if (usageBitmap[j])
                        return j;
            }
            return 0;
        }
        throw new FormatException();
    }

    internal async IAsyncEnumerable<List<IMdbValue>> ReadDataPageAsync(int page, MdbTable table, HashSet<string> columnsToTake, [EnumeratorCancellation] CancellationToken ct)
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

        ushort numRows = MdbBinary.ReadUInt16LittleEndian(buffer.AsSpan(8));

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

    internal IEnumerable<List<IMdbValue>> ReadDataPage(int page, MdbTable table, HashSet<string> columnsToTake)
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

        ushort numRows = MdbBinary.ReadUInt16LittleEndian(buffer.AsSpan(8));

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

    private List<IMdbValue> CrackRow(MdbTable table, RowOffset row, ReadOnlySpan<byte> buffer, HashSet<string> columnsToTake)
    {
        List<IMdbValue> fields = new(table.Columns.Length);
        ReadOnlySpan<byte> region = buffer[row.StartOffset..row.EndOffset];

        byte numFixedCols = region[0];

        int nullBitmaskSize = (numFixedCols + 7) / 8;
        BitArray nullBitmask = new(region[^nullBitmaskSize..].ToArray());
        byte numVarCols;
        int[] varColOffsets;
        if (table.NumVarCols > 0)
        {
            numVarCols = region[^(nullBitmaskSize + 1)];
            varColOffsets = GetVarColOffsets(region, nullBitmaskSize, numVarCols);
        }
        else
        {
            numVarCols = 0;
            varColOffsets = Array.Empty<int>();
        }
        int fixedColsFound = 0;

        for (int i = 0; i < table.Columns.Length; i++)
        {
            MdbColumn? column = table.Columns[i];
            if (columnsToTake.Any() && !columnsToTake.Contains(column.Name))
                continue;

            fields.Add(ProcessRow(region, numFixedCols, !nullBitmask[i], numVarCols, varColOffsets, ref fixedColsFound, column));
        }

        return fields;
    }

    private IMdbValue ProcessRow(ReadOnlySpan<byte> region, byte numFixedCols, bool isNull, byte numVarCols, int[] varColOffsets, ref int fixedColsFound, MdbColumn column)
    {
        if (column.Flags.HasFlag(MdbColumnFlags.FixedLength) && fixedColsFound < numFixedCols)
        {
            var field = MdbValueFactory.CreateValue(this, column, isNull, region.Slice(column.OffsetFixed + 1, column.Length).ToImmutableArray());
            fixedColsFound++;
            return field;
        }
        else if (!column.Flags.HasFlag(MdbColumnFlags.FixedLength) && column.OffsetVariable < numVarCols)
        {
            var startOffset = varColOffsets[column.OffsetVariable];
            var endOffset = varColOffsets[column.OffsetVariable + 1];
            return MdbValueFactory.CreateValue(this, column, isNull, region[startOffset..endOffset].ToImmutableArray());

        }
        return MdbValueFactory.CreateValue(this, column, true, ImmutableArray<byte>.Empty);
    }

    private int ReadRowFromLvalPage(int pageNo, int rowNo, Span<byte> oleBuffer)
    {
        Span<byte> buffer = new byte[PageSize];
        ReadPageToBuffer(pageNo, buffer, MdbPageType.Data);

        if (!buffer.Slice(4, 4).SequenceEqual(JetConstants.LvalString))
            throw new FormatException($"Error trying to read LVAL page {pageNo}. (Expected \"LVAL\" at byte 4)");

        int start = MdbBinary.ReadUInt16LittleEndian(buffer.Slice(2 + JetConstants.RowCountOffset + rowNo * 2, 2));

        int nextStart = rowNo == 0 ? PageSize : MdbBinary.ReadUInt16LittleEndian(buffer.Slice(JetConstants.RowCountOffset + rowNo * 2, 2));

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

        int start = MdbBinary.ReadUInt16LittleEndian(buffer.Slice(2 + JetConstants.RowCountOffset + rowNo * 2, 2));

        int nextStart = rowNo == 0 ? PageSize : MdbBinary.ReadUInt16LittleEndian(buffer.Slice(JetConstants.RowCountOffset + rowNo * 2, 2));

        int length = nextStart - start & JetConstants.OffsetMask;
        int startOffset = start & JetConstants.OffsetMask;

        ReadOnlySpan<byte> recordBuffer = buffer.Slice(startOffset, length);
        int nextPage = MdbBinary.ReadInt32LittleEndian(recordBuffer);
        if (startOffset > PageSize || startOffset > nextStart || nextStart > PageSize)
            throw new FormatException($"Error reading long value (page {pageNo}, row {rowNo})");

        recordBuffer[4..].CopyTo(oleBuffer);

        return (length - 4, nextPage);
    }

    private static int[] GetVarColOffsets(ReadOnlySpan<byte> rowRegion, int nullBitmaskSize, byte numVarCols)
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

        for (int i = 0; i < numVarCols + 1; i++)
        {
            while (jumpsUsed < numJumps && i == rowRegion[Index.FromEnd(nullBitmaskSize + jumpsUsed + i)])
            {
                jumpsUsed++;
            }
            varColOffsets[i] = rowRegion[colPtr - i] + (jumpsUsed * 256);
        }
        return varColOffsets;
    }

    private RowOffset[] GetRowOffsets(ushort numRows, ReadOnlySpan<byte> buffer)
    {
        // The high byte is not used to get a row offset (the max row offset in Jet4 is 4096 = 2^12)
        // Offsets that have 0x40 in the high order byte point to a location within the page where a
        // Data Pointer (4 bytes) to another data page (also known as an overflow page) is stored.
        // Offsets that have 0x80 in the high order byte are deleted rows.
        ReadOnlySpan<byte> region = buffer.Slice(10, numRows * 2);
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

    internal MdbTables GetUserTables()
    {
        MdbTable.Builder tableDef = ReadTableDef(2);
        MdbTable catalogTable = tableDef.Build("MSysObjects", this);

        ImmutableArray<MdbTable> tables = EnumerateRows(catalogTable, new HashSet<string>()
            {
                "Id",
                "Name",
                 "Type",
                 "Flags"
            })
            .Where(r => ((MdbIntValue.Nullable)r.Values.First(f => f.Column.Name == "Type")).Value == 1 &&
                        ((MdbLongIntValue.Nullable)r.Values.First(f => f.Column.Name == "Flags")).Value == 0)
            .Select(CreateMdbTableFromRecord)
            .ToImmutableArray();
        return new MdbTables(tables);
    }


    internal async Task<MdbTables> GetUserTablesAsync(CancellationToken ct)
    {
        var tableDef = await ReadTableDefAsync(2, ct).ConfigureAwait(false);
        MdbTable catalogTable = tableDef.Build("MSysObjects", this);

        var tables = await EnumerateRowsAsync(catalogTable, new HashSet<string>()
            {
                "Id",
                "Name",
                 "Type",
                 "Flags"
            }, ct)
            .Where(r => ((MdbIntValue.Nullable)r.Values.First(f => f.Column.Name == "Type")).Value == 1 &&
                        ((MdbLongIntValue.Nullable)r.Values.First(f => f.Column.Name == "Flags")).Value == 0)
            .Select(CreateMdbTableFromRecord)
            .ToArrayAsync(cancellationToken: ct).ConfigureAwait(false);
        ImmutableArray<MdbTable> immutTables = Unsafe.As<MdbTable[], ImmutableArray<MdbTable>>(ref tables);
        return new MdbTables(immutTables);
    }

    internal async IAsyncEnumerable<MdbDataRow> EnumerateRowsAsync(MdbTable table, HashSet<string>? columnsToTake, [EnumeratorCancellation] CancellationToken ct)
    {
        byte[] usageMap = await ReadUsageMapAsync(table.UsedPagesPtr, ct).ConfigureAwait(false);

        int page = 0;
        while (true)
        {
            page = await FindNextMapAsync(usageMap, page, ct).ConfigureAwait(false);
            if (page == 0)
                break;
            await foreach (var row in ReadDataPageAsync(page, table, columnsToTake ?? new(0), ct))
                yield return new(row);
        }
    }

    internal IEnumerable<MdbDataRow> EnumerateRows(MdbTable table, HashSet<string>? columnsToTake)
    {
        byte[] usageMap = ReadUsageMap(table.UsedPagesPtr);
        //byte[] freeMap = Reader.ReadUsageMap(table.FreePagesPtr);

        int page = 0;
        while (true)
        {
            page = FindNextMap(usageMap, page);
            if (page == 0)
                break;
            foreach (var row in ReadDataPage(page, table, columnsToTake ?? new(0)))
                yield return new(row);
        }
    }

    private MdbTable CreateMdbTableFromRecord(MdbDataRow row)
    {
        var result = row.Values.ToDictionary(field => field.Column!.Name);
        int id = ((MdbLongIntValue.Nullable)result["Id"]).Value ?? throw new FormatException("Could not get ID of table");
        string name = ((MdbStringValue.Nullable)result["Name"]).Value ?? throw new FormatException("Could not get Name of table");
        return ReadTableDef(id).Build(name, this);
    }


}
