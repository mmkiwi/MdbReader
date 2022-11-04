using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;

using FluentAssertions;
using FluentAssertions.Execution;

using MMKiwi.MdbTools;
using MMKiwi.MdbTools.Tests.Model;

namespace MMKiwi.MdbTools.Tests;

public class PrelimTests
{
    [Fact]
    public async Task Test1()
    {
        await using MdbHandle handle = MdbHandle.Open("Databases/Northwind_Modified.mdb");
        Task<Database?> deserializeJson = ReadJson("Databases/Northwind_Modified.json");
        Task<IEnumerable<MdbTable>> loadTables = handle.GetUserTablesAsync();

        await Task.WhenAll(deserializeJson, loadTables);



        using var _ = new AssertionScope();

        Database? jsonDatabase = await deserializeJson;
        jsonDatabase.Should().NotBeNull();

        await Parallel.ForEachAsync(await loadTables, async (table, ct) =>
        {
            jsonDatabase!.Tables.Should().ContainKey(table.Name);
            Table jsonTable = jsonDatabase.Tables[table.Name];

            foreach (var col in table.Columns)
            {
                jsonTable.Columns.Should().ContainKey(col.Name).WhoseValue.Should().Be(col.Type);
            }

            var rows = await table.GetRows(handle).ToListAsync();
            rows.Should().HaveCount(jsonTable.Rows.Count());
            int i = 0;
            foreach (var row in rows)
            {
                var jsonRow = jsonTable.Rows[i];
                foreach (var field in row.Fields)
                {
                    jsonRow.Should().ContainKey(field.Column.Name);
                    var jv = jsonRow[field.Column.Name];
                    if (jv.ValueKind == JsonValueKind.Null)
                        field.IsNull.Should().BeTrue();
                    else
                    {
                        switch (field.Column.Type)
                        {
                            case ColumnType.Boolean:
                                field.AsBoolean().Should().Be(jv.GetBoolean());
                                break;
                            case ColumnType.Byte:
                                field.AsByte().Should().Be(jv.GetByte());
                                break;
                            case ColumnType.Int:
                                field.AsInt16().Should().Be(jv.GetInt16());
                                break;
                            case ColumnType.LongInt:
                                field.AsInt32().Should().Be(jv.GetInt32());
                                break;
                            case ColumnType.Money:
                                field.AsDecimal().Should().BeApproximately(jv.GetDecimal(), 0.001M);
                                break;
                            case ColumnType.Float:
                                field.AsFloat().Should().BeApproximately(jv.GetSingle(), 0.001F);
                                break;
                            case ColumnType.Double:
                                field.AsDouble().Should().BeApproximately(jv.GetDouble(), 0.001);
                                break;
                            case ColumnType.DateTime:
                                field.AsDateTime().Should().Be(jv.GetDateTime());
                                break;
                            case ColumnType.Binary:
                            case ColumnType.OLE:
                                field.AsBinary().Should().BeEquivalentTo(jv.GetBytesFromBase64());
                                break;
                            case ColumnType.Text:
                            case ColumnType.Memo:
                                field.AsString().Should().Be(jv.GetString());
                                break;
                            case ColumnType.Guid:
                                field.AsGuid().Should().Be(jv.GetGuid());
                                break;
                            default:
                                throw new NotImplementedException();
                        }
                    }
                }
                i++;

            }
        });
    }

    private async Task<Database?> ReadJson(string jsonPath)
    {
        using var jsonFile = File.Open(jsonPath, FileMode.Open, FileAccess.Read);
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            TypeInfoResolver = JsonContext.Default,
            Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
        };
        return await JsonSerializer.DeserializeAsync<Database>(jsonFile, options);
    }
}