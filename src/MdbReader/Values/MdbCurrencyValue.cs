// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.
//
// Based on code from libmdb (https://github.com/mdbtools/mdbtools)

using System.Collections.Immutable;
using System.Diagnostics;

namespace MMKiwi.MdbReader.Values;

/// <summary>
/// A database value corresponding to an Access <see cref="MdbColumnType.Currency"/>. 
/// </summary>
/// <remarks>
/// <para>
/// This is a 64-bit signed integer in the database, multiplied by 10,000 (e.g. $182.60 is
/// stored as 1,826,000). <see cref="Value" /> returns a <see cref="decimal" /> (in the 
/// example above, <c>183.60M</c>)
/// Referred to in the Access GUI as a Currency column with the Integer size, and as an <c>CURRENCY</c> in SQL.
/// </para>
/// <para> 
/// This class is used for non-nullable columns only. For nullable columns, use <see cref="Nullable" />
/// </para>
/// </remarks>
[DebuggerDisplay("{Column.Name}: {Value}")]
public sealed class MdbCurrencyValue : MdbValue<decimal>, IValueAllowableType
{
    internal MdbCurrencyValue(MdbColumn column, bool isNull, ImmutableArray<byte> binaryValue)
        : base(column, isNull, binaryValue, false, 8, 8, AllowableType) { }

    /// <summary>
    /// The <see cref="MdbColumnType" /> that can be used for this value.
    /// This will always be <see cref="MdbColumnType.Currency" />
    /// </summary>
    public static MdbColumnType AllowableType => MdbColumnType.Currency;

    /// <summary>
    /// The value for the specific row and column. A <see cref="decimal" />.
    /// </summary>
    public override decimal Value => ConversionFunctions.AsCurrency(BinaryValue.AsSpan());

    /// <summary>
    /// A database value corresponding to an Access <see cref="MdbColumnType.Currency"/>. 
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is a 64-bit signed integer in the database, multiplied by 10,000 (e.g. $182.60 is
    /// stored as 1,826,000). <see cref="Value" /> returns a <see cref="decimal" /> (in the 
    /// example above, <c>183.60M</c>)
    /// Referred to in the Access GUI as a Currency column with the Integer size, and as an <c>CURRENCY</c> in SQL.
    /// </para>
    /// <para> 
    /// This class can be used for nullable and non-nullable columns. 
    /// For non-nullable columns, use <see cref="MdbCurrencyValue" /> to return a non-nullable value.
    /// </para>
    /// </remarks>
    [DebuggerDisplay("{Column.Name}: {Value}")]
    public sealed class Nullable : MdbValue<decimal?>
    {
        internal Nullable(MdbColumn column, bool isNull, ImmutableArray<byte> binaryValue)
            : base(column, isNull, binaryValue, true, 8, 8, AllowableType) { }

        /// <summary>
        /// The value for the specific row and column. A <see cref="decimal" />.
        /// </summary>
        public override decimal? Value => IsNull ? null : ConversionFunctions.AsCurrency(BinaryValue.AsSpan());
    }
}