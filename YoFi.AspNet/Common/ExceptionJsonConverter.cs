using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Common.NET
{
    /// <summary>
    /// Convert an Exception to Json
    /// </summary>
    /// <remarks>
    /// Needed because System.Text.Json.Serialization will not serialize an Exception because it contains
    /// a type, and the framework refuses to serialize out types.
    /// 
    /// Not currently used. Could be removed from the project.
    /// </remarks>
    /// <see href="https://github.com/dotnet/runtime/issues/43026"/>
   public class ExceptionJsonConverter: JsonConverter<Exception>
    {
        /// <summary>
        /// Deserialize an exception from JSON
        /// </summary>
        /// <remarks>
        /// Not implemented. This only handles writing.
        /// </remarks>
        public override Exception Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Write an exception <paramref name="value"/> to JSON using the provided <paramref name="writer"/>
        /// </summary>
        /// <param name="writer">Destination for writing</param>
        /// <param name="value">The exception to serialize</param>
        public override void Write(Utf8JsonWriter writer, Exception value, JsonSerializerOptions _)
        {
            writer.WriteStartObject();
            writer.WriteString("Message", value.Message);
            writer.WriteString("Type", value.GetType().Name);
            writer.WriteEndObject();
        }
    }
}
