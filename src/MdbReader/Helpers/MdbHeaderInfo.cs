// This file is part of MdbReader. Licensed under the LGPL version 2.0.
// You should have received a coy of the GNU LGPL version along with this
// program. If not, see https://www.gnu.org/licenses/old-licenses/lgpl-2.0.html
//
// Copyright Micah Makaiwi.
// Based on code from libmdb (https://github.com/mdbtools/mdbtools)

namespace MMKiwi.MdbReader.Helpers;

internal record MdbHeaderInfo
{
    public MdbHeaderInfo(JetVersion jetVersion, ushort collation, ushort codePage, int dbKey, DateTime creationDate, JetConstants constants)
    {
        JetVersion = jetVersion;
        Collation = collation;

        RegisterEncoding();
        if (JetVersion == JetVersion.Jet3)
            Encoding = Encoding.GetEncoding(codePage);
        else
            Encoding = Jet4CompressedString.GetForEncoding(Encoding.GetEncoding(codePage));

        DbKey = dbKey;
        CreationDate = creationDate;

        Constants = constants;
    }

    static void RegisterEncoding()
    {
        if (!s_isRegistered)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            s_isRegistered = true;
        }
    }

    static bool s_isRegistered = false;
    public JetVersion JetVersion { get; }
    public ushort Collation { get; }
    public Encoding Encoding { get; }
    public int DbKey { get; }
    public DateTime CreationDate { get; }
    public JetConstants Constants { get; }
}
