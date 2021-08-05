using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Common.AspNetCore
{
    // https://github.com/dotnet/runtime/issues/43026
    public class ExceptionJsonConverter: JsonConverter<Exception>
    {
        public override Exception Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            throw new NotImplementedException();
        }

        public override void Write(Utf8JsonWriter writer, Exception value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteString("Message", value.Message);
            writer.WriteString("Type", value.GetType().Name);
            writer.WriteEndObject();
        }
    }
}
