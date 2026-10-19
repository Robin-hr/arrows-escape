using System;
using System.Collections.Generic;

namespace ArrowEscape.Core
{
    /// <summary>A loaded board or a read-only list of validation errors.</summary>
    public sealed class LevelLoadResult
    {
        /// <summary>Whether a board was successfully built.</summary>
        public bool Success { get; }
        /// <summary>The built board, or null on failure.</summary>
        public Board Board { get; }
        /// <summary>Validation errors; empty on success.</summary>
        public IReadOnlyList<string> Errors { get; }
        /// <summary>The level's display name, or an empty string if unavailable.</summary>
        public string LevelName { get; }

        private LevelLoadResult(Board board, string levelName, List<string> errors)
        {
            Success = board != null;
            Board = board;
            LevelName = levelName ?? string.Empty;
            Errors = errors.AsReadOnly();
        }

        /// <summary>Creates a successful result for a non-null board.</summary>
        public static LevelLoadResult Ok(Board board, string levelName)
        {
            if (board == null) throw new ArgumentNullException(nameof(board));
            return new LevelLoadResult(board, levelName, new List<string>());
        }

        /// <summary>Creates a failure, copying errors and ensuring at least one message.</summary>
        public static LevelLoadResult Fail(IEnumerable<string> errors, string levelName = null)
        {
            var messages = new List<string>();
            if (errors != null)
                foreach (string error in errors)
                    if (!string.IsNullOrWhiteSpace(error)) messages.Add(error);
            if (messages.Count == 0) messages.Add("The level could not be loaded.");
            return new LevelLoadResult(null, levelName, messages);
        }
    }
}
