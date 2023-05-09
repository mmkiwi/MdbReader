// This file is part of MdbReader. Licensed under the LGPL version 2.0.
// You should have received a coy of the GNU LGPL version along with this
// program. If not, see https://www.gnu.org/licenses/old-licenses/lgpl-2.0.html
//
// Copyright Micah Makaiwi.
// Based on code from libmdb (https://github.com/mdbtools/mdbtools)

using MMKiwi.MdbReader.Helpers;

namespace MMKiwi.MdbReader;

internal partial class Jet3Reader
{
    internal class LvalStream
    {
        internal LvalStream(Jet3Reader reader, MdbColumn column, ImmutableArray<byte> firstDataPointer)
        {
            Reader = reader;
            Column = column;
            FirstDataPointer = firstDataPointer;
            // if the memo is null, nothing special is needed
            ReadOnlySpan<byte> binRowRegion = FirstDataPointer.AsSpan();

            //Zero out the two highest bits on the fourth byte of the int. These flags are used below
            Length = unchecked((int)
                  (MdbBinary.ReadUInt32LittleEndian(binRowRegion)
                & (BitConverter.IsLittleEndian ? 0b00111111_11111111_11111111_11111111
                                               : 0b11111111_11111111_11111111_00111111)));
            LvalType = (MdbLvalType)FirstDataPointer[3];
            if (!LvalType.HasFlag(MdbLvalType.Inline)) // Memo is stored on Lval Page
            {
                LvalPointer = MdbBinary.ReadInt32LittleEndian(FirstDataPointer.AsSpan().Slice(4, 4));
            }
        }
        internal Jet3Reader Reader { get; }
        public MdbColumn Column { get; }
        private ImmutableArray<byte> FirstDataPointer { get; }

        public int Length { get; }
        public MdbLvalType LvalType { get; }
        private int BytesRead { get; set; }
        private int LvalPointer { get; set; }

        public int ReadNextData(Span<byte> buffer)
        {
            if (Reader.IsDisposed)
                throw new ObjectDisposedException($"{nameof(MdbConnection)} has already been disposed");

            if (BytesRead >= Length)
                return 0;

            if (LvalType.HasFlag(MdbLvalType.Inline)) // 0x80 == 1
            {
                FirstDataPointer.AsSpan().Slice(12, Length).CopyTo(buffer);
                BytesRead += Length;
                return Length;
            }
            else  // Memo is stored on Lval Page
            {
                int pageNo = LvalPointer >> 8;
                int rowNo = LvalPointer & 0xff;

                if (LvalType.HasFlag(MdbLvalType.LvalPageType1)) // 0x40 == 1
                {
                    var lengthInBuffer = Reader.ReadRowFromLvalPage(pageNo, rowNo, buffer);
                    BytesRead += lengthInBuffer;
                    return lengthInBuffer;
                }
                else
                {
                    (var lengthInBuffer, LvalPointer) = Reader.ReadRowFromLvalPage2(pageNo, rowNo, buffer);
                    BytesRead += lengthInBuffer;
                    return lengthInBuffer;
                }
            }
        }

        internal void Reset()
        {
            if (!LvalType.HasFlag(MdbLvalType.Inline)) // Memo is stored on Lval Page
            {
                LvalPointer = MdbBinary.ReadInt32LittleEndian(FirstDataPointer.AsSpan().Slice(4, 4));
            }
            BytesRead = 0;
        }
    }
}

