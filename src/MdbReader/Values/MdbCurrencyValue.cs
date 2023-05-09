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
/// A database value corresponding to an Access <see cref="MdbColumnType.Currency"/>. 
/// </summary>
/// <remarks>
/// <para>
/// This is a 64-bit signed integer in the database, multiplied by 10,000 (e.g. $182.60 is
/// stored as 1,826,000). <see cref="Value" /> returns a <see cref="decimal" /> (in the 
/// example above, <c>183.60M</c>)
/// Referred to in the Access GUI as a Currency column with the Integer size, and as an <c>CURRENCY</c> in SQL.
/// </para>
/// </remarks>
[DebuggerDisplay("{Column.Name}: {Value}")]
internal sealed class MdbCurrencyValue : MdbValue<decimal>, IValueAllowableType
{
    internal MdbCurrencyValue(MdbColumn column, bool isNull, ImmutableArray<byte> binaryValue)
        : base(column, isNull, binaryValue, 8, 8, AllowableType) { }

    /// <summary>
    /// The <see cref="MdbColumnType" /> that can be used for this value.
    /// This will always be <see cref="MdbColumnType.Currency" />
    /// </summary>
    public static MdbColumnType AllowableType => MdbColumnType.Currency;

    /// <summary>
    /// The value for the specific row and column. A <see cref="decimal" />.
    /// </summary>
    public override decimal Value => ConversionFunctions.AsCurrency(BinaryValue.AsSpan());
}