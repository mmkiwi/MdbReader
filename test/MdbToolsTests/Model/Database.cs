using System.Collections.Immutable;

namespace MMKiwi.MdbTools.Tests.Model;
internal sealed record class Database(ImmutableDictionary<string, Table?> Tables) { }