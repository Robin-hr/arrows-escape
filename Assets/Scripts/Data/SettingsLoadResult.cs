using ArrowEscape.Core;
namespace ArrowEscape.Data
{
    /// <summary>Settings data with the same storage status vocabulary as progress.</summary>
    public sealed class SettingsLoadResult
    {
        /// <summary>Loaded preferences, or true/true defaults on failure.</summary>
        public SettingsData Data { get; }
        /// <summary>Loaded, missing, corrupt, future, or unreadable.</summary>
        public ProgressLoadStatus Status { get; }
        /// <summary>Diagnostic and backup name when relevant.</summary>
        public string Message { get; }
        /// <summary>Creates a result, substituting defaults for null.</summary>
        public SettingsLoadResult(SettingsData data, ProgressLoadStatus status, string message = "")
        { Data = data ?? new SettingsData(); Status = status; Message = message ?? ""; }
    }
}
