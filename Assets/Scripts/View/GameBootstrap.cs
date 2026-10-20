using System;
using System.Collections.Generic;
using System.IO;
using ArrowEscape.Core;
using ArrowEscape.Data;
using UnityEngine;

namespace ArrowEscape.View
{
    /// <summary>Loads levels, records clears, and owns the progress service for this runtime.</summary>
    [RequireComponent(typeof(BoardView), typeof(CameraFitter))]
    [RequireComponent(typeof(TapController), typeof(PointerInputAdapter))]
    [RequireComponent(typeof(GameController), typeof(ArrowAnimator))]
    [RequireComponent(typeof(GuideController))]
    public sealed class GameBootstrap : MonoBehaviour
    {
        [SerializeField] private int startLevelIndex = 0;
        [SerializeField] private bool useSavedProgress = true;
        [SerializeField] private bool autoStartOnStart = true;
        /// <summary>Loaded preferences, separate from progress.</summary>
        public SettingsService Settings { get; private set; }
        /// <summary>Whether both stores have been loaded.</summary>
        public bool IsInitialized => Progress != null && Settings != null;
        /// <summary>Ordered stable ids used by level selection.</summary>
        public IReadOnlyList<string> LevelIds => Array.AsReadOnly(levelIds);
        /// <summary>The first incomplete level, or zero when there are none.</summary>
        public int ContinueIndex => Progress == null ? 0 : Math.Max(0, Progress.Tracker.GetResumeIndex(levelIds));
        /// <summary>Whether the player has a remembered start or any completion.</summary>
        public bool HasAnyProgress => Progress != null && (!string.IsNullOrEmpty(Progress.Tracker.Data.lastLevelId) || Progress.Tracker.Data.levels.Exists(record => record.completions > 0));
        /// <summary>Raised after explicit reset and restart so menus can refresh.</summary>
        public event Action ProgressReset;
        private bool settingsWarningLogged;
        private IReadOnlyList<string> paths = Array.Empty<string>();
        private string[] levelIds = Array.Empty<string>();
        private GameController controller;
        private bool clearRecorded, saveWarningLogged;
        /// <summary>Current zero-based index, or -1 before loading.</summary>
        public int CurrentLevelIndex { get; private set; } = -1;
        /// <summary>Number of available resource levels.</summary>
        public int LevelCount => paths.Count;
        /// <summary>Stable id of the current level.</summary>
        public string CurrentLevelId => CurrentLevelIndex >= 0 && CurrentLevelIndex < levelIds.Length ? levelIds[CurrentLevelIndex] : "";
        /// <summary>Whether the current level is the final resource level.</summary>
        public bool IsLastLevel => LevelCount > 0 && CurrentLevelIndex == LevelCount - 1;
        /// <summary>Whether another unlocked level exists after this one.</summary>
        public bool CanGoNext => Progress != null && Progress.Tracker.IsUnlocked(levelIds, CurrentLevelIndex + 1) && CurrentLevelIndex >= 0;
        /// <summary>The loaded in-memory rules and their storage service.</summary>
        public ProgressService Progress { get; private set; }
        /// <summary>Summary retained for UI that subscribes after a clear.</summary>
        public CompletionResult LastCompletion { get; private set; }
        /// <summary>Whether all currently available ids have been completed.</summary>
        public bool AllLevelsCompleted => LevelCount > 0 && Progress != null && Progress.Tracker.CompletedCount(levelIds) == LevelCount;
        /// <summary>Raised once after recording and saving a clear.</summary>
        public event Action<CompletionResult> LevelCompleted;

        private void Awake()
        {
            TapController taps = GetComponent<TapController>();
            taps.Bind(GetComponent<BoardView>());
            GetComponent<PointerInputAdapter>().Bind(taps);
            GetComponent<GameController>().BindGuide(GetComponent<GuideController>());
        }
        private void Start()
        {
            EnsureInitialized();
            if (autoStartOnStart) StartInitialLevel();
        }

        /// <summary>Injects storage before starting; tests use this hook and never access the real save path.</summary>
        public void Initialize(IProgressStore store) => Initialize(store, new MemorySettingsStore());

        /// <summary>Injects both independent stores before starting; tests never use the real save folder.</summary>
        public void Initialize(IProgressStore store, ISettingsStore settingsStore)
        {
            if (controller != null)
            { controller.BoardCleared -= OnCleared; controller.LevelStarted -= OnLevelStarted; }
            paths = LevelRepository.GetLevelResourcePaths();
            levelIds = new string[paths.Count];
            for (int i = 0; i < paths.Count; i++) levelIds[i] = ProgressTracker.ToLevelId(paths[i]);
            Progress = new ProgressService(store);
            Progress.Load();
            Settings = new SettingsService(settingsStore);
            Settings.Load();
            settingsWarningLogged = false;
            var settingsLoad = Settings.LoadResult;
            if (settingsLoad.Status != ProgressLoadStatus.Loaded && settingsLoad.Status != ProgressLoadStatus.NoFile)
                Debug.LogWarning("ArrowEscape settings: " + settingsLoad.Message, this);
            GetComponent<CameraFitter>().ApplyBackground();
            CurrentLevelIndex = -1;
            LastCompletion = null;
            saveWarningLogged = false;
            controller = GetComponent<GameController>();
            controller.BoardCleared += OnCleared;
            controller.LevelStarted += OnLevelStarted;
            ProgressLoadResult load = Progress.LoadResult;
            if (load.Status != ProgressLoadStatus.Loaded && load.Status != ProgressLoadStatus.NoFile)
                Debug.LogWarning("ArrowEscape progress: " + load.Message +
                    " Invalid readable saves use progress.corrupt.json or progress.newer.json backups.", this);
        }

        /// <summary>Starts the resume level, or the Inspector index when saved progress is disabled.</summary>
        public void StartInitialLevel()
        {
            EnsureInitialized();
            int index = useSavedProgress ? Progress.Tracker.GetResumeIndex(levelIds) : startLevelIndex;
            if (index >= 0) ShowLevel(index);
        }

        /// <summary>Loads an explicit zero-based resource index; also supports debugging locked levels.</summary>
        public void ShowLevel(int index)
        {
            EnsureInitialized();
            BoardView view = GetComponent<BoardView>();
            view.Clear();
            if (index < 0 || index >= paths.Count)
            {
                Debug.LogError($"ArrowEscape: level index {index} is unavailable ({paths.Count} levels found).", this);
                return;
            }
            LevelLoadResult result = LevelRepository.LoadFromResources(paths[index]);
            if (!result.Success)
            { foreach (string error in result.Errors) Debug.LogError(error, this); return; }
            CurrentLevelIndex = index;
            controller.SetLevelInfo(result.LevelName, paths[index]);
            controller.StartLevel(result.Board, result.LevelName);
            GetComponent<CameraFitter>().Fit(result.Board.Width, result.Board.Height);
        }

        /// <summary>Loads the next unlocked level; never wraps or skips a lock.</summary>
        public void NextLevel() { EnsureInitialized(); if (CanGoNext) TryStartLevel(CurrentLevelIndex + 1); }
        /// <summary>Loads the previous level if it exists; never wraps.</summary>
        public void PreviousLevel() { if (CurrentLevelIndex > 0) TryStartLevel(CurrentLevelIndex - 1); }
        /// <summary>Restarts the current level from its JSON resource.</summary>
        [ContextMenu("Restart Level")]
        public void RestartLevel() { if (CurrentLevelIndex >= 0) TryStartLevel(CurrentLevelIndex); }
        /// <summary>Inspector-only style jump: wraps around and ignores locks.</summary>
        public void DebugJump(int delta)
        {
            EnsureInitialized();
            if (LevelCount == 0) return;
            int index = (int)(((long)Math.Max(0, CurrentLevelIndex) + delta) % LevelCount);
            ShowLevel((index + LevelCount) % LevelCount);
        }
        [ContextMenu("Next Level")] private void DebugNextLevel() => DebugJump(1);
        [ContextMenu("Previous Level")] private void DebugPreviousLevel() => DebugJump(-1);
        /// <summary>Clears persistent and in-memory progress, then starts the first level.</summary>
        [ContextMenu("Reset Progress")]
        public void ResetProgress()
        {
            EnsureInitialized(); Progress.ResetAll(); WarnSaveFailure();
            if (LevelCount > 0 && controller.Session != null && !controller.IsCleared) TryStartLevel(0);
            ProgressReset?.Invoke();
        }

        /// <summary>Loads both services once; only play-mode defaults open the application save folder.</summary>
        public void EnsureInitialized()
        {
            if (IsInitialized) return;
            if (Application.isPlaying)
                Initialize(new FileProgressStore(Path.Combine(Application.persistentDataPath, "progress.json")),
                    new FileSettingsStore(Path.Combine(Application.persistentDataPath, "settings.json")));
            else Initialize(new MemoryProgressStore(), new MemorySettingsStore());
        }
        /// <summary>Starts an existing unlocked level; invalid requests leave the current attempt untouched.</summary>
        public bool TryStartLevel(int index)
        {
            EnsureInitialized();
            if (!Progress.Tracker.IsUnlocked(levelIds, index)) return false;
            ShowLevel(index);
            return CurrentLevelIndex == index && controller.Session != null;
        }
        /// <summary>Logs a failed preference write at most once while allowing play to continue.</summary>
        public void CheckSettingsSave()
        {
            if (!Settings.LastSaveFailed || settingsWarningLogged) return;
            settingsWarningLogged = true;
            Debug.LogWarning("ArrowEscape settings could not be saved. Preferences remain available in memory.", this);
        }
        private void OnLevelStarted()
        {
            clearRecorded = false;
            LastCompletion = null;
            if (string.IsNullOrEmpty(CurrentLevelId)) return;
            Progress.RememberLevel(CurrentLevelId);
            WarnSaveFailure();
        }
        private void OnCleared()
        {
            if (clearRecorded || !controller.IsCleared || string.IsNullOrEmpty(CurrentLevelId)) return;
            clearRecorded = true;
            LastCompletion = Progress.RecordCompletion(CurrentLevelId, controller.MistakeCount, controller.UndoCount);
            WarnSaveFailure();
            LevelCompleted?.Invoke(LastCompletion);
        }
        private void WarnSaveFailure()
        {
            if (!Progress.LastSaveFailed || saveWarningLogged) return;
            saveWarningLogged = true;
            Debug.LogWarning("ArrowEscape progress could not be saved. Play continues with in-memory progress.", this);
        }
        private void OnDestroy()
        {
            if (controller != null)
            { controller.BoardCleared -= OnCleared; controller.LevelStarted -= OnLevelStarted; }
            SpriteFactory.ClearCache();
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void CreateRuntimeRoot()
        {
            if (UnityEngine.Object.FindFirstObjectByType<GameBootstrap>(FindObjectsInactive.Include) != null) return;
            var bootstrap = new GameObject("ArrowEscape").AddComponent<GameBootstrap>();
            bootstrap.autoStartOnStart = false;
        }
    }
}
