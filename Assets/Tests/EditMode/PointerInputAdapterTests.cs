using System.Reflection;
using ArrowEscape.Core;
using ArrowEscape.View;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;

namespace ArrowEscape.Tests
{
    /// <summary>Checks mouse/touch forwarding with virtual Input System devices, not physical input.</summary>
    public class PointerInputAdapterTests
    {
        private GameObject host;
        private GameObject cameraObject;
        private Camera camera;
        private BoardView view;
        private PointerInputAdapter adapter;
        private Mouse mouse;
        private Touchscreen touch;
        private Mouse previousMouse;
        private Touchscreen previousTouch;
        private int tapped;
        private Arrow lastTap;

        /// <summary>Creates virtual devices and a board, reusing the scene camera without modifying it.</summary>
        [SetUp]
        public void SetUp()
        {
            previousMouse = Mouse.current;
            previousTouch = Touchscreen.current;
            mouse = InputSystem.AddDevice<Mouse>();
            touch = InputSystem.AddDevice<Touchscreen>();
            camera = Camera.main;
            if (camera == null)
            {
                cameraObject = new GameObject("Input test camera");
                cameraObject.tag = "MainCamera";
                camera = cameraObject.AddComponent<Camera>();
                camera.orthographic = true;
                camera.transform.position = new Vector3(0, 0, -10);
            }
            host = new GameObject("Pointer adapter test");
            view = host.AddComponent<BoardView>();
            var board = new Board(4, 3);
            board.TryAddArrow(1, 0, 0, Direction.Right, out _);
            board.TryAddArrow(2, 1, 0, Direction.Up, out _);
            view.Show(board);
            TapController controller = host.AddComponent<TapController>();
            controller.Bind(view);
            adapter = host.AddComponent<PointerInputAdapter>();
            adapter.Bind(controller);
            tapped = 0;
            lastTap = null;
            controller.ArrowTapped += arrow => { tapped++; lastTap = arrow; };
            // Edit Mode does not run the normal player lifecycle or idle input frame.
            InvokeCallback("OnDisable");
            InvokeCallback("OnEnable");
            Poll();
        }

        /// <summary>Restores device selection and removes all created objects and resources.</summary>
        [TearDown]
        public void TearDown()
        {
            if (adapter != null) InvokeCallback("OnDisable");
            Object.DestroyImmediate(host);
            Object.DestroyImmediate(cameraObject);
            if (mouse != null && mouse.added) InputSystem.RemoveDevice(mouse);
            if (touch != null && touch.added) InputSystem.RemoveDevice(touch);
            if (previousMouse != null && previousMouse.added) previousMouse.MakeCurrent();
            if (previousTouch != null && previousTouch.added) previousTouch.MakeCurrent();
            SpriteFactory.ClearCache();
        }

        private Vector2 ScreenCell(int x, int y) => camera.WorldToScreenPoint(view.CellToWorld(x, y));
        private void Poll()
        {
            // Inspect Editor input while its frame flags are active, before Unity restores player state.
            InputSystem.onAfterUpdate += PollAdapter;
            try { InputSystem.Update(); }
            finally { InputSystem.onAfterUpdate -= PollAdapter; }
        }

        private void PollAdapter()
        {
            mouse.MakeCurrent();
            touch.MakeCurrent();
            InvokeCallback("Update");
        }
        private void InvokeCallback(string name, params object[] arguments) =>
            typeof(PointerInputAdapter).GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(adapter, arguments);

        private void QueueMouse(bool down, Vector2 position, double time = -1)
        {
            var state = new MouseState { position = position };
            if (down) state = state.WithButton(MouseButton.Left);
            InputSystem.QueueStateEvent(mouse, state, time);
        }

        private void QueueTouch(int id, TouchPhase phase, Vector2 position, double time = -1)
        {
            InputSystem.QueueStateEvent(touch, new TouchState
            {
                touchId = id, phase = phase, position = position,
                pressure = phase == TouchPhase.Ended || phase == TouchPhase.Canceled ? 0 : 1
            }, time);
        }

        /// <summary>A virtual mouse press highlights and its release reports one tap.</summary>
        [Test]
        public void MousePressReleaseWorks()
        {
            Vector2 position = ScreenCell(0, 0);
            QueueMouse(true, position); Poll();
            view.TryGetArrowView(1, out ArrowView arrow);
            Assert.That(arrow.SpriteRenderer.color, Is.EqualTo(ViewTheme.HighlightColor));
            QueueMouse(false, position); Poll();
            Assert.That(tapped, Is.EqualTo(1));
            Assert.That(arrow.SpriteRenderer.color, Is.EqualTo(ViewTheme.ArrowColor));
        }

        /// <summary>A second finger cannot move or release the original touch's selection.</summary>
        [Test]
        public void FirstTouchOwnsInteraction()
        {
            QueueTouch(11, TouchPhase.Began, ScreenCell(0, 0)); Poll();
            QueueTouch(22, TouchPhase.Began, ScreenCell(1, 0)); Poll();
            QueueTouch(22, TouchPhase.Ended, ScreenCell(1, 0)); Poll();
            Assert.That(tapped, Is.Zero);
            QueueTouch(11, TouchPhase.Ended, ScreenCell(0, 0)); Poll();
            Assert.That(tapped, Is.EqualTo(1));
            Assert.That(lastTap.Id, Is.EqualTo(1));
        }

        /// <summary>An empty first press still prevents a second finger from selecting anything.</summary>
        [Test]
        public void EmptyFirstTouchStillOwnsPointer()
        {
            QueueTouch(11, TouchPhase.Began, ScreenCell(3, 2)); Poll();
            QueueTouch(22, TouchPhase.Began, ScreenCell(0, 0)); Poll();
            QueueTouch(11, TouchPhase.Ended, ScreenCell(3, 2)); Poll();
            QueueTouch(22, TouchPhase.Moved, ScreenCell(0, 0)); Poll();
            QueueTouch(22, TouchPhase.Ended, ScreenCell(0, 0)); Poll();
            Assert.That(tapped, Is.Zero);
        }

        /// <summary>Focus loss and app pause cancel before a later release can tap.</summary>
        [TestCase("OnApplicationFocus", false)]
        [TestCase("OnApplicationPause", true)]
        public void FocusOrPauseCancels(string callback, bool value)
        {
            QueueMouse(true, ScreenCell(0, 0)); Poll();
            InvokeCallback(callback, value);
            QueueMouse(false, ScreenCell(0, 0)); Poll();
            Assert.That(tapped, Is.Zero);
            view.TryGetArrowView(1, out ArrowView arrow);
            Assert.That(arrow.SpriteRenderer.color, Is.EqualTo(ViewTheme.ArrowColor));
        }

        /// <summary>A finger held while focus is lost or the app is paused cannot become a new press.</summary>
        [TestCase("OnApplicationFocus", false)]
        [TestCase("OnApplicationPause", true)]
        public void ReturningToAppDoesNotCaptureAlreadyHeldFinger(string callback, bool value)
        {
            InvokeCallback(callback, value);
            QueueTouch(11, TouchPhase.Began, ScreenCell(0, 0)); Poll();
            InvokeCallback(callback, !value);
            QueueTouch(11, TouchPhase.Moved, ScreenCell(0, 0)); Poll();
            QueueTouch(11, TouchPhase.Ended, ScreenCell(0, 0)); Poll();
            Assert.That(tapped, Is.Zero);
            view.TryGetArrowView(1, out ArrowView arrow);
            Assert.That(arrow.SpriteRenderer.color, Is.EqualTo(ViewTheme.ArrowColor));
        }

        /// <summary>Cross-device event order wins even when the mouse moves again later that frame.</summary>
        [TestCase(true)]
        [TestCase(false)]
        public void EarliestPointerWinsWithinSameInputUpdate(bool mouseFirst)
        {
            double time = Time.realtimeSinceStartupAsDouble - 0.1;
            QueueMouse(true, ScreenCell(0, 0), time + (mouseFirst ? 0 : 0.02));
            QueueTouch(11, TouchPhase.Began, ScreenCell(1, 0), time + (mouseFirst ? 0.02 : 0));
            QueueMouse(true, ScreenCell(0, 0), time + 0.04);
            Poll();
            view.TryGetArrowView(mouseFirst ? 1 : 2, out ArrowView selected);
            view.TryGetArrowView(mouseFirst ? 2 : 1, out ArrowView ignored);
            Assert.That(selected.SpriteRenderer.color, Is.EqualTo(ViewTheme.HighlightColor));
            Assert.That(ignored.SpriteRenderer.color, Is.EqualTo(ViewTheme.ArrowColor));
        }

        /// <summary>An operating-system touch cancellation never reports a tap.</summary>
        [Test]
        public void TouchCancellationClearsHighlight()
        {
            QueueTouch(11, TouchPhase.Began, ScreenCell(0, 0)); Poll();
            QueueTouch(11, TouchPhase.Canceled, ScreenCell(0, 0)); Poll();
            Assert.That(tapped, Is.Zero);
            view.TryGetArrowView(1, out ArrowView arrow);
            Assert.That(arrow.SpriteRenderer.color, Is.EqualTo(ViewTheme.ArrowColor));
        }

        /// <summary>A quick began/ended pair in one input update is still a single tap.</summary>
        [Test]
        public void FastTouchWithinOneUpdateTapsOnce()
        {
            QueueTouch(11, TouchPhase.Began, ScreenCell(0, 0));
            QueueTouch(11, TouchPhase.Ended, ScreenCell(0, 0));
            Poll();
            Assert.That(tapped, Is.EqualTo(1));
        }
    }
}
