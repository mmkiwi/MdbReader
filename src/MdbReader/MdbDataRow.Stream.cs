// This file is part of MdbReader. Licensed under the LGPL version 2.0.
// You should have received a coy of the GNU LGPL version along with this
// program. If not, see https://www.gnu.org/licenses/old-licenses/lgpl-2.0.html
//
// Copyright Micah Makaiwi.
// Based on code from libmdb (https://github.com/mdbtools/mdbtools)

using MMKiwi.MdbReader.Helpers;
using MMKiwi.MdbReader.Schema;
using MMKiwi.MdbReader.Values;

namespace MMKiwi.MdbReader;

public sealed partial class MdbDataRow
{
    private MdbLValStream? GetStream(IMdbValue fieldValue) => fieldValue switch
    {
        MdbOleValue oleValue => oleValue.Value,
        MdbMemoValue memoValue => memoValue.Value,
        _ => ThrowInvalidCast<MdbLValStream>(fieldValue, nameof(MdbLValStream))
    };

    /// <summary>
    /// Gets the value of the specified column as a <see cref="MdbLValStream"/>
    /// </summary>
    /// <param name="index">The zero-based column ordinal.</param>
    /// <returns>The value of the column.</returns>
    /// <throws cref="InvalidCastException">The specified cast is not valid.</throws>
    /// <remarks>
    /// <para>
    /// This function only works on a <see cref="MdbColumnType.OLE"/> or <see cref="MdbColumnType.Memo"/> column.
    /// On Access 2000 (JET 4) or later databases, memo columns may use a specialized compressed encoding. Check the 
    /// <see cref="MdbTextColumnInfo.Encoding"/> property on the column to get the correct <see cref="Jet4CompressedString"/>
    /// encoding.
    /// </para>
    /// <para>This function returns null (not a stream over nothing) if the field value is null.</para>
    /// </remarks>
    public MdbLValStream? GetStream(int index)
    {
        var fieldValue = GetFieldValue(index);
        return fieldValue.IsNull ? null : GetStream(fieldValue);
    }

    /// <summary>
    /// Gets the value of the specified column as a <see cref="MdbLValStream"/>
    /// </summary>
    /// <param name="columnName">The column name (case sensitive).</param>
    /// <returns>The value of the column.</returns>
    /// <throws cref="InvalidCastException">The specified cast is not valid.</throws>
    /// <remarks>
    /// <para>
    /// This function only works on a <see cref="MdbColumnType.OLE"/> or <see cref="MdbColumnType.Memo"/> column.
    /// On Access 2000 (JET 4) or later databases, memo columns may use a specialized compressed encoding. Check the 
    /// <see cref="MdbTextColumnInfo.Encoding"/> property on the column to get the correct <see cref="Jet4CompressedString"/>
    /// encoding.
    /// </para>
    /// <para>This function returns null (not a stream over nothing) if the field value is null.</para>
    /// </remarks>
    public MdbLValStream? GetStream(string columnName)
    {
        var fieldValue = GetFieldValue(columnName);
        return fieldValue.IsNull ? null : GetStream(fieldValue);
    }

    private StreamReader? GetTextReader(IMdbValue fieldValue) => fieldValue switch
    {
        MdbMemoValue memoValue => memoValue.StreamReader,
        _ => ThrowInvalidCast<StreamReader>(fieldValue, nameof(StreamReader))
    };

    /// <summary>
    /// Gets the value of the specified column as a <see cref="MdbLValStream"/>
    /// </summary>
    /// <param name="index">The zero-based column ordinal.</param>
    /// <returns>The value of the column.</returns>
    /// <throws cref="InvalidCastException">The specified cast is not valid.</throws>
    /// <remarks>
    /// <para>
    /// This function only works on a <see cref="MdbColumnType.Memo"/> column. The StreamReader will be set
    /// to use the correct compressed encoding for JET4 (Access 2000 and later) databases.
    /// encoding.
    /// </para>
    /// <para>This function returns null (not a stream over nothing) if the field value is null.</para>
    /// </remarks>
    public StreamReader? GetStreamReader(int index)
    {
        var fieldValue = GetFieldValue(index);
        return fieldValue.IsNull ? null : GetTextReader(fieldValue);
    }

    /// <summary>
    /// Gets the value of the specified column as a <see cref="MdbLValStream"/>
    /// </summary>
    /// <param name="columnName">The column name (case sensitive).</param>
    /// <returns>The value of the column.</returns>
    /// <throws cref="InvalidCastException">The specified cast is not valid.</throws>
    /// <remarks>
    /// <para>
    /// This function only works on a <see cref="MdbColumnType.Memo"/> column. The StreamReader will be set
    /// to use the correct compressed encoding for JET4 (Access 2000 and later) databases.
    /// encoding.
    /// </para>
    /// <para>This function returns null (not a stream over nothing) if the field value is null.</para>
    /// </remarks>
    public StreamReader? GetStreamReader(string columnName)
    {
        var fieldValue = GetFieldValue(columnName);
        return fieldValue.IsNull ? null : GetTextReader(fieldValue);
    }
}

