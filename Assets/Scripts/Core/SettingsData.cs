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
    }
}
