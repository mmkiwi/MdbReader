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
/// A database value corresponding to an Access <see cref="MdbColumnType.Double"/>. 
/// </summary>
/// <remarks>
/// <para>
/// This is a 64-bit floatting-point number and <see cref="Value" /> returns a <see cref="double" />.
/// Referred to in the Access GUI as a Number column with the Double size, and as a <c>DOUBLE</c> in SQL.
/// </para>
/// <para> 
/// This class is used for non-nullable columns only. For nullable columns, use <see cref="Nullable" />
/// </para>
/// </remarks>
[DebuggerDisplay("{Column.Name}: {Value}")]
internal sealed class MdbDoubleValue : MdbValue<double>, IValueAllowableType
{
    internal MdbDoubleValue(MdbColumn column, bool isNull, ImmutableArray<byte> binaryValue)
        : base(column, isNull, binaryValue, 8, 8, AllowableType) { }

    /// <summary>
    /// The <see cref="MdbColumnType" /> that can be used for this value.
    /// This will always be <see cref="MdbColumnType.Double" />
    /// </summary>
    public static MdbColumnType AllowableType => MdbColumnType.Double;

    /// <summary>
    /// The value for the specific row and column. A <see cref="double" />.
    /// </summary>
    public override double Value => ConversionFunctions.AsDouble(BinaryValue.AsSpan());

}
