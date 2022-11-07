// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.
//
// Based on code from libmdb (https://github.com/mdbtools/mdbtools)

using System.Text;

namespace MMKiwi.MdbTools.Fields;


public class MdbLongValStream : Stream
{
    internal MdbLongValStream(Jet3Reader.LvalStream lvalStream)
    {
        LvalStream = lvalStream;
        Length = LvalStream.Length;
        Buffer = new byte[Math.Min(Length, Jet3Reader.Constants.PageSize)];
    }

    private Jet3Reader.LvalStream LvalStream { get; }

    public override bool CanRead => true;
    public override bool CanSeek => false;

    public override bool CanTimeout => false;
    public override bool CanWrite => false;
    public override long Length { get; }
    public byte[] Buffer { get; }

    private int _position = 0;
    private int _bufferStartPos;
    public override long Position { get => _position; set => throw new NotSupportedException(); }
    private int _lengthInBuffer;

    public override void Flush() => throw new NotSupportedException();

    public override int Read(byte[] buffer, int offset, int count) => Read(buffer.AsSpan(offset, count));

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
            int numToSend = Math.Min(buffer.Length, lengthLeft);
            Buffer.AsSpan(_position - _bufferStartPos, numToSend).CopyTo(buffer);
            numSent += numToSend;
            _position += numSent;
        }

        return numSent;
    }

    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

    public override void SetLength(long value) => throw new NotSupportedException();

    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();


    public byte[] ReadToEnd()
    {
        byte[] result = new byte[Length];
        Read(result.AsSpan());
        return result;
    }
}
