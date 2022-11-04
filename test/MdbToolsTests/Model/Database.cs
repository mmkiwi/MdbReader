using System.Collections.Immutable;

namespace MMKiwi.MdbTools.Tests.Model;
internal record class Database(ImmutableDictionary<string, Table> Tables) { }