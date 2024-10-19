using System;

namespace Flexus.Serialization
{
    public struct SerializationPosition
    {
        public string Patch { get; }
        
        public int Depth { get; }

        public SerializationPosition(string patch, int depth)
        {
            Patch = patch;

            Depth = depth;
        }

        public override bool Equals(object obj)
        {
            if (obj is SerializationPosition serializationPosition)
            {
                return Equals(serializationPosition);
            }
            
            return false;
        }

        public bool Equals(SerializationPosition other)
        {
            return Patch == other.Patch && Depth == other.Depth;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Patch, Depth);
        }
    }
}