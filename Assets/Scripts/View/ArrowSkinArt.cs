using System;
using UnityEngine;
namespace ArrowEscape.View
{
    /// <summary>Imported parts of one reference arrow; source files are never changed.</summary>
    [Serializable] public sealed class ArrowSkinArt
    {
        /// <summary>Permanent catalog or supplementary reference id.</summary>
        public string Id;
        /// <summary>Complete right-facing reference.</summary>
        public Sprite Full;
        /// <summary>Art at the leading end.</summary>
        public Sprite Head;
        /// <summary>Fletching at the trailing end.</summary>
        public Sprite Tail;
        /// <summary>Seamless body texture.</summary>
        public Texture2D Shaft;
        /// <summary>Optional sheet-B icon.</summary>
        public Sprite Icon;
    }
}
