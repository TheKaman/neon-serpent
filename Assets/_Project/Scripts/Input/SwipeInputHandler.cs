using System;
using UnityEngine;
using UnityEngine.InputSystem;
using NeonSerpent.Snake;

namespace NeonSerpent.Input
{
    /// <summary>
    /// Detects swipe gestures using the Unity Input System and translates them
    /// into 4-directional Vector2Int values for SnakeController.
    /// Rejects diagonal swipes to prevent accidental direction changes.
    /// </summary>
    public class SwipeInputHandler : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private SnakeController _snake;

        [Header("Swipe Settings")]
        [SerializeField] private float _swipeThreshold = 50f;  // pixels
        [SerializeField] private float _maxSwipeTime   = 0.35f; // seconds

        private Vector2 _touchStartPos;
        private float   _touchStartTime;
        private bool    _tracking;

        // Raised for any system that wants to know about swipes (e.g., UI, replay)
        public event Action<Vector2Int> OnSwipeDetected;

        private void OnEnable()
        {
            // Use the new Input System's Touchscreen device events
            if (Touchscreen.current != null)
            {
                Touchscreen.current.primaryTouch.press.started   += OnTouchStarted;
                Touchscreen.current.primaryTouch.press.canceled  += OnTouchEnded;
            }

            // Editor / mouse fallback
#if UNITY_EDITOR
            Mouse.current?.leftButton.WasPressedThisFrame.Equals(true);
#endif
        }

        private void OnDisable()
        {
            if (Touchscreen.current != null)
            {
                Touchscreen.current.primaryTouch.press.started   -= OnTouchStarted;
                Touchscreen.current.primaryTouch.press.canceled  -= OnTouchEnded;
            }
        }

        private void Update()
        {
            // Editor mouse fallback for swipe testing
#if UNITY_EDITOR
            if (Mouse.current != null)
            {
                if (Mouse.current.leftButton.wasPressedThisFrame)
                {
                    _touchStartPos  = Mouse.current.position.ReadValue();
                    _touchStartTime = Time.realtimeSinceStartup;
                    _tracking = true;
                }
                if (Mouse.current.leftButton.wasReleasedThisFrame && _tracking)
                {
                    TryEvaluateSwipe(Mouse.current.position.ReadValue());
                }
            }
#endif
        }

        private void OnTouchStarted(InputAction.CallbackContext ctx)
        {
            _touchStartPos  = Touchscreen.current.primaryTouch.position.ReadValue();
            _touchStartTime = Time.realtimeSinceStartup;
            _tracking = true;
        }

        private void OnTouchEnded(InputAction.CallbackContext ctx)
        {
            if (!_tracking) return;
            TryEvaluateSwipe(Touchscreen.current.primaryTouch.position.ReadValue());
        }

        private void TryEvaluateSwipe(Vector2 endPos)
        {
            _tracking = false;

            if (Time.realtimeSinceStartup - _touchStartTime > _maxSwipeTime) return;

            Vector2 delta = endPos - _touchStartPos;
            if (delta.magnitude < _swipeThreshold) return;

            Vector2Int dir = GetDominantAxis(delta);
            if (dir == Vector2Int.zero) return;

            OnSwipeDetected?.Invoke(dir);
            _snake?.SetDirection(dir);
        }

        private Vector2Int GetDominantAxis(Vector2 delta)
        {
            float absX = Mathf.Abs(delta.x);
            float absY = Mathf.Abs(delta.y);

            // Require clear dominance to reject diagonals
            if (absX > absY * 1.5f)
                return delta.x > 0 ? Vector2Int.right : Vector2Int.left;

            if (absY > absX * 1.5f)
                return delta.y > 0 ? Vector2Int.up : Vector2Int.down;

            return Vector2Int.zero; // diagonal — rejected
        }
    }
}
