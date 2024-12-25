using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Flexus.Serialization
{
    public static class SerializationUtility
    {
        private static JsonSerializerSettings JsonSerializerSettings;
        private static ObjectConverter ObjectConverter;
        private static Assembly CoreLibAssembly;
        private static Assembly SystemCoreAssembly;
        private static Assembly SystemAssembly;
        
        static SerializationUtility()
        {
            ObjectConverter = new ObjectConverter();

            JsonSerializerSettings = new JsonSerializerSettings()
            {
                Converters = new List<JsonConverter>()
                {
                    new TypeConverter(),
                    new Vector2Converter(),
                    new Vector3Converter(),
                    new Vector4Converter(),
                    new QuaternionConverter(),
                    ObjectConverter,
                },
                TypeNameHandling = TypeNameHandling.All,
                TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple,
                ReferenceLoopHandling = ReferenceLoopHandling.Serialize
            };

            CoreLibAssembly = typeof(List<>).Assembly;
            SystemCoreAssembly = typeof(HashSet<>).Assembly;
            SystemAssembly = typeof(LinkedList<>).Assembly;
        }
        
        private static object GetElementAt(IEnumerable enumerable, int index)
        {
            var enumerator = enumerable.GetEnumerator();
            
            for (var i = 0; i <= index; i++)
            {
                if (!enumerator.MoveNext())
                {
                    return null;
                }
            }
            
            return enumerator.Current;
        }

        private static Type GetEnumerableElementType(Type type)
        {
            if (type.IsArray)
            {
                return type.GetElementType();
            }

            if (type.IsGenericType)
            {
                var genericTypeDefinition = type.GetGenericTypeDefinition();

                if (genericTypeDefinition == typeof(List<>))
                {
                    var elementType = type.GetGenericArguments()[0];

                    return elementType;
                }
            }
            
            return default;
        }
        
        private static bool IsEnumerable(FieldInfo fieldInfo)
        {
            return fieldInfo.FieldType != typeof(string) &&
                   fieldInfo.FieldType.IsArray || 
                   fieldInfo.FieldType.IsGenericType && 
                   fieldInfo.FieldType.GetGenericTypeDefinition() == typeof(List<>);
        }
        
        private static bool IsSerializableType(Type type, bool allowCollections = true)
        {
            if (type == typeof(object) || type.IsInterface)
            {
                return false;
            }

            if (type.IsEnum)
            {
                var underlyingType = Enum.GetUnderlyingType(type);

                return underlyingType != typeof(long) && underlyingType != typeof(ulong);
            }

            if (type.IsPrimitive ||
                type == typeof(string) ||
                type == typeof(Vector2) ||
                type == typeof(Vector2Int) ||
                type == typeof(Vector3) ||
                type == typeof(Vector3Int) ||
                type == typeof(Vector4) ||
                type == typeof(Color) ||
                type == typeof(Color32) ||
                type == typeof(LayerMask) ||
                type == typeof(Rect) ||
                type == typeof(RectInt) ||
                type == typeof(AnimationCurve) ||
                type == typeof(Bounds) ||
                type == typeof(BoundsInt) ||
                type == typeof(Gradient) ||
                type == typeof(Quaternion))
            {
                return true;
            }

            if (typeof(Object).IsAssignableFrom(type))
            {
                return true;
            }

            if (typeof(Delegate).IsAssignableFrom(type))
            {
                return false;
            }

            if (type.IsArray)
            {
                var elementType = type.GetElementType();

                return type.GetArrayRank() == 1 &&
                       allowCollections &&
                       IsSerializableType(elementType, allowCollections: false);
            }

            if (type.IsGenericType)
            {
                var genericTypeDefinition = type.GetGenericTypeDefinition();

                if (genericTypeDefinition == typeof(List<>))
                {
                    var elementType = type.GetGenericArguments()[0];

                    return allowCollections &&
                           IsSerializableType(elementType, allowCollections: false);
                }

                if (genericTypeDefinition == typeof(Dictionary<,>))
                {
                    return false;
                }
            }

            if (type.Assembly == CoreLibAssembly ||
                type.Assembly == SystemAssembly ||
                type.Assembly == SystemCoreAssembly)
            {
                return false;
            }

            if (type.GetCustomAttribute<SerializableAttribute>() != null)
            {
                return true;
            }

            return false;
        }

        private static bool IsSerializableByUnity(FieldInfo fieldInfo)
        {
            if (fieldInfo.IsInitOnly)
            {
                return false;
            }

            if (fieldInfo.GetCustomAttribute<NonSerializedAttribute>() != null)
            {
                return false;
            }

            if (fieldInfo.GetCustomAttribute<SerializeReference>() != null)
            {
                if (fieldInfo.FieldType.IsArray)
                {
                    var type = fieldInfo.FieldType.GetElementType();

                    if (type != null && (type.IsSerializable || type.IsInterface))
                    {
                        return true;
                    }

                    return false;
                }

                if (fieldInfo.FieldType.IsGenericType &&
                    fieldInfo.FieldType.GetGenericTypeDefinition() == typeof(List<>))
                {
                    var type = fieldInfo.FieldType.GenericTypeArguments[0];

                    if (type.IsSerializable || type.IsInterface)
                    {
                        return true;
                    }

                    return false;
                }

                return IsSerializableType(fieldInfo.FieldType);
            }

            if (fieldInfo.IsPublic || fieldInfo.GetCustomAttribute<SerializeField>() != null)
            {
                return IsSerializableType(fieldInfo.FieldType);
            }

            return false;
        }

        private static bool ContainUnityAttributes(FieldInfo fieldInfo)
        {
            return fieldInfo.GetCustomAttribute(typeof(SerializeField)) != null
                   || fieldInfo.GetCustomAttribute(typeof(SerializeReference)) != null;
        }

        private static void SerializeValue<T>(T value, string path, JArray jArray)
        {
            if (value != null)
            {
                jArray.Add(new JObject
                {
                    { "$path", new JValue($"{path}") },
                    {
                        "$value",
                        JToken.FromObject(value, JsonSerializer.Create(JsonSerializerSettings))
                    }
                });
            }
        }
        
        private static void SerializeObject<T>(T value, string path, JArray jArray)
        {
            var valueType = value?.GetType() ?? typeof(T);

            var valueSerializationObjectAttribute = (SerializationObjectAttribute)valueType.GetCustomAttribute(typeof(SerializationObjectAttribute));

            if (valueSerializationObjectAttribute != null)
            {
                switch (valueSerializationObjectAttribute.SerializationType)
                {
                    case SerializationType.Selective:
                        break;

                    case SerializationType.Complete:
                       
                        SerializeValue(value, path, jArray);

                        return;

                    case SerializationType.Ignore:
                        return;
                }
            }

            var fieldInfos = valueType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            foreach (var fieldInfo in fieldInfos)
            {
                if (fieldInfo.GetCustomAttribute(typeof(SerializationIgnoredAttribute)) != null
                    || typeof(Object).IsAssignableFrom(fieldInfo.FieldType)
                    || IsEnumerable(fieldInfo) && typeof(Object).IsAssignableFrom(GetEnumerableElementType(fieldInfo.FieldType)))
                {
                    continue;
                }
                
                if (fieldInfo.GetCustomAttribute(typeof(SerializationIncludedAttribute)) != null)
                {
                    SerializeValue(fieldInfo.GetValue(value), $"{path}.{fieldInfo.Name}", jArray);
                }
                else if (ContainUnityAttributes(fieldInfo))
                {
                    if (IsSerializableByUnity(fieldInfo))
                    {
                        if (IsEnumerable(fieldInfo))
                        {
                            if (fieldInfo.GetValue(value) is IEnumerable fieldValue)
                            {
                                var index = 0;

                                foreach (var tempValue in fieldValue)
                                {
                                    SerializeObject(tempValue, $"{path}.{fieldInfo.Name}.[{index}]", jArray);

                                    index++;
                                }
                            }
                        }
                        else if (fieldInfo.FieldType.GetCustomAttribute(typeof(SerializationObjectAttribute)) != null)
                        {
                            SerializeObject(fieldInfo.GetValue(value), $"{path}.{fieldInfo.Name}", jArray);
                        }
                    }
                    else
                    {
                        SerializeValue(fieldInfo.GetValue(value), $"{path}.{fieldInfo.Name}", jArray);
                    }
                }
            }
        }

        private static void OverrideInternal(object value, string path, JToken jToken)
        {
            var segments = path.Split('.', StringSplitOptions.RemoveEmptyEntries);
            var fieldValue = value;
            var fieldInfo = default(FieldInfo);

            for (int i = 0; i < segments.Length; i++)
            {
                var segment = segments[i];

                if (segment.StartsWith("[") && segment.EndsWith("]"))
                {
                    if (fieldValue is IEnumerable enumerable)
                    {
                        var index = int.Parse(segment.Trim('[', ']'));
                        
                        fieldValue = GetElementAt(enumerable, index);
                    }
                    
                    continue;
                }

                fieldInfo = fieldValue.GetType().GetField(segment, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

                if (fieldInfo == null)
                {
                    return;
                }

                if (i == segments.Length - 1)
                {
                    var tempValue = jToken.ToObject(fieldInfo.FieldType, JsonSerializer.Create(JsonSerializerSettings));
                    fieldInfo.SetValue(fieldValue, tempValue);
                }
                else
                {
                    fieldValue = fieldInfo.GetValue(fieldValue);
                }
            }
        }
        
        public static string Serialize<T>(T value, IList<Object> internalObjects)
        {
            ObjectConverter.InternalObjects = internalObjects;

            var jArray = new JArray();
            
            SerializeObject(value, string.Empty, jArray);

            return jArray.ToString(Formatting.None);
        }

        public static void Override<T>(string data, T instance, IList<Object> internalObjects)
        {
            ObjectConverter.InternalObjects = internalObjects;

            if (!string.IsNullOrWhiteSpace(data))
            {
                var jArray = JArray.Parse(data);

                foreach (var jToken in jArray)
                {
                    var jObject = (JObject)jToken;
                    var path = jObject["$path"]?.ToString();
                    var valueToken = jObject["$value"];

                    if (string.IsNullOrEmpty(path) || valueToken == null)
                    {
                        continue;
                    }

                    OverrideInternal(instance, path, valueToken);
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
                var serializationNodes = new List<SerializableNode>();
                
                var jArray = JArray.Parse(serializationData);

                foreach (var jToken in jArray)
                {
                    if (jToken is JObject jObject)
                    {
                        var serializableNode = new SerializableNode(jObject["$path"]?.ToObject<string>(), string.Empty, string.Empty);

                        if (jObject["$value"] is JValue jValue)
                        {
                            serializableNode.value = JsonConvert.SerializeObject(jValue.Value);
                        }
                        else
                        {
                            serializableNode.childrenSerializableNodes.AddRange(JTokenToSerializationNodes(jObject["$value"]));
                        }
                        

                        serializationNodes.Add(serializableNode);
                    }
                }

                return new SerializableTree
                {
                    serializableNodes = serializationNodes
                };
            }

            return default;
        }

        public static string SerializationTreeToSerializationData(SerializableTree serializableTree)
        {
            var jArray = new JArray();

            foreach (var serializableNode in serializableTree.serializableNodes)
            {
                var jObject = new JObject
                {
                    { "$path", new JValue(string.Empty) },
                    { "$value", new JValue(string.Empty) }
                };

                jObject["$path"] = serializableNode.key;
                jObject["$value"] = SerializationNodeToJObject(serializableNode);
                
                jArray.Add(jObject);
            }

            return jArray.ToString(Formatting.None);
        }
#endif
    }
}