using ArrowEscape.Core;
namespace ArrowEscape.Data
{
    /// <summary>Storage boundary for real disk or isolated in-memory tests.</summary>
    public interface IProgressStore
    {
        /// <summary>Reads progress without throwing for storage failures.</summary>
        ProgressLoadResult Load();
        /// <summary>Attempts to save progress, returning false on failure.</summary>
        bool TrySave(ProgressData data);
    }
}
