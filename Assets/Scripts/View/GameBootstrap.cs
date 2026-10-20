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
        /// <summary>Independent economic persistence, never part of progress.json.</summary>
        public InventoryService Inventory { get; private set; }
        /// <summary>Monotonic receipt issued when this attempt starts.</summary>
        public int CurrentAttemptId { get; private set; }
        /// <summary>Coins earned by the current completed attempt, zero for sandbox.</summary>
        public int LastCoinReward { get; private set; }

        private string sandboxPath;
        /// <summary>Whether this attempt comes from the non-progress test sandbox.</summary>
        public bool IsSandboxLevel { get; private set; }
        /// <summary>Loads a test resource without remembering or recording player progress.</summary>
        public bool TryStartSandboxLevel(string resourcePath)
        {
            EnsureInitialized();LevelLoadResult result=LevelRepository.LoadFromResources(resourcePath);
            if(!result.Success){Debug.LogWarning("ArrowEscape sandbox: "+string.Join("; ",result.Errors),this);return false;}
            sandboxPath=resourcePath;IsSandboxLevel=true;controller.SetLevelInfo(result.LevelName,resourcePath);
            controller.StartLevel(result.Board,result.LevelName);GetComponent<CameraFitter>().Fit(result.Board.Width,result.Board.Height);return true;
        }
        private void LeaveSandbox(){if(!IsSandboxLevel)return;IsSandboxLevel=false;sandboxPath=null;CurrentLevelIndex=-1;}
        [ContextMenu("Debug: Sandbox Path 1")]private void SandboxPathOne(){if(Application.isPlaying)TryStartSandboxLevel("TestLevels/path_001");}
        [ContextMenu("Debug: Sandbox Path 2")]private void SandboxPathTwo(){if(Application.isPlaying)TryStartSandboxLevel("TestLevels/path_002");}
        [ContextMenu("Debug: Sandbox Path 3")]private void SandboxPathThree(){if(Application.isPlaying)TryStartSandboxLevel("TestLevels/path_003");}
        [ContextMenu("Debug: Sandbox Ice 1")]private void SandboxOne(){if(Application.isPlaying)TryStartSandboxLevel("TestLevels/ice_001");}
        [ContextMenu("Debug: Sandbox Ice 2")]private void SandboxTwo(){if(Application.isPlaying)TryStartSandboxLevel("TestLevels/ice_002");}
        [ContextMenu("Debug: Sandbox Ice 3")]private void SandboxThree(){if(Application.isPlaying)TryStartSandboxLevel("TestLevels/ice_003");}
        private IReadOnlyList<string> catalogOverride;
        /// <summary>Optionally injects ordered resource paths before initialization; null restores the real catalog.</summary>
        public void SetLevelCatalog(IReadOnlyList<string> resourcePaths)
        {
            if(IsInitialized)throw new InvalidOperationException("Set the catalog before initialization.");
            if(resourcePaths==null){catalogOverride=null;return;}
            var copy=new string[resourcePaths.Count];for(int i=0;i<copy.Length;i++)copy[i]=resourcePaths[i];
            catalogOverride=Array.AsReadOnly(copy);
        }
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
        public bool IsLastLevel => IsSandboxLevel || LevelCount > 0 && CurrentLevelIndex == LevelCount - 1;
        /// <summary>Whether another unlocked level exists after this one.</summary>
        public bool CanGoNext => !IsSandboxLevel && Progress != null && Progress.Tracker.IsUnlocked(levelIds, CurrentLevelIndex + 1) && CurrentLevelIndex >= 0;
        /// <summary>The loaded in-memory rules and their storage service.</summary>
        public ProgressService Progress { get; private set; }
        /// <summary>Summary retained for UI that subscribes after a clear.</summary>
        public CompletionResult LastCompletion { get; private set; }
        /// <summary>Whether all currently available ids have been completed.</summary>
        public bool AllLevelsCompleted => !IsSandboxLevel && LevelCount > 0 && Progress != null && Progress.Tracker.CompletedCount(levelIds) == LevelCount;
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
        public void Initialize(IProgressStore store, ISettingsStore settingsStore) => Initialize(store,settingsStore,new MemoryInventoryStore());

        /// <summary>Injects all three stores; Editor tests never access player files.</summary>
        public void Initialize(IProgressStore store,ISettingsStore settingsStore,IInventoryStore inventoryStore)
        {
            if(Inventory!=null)Inventory.Shop.EquippedChanged-=OnEquipped;
            Inventory=new InventoryService(inventoryStore);Inventory.Load();Inventory.Shop.EquippedChanged+=OnEquipped;
            ApplyEquipment();
            if (controller != null)
            { controller.BoardCleared -= OnCleared; controller.LevelStarted -= OnLevelStarted; }
            paths = catalogOverride ?? LevelRepository.GetLevelResourcePaths();
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
            LeaveSandbox();
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
        public void NextLevel() { if(IsSandboxLevel)return;EnsureInitialized(); if (CanGoNext) TryStartLevel(CurrentLevelIndex + 1); }
        /// <summary>Loads the previous level if it exists; never wraps.</summary>
        public void PreviousLevel() { int index=CurrentLevelIndex;bool sandbox=IsSandboxLevel;LeaveSandbox();if(sandbox)TryStartLevel(Math.Max(0,index-1));else if(index>0)TryStartLevel(index-1); }
        /// <summary>Restarts the current level from its JSON resource.</summary>
        [ContextMenu("Restart Level")]
        public void RestartLevel() { if(IsSandboxLevel){TryStartSandboxLevel(sandboxPath);return;}if (CurrentLevelIndex >= 0) TryStartLevel(CurrentLevelIndex); }
        /// <summary>Inspector-only style jump: wraps around and ignores locks.</summary>
        public void DebugJump(int delta)
        {
            int current=CurrentLevelIndex;LeaveSandbox();
            EnsureInitialized();
            if (LevelCount == 0) return;
            int index = (int)(((long)Math.Max(0, current) + delta) % LevelCount);
            ShowLevel((index + LevelCount) % LevelCount);
        }
        [ContextMenu("Next Level")] private void DebugNextLevel() => DebugJump(1);
        [ContextMenu("Previous Level")] private void DebugPreviousLevel() => DebugJump(-1);
        /// <summary>Clears persistent and in-memory progress, then starts the first level.</summary>
        [ContextMenu("Reset Progress")]
        public void ResetProgress()
        {
            LeaveSandbox();
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
                    new FileSettingsStore(Path.Combine(Application.persistentDataPath, "settings.json")),
                    new FileInventoryStore(Path.Combine(Application.persistentDataPath,"inventory.json")));
            else Initialize(new MemoryProgressStore(), new MemorySettingsStore());
        }
        /// <summary>Starts an existing unlocked level; invalid requests leave the current attempt untouched.</summary>
        public bool TryStartLevel(int index)
        {
            LeaveSandbox();
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
            LastCoinReward=0;CurrentAttemptId=Inventory.Shop.BeginAttempt();ApplyEquipment();
            if(IsSandboxLevel)return;
            if (string.IsNullOrEmpty(CurrentLevelId)) return;
            if(Inventory.LastSaveFailed&&!Inventory.Save())return;
            Progress.RememberLevel(CurrentLevelId);
            WarnSaveFailure();
        }
        private void OnCleared()
        {
            if(IsSandboxLevel)
            {
                if(clearRecorded||!controller.IsCleared)return;clearRecorded=true;
                LastCompletion=new CompletionResult(new CompletionResult(false,false,controller.MistakeCount,controller.UndoCount,0), StarRating.Stars(controller.TapCount, controller.Par), controller.TapCount, controller.Par, 0, false);
                LevelCompleted?.Invoke(LastCompletion);return;
            }
            if (clearRecorded || !controller.IsCleared || string.IsNullOrEmpty(CurrentLevelId)) return;
            clearRecorded = true;
            LastCoinReward=Inventory.AwardLevelClear(CurrentLevelId,StarRating.Stars(controller.TapCount,controller.Par),CurrentAttemptId);
            // Never commit progress ahead of a failed inventory write. Keep play available in memory.
            if(Inventory.LastSaveFailed)
            {
                LastCompletion=Progress.Tracker.RecordCompletion(CurrentLevelId,controller.MistakeCount,controller.UndoCount,controller.TapCount,controller.Par);
                Progress.Tracker.SetLastLevel(CurrentLevelId);
            }
            else LastCompletion = Progress.RecordCompletion(CurrentLevelId, controller.MistakeCount, controller.UndoCount, controller.TapCount, controller.Par);
            WarnSaveFailure();
            LevelCompleted?.Invoke(LastCompletion);
        }
        private void WarnSaveFailure()
        {
            if (!Progress.LastSaveFailed || saveWarningLogged) return;
            saveWarningLogged = true;
            Debug.LogWarning("ArrowEscape progress could not be saved. Play continues with in-memory progress.", this);
        }
        private void OnEquipped(ShopSlot slot,string id)=>ApplyEquipment();
        private void ApplyEquipment()
        {if(Inventory!=null)GetComponent<BoardView>().SetNormalArrowColor(ArrowTint.For(Inventory.State.EquippedArrowId,ViewTheme.Background));}
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        /// <summary>Development-only grant persisted with the normal inventory store.</summary>
        [ContextMenu("Debug: Add 1000 coins")]
        public void DebugAddCoins(){EnsureInitialized();Inventory.Shop.AddCoins(1000);Inventory.Save();}
        /// <summary>Backs up the inventory before an explicit development reset.</summary>
        [ContextMenu("Debug: Reset inventory (keeps a backup file)")]
        public void DebugResetInventory()
        {
            EnsureInitialized();var previous=Inventory.Shop;
            if(!Inventory.ResetWithBackup())return;
            previous.EquippedChanged-=OnEquipped;Inventory.Shop.EquippedChanged+=OnEquipped;ApplyEquipment();
            InventoryReset?.Invoke();
        }
#endif
        /// <summary>Notifies UI after a deliberate development reset swaps the economic state.</summary>
        public event Action InventoryReset;
        private void OnDestroy()
        {
            if(Inventory!=null)Inventory.Shop.EquippedChanged-=OnEquipped;
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
            bootstrap.gameObject.AddComponent<AudioDirector>();
            bootstrap.gameObject.AddComponent<EffectsController>();
            bootstrap.gameObject.AddComponent<EffectsDirector>();
            bootstrap.gameObject.AddComponent<HapticsDirector>();
            bootstrap.gameObject.AddComponent<EconomyFeedback>();
        }
    }
}
