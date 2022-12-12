using System.Collections.Immutable;
using System.Text.Json;
using System.Text.Json.Serialization;
using ICSharpCode.SharpZipLib.Zip.Compression.Streams;
using MMKiwi.MdbTools;
using MMKiwi.MdbTools.Values;
using MMKiwi.MdbTools.MdbCreateJson.Model;

var tempMdb = Util.DecompressFile(args[0]);
try
{

    using MdbHandle handle = MdbHandle.Open(tempMdb);

    var tables = handle.GetTables();

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

    using var jsonFile = File.Open($"{args[0]}.json", FileMode.Create, FileAccess.Write);
    JsonSerializer.Serialize(jsonFile, new MdbJsonDatabase(outTables.ToImmutableDictionary(), handle.DbKey, handle.CreationDate, handle.Encoding.CodePage, handle.Collation), options);
}
finally
{
    File.Delete(tempMdb);
}
public static class Util
{
    internal static string DecompressFile(string mdbFilePath)
    {
        var tempDir = Directory.CreateTempSubdirectory("AgiLink");
        string tmpFilePath = Path.Combine(tempDir.FullName, $"{new FileInfo(mdbFilePath).Name}.mdb");
        // Open Database Connection
        //Check to see if valid zlib file and decompress to see if it contains the JET header.
        using FileStream stream = File.OpenRead(mdbFilePath);
        using InflaterInputStream gz = new(stream);
        using FileStream outMdbFile = File.Open(tmpFilePath, FileMode.Create);

        gz.CopyTo(outMdbFile);

        return tmpFilePath;
    }
}