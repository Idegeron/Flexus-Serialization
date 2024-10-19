using System;

namespace Flexus.Serialization
{
    public struct SerializationTarget
    {
        public string Patch { get; }

        public object Object { get; }

        public SerializationTarget(string patch, object obj)
        {
            Patch = patch;

            Object = obj;
        }

        public override bool Equals(object obj)
        {
            if (obj is SerializationTarget serializationPosition)
            {
                return Equals(serializationPosition);
            }

            return false;
        }

        public bool Equals(SerializationTarget other)
        {
            return Patch == other.Patch && Object == other.Object;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Patch, Object);
        }
    }
}