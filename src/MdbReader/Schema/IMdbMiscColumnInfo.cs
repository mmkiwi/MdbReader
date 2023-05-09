// This file is part of MdbReader. Licensed under the LGPL version 2.0.
// You should have received a coy of the GNU LGPL version along with this
// program. If not, see https://www.gnu.org/licenses/old-licenses/lgpl-2.0.html
//
// Copyright Micah Makaiwi.
// Based on code from libmdb (https://github.com/mdbtools/mdbtools)

namespace MMKiwi.MdbReader.Schema;

/// <summary>
/// Parent class for <see cref="MdbDecimalColumnInfo" /> and <see cref="MdbMiscColumnInfo" />, which 
/// contain extra column information fields for decimal columns and textual columns, respesctively.
/// </summary>
public abstract record class MdbMiscColumnInfo
{
    internal MdbMiscColumnInfo() { }

    private protected abstract IEnumerable<MdbColumnType> ColumnTypes { get; }
}
