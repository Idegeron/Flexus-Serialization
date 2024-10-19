using System;

namespace Flexus.Serialization
{
    public abstract class SerializationContext
    {
        public static SerializationContext CurrentSerializationContext { get; set; }
        
        public Type SerializationType { get; protected set; }

        protected SerializationContext(Type serializationType)
        {
            SerializationType = serializationType;
        }

        public static SerializationScope Override(SerializationContext serializeContext)
        {
            return new SerializationScope(serializeContext);
        }
        
        public struct SerializationScope : IDisposable
        {
            private SerializationContext _previousSerializeContext;
            
            public SerializationScope(SerializationContext serializationContext)
            {
                _previousSerializeContext = CurrentSerializationContext;

                CurrentSerializationContext = serializationContext;
            }

            public void Dispose()
            {
                CurrentSerializationContext = _previousSerializeContext;
            }
        }
    }
}