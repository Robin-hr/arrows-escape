using System;
using System.Reflection;
using ArrowEscape.Core;
using ArrowEscape.UI;
using ArrowEscape.View;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
namespace ArrowEscape.Tests
{
    /// <summary>Builds a complete isolated app with injected stores and explicit lifecycle control.</summary>
    public sealed class FlowTestScene : IDisposable
    {
        private readonly GameObject host, ui, camera;
        private GameObject ownEventSystem;
        /// <summary>Fake progress storage.</summary>
        public FakeProgressStore ProgressStore { get; }
        /// <summary>Fake preference storage.</summary>
        public FakeSettingsStore SettingsStore { get; }
        /// <summary>Bootstrap under test.</summary>
        public GameBootstrap Bootstrap { get; }
        /// <summary>Gameplay owner.</summary>
        public GameController Game { get; }
        /// <summary>Board display.</summary>
        public BoardView Board { get; }
        /// <summary>HUD owner.</summary>
        public HudController Hud { get; }
        /// <summary>Screen glue.</summary>
        public AppFlowController Flow { get; }
        /// <summary>Creates an app without player-file access or an implicit level start.</summary>
        public FlowTestScene(FakeProgressStore progress = null, FakeSettingsStore settings = null, bool initializeFlow = true)
        {
            camera = new GameObject("Flow camera", typeof(Camera)); camera.tag = "MainCamera";
            host = new GameObject("Flow gameplay"); Bootstrap = host.AddComponent<GameBootstrap>(); Bootstrap.SetLevelCatalog(OriginalTestCatalog.Paths, OriginalTestCatalog.LegacyPaths);
            SetAutoStart(false); ProgressStore = progress ?? new FakeProgressStore(); SettingsStore = settings ?? new FakeSettingsStore();
            Bootstrap.Initialize(ProgressStore, SettingsStore); Game = host.GetComponent<GameController>(); Board = host.GetComponent<BoardView>();
            ui = new GameObject("Flow UI"); Hud = ui.AddComponent<HudController>(); Flow = ui.AddComponent<AppFlowController>();
            if (initializeFlow) InitializeFlow();
        }
        /// <summary>Initializes flow and tracks the event system created by this fixture.</summary>
        public void InitializeFlow()
        {
            EventSystem existing = UnityEngine.Object.FindFirstObjectByType<EventSystem>();
            Flow.Initialize(Bootstrap, Hud);
            if (existing == null) ownEventSystem = UnityEngine.Object.FindFirstObjectByType<EventSystem>().gameObject;
        }
        /// <summary>Controls the legacy/manual auto-start flag.</summary>
        public void SetAutoStart(bool value) => typeof(GameBootstrap).GetField("autoStartOnStart", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(Bootstrap, value);
        /// <summary>Invokes an explicit private lifecycle callback without real frames.</summary>
        public static void Callback(object target, string method, params object[] args) => target.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(target, args);
        /// <summary>Drives a world-space tap.</summary>
        public void Tap(int x, int y) { var taps = host.GetComponent<TapController>(); Vector2 p = Board.CellToWorld(x, y); taps.PointerDown(1, p); taps.PointerUp(1, p); }
        /// <summary>Clears via actual tap reporting, expecting the single clear log.</summary>
        public void Clear()
        {
            while (!Game.IsCleared)
            {
                Arrow free = null; foreach (Arrow a in Game.Session.Board.Arrows) if (Game.Session.Board.CanExit(a)) { free = a; break; }
                Assert.That(free, Is.Not.Null); if (Game.Session.Board.Arrows.Count == 1) LogAssert.Expect(LogType.Log, "Level cleared. Mistakes: " + Game.MistakeCount);
                Tap(free.X, free.Y);
            }
        }
        /// <summary>Removes only objects and resources owned by this fixture.</summary>
        public void Dispose() { UnityEngine.Object.DestroyImmediate(ui); UnityEngine.Object.DestroyImmediate(host); UnityEngine.Object.DestroyImmediate(camera); UnityEngine.Object.DestroyImmediate(ownEventSystem); SpriteFactory.ClearCache(); }
    }
}
