using System;
namespace ArrowEscape.Core
{
    /// <summary>Pure navigation rules; rendering and gameplay remain outside this class.</summary>
    public sealed class ScreenStateMachine
    {
        /// <summary>The active screen, initially MainMenu.</summary>
        public ScreenState Current { get; private set; } = ScreenState.MainMenu;
        /// <summary>The screen to restore when settings closes.</summary>
        public ScreenState SettingsReturnState { get; private set; } = ScreenState.MainMenu;
        /// <summary>Raised after every real transition.</summary>
        public event Action<ScreenState, ScreenState> StateChanged;
        /// <summary>Opens level selection from the menu or completion.</summary>
        public bool OpenLevelSelect() => (Current == ScreenState.MainMenu || Current == ScreenState.LevelComplete) && Change(ScreenState.LevelSelect);
        /// <summary>Opens settings and remembers its menu or pause origin.</summary>
        public bool OpenSettings()
        {
            if (Current != ScreenState.MainMenu && Current != ScreenState.Paused) return false;
            SettingsReturnState = Current;
            return Change(ScreenState.Settings);
        }
        /// <summary>Starts or restarts play from an allowed screen.</summary>
        public bool StartPlaying()
        {
            if (Current == ScreenState.Settings) return false;
            return Change(ScreenState.Playing);
        }
        /// <summary>Pauses only active play.</summary>
        public bool Pause() => Current == ScreenState.Playing && Change(ScreenState.Paused);
        /// <summary>Resumes only paused play.</summary>
        public bool Resume() => Current == ScreenState.Paused && Change(ScreenState.Playing);
        /// <summary>Finishes only active play.</summary>
        public bool LevelCompleted() => Current == ScreenState.Playing && Change(ScreenState.LevelComplete);
        /// <summary>Returns from selection, pause, or completion to the menu.</summary>
        public bool ToMainMenu() => (Current == ScreenState.LevelSelect || Current == ScreenState.Paused || Current == ScreenState.LevelComplete) && Change(ScreenState.MainMenu);
        /// <summary>Applies platform Back/Escape behavior.</summary>
        public bool Back()
        {
            switch (Current)
            {
                case ScreenState.LevelSelect: return Change(ScreenState.MainMenu);
                case ScreenState.Settings: return Change(SettingsReturnState);
                case ScreenState.Playing: return Pause();
                case ScreenState.Paused: return Resume();
                case ScreenState.LevelComplete: return OpenLevelSelect();
                default: return false;
            }
        }
        private bool Change(ScreenState next)
        {
            if (Current == next) return true;
            ScreenState previous = Current; Current = next;
            StateChanged?.Invoke(previous, next);
            return true;
        }
    }
}
