// This file is part of MdbReader. Licensed under the LGPL version 2.0.
// You should have received a coy of the GNU LGPL version along with this
// program. If not, see https://www.gnu.org/licenses/old-licenses/lgpl-2.0.html
//
// Copyright Micah Makaiwi.
// Based on code from libmdb (https://github.com/mdbtools/mdbtools)

using System.Diagnostics;
using MMKiwi.MdbReader.Schema;

namespace MMKiwi.MdbReader.Values;

/// <summary>
/// A database value corresponding to an Access <see cref="MdbColumnType.DateTime"/>. 
/// </summary>
/// <remarks>
/// <para>
/// This is stored in the database as an 64-bit floating-point number repesenting the decimal days
/// since January 1, 1900. <see cref="Value" /> returns a <see cref="DateTime" />
/// Referred to in the Access GUI as a Date/Time column, and a <c>DATETIME</c> in SQL.
/// </para>
/// <para> 
/// This class is used for non-nullable columns only. For nullable columns, use <see cref="Nullable" />
/// </para>
/// </remarks>
[DebuggerDisplay("{Column.Name}: {Value}")]
internal sealed class MdbDateTimeValue : MdbValue<DateTime>, IValueAllowableType
{
    internal MdbDateTimeValue(MdbColumn column, bool isNull, ImmutableArray<byte> binaryValue)
        : base(column, isNull, binaryValue, 8, 8, AllowableType) { }

    /// <summary>
    /// The <see cref="MdbColumnType" /> that can be used for this value.
    /// This will always be <see cref="MdbColumnType.DateTime" />
    /// </summary>
    public static MdbColumnType AllowableType => MdbColumnType.DateTime;

    /// <summary>
    /// The value for the specific row and column. A <see cref="DateTime" />.
    /// </summary>
    public override DateTime Value => ConversionFunctions.AsDateTime(BinaryValue.AsSpan());

}
