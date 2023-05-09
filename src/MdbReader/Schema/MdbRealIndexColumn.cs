// This file is part of MdbReader. Licensed under the LGPL version 2.0.
// You should have received a coy of the GNU LGPL version along with this
// program. If not, see https://www.gnu.org/licenses/old-licenses/lgpl-2.0.html
//
// Copyright Micah Makaiwi.
// Based on code from libmdb (https://github.com/mdbtools/mdbtools)

namespace MMKiwi.MdbReader.Schema;

internal class MdbRealIndexColumn
{
    public MdbRealIndexColumn(int colNum, byte colOrder)
    {
        ColNum = colNum;
        ColOrder = colOrder;
    }

    /// <summary>
    /// The mutable builder for a future MdbRealIndexColumn class.
    /// </summary>
    internal class Builder
    {
        public ushort ColNum { get; set; }
        public byte ColOrder { get; set; }

        public MdbRealIndexColumn Build()
             => new MdbRealIndexColumn(
                 colNum: ColNum,
                 colOrder: ColOrder);
    }
    public int ColNum { get; }
    public byte ColOrder { get;  }
}
