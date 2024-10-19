using System;
using Newtonsoft.Json;

namespace Flexus.Serialization
{
    public class TypeConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(Type);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var type = value as Type;
            
            if (type == null)
            {
                writer.WriteNull();
            }
            else
            {
                writer.WriteValue(type.AssemblyQualifiedName);
            }
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
            {
                return null;
            }

            var typeName = (string)reader.Value;
            
            if (string.IsNullOrEmpty(typeName))
            {
                return null;
            }

            return Type.GetType(typeName);
        }
    }
}