using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
namespace ArrowEscape.UI
{
    /// <summary>Shares one Input-System-only event system across HUD and menu canvases.</summary>
    public static class EventSystemEnsurer
    {
        /// <summary>Reuses one system, retires duplicates, and equips default UI actions.</summary>
        public static EventSystem Ensure()
        {
            var systems = Object.FindObjectsByType<EventSystem>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            EventSystem chosen = EventSystem.current != null ? EventSystem.current : systems.Length > 0 ? systems[0] : new GameObject("EventSystem").AddComponent<EventSystem>();
            for (int i = 0; i < systems.Length; i++)
            {
                if (systems[i] == chosen) continue;
                systems[i].enabled = false;
                foreach (BaseInputModule duplicateModule in systems[i].GetComponents<BaseInputModule>()) Object.DestroyImmediate(duplicateModule);
                Object.DestroyImmediate(systems[i]);
            }
            chosen.gameObject.SetActive(true);
            chosen.enabled = true;
            foreach (BaseInputModule other in chosen.GetComponents<BaseInputModule>())
                if (!(other is InputSystemUIInputModule)) other.enabled = false;
            var module = chosen.GetComponent<InputSystemUIInputModule>();
            if (module == null) module = chosen.gameObject.AddComponent<InputSystemUIInputModule>();
            module.enabled = true;
            if (module.actionsAsset == null) module.AssignDefaultActions();
            return chosen;
        }
    }
}
