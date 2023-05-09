// This file is part of MdbReader. Licensed under the LGPL version 2.0.
// You should have received a coy of the GNU LGPL version along with this
// program. If not, see https://www.gnu.org/licenses/old-licenses/lgpl-2.0.html
//
// Copyright Micah Makaiwi.
// Based on code from libmdb (https://github.com/mdbtools/mdbtools)

using System.Text.Json.Serialization;

namespace MMKiwi.MdbReader.JsonModel;
public sealed record class MdbJsonDatabase(
    [property: JsonPropertyOrder(99)] ImmutableDictionary<string, MdbJsonTable> Tables,
    [property: JsonPropertyOrder(0)][property: JsonRequired] JetVersion JetVersion,
    [property: JsonPropertyOrder(1)][property: JsonRequired] int DbKey,
    [property: JsonPropertyOrder(2)][property: JsonRequired] DateTime CreateDate,
    [property: JsonPropertyOrder(3)][property: JsonRequired] int CodePage,
    [property: JsonPropertyOrder(4)][property: JsonRequired] int Collation);