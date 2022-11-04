using System.Text.Json.Serialization;

namespace MMKiwi.MdbTools.Tests.Model;

[JsonSerializable(typeof(Database))]
internal partial class JsonContext : JsonSerializerContext
{
}
