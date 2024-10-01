#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Flexus.Serialization
{
    public class SerializationAssetPostprocessor : AssetPostprocessor
    {
        private static Dictionary<string, List<Object>> _prefabMapping = new();

        private static Object SerializableToObject(ISerializable serializable)
        {
            return serializable switch
            {
                SerializableBehaviour serializableBehaviour => serializableBehaviour,
                SerializableComponent serializableComponent => serializableComponent,
                SerializableMonoBehaviour serializableMonoBehaviour => serializableMonoBehaviour,
                SerializableScriptableObject serializableScriptableObject => serializableScriptableObject,
                SerializableStateMachineBehaviour serializableStateMachineBehaviour => serializableStateMachineBehaviour,
                SerializableUnityObject serializableUnityObject => serializableUnityObject,
                _ => default
            };
        }
        
        private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            foreach (string assetPath in importedAssets)
            {
                if (!_prefabMapping.ContainsKey(assetPath) && assetPath.EndsWith(".prefab"))
                {
                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
                    var objects = new List<Object>();
                    
                    if (prefab.TryGetComponent<ISerializable>(out var serializable))
                    {
                        var serializableObject = SerializableToObject(serializable);

                        if (serializableObject != null)
                        {
                            objects.Add(serializableObject);
                        }
                    }

                    foreach (var variableSerializable in prefab.GetComponentsInChildren<ISerializable>())
                    {
                        var serializableObject = SerializableToObject(variableSerializable);

                        if (serializableObject != null)
                        {
                            objects.Add(serializableObject);
                        }
                    }

                    if (objects.Count > 0)
                    {
                        _prefabMapping.Add(assetPath, objects);
                    }
                }

                if (_prefabMapping.TryGetValue(assetPath, out var serializableObjects))
                {
                    var needClean = false;
                    
                    foreach (var serializableObject in serializableObjects)
                    {
                        if (serializableObject != null)
                        {
                            if (PrefabUtility.HasModification(serializableObject))
                            {
                                PrefabUtility.ValidatePrefab(serializableObject);
                            }
                        }
                        else
                        {
                            needClean = true;
                        }
                    }

                    if (needClean)
                    {
                        var newSerializableObjects = new List<Object>();

                        foreach (var serializableObject in serializableObjects)
                        {
                            if (serializableObject != null)
                            {
                                newSerializableObjects.Add(serializableObject);
                            }
                        }

                        if (newSerializableObjects.Count > 0)
                        {
                            _prefabMapping[assetPath] = newSerializableObjects;
                        }
                        else
                        {
                            _prefabMapping.Remove(assetPath);
                        }
                    }
                }
            }
        }
    }
}
#endif