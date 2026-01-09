// This file is part of MdbReader. Licensed under the LGPL version 2.0.
// You should have received a coy of the GNU LGPL version along with this
// program. If not, see https://www.gnu.org/licenses/old-licenses/lgpl-2.0.html
//
// Copyright Micah Makaiwi.
// Based on code from libmdb (https://github.com/mdbtools/mdbtools)

using System.Diagnostics;
using MMKiwi.MdbReader.Schema;

namespace MMKiwi.MdbReader.Values;

/// <summary>
/// A database value corresponding to an Access <see cref="MdbColumnType.Boolean" />. 
/// </summary>
/// <remarks>
/// The boolean value is stored in the row's nullable bitmask. Therefore, this is the only 
/// <see cref="MdbColumnType" /> that cannot be null. If the nullable bitmask is set, then the
/// boolean value is true. <see cref="IMdbValue.IsNull" /> always returns true.
/// Referred to in the Access GUI as a "Yes/No" column, and as a <c>BIT</c> in SQL.
/// </remarks>
[DebuggerDisplay("{Column.Name}: {Value}")]
internal sealed class MdbBoolValue : MdbValue<bool>, IValueAllowableType
{
    internal MdbBoolValue(MdbColumn column, bool isNull)
     : base(column, false, default, 0, 0, AllowableType)
    {
        Value = !isNull;
    }

    /// <summary>
    /// The value for the specific row and column. A <see cref="bool" /> (true/false).
    /// </summary>
    public override bool Value { get; }

    /// <summary>
    /// A list of all <see cref="MdbColumnType" /> that can be used for this value.
    /// This will always be <see cref="MdbColumnType.Boolean" />
    /// </summary>
    public static MdbColumnType AllowableType => MdbColumnType.Boolean;
}
