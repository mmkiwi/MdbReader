// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.
//
// Based on code from libmdb (https://github.com/mdbtools/mdbtools)

using MMKiwi.MdbTools;

await using MdbHandle handle = MdbHandle.Open(args[0]);
var table = await handle.GetTablesAsync();
foreach(var col in table.First().Columns)
{
    Console.WriteLine(col.Name);
}
/*
using OleDbConnection connection = new(@"provider=Microsoft.Jet.OLEDB.4.0;Data Source=C:\Temp\mdb\test.mdb");

OleDbCommand permCommand = new("GRANT SELECT ON MSysObjects TO Admin;", connection);
await connection.OpenAsync();

//await permCommand.ExecuteNonQueryAsync();

OleDbCommand command = new("SELECT * from MSysObjects ", connection);


DbDataReader reader = await command.ExecuteReaderAsync();

// Set the Connection to the new OleDbConnection.

Stream stdout = Console.OpenStandardOutput();
Utf8JsonWriter w = new(stdout, new JsonWriterOptions
{
    Indented = true
});
w.WriteStartArray();

while (reader.Read())
{
    w.WriteStartObject();
    for (int i = 0; i < reader.FieldCount; i++)
    {
        w.WriteString(reader.GetName(i), reader.GetValue(i)?.ToString());
    }
    w.WriteEndObject();
}
w.WriteEndArray();
await w.FlushAsync();
*/