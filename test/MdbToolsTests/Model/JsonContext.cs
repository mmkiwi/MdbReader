using System.Text.Json.Serialization;

namespace MMKiwi.MdbTools.Tests.Model;

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(Database))]
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
internal partial class JsonContext : JsonSerializerContext
{
    
}

/*
 *         ColumnType.Boolean => AsBoolean(),
        ColumnType.Byte => IsNull ? null : AsByte(),
        ColumnType.Int => IsNull ? null : AsInt16(),
        ColumnType.LongInt => IsNull ? null : AsInt32(),
        ColumnType.Money => IsNull ? null : AsDecimal(),
        ColumnType.Float => IsNull ? null : AsFloat(),
        ColumnType.Double => IsNull ? null : AsDouble(),
        ColumnType.DateTime => IsNull ? null : AsDateTime(),
        ColumnType.Binary => IsNull ? null : AsBinary(),
        ColumnType.Text => IsNull ? null : AsStringNotNull(),
        ColumnType.OLE => IsNull ? null : AsBinary(),
        ColumnType.Memo => IsNull ? null : AsStringNotNull(),
        ColumnType.Guid => IsNull ? null : AsGuid(),*/