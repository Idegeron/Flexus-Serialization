using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Flexus.Serialization
{
    public class Vector3Converter : JsonConverter<Vector3>
    {
        public override void WriteJson(JsonWriter writer, Vector3 value, JsonSerializer serializer)
        {
            writer.WriteStartArray();
            writer.WriteValue(value.x);
            writer.WriteValue(value.y);
            writer.WriteValue(value.z);
            writer.WriteEndArray();
        }

        public override Vector3 ReadJson(JsonReader reader, Type objectType, Vector3 existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            var array = JArray.Load(reader);
            return new Vector3((float)array[0], (float)array[1], (float)array[2]);
        }
    }
}