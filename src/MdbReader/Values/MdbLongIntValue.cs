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
/// A database value corresponding to an Access <see cref="MdbColumnType.LongInt"/>. 
/// </summary>
/// <remarks>
/// <para>
/// This is a 32-bit signed integer and <see cref="Value" /> returns a <see cref="int" /> 
/// (values from -2,147,483,648 to 2,147,483,647).
/// Referred to in the Access GUI as a Number column with the Long Integer size, and as an <c>LONG</c> in SQL.
/// </para>
/// <para> 
/// This class is used for non-nullable columns only. For nullable columns, use <see cref="Nullable" />
/// </para>
/// </remarks>
[DebuggerDisplay("{Column.Name}: {Value}")]
internal sealed class MdbLongIntValue : MdbValue<int>, IValueAllowableType
{
    internal MdbLongIntValue(MdbColumn column, bool isNull, ImmutableArray<byte> binaryValue)
        : base(column, isNull, binaryValue, 4, 4, AllowableType) { }

    /// <summary>
    /// The <see cref="MdbColumnType" /> that can be used for this value.
    /// This will always be <see cref="MdbColumnType.LongInt" />
    /// </summary>
    public static MdbColumnType AllowableType => MdbColumnType.LongInt;

    /// <summary>
    /// The value for the specific row and column. An <see cref="int" />
    /// </summary>
    public override int Value => ConversionFunctions.AsInt(BinaryValue.AsSpan());

}
