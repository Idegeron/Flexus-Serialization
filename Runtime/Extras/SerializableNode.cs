#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine.Serialization;

namespace Flexus.Serialization
{
    [Serializable]
    public class SerializableNode
    {
        public string key;
        public string value;
        public string type;
        public List<SerializableNode> childrenSerializableNodes;

        public SerializableNode(string key, string value, string type)
        {
            this.key = key;
            this.value = value;
            this.type = type;
            childrenSerializableNodes = new List<SerializableNode>();
        }
    }
}
#endif