using System.Diagnostics;
using System.Reflection.Metadata;
using System.Text.Json;
using System.Text.Json.Serialization;

using FluentAssertions;
using FluentAssertions.Execution;

using MMKiwi.MdbTools;
using MMKiwi.MdbTools.Fields;
using MMKiwi.MdbTools.Tests.Model;

namespace MMKiwi.MdbTools.Tests;

public class PrelimTests
{
    [Fact]
    public async Task TestAsync()
    {
        await using MdbHandle handle = MdbHandle.Open("Databases/Northwind_Modified.mdb");
        Task<Database?> deserializeJson = ReadJsonAsync("Databases/Northwind_Modified.mdb.json");

        Database? jsonDatabase = await deserializeJson;
        jsonDatabase.Should().NotBeNull();

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
        Database? jsonDatabase = ReadJson("Databases/Northwind_Modified.mdb.json");
        jsonDatabase.Should().NotBeNull();

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
        Table? jsonTable = jsonDatabase.Tables[table.Name];

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
        var rows = await table.GetRowsAsync(tableRunInfo.Handle, ct).ToListAsync(ct);
        rows.Should().HaveCount(jsonTable.Rows.Value.Length);
        int i = 0;
        foreach (var row in rows)
        {
            var jsonRow = jsonTable.Rows.Value[i];
            foreach (var field in row.Fields)
            {
                jsonRow.Should().ContainKey(field.Column.Name);
                JsonElement jv = jsonRow[field.Column.Name];
                if (jv.ValueKind == JsonValueKind.Null)
                    field.IsNull.Should().BeTrue();
                else
                {
                    switch (field)
                    {
                        case MdbBoolField boolField:
                            boolField.Value.Should().Be(jv.GetBoolean());
                            break;
                        case MdbByteField byteField:
                            byteField.Value.Should().Be(jv.GetByte());
                            break;
                        case MdbByteField.Nullable byteFieldNull:
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
        Table? jsonTable = jsonDatabase.Tables[table.Name];

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
        var rows = table.GetRows(tableRunInfo.Handle).ToList();
        rows.Should().HaveCount(jsonTable.Rows.Value.Length);
        int i = 0;
        foreach (var row in rows)
        {
            var jsonRow = jsonTable.Rows.Value[i];
            foreach (var field in row.Fields)
            {
                jsonRow.Should().ContainKey(field.Column.Name);
                JsonElement jv = jsonRow[field.Column.Name];
                if (jv.ValueKind == JsonValueKind.Null)
                    field.IsNull.Should().BeTrue();
                else
                {
                    switch (field)
                    {
                        case MdbBoolField boolField:
                            boolField.Value.Should().Be(jv.GetBoolean());
                            break;
                        case MdbByteField byteField:
                            byteField.Value.Should().Be(jv.GetByte());
                            break;
                        case MdbByteField.Nullable byteFieldNull:
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

    private static async Task<Database?> ReadJsonAsync(string jsonPath)
    {
        await using var jsonFile = File.OpenRead(jsonPath);
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            TypeInfoResolver = JsonContext.Default,
            Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
        };
        return await JsonSerializer.DeserializeAsync(jsonFile, typeof(Database), options) as Database;
    }

    private static Database? ReadJson(string jsonPath)
    {
        using var jsonFile = File.OpenRead(jsonPath);
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            TypeInfoResolver = JsonContext.Default,
            Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
        };
        return JsonSerializer.Deserialize(jsonFile, typeof(Database), options) as Database;
    }

    private record class TableRunInfo(MdbTable Table, Database JsonDatabase, MdbHandle Handle)
    {
    }
}
