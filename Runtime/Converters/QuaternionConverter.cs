using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Flexus.Serialization
{
    public class QuaternionConverter : JsonConverter<Quaternion>
    {
        public override void WriteJson(JsonWriter writer, Quaternion value, JsonSerializer serializer)
        {
            writer.WriteStartArray();
            writer.WriteValue(value.x);
            writer.WriteValue(value.y);
            writer.WriteValue(value.z);
            writer.WriteValue(value.w);
            writer.WriteEndArray();
        }

        public override Quaternion ReadJson(JsonReader reader, Type objectType, Quaternion existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            var array = JArray.Load(reader);
            return new Quaternion((float)array[0], (float)array[1], (float)array[2], (float)array[3]);
        }
    }
}