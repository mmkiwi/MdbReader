using System.Text.Json.Serialization;

namespace MMKiwi.MdbReader.JsonModel;

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(MdbJsonDatabase))]
[JsonSerializable(typeof(int))]
[JsonSerializable(typeof(byte))]
[JsonSerializable(typeof(bool))]
[JsonSerializable(typeof(short))]
[JsonSerializable(typeof(float))]
[JsonSerializable(typeof(Guid))]
[JsonSerializable(typeof(decimal))]
[JsonSerializable(typeof(byte[]))]
[JsonSerializable(typeof(DateTime))]
[JsonSerializable(typeof(string))]
public sealed partial class MdbJsonContext : JsonSerializerContext
{
    
}