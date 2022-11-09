// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.
//
// Based on code from libmdb (https://github.com/mdbtools/mdbtools)

using System.Collections.Immutable;

namespace MMKiwi.MdbTools.Values;

/// <summary>
/// A database value corresponding to an Access <see cref="MdbColumnType.Text"/>. 
/// </summary>
/// <remarks>
/// <para>
/// This is a string encoded with the main database encoding. 
/// <see cref="Value" /> returns a <see cref="string" /> 
/// Referred to in the Access GUI as a Text column, and as an <c>CHAR</c> or <c>VARCHAR</c> in SQL.
/// </para>
/// <para>
/// For <c>CHAR</c> columns (which can only be defined using SQL), there may be null bytes
/// at the end of the string. <see cref="Value" /> will truncate the string at the first null
/// byte. To return the buffer with all the null bytes, or to return the original 8-bit byte value
/// (for instance to write to disc), use <see cref="RawValue" /> instead.
/// </para>
/// <para> 
/// This class is used for non-nullable columns only. For nullable columns, use <see cref="Nullable" />
/// </para>
/// </remarks>
public sealed class MdbStringValue : MdbValue<string>, IValueAllowableType
{
    //Not saving the string to the BinaryValue buffer in order to not duplicate it
    internal MdbStringValue(MdbColumn column, bool isNull, ImmutableArray<byte> binaryValue)
        : base(column, isNull, ImmutableArray<byte>.Empty, false, 0, column.Length, AllowableType)
    {
        Value = ConversionFunctions.AsString(Column.Encoding, binaryValue.AsSpan());
    }

    /// <summary>
    /// The <see cref="MdbColumnType" /> that can be used for this value.
    /// This will always be <see cref="MdbColumnType.Text" />
    /// </summary>
    public static MdbColumnType AllowableType => MdbColumnType.Text;

    /// <summary>
    /// The value for the specific row and column. A <see cref="string" />.
    /// </summary>
    /// <remarks>
    /// <para>
    /// For <c>CHAR</c> columns (which can only be defined using SQL), there may be null bytes
    /// at the end of the string. <see cref="Value" /> will truncate the string at the first null
    /// byte. To return the buffer with all the null bytes, or to return the original 8-bit byte value
    /// (for instance to write to disc), use <see cref="RawValue" /> instead.
    /// </para>
    /// </remarks>
    public override string Value { get; }

    /// <summary>
    /// The raw text bytes for the specific row and column.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The encoding of the text can be retrieved from the <see cref="MdbColumn.Encoding" /> property
    /// on <see cref="MdbValue{TVal}.Column" />
    /// </para>
    /// <para>
    /// For <c>CHAR</c> columns (which can only be defined using SQL), there may be null bytes
    /// at the end of the string. <see cref="Value" /> will truncate the string at the first null
    /// byte. To return the buffer with all the null bytes, or to return the original 8-bit byte value
    /// (for instance to write to disc), use <see cref="RawValue" /> instead.
    /// </para>
    /// </remarks>
    public ImmutableArray<byte> RawValue => BinaryValue;

    /// <summary>
    /// A database value corresponding to an Access <see cref="MdbColumnType.Text"/>. 
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is a string encoded with the main database encoding. 
    /// <see cref="Value" /> returns a <see cref="string" /> 
    /// Referred to in the Access GUI as a Text column, and as an <c>CHAR</c> or <c>VARCHAR</c> in SQL.
    /// </para>
    /// <para>
    /// For <c>CHAR</c> columns (which can only be defined using SQL), there may be null bytes
    /// at the end of the string. <see cref="Value" /> will truncate the string at the first null
    /// byte. To return the buffer with all the null bytes, or to return the original 8-bit byte value
    /// (for instance to write to disc), use <see cref="RawValue" /> instead.
    /// </para>
    /// <para> 
    /// This class is used for nullable and non-nullable columns. For non-nullable columns, use <see cref="MdbStringValue" />
    /// </para>
    /// </remarks>
    public sealed class Nullable : MdbValue<string?>
    {
        internal Nullable(MdbColumn column, bool isNull, ImmutableArray<byte> binaryValue)
            : base(column, isNull, ImmutableArray<byte>.Empty, true, 0, column.Length, AllowableType)
        {
            Value = IsNull ? null : ConversionFunctions.AsString(Column.Encoding, binaryValue.AsSpan());
        }

        /// <summary>
        /// The value for the specific row and column. A <see cref="string" />.
        /// </summary>
        /// <remarks>
        /// <para>
        /// For <c>CHAR</c> columns (which can only be defined using SQL), there may be null bytes
        /// at the end of the string. <see cref="Value" /> will truncate the string at the first null
        /// byte. To return the buffer with all the null bytes, or to return the original 8-bit byte value
        /// (for instance to write to disc), use <see cref="RawValue" /> instead.
        /// </para>
        /// <para>
        /// If <see cref="MdbValue{TVal}.IsNull" /> is true, this returns <c>null</c>.
        /// </para>
        /// </remarks>
        public override string? Value { get; }

        /// <summary>
        /// The raw text bytes for the specific row and column.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The encoding of the text can be retrieved from the <see cref="MdbColumn.Encoding" /> property
        /// on <see cref="MdbValue{TVal}.Column" />
        /// </para>
        /// <para>
        /// For <c>CHAR</c> columns (which can only be defined using SQL), there may be null bytes
        /// at the end of the string. <see cref="Value" /> will truncate the string at the first null
        /// byte. To return the buffer with all the null bytes, or to return the original 8-bit byte value
        /// (for instance to write to disc), use <see cref="RawValue" /> instead.
        /// </para>
        /// <para>
        /// This property never returns null. For an null string, will return an empty array.
        /// </para>
        /// </remarks>
        public ImmutableArray<byte> RawValue => IsNull ? ImmutableArray<byte>.Empty : BinaryValue;

        /// <summary>
        /// Implicitly cast a non-nullable <see cref="MdbStringValue" /> into a <see cref="Nullable" />
        /// </summary>
        /// <param name="val">The <see cref="MdbStringValue" /> to convert</param>
        public static implicit operator Nullable(MdbStringValue val)
            => new(val.Column, val.IsNull, val.BinaryValue);
    }
}
