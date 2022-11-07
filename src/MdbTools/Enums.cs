// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.
//
// Based on code from libmdb (https://github.com/mdbtools/mdbtools)

namespace MMKiwi.MdbTools;

public enum TableType : byte
{
    UserTable = 0x4e,
    SystemTable = 0x53
}

public enum ColumnType : byte
{
    Boolean = 0x01,
    Byte = 0x02,
    Int = 0x03,
    LongInt = 0x04,
    Money = 0x05,
    Float = 0x06,
    Double = 0x07,
    DateTime = 0x08,
    Binary = 0x09,
    Text = 0x0A,
    OLE = 0x0B,
    Memo = 0x0C,
    //UNKNOWN_0D      = 0x0D,
    //UNKNOWN_0E      = 0x0E,
    Guid = 0x0F,
    Numeric = 0x10
}
public enum PageType : byte
{
    DatabaseDefinition = 0x00,
    Data = 0x01,
    TableDefinition = 0x02,
    IntermediateIndex = 0x03,
    LeafIndex = 0x04,
    PageUseageBitmap = 0x05
}

[Flags]
public enum ColumnFlags : byte
{
    FixedLength = 0x01,
    CanBeNull = 0x02,
    IsAutoLong = 0x04,
    ReplicationRelated = 0x10,
    IsAutoGuid = 0x40,
    IsHyperlink = 0x80
}

public enum IndexType : byte
{
    Primary = 0x01,
    Foreign = 0x02
}

[Flags]
internal enum LVALType : byte
{
    Inline = 0x80,
    LvalPageType1 = 0x40,
    LvalPageType2 = 0x00
}