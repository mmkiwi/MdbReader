// This file is part of MdbReader. Licensed under the LGPL version 2.0.
// You should have received a coy of the GNU LGPL version along with this
// program. If not, see https://www.gnu.org/licenses/old-licenses/lgpl-2.0.html
//
// Copyright Micah Makaiwi.
// Based on code from libmdb (https://github.com/mdbtools/mdbtools)

using System.Text.Json;
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
    static MdbJsonContext()
    {
        s_defaultOptions.Converters.Add(new JetVersionConverter());
        s_defaultOptions.Converters.Add(new MdbColumnTypeConverter());
    }
}

public class JetVersionConverter : JsonConverter<JetVersion>
{
    public override JetVersion Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        string? value = reader.GetString();
        return JetVersionExtensions.TryParse(value, out JetVersion outVersion, true) ? outVersion : throw new InvalidDataException($"Could not cast {value} to {nameof(JetVersion)}");
    }

    public override void Write(Utf8JsonWriter writer, JetVersion value, JsonSerializerOptions options) => writer.WriteStringValue(value.ToStringFast());
}

public class MdbColumnTypeConverter : JsonConverter<MdbColumnType>
{
    public override MdbColumnType Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        string? value = reader.GetString();
        return MdbColumnTypeExtensions.TryParse(value, out MdbColumnType outVersion, true) ? outVersion : throw new InvalidDataException($"Could not cast {value} to {nameof(JetVersion)}");
    }

    public override void Write(Utf8JsonWriter writer, MdbColumnType value, JsonSerializerOptions options) => writer.WriteStringValue(value.ToStringFast());
}