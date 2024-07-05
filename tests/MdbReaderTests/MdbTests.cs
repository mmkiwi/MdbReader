// This file is part of MdbReader. Licensed under the LGPL version 2.0.
// You should have received a coy of the GNU LGPL version along with this
// program. If not, see https://www.gnu.org/licenses/old-licenses/lgpl-2.0.html
//
// Copyright Micah Makaiwi.
// Based on code from libmdb (https://github.com/mdbtools/mdbtools)

using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;

using FluentAssertions;

using MMKiwi.MdbReader.Values;
using FluentAssertions.Execution;
using MMKiwi.MdbReader.JsonModel;
using System.Collections.Immutable;
using Xunit.Abstractions;

namespace MMKiwi.MdbReader.Tests;

public sealed class MdbTests
{
    private ITestOutputHelper Output { get; }

    public MdbTests(ITestOutputHelper output)
    {
        Output = output;
    }

    [Theory]
    [ClassData(typeof(MdbTestDatabases))]
    public async Task TestFirstPageAsync(string mdbPath, string jsonPath)
    {
        using (new AssertionScope())
        {
#if Debug
            Jet3Reader.SetDebugCallback(m => Output.WriteLine(m));
#endif
            await using MdbConnection handle = await MdbConnection.OpenAsync(mdbPath);
            MdbJsonDatabase jsonDatabase = await ReadJsonAsync(jsonPath) ?? throw new Exception();
            handle.JetVersion.Should().Be(jsonDatabase.JetVersion);
            handle.Collation.Should().Be(jsonDatabase.Collation);
            if (handle.JetVersion == JetVersion.Jet3)
                handle.Encoding.CodePage.Should().Be(jsonDatabase.CodePage);
            else
                handle.Encoding.CodePage.Should().Be(-1);
            handle.DbKey.Should().Be(jsonDatabase.DbKey);
            handle.CreationDate.Should().BeCloseTo(jsonDatabase.CreateDate, TimeSpan.FromSeconds(.5));
        }
    }

    [Theory]
    [ClassData(typeof(MdbTestDatabases))]

    public void TestFirstPage(string mdbPath, string jsonPath)
    {
        using (new AssertionScope())
        {
#if Debug
            Jet3Reader.SetDebugCallback(m => Output.WriteLine(m));
#endif
            using MdbConnection handle = MdbConnection.Open(mdbPath);
            MdbJsonDatabase jsonDatabase = ReadJson(jsonPath) ?? throw new Exception();
            handle.JetVersion.Should().Be(jsonDatabase.JetVersion);
            handle.Collation.Should().Be(jsonDatabase.Collation);
            if (handle.JetVersion == JetVersion.Jet3)
                handle.Encoding.CodePage.Should().Be(jsonDatabase.CodePage);
            else
                handle.Encoding.CodePage.Should().Be(-1);
            handle.DbKey.Should().Be(jsonDatabase.DbKey);
            handle.CreationDate.Should().BeCloseTo(jsonDatabase.CreateDate, TimeSpan.FromSeconds(.5));
        }
    }

    [Theory]
    [ClassData(typeof(MdbTestDatabases))]

    public async Task TestGettingTablesAsync(string mdbPath, string jsonPath)
    {
        using (new AssertionScope())
        {
#if Debug
            Jet3Reader.SetDebugCallback(m => Output.WriteLine(m));
#endif
            await using MdbConnection handle = await MdbConnection.OpenAsync(mdbPath);
            MdbJsonDatabase jsonDatabase = await ReadJsonAsync(jsonPath) ?? throw new Exception();
            handle.Tables.Count.Should().Be(jsonDatabase.Tables.Count);
            foreach (var table in handle.Tables)
            {
                if (!jsonDatabase.Tables.ContainsKey(table.Name))
                    continue;

                var jsonTable = jsonDatabase.Tables[table.Name];
                foreach (var col in table.Columns)
                {
                    jsonTable.Columns.Should().ContainKey(col.Name).WhoseValue.Should().Be(col.Type);
                }
            }
        }
    }

    [Theory]
    [ClassData(typeof(MdbTestDatabases))]

    public void TestGettingTables(string mdbPath, string jsonPath)
    {
        using (new AssertionScope())
        {
#if Debug
            Jet3Reader.SetDebugCallback(m => Output.WriteLine(m));
#endif
            using MdbConnection handle = MdbConnection.Open(mdbPath);
            MdbJsonDatabase jsonDatabase = ReadJson(jsonPath) ?? throw new Exception();
            handle.Tables.Count.Should().Be(jsonDatabase.Tables.Count);
            foreach (var table in handle.Tables)
            {
                if (!jsonDatabase.Tables.ContainsKey(table.Name))
                    continue;

                var jsonTable = jsonDatabase.Tables[table.Name];
                foreach (var col in table.Columns)
                {
                    jsonTable.Columns.Should().ContainKey(col.Name).WhoseValue.Should().Be(col.Type);
                }
            }
        }
    }

    [Theory]
    [ClassData(typeof(MdbTestDatabases))]
    public async Task TestFullEquivalencyAsync(string mdbPath, string jsonPath)
    {
        //using (new AssertionScope())
        {
#if Debug
            Jet3Reader.SetDebugCallback(m => Output.WriteLine(m));
#endif
            await using MdbConnection handle = await MdbConnection.OpenAsync(mdbPath);
            Task<MdbJsonDatabase?> deserializeJson = ReadJsonAsync(jsonPath);

            MdbJsonDatabase? jsonDatabase = await deserializeJson;
            jsonDatabase.Should().NotBeNull();

            int maxThreads = Debugger.IsAttached ? 1 : -1; // When debugger is attached, only run one thread at a time

            await Parallel.ForEachAsync(handle.Tables.Select(t => new TableRunInfo(t, jsonDatabase!, handle)),
                                        new ParallelOptions
                                        {
                                            MaxDegreeOfParallelism = maxThreads
                                        },
                                        ProcessTableAsync);
        }
    }

    [Theory]
    [ClassData(typeof(MdbTestDatabases))]
    public void TestFullEquivalencyParallel(string mdbPath, string jsonPath)
    {
        //using (new AssertionScope())
        {
#if Debug
            Jet3Reader.SetDebugCallback(m => Output.WriteLine(m));
#endif
            using MdbConnection handle = MdbConnection.Open(mdbPath);
            MdbJsonDatabase? jsonDatabase = ReadJson(jsonPath);
            jsonDatabase.Should().NotBeNull();

            int maxThreads = Debugger.IsAttached ? 1 : -1; // When debugger is attached, only run one thread at a time

            Parallel.ForEach(handle.Tables.Select(t => new TableRunInfo(t, jsonDatabase!, handle)),
                                        new ParallelOptions
                                        {
                                            MaxDegreeOfParallelism = maxThreads
                                        },
                                        ProcessTable);
        }
    }

    [Theory]
    [ClassData(typeof(MdbTestDatabases))]
    public void TestFullEquivalency(string mdbPath, string jsonPath)
    {
        using (new AssertionScope())
        {
#if Debug
            Jet3Reader.SetDebugCallback(m => Output.WriteLine(m));
#endif
            using MdbConnection handle = MdbConnection.Open(mdbPath);
            MdbJsonDatabase? jsonDatabase = ReadJson(jsonPath);
            jsonDatabase.Should().NotBeNull();

            int maxThreads = Debugger.IsAttached ? 1 : -1; // When debugger is attached, only run one thread at a time

            foreach (var table in handle.Tables.Select(t => new TableRunInfo(t, jsonDatabase!, handle)))
                ProcessTable(table);
        }
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
        var rows = await table.Rows.ToListAsync(ct);
        rows.Should().HaveCount(jsonTable.Rows.Value.Length);
        int i = 0;
        foreach (var row in rows)
        {
            var jsonRow = jsonTable.Rows.Value[i];
            foreach (var field in row.FieldValues)
            {
                CheckField(jsonRow, field);
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
            foreach (var field in row.FieldValues)
            {
                CheckField(jsonRow, field);
            }
            i++;

        }
    }

    private static void CheckField(ImmutableDictionary<string, JsonElement> jsonRow, IMdbValue field)
    {
        jsonRow.Should().ContainKey(field.Column.Name);
        JsonElement jv = jsonRow[field.Column.Name];
        if (jv.ValueKind == JsonValueKind.Null)
            field.IsNull.Should().BeTrue();
        else
        {
            switch (field)
            {
                case MdbBoolValue castValue:
                    castValue.Value.Should().Be(jv.GetBoolean());
                    break;
                case MdbByteValue castValue:
                    castValue.Value.Should().Be(jv.GetByte());
                    break;
                case MdbIntValue castValue:
                    castValue.Value.Should().Be(jv.GetInt16());
                    break;
                case MdbLongIntValue castValue:
                    castValue.Value.Should().Be(jv.GetInt32());
                    break;
                case MdbStringValue castValue:
                    castValue.Value.Should().Be(jv.GetString());
                    break;
                case MdbMemoValue castValue:
                    var encoding = castValue.Encoding;
                    string dbString = encoding.GetString(castValue.Value!.ReadToEnd());
                    string? jsonString = jv.GetString();
                    dbString.Should().Be(jsonString);
                    break;
                case MdbDateTimeValue castValue:
                    castValue.Value.Should().Be(jv.GetDateTime());
                    break;
                case MdbCurrencyValue castValue:
                    castValue.Value.Should().Be(jv.GetDecimal());
                    break;
                case MdbOleValue castValue:
                    byte[] a = castValue.Value!.ReadToEnd();
                    a.Should().BeEquivalentTo(jv.GetBytesFromBase64());
                    break;
                case MdbSingleValue castValue:
                    castValue.Value.Should().Be(jv.GetSingle());
                    break;
                case MdbDoubleValue castValue:
                    castValue.Value.Should().Be(jv.GetDouble());
                    break;
                case MdbBinaryValue castValue:
                    castValue.Value.Should().BeEquivalentTo(jv.GetBytesFromBase64());
                    break;
                default:
                    throw new NotImplementedException(field.GetType().Name);
            }
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

    private record class TableRunInfo(MdbTable Table, MdbJsonDatabase JsonDatabase, MdbConnection Handle)
    {
    }
}
