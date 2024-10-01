using System;
using Unity.Properties;

namespace Flexus.Serialization
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public class SerializablePropertyAttribute : CreatePropertyAttribute
    {
        
    }
}