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
        
        private static bool SerializedPropertyEqual(SerializedProperty property1, SerializedProperty property2)
        {
            switch (property1.propertyType)
            {
                case SerializedPropertyType.Integer:
                    return property1.intValue == property2.intValue;
                case SerializedPropertyType.Boolean:
                    return property1.boolValue == property2.boolValue;
                case SerializedPropertyType.Float:
                    return Mathf.Approximately(property1.floatValue, property2.floatValue);
                case SerializedPropertyType.String:
                    return property1.stringValue == property2.stringValue;
                case SerializedPropertyType.Color:
                    return property1.colorValue == property2.colorValue;
                case SerializedPropertyType.ObjectReference:
                    return property1.objectReferenceValue == property2.objectReferenceValue;
                case SerializedPropertyType.Enum:
                    return property1.enumValueIndex == property2.enumValueIndex;
                case SerializedPropertyType.Vector2:
                    return property1.vector2Value == property2.vector2Value;
                case SerializedPropertyType.Vector3:
                    return property1.vector3Value == property2.vector3Value;
                case SerializedPropertyType.Rect:
                    return property1.rectValue == property2.rectValue;
                case SerializedPropertyType.Bounds:
                    return property1.boundsValue == property2.boundsValue;
                case SerializedPropertyType.Quaternion:
                    return property1.quaternionValue == property2.quaternionValue;
                default:
                    return false;
            }
        }

        private static bool HasModification(Object obj)
        {
            return PrefabUtility.GetPropertyModifications(obj)?.Length > 0;
        }
        
        private static void ValidatePrefab(Object obj)
        {
            if (!(obj is ISerializable serializableObject) && 
                (!(obj is GameObject gameObject) || !gameObject.TryGetComponent(out serializableObject)))
            {
                return;
            }

            var objectPrefabPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(obj);
           
            if (string.IsNullOrEmpty(objectPrefabPath))
            {
                return;
            }

            var objectPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(objectPrefabPath);
            var originPrefab = PrefabUtility.GetCorrespondingObjectFromSource(obj);

            if (objectPrefab == originPrefab || originPrefab == null || !(originPrefab is ISerializable serializablePrefab) &&
                (!(originPrefab is GameObject prefabGameObject) || !prefabGameObject.TryGetComponent(out serializablePrefab)))
            {
                return;
            }

            var needRevert = false;
            var objectSerializableTree = SerializationUtility.SerializationDataToSerializationTree(serializableObject.SerializationData);
            var prefabSerializableTree = SerializationUtility.SerializationDataToSerializationTree(serializablePrefab.SerializationData);

            for (var i = 0; i < prefabSerializableTree.serializableNodes.Count; i++)
            {
                var objectNode = objectSerializableTree.serializableNodes[i];
                var prefabNode = prefabSerializableTree.serializableNodes[i];

                if (objectNode.key == prefabNode.key && objectNode.type != prefabNode.type)
                {
                    needRevert = true;
                    
                    objectNode.value = prefabNode.value;
                    objectNode.type = prefabNode.type;
                    objectNode.childrenSerializableNodes = prefabNode.childrenSerializableNodes;
                }
            }

            serializableObject.Apply(SerializationUtility.SerializationTreeToSerializationData(objectSerializableTree));
            
            if (objectPrefab != null)
            {
                PrefabUtility.SavePrefabAsset(objectPrefab);
            }

            if (needRevert)
            {
                var serializedInstance = new SerializedObject(serializableObject as Component);
                var serializedPrefab = new SerializedObject(serializablePrefab as Component);

                var propertyInstance = serializedInstance.GetIterator();

                if (propertyInstance.Next(true))
                {
                    while (propertyInstance.Next(true))
                    {
                        var propertyPrefab = serializedPrefab.FindProperty(propertyInstance.propertyPath);

                        if (propertyPrefab != null && propertyInstance.propertyType == propertyPrefab.propertyType &&
                            SerializedPropertyEqual(propertyInstance, propertyPrefab))
                        {
                            PrefabUtility.RevertPropertyOverride(propertyInstance,
                                InteractionMode.AutomatedAction);
                        }
                    }
                }

                serializedInstance.ApplyModifiedProperties();
            }
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
                            if (HasModification(serializableObject))
                            {
                                ValidatePrefab(serializableObject);
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