// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.
//
// Based on code from libmdb (https://github.com/mdbtools/mdbtools)

using System.Collections.Immutable;

namespace MMKiwi.MdbTools.Values;

/// <summary>
/// A database value corresponding to an Access <see cref="MdbColumnType.Int"/>. 
/// </summary>
/// <remarks>
/// <para>
/// This is a 16-bit signed integer and <see cref="Value" /> returns a <see cref="short" /> 
/// (values from -32768 to 32767).
/// Referred to in the Access GUI as a Number column with the Integer size, and as an <c>INT</c> in SQL.
/// </para>
/// <para> 
/// This class is used for non-nullable columns only. For nullable columns, use <see cref="Nullable" />
/// </para>
/// </remarks>
public sealed class MdbIntValue : MdbValue<short>, IValueAllowableType
{
    internal MdbIntValue(MdbColumn column, bool isNull, ImmutableArray<byte> binaryValue)
        : base(column, isNull, binaryValue, false, 2, 2, AllowableType) { }

    /// <summary>
    /// The <see cref="MdbColumnType" /> that can be used for this value.
    /// This will always be <see cref="MdbColumnType.Int" />
    /// </summary>
    public static MdbColumnType AllowableType => MdbColumnType.Int;

    /// <summary>
    /// The value for the specific row and column. A <see cref="short" /> (values from -32768 to 32767).
    /// </summary>
    public override short Value => ConversionFunctions.AsShort(BinaryValue.AsSpan());

    /// <summary>
    /// A database value corresponding to an Access <see cref="MdbColumnType.Int" />. 
    /// Referred to in the Access GUI as a Number column with the Integer size, and as an <c>INT</c> in SQL.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is a 16-bit signed integer and <see cref="Value" /> returns a <see cref="short" /> 
    /// (values from -32768 to 32767).
    /// </para>
    /// <para> 
    /// This class can be used for nullable and non-nullable columns. 
    /// For non-nullable columns, use <see cref="MdbIntValue" /> to return a non-nullable value.
    /// </para>
    /// </remarks>
    public sealed class Nullable : MdbValue<int?>
    {
        internal Nullable(MdbColumn column, bool isNull, ImmutableArray<byte> binaryValue)
            : base(column, isNull, binaryValue, true, 2, 2, AllowableType) { }

        /// <summary>
        /// The value for the specific row and column. A <see cref="Nullable{T}">nullable</see>
        /// <see cref="short" /> (values from -32768 to 32767).
        /// </summary>
        public override int? Value => IsNull ? null : ConversionFunctions.AsShort(BinaryValue.AsSpan());

        /// <summary>
        /// Implicitly cast a non-nullable <see cref="MdbIntValue" /> into a <see cref="Nullable" />
        /// </summary>
        /// <param name="val">The <see cref="MdbIntValue" /> to convert</param>
        public static implicit operator Nullable(MdbIntValue val)
            => new(val.Column, val.IsNull, val.BinaryValue);
    }
}
