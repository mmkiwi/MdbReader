// This file is part of MdbReader. Licensed under the LGPL version 2.0.
// You should have received a coy of the GNU LGPL version along with this
// program. If not, see https://www.gnu.org/licenses/old-licenses/lgpl-2.0.html
//
// Copyright Micah Makaiwi.
// Based on code from libmdb (https://github.com/mdbtools/mdbtools)

namespace MMKiwi.MdbReader.Values;

internal static class MdbValueFactory
{
    /// <summary>
    /// Create the appropriate <see cref="IMdbValue" /> for a specific <see cref="MdbColumnType" />.
    /// </summary>
    /// <param name="reader">
    /// The <see cref="Jet3Reader" /> instance that is creating this field. (Required for 
    /// <see cref="MdbMemoValue" /> and <see cref="MdbOleValue" />). 
    /// </param>
    /// <param name="column"></param>
    /// <param name="isNull"></param>
    /// <param name="binaryValue"></param>
    /// <returns></returns>
    public static IMdbValue CreateValue(Jet3Reader reader, MdbColumn column, bool isNull, ImmutableArray<byte> binaryValue)
    {
        return (column.Type) switch
        {
            MdbColumnType.Boolean => new MdbBoolValue(column, isNull),
            MdbColumnType.Byte => new MdbByteValue(column, isNull, binaryValue),
            MdbColumnType.Int => new MdbIntValue(column, isNull, binaryValue),
            MdbColumnType.LongInt => new MdbLongIntValue(column, isNull, binaryValue),
            MdbColumnType.Currency => new MdbCurrencyValue(column, isNull, binaryValue),
            MdbColumnType.Single => new MdbSingleValue(column, isNull, binaryValue),
            MdbColumnType.Double => new MdbDoubleValue(column, isNull, binaryValue),
            MdbColumnType.DateTime => new MdbDateTimeValue(column, isNull, binaryValue),
            MdbColumnType.Binary => new MdbBinaryValue(column, isNull, binaryValue),
            MdbColumnType.Text => new MdbStringValue(column, isNull, binaryValue),
            MdbColumnType.OLE => new MdbOleValue(reader, column, isNull, binaryValue),
            MdbColumnType.Memo => new MdbMemoValue(reader, column, isNull, binaryValue),
            MdbColumnType.Guid => new MdbGuidValue(column, isNull, binaryValue),
            _ => throw new NotImplementedException()
        };
    }
}
