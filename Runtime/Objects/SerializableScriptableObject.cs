using System.Collections.Generic;
using Newtonsoft.Json;
using Unity.Properties;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Flexus.Serialization
{
    [JsonObject(MemberSerialization.OptIn)]
    public abstract class SerializableScriptableObject : ScriptableObject, ISerializable, ISerializationCallbackReceiver
    {
        [SerializeField, HideInInspector, DontCreateProperty, JsonIgnore]
        protected string _serializationData;
        
        [SerializeField, HideInInspector, DontCreateProperty, JsonIgnore] 
        protected List<Object> _objects = new();

        public virtual string SerializationData => _serializationData;
        
        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
            OnBeforeSerialize();
            
#if UNITY_EDITOR
            _objects.Clear();
#endif
            
            _serializationData = SerializationUtility.Serialize(this, _objects);

#if UNITY_EDITOR
            EditorUtility.SetDirty(this);
#endif
        }

        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            try
            {
                if (!string.IsNullOrEmpty(_serializationData))
                {
                    var instance = this;
                    
                    SerializationUtility.Override(_serializationData, ref instance, _objects);
                }
            }
            catch
            {
#if !UNITY_EDITOR
               Debug.LogError($"Can't populate object {gameObject.name}", this);
#endif
            }
            
            OnAfterDeserialize();
        }
        
        protected virtual void OnBeforeSerialize()
        {
        }
        
        protected virtual void OnAfterDeserialize()
        {
        }
        
#if UNITY_EDITOR
        public void Apply(string serializationData)
        {
            _serializationData = serializationData;
            
            try
            {
                if (!string.IsNullOrEmpty(_serializationData))
                {
                    var instance = this;
                    
                    SerializationUtility.Override(_serializationData, ref instance, _objects);
                }
            }
            catch
            {
            }
        }
#endif
    }
}