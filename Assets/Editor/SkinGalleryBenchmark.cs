using UnityEditor;
namespace ArrowEscape.Editor
{
    /// <summary>Explicit Editor-only Play-mode benchmark entry; never runs in a player build.</summary>
    [InitializeOnLoad]
    public static class SkinGalleryBenchmark
    {
        private static double waitUntil;
        static SkinGalleryBenchmark(){if(SessionState.GetBool("ArrowSkinBenchmark",false))Register();}
        private static void Register(){waitUntil=EditorApplication.timeSinceStartup+30;EditorApplication.update-=WaitForImports;EditorApplication.update+=WaitForImports;}
        /// <summary>Starts the reusable 100-arrow profiler with fake in-memory stores.</summary>
        [MenuItem("Tools/Arrow Escape/Skin Gallery - Profile 100 arrows")]
        public static void Run()
        {
            if(EditorApplication.isPlaying){EditorUtility.DisplayDialog("Skin profiler","Stop Play mode before starting an isolated memory-only benchmark.","OK");return;}SessionState.SetBool("ArrowSkinBenchmark",true);Register();
        }
        private static void WaitForImports()
        {if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.timeSinceStartup<waitUntil)return;EditorApplication.update-=WaitForImports;EditorApplication.isPlaying=true;}
    }
}
