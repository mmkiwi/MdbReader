using System.Collections.Immutable;
using System.Text.Json;
using System.Text.Json.Serialization;

using MMKiwi.MdbTools;
using MMKiwi.MdbTools.Tests.Model;

await using MdbHandle handle = await MdbHandle.Open("test/MdbToolsTests/Databases/Northwind_Modified.mdb");
var tables = await handle.GetUserTablesAsync();

Dictionary<string, Table> outTables = new(tables.Count());

foreach (var table in tables)
{
    try
    {
        Dictionary<string, ColumnType> outColumns = new(table.Columns.Length);
        foreach (var column in table.Columns)
        {
            outColumns[column.Name] = column.Type;
        }
        List<Dictionary<string, object>> outRows = new(table.NumRows);
        await foreach (var row in table.GetRows(handle))
        {
            var fields = row.Fields.ToDictionary(f => f.Column.Name, f => f.Value);
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

using var jsonFile = File.Open("test/MdbToolsTests/Databases/Northwind_Modified.mdb.json", FileMode.Open, FileAccess.Write);
await JsonSerializer.SerializeAsync(jsonFile, new Database(outTables.ToImmutableDictionary()), options);