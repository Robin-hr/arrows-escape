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
        private readonly TapStreak coinTaps = new TapStreak(5, 0.8f);
        /// <summary>Level-start transition that lands the arrows on the board.</summary>
        public BoardIntro Intro { get; private set; }
        /// <summary>Short neon notices (developer coin grant).</summary>
        public ToastView Toast { get; private set; }
        /// <summary>World ranking page.</summary>
        public RankingView Ranking { get; private set; }
        /// <summary>Menu background music (menus only, never over the board).</summary>
        public MenuMusicPlayer Music { get; private set; }
        /// <summary>Journey road map: every level as a stop on one winding road.</summary>
        public JourneyView Journey { get; private set; }
        /// <summary>"No lives left" panel for the Journey.</summary>
        public JourneyLivesView JourneyLives { get; private set; }
        /// <summary>Gift box after a Journey level's first clear.</summary>
        public GiftBoxView Gift { get; private set; }
        /// <summary>Profile name editor.</summary>
        public NameEditView NameEditor { get; private set; }
        /// <summary>UTC clock for Journey life refills; tests may replace it.</summary>
        public System.Func<long> UtcNow { get; set; } = () => System.DateTime.UtcNow.Ticks;
        private DailyReward pendingGift;
        private float pendingGiftDelay;
        /// <summary>Daily puzzle page (streak, badges, play).</summary>
        public DailyView DailyPage { get; private set; }
        /// <summary>Seven-day reward calendar.</summary>
        public DailyRewardsView Rewards { get; private set; }
        /// <summary>Daily puzzle rules: hearts, result and share.</summary>
        public DailyPuzzleController Daily { get; private set; }
        /// <summary>Stand-in rewarded ad until a real ad network is registered.</summary>
        public AdPlaceholderView Ads { get; private set; }
        /// <summary>Today's calendar key (local date); tests may replace it.</summary>
        public System.Func<int> TodayKey { get; set; } = () => DailyCalendar.Key(System.DateTime.Now);
        /// <summary>Clock for the five-tap developer grant; tests may replace it.</summary>
        public System.Func<float> Clock { get; set; } = () => Time.realtimeSinceStartup;

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
            Intro = boot.GetComponent<BoardIntro>(); if (Intro == null) Intro = boot.gameObject.AddComponent<BoardIntro>();
            hud.Bind(game, bootstrap);
            EventSystemEnsurer.Ensure();
            BuildScreens();
            screenBlocker = IsBlocked; input.AddBlocker(screenBlocker);
            Machine.StateChanged += OnStateChanged;
            hud.PauseRequested += OnPauseRequested;
            hud.CompletePanel.LevelsRequested += OnLevelsRequested;
            hud.CompletePanel.HomeRequested += OnHomeRequested;
            bootstrap.LevelCompleted += OnLevelCompleted;
            bootstrap.ProgressReset += OnProgressReset;
            bootstrap.InventoryReset += BindInventory;
            BindInventory();
            game.LevelStarted += OnLevelStarted;
            ApplyScreen(ScreenState.MainMenu, ScreenState.MainMenu);
            // An explicitly auto-started scene component keeps its existing play behavior.
            if (game.Session != null) Machine.StartPlaying();
            if (LoadingScreensEnabled) Loading.Show(LoadingKind.Startup, null, () => { MainMenu.PlayEntrance(); if (RewardWaiting) OpenRewards(); });
        }
        private bool RewardWaiting => DailyRewardRules.CanClaim(bootstrap.Inventory.State, TodayKey());
        private void BuildScreens()
        {
            var canvasObject = new GameObject("ArrowEscapeMenus", typeof(RectTransform), typeof(Canvas), typeof(UnityEngine.UI.CanvasScaler), typeof(UnityEngine.UI.GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false); canvasObject.transform.SetAsLastSibling();
            MenusCanvas = canvasObject.GetComponent<Canvas>(); MenusCanvas.renderMode = RenderMode.ScreenSpaceOverlay; MenusCanvas.sortingOrder = 100;
            var scaler = canvasObject.GetComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920); scaler.matchWidthOrHeight = 0.5f;
            Music = gameObject.AddComponent<MenuMusicPlayer>(); Music.Bind(bootstrap.Settings.Model);
            MainMenu = gameObject.AddComponent<MainMenuView>(); MainMenu.Build(canvasObject.transform);
            Levels = gameObject.AddComponent<LevelSelectView>(); Levels.Build(canvasObject.transform, StartLevel, Back);
            PauseScreen = gameObject.AddComponent<PauseView>(); PauseScreen.Build(canvasObject.transform);
            SettingsScreen = gameObject.AddComponent<SettingsView>();
            SettingsScreen.Build(canvasObject.transform, bootstrap.Settings.Model, SetSound, SetHaptics, AskReset, Back);
            Confirm = gameObject.AddComponent<ConfirmDialog>(); Confirm.Build(canvasObject.transform);
            Shop=gameObject.AddComponent<ShopView>();Shop.Build(canvasObject.transform,Confirm,game);Shop.Closed+=OnShopClosed;
            Profile = gameObject.AddComponent<ProfileView>(); Profile.Build(canvasObject.transform, CloseProfile, OpenAvatarShop, OpenRanking);
            Ranking = gameObject.AddComponent<RankingView>(); Ranking.Build(canvasObject.transform, CloseRanking, bootstrap.RankingSnapshot);
            Loading = gameObject.AddComponent<LoadingOverlay>(); Loading.Build(canvasObject.transform);
            DailyPage = gameObject.AddComponent<DailyView>(); DailyPage.Build(canvasObject.transform, PlayDaily, CloseDailyPage);
            Rewards = gameObject.AddComponent<DailyRewardsView>(); Rewards.Build(canvasObject.transform, ClaimReward, CloseRewards);
            Journey = gameObject.AddComponent<JourneyView>(); Journey.Build(canvasObject.transform, PlayFromJourney, CloseJourney);
            Journey.DeveloperOpen = EconomyConfig.DeveloperGrantEnabled ? DeveloperOpenFromJourney : (System.Action<int>)null;
            Journey.LivesSource = () => { var st = bootstrap.Inventory.State; long t = UtcNow(); int l = JourneyLifeRules.Update(st, t); return new Vector2Int(l, JourneyLifeRules.SecondsToNext(st, t)); };
            JourneyLives = gameObject.AddComponent<JourneyLivesView>(); JourneyLives.Build(canvasObject.transform, () => bootstrap.Inventory.State, () => UtcNow());
            JourneyLives.AdButton.onClick.AddListener(WatchAdForLife); JourneyLives.BuyButton.onClick.AddListener(BuyLife);
            Journey.LivesButton.onClick.AddListener(() => JourneyLives.Show());
            Gift = gameObject.AddComponent<GiftBoxView>(); Gift.Build(canvasObject.transform);
            NameEditor = gameObject.AddComponent<NameEditView>(); NameEditor.Build(canvasObject.transform, SavePlayerName);
            Profile.EditNameRequested += () => NameEditor.Show(bootstrap.Inventory.State.PlayerName);
            Toast = gameObject.AddComponent<ToastView>(); Toast.Build(canvasObject.transform);
            Daily = gameObject.AddComponent<DailyPuzzleController>(); Daily.Initialize(canvasObject.transform, bootstrap, game, hud, Toast, () => TodayKey(), GoHomeFromDaily);
            Daily.Completed += () => Machine.LevelCompleted();
            hud.BeforeNewAttempt = JourneyAttemptAllowed; Daily.BeforeRestart = JourneyAttemptAllowed;
            Ads = gameObject.AddComponent<AdPlaceholderView>(); Ads.Build(canvasObject.transform);
            if (RewardedAds.Service == null || RewardedAds.Service is AdPlaceholderView) RewardedAds.Service = Ads;
            Levels.DeveloperOpen = EconomyConfig.DeveloperGrantEnabled ? DeveloperOpenLevel : (System.Action<int>)null;
            MainMenu.DailyButton.onClick.AddListener(OpenDailyPage);
            MainMenu.RewardsButton.onClick.AddListener(OpenRewards);
            MainMenu.JourneyButton.onClick.AddListener(OpenJourney);
            MainMenu.ProfileButton.onClick.AddListener(OpenProfile);
            MainMenu.StarsButton.onClick.AddListener(OnLevelsRequested);
            MainMenu.ShopButton.onClick.AddListener(OpenShop);MainMenu.CoinsButton.onClick.AddListener(OnCoinsTapped);Shop.CoinsButton.onClick.AddListener(OnCoinsTapped);
            MainMenu.PlayButton.onClick.AddListener(Continue);
            MainMenu.LevelsButton.onClick.AddListener(OnLevelsRequested);
            MainMenu.SettingsButton.onClick.AddListener(OpenSettings);
            MainMenu.RankingButton.onClick.AddListener(OpenRanking);
            MainMenu.QuitButton.onClick.AddListener(Quit);
            PauseScreen.ResumeButton.onClick.AddListener(Resume);
            PauseScreen.RestartButton.onClick.AddListener(Restart);
            PauseScreen.SettingsButton.onClick.AddListener(OpenSettings);
            PauseScreen.MainMenuButton.onClick.AddListener(ToMainMenu);
        }
        private bool IsBlocked(Vector2 position) => Machine.Current != ScreenState.Playing || Confirm.IsOpen || JourneyLives.IsOpen || Gift.IsOpen || NameEditor.IsOpen || (Loading != null && Loading.IsVisible) || (Intro != null && Intro.IsPlaying) || (Daily != null && Daily.IsModalOpen) || (Ads != null && Ads.IsShowing);
        private void OnStateChanged(ScreenState oldState, ScreenState newState) => ApplyScreen(oldState, newState);
        private void ApplyScreen(ScreenState previous, ScreenState state)
        {
            if(state!=ScreenState.MainMenu&&Shop.IsVisible)Shop.Close();
            if (state != ScreenState.MainMenu && Profile.IsVisible) Profile.Hide();
            if (state != ScreenState.MainMenu && Ranking.IsVisible) Ranking.Hide();
            if (state != ScreenState.MainMenu && DailyPage.IsVisible) DailyPage.Hide();
            if (state != ScreenState.MainMenu && Rewards.IsVisible) Rewards.Hide();
            if (state != ScreenState.MainMenu && Journey.IsVisible) Journey.Hide();
            if (state != ScreenState.Playing && state != ScreenState.Paused && state != ScreenState.LevelComplete && state != ScreenState.Settings) { Daily.Fail.Hide(); Daily.Result.Hide(); }
            MainMenu.Root.gameObject.SetActive(state == ScreenState.MainMenu&&!Shop.IsVisible&&!Profile.IsVisible&&!Ranking.IsVisible&&!DailyPage.IsVisible&&!Rewards.IsVisible&&!Journey.IsVisible);
            Levels.Root.gameObject.SetActive(state == ScreenState.LevelSelect);
            PauseScreen.Root.gameObject.SetActive(state == ScreenState.Paused);
            SettingsScreen.Root.gameObject.SetActive(state == ScreenState.Settings);
            bool settingsPaused = state == ScreenState.Settings && Machine.SettingsReturnState == ScreenState.Paused;
            SettingsScreen.SetBackdrop(!settingsPaused);
            bool showBoard = state == ScreenState.Playing || state == ScreenState.Paused || state == ScreenState.LevelComplete || settingsPaused;
            game.SetPaused(state != ScreenState.Playing && state != ScreenState.LevelComplete);
            if (state == ScreenState.MainMenu && previous == ScreenState.Paused) game.DiscardAttempt();
            board.SetVisible(showBoard); hud.SetVisible(showBoard);
            Music.SetWanted(!showBoard && state != ScreenState.Playing && state != ScreenState.LevelComplete);
            if (state != ScreenState.Settings) Confirm.Hide();
            if (state == ScreenState.MainMenu) RefreshMainMenu();
            if (state == ScreenState.LevelSelect) RefreshLevels();
        }
        private void RefreshMainMenu()
        {
            MainMenu.Refresh(bootstrap.HasAnyProgress,bootstrap.Progress.Tracker.CompletedCount(bootstrap.LevelIds),bootstrap.LevelCount,bootstrap.ContinueIndex);
            MainMenu.SetTheme(bootstrap.Inventory.State.EquippedThemeId);
            MainMenu.SetEconomy(bootstrap.Inventory.State.Coins,MainStars(),bootstrap.LevelCount*3,false);
            MainMenu.SetAvatar(bootstrap.Inventory.State.EquippedAvatarId,bootstrap.Inventory.State.EquippedFrameId);
            MainMenu.SetPlayerName(bootstrap.Inventory.State.PlayerName);
            MainMenu.SetJourneyLives(JourneyLifeRules.Update(bootstrap.Inventory.State, UtcNow()));
            int key=TodayKey();var state=bootstrap.Inventory.State;
            MainMenu.SetDaily(DailyStreakRules.CurrentStreak(state,key),DailyStreakRules.SolvedToday(state,key),DailyRewardRules.CanClaim(state,key));
        }
        // Stars earned on the main levels only (the Journey has its own road).
        private int MainStars()
        {
            int total = 0; var tracker = bootstrap.Progress.Tracker;
            foreach (string id in bootstrap.LevelIds) if (tracker.TryGetRecord(id, out LevelRecord record)) total += record.bestStars;
            return total;
        }
        /// <summary>Opens the daily puzzle page from home.</summary>
        public void OpenDailyPage()
        {
            if (Machine.Current != ScreenState.MainMenu || Shop.IsVisible) return;
            MainMenu.Root.gameObject.SetActive(false); DailyPage.Show(bootstrap.Inventory.State, TodayKey());
        }
        private void CloseDailyPage()
        {
            if (!DailyPage.IsVisible) return;
            DailyPage.Hide(); MainMenu.Root.gameObject.SetActive(Machine.Current == ScreenState.MainMenu); if (Machine.Current == ScreenState.MainMenu) RefreshMainMenu();
        }
        /// <summary>Builds today's board (off the main thread in Play mode) and starts it.</summary>
        public void PlayDaily()
        {
            if (Machine.Current != ScreenState.MainMenu || Daily.IsBuilding) return;
            var label = DailyPage.PlayButton.GetComponentInChildren<UnityEngine.UI.Text>(); label.text = "BUILDING PUZZLE..."; DailyPage.PlayButton.interactable = false;
            Daily.Build(TodayKey(), (built, key) =>
            {
                DailyPage.PlayButton.interactable = true;
                if (Machine.Current != ScreenState.MainMenu) return;
                DailyPage.Hide(); bootstrap.StartDailyLevel(built, key); Machine.StartPlaying();
            });
        }
        private void GoHomeFromDaily()
        {
            if (Machine.Current == ScreenState.Playing) Machine.Pause();
            Machine.ToMainMenu();
        }
        /// <summary>Opens the Journey road map from home, centred on the player's current level.</summary>
        public void OpenJourney()
        {
            if (Machine.Current != ScreenState.MainMenu || Shop.IsVisible) return;
            MainMenu.Root.gameObject.SetActive(false);
            var state = bootstrap.Inventory.State;
            Journey.Show(LevelSelectModel.Build(bootstrap.JourneyIds, bootstrap.Progress.Tracker), bootstrap.JourneyContinueIndex, state.EquippedAvatarId, state.EquippedFrameId, state.PlayerName);
        }
        private void CloseJourney()
        {
            if (!Journey.IsVisible) return;
            Journey.Hide(); MainMenu.Root.gameObject.SetActive(Machine.Current == ScreenState.MainMenu); if (Machine.Current == ScreenState.MainMenu) RefreshMainMenu();
        }
        private void PlayFromJourney(int index)
        {
            if (Machine.Current != ScreenState.MainMenu) return;
            if (!SpendJourneyLife()) return;
            if (bootstrap.TryStartJourneyLevel(index)) { Journey.Hide(); Machine.StartPlaying(); }
            else { JourneyLifeRules.Add(bootstrap.Inventory.State, 1, UtcNow()); bootstrap.Inventory.Save(); }
        }
        /// <summary>Spends one Journey life (saved); with none left it opens the lives panel and returns false.</summary>
        public bool SpendJourneyLife()
        {
            if (!JourneyLifeRules.TrySpend(bootstrap.Inventory.State, UtcNow()))
            { JourneyLives.Show(); var audio = game.GetComponent<AudioDirector>(); if (audio != null) audio.PlayLose(); return false; }
            bootstrap.Inventory.Save(); MainMenu.SetJourneyLives(bootstrap.Inventory.State.JourneyLives); Journey.RefreshLives();
            return true;
        }
        private bool JourneyAttemptAllowed()
        {
            if (!bootstrap.IsJourneyLevel) return true;
            if (!SpendJourneyLife()) return false;
            Toast.Show("-1 JOURNEY LIFE\n" + bootstrap.Inventory.State.JourneyLives + " / " + JourneyLifeRules.Max + " LEFT", "Daily/icon_heart");
            return true;
        }
        private void GrantLife()
        {
            JourneyLifeRules.Add(bootstrap.Inventory.State, 1, UtcNow()); bootstrap.Inventory.Save();
            MainMenu.SetJourneyLives(bootstrap.Inventory.State.JourneyLives); Journey.RefreshLives(); JourneyLives.Refresh();
        }
        private void WatchAdForLife()
        {
            void Done(bool earned) { if (earned) GrantLife(); }
            if (RewardedAds.Service == null) Done(true); else RewardedAds.Service.Show("journey_life", Done);
        }
        private void BuyLife()
        {
            var state = bootstrap.Inventory.State;
            if (JourneyLifeRules.Update(state, UtcNow()) >= JourneyLifeRules.Max || !bootstrap.Inventory.Shop.TrySpendCoins(EconomyConfig.JourneyLifePrice)) return;
            game.RequestUiFeedback(UiFeedbackCue.Purchase); GrantLife();
        }
        private void SavePlayerName(string name)
        {
            var state = bootstrap.Inventory.State; state.PlayerName = PlayerNameRules.Clean(name); bootstrap.Inventory.Save();
            MainMenu.SetPlayerName(state.PlayerName); Profile.SetPlayerName(state.PlayerName);
        }
        /// <summary>Developer option: opens a locked level from the Journey map (three quick taps).</summary>
        public void DeveloperOpenFromJourney(int index)
        {
            if (Machine.Current != ScreenState.MainMenu || !bootstrap.TryStartJourneyLevelIgnoringLock(index)) return;
            Journey.Hide(); Toast.Show("DEVELOPER MODE\nLEVEL " + (index + 1) + " OPENED", "icon_lock");
            Machine.StartPlaying();
        }
        /// <summary>Opens the 7-day reward calendar.</summary>
        public void OpenRewards()
        {
            if (Machine.Current != ScreenState.MainMenu || Shop.IsVisible) return;
            if (Profile.IsVisible) Profile.Hide(); if (Ranking.IsVisible) Ranking.Hide(); if (DailyPage.IsVisible) DailyPage.Hide();
            MainMenu.Root.gameObject.SetActive(false); Rewards.Show(bootstrap.Inventory.State, TodayKey());
        }
        private void CloseRewards()
        {
            if (!Rewards.IsVisible) return;
            Rewards.Hide(); MainMenu.Root.gameObject.SetActive(Machine.Current == ScreenState.MainMenu); if (Machine.Current == ScreenState.MainMenu) RefreshMainMenu();
        }
        /// <summary>Claims today's reward, saves it and presents it (day 7 spins the mystery reel).</summary>
        public bool ClaimReward()
        {
            int key = TodayKey(); var shop = bootstrap.Inventory.Shop; int day = DailyRewardRules.NextDay(shop.Inventory);
            var reward = DailyRewardRules.Claim(shop, key); if (reward == null) return false;
            bootstrap.Inventory.Save(); Rewards.Present(reward, day, shop.Inventory, key); OnCoins(shop.Inventory.Coins);
            return true;
        }
        /// <summary>Developer option: opens a locked level from the level list (three quick taps).</summary>
        public void DeveloperOpenLevel(int index)
        {
            if (Machine.Current != ScreenState.LevelSelect) return;
            if (!bootstrap.TryStartLevelIgnoringLock(index)) return;
            Toast.Show("DEVELOPER MODE\nLEVEL " + (index + 1) + " OPENED", "icon_lock");
            Machine.StartPlaying();
        }
        private void BindInventory()
        {
            if(wallet!=null){wallet.CoinsChanged-=OnCoins;wallet.EquippedChanged-=OnThemeEquipped;}wallet=bootstrap.Inventory.Shop;wallet.CoinsChanged+=OnCoins;wallet.EquippedChanged+=OnThemeEquipped;
            Shop.Bind(bootstrap.Inventory);OnCoins(wallet.Inventory.Coins);
        }
        private void OnThemeEquipped(ShopSlot slot,string id)
        {
            if(slot==ShopSlot.Theme)MainMenu.SetTheme(id);
            if(slot==ShopSlot.Avatar||slot==ShopSlot.Frame)MainMenu.SetAvatar(bootstrap.Inventory.State.EquippedAvatarId,bootstrap.Inventory.State.EquippedFrameId);
        }
        /// <summary>
        /// Coin pill taps: the first opens the shop; five quick taps (home and shop pills share one position)
        /// grant the developer coins when <see cref="EconomyConfig.DeveloperGrantEnabled"/> is on.
        /// </summary>
        public void OnCoinsTapped()
        {
            bool grant=EconomyConfig.DeveloperGrantEnabled&&coinTaps.Tap(Clock());
            if(grant){GrantDeveloperCoins();return;}
            if(!Shop.IsVisible)OpenShop();
        }
        /// <summary>Adds the developer coin grant, saves it and shows a notice.</summary>
        public void GrantDeveloperCoins()
        {
            bootstrap.Inventory.Shop.AddCoins(EconomyConfig.DeveloperGrant);bootstrap.Inventory.Save();
            Toast.Show("DEVELOPER MODE\n+"+EconomyConfig.DeveloperGrant.ToString("N0",System.Globalization.CultureInfo.InvariantCulture)+" COINS");
        }
        private void OnCoins(int amount)=>MainMenu.SetEconomy(amount,MainStars(),bootstrap.LevelCount*3);
        /// <summary>Opens the shop only from the main menu.</summary>
        public void OpenShop(){if(Machine.Current!=ScreenState.MainMenu)return;MainMenu.Root.gameObject.SetActive(false);Shop.Show();}
        /// <summary>From the profile page: opens the shop on the Avatars tab; closing it returns to the profile.</summary>
        public void OpenAvatarShop(){if(Machine.Current!=ScreenState.MainMenu)return;MainMenu.Root.gameObject.SetActive(false);Shop.Show(ShopSlot.Avatar);}
        /// <summary>Opens the profile page from the home page, behind a 2-second loading screen.</summary>
        public void OpenProfile()
        {
            if (Machine.Current != ScreenState.MainMenu || Shop.IsVisible) return;
            MainMenu.Root.gameObject.SetActive(false);
            Profile.Show(ProfileStats.Build(bootstrap.Progress.Tracker, bootstrap.LevelIds, bootstrap.Inventory.State));
            Profile.SetPlayerName(bootstrap.Inventory.State.PlayerName);
            if (LoadingScreensEnabled) Loading.Show(LoadingKind.Profile, "LOADING PROFILE");
        }
        /// <summary>Opens the world ranking from the home page or the profile league card.</summary>
        public void OpenRanking()
        {
            if (Machine.Current != ScreenState.MainMenu || Shop.IsVisible) return;
            MainMenu.Root.gameObject.SetActive(false);
            Ranking.Show();
            if (LoadingScreensEnabled) Loading.Show(LoadingKind.Profile, "LOADING RANKING");
        }
        private void CloseRanking()
        {
            if (!Ranking.IsVisible) return;
            Ranking.Hide();
            if (Profile.IsVisible) { Profile.Show(ProfileStats.Build(bootstrap.Progress.Tracker, bootstrap.LevelIds, bootstrap.Inventory.State)); return; }
            MainMenu.Root.gameObject.SetActive(Machine.Current == ScreenState.MainMenu);
            if (Machine.Current == ScreenState.MainMenu) RefreshMainMenu();
        }
        private void CloseProfile()
        {
            if (!Profile.IsVisible) return;
            Profile.Hide();
            MainMenu.Root.gameObject.SetActive(Machine.Current == ScreenState.MainMenu);
            if (Machine.Current == ScreenState.MainMenu) RefreshMainMenu();
        }
        private void OnShopClosed()
        {
            if(Profile.IsVisible){Profile.Show(ProfileStats.Build(bootstrap.Progress.Tracker,bootstrap.LevelIds,bootstrap.Inventory.State));MainMenu.Root.gameObject.SetActive(false);return;}
            MainMenu.Root.gameObject.SetActive(Machine.Current==ScreenState.MainMenu);if(Machine.Current==ScreenState.MainMenu)RefreshMainMenu();
        }

        private void RefreshLevels() => Levels.Refresh(LevelSelectModel.Build(bootstrap.LevelIds, bootstrap.Progress.Tracker));
        private void Continue() => StartLevel(bootstrap.ContinueIndex);
        private void StartLevel(int index)
        {
            if (Machine.Current != ScreenState.MainMenu && Machine.Current != ScreenState.LevelSelect) return;
            if (bootstrap.TryStartLevel(index)) Machine.StartPlaying();
        }
        private void OnPauseRequested() => Machine.Pause();
        private void OnHomeRequested() { if (Machine.Current == ScreenState.LevelComplete) Machine.ToMainMenu(); }
        private void OnLevelsRequested()
        {
            // From a Journey level's completion card, "Levels" returns to the Journey map.
            if (Machine.Current == ScreenState.LevelComplete && bootstrap.IsJourneyLevel) { Machine.ToMainMenu(); OpenJourney(); return; }
            Machine.OpenLevelSelect();
        }
        private void OpenSettings() => Machine.OpenSettings();
        private void Resume() => Machine.Resume();
        private void Restart() { if (Machine.Current != ScreenState.Paused || !JourneyAttemptAllowed()) return; bootstrap.RestartLevel(); Machine.StartPlaying(); }
        private void ToMainMenu() => Machine.ToMainMenu();
        private void Back() => HandleBack();
        private void OnLevelCompleted(CompletionResult result)
        {
            Machine.LevelCompleted();
            if (!bootstrap.IsJourneyLevel || result == null || !result.IsFirstClear) return;
            // First clear of a Journey level: grant and save a random gift, then open the gift box after the stars.
            var shop = bootstrap.Inventory.Shop;
            pendingGift = JourneyGiftRules.Roll(shop.Inventory, bootstrap.CurrentLevelId, bootstrap.CurrentAttemptId);
            DailyRewardRules.Apply(shop, pendingGift); bootstrap.Inventory.Save(); OnCoins(shop.Inventory.Coins);
            pendingGiftDelay = Application.isPlaying ? 1.9f : 0f;
            if (pendingGiftDelay <= 0) ShowPendingGift();
        }
        private void ShowPendingGift() { var gift = pendingGift; pendingGift = null; if (gift != null) Gift.Show(gift, null); }
        private void OnLevelStarted()
        {
            ScreenState previous = Machine.Current;
            if (!Machine.StartPlaying()) ApplyScreen(Machine.Current, Machine.Current);
            // New level starts (menu, level select, next level, replay) get a 1-second loader; restarting from pause does not.
            bool newLevel = previous != ScreenState.Playing && previous != ScreenState.Paused;
            if (LoadingScreensEnabled && newLevel)
                Loading.Show(LoadingKind.Level, bootstrap.IsDailyLevel ? "DAILY PUZZLE" : (bootstrap.IsJourneyLevel ? "JOURNEY " : "LEVEL ") + (bootstrap.CurrentLevelIndex + 1));
            // Arrows land on the board as the loading screen fades (or right away on a restart).
            if (LoadingScreensEnabled) Intro.Play(newLevel ? LoadingOverlay.LevelSeconds - 0.1f : 0f);
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
            if (Gift != null && Gift.IsOpen) { if (Gift.IsRevealed) Gift.CollectButton.onClick.Invoke(); return true; }
            if (NameEditor != null && NameEditor.IsOpen) { NameEditor.Hide(); return true; }
            if (JourneyLives != null && JourneyLives.IsOpen) { JourneyLives.Hide(); return true; }
            if (Ads != null && Ads.IsShowing) return true;
            if (Daily != null && Daily.Result.IsOpen) { Daily.Result.Hide(); GoHomeFromDaily(); return true; }
            if (Daily != null && Daily.Fail.IsOpen) return true;
            if (Rewards != null && Rewards.IsVisible) { if (!Rewards.IsSpinning) CloseRewards(); return true; }
            if (DailyPage != null && DailyPage.IsVisible) { CloseDailyPage(); return true; }
            if (Journey != null && Journey.IsVisible) { CloseJourney(); return true; }
            if(Shop!=null&&Shop.IsVisible)return Shop.HandleBack();
            if (Ranking != null && Ranking.IsVisible) { CloseRanking(); return true; }
            if (Profile != null && Profile.IsVisible) { CloseProfile(); return true; }
            return Machine.Back();
        }
        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) HandleBack();
            if (pendingGift != null) { pendingGiftDelay -= Time.unscaledDeltaTime; if (pendingGiftDelay <= 0) ShowPendingGift(); }
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
            { hud.PauseRequested -= OnPauseRequested; if (hud.CompletePanel != null) { hud.CompletePanel.LevelsRequested -= OnLevelsRequested; hud.CompletePanel.HomeRequested -= OnHomeRequested; } }
            if (bootstrap != null) { bootstrap.LevelCompleted -= OnLevelCompleted; bootstrap.ProgressReset -= OnProgressReset; bootstrap.InventoryReset-=BindInventory; }
            if (game != null) game.LevelStarted -= OnLevelStarted;
        }
    }
}
