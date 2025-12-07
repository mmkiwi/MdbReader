// This file is part of MdbReader. Licensed under the LGPL version 2.0.
// You should have received a coy of the GNU LGPL version along with this
// program. If not, see https://www.gnu.org/licenses/old-licenses/lgpl-2.0.html
//
// Copyright Micah Makaiwi.
// Based on code from libmdb (https://github.com/mdbtools/mdbtools)

using MMKiwi.MdbReader.Schema;

namespace MMKiwi.MdbReader.Values;

/// <summary>
/// A database value corresponding to an Access <see cref="MdbColumnType.Single"/>. 
/// </summary>
/// <remarks>
/// <para>
/// This is a 32-bit floating-point decimal and <see cref="Value" /> returns a <see cref="float" />.
/// Referred to in the Access GUI as a 
/// </para>
/// <para> 
/// This class is used for non-nullable columns only. For nullable columns, use <see cref="Nullable" />
/// </para>
/// </remarks>
[DebuggerDisplay("{Column.Name}: {Value}")]
internal sealed class MdbSingleValue : MdbValue<float>, IValueAllowableType
{
    internal MdbSingleValue(MdbColumn column, bool isNull, ReadOnlySpan<byte> binaryValue)
        : base(column, isNull, binaryValue, 4, 4, AllowableType)
    {
        if (!isNull)
        {
            Value = ConversionFunctions.AsSingle(binaryValue);
        }
    }

    /// <summary>
    /// The <see cref="MdbColumnType" /> that can be used for this value.
    /// This will always be <see cref="MdbColumnType.Single" />
    /// </summary>
    public static MdbColumnType AllowableType => MdbColumnType.Single;

    /// <summary>
    /// The value for the specific row and column. A <see cref="float" />.
    /// </summary>
    public override float Value { get; }
}
