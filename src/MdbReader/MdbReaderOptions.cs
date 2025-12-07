// This file is part of MdbReader. Licensed under the LGPL version 2.0.
// You should have received a coy of the GNU LGPL version along with this
// program. If not, see https://www.gnu.org/licenses/old-licenses/lgpl-2.0.html
//
// Copyright Micah Makaiwi.
// Based on code from libmdb (https://github.com/mdbtools/mdbtools)

namespace MMKiwi.MdbReader;

/// <summary>
/// Provides the ability for the user to define custom behavior for <see cref="MdbConnection" />
/// </summary>
public record class MdbReaderOptions
{
    /// <summary>
    /// The string comparer to use for table names in the <see cref="MdbTables" /> collection.
    /// </summary>
    public IEqualityComparer<string> TableNameComparison { get; }

    /// <summary>
    /// Gets the minimum number of columns required before a dictionary is created for column lookups.
    /// </summary>
    public int RowDictionaryCreationThreshold { get; set; } = 10;

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
