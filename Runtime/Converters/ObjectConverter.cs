using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Object = UnityEngine.Object;

namespace Flexus.Serialization
{
    public class ObjectConverter : SerializationConverter
    {
        public IList<Object> InternalObjects { get; set; }

        protected override void WriteJsonInternal(JsonWriter jsonWriter, object value, JsonSerializer jsonSerializer)
        {
            if (value is Object unityObject)
            {
                if (jsonWriter.WriteState != WriteState.Start || jsonSerializer.Context.Context is SerializationState { IsRoot: false })
                {
                    var index = InternalObjects.IndexOf(unityObject);

                    if (index == -1)
                    {
                        InternalObjects.Add(unityObject);

                        index = InternalObjects.Count - 1;
                    }

                    jsonWriter.WriteStartObject();

                    jsonWriter.WritePropertyName("$index");

                    jsonWriter.WriteValue(index);

                    jsonWriter.WriteEndObject();
                    
                    return;
                }
            }
            
            base.WriteJsonInternal(jsonWriter, value, jsonSerializer);
        }

        protected override object ReadJsonInternal(JsonReader jsonReader, Type objectType, object existingValue, JsonSerializer jsonSerializer)
        {
            try
            {
                var jsonObject = JObject.Load(jsonReader);
            
                if (jsonObject.ContainsKey("$index") && jsonObject["$index"] != null)
                {
                    var index = Convert.ToInt32(jsonObject["$index"].ToObject(typeof(int)));
                    
                    if (index < 0 || index >= InternalObjects.Count)
                    {
                        return null;
                    }

                    return InternalObjects[index];
                }
                
                return base.ReadJsonInternal(jsonObject.CreateReader(), objectType, existingValue, jsonSerializer);
            }
            catch
            {
                return base.ReadJsonInternal(jsonReader, objectType, existingValue, jsonSerializer);
            }
        }
    }
}