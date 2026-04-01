using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

namespace Flexus.Serialization
{
    [JsonObject(MemberSerialization.OptIn)]
    public abstract class SerializableBehaviour : Behaviour, ISerializable, ISerializationCallbackReceiver
    {
        [SerializeField, HideInInspector, SerializationIgnored, JsonIgnore]
        protected string _serializationData;
        
        [SerializeField, HideInInspector, SerializationIgnored, JsonIgnore] 
        protected List<Object> _objects = new();

#if UNITY_EDITOR
        [SerializeField, HideInInspector, SerializationIgnored, JsonIgnore]
        protected bool _isDirty;
        
        [SerializeField, HideInInspector, SerializationIgnored, JsonIgnore] 
        protected SerializableTree _serializableTree;
#endif

        [JsonIgnore]
        public virtual string SerializationData => _serializationData;

        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
            PreSerialize();
            
#if UNITY_EDITOR
            if(_isDirty)
            {
                _objects.Clear();
                
                _serializationData = SerializationUtility.Serialize(this, _objects);

                _isDirty = false;
            }
            
            _serializableTree = SerializationUtility.SerializationDataToSerializationTree(_serializationData);
#endif
            
            PostSerialize();
        } 

        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            PreDeserialize();
            
#if UNITY_EDITOR
            _serializationData = SerializationUtility.SerializationTreeToSerializationData(_serializableTree);
#endif
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
               Debug.LogError($"Can't populate object {gameObject.name}", this);
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
            
            _serializableTree = SerializationUtility.SerializationDataToSerializationTree(_serializationData);

            SerializationUtility.Override(_serializationData, this, _objects);
        }
#endif
    }
}