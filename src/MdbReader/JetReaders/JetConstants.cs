// This file is part of MdbReader. Licensed under the LGPL version 2.0.
// You should have received a coy of the GNU LGPL version along with this
// program. If not, see https://www.gnu.org/licenses/old-licenses/lgpl-2.0.html
//
// Copyright Micah Makaiwi.
// Based on code from libmdb (https://github.com/mdbtools/mdbtools)

namespace MMKiwi.MdbReader;
internal class JetConstants
{
    public JetConstants(JetVersion version)
    {
        Version = version;
        PageSize = version is JetVersion.Jet3 ? Jet3.PageSize : Jet4.PageSize;
        DbPage = new DbPageCosntants(this);
        TablePage = new TablePageConstants(this);
        UsageMap = new UsageMapConstants(this);
        DataPage = new DataPageConstants(this);
    }

    public static class Jet3
    {
        public const ushort PageSize = 2048;
    }

    public static class Jet4
    {
        public const ushort PageSize = 4096;
    }

    public JetVersion Version { get; }
    public ushort PageSize { get; } // Accessed a lot, so cache this value
    public DbPageCosntants DbPage { get; }
    public TablePageConstants TablePage { get; }
    public UsageMapConstants UsageMap { get; }
    public DataPageConstants DataPage { get; }

    public static ushort OffsetMask => 0x1fff;

    public static ReadOnlySpan<byte> LvalString => "LVAL"u8;

    internal record class DataPageConstants
    {
        public DataPageConstants(JetConstants constants)
        {
            Constants = constants;
            JetVersion = constants.Version;
        }

        JetConstants Constants { get; }
        JetVersion JetVersion { get; }

        public Range RecordCount => JetVersion is JetVersion.Jet3 ? 8..10 : 12..14;
        public int HeaderSize => JetVersion is JetVersion.Jet3 ? 10 : 14;
    }

    internal record class DbPageCosntants
    {
        public DbPageCosntants(JetConstants constants)
        {
            Constants = constants;
            JetVersion = constants.Version;
        }
        JetConstants Constants { get; }
        JetVersion JetVersion { get; }

        public const uint DbMagicNumber = 0x100;
        public ReadOnlySpan<byte> DbFileFormatId => JetVersion is JetVersion.Jet3 or JetVersion.Jet4 ? "Standard Jet DB"u8 : "Standard ACE DB"u8;

        public int DbHeaderSize => JetVersion is JetVersion.Jet3 ? 126 : 128;
        public int DbCollationOffset => JetVersion is JetVersion.Jet3 ? 0x22 : 0x56;

        public int DbCodePageOffset => 0x24;
        public int DbKeyOffset => 0x26;
        public int DbPasswdOffset => 0x2a;
        public int DbPasswdSize => JetVersion is JetVersion.Jet3 ? 20 : 40;
        public int DbCreationDateOffset => 0x5A;
    }

    internal record class TablePageConstants
    {
        public TablePageConstants(JetConstants constants)
        {
            Constants = constants;
            JetVersion = constants.Version;
        }
        JetConstants Constants { get; }
        JetVersion JetVersion { get; }

        public ReadOnlySpan<byte> TdefId => "VC"u8;
        public int NextPage => 4;
        public int TDefLength => 0;

        // The following offsets are assuming we are splicing starting at 
        // the num_rows field (which is the 12th byte in JET3 or 16th byte Jet4+
        public Range NumRows => JetVersion == JetVersion.Jet3 ? 4..8 : 8..12;
        public Range NextAutoNum => JetVersion == JetVersion.Jet3 ? 8..12 : 12..16;
        // Jet4Only
        public Range AutonumIncrement => 16..20;

        public Range ComplexAutonum => 20..24;

        public int RowCountOffset => JetVersion == JetVersion.Jet3 ? 8 : 12;

        //JET4, 24-32 unknown

        public Range TableType => JetVersion == JetVersion.Jet3 ? 12..13 : 32..33;
        public Range MaxCols => JetVersion == JetVersion.Jet3 ? 13..15 : 33..35;
        public Range NumVarCols => JetVersion == JetVersion.Jet3 ? 15..17 : 35..37;
        public Range NumCols => JetVersion == JetVersion.Jet3 ? 17..19 : 37..39;
        public Range NumIndexes => JetVersion == JetVersion.Jet3 ? 19..23 : 39..43;
        public Range NumRealIndexes => JetVersion == JetVersion.Jet3 ? 23..27 : 43..47;
        public Range UsedPages => JetVersion == JetVersion.Jet3 ? 27..31 : 47..51;
        public Range FreePages => JetVersion == JetVersion.Jet3 ? 31..35 : 51..55;

        public int TableCursorStartPoint => JetVersion == JetVersion.Jet3 ? 35 : 55;

        // Real index loop 11
        public int RealIndexSlice1Length => JetVersion == JetVersion.Jet3 ? 8 : 12;
        public Range RealIndexRows => 4..8;

        // Column Loop
        public int ColumnSliceLength => JetVersion == JetVersion.Jet3 ? 18 : 25;
        public Range ColType => 0..1;
        // 4 unknown bytes in JET 4 1...5
        public Range ColNumInclDeleted => JetVersion == JetVersion.Jet3 ? 1..3 : 5..7;
        public Range OffsetVariable => JetVersion == JetVersion.Jet3 ? 3..5 : 7..9;
        public Range ColNum => JetVersion == JetVersion.Jet3 ? 5..7 : 9..11;

        /*
         * In Jet 3, textual columns:
         *   * 2 bytes collation (LCID = Localisation ID)
         *   * 2 bytes code page
         *   * 2 bytes unknown 
         * Jet 3, decimal columns:
         *   * 2 bytes unknown
         *   * 1 byte Maximum total number of digits
         *   * 1 byte Number of decimal digits
         *   * 2 bytes unknown 
         * Jet 4, textual columns:
         *   * 2 bytes collation
         *   * 1 byte unknown
         *   * 1 byte collation version number ? 
         * Jet 4, decimal columns:
         *   * 1 byte Maximum total number of digits
         *   * 1 byte Number of decimal digits
         *   * 2 bytes unknown 
         * Jet 4, complex columns:
         *   * 4 bytes, TDEF page number of the complex field 
         */
        public Range Misc => JetVersion == JetVersion.Jet3 ? 9..13 : 11..15;

        public Range ColFlags => JetVersion == JetVersion.Jet3 ? 13..14 : 15..16;

        //JET4 4 unknown 16..21
        public Range OffsetFixed => JetVersion == JetVersion.Jet3 ? 14..16 : 21..23;
        public Range ColLength => JetVersion == JetVersion.Jet3 ? 16..18 : 23..25;

        public int ColNameLengthSize => JetVersion == JetVersion.Jet3 ? 1 : 2;

        // Real Index Loop 2
        public int RealIndexSlice2Length => JetVersion == JetVersion.Jet3 ? 39 : 52;
        //There are four unknown bytes at the beginning of JET4
        public Range RealIndexColsSlice => JetVersion == JetVersion.Jet3 ? 0..30 : 4..35;
        public int RealIndexColSize => 3;
        public Range RealIndexColNum => 0..2;
        public Range RealIndexColOrder => 2..3;

        // Four unknown bytes for JET4
        public Range RealIndexUsagePointer => JetVersion == JetVersion.Jet3 ? 30..34 : 36..40;
        public Range RealIndexFirstData => JetVersion == JetVersion.Jet3 ? 34..38 : 40..44;
        public Range RealIndexFlags => JetVersion == JetVersion.Jet3 ? 38..39 : 44..46;

        // bytes 46-52 unknown in JET4;

        // Index Loop
        public int IndexSliceLength => JetVersion == JetVersion.Jet3 ? 20 : 28;
        //First 4 bytes unknown JET4
        public Range IndexNum => JetVersion == JetVersion.Jet3 ? 0..4 : 4..8;
        public Range IndexNum2 => JetVersion == JetVersion.Jet3 ? 4..8 : 8..12;
        public Range IndexRelTableType => JetVersion == JetVersion.Jet3 ? 8..9 : 12..13;
        public Range IndexRelIndexNum => JetVersion == JetVersion.Jet3 ? 9..13 : 13..17;
        public Range IndexRelTablePage => JetVersion == JetVersion.Jet3 ? 13..17 : 17..21;
        public Range IndexCascadeUpdates => JetVersion == JetVersion.Jet3 ? 17..18 : 22..23;
        public Range IndexCascadeDeletes => JetVersion == JetVersion.Jet3 ? 18..19 : 23..24;
        public Range IndexType => JetVersion == JetVersion.Jet3 ? 19..20 : 24..25;
        // 25-28 unknown JET4

        // Var col loop
        public int VarColSliceSize => 10;
        public Range VarColNum => 0..2;
        public Range VarColUsedPagePtr => 2..6;
        public Range VarColFreePagePtr => 6..10;
    }
    public class UsageMapConstants
    {
        public UsageMapConstants(JetConstants constants)
        {
            Constants = constants;
            JetVersion = constants.Version;
        }
        JetConstants Constants { get; }
        JetVersion JetVersion { get; }

        public int UsageMapLen => (Constants.PageSize - 4) * 8;
        public Range RowCount => JetVersion == JetVersion.Jet3 ? 10..12 : 14..16;
    }
}
