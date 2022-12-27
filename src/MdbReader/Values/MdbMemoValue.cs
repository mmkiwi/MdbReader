// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.
//
// Based on code from libmdb (https://github.com/mdbtools/mdbtools)

using System.Collections.Immutable;
using System.Diagnostics;
using System.Text;

using MMKiwi.MdbReader.Helpers;

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
public class MdbMemoValue : MdbLongValField<StreamReader?>, IValueAllowableType
{

    internal MdbMemoValue(Jet3Reader reader, MdbColumn column, bool isNull, ImmutableArray<byte> binaryValue)
        : base(reader, column, isNull, binaryValue, AllowableType)
    {
        if (BinaryValue.Length > 2 && BinaryValue[0] == 0xff && BinaryValue[1] == 0xfe)
        {
            Encoding = Jet4CompressedString.GetForEncoding((column.ColumnInfo as MdbTextColumnInfo)!.Encoding);
        }
        else
        {
            Encoding = (column.ColumnInfo as MdbTextColumnInfo)!.Encoding; // UCS-2 but UTF-16 should be close enough;
        }
    }
    /// <summary>
    /// A <see cref="StreamReader" /> wrapping a <see cref="MdbLValStream" /> that gets the 
    /// full text value for the field. Call <see cref="StreamReader.ReadToEnd" /> to get a 
    /// <see cref="string" />.
    /// </summary>
    /// <returns>The stream for for memo value, or null if the value is null.</returns>
    public override StreamReader? Value => IsNull ? null : new StreamReader(new MdbLValStream(new Jet3Reader.LvalStream(Reader, Column, BinaryValue)), Encoding);
    
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
