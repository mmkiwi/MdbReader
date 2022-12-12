// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.
//
// Based on code from libmdb (https://github.com/mdbtools/mdbtools)

using System.Text;

namespace MMKiwi.MdbTools.Helpers;

internal record MdbHeaderInfo
{
    public MdbHeaderInfo(JetVersion jetVersion, ushort collation, ushort codePage, uint dbKey, DateTime creationDate, JetConstants constants)
    {
        JetVersion = jetVersion;
        Collation = collation;

        RegisterEncoding();
        Encoding = Encoding.GetEncoding(codePage);

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
    public uint DbKey { get; }
    public DateTime CreationDate { get; }
    public JetConstants Constants { get; }
}
