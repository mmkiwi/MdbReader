// This file is part of MdbReader. Licensed under the LGPL version 2.0.
// You should have received a coy of the GNU LGPL version along with this
// program. If not, see https://www.gnu.org/licenses/old-licenses/lgpl-2.0.html
//
// Copyright Micah Makaiwi.
// Based on code from libmdb (https://github.com/mdbtools/mdbtools)

using MMKiwi.MdbReader.Values;

namespace MMKiwi.MdbReader;

public sealed partial class MdbDataRow
{
    private string GetString(IMdbValue fieldValue) => fieldValue switch
    {
        MdbStringValue stringValue => stringValue.Value,
        MdbMemoValue memoValue => memoValue.StreamReader!.ReadToEnd(),
        _ => ThrowInvalidCast<string>(fieldValue, nameof(String))
    };

    /// <summary>
    /// Gets the value of the specified column as a string.
    /// </summary>
    /// <param name="index">The zero-based column ordinal.</param>
    /// <returns>The value of the column.</returns>
    /// <throws cref="InvalidCastException">The specified cast is not valid.</throws>
    /// <remarks>
    /// <para>
    /// No conversions are performed; therefore, the column must be a <see cref="MdbColumnType.Text"/> or 
    /// <see cref="MdbColumnType.Memo"/>, otherwise an exception is generated.
    /// </para>
    /// </remarks>
    public string? GetString(int index)
    {
        var fieldValue = GetFieldValue(index);
        if (fieldValue.IsNull)
            return null;
        return GetString(fieldValue);
    }

    /// <summary>
    /// Gets the value of the specified column as a string.
    /// </summary>
    /// <param name="columnName">The column name (case sensitive).</param>
    /// <returns>The value of the column.</returns>
    /// <throws cref="InvalidCastException">The specified cast is not valid.</throws>
    /// <remarks>
    /// <para>
    /// No conversions are performed; therefore, the column must be a <see cref="MdbColumnType.Text"/> or 
    /// <see cref="MdbColumnType.Memo"/>, otherwise an exception is generated.
    /// </para>
    /// </remarks>
    public string? GetString(string columnName)
    {
        var fieldValue = GetFieldValue(columnName);
        if (fieldValue.IsNull)
            return null;
        return GetString(fieldValue);
    }

    /// <summary>
    /// Gets the value of the specified column as a string, throwing an exception for a null string.
    /// </summary>
    /// <param name="index">The zero-based column ordinal.</param>
    /// <returns>The value of the column.</returns>
    /// <throws cref="InvalidCastException">The specified cast is not valid.</throws>
    /// <remarks>
    /// <para>
    /// No conversions are performed; therefore, the column must be a <see cref="MdbColumnType.Text"/> or 
    /// <see cref="MdbColumnType.Memo"/>, otherwise an exception is generated.
    /// </para>
    /// <para>For nullable columns, call <see cref="IsNull(int)"/> to check for null values before calling this method.
    /// This method throws when trying to return a null value.</para>
    /// </remarks>
    public string GetStringNotNull(int index)
    {
        var fieldValue = GetFieldValue(index);
        ThrowIfNullCast(fieldValue, nameof(String));
        return GetString(fieldValue);
    }

    /// <summary>
    /// Gets the value of the specified column as a string, throwing an exception for a null string.
    /// </summary>
    /// <param name="columnName">The column name (case sensitive).</param>
    /// <returns>The value of the column.</returns>
    /// <throws cref="InvalidCastException">The specified cast is not valid.</throws>
    /// <remarks>
    /// <para>
    /// No conversions are performed; therefore, the column must be a <see cref="MdbColumnType.Text"/> or 
    /// <see cref="MdbColumnType.Memo"/>, otherwise an exception is generated.
    /// </para>
    /// <para>For nullable columns, call <see cref="IsNull(int)"/> to check for null values before calling this method.
    /// This method throws when trying to return a null value.</para>
    /// </remarks>
    public string GetStringNotNull(string columnName)
    {
        var fieldValue = GetFieldValue(columnName);
        ThrowIfNullCast(fieldValue, nameof(String));
        return GetString(fieldValue);
    }
}
