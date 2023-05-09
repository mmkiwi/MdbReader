// This file is part of MdbReader. Licensed under the LGPL version 2.0.
// You should have received a coy of the GNU LGPL version along with this
// program. If not, see https://www.gnu.org/licenses/old-licenses/lgpl-2.0.html
//
// Copyright Micah Makaiwi.
// Based on code from libmdb (https://github.com/mdbtools/mdbtools)

namespace MMKiwi.MdbReader.Schema;

internal record class MdbComplexColumnInfo : MdbMiscColumnInfo
{
    public MdbComplexColumnInfo(uint tdefPageNo)
    {
        TdefPageNo = tdefPageNo;
    }

    public uint TdefPageNo { get; }

    private protected override IEnumerable<MdbColumnType> ColumnTypes => new MdbColumnType[] { MdbColumnType.Complex };
}