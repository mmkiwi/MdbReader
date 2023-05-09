// This file is part of MdbReader. Licensed under the LGPL version 2.0.
// You should have received a coy of the GNU LGPL version along with this
// program. If not, see https://www.gnu.org/licenses/old-licenses/lgpl-2.0.html
//
// Copyright Micah Makaiwi.
// Based on code from libmdb (https://github.com/mdbtools/mdbtools)

namespace MMKiwi.MdbReader.Values;

/// <summary>
/// The base interface for all <see cref="MdbValue{TVal}" /> classes. This
/// represents a single value for a specific row or column.
/// </summary>
internal interface IMdbValue
{
    /// <summary>
    /// The column for the 
    /// </summary>
    /// <value></value>
    MdbColumn Column { get; }

    /// <summary>
    /// The value for the specific field and row.
    /// </summary>
    object? Value { get; }

    /// <summary>
    /// If true, the value is null in the database. <see cref="Value" /> will 
    /// be null if this is true.
    /// </summary>
    bool IsNull { get; }
}

/// <summary>
/// The base interface for all <see cref="MdbValue{TVal}" /> classes. This
/// represents a single value for a specific row or column. This interface 
/// is strongly-typed based on the type of the column.
/// </summary>
/// <typeparam name="TVal">
/// The output type. See <see cref="MdbValue{TVal}" /> for a list of the allowed
/// types.
/// </typeparam>
internal interface IMdbValue<TVal> : IMdbValue
{
    /// <summary>
    /// The value for the specific field and row.
    /// </summary>
    new TVal? Value { get; }
}
