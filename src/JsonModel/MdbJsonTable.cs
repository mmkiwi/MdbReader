using System.Collections.Immutable;
using System.Text.Json;

namespace MMKiwi.MdbReader.JsonModel;


public sealed record class MdbJsonTable(ImmutableDictionary<string, MdbColumnType>? Columns, ImmutableArray<ImmutableDictionary<string, JsonElement>>? Rows) { }
