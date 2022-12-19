// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.
//
// Based on code from libmdb (https://github.com/mdbtools/mdbtools)

using System.Collections.Immutable;

using MMKiwi.Collections;
using MMKiwi.MdbReader.Values;

namespace MMKiwi.MdbReader;

/// <summary>
/// A row in an Access Database
/// </summary>
public sealed class MdbDataRow: ImmutableKeyedCollection<string,IMdbValue>
{
    internal MdbDataRow(ImmutableArray<IMdbValue> baseCollection, IEqualityComparer<string>? comparer, int dictionaryCreationThreshold) : base(baseCollection, comparer, dictionaryCreationThreshold)
    {
    }

    /// <inheritdocs />
    protected override string GetKeyForItem(IMdbValue item) => item.Column.Name;
}