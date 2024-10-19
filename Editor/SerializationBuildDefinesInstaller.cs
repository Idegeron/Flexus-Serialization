#if UNITY_EDITOR
using UnityEditor;

namespace Flexus.Serialization
{
    [InitializeOnLoad]
    public class SerializationBuildDefinesInstaller
    {
        static SerializationBuildDefinesInstaller()
        {
            var targetGroup = EditorUserBuildSettings.selectedBuildTargetGroup;
            var defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(targetGroup);
            var define = "FLEXUS_SERIALIZATION";

            if (!defines.Contains(define))
            {
                defines += ";" + define;
                
                PlayerSettings.SetScriptingDefineSymbolsForGroup(targetGroup, defines);
            }
        }
    }
}
#endif