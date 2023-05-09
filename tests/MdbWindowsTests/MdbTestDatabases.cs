// This file is part of MdbReader. Licensed under the LGPL version 2.0.
// You should have received a coy of the GNU LGPL version along with this
// program. If not, see https://www.gnu.org/licenses/old-licenses/lgpl-2.0.html
//
// Copyright Micah Makaiwi.
// Based on code from libmdb (https://github.com/mdbtools/mdbtools)

using System.Collections;
using System.Text.Json;
using System.Text.Json.Serialization;


namespace MMKiwi.MdbReader.WindowsTests;


public class MdbTestDatabases : IEnumerable<object[]>
{
    public IEnumerator<object[]> GetEnumerator()
    {
        foreach (var dbFile in Directory.GetFiles("Databases", "*.mdb").Union(Directory.GetFiles("Databases", "*.accdb")))
        {
            
            string? schemaFilePath = dbFile + ".schema.json";
            if (File.Exists(schemaFilePath))
            {
                Dictionary<string, string[]>? tableSchemas;

                using (FileStream schemaStream = File.OpenRead(schemaFilePath))
                {
                    tableSchemas = JsonSerializer.Deserialize(schemaStream, JsonContext.Default.DictionaryStringStringArray);
                }
                if (tableSchemas == null) continue;
                foreach ((string tableName, string[] primaryKeys) in tableSchemas)
                {
                    yield return new object[] { dbFile, tableName, primaryKeys };
                }
            }
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(Dictionary<string, string[]>))]
public sealed partial class JsonContext : JsonSerializerContext
{ }
