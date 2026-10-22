using ArrowEscape.Core;
using ArrowEscape.View;
using UnityEngine;
using UnityEngine.InputSystem;
namespace ArrowEscape.UI
{
    /// <summary>Single-scene screen glue; only the pure machine decides navigation.</summary>
    public sealed class AppFlowController : MonoBehaviour
    {
        private GameBootstrap bootstrap;
        private GameController game;
        private BoardView board;
        private HudController hud;
        private PointerInputAdapter input;
        private System.Func<Vector2, bool> screenBlocker;
        private bool initialized;
        private ShopService wallet;
        /// <summary>Inventory shop screen; the existing navigation machine remains the parent flow.</summary>
        public ShopView Shop { get; private set; }
        /// <summary>The pure navigation owner, initially MainMenu.</summary>
        public ScreenStateMachine Machine { get; } = new ScreenStateMachine();
        /// <summary>Main menu view.</summary>
        public MainMenuView MainMenu { get; private set; }
        /// <summary>Scrollable level selection.</summary>
        public LevelSelectView Levels { get; private set; }
        /// <summary>Pause overlay.</summary>
        public PauseView PauseScreen { get; private set; }
        /// <summary>Preferences view.</summary>
        public SettingsView SettingsScreen { get; private set; }
        /// <summary>Topmost confirmation modal.</summary>
        public ConfirmDialog Confirm { get; private set; }
        /// <summary>The independent menus canvas.</summary>
        public Canvas MenusCanvas { get; private set; }
        /// <summary>Neon profile page, opened from the home page header.</summary>
        public ProfileView Profile { get; private set; }
        /// <summary>Snake-arrow loading screens (startup 3 s, level start 1 s, profile 2 s).</summary>
        public LoadingOverlay Loading { get; private set; }
        /// <summary>Loading screens run in the player and Play mode; Edit Mode tests keep instant flow unless enabled.</summary>
        public bool LoadingScreensEnabled { get => loadingScreens ?? Application.isPlaying; set => loadingScreens = value; }
        private bool? loadingScreens;

        private void Start()
        {
            if (initialized) return;
            var boot = Object.FindFirstObjectByType<GameBootstrap>();
            if (boot == null)
            { Debug.LogWarning("ArrowEscape menus: GameBootstrap is missing.", this); enabled = false; return; }
            var display = Object.FindFirstObjectByType<HudController>();
            if (display == null) display = gameObject.AddComponent<HudController>();
            Initialize(boot, display);
        }
        /// <summary>Explicit startup hook for tests with both stores already injected.</summary>
        public void Initialize(GameBootstrap boot, HudController display)
        {
            if (initialized || boot == null || display == null) return;
            initialized = true;
            bootstrap = boot; bootstrap.EnsureInitialized();
            game = boot.GetComponent<GameController>(); board = boot.GetComponent<BoardView>();
            input = boot.GetComponent<PointerInputAdapter>(); hud = display;
            hud.Bind(game, bootstrap);
            EventSystemEnsurer.Ensure();
            BuildScreens();
            screenBlocker = IsBlocked; input.AddBlocker(screenBlocker);
            Machine.StateChanged += OnStateChanged;
            hud.PauseRequested += OnPauseRequested;
            hud.CompletePanel.LevelsRequested += OnLevelsRequested;
            bootstrap.LevelCompleted += OnLevelCompleted;
            bootstrap.ProgressReset += OnProgressReset;
            bootstrap.InventoryReset += BindInventory;
            BindInventory();
            game.LevelStarted += OnLevelStarted;
            ApplyScreen(ScreenState.MainMenu, ScreenState.MainMenu);
            // An explicitly auto-started scene component keeps its existing play behavior.
            if (game.Session != null) Machine.StartPlaying();
            if (LoadingScreensEnabled) Loading.Show(LoadingKind.Startup, null, MainMenu.PlayEntrance);
        }
        private void BuildScreens()
        {
            var canvasObject = new GameObject("ArrowEscapeMenus", typeof(RectTransform), typeof(Canvas), typeof(UnityEngine.UI.CanvasScaler), typeof(UnityEngine.UI.GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false); canvasObject.transform.SetAsLastSibling();
            MenusCanvas = canvasObject.GetComponent<Canvas>(); MenusCanvas.renderMode = RenderMode.ScreenSpaceOverlay; MenusCanvas.sortingOrder = 100;
            var scaler = canvasObject.GetComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920); scaler.matchWidthOrHeight = 0.5f;
            MainMenu = gameObject.AddComponent<MainMenuView>(); MainMenu.Build(canvasObject.transform);
            Levels = gameObject.AddComponent<LevelSelectView>(); Levels.Build(canvasObject.transform, StartLevel, Back);
            PauseScreen = gameObject.AddComponent<PauseView>(); PauseScreen.Build(canvasObject.transform);
            SettingsScreen = gameObject.AddComponent<SettingsView>();
            SettingsScreen.Build(canvasObject.transform, bootstrap.Settings.Model, SetSound, SetHaptics, AskReset, Back);
            Confirm = gameObject.AddComponent<ConfirmDialog>(); Confirm.Build(canvasObject.transform);
            Shop=gameObject.AddComponent<ShopView>();Shop.Build(canvasObject.transform,Confirm,game);Shop.Closed+=OnShopClosed;
            Profile = gameObject.AddComponent<ProfileView>(); Profile.Build(canvasObject.transform, CloseProfile);
            Loading = gameObject.AddComponent<LoadingOverlay>(); Loading.Build(canvasObject.transform);
            MainMenu.ProfileButton.onClick.AddListener(OpenProfile);
            MainMenu.StarsButton.onClick.AddListener(OnLevelsRequested);
            MainMenu.ShopButton.onClick.AddListener(OpenShop);MainMenu.CoinsButton.onClick.AddListener(OpenShop);
            MainMenu.PlayButton.onClick.AddListener(Continue);
            MainMenu.LevelsButton.onClick.AddListener(OnLevelsRequested);
            MainMenu.SettingsButton.onClick.AddListener(OpenSettings);
            MainMenu.QuitButton.onClick.AddListener(Quit);
            PauseScreen.ResumeButton.onClick.AddListener(Resume);
            PauseScreen.RestartButton.onClick.AddListener(Restart);
            PauseScreen.SettingsButton.onClick.AddListener(OpenSettings);
            PauseScreen.MainMenuButton.onClick.AddListener(ToMainMenu);
        }
        private bool IsBlocked(Vector2 position) => Machine.Current != ScreenState.Playing || Confirm.IsOpen || (Loading != null && Loading.IsVisible);
        private void OnStateChanged(ScreenState oldState, ScreenState newState) => ApplyScreen(oldState, newState);
        private void ApplyScreen(ScreenState previous, ScreenState state)
        {
            if(state!=ScreenState.MainMenu&&Shop.IsVisible)Shop.Close();
            if (state != ScreenState.MainMenu && Profile.IsVisible) Profile.Hide();
            MainMenu.Root.gameObject.SetActive(state == ScreenState.MainMenu&&!Shop.IsVisible&&!Profile.IsVisible);
            Levels.Root.gameObject.SetActive(state == ScreenState.LevelSelect);
            PauseScreen.Root.gameObject.SetActive(state == ScreenState.Paused);
            SettingsScreen.Root.gameObject.SetActive(state == ScreenState.Settings);
            bool settingsPaused = state == ScreenState.Settings && Machine.SettingsReturnState == ScreenState.Paused;
            bool showBoard = state == ScreenState.Playing || state == ScreenState.Paused || state == ScreenState.LevelComplete || settingsPaused;
            game.SetPaused(state != ScreenState.Playing && state != ScreenState.LevelComplete);
            if (state == ScreenState.MainMenu && previous == ScreenState.Paused) game.DiscardAttempt();
            board.SetVisible(showBoard); hud.SetVisible(showBoard);
            if (state != ScreenState.Settings) Confirm.Hide();
            if (state == ScreenState.MainMenu) RefreshMainMenu();
            if (state == ScreenState.LevelSelect) RefreshLevels();
        }
        private void RefreshMainMenu()
        {
            MainMenu.Refresh(bootstrap.HasAnyProgress,bootstrap.Progress.Tracker.CompletedCount(bootstrap.LevelIds),bootstrap.LevelCount,bootstrap.ContinueIndex);
            MainMenu.SetTheme(bootstrap.Inventory.State.EquippedThemeId);
            MainMenu.SetEconomy(bootstrap.Inventory.State.Coins,bootstrap.Progress.Tracker.TotalStars,bootstrap.LevelCount*3,false);
        }
        private void BindInventory()
        {
            if(wallet!=null){wallet.CoinsChanged-=OnCoins;wallet.EquippedChanged-=OnThemeEquipped;}wallet=bootstrap.Inventory.Shop;wallet.CoinsChanged+=OnCoins;wallet.EquippedChanged+=OnThemeEquipped;
            Shop.Bind(bootstrap.Inventory);OnCoins(wallet.Inventory.Coins);
        }
        private void OnThemeEquipped(ShopSlot slot,string id){if(slot==ShopSlot.Theme)MainMenu.SetTheme(id);}
        private void OnCoins(int amount)=>MainMenu.SetEconomy(amount,bootstrap.Progress.Tracker.TotalStars,bootstrap.LevelCount*3);
        /// <summary>Opens the shop only from the main menu.</summary>
        public void OpenShop(){if(Machine.Current!=ScreenState.MainMenu)return;MainMenu.Root.gameObject.SetActive(false);Shop.Show();}
        /// <summary>Opens the profile page from the home page, behind a 2-second loading screen.</summary>
        public void OpenProfile()
        {
            if (Machine.Current != ScreenState.MainMenu || Shop.IsVisible) return;
            MainMenu.Root.gameObject.SetActive(false);
            Profile.Show(ProfileStats.Build(bootstrap.Progress.Tracker, bootstrap.LevelIds, bootstrap.Inventory.State));
            if (LoadingScreensEnabled) Loading.Show(LoadingKind.Profile, "LOADING PROFILE");
        }
        private void CloseProfile()
        {
            if (!Profile.IsVisible) return;
            Profile.Hide();
            MainMenu.Root.gameObject.SetActive(Machine.Current == ScreenState.MainMenu);
            if (Machine.Current == ScreenState.MainMenu) RefreshMainMenu();
        }
        private void OnShopClosed(){MainMenu.Root.gameObject.SetActive(Machine.Current==ScreenState.MainMenu);if(Machine.Current==ScreenState.MainMenu)RefreshMainMenu();}

        private void RefreshLevels() => Levels.Refresh(LevelSelectModel.Build(bootstrap.LevelIds, bootstrap.Progress.Tracker));
        private void Continue() => StartLevel(bootstrap.ContinueIndex);
        private void StartLevel(int index)
        {
            if (Machine.Current != ScreenState.MainMenu && Machine.Current != ScreenState.LevelSelect) return;
            if (bootstrap.TryStartLevel(index)) Machine.StartPlaying();
        }
        private void OnPauseRequested() => Machine.Pause();
        private void OnLevelsRequested() => Machine.OpenLevelSelect();
        private void OpenSettings() => Machine.OpenSettings();
        private void Resume() => Machine.Resume();
        private void Restart() { if (Machine.Current != ScreenState.Paused) return; bootstrap.RestartLevel(); Machine.StartPlaying(); }
        private void ToMainMenu() => Machine.ToMainMenu();
        private void Back() => HandleBack();
        private void OnLevelCompleted(CompletionResult result) => Machine.LevelCompleted();
        private void OnLevelStarted()
        {
            ScreenState previous = Machine.Current;
            if (!Machine.StartPlaying()) ApplyScreen(Machine.Current, Machine.Current);
            // New level starts (menu, level select, next level, replay) get a 1-second loader; restarting from pause does not.
            if (LoadingScreensEnabled && previous != ScreenState.Playing && previous != ScreenState.Paused)
                Loading.Show(LoadingKind.Level, "LEVEL " + (bootstrap.CurrentLevelIndex + 1));
        }
        private void OnProgressReset()
        {
            RefreshMainMenu(); RefreshLevels();
            ApplyScreen(Machine.Current, Machine.Current);
        }
        private void SetSound(bool value) { bootstrap.Settings.SetSound(value); bootstrap.CheckSettingsSave(); }
        private void SetHaptics(bool value) { bootstrap.Settings.SetHaptics(value); bootstrap.CheckSettingsSave(); }
        private void AskReset()
        {
            Confirm.Show("Reset Progress", "Delete all progress and best results? This cannot be undone.", "Delete", bootstrap.ResetProgress);
        }
        /// <summary>Consumes a modal first, otherwise delegates platform Back to the pure machine.</summary>
        public bool HandleBack()
        {
            if (Loading != null && Loading.IsVisible) return true;
            if (Confirm != null && Confirm.IsOpen) { Confirm.Hide(); return true; }
            if (Profile != null && Profile.IsVisible) { CloseProfile(); return true; }
            if(Shop!=null&&Shop.IsVisible)return Shop.HandleBack();
            return Machine.Back();
        }
        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) HandleBack();
        }
        private void OnApplicationPause(bool paused) { if (paused && Machine.Current == ScreenState.Playing) Machine.Pause(); }
#if !UNITY_EDITOR
        private void OnApplicationFocus(bool focused) { if (!focused && Machine.Current == ScreenState.Playing) Machine.Pause(); }
#endif
        private static void Quit()
        {
#if !UNITY_IOS
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
#endif
        }
        private void OnDestroy()
        {
            if(wallet!=null){wallet.CoinsChanged-=OnCoins;wallet.EquippedChanged-=OnThemeEquipped;}
            if(Shop!=null)Shop.Closed-=OnShopClosed;
            Machine.StateChanged -= OnStateChanged;
            if (input != null) input.RemoveBlocker(screenBlocker);
            if (hud != null)
            { hud.PauseRequested -= OnPauseRequested; if (hud.CompletePanel != null) hud.CompletePanel.LevelsRequested -= OnLevelsRequested; }
            if (bootstrap != null) { bootstrap.LevelCompleted -= OnLevelCompleted; bootstrap.ProgressReset -= OnProgressReset; bootstrap.InventoryReset-=BindInventory; }
            if (game != null) game.LevelStarted -= OnLevelStarted;
        }
    }
}
