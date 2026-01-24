// This file is part of MdbReader. Licensed under the LGPL version 2.0.
// You should have received a coy of the GNU LGPL version along with this
// program. If not, see https://www.gnu.org/licenses/old-licenses/lgpl-2.0.html
// 
// Copyright Micah Makaiwi.
// Based on code from libmdb (https://github.com/mdbtools/mdbtools)

using System.Runtime.CompilerServices;

namespace MMKiwi.MdbReader.Helpers;

internal readonly struct MdbBitArray
{
    public int Length { get; }

    private readonly ReadOnlyMemory<byte> _bytes;

    public MdbBitArray(ReadOnlyMemory<byte> bytes)
    {
        int length = bytes.Length;
        while (bytes.Span[length - 1] == 0 && length > 0)
        {
            length--;
        }

        Length = length * 8;
        _bytes = bytes;
    }

    public bool this[int index]
    {
        get => Get(index);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool Get(int index)
    {
        if ((uint)index >= (uint)Length)
            return false;

        return (_bytes.Span[index >> 3] & (1 << (index % 8))) != 0;
    }
}
