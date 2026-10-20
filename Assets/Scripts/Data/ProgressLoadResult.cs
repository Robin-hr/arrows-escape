using ArrowEscape.Core;
namespace ArrowEscape.Data
{
    /// <summary>Non-null load payload and diagnostic information; this object never logs.</summary>
    public sealed class ProgressLoadResult
    {
        /// <summary>Loaded or empty progress.</summary>
        public ProgressData Data { get; }
        /// <summary>Load outcome.</summary>
        public ProgressLoadStatus Status { get; }
        /// <summary>Human-readable details, including the backup path for invalid data.</summary>
        public string Message { get; }
        /// <summary>Creates a load result with safe defaults.</summary>
        public ProgressLoadResult(ProgressData data, ProgressLoadStatus status, string message = "")
        { Data = data ?? new ProgressData(); Status = status; Message = message ?? ""; }
    }
}
