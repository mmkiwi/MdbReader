// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.
//
// Based on code from libmdb (https://github.com/mdbtools/mdbtools)

using System.Collections.Immutable;

namespace MMKiwi.MdbTools.Values;

/// <summary>
/// A database value corresponding to an Access <see cref="MdbColumnType.Single"/>. 
/// </summary>
/// <remarks>
/// <para>
/// This is a 32-bit floating-point decimal and <see cref="Value" /> returns a <see cref="float" />.
/// Referred to in the Access GUI as a 
/// </para>
/// <para> 
/// This class is used for non-nullable columns only. For nullable columns, use <see cref="Nullable" />
/// </para>
/// </remarks>
public sealed class MdbSingleValue : MdbValue<float>, IValueAllowableType
{
    internal MdbSingleValue(MdbColumn column, bool isNull, ImmutableArray<byte> binaryValue)
        : base(column, isNull, binaryValue, false, 4, 4, AllowableType) { }

    /// <summary>
    /// The <see cref="MdbColumnType" /> that can be used for this value.
    /// This will always be <see cref="MdbColumnType.Single" />
    /// </summary>
    public static MdbColumnType AllowableType => MdbColumnType.Single;

    /// <summary>
    /// The value for the specific row and column. A <see cref="float" />.
    /// </summary>
    public override float Value => ConversionFunctions.AsSingle(BinaryValue.AsSpan());

    /// <summary>
    /// A database value corresponding to an Access <see cref="MdbColumnType.Single"/>. 
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is a 32-bit floating-point decimal and <see cref="Value" /> returns a <see cref="float" /> 
    /// </para>
    /// <para> 
    /// This class can be used for nullable and non-nullable columns. 
    /// For non-nullable columns, use <see cref="MdbSingleValue" /> to return a non-nullable value.
    /// </para>
    /// </remarks>
    public sealed class Nullable : MdbValue<float?>
    {
        internal Nullable(MdbColumn column, bool isNull, ImmutableArray<byte> binaryValue)
            : base(column, isNull, binaryValue, true, 4, 4, AllowableType) { }

        /// <summary>
        /// The value for the specific row and column. A <see cref="float" />.
        /// </summary>
        public override float? Value => IsNull ? null : ConversionFunctions.AsSingle(BinaryValue.AsSpan());
        
        /// <summary>
        /// Implicitly cast a non-nullable <see cref="MdbSingleValue" /> into a <see cref="Nullable" />
        /// </summary>
        /// <param name="val">The <see cref="MdbSingleValue" /> to convert</param>
        public static implicit operator Nullable(MdbSingleValue val)
            => new(val.Column, val.IsNull, val.BinaryValue);
    }
}
