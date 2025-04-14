namespace Flexus.Serialization
{
    public struct SerializationState
    {
        public bool IsRoot { get; }
        
        public SerializationState(bool isRoot)
        {
            IsRoot = isRoot;
        }
    }
}