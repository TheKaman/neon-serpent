using System;
using UnityEngine;
using UnityEngine.InputSystem;
using NeonSerpent.Snake;

namespace NeonSerpent.Input
{
    /// <summary>
    /// Detects swipe gestures (touch + mouse) and arrow/WASD keys (editor only).
    /// Translates input into 4-directional Vector2Int for SnakeController.
    /// </summary>
    public class SwipeInputHandler : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private SnakeController _snake;

        [Header("Swipe Settings")]
        [SerializeField] private float _swipeThreshold = 30f;   // pixels — lower = easier to trigger
        [SerializeField] private float _maxSwipeTime   = 0.5f;  // seconds

        private Vector2 _touchStartPos;
        private float   _touchStartTime;
        private bool    _tracking;

        public event Action<Vector2Int> OnSwipeDetected;

        private void Update()
        {
            HandleKeyboard();
            HandleMouse();
            HandleTouch();
        }

        // ── Keyboard (works in Editor and on desktop builds) ──────────────────
        private void HandleKeyboard()
        {
            var kb = Keyboard.current;
            if (kb == null) return;

            if (kb.upArrowKey.wasPressedThisFrame    || kb.wKey.wasPressedThisFrame)
                Send(Vector2Int.up);
            else if (kb.downArrowKey.wasPressedThisFrame || kb.sKey.wasPressedThisFrame)
                Send(Vector2Int.down);
            else if (kb.leftArrowKey.wasPressedThisFrame || kb.aKey.wasPressedThisFrame)
                Send(Vector2Int.left);
            else if (kb.rightArrowKey.wasPressedThisFrame || kb.dKey.wasPressedThisFrame)
                Send(Vector2Int.right);
        }

        // ── Mouse drag (Editor testing) ───────────────────────────────────────
        private void HandleMouse()
        {
            var mouse = Mouse.current;
            if (mouse == null) return;

            if (mouse.leftButton.wasPressedThisFrame)
            {
                _touchStartPos  = mouse.position.ReadValue();
                _touchStartTime = Time.realtimeSinceStartup;
                _tracking       = true;
            }

            if (_tracking && mouse.leftButton.wasReleasedThisFrame)
                TryEvaluateSwipe(mouse.position.ReadValue());
        }

        // ── Touch (Android device) ────────────────────────────────────────────
        private void HandleTouch()
        {
            var screen = Touchscreen.current;
            if (screen == null) return;

            var touch = screen.primaryTouch;

            if (touch.press.wasPressedThisFrame)
            {
                _touchStartPos  = touch.position.ReadValue();
                _touchStartTime = Time.realtimeSinceStartup;
                _tracking       = true;
            }

            if (_tracking && touch.press.wasReleasedThisFrame)
                TryEvaluateSwipe(touch.position.ReadValue());
        }

        // ── Shared swipe evaluation ───────────────────────────────────────────
        private void TryEvaluateSwipe(Vector2 endPos)
        {
            _tracking = false;

            if (Time.realtimeSinceStartup - _touchStartTime > _maxSwipeTime) return;

            Vector2 delta = endPos - _touchStartPos;
            if (delta.magnitude < _swipeThreshold) return;

            Send(GetDominantAxis(delta));
        }

        private void Send(Vector2Int dir)
        {
            if (dir == Vector2Int.zero) return;
            OnSwipeDetected?.Invoke(dir);
            _snake?.SetDirection(dir);
        }

        private Vector2Int GetDominantAxis(Vector2 delta)
        {
            float absX = Mathf.Abs(delta.x);
            float absY = Mathf.Abs(delta.y);

            if (absX > absY * 1.5f)
                return delta.x > 0 ? Vector2Int.right : Vector2Int.left;

            if (absY > absX * 1.5f)
                return delta.y > 0 ? Vector2Int.up : Vector2Int.down;

            return Vector2Int.zero;
        }
    }
}
