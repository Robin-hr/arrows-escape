using System;
using System.Collections;
using System.Collections.Generic;
using ArrowEscape.Core;
using UnityEngine;
#if AK_NOTIFICATIONS && UNITY_ANDROID && !UNITY_EDITOR
using Unity.Notifications.Android;
#endif
namespace ArrowEscape.Notifications
{
    /// <summary>
    /// Step 17: Android local notifications through Unity Mobile Notifications. Registers itself at startup on Android
    /// devices; everywhere else the Core memory scheduler stays in place. Inexact scheduling only (no exact-alarm
    /// permission), three channels (Daily Puzzle, Special offers, Reminders) and the app's own notification icons.
    /// </summary>
    public sealed class AndroidLocalNotifications : INotificationScheduler
    {
        /// <summary>Small (status bar) icon id set up by the editor release step.</summary>
        public const string SmallIcon = "ak_small";
        /// <summary>Large icon id set up by the editor release step.</summary>
        public const string LargeIcon = "ak_large";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Register()
        {
#if AK_NOTIFICATIONS && UNITY_ANDROID && !UNITY_EDITOR
            try { NotificationRegistry.Scheduler = new AndroidLocalNotifications(); }
            catch (Exception e) { Debug.LogWarning("Arrows Keymaster: notifications unavailable: " + e.Message); }
#endif
        }

#if AK_NOTIFICATIONS && UNITY_ANDROID && !UNITY_EDITOR
        private AndroidLocalNotifications()
        {
            AndroidNotificationCenter.Initialize();
            Channel(NotificationPlanner.DailyChannel, "Daily Puzzle", "The new Daily Puzzle and streak reminders", Importance.Default);
            Channel(NotificationPlanner.OffersChannel, "Special offers", "Weekend sales and festival gifts in the shop", Importance.Default);
            Channel(NotificationPlanner.RemindersChannel, "Reminders", "Journey lives and come-back reminders", Importance.Low);
            AndroidNotificationCenter.CancelAllDisplayedNotifications();
        }
        private static void Channel(string id, string name, string description, Importance importance) =>
            AndroidNotificationCenter.RegisterNotificationChannel(new AndroidNotificationChannel(id, name, description, importance));

        /// <inheritdoc/>
        public bool IsSupported => true;
        /// <inheritdoc/>
        public NotificationPermission Permission
        {
            get
            {
                var status = AndroidNotificationCenter.UserPermissionToPost;
                if (status == PermissionStatus.Allowed) return NotificationPermission.Granted;
                if (status == PermissionStatus.Denied) return NotificationPermission.Denied;
                return NotificationPermission.Unknown;
            }
        }
        /// <inheritdoc/>
        public void RequestPermission(Action<bool> done)
        {
            if (Permission == NotificationPermission.Granted) { done?.Invoke(true); return; }
            Runner.Instance.StartCoroutine(Ask(done));
        }
        private static IEnumerator Ask(Action<bool> done)
        {
            var request = new PermissionRequest();
            while (request.Status == PermissionStatus.RequestPending) yield return null;
            done?.Invoke(request.Status == PermissionStatus.Allowed);
        }
        /// <inheritdoc/>
        public void Replace(IReadOnlyList<PlannedNotification> plan)
        {
            AndroidNotificationCenter.CancelAllScheduledNotifications();
            AndroidNotificationCenter.CancelAllDisplayedNotifications();
            foreach (var p in plan)
            {
                var n = new AndroidNotification(p.Title, p.Body, p.Fire) { SmallIcon = SmallIcon, LargeIcon = LargeIcon };
                AndroidNotificationCenter.SendNotificationWithExplicitID(n, p.Channel, p.Id);
            }
        }
        /// <inheritdoc/>
        public void CancelAll() { AndroidNotificationCenter.CancelAllScheduledNotifications(); }

        // Hidden object that runs the permission coroutine.
        private sealed class Runner : MonoBehaviour
        {
            private static Runner instance;
            public static Runner Instance { get { if (instance == null) { var go = new GameObject("Notification permission"); DontDestroyOnLoad(go); go.hideFlags = HideFlags.HideAndDontSave; instance = go.AddComponent<Runner>(); } return instance; } }
        }
#else
        /// <inheritdoc/>
        public bool IsSupported => false;
        /// <inheritdoc/>
        public NotificationPermission Permission => NotificationPermission.NotSupported;
        /// <inheritdoc/>
        public void RequestPermission(Action<bool> done) => done?.Invoke(false);
        /// <inheritdoc/>
        public void Replace(IReadOnlyList<PlannedNotification> plan) { }
        /// <inheritdoc/>
        public void CancelAll() { }
#endif
    }
}
