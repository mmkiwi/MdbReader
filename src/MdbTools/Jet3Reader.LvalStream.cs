// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.
//
// Based on code from libmdb (https://github.com/mdbtools/mdbtools)

using System.Collections.Immutable;

using MMKiwi.MdbTools.Helpers;

namespace MMKiwi.MdbTools;

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
            LvalType = (LVALType)FirstDataPointer[3];
            if (!LvalType.HasFlag(LVALType.Inline)) // Memo is stored on Lval Page
            {
                LvalPointer = MdbBinary.ReadInt32LittleEndian(FirstDataPointer.AsSpan().Slice(4, 4));
            }
        }
        private Jet3Reader Reader { get; }
        public MdbColumn Column { get; }
        private ImmutableArray<byte> FirstDataPointer { get; }

        public int Length { get; }
        public LVALType LvalType { get; }
        private int BytesRead { get; set; }
        private int LvalPointer { get; set; }

        public int ReadNextData(Span<byte> buffer)
        {
            if (Reader.IsDisposed)
                throw new ObjectDisposedException($"{nameof(MdbHandle)} has already been disposed");

            if (BytesRead >= Length)
                return 0;

            if (LvalType.HasFlag(LVALType.Inline)) // 0x80 == 1
            {
                FirstDataPointer.AsSpan().Slice(12, Length).CopyTo(buffer);
                BytesRead += Length;
                return Length;
            }
            else  // Memo is stored on Lval Page
            {
                int pageNo = LvalPointer >> 8;
                int rowNo = LvalPointer & 0xff;

                if (LvalType.HasFlag(LVALType.LvalPageType1)) // 0x40 == 1
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
            if (!LvalType.HasFlag(LVALType.Inline)) // Memo is stored on Lval Page
            {
                LvalPointer = MdbBinary.ReadInt32LittleEndian(FirstDataPointer.AsSpan().Slice(4, 4));
            }
            BytesRead = 0;
        }
    }
}

