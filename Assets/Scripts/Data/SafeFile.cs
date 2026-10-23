using System;
using System.IO;

namespace ArrowEscape.Data
{
    /// <summary>
    /// Replaces a save file with its freshly written temporary copy. Uses the atomic <see cref="File.Replace(string,string,string)"/>
    /// where the platform supports it; on file systems without it (some Android/iOS storage) it falls back to
    /// overwrite-copy then delete, which still never leaves a half-written save (the temporary file is complete).
    /// </summary>
    public static class SafeFile
    {
        /// <summary>Moves <paramref name="temporary"/> over <paramref name="target"/>.</summary>
        public static void Replace(string temporary, string target)
        {
            try { File.Replace(temporary, target, null); }
            catch (PlatformNotSupportedException) { CopyOver(temporary, target); }
            catch (NotSupportedException) { CopyOver(temporary, target); }
            catch (IOException) when (File.Exists(temporary) && File.Exists(target)) { CopyOver(temporary, target); }
        }

        private static void CopyOver(string temporary, string target)
        {
            File.Copy(temporary, target, true);
            File.Delete(temporary);
        }
    }
}
