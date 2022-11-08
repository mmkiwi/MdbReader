// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.
//
// Based on code from libmdb (https://github.com/mdbtools/mdbtools)

namespace MMKiwi.MdbTools.Values;

/// <summary>
/// The base interface for all <see cref="MdbValue{TVal}" /> classes. This
/// represents a single value for a specific row or column.
/// </summary>
public interface IMdbValue
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
public interface IMdbValue<TVal> : IMdbValue
{
    /// <summary>
    /// The value for the specific field and row.
    /// </summary>
    new TVal? Value { get; }
}
