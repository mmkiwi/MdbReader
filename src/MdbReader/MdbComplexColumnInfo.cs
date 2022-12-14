// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.
//
// Based on code from libmdb (https://github.com/mdbtools/mdbtools)

namespace MMKiwi.MdbReader;

internal record class MdbComplexColumnInfo : MdbMiscColumnInfo
{
    public MdbComplexColumnInfo(uint tdefPageNo)
    {
        TdefPageNo = tdefPageNo;
    }

    public uint TdefPageNo { get; }

    private protected override IEnumerable<MdbColumnType> ColumnTypes => new MdbColumnType[] { MdbColumnType.Complex };
}