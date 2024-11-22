//HintName: Test1_TodoJsonConverter.g.cs

using System;
using System.Text.Json;
using System.Text.Json.Serialization;
#nullable enable
namespace GeneratedConverters
{
    public class Test1_TodoJsonConverter : JsonConverter<Test1.Todo>
    {
        public override Test1.Todo? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return JsonSerializer.Deserialize<Test1.Todo>(ref reader, options);
        }

        public override void Write(Utf8JsonWriter writer, Test1.Todo? value, JsonSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNullValue();
                return;
            }

            writer.WriteStartObject();
            
            
                        // int
                        writer.WritePropertyName("Id");
                        JsonSerializer.Serialize(writer, value.Id, options);

                        if (value.Title != null)
                        {
                            // string? Title
                            writer.WritePropertyName("Title");
                            JsonSerializer.Serialize(writer, value.Title, options);
                        }

                        if (value.DueBy != null)
                        {
                            // System.DateTimeOffset? DueBy
                            writer.WritePropertyName("DueBy");
                            JsonSerializer.Serialize(writer, value.DueBy, options);
                        }

                        if (value.IsComplete != null)
                        {
                            // bool IsComplete
                            writer.WritePropertyName("IsComplete");
                            JsonSerializer.Serialize(writer, value.IsComplete, options);
                        }


            writer.WriteEndObject();
        }
    }
}
