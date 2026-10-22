using UnityEditor;
namespace ArrowEscape.Editor
{
    /// <summary>Explicit, isolated fire benchmark; never starts automatically.</summary>
    [InitializeOnLoad]
    public static class RareThemeBenchmark
    {
        private static double waitUntil;
        static RareThemeBenchmark(){if(SessionState.GetBool("RareThemeBenchmark",false))Register();}
        private static void Register(){waitUntil=EditorApplication.timeSinceStartup+12;EditorApplication.update-=Wait;EditorApplication.update+=Wait;}
        /// <summary>Enters Play mode using memory-only stores for 100-arrow CPU/GPU counters.</summary>
        [MenuItem("Tools/Arrow Escape/Skin Gallery - Profile Rare and Themes")]
        public static void Run(){if(EditorApplication.isPlaying){EditorUtility.DisplayDialog("Rare and theme profiler","Stop Play mode first. The benchmark uses isolated memory-only player data.","OK");return;}SessionState.SetBool("RareThemeBenchmark",true);Register();}
        private static void Wait(){if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.timeSinceStartup<waitUntil)return;EditorApplication.update-=Wait;EditorApplication.isPlaying=true;}
    }
}
