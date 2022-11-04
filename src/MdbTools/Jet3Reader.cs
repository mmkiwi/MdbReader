// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.
//
// Based on code from libmdb (https://github.com/mdbtools/mdbtools)

using MMKiwi.MdbTools.Mutable;

using System.Buffers.Binary;
using System.Collections;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;

namespace MMKiwi.MdbTools;

public sealed class Jet3Reader : IDisposable, IAsyncDisposable
{
    internal Jet3Reader(Stream mdbStream)
    {
        MdbStream = mdbStream;
        PageBuffer = new byte[Constants.PageSize];
#warning TODO read header
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        Encoding = Encoding.GetEncoding(1252);
    }

    Encoding Encoding { get; }

    public Stream MdbStream { get; }
    private byte[] PageBuffer { get; }
    public int CurrentPage { get; private set; }

    private async Task ReadPageAsync(int pageNo, CancellationToken ct)
    {
        CurrentPage = pageNo;
        MdbStream.Seek(pageNo * Constants.PageSize, SeekOrigin.Begin);
        await MdbStream.ReadAsync(new Memory<byte>(PageBuffer), ct).ConfigureAwait(false);
    }

    private Task<byte[]> ReadPageToBufferAsync(int pageNo, CancellationToken ct) => ReadPageToBufferAsync(pageNo, 0, Constants.PageSize, ct);

    private async Task<byte[]> ReadPageToBufferAsync(int pageNo, int start, int size, CancellationToken ct)
    {
        byte[] res = new byte[size];
        await ReadPageToBufferAsync(pageNo, start, res, ct).ConfigureAwait(false);
        return res;
    }

    private async Task ReadPageToBufferAsync(int pageNo, int start, Memory<byte> buffer, CancellationToken ct)
    {
        MdbStream.Seek(pageNo * Constants.PageSize + start, SeekOrigin.Begin);
        await MdbStream.ReadAsync(buffer, ct).ConfigureAwait(false);
    }
    private void ReadPage(int pageNo)
    {
        CurrentPage = pageNo;
        MdbStream.Seek(pageNo * Constants.PageSize, SeekOrigin.Begin);
        MdbStream.ReadExactly(PageBuffer);
    }

    internal async Task<byte[]> ReadUsageMap(int mapPtr, CancellationToken ct)
    {
        int pageNo = mapPtr >> 8;
        uint row = unchecked((uint)mapPtr & 0xff);

        await ReadPageAsync(pageNo, ct).ConfigureAwait(false);

        ThrowIfWrongPageType(PageType.Data);
        //byte 2 is unknown

        ushort freeSpace = BinaryPrimitives.ReadUInt16LittleEndian(PageBuffer.AsSpan(2));
        uint tdefPtr = BinaryPrimitives.ReadUInt32LittleEndian(PageBuffer.AsSpan(4));
        ushort numRows = BinaryPrimitives.ReadUInt16LittleEndian(PageBuffer.AsSpan(8));
        int cursor = 10;
        ushort rowLocation = BinaryPrimitives.ReadUInt16LittleEndian(PageBuffer.AsSpan(cursor));
        int mapSize = Constants.PageSize - rowLocation;

        return PageBuffer.AsSpan(rowLocation, mapSize).ToArray();
    }

    internal async Task<MdbBuilder.Table> ReadTableDefAsync(int startPage, CancellationToken ct)
    {
        byte[] headerBuffer = new byte[12];
        await ReadPageToBufferAsync(startPage, 0, headerBuffer, ct).ConfigureAwait(false);

        //Header is the first 8 bytes. The first byte is the PageType == 0x02
        ThrowIfWrongPageType(PageType.TableDefinition, headerBuffer);

        // Skip the second byte
        // Third and fourth bytes is the word "VC"
        if (!headerBuffer.AsSpan(2, 2).ByteArrayCompare(Constants.TableDef.TDefId))
            throw new FormatException("Invalid JET3 table (missing 'VC' in header)");

        // Bytes 4-7 are the pointer to the next page if the table definiton spans
        // multiple pages.
        int nextPage = BinaryPrimitives.ReadInt32LittleEndian(headerBuffer.AsSpan(4));


        // Get the length of the data for this page
        int dataLength = BinaryPrimitives.ReadInt32LittleEndian(headerBuffer.AsSpan(8));
        int usedDataLength = Constants.PageSize - 8;

        byte[] buffer = new byte[dataLength];
        await ReadPageToBufferAsync(startPage, 8, buffer.AsMemory(0, Math.Min(dataLength, Constants.PageSize - 8)), ct).ConfigureAwait(false);

        while (dataLength > usedDataLength)
        {
            nextPage = await ReadNextTablePage(nextPage, buffer.AsMemory(usedDataLength), ct).ConfigureAwait(false);
            usedDataLength += Constants.PageSize - 8;
        }

        // This will always be on the first page of the table def
        MdbBuilder.Table table = new(
            firstPage: startPage,
            numRows: BinaryPrimitives.ReadInt32LittleEndian(buffer.AsSpan(4)),
            nextAutoNum: BinaryPrimitives.ReadInt32LittleEndian(buffer.AsSpan(8)),
            tableType: (TableType)buffer[12],
            maxCols: BinaryPrimitives.ReadUInt16LittleEndian(buffer.AsSpan(13)),
            numVarCols: BinaryPrimitives.ReadUInt16LittleEndian(buffer.AsSpan(15)),
            numCols: BinaryPrimitives.ReadUInt16LittleEndian(buffer.AsSpan(17)),
            numIndexes: BinaryPrimitives.ReadInt32LittleEndian(buffer.AsSpan(19)),
            numRealIndexes: BinaryPrimitives.ReadInt32LittleEndian(buffer.AsSpan(23)),
            usedPagesPtr: BinaryPrimitives.ReadInt32LittleEndian(buffer.AsSpan(27)),
            freePagesPtr: BinaryPrimitives.ReadInt32LittleEndian(buffer.AsSpan(31))
        );

        int cursor = 35;
        for (int i = 0; i < table.NumRealIndexes; i++)
        {
            // first four bytes are zero
            // second four bytes are the number of index rows (currently unused)
            table.RealIndices[i] = new MdbBuilder.RealIndex
            {
                NumIndexRows = BinaryPrimitives.ReadInt32LittleEndian(buffer.AsSpan(cursor + 4))
            };
            cursor += 8;
        }


        for (int i = 0; i < table.NumCols; i++)
        {
            table.Columns[i] = ProcessColumn(ref cursor, buffer);
        }
        for (int i = 0; i < table.NumCols; i++)
        {
            byte colNameLen = buffer[cursor];
            table.Columns[i].Name = Encoding.GetString(buffer.AsSpan(cursor + 1, colNameLen));
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
            table.Indices[i].Name = Encoding.GetString(buffer.AsSpan(cursor + 1, indexNameLength));
            cursor += 1 + indexNameLength;
        }

        while (true)
        {
            if (!ProcessVarCol(ref cursor, table.Columns, buffer))
                break;
        }

        return table;
    }

    private static bool ProcessVarCol(ref int cursor, MdbBuilder.Column[] columns, byte[] buffer)
    {
        ReadOnlySpan<byte> region = buffer.AsSpan(cursor, 10);

        ushort colNum = BinaryPrimitives.ReadUInt16LittleEndian(region);
        if (colNum == ushort.MaxValue)
        {
            cursor += 2;
            return false;
        }
        else
        {
            columns[colNum].UsedPages = BinaryPrimitives.ReadInt32LittleEndian(region[2..]);
            columns[colNum].FreePages = BinaryPrimitives.ReadInt32LittleEndian(region[6..]);
            cursor += 10;
            return true;
        }
    }

    internal static MdbBuilder.Column ProcessColumn(ref int cursor, byte[] buffer)
    {
        ReadOnlySpan<byte> columnEntry = buffer.AsSpan(cursor, 18);
        cursor += 18;
        return new()
        {
            Type = (ColumnType)columnEntry[0],
            NumInclDeleted = BinaryPrimitives.ReadUInt16LittleEndian(columnEntry[1..]),
            OffsetVariable = BinaryPrimitives.ReadUInt16LittleEndian(columnEntry[3..]),
            ColNum = BinaryPrimitives.ReadUInt16LittleEndian(columnEntry[5..]),
            SortOrder = BinaryPrimitives.ReadUInt16LittleEndian(columnEntry[7..]),
            Misc = BinaryPrimitives.ReadUInt16LittleEndian(columnEntry[9..]),
            //skip next 2 bytes
            Flags = (ColumnFlags)columnEntry[13],
            Offset = BinaryPrimitives.ReadUInt16LittleEndian(columnEntry[14..]),
            Length = BinaryPrimitives.ReadUInt16LittleEndian(columnEntry[16..])
        };
    }

    internal static MdbBuilder.Index ProcessIndex(ref int cursor, byte[] buffer)
    {
        ReadOnlySpan<byte> region = buffer.AsSpan(cursor, 20);
        MdbBuilder.Index index = new()
        {
            IndexNum = BinaryPrimitives.ReadInt32LittleEndian(region),
            IndexNum2 = BinaryPrimitives.ReadInt32LittleEndian(region[4..]),
            RelTableType = region[8],
            RlIndexNum = BinaryPrimitives.ReadInt32LittleEndian(region[9..]),
            RelTablePage = BinaryPrimitives.ReadInt32LittleEndian(region[13..]),
            CascadeUpdates = region[17] == 1,
            CascadeDeletes = region[18] == 1,
            IndexType = (IndexType)region[19],
        };
        cursor += 20;
        return index;
    }

    internal string? ProcessIndexName(ref int cursor, byte[] buffer)
    {
        byte indexNameLen = buffer[cursor];
        ReadOnlySpan<byte> indexNameEntry = buffer.AsSpan(cursor, indexNameLen + 1);
        cursor += 1 + indexNameLen;
        return Encoding.GetString(indexNameEntry[1..]);
    }

    internal static void ProcessRealIndex(ref int cursor, in MdbBuilder.RealIndex realIndex, byte[] buffer)
    {
        ReadOnlySpan<byte> region = buffer.AsSpan(cursor, 39);
        for (int i = 0; i < 10; i++)
        {
            realIndex.Columns[i] = new MdbBuilder.RealIndexColumn
            {
                ColNum = BinaryPrimitives.ReadUInt16LittleEndian(region[(i * 3)..]),
                ColOrder = region[i * 3 + 2]
            };
        }
        realIndex.UsedPages = BinaryPrimitives.ReadInt32LittleEndian(region[30..]);
        realIndex.FirstDataPointer = BinaryPrimitives.ReadInt32LittleEndian(region[34..]);
        realIndex.Flags = region[38];

        cursor += 39;
    }


    private async Task<int> ReadNextTablePage(int nextPage, Memory<byte> buffer, CancellationToken ct)
    {
        byte[] header = new byte[16];
        await ReadPageToBufferAsync(nextPage, 0, header, ct).ConfigureAwait(false);
        //Header is the first 8 bytes. The first byte is the PageType == 0x02
        ThrowIfWrongPageType(PageType.TableDefinition, header);

        // Skip the second byte
        // Third and fourth bytes is the word "VC"
        if (!header.AsSpan(2, 2).ByteArrayCompare(Constants.TableDef.TDefId))
            throw new FormatException("Invalid JET3 table (missing 'VC' in header)");

        // Bytes 4-7 are the pointer to the next page if the table definiton spans
        // multiple pages.
        int newNextPage = BinaryPrimitives.ReadInt32LittleEndian(header.AsSpan(4));
        int newLength = BinaryPrimitives.ReadInt32LittleEndian(header.AsSpan(8));
        int autoNumber = BinaryPrimitives.ReadInt32LittleEndian(header.AsSpan(12));

        await ReadPageToBufferAsync(nextPage, 8, buffer[..Math.Min(buffer.Length, Constants.PageSize - 8)], ct).ConfigureAwait(false);

        return newNextPage;
    }

    private void ThrowIfWrongPageType(PageType header)
    {
        ThrowIfWrongPageType(header, PageBuffer);
    }

    private static void ThrowIfWrongPageType(PageType header, byte[] buffer)
    {
        if (buffer[0] != (byte)header)
            throw new FormatException($"Incorrect page type (Expected {(byte)header}, observed {buffer[0]}");
    }

    public ValueTask DisposeAsync() => MdbStream.DisposeAsync();
    public void Dispose() => MdbStream.Dispose();

    internal async Task<int> FindNextMap(byte[] map, int startPage, CancellationToken ct)
    {
        if (map[0] == 0) // Type 0 usage map
        {
            int pageStart = BinaryPrimitives.ReadInt32LittleEndian(map.AsSpan(1));
            BitArray usageBitmap = new(map);
            for (int j = startPage > pageStart ? startPage - pageStart + 1 : 0; j < usageBitmap.Length; j++)
                if (usageBitmap[j])
                    return pageStart + j;
            return 0;
        }
        else if (map[0] == 1)
        {

            int maxPages = (map.Length - 1) / 4;
            int offset = (startPage + 1) % Constants.UsageMapLen;
            for (int i = (startPage + 1) / Constants.UsageMapLen; i < maxPages; i++)
            {
                int mapPage = BinaryPrimitives.ReadInt32LittleEndian(map.AsSpan(i * 4 + 1));
                if (mapPage == 0)
                    continue;
                await ReadBitPage(mapPage, ct).ConfigureAwait(false);

                BitArray usageBitmap = new(PageBuffer[4..]);
                for (int j = startPage + 1; j < usageBitmap.Length; j++)
                    if (usageBitmap[j])
                        return j;
            }
            return 0;
        }
        throw new FormatException();
    }

    private async Task ReadBitPage(int mapPage, CancellationToken ct)
    {
        await ReadPageAsync(mapPage, ct).ConfigureAwait(false);
        ThrowIfWrongPageType(PageType.PageUseageBitmap);

    }

    internal async IAsyncEnumerable<MdbField[]> ReadDataPageAsync(int page, MdbTable table, [EnumeratorCancellation] CancellationToken ct)
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

        await ReadPageAsync(page, ct).ConfigureAwait(false);
        //Header is the first 8 bytes. The first byte is the PageType == 0x02
        ThrowIfWrongPageType(PageType.Data);

        ushort freeSpace = BinaryPrimitives.ReadUInt16LittleEndian(PageBuffer.AsSpan(2));
        int tableDefPage = BinaryPrimitives.ReadInt32LittleEndian(PageBuffer.AsSpan(4));
        ushort numRows = BinaryPrimitives.ReadUInt16LittleEndian(PageBuffer.AsSpan(8));

        var rowOffsets = GetRowOffsets(numRows);

        foreach (var rowOffset in rowOffsets)
        {
            if (rowOffset.IsDeleted)
                continue;
            else if (rowOffset.IsLookup)
                yield return CrackRow(table, rowOffset);
            else
                yield return CrackRow(table, rowOffset);
        }
    }

    private MdbField[] CrackRow(MdbTable table, RowOffset row)
    {
        MdbField[] fields = new MdbField[table.Columns.Length];
        ReadOnlySpan<byte> region = PageBuffer.AsSpan(row.StartOffset..row.EndOffset);

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
            if (column.Flags.HasFlag(ColumnFlags.FixedLength) && fixedColsFound < numFixedCols)
            {
                var field = new MdbField(column, !nullBitmask[i], region.Slice(column.Offset + 1, column.Length).ToImmutableArray());
                fields[i] = field;
                fixedColsFound++;
            }
            else if (!column.Flags.HasFlag(ColumnFlags.FixedLength) && column.OffsetVariable < numVarCols)
            {
                var startOffset = varColOffsets[column.OffsetVariable];
                var endOffset = varColOffsets[column.OffsetVariable + 1];
                var field = new MdbField(column, !nullBitmask[i], region[startOffset..endOffset].ToImmutableArray());
                fields[i] = field;
            }
            else
            {
                fields[i] = new MdbField(column, true, ImmutableArray<byte>.Empty);
            }
        }

        return fields;
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

        varColOffsets[^1] = colPtr;

        var jumpsUsed = 0;

        for (int i = 0; i < numVarCols; i++)
        {
            while (jumpsUsed < numJumps && i == rowRegion[Index.FromEnd(nullBitmaskSize + jumpsUsed + i)])
            {
                jumpsUsed++;
            }
            varColOffsets[i] = rowRegion[colPtr - i] + (jumpsUsed * 256);
        }
        return varColOffsets;
    }

    private RowOffset[] GetRowOffsets(ushort numRows)
    {
        // The high byte is not used to get a row offset (the max row offset in Jet4 is 4096 = 2^12)
        // Offsets that have 0x40 in the high order byte point to a location within the page where a
        // Data Pointer (4 bytes) to another data page (also known as an overflow page) is stored.
        // Offsets that have 0x80 in the high order byte are deleted rows.
        ReadOnlySpan<byte> region = PageBuffer.AsSpan(10, numRows * 2);
        RowOffset[] rowOffsets = new RowOffset[numRows];
        for (int i = 0; i < numRows; i++)
        {
            ushort offset = BinaryPrimitives.ReadUInt16LittleEndian(region[(i * 2)..]); ;
            rowOffsets[i].StartOffset = unchecked((ushort)(
                                       offset & 0b0001_1111_1111_1111));
            rowOffsets[i].IsLookup = (offset & 0b1000_0000_0000_0000) > 0;
            rowOffsets[i].IsDeleted = (offset & 0b0100_0000_0000_0000) > 0;

            if (i == 0) // First row ends at the end of the page
                rowOffsets[i].EndOffset = Constants.PageSize;
            else // All other rows end at the start of the previous row
                rowOffsets[i].EndOffset = rowOffsets[i - 1].StartOffset;
        }

        return rowOffsets;
    }

    [DebuggerDisplay($"MdbOffset Length {{{nameof(Length)}}}")]
    internal record struct RowOffset(ushort StartOffset, ushort EndOffset, bool IsLookup, bool IsDeleted)
    {
        public int Length => EndOffset - StartOffset;
    }

    private static class Constants
    {
        public const int PageSize = 2048;
        public const int HeaderSize = 126;
        public const int UsageMapLen = (PageSize - 4) * 8;
        public static class TableDef
        {
            public const byte Header = 0x02;
            public static ReadOnlySpan<byte> TDefId => "VC"u8;
        }
    }
}
