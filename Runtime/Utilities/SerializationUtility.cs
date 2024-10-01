using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Unity.Mathematics;
using Unity.Serialization.Json;
using UnityEngine;

namespace Flexus.Serialization
{
    public static class SerializationUtility
    {
        private static bool Initialized;
        
        private static SerializationUnityObjectAdapter SerializationUnityObjectAdapter;
        
        private static List<IJsonAdapter> JsonAdapters;
        
        private static SerializedObjectReaderConfiguration GetDefaultConfigurationForString(string json, JsonSerializationParameters parameters = default)
        {
            var configuration = SerializedObjectReaderConfiguration.Default;

            configuration.UseReadAsync = false;
            configuration.ValidationType = parameters.DisableValidation ? JsonValidationType.None : parameters.Simplified ? JsonValidationType.Simple : JsonValidationType.Standard;
            configuration.BlockBufferSize = math.max(json.Length * sizeof(char), 16);
            configuration.TokenBufferSize = math.max(json.Length / 2, 16);
            configuration.OutputBufferSize = math.max(json.Length * sizeof(char), 16);
            configuration.StripStringEscapeCharacters = parameters.StringEscapeHandling;

            return configuration;
        }

        private static void Initialize()
        {
            if (!Initialized)
            {
                Initialized = true;

                SerializationUnityObjectAdapter = new SerializationUnityObjectAdapter();
                
                JsonAdapters = new List<IJsonAdapter>() { SerializationUnityObjectAdapter, new SerializationTypeAdapter() };
            }
        }
        
        public static string Serialize<T>(T value, IList<Object> objectReferenceList = null)
        {
            Initialize();
            
            SerializationUnityObjectAdapter.ObjectReferenceList = objectReferenceList;
            
            return JsonSerialization.ToJson(value, new JsonSerializationParameters()
            {
                DisableRootAdapters = true,
                Minified = true,
                UserDefinedAdapters = JsonAdapters
            });
        }

        public static T Deserialize<T>(string data, IList<Object> objectReferenceList = null)
        {
            Initialize();
            
            SerializationUnityObjectAdapter.ObjectReferenceList = objectReferenceList;
            
            unsafe
            {
                fixed (char* buffer = data)
                {
                    var jsonSerializationParameters = new JsonSerializationParameters()
                    {
                        DisableRootAdapters = true,
                        Minified = true,
                        UserDefinedAdapters = JsonAdapters
                    };
                    
                    using var reader = new SerializedObjectReader(buffer, data.Length,
                        GetDefaultConfigurationForString(data, jsonSerializationParameters));
                    
                    reader.Read(out var view);
                    
                    return JsonSerialization.FromJson<T>(view, jsonSerializationParameters);
                }
            }
        }

        public static void Override<T>(string data, ref T instance, IList<Object> objectReferenceList = null)
        {
            Initialize();
            
            SerializationUnityObjectAdapter.ObjectReferenceList = objectReferenceList;
            
            unsafe
            {
                fixed (char* buffer = data)
                {
                    var jsonSerializationParameters = new JsonSerializationParameters()
                    {
                        DisableRootAdapters = true,
                        Minified = true,
                        UserDefinedAdapters = JsonAdapters
                    };
                    
                    using var reader = new SerializedObjectReader(buffer, data.Length, GetDefaultConfigurationForString(data, jsonSerializationParameters));
                   
                    reader.Read(out var view);
                    
                    JsonSerialization.FromJsonOverride(view, ref instance, jsonSerializationParameters);
                }
            }
        }
        
#if UNITY_EDITOR
        private static IEnumerable<SerializableNode> JTokenToSerializationNodes(JToken jToken)
        {
            var serializableNodes = new List<SerializableNode>();
    
            if (jToken is JObject jObject)
            {
                foreach (var jProperty in jObject.Properties())
                {
                    var valueJObject = jProperty.Value as JObject;

                    var serializationNode = new SerializableNode(
                        jProperty.Name,
                        jProperty.Value.Type is JTokenType.Object or JTokenType.Array ? string.Empty : JsonConvert.SerializeObject(jProperty.Value),
                        jProperty.Value.Type == JTokenType.Object
                            ? valueJObject?.GetValue("$type")?.ToString() ?? string.Empty
                            : jProperty.Value.Type.ToString());

                    serializationNode.childrenSerializableNodes.AddRange(JTokenToSerializationNodes(jProperty.Value));
            
                    serializableNodes.Add(serializationNode);
                }
            }
            else if (jToken is JArray jArray)
            {
                foreach (var elementJToken in jArray)
                {
                    var serializableNode = new SerializableNode(
                        string.Empty,
                        elementJToken.Type is JTokenType.Object or JTokenType.Array ? string.Empty : JsonConvert.SerializeObject(elementJToken),
                        elementJToken.Type == JTokenType.Object
                            ? (elementJToken as JObject)?.GetValue("$type")?.ToString() ?? string.Empty
                            : elementJToken.Type.ToString());

                    serializableNode.childrenSerializableNodes.AddRange(JTokenToSerializationNodes(elementJToken));

                    serializableNodes.Add(serializableNode);
                }
            }

            return serializableNodes;
        }
        
        private static JToken SerializationNodeToJObject(SerializableNode serializableNode)
        {
            if (serializableNode.childrenSerializableNodes.Count > 0)
            {
                if (serializableNode.childrenSerializableNodes.All(node => string.IsNullOrEmpty(node.key)))
                {
                    var jArray = new JArray();
                    
                    foreach (var childNode in serializableNode.childrenSerializableNodes)
                    {
                        jArray.Add(childNode.childrenSerializableNodes.Count > 0
                            ? SerializationNodeToJObject(childNode)
                            : JsonConvert.DeserializeObject<JToken>(childNode.value));
                    }

                    return jArray;
                }
                else
                {
                    var jObject = new JObject();

                    foreach (var childNode in serializableNode.childrenSerializableNodes)
                    {
                        jObject[childNode.key] = SerializationNodeToJObject(childNode);
                    }

                    return jObject;
                }
            }
            else
            {
                return JsonConvert.DeserializeObject<JToken>(serializableNode.value);
            }
        }
        
        public static SerializableTree SerializationDataToSerializationTree(string serializationData)
        {
            return new SerializableTree()
            {
                serializableNodes = new List<SerializableNode>(JTokenToSerializationNodes(JObject.Parse(serializationData)))
            };
        }

        public static string SerializationTreeToSerializationData(SerializableTree serializableTree)
        {
            var rootObject = new JObject();

            foreach (var node in serializableTree.serializableNodes)
            {
                rootObject[node.key] = SerializationNodeToJObject(node);
            }

            return rootObject.ToString();
        }
#endif
    }
}