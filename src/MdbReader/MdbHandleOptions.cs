// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.
//
// Based on code from libmdb (https://github.com/mdbtools/mdbtools)

namespace MMKiwi.MdbReader;

/// <summary>
/// Provides the ability for the user to define custom behavior for <see cref="MdbDatabaseReader" />
/// </summary>
public record class MdbReaderOptions
{
    /// <summary>
    /// The string comparer to use for table names in the <see cref="MdbTables" /> collection.
    /// </summary>
    public IEqualityComparer<string> TableNameComparison { get; }

    /// <summary>
    /// Initializes a new instance of <see cref="MdbReaderOptions" />
    /// </summary>
    /// <param name="tableNameComparison">The string comparer to use for table names in the <see cref="MdbTables" /> collection.</param>
    public MdbReaderOptions(IEqualityComparer<string> tableNameComparison)
    {
        TableNameComparison = tableNameComparison;
    }

    /// <summary>
    /// The default options
    /// </summary>
    /// <remarks>
    /// By default, the case-sensitive invariant culture (<see cref="StringComparer.InvariantCulture" />) is used
    /// for comparing table names.
    /// </remarks>
    public static MdbReaderOptions Default => new(StringComparer.InvariantCulture);
}
