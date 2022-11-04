using System.Collections.Immutable;
using System.Text.Json;

namespace MMKiwi.MdbTools.Tests.Model;

internal record class Table(ImmutableDictionary<string, ColumnType> Columns, ImmutableArray<ImmutableDictionary<string, JsonElement>> Rows) { }