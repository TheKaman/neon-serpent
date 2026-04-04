using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using NeonSerpent.Utilities;

namespace NeonSerpent.Core
{
    /// <summary>
    /// Handles all scene transitions with a fade-to-black effect.
    /// Lives in Bootstrap scene (DontDestroyOnLoad via Singleton).
    /// </summary>
    public class SceneLoader : Singleton<SceneLoader>
    {
        [SerializeField] private CanvasGroup _fadeOverlay;
        [SerializeField] private float _fadeDuration = 0.3f;

        private bool _isTransitioning;

        /// <summary>Load a scene by name with a fade transition.</summary>
        public void LoadScene(string sceneName, Action onMidFade = null)
        {
            if (_isTransitioning) return;
            StartCoroutine(LoadSceneRoutine(sceneName, onMidFade));
        }

        private IEnumerator LoadSceneRoutine(string sceneName, Action onMidFade)
        {
            _isTransitioning = true;

            if (_fadeOverlay != null)
                yield return StartCoroutine(Fade(0f, 1f));

            onMidFade?.Invoke();

            AsyncOperation load = SceneManager.LoadSceneAsync(sceneName);
            if (load == null)
            {
                Debug.LogError($"[SceneLoader] Scene '{sceneName}' not found. Make sure it is added to Build Settings.");
                _isTransitioning = false;
                if (_fadeOverlay != null) _fadeOverlay.alpha = 0f;
                yield break;
            }

            yield return new WaitUntil(() => load.isDone);

            if (_fadeOverlay != null)
                yield return StartCoroutine(Fade(1f, 0f));

            _isTransitioning = false;
        }

        private IEnumerator Fade(float from, float to)
        {
            if (_fadeOverlay == null) yield break;
            float elapsed = 0f;
            _fadeOverlay.blocksRaycasts = true;
            while (elapsed < _fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                _fadeOverlay.alpha = Mathf.Lerp(from, to, elapsed / _fadeDuration);
                yield return null;
            }
            _fadeOverlay.alpha = to;
            if (to == 0f) _fadeOverlay.blocksRaycasts = false;
        }
    }
}
