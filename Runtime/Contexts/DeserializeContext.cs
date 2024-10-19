using System;

namespace Flexus.Serialization
{
    public class DeserializeContext : SerializationContext
    {
        public SerializationPosition SerializationPosition { get; protected set; }

        public DeserializeContext(SerializationPosition serializationPosition, Type serializationType) : base(serializationType)
        {
            SerializationPosition = serializationPosition;
        }
    }
}