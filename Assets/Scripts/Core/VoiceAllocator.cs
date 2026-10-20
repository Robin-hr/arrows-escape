using System;
using System.Collections.Generic;

namespace ArrowEscape.Core
{
    /// <summary>Chooses an idle voice, or steals the next voice in round-robin order.</summary>
    public static class VoiceAllocator
    {
        /// <summary>Searches just after lastIndex; invalid indices mean nothing has played yet.</summary>
        public static int Pick(IReadOnlyList<bool> busy, int lastIndex)
        {
            if (busy == null) throw new ArgumentNullException(nameof(busy));
            if (busy.Count == 0) throw new ArgumentOutOfRangeException(nameof(busy));
            if (lastIndex < -1 || lastIndex >= busy.Count) lastIndex = -1;
            int first = (lastIndex + 1) % busy.Count;
            for (int offset = 0; offset < busy.Count; offset++)
            {
                int index = (int)(((long)first + offset) % busy.Count);
                if (!busy[index]) return index;
            }
            return first;
        }
    }
}
