using System;

namespace Flexus.Serialization
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Class | AttributeTargets.Struct)]
    public class SerializationIncludedAttribute : Attribute
    {
        
    }
}