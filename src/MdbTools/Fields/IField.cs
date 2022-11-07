// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.
//
// Based on code from libmdb (https://github.com/mdbtools/mdbtools)

namespace MMKiwi.MdbTools.Fields;

public interface IMdbField
{
    MdbColumn Column { get; }
    object? Value { get; }
    bool IsNull { get; }
}

public interface IMdbField<TOut> : IMdbField
{
    new TOut? Value { get; }
}
