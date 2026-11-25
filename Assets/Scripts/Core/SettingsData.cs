namespace ArrowEscape.Core
{
    /// <summary>Separate version-one preferences, without playback or haptic behavior.</summary>
    [System.Serializable]
    public sealed class SettingsData
    {
        /// <summary>Supported settings format.</summary>
        public const int CurrentVersion = 1;
        /// <summary>Serialized format version.</summary>
        public int version = CurrentVersion;
        /// <summary>Preference reserved for future sound playback.</summary>
        public bool soundEnabled = true;
        /// <summary>Preference reserved for future haptic feedback.</summary>
        public bool hapticsEnabled = true;
        /// <summary>Step 17: night board (light arrows on a dark board where the arrow style allows it).</summary>
        public bool nightMode = false;
        /// <summary>Step 17: daily puzzle, streak and special-offer reminders (missing in old files = on).</summary>
        public bool notificationsEnabled = true;
        /// <summary>Step 17: true once the player answered the notification soft prompt.</summary>
        public bool notificationsAsked = false;
    }
}
