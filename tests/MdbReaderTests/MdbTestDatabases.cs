// This file is part of MdbReader. Licensed under the LGPL version 2.0.
// You should have received a coy of the GNU LGPL version along with this
// program. If not, see https://www.gnu.org/licenses/old-licenses/lgpl-2.0.html
//
// Copyright Micah Makaiwi.
// Based on code from libmdb (https://github.com/mdbtools/mdbtools)

using System.Collections;

namespace MMKiwi.MdbReader.Tests;

public class MdbTestDatabases : IEnumerable<object[]>
{
    public IEnumerator<object[]> GetEnumerator()
    {
        foreach (var dbFile in Directory.GetFiles("Databases", "*.mdb").Union(Directory.GetFiles("Databases", "*.accdb")))
            yield return new object[] { dbFile, $"{dbFile}.json" };
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
