// This file is part of MdbReader. Licensed under the LGPL version 2.0.
// You should have received a coy of the GNU LGPL version along with this
// program. If not, see https://www.gnu.org/licenses/old-licenses/lgpl-2.0.html
//
// Copyright Micah Makaiwi.
// Based on code from libmdb (https://github.com/mdbtools/mdbtools)

using MMKiwi.MdbReader.Values;

namespace MMKiwi.MdbReader;

public sealed partial class MdbDataRow
{

    private static byte[] GetByteArray(IMdbValue fieldValue, int maxSize) => fieldValue switch
    {
        MdbBinaryValue binaryValue => binaryValue.Value.Length <= maxSize
            ? binaryValue.Value.ToArray()
            : throw new OverflowException($"Binary size {binaryValue.Value.Length} exceeds {maxSize} bytes"),
        MdbMemoValue memoValue => memoValue.Value!.Length <= maxSize
            ? memoValue.Value!.ReadToEnd()
            : throw new OverflowException($"Binary size {memoValue.Value.Length} exceeds {maxSize} bytes"),
        MdbOleValue oleValue => oleValue.Value!.Length <= maxSize
            ? oleValue.Value!.ReadToEnd()
            : throw new OverflowException($"Binary size {oleValue.Value.Length} exceeds {maxSize} bytes"),
        _ => ThrowInvalidCast<byte[]>(fieldValue, "binary value")
    };

    private static Task<byte[]> GetByteArrayAsync(IMdbValue fieldValue, int maxSize) => fieldValue switch
    {
        MdbBinaryValue binaryValue => binaryValue.Value.Length <= maxSize
            ? Task.FromResult(binaryValue.Value.ToArray())
            : throw new OverflowException($"Binary size {binaryValue.Value.Length} exceeds {maxSize} bytes"),
        MdbMemoValue memoValue => memoValue.Value!.Length <= maxSize
            ? memoValue.Value!.ReadToEndAsync()
            : throw new OverflowException($"Binary size {memoValue.Value.Length} exceeds {maxSize} bytes"),
        MdbOleValue oleValue => oleValue.Value!.Length <= maxSize
            ? oleValue.Value!.ReadToEndAsync()
            : throw new OverflowException($"Binary size {oleValue.Value.Length} exceeds {maxSize} bytes"),
        _ => Task.FromResult(ThrowInvalidCast<byte[]>(fieldValue, "binary value"))
    };

    /// <summary>
    /// Gets the value of the specified column as a byte array.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This method will throw an exception if the value is null or the column type is
    /// not an <see cref="MdbColumnType.Binary" />, <see cref="MdbColumnType.Memo"/>,
    /// or an <see cref="MdbColumnType.OLE" />.
    /// </para>
    /// <para>
    /// Warning, this method can produce very large byte arrays. Set the <c>maxSize</c>
    /// parameter to limit the size of the array returned by this function. Alternatively,
    /// Use <see cref="GetStream(int)" /> to access the binary data using a stream.
    /// This problem is less of an issue for <see cref="MdbColumnType.Binary" /> fields
    /// which are limited to 256 bytes.
    /// </para>
    /// </remarks>
    /// <param name="index">The zero-based column ordinal.</param>
    /// <param name="maxSize">
    /// Optionally, set the maximum number of bytes to be returned. If the stream is 
    /// more than that number of bytes, an <see cref="OverflowException" /> wll be thrown.
    /// </param>
    /// <returns>The value of the specified column.</returns>
    /// <throws cref="IndexOutOfRangeException">The index passed was outside of the range of 0 through <see cref="FieldCount" />.</throws>
    /// <throws cref="InvalidCastException">The specified cast is not valid, or the underlying value is <c>null</c></throws>
    /// <throws cref="OverflowException">The size of the value exceeds <c>maxSize</c></throws>
    public byte[] GetBytes(int index, int maxSize = int.MaxValue)
    {
        var fieldValue = GetFieldValue(index);
        ThrowIfNullCast(fieldValue, "binary value");
        return GetByteArray(fieldValue, maxSize);
    }

    /// <summary>
    /// Gets the value of the specified column as a byte array.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This method will throw an exception if the value is null or the column type is
    /// not an <see cref="MdbColumnType.Binary" />, <see cref="MdbColumnType.Memo"/>,
    /// or an <see cref="MdbColumnType.OLE" />.
    /// </para>
    /// <para>
    /// Warning, this method can produce very large byte arrays. Set the <c>maxSize</c>
    /// parameter to limit the size of the array returned by this function. Alternatively,
    /// Use <see cref="GetStream(string)" /> to access the binary data using a stream.
    /// This problem is less of an issue for <see cref="MdbColumnType.Binary" /> fields
    /// which are limited to 256 bytes.
    /// </para>
    /// </remarks>
    /// <param name="columnName">The column name</param>
    /// <param name="maxSize">
    /// Optionally, set the maximum number of bytes to be returned. If the stream is 
    /// more than that number of bytes, an <see cref="OverflowException" /> wll be thrown.
    /// </param>
    /// <returns>The value of the specified column.</returns>
    /// <throws cref="ArgumentNullException"><c>columnName</c> is <c>null</c>.</throws>
    /// <throws cref="IndexOutOfRangeException">No column with the specified name was found.</throws>
    /// <throws cref="OverflowException">The size of the value exceeds <c>maxSize</c></throws>
    public byte[] GetBytes(string columnName, int maxSize = int.MaxValue)
    {
        var fieldValue = GetFieldValue(columnName);
        ThrowIfNullCast(fieldValue, "binary value");
        return GetByteArray(fieldValue, maxSize);
    }



    /// <summary>
    /// Gets the value of the specified column as a nullable byte array.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This method will throw an exception if the value is null or the column type is
    /// not an <see cref="MdbColumnType.Binary" />, <see cref="MdbColumnType.Memo"/>,
    /// or an <see cref="MdbColumnType.OLE" />.
    /// </para>
    /// <para>
    /// Warning, this method can produce very large byte arrays. Set the <c>maxSize</c>
    /// parameter to limit the size of the array returned by this function. Alternatively,
    /// Use <see cref="GetStream(int)" /> to access the binary data using a stream.
    /// This problem is less of an issue for <see cref="MdbColumnType.Binary" /> fields
    /// which are limited to 256 bytes.
    /// </para>
    /// </remarks>
    /// <param name="index">The zero-based column ordinal.</param>
    /// <param name="maxSize">
    /// Optionally, set the maximum number of bytes to be returned. If the stream is 
    /// more than that number of bytes, an <see cref="OverflowException" /> wll be thrown.
    /// </param>
    /// <returns>The value of the specified column.</returns>
    /// <throws cref="IndexOutOfRangeException">The index passed was outside of the range of 0 through <see cref="FieldCount" />.</throws>
    /// <throws cref="InvalidCastException">The specified cast is not valid, or the underlying value is <c>null</c></throws>
    /// <throws cref="OverflowException">The size of the value exceeds <c>maxSize</c></throws>
    public byte[]? GetNullableBytes(int index, int maxSize = int.MaxValue)
    {
        var fieldValue = GetFieldValue(index);
        return fieldValue == null ? null : GetByteArray(fieldValue, maxSize);
    }

    /// <summary>
    /// Gets the value of the specified column as a nullable byte array.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This method will throw an exception if the value is null or the column type is
    /// not an <see cref="MdbColumnType.Binary" />, <see cref="MdbColumnType.Memo"/>,
    /// or an <see cref="MdbColumnType.OLE" />.
    /// </para>
    /// <para>
    /// Warning, this method can produce very large byte arrays. Set the <c>maxSize</c>
    /// parameter to limit the size of the array returned by this function. Alternatively,
    /// Use <see cref="GetStream(string)" /> to access the binary data using a stream.
    /// This problem is less of an issue for <see cref="MdbColumnType.Binary" /> fields
    /// which are limited to 256 bytes.
    /// </para>
    /// </remarks>
    /// <param name="columnName">The column name</param>
    /// <param name="maxSize">
    /// Optionally, set the maximum number of bytes to be returned. If the stream is 
    /// more than that number of bytes, an <see cref="OverflowException" /> wll be thrown.
    /// </param>
    /// <returns>The value of the specified column.</returns>
    /// <throws cref="ArgumentNullException"><c>columnName</c> is <c>null</c>.</throws>
    /// <throws cref="IndexOutOfRangeException">No column with the specified name was found.</throws>
    /// <throws cref="OverflowException">The size of the value exceeds <c>maxSize</c></throws>
    public byte[]? GetNullableBytes(string columnName, int maxSize = int.MaxValue)
    {
        var fieldValue = GetFieldValue(columnName);
        return fieldValue == null ? null : GetByteArray(fieldValue, maxSize);
    }


    /// <summary>
    /// Gets the value of the specified column as a byte array.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This method will throw an exception if the value is null or the column type is
    /// not an <see cref="MdbColumnType.Binary" />, <see cref="MdbColumnType.Memo"/>,
    /// or an <see cref="MdbColumnType.OLE" />.
    /// </para>
    /// <para>
    /// Warning, this method can produce very large byte arrays. Set the <c>maxSize</c>
    /// parameter to limit the size of the array returned by this function. Alternatively,
    /// Use <see cref="GetStream(int)" /> to access the binary data using a stream.
    /// This problem is less of an issue for <see cref="MdbColumnType.Binary" /> fields
    /// which are limited to 256 bytes.
    /// </para>
    /// </remarks>
    /// <param name="index">The zero-based column ordinal.</param>
    /// <param name="maxSize">
    /// Optionally, set the maximum number of bytes to be returned. If the stream is 
    /// more than that number of bytes, an <see cref="OverflowException" /> wll be thrown.
    /// </param>
    /// <returns>The value of the specified column.</returns>
    /// <throws cref="IndexOutOfRangeException">The index passed was outside of the range of 0 through <see cref="FieldCount" />.</throws>
    /// <throws cref="InvalidCastException">The specified cast is not valid, or the underlying value is <c>null</c></throws>
    /// <throws cref="OverflowException">The size of the value exceeds <c>maxSize</c></throws>
    public Task<byte[]> GetBytesAsync(int index, int maxSize = int.MaxValue)
    {
        var fieldValue = GetFieldValue(index);
        ThrowIfNullCast(fieldValue, "binary value");
        return GetByteArrayAsync(fieldValue, maxSize);
    }

    /// <summary>
    /// Gets the value of the specified column as a byte array.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This method will throw an exception if the value is null or the column type is
    /// not an <see cref="MdbColumnType.Binary" />, <see cref="MdbColumnType.Memo"/>,
    /// or an <see cref="MdbColumnType.OLE" />.
    /// </para>
    /// <para>
    /// Warning, this method can produce very large byte arrays. Set the <c>maxSize</c>
    /// parameter to limit the size of the array returned by this function. Alternatively,
    /// Use <see cref="GetStream(string)" /> to access the binary data using a stream.
    /// This problem is less of an issue for <see cref="MdbColumnType.Binary" /> fields
    /// which are limited to 256 bytes.
    /// </para>
    /// </remarks>
    /// <param name="columnName">The column name</param>
    /// <param name="maxSize">
    /// Optionally, set the maximum number of bytes to be returned. If the stream is 
    /// more than that number of bytes, an <see cref="OverflowException" /> wll be thrown.
    /// </param>
    /// <returns>The value of the specified column.</returns>
    /// <throws cref="ArgumentNullException"><c>columnName</c> is <c>null</c>.</throws>
    /// <throws cref="IndexOutOfRangeException">No column with the specified name was found.</throws>
    /// <throws cref="OverflowException">The size of the value exceeds <c>maxSize</c></throws>
    public Task<byte[]> GetBytesAsync(string columnName, int maxSize = int.MaxValue)
    {
        var fieldValue = GetFieldValue(columnName);
        ThrowIfNullCast(fieldValue, "binary value");
        return GetByteArrayAsync(fieldValue, maxSize);
    }
}

