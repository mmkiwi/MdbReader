// This file is part of MdbReader. Licensed under the LGPL version 2.0.
// You should have received a coy of the GNU LGPL version along with this
// program. If not, see https://www.gnu.org/licenses/old-licenses/lgpl-2.0.html
//
// Copyright Micah Makaiwi.
// Based on code from libmdb (https://github.com/mdbtools/mdbtools)

using System.Text;

using FluentAssertions;

using MMKiwi.MdbReader.Helpers;

namespace MMKiwi.MdbReader.Tests;
public sealed class CompressedUnicodeText
{
    [Fact]
    public void ReadMixedEncodings()
    {
        byte[] compressed =
            {
                0xff, 0xfe, 0x4c, 0xf3, 0x72, 0x65, 0x6d, 0x20,
                0xec, 0x70, 0x73, 0x75, 0x6d, 0x20, 0x64, 0xf2,
                0x6c, 0x6f, 0x72, 0x20, 0x73, 0x69, 0x74, 0x20,
                0x61, 0x6d, 0x65, 0x74, 0x2e, 0x00, 0x34, 0x04,
                0x00, 0x54, 0x65, 0x73, 0x74
            };

        Jet4CompressedString.GetForEncoding(Encoding1252).GetString(compressed).Should().Be("Lórem ìpsum dòlor sit amet.дTest");
    }

    static Encoding Encoding1252
    {
        get
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            return Encoding.GetEncoding(1252);
        }
    }
}
