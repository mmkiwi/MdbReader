using System.Collections.Immutable;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;

using MMKiwi.MdbTools;
using MMKiwi.MdbTools.Fields;
using MMKiwi.MdbTools.Tests.Model;

using MdbHandle handle = MdbHandle.Open("Databases/Northwind_Modified.mdb");
var tables = handle.Tables;

Dictionary<string, Table?> outTables = new(tables.Length);

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
        foreach (var row in table.GetRows(handle))
        {
            outRows.Add(row.Fields.ToDictionary(f => f.Column.Name, f => JsonSerializer.SerializeToElement(f switch
            {
                MdbOleField fOle => ReadOle(fOle),
                MdbMemoField fMemo => ReadMemo(fMemo),
                _ => f.Value
            })));
        }

        outTables[table.Name] = new Table(outColumns.ToImmutableDictionary(),
                                outRows.Select(d => d.ToImmutableDictionary()).ToImmutableArray());
    }
    catch
    {
        continue;
    }
}

byte[]? ReadOle(MdbOleField fOle)
{
    if (fOle.Value == null) return null;
    using (fOle.Value)
    {
        return fOle.Value.ReadToEnd();
    }
}


string? ReadMemo(MdbMemoField fOle)
{
    if (fOle.Value == null) return null;
    using (fOle.Value)
    {
        return fOle.Value.ReadToEnd();
    }
}

var options = new JsonSerializerOptions
{
    WriteIndented = true,
    TypeInfoResolver = JsonContext.Default,
    Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
};

using var jsonFile = File.Open("Databases/Northwind_Modified.mdb.json", FileMode.Create, FileAccess.Write);
JsonSerializer.Serialize(jsonFile, new Database(outTables.ToImmutableDictionary()), options);