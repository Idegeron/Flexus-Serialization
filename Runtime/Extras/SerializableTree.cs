#if UNITY_EDITOR
using System;
using System.Collections.Generic;

namespace Flexus.Serialization
{
    [Serializable]
    public class SerializableTree
    {
        public List<SerializableNode> SerializableNodes = new List<SerializableNode>();
    }
}
#endif