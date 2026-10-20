using ArrowEscape.Core;
namespace ArrowEscape.Data
{
    /// <summary>Preferences storage boundary for disk or test doubles.</summary>
    public interface ISettingsStore
    {
        /// <summary>Returns settings and a nonthrowing load diagnostic.</summary>
        SettingsLoadResult Load();
        /// <summary>Attempts to save without throwing.</summary>
        bool TrySave(SettingsData data);
    }
}
