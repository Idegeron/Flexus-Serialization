using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Serialization.Json;
using Object = UnityEngine.Object;

namespace Flexus.Serialization
{
    public class SerializationUnityObjectAdapter : IContravariantJsonAdapter<Object>, IJsonAdapter<Object>
    {
        public IList<Object> ObjectReferenceList { get; set; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Serialize(JsonWriter jsonWriter, Object value)
        {
            if (value == null || ObjectReferenceList == null)
            {
                jsonWriter.WriteNull();
                
                return;
            }

            var index = ObjectReferenceList.IndexOf(value);
            
            if (index == -1)
            {
                ObjectReferenceList.Add(value);
            
                index = ObjectReferenceList.Count - 1;
            }
            
            jsonWriter.WriteValue(index);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Object Deserialize(SerializedValueView view)
        {
            if (view.IsNull() || ObjectReferenceList == null)
            {
                return null;
            }

            var index = 0;

            if (view.Type == TokenType.String)
            {
                var stringValue = view.AsStringView().ToString();
                
                index = string.IsNullOrEmpty(stringValue) ? -1 : int.Parse(stringValue);
            }
            else
            {
                index = view.AsInt32();
            }
            
            if (index < 0 || index >= ObjectReferenceList.Count)
            {
                return null;
            }
            
            return ObjectReferenceList[index];
        } 

        public void Serialize(IJsonSerializationContext context, Object value)
        {
            Serialize(context.Writer, value);
        }
        
        public void Serialize(in JsonSerializationContext<Object> context, Object value)
        {
            Serialize(context.Writer, value);
        }

        public object Deserialize(IJsonDeserializationContext context)
        {
            return Deserialize(context.SerializedValue);
        }

        public Object Deserialize(in JsonDeserializationContext<Object> context)
        {
            return Deserialize(context.SerializedValue);
        }
    }
}