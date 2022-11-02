// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.
//
// Based on code from libmdb (https://github.com/mdbtools/mdbtools)


using MMKiwi.Collection;
using MMKiwi.MdbTools.Mutable;
using System.Buffers;
using System.Buffers.Binary;
using System.Collections;
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

    private async Task ReadPageAsync(int pageNo, CancellationToken ct)
    {
        MdbStream.Seek(pageNo * Constants.PageSize, SeekOrigin.Begin);
        await MdbStream.ReadExactlyAsync(new Memory<byte>(PageBuffer), ct).ConfigureAwait(false);
    }

    private async Task<byte[]> ReadPageToBufferAsync(int pageNo, CancellationToken ct)
    {
        byte[] res = new byte[Constants.PageSize];
        MdbStream.Seek(pageNo * Constants.PageSize, SeekOrigin.Begin);
        await MdbStream.ReadExactlyAsync(res, 0, Constants.PageSize, ct);
        return res;
    }
    private void ReadPage(int pageNo)
    {
        MdbStream.Seek(pageNo * Constants.PageSize, SeekOrigin.Begin);
        MdbStream.ReadExactly(PageBuffer);
    }

    internal async Task<byte[]> ReadUsageMap(int mapPtr, CancellationToken ct)
    {
        int pageNo = mapPtr >> 8;
        uint row = unchecked((uint)mapPtr & 0xff);

        await ReadPageAsync(pageNo, ct);

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

    internal async Task<MdbBuilder.Table> ReadTableDef(int startPage, CancellationToken ct)
    {
        await ReadPageAsync(startPage, ct).ConfigureAwait(false);

        //Header is the first 8 bytes. The first byte is the PageType == 0x02
        ThrowIfWrongPageType(PageType.TableDefinition);

        // Skip the second byte
        // Third and fourth bytes is the word "VC"
        if (!PageBuffer.AsSpan(2, 2).ByteArrayCompare(Constants.TableDef.TDefId))
            throw new FormatException("Invalid JET3 table (missing 'VC' in header)");

        // Bytes 4-7 are the pointer to the next page if the table definiton spans
        // multiple pages.
        int nextPage = BinaryPrimitives.ReadInt32LittleEndian(PageBuffer.AsSpan(4));

        // Get the length of the data for this page
        int dataLength = BinaryPrimitives.ReadInt32LittleEndian(PageBuffer.AsSpan(8));

        // This will always be on the first page of the table def
        MdbBuilder.Table table = new(
            firstPage: startPage,
            numRows: BinaryPrimitives.ReadInt32LittleEndian(PageBuffer.AsSpan(12)),
            nextAutoNum: BinaryPrimitives.ReadInt32LittleEndian(PageBuffer.AsSpan(16)),
            tableType: (TableType)PageBuffer[20],
            maxCols: BinaryPrimitives.ReadUInt16LittleEndian(PageBuffer.AsSpan(21)),
            numVarCols: BinaryPrimitives.ReadUInt16LittleEndian(PageBuffer.AsSpan(23)),
            numCols: BinaryPrimitives.ReadUInt16LittleEndian(PageBuffer.AsSpan(25)),
            numIndexes: BinaryPrimitives.ReadInt32LittleEndian(PageBuffer.AsSpan(27)),
            numRealIndexes: BinaryPrimitives.ReadInt32LittleEndian(PageBuffer.AsSpan(31)),
            usedPagesPtr: BinaryPrimitives.ReadInt32LittleEndian(PageBuffer.AsSpan(35)),
            freePagesPtr: BinaryPrimitives.ReadInt32LittleEndian(PageBuffer.AsSpan(39))
        );

        int cursor = 35 + 8;
        for (int i = 0; i < table.NumRealIndexes; i++)
        {
            if (cursor + 8 > dataLength)
            {
                ReadNextTablePage(ref nextPage, ref dataLength);
            }
            // first four bytes are zero
            // second four bytes are the number of index rows (currently unused)
            table.RealIndices[i] = new MdbBuilder.RealIndex
            {
                NumIndexRows = BinaryPrimitives.ReadInt32LittleEndian(PageBuffer.AsSpan(cursor + 4))
            };
            cursor += 8;
        }


        for (int i = 0; i < table.NumCols; i++)
        {
            if (cursor + 18 > dataLength)
            {
                ReadNextTablePage(ref nextPage, ref dataLength);
            }
            table.Columns[i] = ProcessColumn(ref cursor);
        }
        for (int i = 0; i < table.NumCols; i++)
        {
            table.Columns[i].Name = ProcessColumnName(ref cursor);
        }

        for (int i = 0; i < table.NumRealIndexes; i++)
        {
            if (cursor + 39 > dataLength)
            {
                ReadNextTablePage(ref nextPage, ref dataLength);
            }
            ProcessRealIndex(ref cursor, table.RealIndices[i]);
        }

        for (int i = 0; i < table.NumIndexes; i++)
        {
            if (cursor + 20 > dataLength)
            {
                ReadNextTablePage(ref nextPage, ref dataLength);
            }
            table.Indices[i] = ProcessIndex(ref cursor);
        }

        for (int i = 0; i < table.NumIndexes; i++)
        {
            table.Indices[i].Name = ProcessIndexName(ref cursor);
        }

        while (true)
        {
#warning TODO multi page
            if (!ProcessVarCol(ref cursor, table.Columns))
                break;
        }

        return table;
    }

    private bool ProcessVarCol(ref int cursor, MdbBuilder.Column[] columns)
    {
        ReadOnlySpan<byte> region = PageBuffer.AsSpan(cursor, 10);

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

    internal MdbBuilder.Column ProcessColumn(ref int cursor)
    {
        ReadOnlySpan<byte> columnEntry = PageBuffer.AsSpan(cursor, 18);
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

    internal string ProcessColumnName(ref int cursor)
    {

        byte colNameLen = PageBuffer[cursor];
        ReadOnlySpan<byte> columnNameEntry = PageBuffer.AsSpan(cursor, colNameLen + 1);
        cursor += 1 + colNameLen;
        return Encoding.GetString(columnNameEntry[1..]);
    }

    internal MdbBuilder.Index ProcessIndex(ref int cursor)
    {
        ReadOnlySpan<byte> region = PageBuffer.AsSpan(cursor, 20);
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

    internal string? ProcessIndexName(ref int cursor)
    {
        byte indexNameLen = PageBuffer[cursor];
        ReadOnlySpan<byte> indexNameEntry = PageBuffer.AsSpan(cursor, indexNameLen + 1);
        cursor += 1 + indexNameLen;
        return Encoding.GetString(indexNameEntry[1..]);
    }

    internal void ProcessRealIndex(ref int cursor, in MdbBuilder.RealIndex realIndex)
    {
        ReadOnlySpan<byte> region = PageBuffer.AsSpan(cursor, 39);
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


    private void ReadNextTablePage(ref int nextPage, ref int dataLength)
    {
        ReadPage(nextPage);
        //Header is the first 8 bytes. The first byte is the PageType == 0x02
        ThrowIfWrongPageType(PageType.TableDefinition);

        // Skip the second byte
        // Third and fourth bytes is the word "VC"
        if (!PageBuffer.AsSpan(2, 2).ByteArrayCompare(Constants.TableDef.TDefId))
            throw new FormatException("Invalid JET3 table (missing 'VC' in header)");

        // Bytes 4-7 are the pointer to the next page if the table definiton spans
        // multiple pages.
        nextPage = BinaryPrimitives.ReadInt32LittleEndian(PageBuffer.AsSpan(4));

        // Get the length of the data for this page
        dataLength = BinaryPrimitives.ReadInt32LittleEndian(PageBuffer.AsSpan(8));
    }

    private void ThrowIfWrongPageType(PageType header)
    {
        if (PageBuffer[0] != (byte)header)
            throw new FormatException($"Incorrect page type (Expected {(byte)header}, observed {PageBuffer[0]}");
    }

    public ValueTask DisposeAsync() => MdbStream.DisposeAsync();
    public void Dispose() => MdbStream.Dispose();

    internal async Task<int> FindNextMap(byte[] map, int startPage, CancellationToken ct)
    {
        if (map[0] == 0) // Type 0 usage map
        {
            int pageNum = BinaryPrimitives.ReadInt32LittleEndian(PageBuffer.AsSpan(1));
            //ReadOnlySpan<byte> usageBitMap = PageBuffer.AsSpan(5);
#warning TODO
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
                await ReadBitPage(mapPage, ct);

                BitmapArray usageBitmap = new(PageBuffer.AsSpan(4));
                for (int j = startPage; j < usageBitmap.Length; j++)
                    if (usageBitmap[j])
                        return j;
            }
        }
        throw new FormatException();
    }

    private async Task ReadBitPage(int mapPage, CancellationToken ct)
    {
        await ReadPageAsync(mapPage, ct);
        ThrowIfWrongPageType(PageType.PageUseageBitmap);

    }

    internal async Task ReadDataPageAsync(int page, CancellationToken ct)
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

        await ReadPageAsync(page, ct);
        //Header is the first 8 bytes. The first byte is the PageType == 0x02
        ThrowIfWrongPageType(PageType.Data);

        ushort freeSpace = BinaryPrimitives.ReadUInt16LittleEndian(PageBuffer.AsSpan(2));
        int tableDefPage = BinaryPrimitives.ReadInt32LittleEndian(PageBuffer.AsSpan(4));
        ushort numRows = BinaryPrimitives.ReadUInt16LittleEndian(PageBuffer.AsSpan(8));

        var rowOffsets = GetRowOffsets(numRows);


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
            ushort offset = BinaryPrimitives.ReadUInt16LittleEndian(PageBuffer.AsSpan(i * 2));;
            rowOffsets[i].Offset = unchecked((ushort)(
                                       offset & 0b0000_1111_1111_1111));
            rowOffsets[i].IsLookup =  (offset & 0b1000_0000_0000_0000) > 0;
            rowOffsets[i].IsDeleted = (offset & 0x0100_0000_0000_0000) > 0;
        }

        return rowOffsets;
    }

    internal record struct RowOffset(ushort Offset, bool IsLookup, bool IsDeleted) {}

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
