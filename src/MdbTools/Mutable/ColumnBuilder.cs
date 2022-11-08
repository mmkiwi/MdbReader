// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.
//
// Based on code from libmdb (https://github.com/mdbtools/mdbtools)

using System.Text;

namespace MMKiwi.MdbTools.Mutable;

/// <summary>
/// A container class for mutable builders for the publicly visiable, immutable Mdb obejects
/// </summary>
internal static partial class MdbBuilder
{
    /// <summary>
    /// The mutable builder for <see cref="MdbColumn" />
    /// </summary>
    internal class Column
    {
        public Column(Encoding encoding)
        {
            Encoding = encoding;
        }
        public ColumnType Type { get; set; }
        public ushort NumInclDeleted { get; set; }
        public ushort OffsetVariable { get; set; }
        public ushort ColNum { get; set; }
        public ushort SortOrder { get; set; }
        public ushort Locale { get; set; }
        public ColumnFlags Flags { get; set; }
        public ushort OffsetFixed { get; set; }
        public ushort Length { get; set; }
        public string? Name { get; set; }
        public int UsedPages { get; internal set; }
        public int FreePages { get; internal set; }
        public Encoding Encoding { get; }
    }
}