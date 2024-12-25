using System;

namespace Flexus.Serialization
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class SerializationIncludedAttribute : Attribute
    {
        
    }
}