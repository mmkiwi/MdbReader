using System.Collections.Immutable;

namespace MMKiwi.MdbReader.JsonModel;
public sealed record class MdbJsonDatabase(ImmutableDictionary<string, MdbJsonTable> Tables, uint DbKey, DateTime CreateDate, int CodePage, ushort Collation) { }