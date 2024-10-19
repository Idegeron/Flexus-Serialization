using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Object = UnityEngine.Object;

namespace Flexus.Serialization
{
    public static class SerializationUtility
    {
        private static FlexusConverter FlexusConverter;
        private static JsonSerializerSettings JsonSerializerSettings;

        static SerializationUtility()
        {
            FlexusConverter = new FlexusConverter();

            JsonSerializerSettings = new JsonSerializerSettings()
            {
                Converters = new List<JsonConverter>()
                {
                    new TypeConverter(),
                    new Vector2Converter(),
                    new Vector3Converter(),
                    new Vector4Converter(),
                    new QuaternionConverter(),
                    FlexusConverter,
                },
                TypeNameHandling = TypeNameHandling.All,
                TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple,
                ReferenceLoopHandling = ReferenceLoopHandling.Serialize
            };
        }
        
        public static string Serialize<T>(T value, IList<Object> internalObjects)
        {
            FlexusConverter.InternalObjects = internalObjects;

            return JsonConvert.SerializeObject(value, JsonSerializerSettings);
        }

        public static T Deserialize<T>(string data, IList<Object> internalObjects)
        {
            FlexusConverter.InternalObjects = internalObjects;

            return JsonConvert.DeserializeObject<T>(data, JsonSerializerSettings);
        }

        public static void Override<T>(string data, T instance, IList<Object> internalObjects)
        {
            FlexusConverter.InternalObjects = internalObjects;
            
            JsonConvert.PopulateObject(data, instance, JsonSerializerSettings);
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

                    var key = jProperty.Name;
                    var value = jProperty.Value.Type is JTokenType.Object ? string.Empty : jProperty.Value is JArray jArray && jArray.Count > 0 ? string.Empty : JsonConvert.SerializeObject(jProperty.Value);
                    var type = jProperty.Value.Type == JTokenType.Object ? valueJObject?.GetValue("$type")?.ToString() ?? string.Empty : jProperty.Value.Type.ToString();
                    
                    var serializationNode = new SerializableNode(key, value, type);

                    serializationNode.childrenSerializableNodes.AddRange(JTokenToSerializationNodes(jProperty.Value));
            
                    serializableNodes.Add(serializationNode);
                }
            }
            else if (jToken is JArray jArray)
            {
                foreach (var elementJToken in jArray)
                {
                    var value = elementJToken.Type is JTokenType.Object ? string.Empty : elementJToken is JArray elementJArray && elementJArray.Count > 0 ? string.Empty : JsonConvert.SerializeObject(elementJToken);
                    var type = elementJToken.Type == JTokenType.Object ? (elementJToken as JObject)?.GetValue("$type")?.ToString() ?? string.Empty : elementJToken.Type.ToString();
                    
                    var serializableNode = new SerializableNode(string.Empty, value, type);

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
            if (!string.IsNullOrEmpty(serializationData))
            {
                return new SerializableTree()
                {
                    serializableNodes =
                        new List<SerializableNode>(JTokenToSerializationNodes(JObject.Parse(serializationData)))
                };
            }

            return default;
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