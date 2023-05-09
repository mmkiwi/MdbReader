// This file is part of MdbReader. Licensed under the LGPL version 2.0.
// You should have received a coy of the GNU LGPL version along with this
// program. If not, see https://www.gnu.org/licenses/old-licenses/lgpl-2.0.html
//
// Copyright Micah Makaiwi.
// Based on code from libmdb (https://github.com/mdbtools/mdbtools)

using System.Diagnostics.CodeAnalysis;
using MMKiwi.MdbReader.Schema;

namespace MMKiwi.MdbReader.Values;
/// <summary>
/// The base class for all Mdb{X}Value classes. This class should not be used 
/// directly by end-user code.
/// </summary>
/// <remarks>
/// <para>The following subclasses represent the data types in an Access database:</para>
/// <list type="table">
/// <listheader>
/// <term>MdbValue subclass</term>
/// <term><see cref="MdbColumnType" /></term>
/// <term>.NET object type (TVal)</term>
/// <term>Seperate Nullable Type</term>
/// <term>Access Column Type, Size</term>
/// <term>SQL type name</term>
/// </listheader>
/// <item>
/// <term><see cref="MdbBoolValue" /></term>
/// <term><see cref="MdbColumnType.Boolean" /></term>
/// <term><see cref="bool" /></term>
/// <term>No</term>
/// <term>Yes/No</term>
/// <term><c>BIT</c></term>
/// </item>
/// <item>
/// <term><see cref="MdbByteValue" /></term>
/// <term><see cref="MdbColumnType.Byte" /></term>
/// <term><see cref="byte" /></term>
/// <term>yes</term>
/// <term>Number, Byte</term>
/// <term><c>BYTE</c></term>
/// </item>
/// <item>
/// <term><see cref="MdbIntValue" /></term>
/// <term><see cref="MdbColumnType.Int" /></term>
/// <term><see cref="short" /></term>
/// <term>Yes</term>
/// <term>Number, Integer</term>
/// <term><c>INT</c></term>
/// </item>
/// <item>
/// <term><see cref="MdbLongIntValue" /></term>
/// <term><see cref="MdbColumnType.LongInt" /></term>
/// <term><see cref="int" /></term>
/// <term>Yes</term>
/// <term>Number, Long Integer</term>
/// <term><c>LONG</c></term>
/// </item>
/// <item>
/// <term><see cref="MdbSingleValue" /></term>
/// <term><see cref="MdbColumnType.Single" /></term>
/// <term><see cref="float" /></term>
/// <term>Yes</term>
/// <term>Number, Single</term>
/// <term><c>SINGLE</c></term>
/// </item>
/// <item>
/// <term><see cref="MdbDoubleValue" /></term>
/// <term><see cref="MdbColumnType.Double" /></term>
/// <term><see cref="float" /></term>
/// <term>Yes</term>
/// <term>Number, Double</term>
/// <term><c>DOUBLE</c></term>
/// </item>
/// <item>
/// <term><see cref="MdbGuidValue" /></term>
/// <term><see cref="MdbColumnType.Guid" /></term>
/// <term><see cref="Guid" /></term>
/// <term>Yes</term>
/// <term>Number, Replication ID</term>
/// <term><c>GUID</c></term>
/// </item>
/// <item>
/// <term><see cref="MdbCurrencyValue" /></term>
/// <term><see cref="MdbColumnType.Currency" /></term>
/// <term><see cref="decimal" /></term>
/// <term>Yes</term>
/// <term>Currency</term>
/// <term><c>CURRENCY</c></term>
/// </item>
/// <item>
/// <term><see cref="MdbDateTimeValue" /></term>
/// <term><see cref="MdbColumnType.DateTime" /></term>
/// <term><see cref="DateTime" /></term>
/// <term>Yes</term>
/// <term>Date/Time</term>
/// <term><c>DATETIME</c></term>
/// </item>
/// <item>
/// <term><see cref="MdbStringValue" /></term>
/// <term><see cref="MdbColumnType.Text" /></term>
/// <term><see cref="string" /> (also available as a 
///   <see cref="MdbStringValue.RawValue">raw byte value</see>)</term>
/// <term>Yes</term>
/// <term>N/A</term>
/// <term><c>CHAR</c> and <c>VARCHAR</c></term>
/// </item>
/// <item>
/// <term><see cref="MdbBinaryValue" /></term>
/// <term><see cref="MdbColumnType.Binary" /></term>
/// <term><see cref="ImmutableArray{T}" /> of <see cref="byte" /></term>
/// <term>Yes</term>
/// <term>N/A</term>
/// <term><c>BINARY</c> and <c>VARBINARY</c></term>
/// </item>
/// <item>
/// <term><see cref="MdbMemoValue" /></term>
/// <term><see cref="MdbColumnType.Memo" /></term>
/// <term><see cref="StreamReader" /></term>
/// <term>No</term>
/// <term>Memo (or Long Text)</term>
/// <term><c>LONGTEXT</c></term>
/// </item>
/// <item>
/// <term><see cref="MdbOleValue" /></term>
/// <term><see cref="MdbColumnType.OLE" /></term>
/// <term><see cref="MdbLValStream" /></term>
/// <term>No</term>
/// <term>OLE Object</term>
/// <term><c>LONGBINARY</c></term>
/// </item>
/// </list>
/// <para>* Data types marked as nullable include a sub-class named "Nullable" that outputs a
/// a nullable <see cref="Value" />. See each individual type for any special notes on use.
/// </para>
/// </remarks>
/// <typeparam name="TVal">The type returned by <see cref="Value" /></typeparam>
internal abstract class MdbValue<TVal> : IMdbValue<TVal>
{
    /// <summary>
    /// Construct a new MdbField instance
    /// </summary>
    /// <param name="column">The MdbColumn that the value is being generated for</param>
    /// <param name="isNull">Sets whether the value is null or not</param>
    /// <param name="binaryValue">The byte array of the value</param>
    /// <param name="minLength">The minimum length that binaryValue should be.</param>
    /// <param name="maxLength">The maximum length that binaryValue should be.</param>
    /// <param name="allowableType">
    ///   A list of allowable column types for this value. If the <see cref="MdbColumn.Type" /> field of the column
    ///   parameter is not in this, an ArgumentException will be thrown.
    /// </param>
    /// <exception cref="ArgumentNullException">
    ///   Thrown if column or binaryValue is null. (If a zero-length binaryValue is needed, pass 
    ///   <see cref="ImmutableArray{T}.Empty" />
    /// </exception>
    /// <exception cref="ArgumentException">
    /// <para>
    /// Thrown if:
    /// </para>
    /// <list>
    /// <item><description>binaryValue's Length is not between minLength and maxLength (inclusive)</description></item>
    /// <item><description>The <see cref="MdbColumn.Type"/> value for the column parameter is not in allowableTypes</description></item>
    /// <item><description>isNull is true but allowNull is false</description></item>
    /// </list>
    /// </exception>
    private protected MdbValue(MdbColumn column, bool isNull, ImmutableArray<byte> binaryValue, int minLength, int maxLength, MdbColumnType allowableType)
    {
        if (column is null)
            throw new ArgumentNullException(nameof(column));

        if (!isNull && binaryValue.Length < minLength || binaryValue.Length > maxLength)
            throw new ArgumentException($"Invalid length {binaryValue.Length}.", nameof(binaryValue));
        if (allowableType != column.Type)
            throw new ArgumentException($"Could not convert field value type {column.Type} to {allowableType}", nameof(column));

        Column = column;
        IsNull = isNull;
        BinaryValue = isNull ? ImmutableArray<byte>.Empty : binaryValue;
    }

    /// <summary>
    /// The column that the specified value is for.
    /// </summary>
    public MdbColumn Column { get; }

    /// <summary>
    /// True if the specified value is null.
    /// </summary>
    [MemberNotNullWhen(false, nameof(Value))]
    public bool IsNull { get; }

    private protected ImmutableArray<byte> BinaryValue { get; }

    /// <summary>
    /// The value for the specific row and column, converted from the raw 
    /// binary data.
    /// </summary>
    public abstract TVal Value { get; }

    object? IMdbValue.Value => Value;

    /// <inheritdoc />
    public override string ToString()
    {
        return Value?.ToString() ?? String.Empty;
    }

    /// <summary>
    /// Implicitly convert this object to its underlying value.
    /// </summary>
    /// <param name="v">The value to convert.</param>
    public static implicit operator TVal(MdbValue<TVal> v) => v.Value;

}
