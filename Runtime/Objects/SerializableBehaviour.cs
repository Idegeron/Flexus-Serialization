using System.Collections.Generic;
using Newtonsoft.Json;
using Unity.Properties;
using UnityEngine;

namespace Flexus.Serialization
{
    [JsonObject(MemberSerialization.OptIn)]
    public abstract class SerializableBehaviour : Behaviour, ISerializable, ISerializationCallbackReceiver
    {
        [SerializeField, HideInInspector, DontCreateProperty, JsonIgnore]
        protected string _serializationData;
        
        [SerializeField, HideInInspector, DontCreateProperty, JsonIgnore] 
        protected List<Object> _objects = new();

#if UNITY_EDITOR
        [SerializeField, HideInInspector, DontCreateProperty, JsonIgnore] 
        protected SerializableTree _serializableTree;
#endif

        public virtual string SerializationData => _serializationData;
        
        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
            OnBeforeSerialize();
            
#if UNITY_EDITOR
            _objects.Clear();
#endif
            
            _serializationData = SerializationUtility.Serialize(this, _objects);

#if UNITY_EDITOR
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
            
            _serializableTree = SerializationUtility.SerializationDataToSerializationTree(_serializationData);

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