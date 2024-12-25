using System;

namespace Flexus.Serialization
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
    public class SerializationObjectAttribute : Attribute
    {
        public SerializationType SerializationType { get; }
        
        public SerializationObjectAttribute()
        {
            SerializationType = SerializationType.Selective;
        }

        public SerializationObjectAttribute(SerializationType serializationType)
        {
            SerializationType = serializationType;
        }
    }
}