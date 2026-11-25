using System;
namespace ArrowEscape.Core
{
    /// <summary>Observable preference values; unchanged assignments raise no event.</summary>
    public sealed class SettingsModel
    {
        /// <summary>Payload used by the settings store.</summary>
        public SettingsData Data { get; }
        /// <summary>Raised only when a preference really changes.</summary>
        public event Action Changed;
        /// <summary>Wraps a payload, accepting null as defaults.</summary>
        public SettingsModel(SettingsData data = null) => Data = data ?? new SettingsData();
        /// <summary>Sound preference.</summary>
        public bool SoundEnabled { get => Data.soundEnabled; set { if (value == Data.soundEnabled) return; Data.soundEnabled = value; Changed?.Invoke(); } }
        /// <summary>Haptics preference.</summary>
        public bool HapticsEnabled { get => Data.hapticsEnabled; set { if (value == Data.hapticsEnabled) return; Data.hapticsEnabled = value; Changed?.Invoke(); } }
        /// <summary>Step 17: night board preference.</summary>
        public bool NightMode { get => Data.nightMode; set { if (value == Data.nightMode) return; Data.nightMode = value; Changed?.Invoke(); } }
        /// <summary>Step 17: reminder notifications preference.</summary>
        public bool NotificationsEnabled { get => Data.notificationsEnabled; set { if (value == Data.notificationsEnabled) return; Data.notificationsEnabled = value; Changed?.Invoke(); } }
        /// <summary>Step 17: the notification soft prompt has been answered.</summary>
        public bool NotificationsAsked { get => Data.notificationsAsked; set { if (value == Data.notificationsAsked) return; Data.notificationsAsked = value; Changed?.Invoke(); } }
    }
}
