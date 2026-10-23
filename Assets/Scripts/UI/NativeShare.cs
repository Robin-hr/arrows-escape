using System;
using UnityEngine;
#if UNITY_IOS && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

namespace ArrowEscape.UI
{
    /// <summary>
    /// Opens the phone's own share sheet with a short text (Step 14N): an Android ACTION_SEND chooser, or the iOS
    /// UIActivityViewController (Assets/Plugins/iOS/ArrowEscapeShare.mm). Desktop and the Editor have no share sheet,
    /// so they return false and the caller falls back to the clipboard.
    /// </summary>
    public static class NativeShare
    {
#if UNITY_IOS && !UNITY_EDITOR
        [DllImport("__Internal")] private static extern void ArrowEscapeShareText(string text);
#endif

        /// <summary>True when this platform shows a native share sheet.</summary>
        public static bool IsAvailable
        {
            get
            {
#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
                return true;
#else
                return false;
#endif
            }
        }

        /// <summary>Shows the share sheet; false when unavailable or when the platform call failed.</summary>
        public static bool Share(string text, string title)
        {
            if (string.IsNullOrEmpty(text)) return false;
            try
            {
#if UNITY_ANDROID && !UNITY_EDITOR
                using (var intent = new AndroidJavaObject("android.content.Intent"))
                using (var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (var activity = unity.GetStatic<AndroidJavaObject>("currentActivity"))
                using (var intentClass = new AndroidJavaClass("android.content.Intent"))
                {
                    intent.Call<AndroidJavaObject>("setAction", "android.intent.action.SEND").Dispose();
                    intent.Call<AndroidJavaObject>("setType", "text/plain").Dispose();
                    intent.Call<AndroidJavaObject>("putExtra", "android.intent.extra.TEXT", text).Dispose();
                    using (var chooser = intentClass.CallStatic<AndroidJavaObject>("createChooser", intent, title)) activity.Call("startActivity", chooser);
                }
                return true;
#elif UNITY_IOS && !UNITY_EDITOR
                ArrowEscapeShareText(text);
                return true;
#else
                return false;
#endif
            }
            catch (Exception e) { Debug.LogWarning("ArrowEscape share sheet unavailable: " + e.Message); return false; }
        }
    }
}
