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
    private Guid GetGuid(IMdbValue fieldValue) => fieldValue switch
    {
        //Internal method for getting GUIDs
        MdbGuidValue guidValue => guidValue.Value,
        _ => ThrowInvalidCast<Guid>(fieldValue, nameof(Guid))
    };

    /// <summary>
    /// Gets the value of the specified column as a globally unique identifier (GUID).
    /// </summary>
    /// <remarks>
    /// This method will throw an exception if the value is null or the column type is
    /// not an <see cref="MdbColumnType.Guid" />.
    /// </remarks>
    /// <param name="index">The zero-based column ordinal.</param>
    /// <returns>The value of the specified column.</returns>
    /// <throws cref="IndexOutOfRangeException">The index passed was outside of the range of 0 through <see cref="FieldCount" />.</throws>
    /// <throws cref="InvalidCastException">The specified cast is not valid, or the underlying value is <c>null</c></throws>
    public Guid GetGuid(int index)
    {
        var fieldValue = GetFieldValue(index);
        ThrowIfNullCast(fieldValue, nameof(Guid));
        return GetGuid(fieldValue);
    }

    /// <summary>
    /// Gets the value of the specified column as a globally unique identifier (GUID).
    /// </summary>
    /// <remarks>
    /// This method will throw an exception if the value is null or the column type is
    /// not an <see cref="MdbColumnType.Guid" />.
    /// </remarks>
    /// <param name="columnName">The column name</param>
    /// <returns>The value of the specified column.</returns>
    /// <throws cref="ArgumentNullException"><c>columnName</c> is <c>null</c>.</throws>
    /// <throws cref="IndexOutOfRangeException"> No column with the specified name was found.</throws>
    /// <throws cref="InvalidCastException">The specified cast is not valid, or the underlying value is <c>null</c></throws>
    public Guid GetGuid(string columnName)
    {
        var fieldValue = GetFieldValue(columnName);
        ThrowIfNullCast(fieldValue, nameof(Guid));
        return GetGuid(fieldValue);
    }

    /// <summary>
    /// Gets the value of the specified column as a globally unique identifier (GUID).
    /// </summary>
    /// <remarks>
    /// This method will throw an exception if the column type is
    /// not an <see cref="MdbColumnType.Guid" />.
    /// </remarks>
    /// <param name="index">The zero-based column ordinal.</param>
    /// <returns>The value of the specified column.</returns>
    /// <throws cref="IndexOutOfRangeException">The index passed was outside of the range of 0 through <see cref="FieldCount" />.</throws>
    /// <throws cref="InvalidCastException">The specified cast is not valid.</throws>
    public Guid? GetNullableGuid(int index)
    {
        var fieldValue = GetFieldValue(index);
        if (fieldValue.IsNull) return null;
        return GetGuid(fieldValue);
    }

    /// <summary>
    /// Gets the value of the specified column as a globally unique identifier (GUID).
    /// </summary>
    /// <remarks>
    /// This method will throw an exception if the column type is
    /// not an <see cref="MdbColumnType.Guid" />.
    /// </remarks>
    /// <param name="columnName">The column name</param>
    /// <returns>The value of the specified column.</returns>
    /// <throws cref="ArgumentNullException"><c>columnName</c> is <c>null</c>.</throws>
    /// <throws cref="IndexOutOfRangeException">No column with the specified name was found.</throws>
    /// <throws cref="InvalidCastException">The specified cast is not valid.</throws>
    public Guid? GetNullableGuid(string columnName)
    {
        var fieldValue = GetFieldValue(columnName);
        if (fieldValue.IsNull) return null;
        return GetGuid(fieldValue);
    }
}
