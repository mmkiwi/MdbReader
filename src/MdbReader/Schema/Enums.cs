// This file is part of MdbReader. Licensed under the LGPL version 2.0.
// You should have received a coy of the GNU LGPL version along with this
// program. If not, see https://www.gnu.org/licenses/old-licenses/lgpl-2.0.html
//
// Copyright Micah Makaiwi.
// Based on code from libmdb (https://github.com/mdbtools/mdbtools)

using NetEscapades.EnumGenerators;

namespace MMKiwi.MdbReader.Schema;

/// <summary>
/// Whether a table is a user-visible table or a system table
/// </summary>
public enum MdbTableType : byte
{
    /// <summary>
    /// User-visible table
    /// </summary>
    UserTable = 0x4e,
    /// <summary>
    /// System table
    /// </summary>
    SystemTable = 0x53
}


/// <summary>
/// The first byte of all Access pages is the type of the page
/// </summary>
/// <remarks>
/// See <a href="https://github.com/mdbtools/mdbtools/blob/dev/HACKING.md#data-pages">https://github.com/mdbtools/mdbtools/blob/dev/HACKING.md#data-pages</a>
/// for more information on the page formats.
/// </remarks>
internal enum MdbPageType : byte
{
    DatabaseDefinition = 0x00,
    Data = 0x01,
    TableDefinition = 0x02,
    IntermediateIndex = 0x03,
    LeafIndex = 0x04,
    PageUseageBitmap = 0x05
}

/// <summary>
/// Flags that can be set on a given column
/// </summary>
[Flags]
public enum MdbColumnFlags : byte
{
    /// <summary>
    /// The column is a fixed length. (Set for all simple numeric values)
    /// </summary>
    FixedLength = 0x01,

    /// <summary>
    /// The column can contain null values.
    /// </summary>
    CanBeNull = 0x02,

    /// <summary>
    /// The values for this column are auto-generated (Auto Number in the Access GUI)
    /// </summary>
    IsAutoLong = 0x04,

    /// <summary>
    /// This column is related to replication.
    /// </summary>
    ReplicationRelated = 0x10,

    /// <summary>
    /// This column is an auto-generated GUID.
    /// </summary>
    IsAutoGuid = 0x40,

    /// <summary>
    /// This column is a hyperlink
    /// </summary>
    IsHyperlink = 0x80
}

/// <summary>
/// The type of an index
/// </summary>
[EnumExtensions]
public enum MdbIndexType : byte
{
    /// <summary>
    /// A primary key index
    /// </summary>
    Primary = 0x01,

    /// <summary>
    /// A foreign key index
    /// </summary>
    Foreign = 0x02
}

/// <summary>
/// The type of long value
/// </summary>
/// <remarks>
/// See <a href="https://github.com/mdbtools/mdbtools/blob/dev/HACKING.md#lval-long-value-pages">https://github.com/mdbtools/mdbtools/blob/dev/HACKING.md#lval-long-value-pages</a>
/// For more information
/// </remarks>
[Flags]
internal enum MdbLvalType : byte
{
    /// <summary>The value is inline in the table (generally short values)</summary>
    Inline = 0x80,
    /// <summary>The value is in an LvalPage with a type 1 (i.e. the value is contained on a single page)</summary>
    LvalPageType1 = 0x40,
    /// <summary>The value is in an LvalPage with a type 2 (i.e. the value is likely contained on multiple pages)</summary>
    LvalPageType2 = 0x00
}

/// <summary>
/// The version of the Jet database
/// </summary>
public enum JetVersion : byte
{
    /// <summary>
    /// Jet version 3 or 3.5 (Access 95 and 97)
    /// </summary>
    Jet3 = 0x00,
    /// <summary>
    /// Jet version 4 (Access 2000)
    /// </summary>
    Jet4 = 0x01,
    /// <summary>
    /// Access 2007 (Access Connectivity Engine 12)
    /// </summary>
    Access2007 = 0x02,

    /// <summary>
    /// Access 2010 (Access Connectivity Engine 14)
    /// </summary>
    Access2010 = 0x03,

    /// <summary>
    /// Access 2013 (Access Connectivity Engine 15)
    /// </summary>
    Access2013 = 0x05,

    /// <summary>
    /// Access 2016 (Access Connectivity Engine 16)
    /// </summary>
    Access2016 = 0x06
}