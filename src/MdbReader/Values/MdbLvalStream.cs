// This file is part of MdbReader. Licensed under the LGPL version 2.0.
// You should have received a coy of the GNU LGPL version along with this
// program. If not, see https://www.gnu.org/licenses/old-licenses/lgpl-2.0.html
//
// Copyright Micah Makaiwi.
// Based on code from libmdb (https://github.com/mdbtools/mdbtools)

namespace MMKiwi.MdbReader.Values;

/// <summary>
/// This stream allows for getting the binary value for Access types that can 
/// span more than one page (<see cref="MdbOleValue" /> and <see cref="MdbMemoValue" />).
/// </summary>
/// <remarks>
/// Ensure that the <see cref="MdbConnection" /> that the field value was originally 
/// created with has not been disposed yet. The stream maintains a reference to the handle
/// and uses its stream to read the file.
/// </remarks>
public class MdbLValStream : Stream
{
    internal MdbLValStream(Jet3Reader.LvalStream lvalStream)
    {
        LvalStream = lvalStream;
        Length = LvalStream.Length;
        Buffer = new byte[Math.Min(Length, lvalStream.Reader.PageSize)];
    }

    private Jet3Reader.LvalStream LvalStream { get; }

    /// <summary>
    /// The stream is readable, returns true.
    /// </summary>
    public override bool CanRead => true;
    /// <summary>
    /// The stream is not seekable, returns false.
    /// </summary>
    public override bool CanSeek => false;
    /// <summary>
    /// The stream cannot timeout, returns false
    /// </summary>
    public override bool CanTimeout => false;
    /// <summary>
    /// The stream is not writable, returns false
    /// </summary>
    public override bool CanWrite => false;
    /// <summary>
    /// The length of the value in bytes.
    /// </summary>
    public override long Length { get; }

    private byte[] Buffer { get; }
    private int _position = 0;
    private int _bufferStartPos;

    /// <summary>
    /// Gets the position of the stream. Calling set will throw a <see cref="NotSupportedException" />. Call 
    /// <see cref="Reset" /> to reset the stream to position zero.
    /// </summary>
    public override long Position { get => _position; set => throw new NotSupportedException(); }
    private int _lengthInBuffer;

    /// <summary>
    /// This stream is not flushable, calling this method does nothing.
    /// </summary>
    public override void Flush() { }

    /// <summary>
    /// Reads a sequence of bytes from the stream and advances the position in the stream by the number of bytes read.
    /// </summary>
    /// <param name="buffer">An array of bytes. When this method returns, the buffer contains the specified byte array with the values between offset and (offset + count - 1) replaced by the bytes read from the current source.</param>
    /// <param name="offset">The zero-based byte offset in <c>buffer</c> at which to begin storing the data read from the current stream.</param>
    /// <param name="count">The maximum number of bytes to be read from the current stream.</param>
    /// <returns>The total number of bytes read into the buffer. This can be less than the number of bytes requested if that many bytes are not currently available, or zero (0) if the end of the stream has been reached.</returns>
    /// <throws cref="ArgumentException">The sum of offset and count is larger than the buffer length.</throws>
    /// <throws cref="ArgumentNullException"><c>buffer</c> is <c>null</c></throws>
    /// <throws cref="ArgumentOutOfRangeException"><c>offset</c> or <c>count</c> is negative.</throws>
    /// <throws cref="ObjectDisposedException">Methods were called after the <see cref="MdbConnection" /> was closed.</throws>
    public override int Read(byte[] buffer, int offset, int count)
    {
        if (buffer is null)
            throw new ArgumentNullException(nameof(buffer));
        if (count > buffer.Length)
            throw new ArgumentException("Count exceeds buffer length");
        if (offset < 0)
            throw new ArgumentOutOfRangeException(nameof(offset), "offset must be positive");
        if (count < 0)
            throw new ArgumentOutOfRangeException(nameof(count), "count must be positive");

        return Read(buffer.AsSpan(offset, count));
    }

    /// <summary>
    /// Reads a sequence of bytes from the stream and advances the position in the stream by the number of bytes read.
    /// </summary>
    /// <param name="buffer">A region of memory. When this method returns, the contents of this region are replaced by the bytes read from the current source.</param>
    /// <returns>The total number of bytes read into the buffer. This can be less than the number of bytes requested if that many bytes are not currently available, or zero (0) if the end of the stream has been reached.</returns>
    /// <throws cref="ObjectDisposedException">Methods were called after the <see cref="MdbConnection" /> was closed.</throws>
    public override int Read(Span<byte> buffer)
    {
        int numSent = 0;
        while (numSent < buffer.Length && numSent < Length)
        {
            if (_position >= Length) // At the end
                return 0;
            if (_lengthInBuffer == 0)
            {
                // First run, lets grab the first portion
                _lengthInBuffer = LvalStream.ReadNextData(Buffer);
            }

            int lengthLeft = _lengthInBuffer - (_position - _bufferStartPos);
            if (lengthLeft < 0)
            {
                // Grab the next page
                _bufferStartPos = _position;
                _lengthInBuffer = LvalStream.ReadNextData(Buffer);
                lengthLeft = _lengthInBuffer - (_position - _bufferStartPos);
            }
            int numToSend = Math.Min(buffer.Length - numSent, lengthLeft);
            Buffer.AsSpan(_position - _bufferStartPos, numToSend).CopyTo(buffer);
            numSent += numToSend;
            _position += numSent;
        }

        return numSent;
    }

    /// <summary>
    /// Throws a <see cref="NotSupportedException" />, since seeking is not supported.
    /// </summary>
    /// <param name="offset">unused</param>
    /// <param name="origin">unused</param>
    /// <throws cref="NotSupportedException">Always thrown</throws>
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

    /// <summary>
    /// Throws a <see cref="NotSupportedException" />, since setting the length is not supported.
    /// </summary>
    /// <param name="value">unused</param>
    /// <throws cref="NotSupportedException">Always thrown</throws>
    public override void SetLength(long value) => throw new NotSupportedException();

    /// <summary>
    /// Throws a <see cref="NotSupportedException" />, since setting the length is not supported.
    /// </summary>
    /// <param name="buffer">unused</param>
    /// <param name="offset">unused</param>
    /// <param name="count">unused</param>
    /// <throws cref="NotSupportedException">Always thrown</throws>
    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

    /// <summary>
    /// Returns a byte array with the entire binary value. The buffer must be at the beginning. 
    /// Call <see cref="Reset" /> to reset the position to zero.
    /// </summary>
    /// <returns></returns>
    /// <throws cref="InvalidOperationException">Thrown if a Read() method has already been called.</throws>
    public byte[] ReadToEnd()
    {
        if (Position != 0)
            throw new InvalidOperationException($"Cannot call {nameof(ReadToEnd)} after calling Read methods");
        byte[] result = new byte[Length];
        Read(result.AsSpan());
        return result;
    }

    /// <summary>
    /// Returns a byte array with the entire binary value. The buffer must be at the beginning. 
    /// Call <see cref="Reset" /> to reset the position to zero.
    /// </summary>
    /// <returns></returns>
    /// <throws cref="InvalidOperationException">Thrown if a Read() method has already been called.</throws>
    public Task<byte[]> ReadToEndAsync()
    {
        if (Position != 0)
            throw new InvalidOperationException($"Cannot call {nameof(ReadToEnd)} after calling Read methods");
        byte[] result = new byte[Length];
        return Impl(result);

        async Task<byte[]> Impl(byte[] result)
        {
            await ReadAsync(result.AsMemory()).ConfigureAwait(false);
            return result;
        }
    }

    /// <summary>
    /// Resets the stream position to zero.
    /// </summary>
    public void Reset()
    {
        LvalStream.Reset();
    }
}
