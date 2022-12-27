// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.
//
// Based on code from libmdb (https://github.com/mdbtools/mdbtools)

using System.Text;

namespace MMKiwi.MdbReader;

/// <summary>
/// Additional column information for textual columns <see cref="MdbColumnType.Text" /> and 
/// <see cref="MdbColumnType.Memo" /> 
/// </summary>
public sealed record class MdbTextColumnInfo : MdbMiscColumnInfo
{
    internal MdbTextColumnInfo(Jet3Reader reader, ushort collation, Encoding? overrideEncoding)
    {
        Collation = collation;
        Encoding = overrideEncoding ?? reader.Db.Encoding;
    }

    internal MdbTextColumnInfo(Jet3Reader reader, ushort collation, ushort codePage)
    {
        Collation = collation;
        var encodings = Encoding.GetEncodings();
        Encoding = encodings.FirstOrDefault(e => e.CodePage == codePage)?.GetEncoding() ?? reader.Db.Encoding;
    }

    internal MdbTextColumnInfo(ushort collation, Encoding encoding)
    {
        Collation = collation;
        Encoding = encoding;
    }

    /// <summary>
    /// The collation code for the column. This is based on an LCID (localization id). For instance, English-US is 1033.
    /// </summary>
    /// <remarks>
    /// See <see href="https://learn.microsoft.com/en-us/previous-versions/windows/embedded/ms912047(v=winembedded.10)" />
    /// for a list of locale IDs
    /// </remarks>
    public ushort Collation { get; }

    /// <summary>
    /// The encoding of the column
    /// </summary>
    public Encoding Encoding { get; }

    private protected override IEnumerable<MdbColumnType> ColumnTypes => new MdbColumnType[] { MdbColumnType.Text, MdbColumnType.Memo };
}
