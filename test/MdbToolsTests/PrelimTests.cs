using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;

using FluentAssertions;

using MMKiwi.MdbTools.Values;
using MMKiwi.MdbTools.Tests.Model;

namespace MMKiwi.MdbTools.Tests;

public class PrelimTests
{
    [Fact]
    public async Task TestAsync()
    {
        await using MdbHandle handle = MdbHandle.Open("Databases/Northwind_Modified.mdb");
        Task<MdbJsonDatabase?> deserializeJson = ReadJsonAsync("Databases/Northwind_Modified.mdb.json");

        MdbJsonDatabase? jsonDatabase = await deserializeJson;
        jsonDatabase.Should().NotBeNull();

        handle.Encoding.CodePage.Should().Be(1252);

        int maxThreads = Debugger.IsAttached ? 1 : -1; // When debugger is attached, only run one thread at a time

        await Parallel.ForEachAsync(handle.Tables.Select(t => new TableRunInfo(t, jsonDatabase!, handle)),
                                    new ParallelOptions
                                    {
                                        MaxDegreeOfParallelism = maxThreads
                                    },
                                    ProcessTableAsync);
    }

    [Fact]
    public void Test()
    {
        using MdbHandle handle = MdbHandle.Open("Databases/Northwind_Modified.mdb");
        MdbJsonDatabase? jsonDatabase = ReadJson("Databases/Northwind_Modified.mdb.json");
        jsonDatabase.Should().NotBeNull();

        handle.Encoding.CodePage.Should().Be(1252);

        int maxThreads = Debugger.IsAttached ? 1 : -1; // When debugger is attached, only run one thread at a time

        Parallel.ForEach(handle.Tables.Select(t => new TableRunInfo(t, jsonDatabase!, handle)),
                                    new ParallelOptions
                                    {
                                        MaxDegreeOfParallelism = maxThreads
                                    },
                                    ProcessTable);
    }

    async ValueTask ProcessTableAsync(TableRunInfo tableRunInfo, CancellationToken ct)
    {
        var table = tableRunInfo.Table;
        var jsonDatabase = tableRunInfo.JsonDatabase;
        jsonDatabase.Tables.Should().ContainKey(table.Name);
        MdbJsonTable? jsonTable = jsonDatabase.Tables[table.Name];

        if (jsonTable == null)
        {
            Debug.WriteLine($"Skipping processing of table {table.Name} because it is null in the json source");
            return;
        }

        if (jsonTable.Columns == null)
        {
            Debug.WriteLine($"Skipping processing of colums for table {table.Name} since column collection is null in json source.");
        }
        else
        {
            foreach (var col in table.Columns)
            {
                jsonTable.Columns.Should().ContainKey(col.Name).WhoseValue.Should().Be(col.Type);
            }
        }

        if (jsonTable.Rows == null)
        {
            Debug.WriteLine($"Skipping processing of rows for table {table.Name} since row collection is null in json source.");
            return;
        }
        var rows = await table.GetRowsAsync(ct).ToListAsync(ct);
        rows.Should().HaveCount(jsonTable.Rows.Value.Length);
        int i = 0;
        foreach (var row in rows)
        {
            var jsonRow = jsonTable.Rows.Value[i];
            foreach (var field in row.Values)
            {
                jsonRow.Should().ContainKey(field.Column.Name);
                JsonElement jv = jsonRow[field.Column.Name];
                if (jv.ValueKind == JsonValueKind.Null)
                    field.IsNull.Should().BeTrue();
                else
                {
                    switch (field)
                    {
                        case MdbBoolValue boolField:
                            boolField.Value.Should().Be(jv.GetBoolean());
                            break;
                        case MdbByteValue byteField:
                            byteField.Value.Should().Be(jv.GetByte());
                            break;
                        case MdbByteValue.Nullable byteFieldNull:
                            byteFieldNull.Value.Should().Be(jv.GetByte());
                            break;
                        default:
                            break;
                    }

                }
            }
            i++;

        }
    }

    void ProcessTable(TableRunInfo tableRunInfo)
    {
        var table = tableRunInfo.Table;
        var jsonDatabase = tableRunInfo.JsonDatabase;
        jsonDatabase.Tables.Should().ContainKey(table.Name);
        MdbJsonTable? jsonTable = jsonDatabase.Tables[table.Name];

        if (jsonTable == null)
        {
            Debug.WriteLine($"Skipping processing of table {table.Name} because it is null in the json source");
            return;
        }

        if (jsonTable.Columns == null)
        {
            Debug.WriteLine($"Skipping processing of colums for table {table.Name} since column collection is null in json source.");
        }
        else
        {
            foreach (var col in table.Columns)
            {
                jsonTable.Columns.Should().ContainKey(col.Name).WhoseValue.Should().Be(col.Type);
            }
        }

        if (jsonTable.Rows == null)
        {
            Debug.WriteLine($"Skipping processing of rows for table {table.Name} since row collection is null in json source.");
            return;
        }
        var rows = table.Rows.ToList();
        rows.Should().HaveCount(jsonTable.Rows.Value.Length);
        int i = 0;
        foreach (var row in rows)
        {
            var jsonRow = jsonTable.Rows.Value[i];
            foreach (var field in row.Values)
            {
                jsonRow.Should().ContainKey(field.Column.Name);
                JsonElement jv = jsonRow[field.Column.Name];
                if (jv.ValueKind == JsonValueKind.Null)
                    field.IsNull.Should().BeTrue();
                else
                {
                    switch (field)
                    {
                        case MdbBoolValue boolField:
                            boolField.Value.Should().Be(jv.GetBoolean());
                            break;
                        case MdbByteValue byteField:
                            byteField.Value.Should().Be(jv.GetByte());
                            break;
                        case MdbByteValue.Nullable byteFieldNull:
                            byteFieldNull.Value.Should().Be(jv.GetByte());
                            break;
                        default:
                            break;
                    }

                }
            }
            i++;

        }
    }

    private static async Task<MdbJsonDatabase?> ReadJsonAsync(string jsonPath)
    {
        await using var jsonFile = File.OpenRead(jsonPath);
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            TypeInfoResolver = MdbJsonContext.Default,
            Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
        };
        return await JsonSerializer.DeserializeAsync(jsonFile, typeof(MdbJsonDatabase), options) as MdbJsonDatabase;
    }

    private static MdbJsonDatabase? ReadJson(string jsonPath)
    {
        using var jsonFile = File.OpenRead(jsonPath);
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            TypeInfoResolver = MdbJsonContext.Default,
            Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
        };
        return JsonSerializer.Deserialize(jsonFile, typeof(MdbJsonDatabase), options) as MdbJsonDatabase;
    }

    private record class TableRunInfo(MdbTable Table, MdbJsonDatabase JsonDatabase, MdbHandle Handle)
    {
    }
}
