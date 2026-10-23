using System.Text;
namespace ArrowEscape.Core
{
    /// <summary>Profile names: up to 16 letters, digits, spaces and . - _ ; surrounding spaces trimmed.</summary>
    public static class PlayerNameRules
    {
        /// <summary>Longest allowed name.</summary>
        public const int MaxLength = 16;
        /// <summary>Shown while the player has not chosen a name.</summary>
        public const string DefaultName = "Player";

        /// <summary>Removes unsupported characters and limits the length.</summary>
        public static string Clean(string name)
        {
            if (string.IsNullOrEmpty(name)) return "";
            var sb = new StringBuilder();
            foreach (char c in name)
            {
                bool ok = (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9') || c == ' ' || c == '.' || c == '-' || c == '_';
                if (!ok) continue;
                if (c == ' ' && (sb.Length == 0 || sb[sb.Length - 1] == ' ')) continue;
                sb.Append(c);
                if (sb.Length >= MaxLength) break;
            }
            return sb.ToString().Trim();
        }

        /// <summary>The name to display (default when empty).</summary>
        public static string Display(string name) { string clean = Clean(name); return clean.Length == 0 ? DefaultName : clean; }
    }
}
