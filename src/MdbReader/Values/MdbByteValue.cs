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
/// A database value corresponding to an Access <see cref="MdbColumnType.Byte"/>. 
/// </summary>
/// <remarks>
/// <para>
/// This is a 8-bit unsigned integer and <see cref="Value" /> returns a <see cref="byte" /> 
/// (values from 0 to 255).
/// Referred to in the Access GUI as a Number column with the Byte size, and a <c>BYTE</c> in SQL.
/// </para>
/// <para> 
/// This class is used for non-nullable columns only. For nullable columns, use <see cref="Nullable" />
/// </para>
/// </remarks>
[DebuggerDisplay("{Column.Name}: {Value}")]
internal sealed class MdbByteValue : MdbValue<byte>, IValueAllowableType
{
    internal MdbByteValue(MdbColumn column, bool isNull, ImmutableArray<byte> binaryValue)
        : base(column, isNull, binaryValue, 1, 1, AllowableType) { }

    /// <summary>
    /// The <see cref="MdbColumnType" /> that can be used for this value.
    /// This will always be <see cref="MdbColumnType.Byte" />
    /// </summary>
    public static MdbColumnType AllowableType => MdbColumnType.Byte;

    /// <summary>
    /// The value for the specific row and column. A <see cref="byte" /> (values from 0 to 255).
    /// </summary>
    public override byte Value => ConversionFunctions.AsByte(BinaryValue.AsSpan());

}
