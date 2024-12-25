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
            OnBeforeSerialize();
            
#if UNITY_EDITOR
            if(_isDirty)
            {
                _objects.Clear();
                
                _serializationData = SerializationUtility.Serialize(this, _objects);

                _isDirty = false;
            }
            
            _serializableTree = SerializationUtility.SerializationDataToSerializationTree(_serializationData);
#endif
        } 

        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
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
            
            OnAfterDeserialize();
        }
        
        protected virtual void OnBeforeSerialize()
        {
        }
        
        protected virtual void OnAfterDeserialize()
        {
        }
        
#if UNITY_EDITOR
        public void SetDirty(bool value)
        {
            _isDirty = value;
        }
        
        public void Apply(string serializationData)
        {
            _serializationData = serializationData;
            
            _serializableTree = SerializationUtility.SerializationDataToSerializationTree(_serializationData);

            try
            {
                if (!string.IsNullOrEmpty(_serializationData))
                {
                    SerializationUtility.Override(_serializationData, this, _objects);
                }
            }
            catch
            {
            }
        }
#endif
    }
}