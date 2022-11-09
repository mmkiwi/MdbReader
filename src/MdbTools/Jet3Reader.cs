// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.
//
// Based on code from libmdb (https://github.com/mdbtools/mdbtools)

using MMKiwi.MdbTools.Values;
using MMKiwi.MdbTools.Mutable;
using System.Collections;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using MMKiwi.MdbTools.Helpers;

namespace MMKiwi.MdbTools;

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
        if (mdbStream.Length < Constants.Jet3.PageSize * 3)
            throw new InvalidDataException("File is not JET database. File too short.");

        byte[] header = new byte[Constants.Jet3.PageSize];
        mdbStream.Seek(0, SeekOrigin.Begin);
        mdbStream.Read(header);

        if (MdbBinary.ReadUInt32LittleEndian(header) != Constants.MagicNumber)
            throw new InvalidDataException("File is not JET database. Missing magic number.");

        JetVersion jetVersion = (JetVersion)header[0x14];

        if (!Enum.IsDefined(typeof(JetVersion), jetVersion))
            Debug.WriteLine($"Version {jetVersion} is unknown. Continuing assuming JET4+");

        // Get the constants for the jet version
        Constants constants = new(jetVersion);

        // "Standard JET Db" (*.mdb, Jet 3 or 4) or "Standard ACE DB" (*.accdb) as ASCII
        var fileFormatId = constants.FileFormatId;

        if (!header.AsSpan(0x04, fileFormatId.Length).SequenceEqual(fileFormatId))
            throw new InvalidDataException("File is not JET database. Invalid file format ID");

        byte[] tempRc4Key = new byte[] { 0xC7, 0xDA, 0x39, 0x6B };
        var encryptedHeader = header.AsSpan(0x18, constants.HeaderSize);
        // The next 126 or 128 bytes are RC4 encoded, decode in memory
        RC4.ApplyInPlace(encryptedHeader, tempRc4Key);

        ushort collation = MdbBinary.ReadUInt16LittleEndian(encryptedHeader[constants.CollationOffset..]);
        ushort codePage = MdbBinary.ReadUInt16LittleEndian(encryptedHeader[Constants.CodePageOffset..]);
        uint dbKey = MdbBinary.ReadUInt32LittleEndian(encryptedHeader[Constants.DbKeyOffset..]);
        ImmutableArray<byte> dbPasswd = encryptedHeader.Slice(Constants.DbPasswdOffset, constants.DbPasswdSize).ToImmutableArray();
        DateTime creationDate = ConversionFunctions.AsDateTime(encryptedHeader[Constants.CreationDateOffset..]);

        return new(jetVersion, collation, codePage, dbKey, dbPasswd, creationDate, constants);
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
        int pageNo = mapPtr >> 8;

        byte[] buffer = new byte[PageSize];

        ReadPageToBuffer(pageNo, buffer, MdbPageType.Data);

        ushort rowLocation = MdbBinary.ReadUInt16LittleEndian(buffer.AsSpan(10));
        int mapSize = PageSize - rowLocation;

        return buffer.AsSpan(rowLocation, mapSize).ToArray();
    }

    internal async Task<MdbBuilder.Table> ReadTableDefAsync(int startPage, CancellationToken ct)
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
        return ParseTable(startPage, buffer, Db.Encoding);
    }

    internal MdbBuilder.Table ReadTableDef(int startPage)
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
        return ParseTable(startPage, buffer, Db.Encoding);
    }

    private MdbBuilder.Table ParseTable(int startPage, ReadOnlySpan<byte> buffer, Encoding encoding)
    {

        // This will always be on the first page of the table def
        MdbBuilder.Table table = new(
            firstPage: startPage,
            numRows: MdbBinary.ReadInt32LittleEndian(buffer.Slice(4, 4)),
            nextAutoNum: MdbBinary.ReadInt32LittleEndian(buffer.Slice(8, 4)),
            tableType: (MdbTableType)buffer[12],
            maxCols: MdbBinary.ReadUInt16LittleEndian(buffer.Slice(13, 2)),
            numVarCols: MdbBinary.ReadUInt16LittleEndian(buffer.Slice(15, 2)),
            numCols: MdbBinary.ReadUInt16LittleEndian(buffer.Slice(17, 2)),
            numIndexes: MdbBinary.ReadInt32LittleEndian(buffer.Slice(19, 4)),
            numRealIndexes: MdbBinary.ReadInt32LittleEndian(buffer.Slice(23, 4)),
            usedPagesPtr: MdbBinary.ReadInt32LittleEndian(buffer.Slice(27, 4)),
            freePagesPtr: MdbBinary.ReadInt32LittleEndian(buffer.Slice(31, 4))
        );

        int cursor = 35;
        for (int i = 0; i < table.NumRealIndexes; i++)
        {
            // first four bytes are zero
            // second four bytes are the number of index rows (currently unused)
            table.RealIndices[i] = new MdbBuilder.RealIndex
            {
                NumIndexRows = MdbBinary.ReadInt32LittleEndian(buffer.Slice(cursor + 4, 4))
            };
            cursor += 8;
        }

        for (int i = 0; i < table.NumCols; i++)
        {
            table.Columns[i] = ProcessColumn(ref cursor, buffer, encoding);
        }
        for (int i = 0; i < table.NumCols; i++)
        {
            byte colNameLen = buffer[cursor];
            table.Columns[i].Name = Db.Encoding.GetString(buffer.Slice(cursor + 1, colNameLen));
            cursor += 1 + colNameLen;
        }

        for (int i = 0; i < table.NumRealIndexes; i++)
        {
            ProcessRealIndex(ref cursor, table.RealIndices[i], buffer);
        }

        for (int i = 0; i < table.NumIndexes; i++)
        {
            table.Indices[i] = ProcessIndex(ref cursor, buffer);
        }

        for (int i = 0; i < table.NumIndexes; i++)
        {
            byte indexNameLength = buffer[cursor];
            table.Indices[i].Name = Db.Encoding.GetString(buffer.Slice(cursor + 1, indexNameLength));
            cursor += 1 + indexNameLength;
        }

        while (true)
        {
            if (!ProcessVarCol(ref cursor, table.Columns, buffer))
                break;
        }

        return table;
    }

    private static bool ProcessVarCol(ref int cursor, MdbBuilder.Column[] columns, ReadOnlySpan<byte> buffer)
    {
        ReadOnlySpan<byte> region = buffer.Slice(cursor, 10);

        ushort colNum = MdbBinary.ReadUInt16LittleEndian(region);
        if (colNum == ushort.MaxValue)
        {
            cursor += 2;
            return false;
        }
        else
        {
            columns[colNum].UsedPages = MdbBinary.ReadInt32LittleEndian(region[2..]);
            columns[colNum].FreePages = MdbBinary.ReadInt32LittleEndian(region[6..]);
            cursor += 10;
            return true;
        }
    }

    internal static MdbBuilder.Column ProcessColumn(ref int cursor, ReadOnlySpan<byte> buffer, Encoding encoding)
    {
        ReadOnlySpan<byte> columnEntry = buffer.Slice(cursor, 18);
        cursor += 18;
        return new(encoding)
        {
            Type = (MdbColumnType)columnEntry[0],
            NumInclDeleted = MdbBinary.ReadUInt16LittleEndian(columnEntry[1..]),
            OffsetVariable = MdbBinary.ReadUInt16LittleEndian(columnEntry[3..]),
            ColNum = MdbBinary.ReadUInt16LittleEndian(columnEntry[5..]),
            SortOrder = MdbBinary.ReadUInt16LittleEndian(columnEntry[7..]),
            Locale = MdbBinary.ReadUInt16LittleEndian(columnEntry[9..]),
            //skip next 2 bytes
            Flags = (MdbColumnFlags)columnEntry[13],
            OffsetFixed = MdbBinary.ReadUInt16LittleEndian(columnEntry[14..]),
            Length = MdbBinary.ReadUInt16LittleEndian(columnEntry[16..])
        };
    }

    internal static MdbBuilder.Index ProcessIndex(ref int cursor, ReadOnlySpan<byte> buffer)
    {
        ReadOnlySpan<byte> region = buffer.Slice(cursor, 20);
        MdbBuilder.Index index = new()
        {
            IndexNum = MdbBinary.ReadInt32LittleEndian(region),
            IndexNum2 = MdbBinary.ReadInt32LittleEndian(region[4..]),
            RelTableType = region[8],
            RlIndexNum = MdbBinary.ReadInt32LittleEndian(region[9..]),
            RelTablePage = MdbBinary.ReadInt32LittleEndian(region[13..]),
            CascadeUpdates = region[17] == 1,
            CascadeDeletes = region[18] == 1,
            IndexType = (MdbIndexType)region[19],
        };
        cursor += 20;
        return index;
    }

    internal string? ProcessIndexName(ref int cursor, ReadOnlySpan<byte> buffer)
    {
        byte indexNameLen = buffer[cursor];
        ReadOnlySpan<byte> indexNameEntry = buffer.Slice(cursor, indexNameLen + 1);
        cursor += 1 + indexNameLen;
        return Db.Encoding.GetString(indexNameEntry[1..]);
    }

    internal static void ProcessRealIndex(ref int cursor, in MdbBuilder.RealIndex realIndex, ReadOnlySpan<byte> buffer)
    {
        ReadOnlySpan<byte> region = buffer.Slice(cursor, 39);
        for (int i = 0; i < 10; i++)
        {
            realIndex.Columns[i] = new MdbBuilder.RealIndexColumn
            {
                ColNum = MdbBinary.ReadUInt16LittleEndian(region[(i * 3)..]),
                ColOrder = region[i * 3 + 2]
            };
        }
        realIndex.UsedPages = MdbBinary.ReadInt32LittleEndian(region[30..]);
        realIndex.FirstDataPointer = MdbBinary.ReadInt32LittleEndian(region[34..]);
        realIndex.Flags = region[38];

        cursor += 39;
    }
    private async Task<(int nextPage, int dataLength)> ReadNextTablePageAsync(int nextPage, Memory<byte> buffer, CancellationToken ct)
    {
        byte[] header = new byte[16];
        await ReadPageToBufferAsync(nextPage, header, MdbPageType.TableDefinition, ct).ConfigureAwait(false);
        //Header is the first 8 bytes. The first byte is the PageType == 0x02

        // Skip the second byte
        // Third and fourth bytes is the word "VC"
        if (!header.AsSpan(2, 2).SequenceEqual(Constants.TableDef.TDefId))
            throw new FormatException("Invalid JET3 table (missing 'VC' in header)");

        // Bytes 4-7 are the pointer to the next page if the table definiton spans
        // multiple pages.
        int newNextPage = MdbBinary.ReadInt32LittleEndian(header.AsSpan(4));
        int dataLength = MdbBinary.ReadInt32LittleEndian(header.AsSpan(8));

        if (buffer.Length > 0) // first time around, we won't 
            await ReadPartialPageToBufferAsync(nextPage, buffer[..Math.Min(buffer.Length, PageSize - 8)], 8, ct).ConfigureAwait(false);

        return (newNextPage, dataLength);
    }

    private (int nextPage, int dataLength) ReadNextTablePage(int nextPage, Span<byte> buffer)
    {
        byte[] header = new byte[16];
        ReadPageToBuffer(nextPage, header, MdbPageType.TableDefinition);
        //Header is the first 8 bytes. The first byte is the PageType == 0x02

        // Skip the second byte
        // Third and fourth bytes is the word "VC"
        if (!header.AsSpan(2, 2).SequenceEqual(Constants.TableDef.TDefId))
            throw new FormatException("Invalid JET3 table (missing 'VC' in header)");

        // Bytes 4-7 are the pointer to the next page if the table definiton spans
        // multiple pages.
        int newNextPage = MdbBinary.ReadInt32LittleEndian(header.AsSpan(4));
        int dataLength = MdbBinary.ReadInt32LittleEndian(header.AsSpan(8));

        if (buffer.Length > 0) // first time around, we won't 
            ReadPartialPageToBuffer(nextPage, buffer[..Math.Min(buffer.Length, PageSize - 8)], 8);

        return (newNextPage, dataLength);
    }
    internal async Task<int> FindNextMapAsync(byte[] map, int startPage, CancellationToken ct)
    {
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
            for (int i = (startPage + 1) / Constants.Jet3.UsageMapLen; i < maxPages; i++)
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
            for (int i = (startPage + 1) / Constants.Jet3.UsageMapLen; i < maxPages; i++)
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

        if (!buffer.Slice(4, 4).SequenceEqual(Constants.LvalString))
            throw new FormatException($"Error trying to read LVAL page {pageNo}. (Expected \"LVAL\" at byte 4)");

        int start = MdbBinary.ReadUInt16LittleEndian(buffer.Slice(2 + Constants.RowCountOffset + rowNo * 2, 2));

        int nextStart = rowNo == 0 ? PageSize : MdbBinary.ReadUInt16LittleEndian(buffer.Slice(Constants.RowCountOffset + rowNo * 2, 2));

        int length = nextStart - start & Constants.OffsetMask;
        int startOffset = start & Constants.OffsetMask;

        if (startOffset > PageSize || startOffset > nextStart || nextStart > PageSize)
            throw new FormatException($"Error reading long value (page {pageNo}, row {rowNo})");

        buffer.Slice(startOffset, length).CopyTo(oleBuffer);
        return length;
    }

    private (int numRead, int nextPage) ReadRowFromLvalPage2(int pageNo, int rowNo, Span<byte> oleBuffer)
    {
        Span<byte> buffer = new byte[PageSize];
        ReadPageToBuffer(pageNo, buffer, MdbPageType.Data);

        if (!buffer.Slice(4, 4).SequenceEqual(Constants.LvalString))
            throw new FormatException($"Error trying to read LVAL page {pageNo}. (Expected \"LVAL\" at byte 4)");

        int start = MdbBinary.ReadUInt16LittleEndian(buffer.Slice(2 + Constants.RowCountOffset + rowNo * 2, 2));

        int nextStart = rowNo == 0 ? PageSize : MdbBinary.ReadUInt16LittleEndian(buffer.Slice(Constants.RowCountOffset + rowNo * 2, 2));

        int length = nextStart - start & Constants.OffsetMask;
        int startOffset = start & Constants.OffsetMask;

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

    protected internal class Constants
    {
        public Constants(JetVersion version)
        {
            Version = version;
            PageSize = version is JetVersion.Jet3 ? Jet3.PageSize : Jet4.PageSize;
        }
        public JetVersion Version { get; }
        public ushort PageSize { get; } // Accessed a lot, so cache this value

        // First page constants
        public ReadOnlySpan<byte> FileFormatId => Version is JetVersion.Jet3 or JetVersion.Jet4 ? Jet3.JetHeader : Jet4.ACEHeader;
        public int CollationOffset => Version is JetVersion.Jet3 ? Jet3.CollationOffset : Jet4.CollationOffset;
        public int HeaderSize => Version is JetVersion.Jet3 ? Jet3.HeaderSize : Jet4.HeaderSize;
        public const int CodePageOffset = 0x24;
        public const int DbKeyOffset = 0x26;
        public const int DbPasswdOffset = 0x2a;
        public int DbPasswdSize => Version is JetVersion.Jet3 ? Jet3.DbPasswdSize : Jet4.DbPasswdSize;
        public const int CreationDateOffset = 0x5A;

        public const ushort OffsetMask = 0x1fff;
        public const int RowCountOffset = 8;
        internal const uint MagicNumber = 0x100;
        public static ReadOnlySpan<byte> LvalString => "LVAL"u8;

        public static class Jet3
        {
            public const ushort PageSize = 2048;

            // First page constants
            public static ReadOnlySpan<byte> JetHeader => "Standard Jet DB"u8;
            public const int HeaderSize = 126;
            public const int CollationOffset = 0x22;
            public const int DbPasswdSize = 20;

            public const int UsageMapLen = (PageSize - 4) * 8;


        }

        public static class Jet4
        {
            public const ushort PageSize = 4096;

            // First page constants
            public static ReadOnlySpan<byte> ACEHeader => "Standard ACE DB"u8;
            public const int HeaderSize = 128;
            public const int CollationOffset = 0x22;
            public const int DbPasswdSize = 40;

            public const int UsageMapLen = (PageSize - 4) * 8;


        }

        public static class TableDef
        {
            public const byte Header = 0x02;
            public static ReadOnlySpan<byte> TDefId => "VC"u8;
        }



    }
}
