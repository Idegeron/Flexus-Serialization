using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Flexus.Serialization
{
    [JsonObject(MemberSerialization.OptIn)]
    public abstract class SerializableScriptableObject : ScriptableObject, ISerializable, ISerializationCallbackReceiver
    {
        [SerializeField, HideInInspector, SerializationIgnored, JsonIgnore]
        protected string _serializationData;
        
        [SerializeField, HideInInspector, SerializationIgnored, JsonIgnore] 
        protected List<Object> _objects = new();

#if UNITY_EDITOR
        [SerializeField, HideInInspector, SerializationIgnored, JsonIgnore]
        protected bool _isDirty;
#endif
        
        public virtual string SerializationData => _serializationData;
        
        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
            PreSerialize();
            
#if UNITY_EDITOR
            if (_isDirty)
            {
                _objects.Clear();

                _serializationData = SerializationUtility.Serialize(this, _objects);
                
                _isDirty = false;

                EditorUtility.SetDirty(this);
            }
#endif
            
            PostSerialize();
        }

        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            PreDeserialize();
            
            try
            {
                if (!string.IsNullOrEmpty(_serializationData))
                {
                    SerializationUtility.Override(_serializationData, this, _objects);
                }
            }
            catch
            {
#if !UNITY_EDITOR
               Debug.LogError($"Can't populate object {name}", this);
#endif
            }
            
            PostDeserialize();
        }
        
        protected virtual void PreSerialize()
        {
        }

        protected virtual void PostSerialize()
        {
            
        }

        protected virtual void PreDeserialize()
        {
            
        }
        
        protected virtual void PostDeserialize()
        {
            
        }
        
#if UNITY_EDITOR
        public void SetDirty(bool value)
        {
            _isDirty = value;
        }
        
        public void SetSerializationData(string serializationData)
        {
            _serializationData = serializationData;
            
            SerializationUtility.Override(_serializationData, this, _objects);
        }
#endif
    }
}