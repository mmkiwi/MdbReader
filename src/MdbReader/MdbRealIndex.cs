﻿// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.
//
// Based on code from libmdb (https://github.com/mdbtools/mdbtools)

namespace MMKiwi.MdbReader;

internal class MdbRealIndex
{
    internal class Builder {
        public MdbRealIndexColumn.Builder[] Columns { get; } = new MdbRealIndexColumn.Builder[10];
        public int UsedPages { get; set; }
        public int FirstDataPointer { get; set; }
        public ushort Flags { get; set; }
        public int NumIndexRows { get; set; }
    }
}
