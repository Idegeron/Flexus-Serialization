using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Flexus.Serialization
{
    public abstract class SerializationConverter : JsonConverter
    {
        protected List<SerializationTarget> _serializationTargetsStack = new();
        protected List<SerializationPosition> _serializationPositionsStack = new();

        protected virtual void WriteJsonInternal(JsonWriter jsonWriter, object value, JsonSerializer jsonSerializer)
        {
            using (SerializationContext.Override(new SerializeContext(new SerializationTarget(jsonWriter.Path, value), jsonWriter, value.GetType())))
            {
                jsonSerializer.Serialize(jsonWriter, value); 
            }
        }

        protected virtual object ReadJsonInternal(JsonReader jsonReader, Type objectType, object existingValue, JsonSerializer jsonSerializer)
        {
            using (SerializationContext.Override(new DeserializeContext(new SerializationPosition(jsonReader.Path, jsonReader.Depth), objectType)))
            {
                return jsonSerializer.Deserialize(jsonReader, objectType);   
            }
        }
        
        public override bool CanConvert(Type objectType)
        {
            if (SerializationContext.CurrentSerializationContext != null &&
                SerializationContext.CurrentSerializationContext.SerializationType == objectType)
            {
                if (SerializationContext.CurrentSerializationContext is SerializeContext serializeContext)
                {
                    return !_serializationTargetsStack.Contains(serializeContext.SerializationTarget) 
                           || serializeContext.SerializationTarget.Patch != serializeContext.JsonWriter.Path;
                }

                if (SerializationContext.CurrentSerializationContext is DeserializeContext deserializeContext)
                {
                    return !_serializationPositionsStack.Contains(deserializeContext.SerializationPosition);
                }
            }

            return true;
        }
        
        public override void WriteJson(JsonWriter jsonWriter, object value, JsonSerializer jsonSerializer)
        {
            var serializationTarget = new SerializationTarget(jsonWriter.Path, value);

            _serializationTargetsStack.Add(serializationTarget);
            
            WriteJsonInternal(jsonWriter, value, jsonSerializer);
            
            _serializationTargetsStack.Remove(serializationTarget);
        }

        public override object ReadJson(JsonReader jsonReader, Type objectType, object existingValue, JsonSerializer jsonSerializer)
        {
            var serializationPosition = new SerializationPosition(jsonReader.Path, jsonReader.Depth);
            
            _serializationPositionsStack.Add(serializationPosition);

            var result = ReadJsonInternal(jsonReader, objectType, existingValue, jsonSerializer);

            _serializationPositionsStack.Remove(serializationPosition);
            
            return result; 
        }
    }
}