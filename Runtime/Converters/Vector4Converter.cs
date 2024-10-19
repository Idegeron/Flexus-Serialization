using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Flexus.Serialization
{
    public class Vector4Converter : JsonConverter<Vector4>
    {
        public override void WriteJson(JsonWriter writer, Vector4 value, JsonSerializer serializer)
        {
            writer.WriteStartArray();
            writer.WriteValue(value.x);
            writer.WriteValue(value.y);
            writer.WriteValue(value.z);
            writer.WriteValue(value.w);
            writer.WriteEndArray();
        }

        public override Vector4 ReadJson(JsonReader reader, Type objectType, Vector4 existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            var array = JArray.Load(reader);
            return new Vector4((float)array[0], (float)array[1], (float)array[2], (float)array[3]);
        }
    }
}