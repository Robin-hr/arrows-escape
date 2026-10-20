using System;
using ArrowEscape.Core;
using ArrowEscape.View;
using UnityEngine;
using UnityEngine.TestTools;

namespace ArrowEscape.Tests
{
    /// <summary>Isolated real gameplay with fake audio, no stores and no device requirements.</summary>
    public sealed class AudioTestScene : IDisposable
    {
        /// <summary>Runtime test root.</summary>
        public GameObject Host { get; }
        /// <summary>Instant gameplay owner.</summary>
        public GameController Game { get; }
        /// <summary>Audio event translator.</summary>
        public AudioDirector Director { get; }
        /// <summary>Controlled preferences.</summary>
        public SettingsModel Settings { get; }
        /// <summary>Recorded requests.</summary>
        public RecordingSoundPlayer Player { get; } = new RecordingSoundPlayer();
        /// <summary>Builds a free-arrow board unless another board is supplied.</summary>
        public AudioTestScene(Board board = null, SettingsModel settings = null)
        {
            Host = new GameObject("Audio gameplay test"); Game = Host.AddComponent<GameController>();
            if (UnityEngine.Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None).Length == 0) Host.AddComponent<AudioListener>();
            Director = Host.AddComponent<AudioDirector>(); Settings = settings ?? new SettingsModel();
            Game.StartLevel(board ?? FreeBoard(4)); Director.Initialize(Game, Settings, Player);
        }
        /// <summary>Creates independent upward arrows.</summary>
        public static Board FreeBoard(int count)
        { var board = new Board(count,1); for (int i = 0; i < count; i++) board.TryAddArrow(i + 1,i,0,Direction.Up,out _); return board; }
        /// <summary>Drives a real tap and explicit animator tick.</summary>
        public void Tap(Arrow arrow)
        {
            if (arrow == null) return;
            Board board = Game.Session.Board;
            if (!Game.IsPaused && board.Arrows.Count == 1 && board.CanExit(arrow))
                LogAssert.Expect(LogType.Log,"Level cleared. Mistakes: " + Game.MistakeCount);
            Vector2 point = Host.GetComponent<BoardView>().CellToWorld(arrow.X,arrow.Y);
            TapController taps = Host.GetComponent<TapController>(); taps.PointerDown(1,point); taps.PointerUp(1,point);
            Host.GetComponent<ArrowAnimator>().Tick(.6f);
        }
        /// <summary>Taps a current arrow by id.</summary>
        public void TapId(int id) => Tap(Game.Session.Board.GetArrowById(id));
        /// <summary>Removes every owned runtime object and sprite.</summary>
        public void Dispose() { UnityEngine.Object.DestroyImmediate(Host); SpriteFactory.ClearCache(); }
    }
}
