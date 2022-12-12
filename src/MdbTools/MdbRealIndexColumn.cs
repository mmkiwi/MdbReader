// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.
//
// Based on code from libmdb (https://github.com/mdbtools/mdbtools)

namespace MMKiwi.MdbTools;

internal class MdbRealIndexColumn
{
    /// <summary>
    /// The mutable builder for a future MdbRealIndexColumn class.
    /// </summary>
    internal class Builder
    {
        public ushort ColNum { get; set; }
        public byte ColOrder { get; set; }
    }
}
