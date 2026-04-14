using System;
using System.Collections;
using System.Collections.Generic;
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
                    new DictionaryConverter(),
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
            using var disposableEnumerator = enumerator as IDisposable;

            for (var i = 0; i <= index; i++)
            {
                if (!enumerator.MoveNext())
                {
                    return null;
                }
            }

            return enumerator.Current;
        }
        
        private static bool IsSerializableByUnity<T>(FieldInfo fieldInfo, T value = default)
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

                return !fieldInfo.FieldType.IsInterface && IsSerializable(fieldInfo.FieldType) || value != null && IsSerializable(value.GetType());
            }

            if (fieldInfo.IsPublic || fieldInfo.GetCustomAttribute<SerializeField>() != null)
            {
                return IsSerializable(fieldInfo.FieldType);
            }

            return false;
        }

        private static bool IsPrimitive(Type type)
        {
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

            if (type.IsEnum)
            {
                return true;
            }

            return false;
        }

        private static bool IsList(Type type)
        {
            return typeof(IList).IsAssignableFrom(type) ||
                   (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>));
        }

        private static bool IsDictionary(Type type)
        {
            return typeof(IDictionary).IsAssignableFrom(type) ||
                   (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IDictionary<,>));
        }

        private static bool IsSet(Type type)
        {
            var setType = typeof(ISet<>);
            
            if (type.IsGenericType && type.GetGenericTypeDefinition() == setType)
            {
                return true;
            }

            foreach (var interfaceType in type.GetInterfaces())
            {
                if (interfaceType.IsGenericType && interfaceType.GetGenericTypeDefinition() == setType)
                {
                    return true;
                }
            }

            return false;
        }
        
        private static bool IsEnumerable(Type type)
        {
            return type != typeof(string) && type.IsArray || type.IsGenericType &&
                (type.GetGenericTypeDefinition() == typeof(List<>) || type.GetGenericTypeDefinition() == typeof(Dictionary<,>));
        }

        private static bool IsSerializable(Type type, bool allowCollections = true)
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

            if (IsPrimitive(type))
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

                return type.GetArrayRank() == 1 && allowCollections && IsSerializable(elementType, allowCollections: false);
            }

            if (type.IsGenericType)
            {
                var genericTypeDefinition = type.GetGenericTypeDefinition();

                if (genericTypeDefinition == typeof(List<>))
                {
                    var elementType = type.GetGenericArguments()[0];

                    return allowCollections && IsSerializable(elementType, allowCollections: false);
                }

                if (genericTypeDefinition == typeof(Dictionary<,>))
                {
                    return false;
                }
            }

            if (type.Assembly == CoreLibAssembly || type.Assembly == SystemAssembly || type.Assembly == SystemCoreAssembly)
            {
                return false;
            }

            return type.GetCustomAttribute<SerializableAttribute>() != null;
        }

        private static bool IsJsonEmpty(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return true;
            }

            var token = JToken.Parse(json);

            return (token.Type == JTokenType.Object && !token.HasValues) || (token.Type == JTokenType.Array && !token.HasValues);
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
                var valueType = value.GetType();

                if (IsPrimitive(valueType) || IsSet(valueType))
                {
                    jArray.Add(new JObject
                    {
                        { "$path", new JValue($"{path}") },
                        { "$value", JToken.FromObject(value, JsonSerializer.Create(JsonSerializerSettings)) }
                    });
                }
                else if (value is IDictionary dictionary)
                {
                    var keyElementType = valueType.GetGenericArguments()[0];
                    var valueElementType = valueType.GetGenericArguments()[1];

                    if (!typeof(Object).IsAssignableFrom(keyElementType) && !typeof(Object).IsAssignableFrom(valueElementType))
                    {
                        if (valueElementType.IsInterface || valueElementType.IsAbstract)
                        {
                            foreach (DictionaryEntry dictionaryEntry in dictionary)
                            {
                                if (dictionaryEntry.Value != null)
                                {
                                    var keyToken = dictionaryEntry.Key == null ? JValue.CreateNull() : JToken.FromObject(dictionaryEntry.Key);

                                    jArray.Add(new JObject
                                    {
                                        { "$path", new JValue($"{path}.[{keyToken}]") },
                                        { "$value", JToken.FromObject(dictionaryEntry.Value, JsonSerializer.Create(JsonSerializerSettings)) }
                                    });
                                }
                            }
                        }
                        else
                        {
                            foreach (DictionaryEntry dictionaryEntry in dictionary)
                            {
                                var keyToken = dictionaryEntry.Key == null ? JValue.CreateNull() : JToken.FromObject(dictionaryEntry.Key);

                                SerializeValue(dictionaryEntry.Value, $"{path}.[{keyToken}]", jArray);
                            }
                        }
                    }
                }
                else if (value is IList list)
                {
                    var elementType = valueType.GetGenericArguments()[0];

                    if (!typeof(Object).IsAssignableFrom(elementType))
                    {
                        if (elementType.IsInterface || elementType.IsAbstract)
                        {
                            var index = 0;

                            foreach (var element in list)
                            {
                                if (element != null)
                                {
                                    jArray.Add(new JObject
                                    {
                                        { "$path", new JValue($"{path}.[{index}]") },
                                        { "$value", JToken.FromObject(element, JsonSerializer.Create(JsonSerializerSettings)) }
                                    });
                                }

                                index++;
                            }
                        }
                        else
                        {
                            var index = 0;

                            foreach (var element in list)
                            {
                                SerializeValue(element, $"{path}.[{index}]", jArray);

                                index++;
                            }
                        }
                    }
                }
                else if (value is IEnumerable enumerable)
                {
                    var elementType = valueType.GetElementType();

                    if (!typeof(Object).IsAssignableFrom(elementType))
                    {
                        var index = 0;

                        foreach (var element in enumerable)
                        {
                            SerializeValue(element, $"{path}.[{index}]", jArray);

                            index++;
                        }
                    }
                }
                else
                {
                    SerializeObject(value, path, jArray, true);
                }
            }
        }

        private static void SerializeObject<T>(T value, string path, JArray jArray, bool isForceSerialization = false)
        {
            var valueType = value?.GetType() ?? typeof(T);

            var valueSerializationObjectAttribute = (SerializationObjectAttribute)valueType.GetCustomAttribute(typeof(SerializationObjectAttribute));

            if (valueSerializationObjectAttribute != null && !isForceSerialization)
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
                if (fieldInfo.GetCustomAttribute(typeof(SerializationIgnoredAttribute)) != null || typeof(Object).IsAssignableFrom(fieldInfo.FieldType))
                {
                    if (IsDictionary(fieldInfo.FieldType))
                    {
                        var keyElementType = fieldInfo.FieldType.GetGenericArguments()[0];
                        var valueElementType = fieldInfo.FieldType.GetGenericArguments()[1];
                        
                        if (typeof(Object).IsAssignableFrom(keyElementType) || typeof(Object).IsAssignableFrom(valueElementType))
                        {
                            continue;
                        }
                    }
                    else if (IsList(fieldInfo.FieldType))
                    {
                        var elementType = fieldInfo.FieldType.GetGenericArguments()[0];

                        if (typeof(Object).IsAssignableFrom(elementType))
                        {
                            continue;
                        }
                    }
                    else if (IsEnumerable(fieldInfo.FieldType))
                    {
                        var elementType = fieldInfo.FieldType.GetElementType();
                        
                        if (typeof(Object).IsAssignableFrom(elementType))
                        {
                            continue;
                        }
                    }
                }

                var fieldValue = fieldInfo.GetValue(value);

                if (fieldInfo.GetCustomAttribute(typeof(SerializationIncludedAttribute)) != null || isForceSerialization)
                {
                    SerializeValue(fieldValue, $"{path}.{fieldInfo.Name}", jArray);
                }
                else if (ContainUnityAttributes(fieldInfo))
                {
                    if (IsSerializableByUnity(fieldInfo, fieldValue))
                    {
                        if (IsEnumerable(fieldInfo.FieldType))
                        {
                            if (fieldValue is IEnumerable enumerable)
                            {
                                var index = 0;

                                foreach (var tempValue in enumerable)
                                {
                                    SerializeObject(tempValue, $"{path}.{fieldInfo.Name}.[{index}]", jArray);

                                    index++;
                                }
                            }
                        }
                        else if (fieldInfo.FieldType.GetCustomAttribute(typeof(SerializationObjectAttribute)) != null)
                        {
                            SerializeObject(fieldValue, $"{path}.{fieldInfo.Name}", jArray);
                        }
                    }
                    else
                    {
                        SerializeValue(fieldValue, $"{path}.{fieldInfo.Name}", jArray);
                    }
                }
            }
        }

        private static void OverrideObject(object value, string path, JToken valueToken)
        {
            var segments = path.Split('.', StringSplitOptions.RemoveEmptyEntries);
            var fieldValue = value;

            for (var i = 0; i < segments.Length; i++)
            {
                var segment = segments[i];

                if (segment.StartsWith("[") && segment.EndsWith("]"))
                {
                    if (fieldValue is IDictionary dictionary)
                    {
                        var genericArguments = fieldValue.GetType().GetGenericArguments();
                        var key = IsPrimitive(genericArguments[0]) ? JToken.FromObject(segment.Trim('[', ']')).ToObject(genericArguments[0]) : JToken.Parse(segment.Trim('[', ']')).ToObject(genericArguments[0]);

                        if (key != null)
                        {
                            if (i == segments.Length - 1)
                            {
                                try
                                {
                                    dictionary[key] = valueToken.ToObject(genericArguments[1], JsonSerializer.Create(JsonSerializerSettings));
                                    
                                    fieldValue = dictionary[key];
                                }
                                catch
                                {
                                    fieldValue = null;
                                }
                            }
                            else
                            {
                                try
                                {
                                    dictionary[key] ??= Activator.CreateInstance(genericArguments[1]);

                                    fieldValue = dictionary[key];
                                }
                                catch
                                {
                                    fieldValue = null;
                                }
                            }
                        }
                    }
                    else if (fieldValue is IList list)
                    {
                        var index = int.Parse(segment.Trim('[', ']'));

                        while (index >= list.Count)
                        {
                            list.Add(default);
                        }
                        
                        if (index < list.Count)
                        {
                            if (i == segments.Length - 1)
                            {
                                try
                                {
                                    list[index] = valueToken.ToObject(fieldValue.GetType().GetGenericArguments()[0], JsonSerializer.Create(JsonSerializerSettings));
                            
                                    fieldValue = list[index];
                                }
                                catch
                                {
                                    fieldValue = null;
                                }
                            }
                            else
                            {
                                try
                                {
                                    list[index] ??= Activator.CreateInstance(fieldValue.GetType().GetGenericArguments()[0]);
                                        
                                    fieldValue = list[index];
                                }
                                catch
                                {
                                    fieldValue = null;
                                }
                            }
                        }
                    }
                    else if (fieldValue is IEnumerable enumerable)
                    {
                        var index = int.Parse(segment.Trim('[', ']'));
                        
                        fieldValue = GetElementAt(enumerable, index);
                    }

                    continue;
                }

                var fieldInfo = fieldValue?.GetType().GetField(segment, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

                if (fieldInfo == null)
                {
                    return;
                }

                if (i == segments.Length - 1)
                {
                    try
                    {
                        fieldInfo.SetValue(fieldValue, valueToken.ToObject(fieldInfo.FieldType, JsonSerializer.Create(JsonSerializerSettings)));
                    }
                    catch
                    {
                        // ignored
                    }
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

            if (!IsJsonEmpty(data))
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

                    OverrideObject(instance, path, valueToken);
                }
            }
        }

#if UNITY_EDITOR
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
                        var serializableNode = new SerializableNode(jObject["$path"]?.ToObject<string>(), JsonConvert.SerializeObject(jObject["$value"]));

                        serializationNodes.Add(serializableNode);
                    }
                }

                return new SerializableTree
                {
                    SerializableNodes = serializationNodes
                };
            }

            return default;
        }

        public static string SerializationTreeToSerializationData(SerializableTree serializableTree)
        {
            var jArray = new JArray();

            foreach (var serializableNode in serializableTree.SerializableNodes)
            {
                var jObject = new JObject
                {
                    { "$path", new JValue(string.Empty) },
                    { "$value", new JValue(string.Empty) }
                };

                jObject["$path"] = serializableNode.Path;
                jObject["$value"] = JsonConvert.DeserializeObject<JToken>(serializableNode.Value);

                jArray.Add(jObject);
            }

            return jArray.ToString(Formatting.None);
        }
#endif
    }
}