using System;
using System.Collections;
using UnityEngine;
using TMPro;

namespace NeonSerpent.UI
{
    /// <summary>
    /// Displays a 3 → 2 → 1 → GO! countdown before the snake starts moving.
    /// Freezes time via Time.timeScale = 0 for the duration so no game systems
    /// advance. Uses Time.unscaledDeltaTime internally so the coroutine is
    /// unaffected by the frozen timescale.
    /// After the sequence completes the panel is hidden and OnCountdownComplete fires.
    /// </summary>
    public class CountdownUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text _countdownText;
        [SerializeField] private GameObject _panel;

        /// <summary>Fires once the countdown finishes and the game may begin.</summary>
        public event Action OnCountdownComplete;

        private Coroutine _countdownCoroutine;

        // ── Colours ────────────────────────────────────────────
        private static readonly Color _numberColor = new Color(0f, 1f, 0.8f, 1f);   // neon cyan
        private static readonly Color _goColor     = new Color(1f, 0.85f, 0f, 1f);  // gold

        private void OnDisable()
        {
            StopCountdown();
            Time.timeScale = 1f; // safety net — never leave time frozen if object is disabled
        }

        // ── Public API ─────────────────────────────────────────

        /// <summary>Begin the 3-2-1-GO sequence. Freezes timeScale until complete.</summary>
        public void StartCountdown()
        {
            StopCountdown();
            // Reset scale in case a previous CancelCountdown() left the text mid-animation (L2).
            if (_countdownText != null)
                _countdownText.transform.localScale = Vector3.one;
            if (_panel != null) _panel.SetActive(true);
            _countdownCoroutine = StartCoroutine(RunCountdown());
        }

        /// <summary>
        /// Cancels an in-progress countdown without firing <see cref="OnCountdownComplete"/>.
        /// Restores <see cref="Time.timeScale"/> to 1 so the game is not left frozen.
        /// Call this when the game ends (game-over, scene unload) during the countdown window.
        /// </summary>
        public void CancelCountdown()
        {
            StopCountdown();
            if (_panel != null) _panel.SetActive(false);
            Time.timeScale = 1f;
        }

        // ── Internal ───────────────────────────────────────────

        private void StopCountdown()
        {
            if (_countdownCoroutine != null)
            {
                StopCoroutine(_countdownCoroutine);
                _countdownCoroutine = null;
            }
        }

        private IEnumerator RunCountdown()
        {
            Time.timeScale = 0f;

            // Steps: "3", "2", "1" — each over 0.8 s unscaled
            string[] steps = { "3", "2", "1" };
            foreach (string step in steps)
            {
                yield return StartCoroutine(AnimateNumber(step, _numberColor, 0.8f));
            }

            // "GO!" — shown briefly then fades over ~0.7 s
            yield return StartCoroutine(AnimateGo());

            // Hide panel, restore time, notify listeners
            if (_panel != null) _panel.SetActive(false);
            if (_countdownText != null) _countdownText.gameObject.SetActive(false);

            Time.timeScale = 1f;
            _countdownCoroutine = null;

            OnCountdownComplete?.Invoke();
        }

        /// <summary>
        /// Scales the label from 1.5× down to 1.0× over <paramref name="duration"/> unscaled seconds,
        /// then briefly holds before moving to the next step.
        /// </summary>
        private IEnumerator AnimateNumber(string label, Color color, float duration)
        {
            if (_countdownText == null) yield break;

            _countdownText.gameObject.SetActive(true);
            _countdownText.text  = label;
            _countdownText.color = color;

            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                // Scale from 1.5 → 1.0 with an ease-out feel (smoothstep t)
                float smooth   = t * t * (3f - 2f * t);
                float scale    = Mathf.Lerp(1.5f, 1.0f, smooth);
                _countdownText.transform.localScale = new Vector3(scale, scale, 1f);

                // Slight fade-out in the last quarter so it exits cleanly
                float alpha = elapsed > duration * 0.75f
                    ? Mathf.Lerp(1f, 0f, (elapsed - duration * 0.75f) / (duration * 0.25f))
                    : 1f;
                _countdownText.color = new Color(color.r, color.g, color.b, alpha);

                yield return null;
            }

            // Ensure final state is fully transparent before next number appears
            _countdownText.color = new Color(color.r, color.g, color.b, 0f);
        }

        /// <summary>Shows "GO!" in gold, scales in, then fades out over ~0.7 s.</summary>
        private IEnumerator AnimateGo()
        {
            if (_countdownText == null) yield break;

            const float holdDuration = 0.25f;
            const float fadeDuration = 0.45f;

            _countdownText.gameObject.SetActive(true);
            _countdownText.text  = "GO!";
            _countdownText.color = _goColor;
            _countdownText.transform.localScale = new Vector3(1.5f, 1.5f, 1f);

            // Scale pop: 1.5 → 1.1 during hold
            float elapsed = 0f;
            while (elapsed < holdDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t     = Mathf.Clamp01(elapsed / holdDuration);
                float scale = Mathf.Lerp(1.5f, 1.1f, t);
                _countdownText.transform.localScale = new Vector3(scale, scale, 1f);
                yield return null;
            }

            // Fade out
            elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float alpha = Mathf.Lerp(1f, 0f, elapsed / fadeDuration);
                _countdownText.color = new Color(_goColor.r, _goColor.g, _goColor.b, alpha);
                yield return null;
            }

            _countdownText.color = new Color(_goColor.r, _goColor.g, _goColor.b, 0f);
        }
    }
}
