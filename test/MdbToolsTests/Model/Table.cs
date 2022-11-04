using System.Collections.Immutable;

namespace MMKiwi.MdbTools.Tests.Model;

internal record class Table(ImmutableDictionary<string, ColumnType> Columns, ImmutableArray<ImmutableDictionary<string, object>> Rows) { }