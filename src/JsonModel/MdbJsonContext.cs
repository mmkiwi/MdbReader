// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.
//
// Based on code from libmdb (https://github.com/mdbtools/mdbtools)

using System.Text.Json.Serialization;

namespace MMKiwi.MdbReader.JsonModel;

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(MdbJsonDatabase))]
[JsonSerializable(typeof(int))]
[JsonSerializable(typeof(byte))]
[JsonSerializable(typeof(bool))]
[JsonSerializable(typeof(short))]
[JsonSerializable(typeof(float))]
[JsonSerializable(typeof(Guid))]
[JsonSerializable(typeof(decimal))]
[JsonSerializable(typeof(byte[]))]
[JsonSerializable(typeof(DateTime))]
[JsonSerializable(typeof(string))]
public sealed partial class MdbJsonContext : JsonSerializerContext
{
    
}