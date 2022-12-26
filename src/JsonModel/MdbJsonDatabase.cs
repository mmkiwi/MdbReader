// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.
//
// Based on code from libmdb (https://github.com/mdbtools/mdbtools)

using System.Collections.Immutable;

namespace MMKiwi.MdbReader.JsonModel;
public sealed record class MdbJsonDatabase(ImmutableDictionary<string, MdbJsonTable> Tables, uint DbKey, DateTime CreateDate, int CodePage, ushort Collation) { }