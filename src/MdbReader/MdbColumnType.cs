// This file is part of MdbReader. Licensed under the LGPL version 2.0.
// You should have received a coy of the GNU LGPL version along with this
// program. If not, see https://www.gnu.org/licenses/old-licenses/lgpl-2.0.html
//
// Copyright Micah Makaiwi.
// Based on code from libmdb (https://github.com/mdbtools/mdbtools)

using NetEscapades.EnumGenerators;

namespace MMKiwi.MdbReader;

/// <summary>
/// The type of a column in an Access database
/// </summary>
[EnumExtensions]
public enum MdbColumnType : byte
{
    /// <summary>
    /// A one-bit yes or no column
    /// </summary>
    /// <remarks>
    /// A "Yes/No" column in the Access GUI and <c>BIT</c> in SQL.
    /// </remarks>
    Boolean = 0x01,

    /// <summary>
    /// An 8-bit integer column (0 to 255)
    /// </summary>
    /// <remarks>
    /// A Number column with the Byte size in the Access GUI and <c>BYTE</c> in SQL.
    /// </remarks>
    Byte = 0x02,

    /// <summary>
    /// A 16-bit signed integer column (-32,768, to 32,767)
    /// </summary>
    /// <remarks>
    /// A Number column with the Int size in the Access GUI and <c>INT</c> in SQL.
    /// </remarks>
    Int = 0x03,

    /// <summary>
    /// A 32-bit signed integer column (-2,147,483,648 to 2,147,483,647)
    /// </summary>
    /// <remarks>
    /// A Number column with the LongInt size in the Access GUI and <c>LONG</c> in SQL.
    /// </remarks>
    LongInt = 0x04,

    /// <summary>
    /// A 64-bit signed integer column with 4 decimal digits after the decimal point.
    /// (i.e. $12,001.23 is stored in the database as 120012300, can store 
    /// numbers between -922,337,203,685,477.5808 and 922,337,203,685,477.5807)
    /// </summary>
    /// <remarks>
    /// A Currency column in the Access GUI and <c>CURRENCY</c> in SQL.
    /// </remarks>
    Currency = 0x05,

    /// <summary>
    /// A 32-bit floating point column.
    /// </summary>
    /// <remarks>
    /// A Number column with the Single size in the Access GUI and <c>SINGLE</c> in SQL.
    /// </remarks>
    Single = 0x06,

    /// <summary>
    /// A 64-bit floating point column.
    /// </summary>
    /// <remarks>
    /// A Number column with the Double size in the Access GUI and <c>DOUBLE</c> in SQL.
    /// </remarks>
    Double = 0x07,

    /// <summary>
    /// An Access Date/Time column. 
    /// </summary>
    /// <remarks>
    /// A Date/Time column in the Access GUI and <c>DATETIME</c> in SQL. A 64-bit decimal value
    /// with the decimal days since January 1, 1900
    /// </remarks>
    DateTime = 0x08,

    /// <summary>
    /// An binary column. 
    /// </summary>
    /// <remarks>
    /// This column type is not selectable in the Access GUI and <c>BINARY</c> and <c>VARBINARY</c> in SQL.
    /// </remarks>
    Binary = 0x09,

    /// <summary>
    /// An text column. 
    /// </summary>
    /// <remarks>
    /// A Short Text (or Text) column in the Access GUI and <c>CHAR</c> and <c>VARCHAR</c> in SQL.
    /// </remarks>
    Text = 0x0A,

    /// <summary>
    /// An OLE column. 
    /// </summary>
    /// <remarks>
    /// An OLE column in the Access GUI and <c>LONGBINARY</c> in SQL.
    /// </remarks>
    OLE = 0x0B,

    /// <summary>
    /// A memo column. 
    /// </summary>
    /// <remarks>
    /// An Memo (or Long Text) column in the Access GUI and <c>LONGTEXT</c> in SQL.
    /// </remarks>
    Memo = 0x0C,

    /// <summary>
    /// A GUID column.
    /// </summary>
    /// 
    /// <remarks>
    /// A Number column with the Replication ID size in the Access GUI and <c>GUID</c> in SQL.
    /// </remarks>
    Guid = 0x0F,

    /// <summary>
    /// Fixed Point, 96 bit, stored in 17 bytes 
    /// </summary>
    Numeric = 0x10,

    /// <summary>
    /// Complex field (32 bit integer key) 
    /// </summary>
    Complex = 0x12
}

/// <summary>
/// The first byte of all Access pages is the type of the page
/// </summary>
/// <remarks>
/// See <a href="https://github.com/mdbtools/mdbtools/blob/dev/HACKING.md#data-pages">https://github.com/mdbtools/mdbtools/blob/dev/HACKING.md#data-pages</a>
/// for more information on the page formats.
/// </remarks>
[EnumExtensions]
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
[EnumExtensions]
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
/// The type of long value
/// </summary>
/// <remarks>
/// See <a href="https://github.com/mdbtools/mdbtools/blob/dev/HACKING.md#lval-long-value-pages">https://github.com/mdbtools/mdbtools/blob/dev/HACKING.md#lval-long-value-pages</a>
/// For more information
/// </remarks>
[Flags]
[EnumExtensions]
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
[EnumExtensions]
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