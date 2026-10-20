using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.LowLevel;

namespace ArrowEscape.View
{
    /// <summary>Polls one mouse or touch owner and forwards world positions without frame allocations.</summary>
    public sealed class PointerInputAdapter : MonoBehaviour
    {
        /// <summary>Optional UI gate evaluated only at the beginning of an interaction.</summary>
        public System.Func<Vector2, bool> IsScreenPointBlocked { get; set; }
        /// <summary>Whether this screen position may begin a board press.</summary>
        public bool ShouldAcceptPointerDown(Vector2 screenPosition)
        {
            if (IsScreenPointBlocked != null && IsScreenPointBlocked(screenPosition)) return false;
            for (int i = 0; i < blockers.Count; i++) if (blockers[i](screenPosition)) return false;
            return true;
        }
        private readonly System.Collections.Generic.List<System.Func<Vector2, bool>> blockers = new System.Collections.Generic.List<System.Func<Vector2, bool>>();
        /// <summary>Adds an independent pointer gate once; null is ignored.</summary>
        public void AddBlocker(System.Func<Vector2, bool> blocker) { if (blocker != null && !blockers.Contains(blocker)) blockers.Add(blocker); }
        /// <summary>Removes only this caller's gate.</summary>
        public void RemoveBlocker(System.Func<Vector2, bool> blocker) { if (blocker != null) blockers.Remove(blocker); }
        private bool ownerBlocked;
        private const int MousePointerId = -1;
        private TapController controller;
        private Mouse ownerMouse;
        private TouchControl ownerTouch;
        private int ownerId;
        private bool hasOwner;
        private bool focused = true;
        private bool paused;
        private bool missingCameraLogged;
        private Mouse pressedMouse;
        private double mouseDownTime;
        private Vector2 mouseDownPosition;
        private bool pendingMousePress;
        private Touchscreen observedScreen;
        private double latestTouchStart = double.NegativeInfinity;

        /// <summary>Binds the receiver, cancelling any interaction on the previous receiver.</summary>
        public void Bind(TapController value)
        {
            CancelOwner();
            controller = value;
        }

        private void Update()
        {
            if (controller == null || !focused || paused) { pendingMousePress = false; return; }
            TouchControl firstTouch = FindNewTouch();
            Camera camera = Camera.main;
            if (camera == null)
            {
                CancelOwner();
                if (!missingCameraLogged)
                    Debug.LogError("ArrowEscape input: no Camera.main found. Tag the camera MainCamera.", this);
                missingCameraLogged = true;
                pendingMousePress = false;
                return;
            }
            if (!hasOwner) BeginPointer(camera, firstTouch);
            pendingMousePress = false;
            if (!hasOwner) return;
            if (ownerTouch != null) PollTouch(camera);
            else PollMouse(camera);
        }

        private TouchControl FindNewTouch()
        {
            TouchControl firstTouch = null;
            Touchscreen screen = Touchscreen.current;
            if (screen != observedScreen)
            {
                observedScreen = screen;
                latestTouchStart = double.NegativeInfinity;
            }
            double newest = latestTouchStart;
            if (screen != null)
                for (int i = 0; i < screen.touches.Count; i++)
                {
                    TouchControl touch = screen.touches[i];
                    var phase = touch.phase.ReadValue();
                    if (phase == UnityEngine.InputSystem.TouchPhase.None) continue;
                    double started = touch.startTime.ReadValue();
                    if (started > newest) newest = started;
                    if (phase == UnityEngine.InputSystem.TouchPhase.Canceled || started <= latestTouchStart) continue;
                    if (firstTouch == null || started < firstTouch.startTime.ReadValue())
                        firstTouch = touch;
                }
            // Also remember ignored fingers so they cannot take over after the owner releases.
            latestTouchStart = newest;
            return firstTouch;
        }

        private void BeginPointer(Camera camera, TouchControl firstTouch)
        {
            Mouse mouse = Mouse.current;
            bool mouseDown = mouse != null && pendingMousePress && pressedMouse == mouse;
            if (firstTouch != null && (!mouseDown || pressedMouse != mouse ||
                firstTouch.startTime.ReadValue() <= mouseDownTime))
            {
                ownerTouch = firstTouch;
                ownerId = firstTouch.touchId.ReadValue();
                hasOwner = true;
                ownerBlocked = !ShouldAcceptPointerDown(firstTouch.startPosition.ReadValue());
                if (!ownerBlocked) controller.PointerDown(ownerId, ToWorld(camera, firstTouch.startPosition.ReadValue()));
            }
            else if (mouseDown)
            {
                ownerMouse = mouse;
                ownerId = MousePointerId;
                hasOwner = true;
                ownerBlocked = !ShouldAcceptPointerDown(mouseDownPosition);
                if (!ownerBlocked) controller.PointerDown(ownerId, ToWorld(camera, mouseDownPosition));
            }
            // Even a press on empty space owns the pointer until its physical release.
        }

        private void PollTouch(Camera camera)
        {
            if (!ownerTouch.device.added || ownerTouch.touchId.ReadValue() != ownerId)
            {
                CancelOwner();
                return;
            }
            var phase = ownerTouch.phase.ReadValue();
            if (phase == UnityEngine.InputSystem.TouchPhase.Canceled || phase == UnityEngine.InputSystem.TouchPhase.None)
            {
                CancelOwner();
                return;
            }
            Vector2 world = ToWorld(camera, ownerTouch.position.ReadValue());
            if (phase == UnityEngine.InputSystem.TouchPhase.Ended) ReleaseOwner(world);
            else if (!ownerBlocked) controller.PointerMove(ownerId, world);
        }

        private void PollMouse(Camera camera)
        {
            if (ownerMouse == null || !ownerMouse.added) { CancelOwner(); return; }
            Vector2 world = ToWorld(camera, ownerMouse.position.ReadValue());
            if (ownerMouse.leftButton.isPressed) { if (!ownerBlocked) controller.PointerMove(ownerId, world); }
            else ReleaseOwner(world);
        }

        private void ReleaseOwner(Vector2 world)
        {
            if (!ownerBlocked) controller.PointerUp(ownerId, world);
            ClearOwner();
        }

        private void CancelOwner()
        {
            if (hasOwner && !ownerBlocked && controller != null) controller.PointerCancel(ownerId);
            pendingMousePress = false;
            ClearOwner();
        }

        private void ClearOwner()
        {
            hasOwner = false;
            ownerBlocked = false;
            ownerTouch = null;
            ownerMouse = null;
        }

        private static Vector2 ToWorld(Camera camera, Vector2 screen)
        {
            float distance = Mathf.Max(camera.nearClipPlane, Vector3.Dot(-camera.transform.position, camera.transform.forward));
            return camera.ScreenToWorldPoint(new Vector3(screen.x, screen.y, distance));
        }

        // Remember the button's actual event time, not a later mouse-move time in the same frame.
        private void OnInputEvent(InputEventPtr eventPtr, InputDevice device)
        {
            if (!eventPtr.IsA<StateEvent>() && !eventPtr.IsA<DeltaStateEvent>()) return;
            if (!focused || paused || hasOwner || pendingMousePress) return;
            if (device is Mouse mouse && !mouse.leftButton.isPressed &&
                mouse.leftButton.ReadValueFromEvent(eventPtr, out float value) && value > 0.5f)
            {
                pressedMouse = mouse;
                mouseDownTime = eventPtr.time;
                pendingMousePress = true;
                if (!mouse.position.ReadValueFromEvent(eventPtr, out mouseDownPosition))
                    mouseDownPosition = mouse.position.ReadValue();
            }
        }

        private void OnEnable()
        {
            FindNewTouch();
            InputSystem.onEvent += OnInputEvent;
        }

        private void OnApplicationFocus(bool value)
        {
            focused = value;
            if (!value) CancelOwner();
            else FindNewTouch();
        }

        private void OnApplicationPause(bool value)
        {
            paused = value;
            if (value) CancelOwner();
            else FindNewTouch();
        }
        private void OnDisable()
        {
            InputSystem.onEvent -= OnInputEvent;
            CancelOwner();
        }
    }
}
