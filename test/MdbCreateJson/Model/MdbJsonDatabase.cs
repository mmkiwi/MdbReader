using System.Collections.Immutable;

namespace MMKiwi.MdbTools.MdbCreateJson.Model;
public sealed record class MdbJsonDatabase(ImmutableDictionary<string, MdbJsonTable> Tables, uint DbKey, DateTime CreateDate, int CodePage, ushort Collation) { }