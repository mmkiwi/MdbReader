// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.
//
// Based on code from libmdb (https://github.com/mdbtools/mdbtools)

using System.Collections.Immutable;
using System.Diagnostics;

namespace MMKiwi.MdbReader.Values;

/// <summary>
/// A database value corresponding to an Access <see cref="MdbColumnType.DateTime"/>. 
/// </summary>
/// <remarks>
/// <para>
/// This is stored in the database as an 64-bit floating-point number repesenting the decimal days
/// since January 1, 1900. <see cref="Value" /> returns a <see cref="DateTime" />
/// Referred to in the Access GUI as a Date/Time column, and a <c>DATETIME</c> in SQL.
/// </para>
/// <para> 
/// This class is used for non-nullable columns only. For nullable columns, use <see cref="Nullable" />
/// </para>
/// </remarks>
[DebuggerDisplay("{Column.Name}: {Value}")]
public sealed class MdbDateTimeValue : MdbValue<DateTime>, IValueAllowableType
{
    internal MdbDateTimeValue(MdbColumn column, bool isNull, ImmutableArray<byte> binaryValue)
        : base(column, isNull, binaryValue, false, 8, 8, AllowableType) { }

    /// <summary>
    /// The <see cref="MdbColumnType" /> that can be used for this value.
    /// This will always be <see cref="MdbColumnType.DateTime" />
    /// </summary>
    public static MdbColumnType AllowableType => MdbColumnType.DateTime;

    /// <summary>
    /// The value for the specific row and column. A <see cref="DateTime" />.
    /// </summary>
    public override DateTime Value => ConversionFunctions.AsDateTime(BinaryValue.AsSpan());

    /// <summary>
    /// A database value corresponding to an Access <see cref="MdbColumnType.DateTime" />.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is stored in the database as an 64-bit floating-point number repesenting the decimal days
    /// since January 1, 1900. <see cref="Value" /> returns a <see cref="DateTime" />
    /// Referred to in the Access GUI as a Date/Time column, and a <c>DATETIME</c> in SQL.
    /// </para>
    /// <para> 
    /// This class can be used for nullable and non-nullable columns. 
    /// For non-nullable columns, use <see cref="MdbDateTimeValue" /> to return a non-nullable value.
    /// </para>
    /// </remarks>
    [DebuggerDisplay("{Column.Name}: {Value}")]
    public sealed class Nullable : MdbValue<DateTime?>
    {
        internal Nullable(MdbColumn column, bool isNull, ImmutableArray<byte> binaryValue)
            : base(column, isNull, binaryValue, true, 8, 8, AllowableType) { }

        /// <summary>
        /// The <see cref="MdbColumnType" /> that can be used for this value.
        /// This will always be <see cref="MdbColumnType.DateTime" />
        /// </summary>
        public override DateTime? Value => IsNull ? null : ConversionFunctions.AsDateTime(BinaryValue.AsSpan());

        /// <summary>
        /// Implicitly cast a non-nullable <see cref="MdbDateTimeValue" /> into a <see cref="Nullable" />
        /// </summary>
        /// <param name="val">The <see cref="MdbDateTimeValue" /> to convert</param>
        public static implicit operator Nullable(MdbDateTimeValue val)
            => new(val.Column, val.IsNull, val.BinaryValue);
    }
}
