// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.
//
// Based on code from libmdb (https://github.com/mdbtools/mdbtools)

namespace MMKiwi.MdbTools.Mutable;
public static partial class MdbBuilder
{
    internal class Index
    {
        public int IndexNum { get; set; }
        public int IndexNum2 { get; set; }
        public byte RelTableType { get; set; }
        public int RlIndexNum { get; set; }
        public int RelTablePage { get; set; }
        public bool CascadeUpdates { get; set; }
        public bool CascadeDeletes { get; set; }
        public IndexType IndexType { get; set; }
        public string? Name { get; set; }
    }
}