// This file is part of MdbReader. Licensed under the LGPL version 2.0.
// You should have received a coy of the GNU LGPL version along with this
// program. If not, see https://www.gnu.org/licenses/old-licenses/lgpl-2.0.html
//
// Copyright Micah Makaiwi.
// Based on code from libmdb (https://github.com/mdbtools/mdbtools)

using System.Collections.Immutable;
using System.Text.Json;
using System.Text.Json.Serialization;
using MMKiwi.MdbReader;
using MMKiwi.MdbReader.JsonModel;
using System.Diagnostics;
using MMKiwi.MdbReader.Values;

if (args.Length == 0)
{
    Console.WriteLine("Please enter a file path");
    return -1;
}
using MdbConnection handle = MdbConnection.Open(args[0]);
var tables = handle.Tables;
Dictionary<string, MdbJsonTable> outTables = new(tables.Count);

IEnumerable<string> tableNames;
if (args.Length > 1)
    tableNames = args[1..];
else
    tableNames = tables.Keys;

foreach (var tableName in tableNames)
{
    try
    {
        var table = tables[tableName];
        Dictionary<string, MdbColumnType> outColumns = new(table.Columns.Length);
        foreach (var column in table.Columns)
        {
            outColumns[column.Name] = column.Type;
        }
        List<Dictionary<string, JsonElement>> outRows = new(table.NumRows);
        foreach (var row in table.Rows)
        {
            Dictionary<string, JsonElement> outRow = new();
            for (int i = 0; i < row.FieldCount; i++)
            {
                string colName = row.GetName(i);
                object? outRaw = row.GetColumnType(i) switch
                {
                    MdbColumnType.OLE => ReadOle(row.GetStream(i)),
                    MdbColumnType.Memo => ReadMemo(row.GetStreamReader(i)),
                    MdbColumnType.Binary => row.IsNull(i) ? null : Convert.ToBase64String(row.GetBytes(i)),
                    _ => row.GetValue(i)
                };
                var outValue = JsonSerializer.SerializeToElement(outRaw);
                outRow[colName] = outValue;
            }
            outRows.Add(outRow);
        }

        outTables[table.Name] = new MdbJsonTable(outColumns.ToImmutableDictionary(),
                                outRows.Select(d => d.ToImmutableDictionary()).ToImmutableArray());
    }
    catch (Exception ex)
    {
        Debug.WriteLine(ex);
        continue;
    }
}

string? ReadOle(MdbLValStream? fOle)
{
    if (fOle == null)
        return null;
    using (fOle)
    {
        return Convert.ToBase64String(fOle.ReadToEnd());
    }
}


string? ReadMemo(StreamReader? fMemo)
{
    if (fMemo == null)
        return null;
    using (fMemo)
    {
        return fMemo.ReadToEnd();
    }
}

var options = new JsonSerializerOptions
{
    WriteIndented = true,
    TypeInfoResolver = MdbJsonContext.Default,
    Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
};

using var jsonFile = File.Open($"{args[0]}.json", FileMode.Create, FileAccess.Write);
JsonSerializer.Serialize(jsonFile, new MdbJsonDatabase(outTables.ToImmutableDictionary(), handle.JetVersion, handle.DbKey, handle.CreationDate, handle.Encoding.CodePage, handle.Collation), options);

return 0;