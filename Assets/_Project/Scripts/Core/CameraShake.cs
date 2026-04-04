using System.Collections;
using UnityEngine;

namespace NeonSerpent.Core
{
    /// <summary>
    /// Scene-local camera shake controller. Attach to the Main Camera in the Game scene.
    ///
    /// Exposes a static <see cref="Instance"/> for null-safe access from anywhere:
    ///   CameraShake.Instance?.Shake(0.3f, 0.15f);  // death / wall hit
    ///   CameraShake.Instance?.Shake(0.1f, 0.05f);  // eat food (subtle)
    ///
    /// Intentionally does NOT inherit Singleton{T} because this component must NOT
    /// survive scene transitions — it is tied to the Main Camera in the Game scene only.
    /// The static Instance is cleared on OnDestroy so calls from other systems fail safely.
    ///
    /// If a shake is already running when Shake() is called, the new call supersedes
    /// the old one immediately — the camera never accumulates offset.
    /// All offsets are applied in local space so the shake works regardless of camera position.
    /// </summary>
    public class CameraShake : MonoBehaviour
    {
        // ─────────────────────────────────────────────────────────────────────
        // STATIC INSTANCE (scene-local, not DontDestroyOnLoad)
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// The active CameraShake in the current scene. Null if no Camera is present
        /// (e.g., Bootstrap or MainMenu scenes). Always null-check before calling.
        /// </summary>
        public static CameraShake Instance { get; private set; }

        // ─────────────────────────────────────────────────────────────────────
        // STATE
        // ─────────────────────────────────────────────────────────────────────

        private Vector3   _originalLocalPosition;
        private Coroutine _shakeCoroutine;

        // ─────────────────────────────────────────────────────────────────────
        // LIFECYCLE
        // ─────────────────────────────────────────────────────────────────────

        private void Awake()
        {
            Instance                = this;
            _originalLocalPosition  = transform.localPosition;
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        private void OnDisable()
        {
            // Restore camera to its resting position if disabled mid-shake.
            if (_shakeCoroutine != null)
            {
                StopCoroutine(_shakeCoroutine);
                _shakeCoroutine = null;
            }
            transform.localPosition = _originalLocalPosition;
        }

        // ─────────────────────────────────────────────────────────────────────
        // PUBLIC API
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Shakes the camera for <paramref name="duration"/> seconds with a maximum
        /// displacement of <paramref name="magnitude"/> Unity units. Any in-progress
        /// shake is stopped immediately and replaced by the new one.
        /// </summary>
        /// <param name="duration">Shake lifetime in seconds.</param>
        /// <param name="magnitude">Peak random displacement per axis in Unity units.</param>
        public void Shake(float duration, float magnitude)
        {
            if (duration <= 0f || magnitude <= 0f) return;

            if (_shakeCoroutine != null)
            {
                StopCoroutine(_shakeCoroutine);
                _shakeCoroutine = null;
                transform.localPosition = _originalLocalPosition;
            }

            _shakeCoroutine = StartCoroutine(ShakeRoutine(duration, magnitude));
        }

        // ─────────────────────────────────────────────────────────────────────
        // COROUTINE
        // ─────────────────────────────────────────────────────────────────────

        private IEnumerator ShakeRoutine(float duration, float magnitude)
        {
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;

                // Linearly attenuate towards zero so the shake eases out naturally.
                float attenuation = 1f - Mathf.Clamp01(elapsed / duration);
                float strength    = magnitude * attenuation;

                // Offset in X/Y only — never touch Z (camera depth axis).
                transform.localPosition = _originalLocalPosition + new Vector3(
                    Random.Range(-strength, strength),
                    Random.Range(-strength, strength),
                    0f);

                yield return null;
            }

            transform.localPosition = _originalLocalPosition;
            _shakeCoroutine = null;
        }
    }
}
