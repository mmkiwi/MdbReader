// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.
//
// Based on code from libmdb (https://github.com/mdbtools/mdbtools)

using System.Collections.Immutable;

namespace MMKiwi.MdbTools.Values;

/// <summary>
/// A database value corresponding to an Access <see cref="MdbColumnType.Double"/>. 
/// </summary>
/// <remarks>
/// <para>
/// This is a 64-bit floatting-point number and <see cref="Value" /> returns a <see cref="double" />.
/// Referred to in the Access GUI as a Number column with the Double size, and as a <c>DOUBLE</c> in SQL.
/// </para>
/// <para> 
/// This class is used for non-nullable columns only. For nullable columns, use <see cref="Nullable" />
/// </para>
/// </remarks>
public sealed class MdbDoubleValue : MdbValue<double>, IValueAllowableType
{
    internal MdbDoubleValue(MdbColumn column, bool isNull, ImmutableArray<byte> binaryValue)
        : base(column, isNull, binaryValue, false, 8, 8, AllowableType) { }

    /// <summary>
    /// The <see cref="MdbColumnType" /> that can be used for this value.
    /// This will always be <see cref="MdbColumnType.Double" />
    /// </summary>
    public static MdbColumnType AllowableType => MdbColumnType.Double;

    /// <summary>
    /// The value for the specific row and column. A <see cref="double" />.
    /// </summary>
    public override double Value => ConversionFunctions.AsDouble(BinaryValue.AsSpan());

    /// <summary>
    /// A database value corresponding to an Access <see cref="MdbColumnType.Double"/>. 
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is a 64-bit floatting-point number and <see cref="Value" /> returns a <see cref="double" />.
    /// Referred to in the Access GUI as a Number column with the Double size, and as a <c>DOUBLE</c> in SQL.
    /// </para>
    /// <para> 
    /// This class can be used for nullable and non-nullable columns. 
    /// For non-nullable columns, use <see cref="MdbDoubleValue" /> to return a non-nullable value.
    /// </para>
    /// </remarks>
    public sealed class Nullable : MdbValue<double?>
    {
        internal Nullable(MdbColumn column, bool isNull, ImmutableArray<byte> binaryValue)
            : base(column, isNull, binaryValue, true, 8, 8, AllowableType) { }

        /// <summary>
        /// The value for the specific row and column. A <see cref="double" />.
        /// </summary>
        public override double? Value => IsNull ? null : ConversionFunctions.AsDouble(BinaryValue.AsSpan());


        /// <summary>
        /// Implicitly cast a non-nullable <see cref="MdbDoubleValue" /> into a <see cref="Nullable" />
        /// </summary>
        /// <param name="val">The <see cref="MdbDoubleValue" /> to convert</param>
        public static implicit operator Nullable(MdbDoubleValue val)
            => new(val.Column, val.IsNull, val.BinaryValue);
    }
}
