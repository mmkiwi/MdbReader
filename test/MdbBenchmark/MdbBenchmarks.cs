using MMKiwi.MdbTools;
using BenchmarkDotNet.Attributes;
using MMKiwi.MdbTools.MdbCreateJson.Model;
using System.Text.Json;
using MMKiwi.MdbTools.Values;
using System.Collections.Immutable;
using System.Text.Json.Serialization;
// See https://aka.ms/new-console-template for more information
[MemoryDiagnoser]
public class MdbBenchmarks
{
    [Benchmark]
    public void RunJet3() => Run("Databases/Northwind_Modified.jet3.mdb");

    //[Benchmark]
    public void RunJet4() => Run("Databases/Northwind_Modified.2007.accdb");

    private void Run(string mdbPath)
    {
        using MdbHandle handle = MdbHandle.Open(mdbPath);
        using var jsonFile = File.Open($"{mdbPath}.json", FileMode.Create, FileAccess.Write);
        using var w = new Utf8JsonWriter(jsonFile);

        w.WriteStartObject();
        w.WriteNumber("DbKey"u8, handle.DbKey);
        w.WriteString("CreateDate"u8, handle.CreationDate);
        w.WriteNumber("CodePage"u8, handle.Encoding.CodePage);
        w.WriteNumber("Collation"u8, handle.Collation);
        w.WriteStartObject("Tables"u8);
        foreach (var table in handle.Tables)
        {
            w.WriteStartObject(table.Name);
            try
            {
                w.WriteStartObject("Columns"u8);
                foreach (var column in table.Columns)
                {

                }
                w.WriteEndObject();
                w.WriteStartArray("Rows"u8);
                foreach (MdbDataRow row in table.Rows)
                {
                    w.WriteStartObject();
                    foreach (var val in row.Values)
                    {
                        w.WritePropertyName(val.Column.Name);
                        if (val.IsNull || val.Value == null)
                        {
                            w.WriteNullValue();
                        }
                        else if (val is MdbOleValue oleVal)
                        {
                            //w.WriteBase64StringValue(ReadOle(oleVal));
                            w.WriteNullValue();
                        }
                        else if (val is MdbMemoValue memoVal)
                        {
                            //w.WriteStringValue(ReadMemo(memoVal));
                            w.WriteNullValue();
                        }
                        else
                        {
                            w.WriteRawValue(JsonSerializer.SerializeToUtf8Bytes(val.Value), false);
                        }
                    }
                    w.WriteEndObject();
                }
                w.WriteEndArray();
            }
            catch
            {
                continue;
            }
            w.WriteEndObject();
        }
        w.WriteEndObject();
        w.WriteEndObject();

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
        w.Flush();
        //Console.WriteLine($"Wrote {w.BytesCommitted} bytes");
    }
}