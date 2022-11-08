using System.Collections.Immutable;
using System.Text.Json;

namespace MMKiwi.MdbTools.Tests.Model;


public sealed record class MdbJsonTable(ImmutableDictionary<string, ColumnType>? Columns, ImmutableArray<ImmutableDictionary<string, JsonElement>>? Rows) { }
