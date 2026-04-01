#if UNITY_EDITOR
using System;
using System.Collections.Generic;

namespace Flexus.Serialization
{
    [Serializable]
    public class SerializableNode
    {
        public string Path;
        public string Value;

        public SerializableNode(string path, string value)
        {
            Path = path;
            Value = value;
        }
    }
}
#endif