using System;
using Newtonsoft.Json;

namespace Flexus.Serialization
{
    public class SerializeContext : SerializationContext
    {
        public SerializationTarget SerializationTarget { get; protected set; }
        
        public JsonWriter JsonWriter { get; }

        public SerializeContext(SerializationTarget serializationTarget, JsonWriter jsonWriter, Type serializationType) : base(serializationType)
        {
            SerializationTarget = serializationTarget;

            JsonWriter = jsonWriter;
        }
    }
}