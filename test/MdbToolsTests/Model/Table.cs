using System.Collections.Immutable;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MMKiwi.MdbTools.Tests.Model;


internal sealed record class Table(ImmutableDictionary<string, ColumnType>? Columns, ImmutableArray<ImmutableDictionary<string, JsonElement>>? Rows) { }
