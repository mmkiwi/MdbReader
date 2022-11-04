// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.
//
// Based on code from libmdb (https://github.com/mdbtools/mdbtools)

using System.Collections.Immutable;

using MMKiwi.MdbTools.Mutable;

namespace MMKiwi.MdbTools;

public sealed record class MdbDataRow
{

    public MdbDataRow(IEnumerable<MdbField> fields)
    {
        Fields = fields.ToImmutableArray();
    }

    public ImmutableArray<MdbField> Fields { get; }
}