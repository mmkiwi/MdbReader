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
/// A database value corresponding to an Access <see cref="MdbColumnType.Int"/>. 
/// </summary>
/// <remarks>
/// <para>
/// This is a 16-bit signed integer and <see cref="Value" /> returns a <see cref="short" /> 
/// (values from -32768 to 32767).
/// Referred to in the Access GUI as a Number column with the Integer size, and as an <c>INT</c> in SQL.
/// </para>
/// <para> 
/// This class is used for non-nullable columns only. For nullable columns, use <see cref="Nullable" />
/// </para>
/// </remarks>
[DebuggerDisplay("{Column.Name}: {Value}")]
internal sealed class MdbIntValue : MdbValue<short>, IValueAllowableType
{
    internal MdbIntValue(MdbColumn column, bool isNull, ImmutableArray<byte> binaryValue)
        : base(column, isNull, binaryValue, 2, 2, AllowableType) { }

    /// <summary>
    /// The <see cref="MdbColumnType" /> that can be used for this value.
    /// This will always be <see cref="MdbColumnType.Int" />
    /// </summary>
    public static MdbColumnType AllowableType => MdbColumnType.Int;

    /// <summary>
    /// The value for the specific row and column. A <see cref="short" /> (values from -32768 to 32767).
    /// </summary>
    public override short Value => ConversionFunctions.AsShort(BinaryValue.AsSpan());

}
