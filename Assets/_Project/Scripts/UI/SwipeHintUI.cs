using System.Collections;
using UnityEngine;
using TMPro;
using NeonSerpent.Snake;

namespace NeonSerpent.UI
{
    /// <summary>
    /// Displays a "SWIPE TO STEER" hint with a cycling directional arrow on the very first
    /// session only. Dismissed automatically on the player's first valid swipe (via
    /// <see cref="SnakeController.OnMoved"/>) or after a 5-second timeout. Uses a
    /// CanvasGroup for smooth fade in / fade out.
    /// </summary>
    public class SwipeHintUI : MonoBehaviour
    {
        private const string PREFS_KEY       = "ns_swipe_hint_shown";
        private const float  FADE_IN_TIME    = 0.4f;
        private const float  FADE_OUT_TIME   = 0.3f;
        private const float  AUTO_DISMISS_DELAY = 5f;
        private const float  ARROW_CYCLE_INTERVAL = 0.8f;

        // Directional arrows cycle: Up → Right → Down → Left
        private static readonly string[] ARROWS = { "↑", "→", "↓", "←" };

        [Header("References")]
        [SerializeField] private CanvasGroup    _canvasGroup;
        [SerializeField] private TMP_Text       _arrowText;
        [SerializeField] private SnakeController _snake;

        private Coroutine _fadeCoroutine;
        private Coroutine _arrowCoroutine;
        private Coroutine _autoDismissCoroutine;

        private bool _dismissed;
        // Set to true only after Start() has confirmed the hint should be displayed.
        // Prevents OnMoved fired by SnakeController.Initialize() — which can happen
        // before Start() runs — from permanently dismissing the hint on first install.
        private bool _initialized;

        private void Start()
        {
            // Only show on the very first session
            if (PlayerPrefs.GetInt(PREFS_KEY, 0) != 0)
            {
                gameObject.SetActive(false);
                return;
            }

            _initialized = true;

            if (_canvasGroup != null)
                _canvasGroup.alpha = 0f;

            _fadeCoroutine = StartCoroutine(FadeRoutine(0f, 1f, FADE_IN_TIME));
            _arrowCoroutine = StartCoroutine(CycleArrows());
            _autoDismissCoroutine = StartCoroutine(AutoDismiss());
        }

        private void OnEnable()
        {
            if (_snake != null)
                _snake.OnMoved += HandleMoved;
        }

        private void OnDisable()
        {
            if (_snake != null)
                _snake.OnMoved -= HandleMoved;
        }

        private void OnDestroy()
        {
            // Belt-and-braces: unsubscribe in case OnDisable was not called
            if (_snake != null)
                _snake.OnMoved -= HandleMoved;
        }

        /// <summary>
        /// Called when the snake moves for the first time. Marks the hint as seen and
        /// begins the fade-out sequence. Ignored if Start() has not yet completed its
        /// initialisation check — guards against the premature OnMoved fired by
        /// SnakeController.Initialize() before this MonoBehaviour's Start() runs.
        /// </summary>
        private void HandleMoved(Vector2Int _)
        {
            if (!_initialized) return;
            Dismiss();
        }

        /// <summary>Dismiss the hint: mark seen, stop internal coroutines, fade out.</summary>
        private void Dismiss()
        {
            if (_dismissed) return;
            _dismissed = true;

            PlayerPrefs.SetInt(PREFS_KEY, 1);
            PlayerPrefs.Save();

            // Stop cycling and auto-dismiss; the fade coroutine will clean up
            if (_arrowCoroutine != null)
            {
                StopCoroutine(_arrowCoroutine);
                _arrowCoroutine = null;
            }
            if (_autoDismissCoroutine != null)
            {
                StopCoroutine(_autoDismissCoroutine);
                _autoDismissCoroutine = null;
            }
            if (_fadeCoroutine != null)
            {
                StopCoroutine(_fadeCoroutine);
                _fadeCoroutine = null;
            }

            _fadeCoroutine = StartCoroutine(FadeOutAndHide());
        }

        private IEnumerator AutoDismiss()
        {
            float elapsed = 0f;
            while (elapsed < AUTO_DISMISS_DELAY)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
            Dismiss();
        }

        /// <summary>Cycles the arrow text through the four cardinal directions.</summary>
        private IEnumerator CycleArrows()
        {
            int index = 0;
            while (true)
            {
                if (_arrowText != null)
                    _arrowText.text = ARROWS[index];
                index = (index + 1) % ARROWS.Length;

                float elapsed = 0f;
                while (elapsed < ARROW_CYCLE_INTERVAL)
                {
                    elapsed += Time.unscaledDeltaTime;
                    yield return null;
                }
            }
        }

        private IEnumerator FadeRoutine(float from, float to, float duration)
        {
            if (_canvasGroup == null) yield break;
            float elapsed = 0f;
            _canvasGroup.alpha = from;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                _canvasGroup.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(elapsed / duration));
                yield return null;
            }
            _canvasGroup.alpha = to;
            _fadeCoroutine = null;
        }

        private IEnumerator FadeOutAndHide()
        {
            float startAlpha = _canvasGroup != null ? _canvasGroup.alpha : 1f;
            yield return StartCoroutine(FadeRoutine(startAlpha, 0f, FADE_OUT_TIME));
            gameObject.SetActive(false);
        }
    }
}
