using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace NeonSerpent.VFX
{
    /// <summary>
    /// Full-screen color flash effect. Attach to the HUD canvas.
    /// Scene-local singleton: not DontDestroyOnLoad (same pattern as CameraShake).
    /// Creates its own full-screen Image overlay at runtime — no prefab required.
    /// </summary>
    public class ScreenFlash : MonoBehaviour
    {
        public static ScreenFlash Instance { get; private set; }

        private CanvasGroup _canvasGroup;
        private Image       _flashImage;
        private Coroutine   _flashCoroutine;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            BuildFlashOverlay();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void OnDisable()
        {
            if (_flashCoroutine != null)
            {
                StopCoroutine(_flashCoroutine);
                _flashCoroutine = null;
            }
            if (_canvasGroup != null)
                _canvasGroup.alpha = 0f;
        }

        /// <summary>
        /// Flash the screen with the given color, fading from full alpha to zero over
        /// the specified duration in seconds.
        /// </summary>
        public void Flash(Color color, float duration = 0.2f)
        {
            if (_flashImage == null || _canvasGroup == null) return;

            if (_flashCoroutine != null)
            {
                StopCoroutine(_flashCoroutine);
                _flashCoroutine = null;
            }

            _flashImage.color    = color;
            _canvasGroup.alpha   = 1f;
            _flashCoroutine      = StartCoroutine(FadeRoutine(duration));
        }

        private IEnumerator FadeRoutine(float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                _canvasGroup.alpha = 1f - Mathf.Clamp01(elapsed / duration);
                yield return null;
            }
            _canvasGroup.alpha = 0f;
            _flashCoroutine    = null;
        }

        /// <summary>
        /// Builds a full-screen Image inside a CanvasGroup child of this GameObject.
        /// This component must live on a Canvas or a child of one.
        /// </summary>
        private void BuildFlashOverlay()
        {
            var overlayGO = new GameObject("[ScreenFlashOverlay]");
            overlayGO.transform.SetParent(transform, false);

            var rt          = overlayGO.AddComponent<RectTransform>();
            rt.anchorMin    = Vector2.zero;
            rt.anchorMax    = Vector2.one;
            rt.offsetMin    = Vector2.zero;
            rt.offsetMax    = Vector2.zero;

            _flashImage       = overlayGO.AddComponent<Image>();
            _flashImage.color = Color.white;
            _flashImage.raycastTarget = false;

            _canvasGroup                 = overlayGO.AddComponent<CanvasGroup>();
            _canvasGroup.alpha           = 0f;
            _canvasGroup.blocksRaycasts  = false;
            _canvasGroup.interactable    = false;

            // Ensure it renders on top of all other HUD elements
            var canvas = overlayGO.AddComponent<Canvas>();
            canvas.overrideSorting = true;
            canvas.sortingOrder    = 100;
        }
    }
}
