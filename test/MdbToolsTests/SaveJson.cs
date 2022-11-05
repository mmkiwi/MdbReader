using System.Collections.Immutable;
using System.Text.Json;
using System.Text.Json.Serialization;

using MMKiwi.MdbTools;
using MMKiwi.MdbTools.Tests.Model;

await using MdbHandle handle = MdbHandle.Open("Databases/Northwind_Modified.mdb");
var tables = handle.Tables;

Dictionary<string, Table> outTables = new(tables.Length);

foreach (var table in tables)
{
    try
    {
        Dictionary<string, ColumnType> outColumns = new(table.Columns.Length);
        foreach (var column in table.Columns)
        {
            outColumns[column.Name] = column.Type;
        }
        List<Dictionary<string, JsonElement>> outRows = new(table.NumRows);
        await foreach (var row in table.GetRowsAsync(handle))
        {
            outRows.Add(row.Fields.ToDictionary(f => f.Column.Name, f => JsonSerializer.SerializeToElement(f.Value)));
        }

        outTables[table.Name] = new Table(outColumns.ToImmutableDictionary(),
                                outRows.Select(d => d.ToImmutableDictionary()).ToImmutableArray());
    }
    catch
    {
        continue;
    }
}

var options = new JsonSerializerOptions
{
    WriteIndented = true,
    TypeInfoResolver = JsonContext.Default,
    Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
};

using var jsonFile = File.Open("Databases/Northwind_Modified.mdb.json", FileMode.Create, FileAccess.Write);
await JsonSerializer.SerializeAsync(jsonFile, new Database(outTables.ToImmutableDictionary()), options);