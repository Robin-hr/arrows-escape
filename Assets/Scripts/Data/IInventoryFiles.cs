namespace ArrowEscape.Data
{
    /// <summary>File operations injectable as an in-memory fake; tests never touch player files.</summary>
    public interface IInventoryFiles
    {
        /// <summary>Checks for an existing file.</summary>
        bool Exists(string path);
        /// <summary>Reads UTF-8 JSON.</summary>
        string Read(string path);
        /// <summary>Writes UTF-8 JSON without a BOM.</summary>
        void Write(string path,string text);
        /// <summary>Creates the parent directory.</summary>
        void CreateDirectory(string path);
        /// <summary>Renames without overwriting another file.</summary>
        void Move(string source,string target);
        /// <summary>Atomically replaces an existing target.</summary>
        void Replace(string source,string target);
        /// <summary>Deletes only a temporary file.</summary>
        void Delete(string path);
        /// <summary>Copies to a new backup path.</summary>
        void Copy(string source,string target);
    }
}
