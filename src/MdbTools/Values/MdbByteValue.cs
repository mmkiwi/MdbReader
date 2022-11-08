// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.
//
// Based on code from libmdb (https://github.com/mdbtools/mdbtools)

using System.Collections.Immutable;

namespace MMKiwi.MdbTools.Values;

/// <summary>
/// A database value corresponding to an Access <see cref="ColumnType.Byte"/>. 
/// </summary>
/// <remarks>
/// <para>
/// This is a 8-bit unsigned integer and <see cref="Value" /> returns a <see cref="byte" /> 
/// (values from 0 to 255).
/// Referred to in the Access GUI as a Number column with the Byte size, and a <c>BYTE</c> in SQL.
/// </para>
/// <para> 
/// This class is used for non-nullable columns only. For nullable columns, use <see cref="Nullable" />
/// </para>
/// </remarks>
public sealed class MdbByteValue : MdbValue<byte>, IValueAllowableType
{
    internal MdbByteValue(MdbColumn column, bool isNull, ImmutableArray<byte> binaryValue)
        : base(column, isNull, binaryValue, false, 1, 1, AllowableType) { }

    /// <summary>
    /// The <see cref="ColumnType" /> that can be used for this value.
    /// This will always be <see cref="ColumnType.Byte" />
    /// </summary>
    public static ColumnType AllowableType => ColumnType.Byte;

    /// <summary>
    /// The value for the specific row and column. A <see cref="byte" /> (values from 0 to 255).
    /// </summary>
    public override byte Value => ConversionFunctions.AsByte(BinaryValue.AsSpan());

    /// <summary>
    /// A database value corresponding to an Access <see cref="ColumnType.Byte" />. 
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is a 8-bit unsigned integer and <see cref="Value" /> returns a <see cref="byte" /> 
    /// (values from 0 to 255).
    /// Referred to in the Access GUI as a Number column with the Byte size, and a <c>BYTE</c> in SQL.
    /// </para>
    /// This class can be used for nullable and non-nullable columns. 
    /// For non-nullable columns, use <see cref="MdbByteValue" /> to return a non-nullable value.
    /// </remarks>

    public sealed class Nullable : MdbValue<byte?>
    {
        internal Nullable(MdbColumn column, bool isNull, ImmutableArray<byte> binaryValue)
            : base(column, isNull, binaryValue, true, 1, 1, AllowableType) { }

        /// <summary>
        /// The value for the specific row and column. A <see cref="Nullable{T}">nullable</see> 
        /// <see cref="byte" />  (values from 0 to 255).
        /// </summary>
        public override byte? Value => IsNull ? null : ConversionFunctions.AsByte(BinaryValue.AsSpan());

        /// <summary>
        /// Implicitly cast a non-nullable <see cref="MdbByteValue" /> into a <see cref="Nullable" />
        /// </summary>
        /// <param name="val">The <see cref="MdbByteValue" /> to convert</param>
        public static implicit operator Nullable(MdbByteValue val)
            => new(val.Column, val.IsNull, val.BinaryValue);
    }
}
