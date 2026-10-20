using UnityEngine;

namespace ArrowEscape.UI
{
    /// <summary>Creates the UI independently of gameplay bootstrap callback order.</summary>
    public static class UiBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void CreateRuntimeRoot()
        {
            var hud = Object.FindFirstObjectByType<HudController>(FindObjectsInactive.Include);
            if (hud == null) hud = new GameObject("ArrowEscapeUI").AddComponent<HudController>();
            if (Object.FindFirstObjectByType<AppFlowController>(FindObjectsInactive.Include) == null)
                hud.gameObject.AddComponent<AppFlowController>();
        }
    }
}
