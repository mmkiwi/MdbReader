// This file is part of MdbReader. Licensed under the LGPL version 2.0.
// You should have received a coy of the GNU LGPL version along with this
// program. If not, see https://www.gnu.org/licenses/old-licenses/lgpl-2.0.html
//
// Copyright Micah Makaiwi.
// Based on code from libmdb (https://github.com/mdbtools/mdbtools)

using MMKiwi.MdbReader.Helpers;
using MMKiwi.MdbReader.Schema;

namespace MMKiwi.MdbReader.Values;

/// <summary>
/// A database value corresponding to an Access <see cref="MdbColumnType.Memo"/>. 
/// </summary>
/// <remarks>
/// <para>
/// This is variable-length text and <see cref="Value" /> returns a <see cref="StreamReader" /> that
/// wraps a <see cref="MdbLValStream" /> to get the full text value. 
/// Referred to in the Access GUI as a Memo or Long Text column , and as an <c>LONGTEXT</c> in SQL.
/// </para>
/// <para>
/// The encoding of the text can be retrieved from the <see cref="MdbTextColumnInfo.Encoding" /> property
/// on <see cref="MdbColumn.ColumnInfo" />
/// </para>
/// </remarks>
[DebuggerDisplay("{Column.Name}: [MEMO]")]
internal class MdbMemoValue : MdbLongValField<MdbLValStream?>, IValueAllowableType
{

    internal MdbMemoValue(Jet3Reader reader, MdbColumn column, bool isNull, ReadOnlySpan<byte> binaryValue)
        : base(reader, column, isNull, binaryValue, AllowableType)
    {
        Encoding = (column.ColumnInfo as MdbTextColumnInfo)!.Encoding;
    }
    /// <summary>
    /// A <see cref="StreamReader" /> wrapping a <see cref="MdbLValStream" /> that gets the 
    /// full text value for the field. Call <see cref="StreamReader.ReadToEnd" /> to get a 
    /// <see cref="string" />.
    /// </summary>
    /// <returns>The stream for for memo value, or null if the value is null.</returns>
    public override MdbLValStream? Value => IsNull ? null : new MdbLValStream(new Jet3Reader.LvalStream(Reader, Column, BinaryValue));

    public StreamReader? StreamReader => IsNull ? null : new StreamReader(Value!, Encoding, detectEncodingFromByteOrderMarks:false);

    /// <summary>
    /// The <see cref="MdbColumnType" /> that can be used for this value.
    /// This will always be <see cref="MdbColumnType.Memo" />
    /// </summary>
    public static MdbColumnType AllowableType => MdbColumnType.Memo;

    /// <summary>
    /// The encoding of the text value
    /// </summary>
    public Encoding Encoding { get; }
}
