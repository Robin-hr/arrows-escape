namespace ArrowEscape.Data
{
    /// <summary>Storage outcomes, separate from the pure progress rules.</summary>
    public enum ProgressLoadStatus
    {
        /// <summary>Supported data was read.</summary>
        Loaded,
        /// <summary>No save exists yet.</summary>
        NoFile,
        /// <summary>The payload is invalid.</summary>
        Corrupt,
        /// <summary>A future format cannot be interpreted.</summary>
        NewerVersion,
        /// <summary>The file or its required backup could not be read or copied.</summary>
        ReadError
    }
}
