// This file is part of MdbReader. Licensed under the LGPL version 2.0.
// You should have received a coy of the GNU LGPL version along with this
// program. If not, see https://www.gnu.org/licenses/old-licenses/lgpl-2.0.html
//
// Copyright Micah Makaiwi.
// Based on code from libmdb (https://github.com/mdbtools/mdbtools)

using MMKiwi.MdbReader.Schema;

namespace MMKiwi.MdbReader.Values;

/// <summary>
/// A database value corresponding to an Access <see cref="MdbColumnType.Text"/>. 
/// </summary>
/// <remarks>
/// <para>
/// This is a string encoded with the main database encoding. 
/// <see cref="Value" /> returns a <see cref="string" /> 
/// Referred to in the Access GUI as a Text column, and as an <c>CHAR</c> or <c>VARCHAR</c> in SQL.
/// </para>
/// <para>
/// For <c>CHAR</c> columns (which can only be defined using SQL), there may be null bytes
/// at the end of the string. <see cref="Value" /> will truncate the string at the first null
/// byte. To return the buffer with all the null bytes, or to return the original 8-bit byte value
/// (for instance to write to disc), use <see cref="RawValue" /> instead.
/// </para>
/// <para> 
/// This class is used for non-nullable columns only. For nullable columns, use <see cref="Nullable" />
/// </para>
/// </remarks>
[DebuggerDisplay("{Column.Name}: {Value}")]
internal sealed class MdbStringValue : MdbValue<string>, IValueAllowableType
{
    //Not saving the string to the BinaryValue buffer in order to not duplicate it
    internal MdbStringValue(MdbColumn column, bool isNull, ImmutableArray<byte> binaryValue)
        : base(column, isNull, ImmutableArray<byte>.Empty, 0, column.Length, AllowableType)
    {

        Encoding = (column.ColumnInfo as MdbTextColumnInfo)!.Encoding;

        Value = ConversionFunctions.AsString(Encoding, binaryValue.AsSpan());
    }

    /// <summary>
    /// The text encoding
    /// </summary>
    public Encoding Encoding { get; }

    /// <summary>
    /// The <see cref="MdbColumnType" /> that can be used for this value.
    /// This will always be <see cref="MdbColumnType.Text" />
    /// </summary>
    public static MdbColumnType AllowableType => MdbColumnType.Text;

    /// <summary>
    /// The value for the specific row and column. A <see cref="string" />.
    /// </summary>
    /// <remarks>
    /// <para>
    /// For <c>CHAR</c> columns (which can only be defined using SQL), there may be null bytes
    /// at the end of the string. <see cref="Value" /> will truncate the string at the first null
    /// byte. To return the buffer with all the null bytes, or to return the original 8-bit byte value
    /// (for instance to write to disc), use <see cref="RawValue" /> instead.
    /// </para>
    /// </remarks>
    public override string Value { get; }

    /// <summary>
    /// The raw text bytes for the specific row and column.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The encoding of the text can be retrieved from the <see cref="MdbTextColumnInfo.Encoding" /> property
    /// on <see cref="MdbValue{TVal}.Column" />
    /// </para>
    /// <para>
    /// For <c>CHAR</c> columns (which can only be defined using SQL), there may be null bytes
    /// at the end of the string. <see cref="Value" /> will truncate the string at the first null
    /// byte. To return the buffer with all the null bytes, or to return the original 8-bit byte value
    /// (for instance to write to disc), use <see cref="RawValue" /> instead.
    /// </para>
    /// </remarks>
    public ImmutableArray<byte> RawValue => BinaryValue;
}
