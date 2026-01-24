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
    private static bool GetBoolean(IMdbValue fieldValue) => fieldValue switch
    {
        MdbBoolValue boolValue => boolValue.Value,
        _ => ThrowInvalidCast<bool>(fieldValue, nameof(Boolean))
    };

    /// <summary>
    /// Gets the value of the specified column as a Boolean.
    /// </summary>
    /// <param name="index">The zero-based column ordinal.</param>
    /// <returns>The value of the column.</returns>
    /// <throws cref="InvalidCastException">The specified cast is not valid.</throws>
    /// <remarks>
    /// <para>No conversions are performed; therefore, the column must be a <see cref="MdbColumnType.Boolean"/>, or an exception is generated.</para>
    /// <para>Access boolean columns can never be null.</para>
    /// </remarks>
    public bool GetBoolean(int index)
    {
        var fieldValue = GetFieldValue(index);
        return GetBoolean(fieldValue);
    }

    /// <summary>
    /// Gets the value of the specified column as a Boolean.
    /// </summary>
    /// <param name="columnName">The column name (case sensitive).</param>
    /// <returns>The value of the column.</returns>
    /// <throws cref="InvalidCastException">The specified cast is not valid.</throws>
    /// <remarks>
    /// <para>No conversions are performed; therefore, the column must be a <see cref="MdbColumnType.Boolean"/>, or an exception is generated.</para>
    /// <para>Access boolean columns can never be null.</para>
    /// </remarks>
    public bool GetBoolean(string columnName)
    {
        var fieldValue = GetFieldValue(columnName);
        return GetBoolean(fieldValue);
    }
}
