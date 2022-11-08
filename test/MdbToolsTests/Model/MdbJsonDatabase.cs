using System.Collections.Immutable;

namespace MMKiwi.MdbTools.Tests.Model;
public sealed record class MdbJsonDatabase(ImmutableDictionary<string, MdbJsonTable> Tables) { }