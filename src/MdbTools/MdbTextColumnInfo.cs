// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.
//
// Based on code from libmdb (https://github.com/mdbtools/mdbtools)

using System.Text;

namespace MMKiwi.MdbTools;

public record class MdbTextColumnInfo : IMdbMiscColumnInfo
{
    internal MdbTextColumnInfo(Jet3Reader reader, ushort collation)
    {
        Collation = collation;
        Encoding = reader.Db.Encoding;
    }

    internal MdbTextColumnInfo(Jet3Reader reader, ushort collation, ushort codePage)
    {
        Collation = collation;
        var encodings = Encoding.GetEncodings();
        Encoding = encodings.FirstOrDefault(e => e.CodePage == codePage)?.GetEncoding() ?? reader.Db.Encoding;
    }

    public MdbTextColumnInfo(ushort collation, Encoding encoding)
    {
        Collation = collation;
        Encoding = encoding;
    }

    public ushort Collation { get; }
    public Encoding Encoding { get; }
}
