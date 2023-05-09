// This file is part of MdbReader. Licensed under the LGPL version 2.0.
// You should have received a coy of the GNU LGPL version along with this
// program. If not, see https://www.gnu.org/licenses/old-licenses/lgpl-2.0.html
//
// Copyright Micah Makaiwi.
// Based on code from libmdb (https://github.com/mdbtools/mdbtools)

using System.Text.Json;
using System.Text.Json.Serialization;

namespace MMKiwi.MdbReader.JsonModel;
public sealed record class MdbJsonTable(
    [property: JsonPropertyOrder(0)] ImmutableDictionary<string, MdbColumnType>? Columns,
    [property: JsonPropertyOrder(1)] ImmutableArray<ImmutableDictionary<string, JsonElement>>? Rows);