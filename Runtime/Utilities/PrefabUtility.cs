#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Flexus.Serialization
{
    public static class PrefabUtility
    {
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

        public static bool HasModification(Object obj)
        {
            return UnityEditor.PrefabUtility.GetPropertyModifications(obj)?.Length > 0;
        }
        
        public static void ValidatePrefab(Object obj)
        {
            if (!(obj is ISerializable serializableObject) && 
                (!(obj is GameObject gameObject) || !gameObject.TryGetComponent(out serializableObject)))
            {
                return;
            }

            var objectPrefabPath = UnityEditor.PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(obj);
           
            if (string.IsNullOrEmpty(objectPrefabPath))
            {
                return;
            }

            var objectPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(objectPrefabPath);
            var originPrefab = UnityEditor.PrefabUtility.GetCorrespondingObjectFromSource(obj);

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
                UnityEditor.PrefabUtility.SavePrefabAsset(objectPrefab);
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
                            UnityEditor.PrefabUtility.RevertPropertyOverride(propertyInstance,
                                InteractionMode.AutomatedAction);
                        }
                    }
                }

                serializedInstance.ApplyModifiedProperties();
            }
        }
    }
}
#endif