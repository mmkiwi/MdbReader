using System.Collections.Immutable;
using System.Text.Json;
using System.Text.Json.Serialization;
using MMKiwi.MdbReader;
using MMKiwi.MdbReader.Values;
using MMKiwi.MdbReader.JsonModel;

if (args.Length != 1)
{
    Console.WriteLine("Please enter a file path");
    return -1;
}
using MdbReader handle = MdbReader.Open(args[0]);

var tables = handle.Tables;

Dictionary<string, MdbJsonTable> outTables = new(tables.Count);

string[] tableNames = new string[] {
        "Project",
        "LuminairesIndex",
        "Luminaires",
        "CalcPts",
        "StatAreas",
        "RoadOpt"
    };

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
            outRows.Add(row.ToDictionary(f => f.Column.Name, f => JsonSerializer.SerializeToElement(f switch
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

using var jsonFile = File.Open($"{args[0]}.json", FileMode.Create, FileAccess.Write);
JsonSerializer.Serialize(jsonFile, new MdbJsonDatabase(outTables.ToImmutableDictionary(), handle.DbKey, handle.CreationDate, handle.Encoding.CodePage, handle.Collation), options);

return 0;