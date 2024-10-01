namespace Flexus.Serialization
{
    public interface ISerializable
    {
        public string SerializationData { get; }
        
#if UNITY_EDITOR
        public void Apply(string serializationData);
#endif
    }
}