// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.
//
// Based on code from libmdb (https://github.com/mdbtools/mdbtools)

using System.Collections.Immutable;

namespace MMKiwi.MdbTools.Values;

/// <summary>
/// A database value corresponding to an Access <see cref="ColumnType.LongInt"/>. 
/// </summary>
/// <remarks>
/// <para>
/// This is a 32-bit signed integer and <see cref="Value" /> returns a <see cref="int" /> 
/// (values from -2,147,483,648 to 2,147,483,647).
/// Referred to in the Access GUI as a Number column with the Long Integer size, and as an <c>LONG</c> in SQL.
/// </para>
/// <para> 
/// This class is used for non-nullable columns only. For nullable columns, use <see cref="Nullable" />
/// </para>
/// </remarks>
public sealed class MdbLongIntValue : MdbValue<int>, IValueAllowableType
{
    internal MdbLongIntValue(MdbColumn column, bool isNull, ImmutableArray<byte> binaryValue)
        : base(column, isNull, binaryValue, false, 4, 4, AllowableType) { }

    /// <summary>
    /// The <see cref="ColumnType" /> that can be used for this value.
    /// This will always be <see cref="ColumnType.LongInt" />
    /// </summary>
    public static ColumnType AllowableType => ColumnType.LongInt;

    /// <summary>
    /// The value for the specific row and column. An <see cref="int" />
    /// </summary>
    public override int Value => ConversionFunctions.AsInt(BinaryValue.AsSpan());

    /// <summary>
    /// A database value corresponding to an Access <see cref="ColumnType.LongInt"/>. 
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is a 32-bit signed integer and <see cref="Value" /> returns a <see cref="int" /> 
    /// (values from -2,147,483,648 to 2,147,483,647).
    /// Referred to in the Access GUI as a Number column with the Long Integer size, and as an <c>LONG</c> in SQL.
    /// </para>
    /// <para> 
    /// This class can be used for nullable and non-nullable columns. 
    /// For non-nullable columns, use <see cref="MdbLongIntValue" /> to return a non-nullable value.
    /// </para>
    /// </remarks>
    public sealed class Nullable : MdbValue<int?>
    {
        internal Nullable(MdbColumn column, bool isNull, ImmutableArray<byte> binaryValue)
            : base(column, isNull, binaryValue, true, 4, 4, AllowableType) { }

        /// <summary>
        /// The value for the specific row and column. An <see cref="int" />
        /// </summary>
        public override int? Value => IsNull ? null : ConversionFunctions.AsInt(BinaryValue.AsSpan());

        /// <summary>
        /// Implicitly cast a non-nullable <see cref="MdbLongIntValue" /> into a <see cref="Nullable" />
        /// </summary>
        /// <param name="val">The <see cref="MdbLongIntValue" /> to convert</param>
        public static implicit operator Nullable(MdbLongIntValue val)
            => new(val.Column, val.IsNull, val.BinaryValue);
    }
}
