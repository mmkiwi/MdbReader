// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.
//
// Based on code from libmdb (https://github.com/mdbtools/mdbtools)

using System.Collections.Immutable;

using MMKiwi.MdbTools.Values;

namespace MMKiwi.MdbTools;

/// <summary>
/// A row in an Access Database
/// </summary>
public sealed record class MdbDataRow
{

    internal MdbDataRow(List<IMdbValue> fields)
    {
        Values = fields.ToImmutableArray();
    }

    /// <summary>
    /// The values of all columns in the row
    /// </summary>
    /// <remarks>
    /// 
    /// </remarks>
    public ImmutableArray<IMdbValue> Values { get; }
}