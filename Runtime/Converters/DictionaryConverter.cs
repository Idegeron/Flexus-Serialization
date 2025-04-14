using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Flexus.Serialization
{
    public class DictionaryConverter : JsonConverter
    {
        protected string RemoveAssemblyDetails(string fullyQualifiedTypeName)
        {
            var builder = new StringBuilder();

            var writingAssemblyName = false;
            var skippingAssemblyDetails = false;
            var followBrackets = false;
            
            for (var i = 0; i < fullyQualifiedTypeName.Length; i++)
            {
                var current = fullyQualifiedTypeName[i];
                
                switch (current)
                {
                    case '[':
                        writingAssemblyName = false;
                        skippingAssemblyDetails = false;
                        followBrackets = true;
                        builder.Append(current);
                        break;
                    case ']':
                        writingAssemblyName = false;
                        skippingAssemblyDetails = false;
                        followBrackets = false;
                        builder.Append(current);
                        break;
                    case ',':
                        if (followBrackets)
                        {
                            builder.Append(current);
                        }
                        else if (!writingAssemblyName)
                        {
                            writingAssemblyName = true;
                            builder.Append(current);
                        }
                        else
                        {
                            skippingAssemblyDetails = true;
                        }
                        break;
                    default:
                        followBrackets = false;
                        if (!skippingAssemblyDetails)
                        {
                            builder.Append(current);
                        }
                        break;
                }
            }

            return builder.ToString();
        }
        
        public override bool CanConvert(Type objectType)
        {
            return typeof(IDictionary).IsAssignableFrom(objectType) || objectType.IsGenericType && objectType.GetGenericTypeDefinition() == typeof(IDictionary<,>);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var dictionaryType = value.GetType();
            var keyType = dictionaryType.GetGenericArguments()[0];
            var streamingContext = serializer.Context;

            var jObject = new JObject
            {
                { "$type", RemoveAssemblyDetails(dictionaryType.FullName) }
            };

            serializer.Context = new StreamingContext(streamingContext.State, new SerializationState(false));

            foreach (DictionaryEntry entry in (IDictionary)value)
            {
                jObject.Add(keyType.IsPrimitive || keyType == typeof(string) ? entry.Key.ToString() 
                    : JToken.FromObject(entry.Key, serializer).ToString(Formatting.None), entry.Value != null ? JToken.FromObject(entry.Value, serializer) : JValue.CreateNull());
            }

            serializer.Context = streamingContext;

            jObject.WriteTo(writer);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
            {
                return null;
            }

            var jObject = JObject.Load(reader);

            var typeName = jObject["$type"]?.ToString();
            var dictionaryType = Type.GetType(typeName);
            
            if (dictionaryType == null)
            {
                throw new JsonSerializationException($"Could not resolve type {typeName}");
            }

            var keyType = dictionaryType.GetGenericArguments()[0];
            var valueType = dictionaryType.GetGenericArguments()[1];

            var dictionary = (IDictionary)Activator.CreateInstance(dictionaryType);

            foreach (var property in jObject.Properties())
            {
                if (property.Name != "$type")
                {
                    var key = keyType.IsPrimitive || keyType == typeof(string) ? Convert.ChangeType(property.Name, keyType) : JToken.Parse(property.Name).ToObject(keyType, serializer);
                    var value = property.Value.ToObject(valueType, serializer);

                    if (key != null)
                    {
                        dictionary.Add(key, value);
                    }
                }
            }

            return dictionary;
        }
    }
}