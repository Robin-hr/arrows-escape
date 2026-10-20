using System;
using ArrowEscape.Core;
using ArrowEscape.UI;
using ArrowEscape.View;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
namespace ArrowEscape.Tests
{
    /// <summary>Isolated scene fixture with injected memory storage and optional UI.</summary>
    public sealed class ProgressTestScene : IDisposable
    {
        private readonly GameObject host, cameraHost, uiHost;
        /// <summary>The injected fake, never real disk.</summary>
        public FakeProgressStore Store { get; }
        /// <summary>The level-flow owner.</summary>
        public GameBootstrap Bootstrap { get; }
        /// <summary>The shared-board gameplay owner.</summary>
        public GameController Game { get; }
        /// <summary>Optional HUD controller.</summary>
        public HudController Hud { get; }
        /// <summary>Builds runtime objects without calling Start or reading persistentDataPath.</summary>
        public ProgressTestScene(FakeProgressStore store = null, bool withUi = false, bool bindBeforeInitialize = false)
        {
            Store = store ?? new FakeProgressStore();
            cameraHost = new GameObject("Progress camera", typeof(Camera)); cameraHost.tag = "MainCamera";
            host = new GameObject("Progress gameplay"); Bootstrap = host.AddComponent<GameBootstrap>(); Game = host.GetComponent<GameController>();
            if (withUi)
            {
                uiHost = new GameObject("Progress HUD"); Hud = uiHost.AddComponent<HudController>();
                if (bindBeforeInitialize) Hud.Bind(Game, Bootstrap);
            }
            Bootstrap.Initialize(Store);
            if (withUi && !bindBeforeInitialize) Hud.Bind(Game, Bootstrap);
        }
        /// <summary>Drives one tap at a cell without hardware input.</summary>
        public void Tap(int x, int y)
        {
            var view = host.GetComponent<BoardView>(); var taps = host.GetComponent<TapController>(); Vector2 point = view.CellToWorld(x, y);
            taps.PointerDown(1, point); taps.PointerUp(1, point);
        }
        /// <summary>Taps any free arrow, leaving other arrows available for undo.</summary>
        public void ExitOne()
        {
            foreach (Arrow arrow in Game.Session.Board.Arrows)
                if (Game.Session.Board.CanExit(arrow)) { Tap(arrow.X, arrow.Y); return; }
            Assert.Fail("Expected a free arrow.");
        }
        /// <summary>Clears through real taps and expects the single clear log.</summary>
        public void Clear()
        {
            while (!Game.IsCleared)
            {
                if (Game.Session.Board.Arrows.Count == 1)
                    LogAssert.Expect(LogType.Log, "Level cleared. Mistakes: " + Game.MistakeCount);
                ExitOne();
            }
        }
        /// <summary>Removes runtime objects and generated sprites.</summary>
        public void Dispose()
        {
            UnityEngine.Object.DestroyImmediate(uiHost); UnityEngine.Object.DestroyImmediate(host); UnityEngine.Object.DestroyImmediate(cameraHost); SpriteFactory.ClearCache();
        }
    }
}
