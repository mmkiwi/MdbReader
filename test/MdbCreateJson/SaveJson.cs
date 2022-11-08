using System.Collections.Immutable;
using System.Text.Json;
using System.Text.Json.Serialization;

using MMKiwi.MdbTools;
using MMKiwi.MdbTools.Values;
using MMKiwi.MdbTools.Tests.Model;

using MdbHandle handle = MdbHandle.Open("Databases/Northwind_Modified.mdb");

Dictionary<string, MdbJsonTable> outTables = new(handle.Tables.Length);

foreach (var table in handle.Tables)
{
    try
    {
        Dictionary<string, ColumnType> outColumns = new(table.Columns.Length);
        foreach (var column in table.Columns)
        {
            outColumns[column.Name] = column.Type;
        }
        List<Dictionary<string, JsonElement>> outRows = new(table.NumRows);
        foreach (var row in table.Rows)
        {
            outRows.Add(row.Values.ToDictionary(f => f.Column.Name, f => JsonSerializer.SerializeToElement(f switch
            {
                MdbOleValue fOle => ReadOle(fOle),
                MdbMemoValue fMemo => ReadMemo(fMemo),
                _ => f.Value
            })));
        }

        outTables[table.Name] = new MdbJsonTable(outColumns.ToImmutableDictionary(),
                                outRows.Select(d => d.ToImmutableDictionary()).ToImmutableArray());
    }
    catch
    {
        continue;
    }
}

byte[]? ReadOle(MdbOleValue fOle)
{
    if (fOle.Value == null) return null;
    using (fOle.Value)
    {
        return fOle.Value.ReadToEnd();
    }
}


string? ReadMemo(MdbMemoValue fOle)
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
    TypeInfoResolver = MdbJsonContext.Default,
    Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
};

using var jsonFile = File.Open("Databases/Northwind_Modified.mdb.json", FileMode.Create, FileAccess.Write);
JsonSerializer.Serialize(jsonFile, new MdbJsonDatabase(outTables.ToImmutableDictionary()), options);