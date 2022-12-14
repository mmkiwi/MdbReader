// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.
//
// Based on code from libmdb (https://github.com/mdbtools/mdbtools)

namespace MMKiwi.MdbReader;

/// <summary>
/// Parent class for <see cref="MdbDecimalColumnInfo" /> and <see cref="MdbMiscColumnInfo" />, which 
/// contain extra column information fields for decimal columns and textual columns, respesctively.
/// </summary>
public abstract record class MdbMiscColumnInfo
{
    internal MdbMiscColumnInfo() { }

    private protected abstract IEnumerable<MdbColumnType> ColumnTypes { get; }
}
