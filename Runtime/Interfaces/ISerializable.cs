namespace Flexus.Serialization
{
    public interface ISerializable
    {
        public string SerializationData { get; }
        
#if UNITY_EDITOR
        public void SetDirty(bool value);
        public void Apply(string serializationData);
#endif
    }
}