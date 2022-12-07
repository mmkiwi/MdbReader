// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.
//
// Based on code from libmdb (https://github.com/mdbtools/mdbtools)

using System.Collections.Immutable;
using System.Diagnostics;

namespace MMKiwi.MdbTools.Values;

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
public sealed class MdbBoolValue : MdbValue<bool>, IValueAllowableType
{
    internal MdbBoolValue(MdbColumn column, bool isNull)
     : base(column, isNull, ImmutableArray<byte>.Empty, true, 0, 0, AllowableType)
    {
        Value = isNull;
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
