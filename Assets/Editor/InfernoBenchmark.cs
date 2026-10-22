using UnityEditor;
namespace ArrowEscape.Editor
{
    /// <summary>Explicit, isolated fire benchmark; never starts automatically.</summary>
    [InitializeOnLoad]
    public static class InfernoBenchmark
    {
        private static double waitUntil;
        static InfernoBenchmark(){if(SessionState.GetBool("InfernoBenchmark",false))Register();}
        private static void Register(){waitUntil=EditorApplication.timeSinceStartup+12;EditorApplication.update-=Wait;EditorApplication.update+=Wait;}
        /// <summary>Enters Play mode using memory-only stores for 100-arrow CPU/GPU counters.</summary>
        [MenuItem("Tools/Arrow Escape/Skin Gallery - Profile Inferno")]
        public static void Run(){if(EditorApplication.isPlaying){EditorUtility.DisplayDialog("Inferno profiler","Stop Play mode first. The benchmark uses isolated memory-only player data.","OK");return;}SessionState.SetBool("InfernoBenchmark",true);Register();}
        private static void Wait(){if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.timeSinceStartup<waitUntil)return;EditorApplication.update-=Wait;EditorApplication.isPlaying=true;}
    }
}
