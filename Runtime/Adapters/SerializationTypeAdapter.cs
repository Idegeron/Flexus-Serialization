using System;
using System.Runtime.CompilerServices;
using Newtonsoft.Json;
using Unity.Serialization.Json;
using JsonWriter = Unity.Serialization.Json.JsonWriter;

namespace Flexus.Serialization
{
    public class SerializationTypeAdapter : IContravariantJsonAdapter<Type>, IJsonAdapter<Type>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Serialize(JsonWriter jsonWriter, Type value)
        {
           jsonWriter.WriteValue(JsonConvert.SerializeObject(value));
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Type Deserialize(SerializedValueView view)
        {
            return JsonConvert.DeserializeObject<Type>(view.AsStringView().ToString());
        } 
        
        public void Serialize(IJsonSerializationContext context, Type value)
        {
            Serialize(context.Writer, value);
        }

        public object Deserialize(IJsonDeserializationContext context)
        {
            return Deserialize(context.SerializedValue);
        }

        public void Serialize(in JsonSerializationContext<Type> context, Type value)
        {
            Serialize(context.Writer, value);
        }

        public Type Deserialize(in JsonDeserializationContext<Type> context)
        {
            return Deserialize(context.SerializedValue);
        }
    }
}