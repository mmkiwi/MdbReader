// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.
//
// Based on code from libmdb (https://github.com/mdbtools/mdbtools)

namespace MMKiwi.MdbReader;

/// <summary>
/// Additional column information fields for decimal columns.
/// </summary>
/// <remarks>
/// Decimal columns are only supported in Access 2000 and later.
/// </remarks>
public sealed record class MdbDecimalColumnInfo : MdbMiscColumnInfo
{
    internal MdbDecimalColumnInfo(byte maxDigits, byte decimalDigits)
    {
        MaxDigits = maxDigits;
        DecimalDigits = decimalDigits;
    }

    /// <summary>
    /// The maximum number of decimal digits for the decimal column
    /// </summary>
    public byte MaxDigits { get; }

    /// <summary>
    /// The maximum number of decimal digits after the decimal point for the column
    /// </summary>
    public byte DecimalDigits { get; }

    private protected override IEnumerable<MdbColumnType> ColumnTypes => new MdbColumnType[] {
        MdbColumnType.Boolean,
        MdbColumnType.Byte,
        MdbColumnType.Int, 
        MdbColumnType.LongInt, 
        MdbColumnType.Currency, 
        MdbColumnType.Single,
        MdbColumnType.Double,
        MdbColumnType.Binary,
        MdbColumnType.Numeric };

}
