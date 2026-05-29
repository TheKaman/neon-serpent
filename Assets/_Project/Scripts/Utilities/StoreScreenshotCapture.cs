using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using NeonSerpent.Core;
using NeonSerpent.Scoring;
using NeonSerpent.Utilities;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace NeonSerpent.Utilities
{
    /// <summary>
    /// Runtime automation component activated by StoreScreenshotTool.
    /// Navigates scenes, sets up game states, and captures 5 shots per requested size.
    /// All logic is guarded by #if UNITY_EDITOR and has zero runtime overhead in builds.
    /// </summary>
    public class StoreScreenshotCapture : MonoBehaviour
    {
#if UNITY_EDITOR
        private const string PREF_ACTIVE = "NeonSerpent_ScreenshotMode";
        private const string PREF_SIZES  = "NeonSerpent_Screenshot_Sizes";

        // (width, height, label) for each supported device class
        private static readonly (int w, int h, string label)[] SizeOptions =
        {
            (1080, 1920, "Phone"),
            (1200, 1920, "Tablet7"),
            (1600, 2560, "Tablet10"),
        };

        // Fires after Bootstrap scene's Awake() calls but before Start() —
        // singletons are ready but ApplicationController.Start() hasn't run yet.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void TryStart()
        {
            if (!EditorPrefs.GetBool(PREF_ACTIVE, false)) return;
            EditorPrefs.SetBool(PREF_ACTIVE, false); // clear immediately to prevent re-entry on next scene load

            var go = new GameObject("[ScreenshotCapture]");
            DontDestroyOnLoad(go);
            var capture = go.AddComponent<StoreScreenshotCapture>();
            capture.StartCoroutine(capture.RunCaptures());
        }

        private string _outputDir;

        private IEnumerator RunCaptures()
        {
            _outputDir = Path.GetFullPath(
                Path.Combine(Application.dataPath, "..", "Screenshots", "Store"));
            Directory.CreateDirectory(_outputDir);

            string requestedSizes = EditorPrefs.GetString(PREF_SIZES, "Phone");

            // Let ApplicationController.Start() fire so it kicks off the MainMenu load
            yield return null;

            foreach (var (w, h, label) in SizeOptions)
            {
                if (!requestedSizes.Contains(label)) continue;

                Screen.SetResolution(w, h, false);
                yield return new WaitForSeconds(0.3f);

                yield return CaptureSequence(w, h, label);
            }

            Debug.Log($"[Screenshots] Complete. Files in: {_outputDir}");
            EditorApplication.ExitPlaymode();
        }

        private IEnumerator CaptureSequence(int w, int h, string label)
        {
            // --- Shot 1: Main Menu ---
            yield return GoToMainMenu();
            yield return new WaitForSeconds(1.2f); // title animations settle
            yield return TakeShot("01_MainMenu", label, w, h);

            // --- Shot 2: Classic gameplay ---
            yield return NavigateTo(Constants.SCENE_GAME);
            if (GameManager.Instance != null)
                GameManager.Instance.StartGame(GameMode.ClassicEndless);
            // 5s covers a 3s countdown + 2s of visible snake movement
            yield return new WaitForSeconds(5f);
            yield return TakeShot("02_Gameplay", label, w, h);

            // --- Shot 3: Score multiplier active ---
            var sm = FindFirstObjectByType<ScoreManager>();
            if (sm != null)
            {
                sm.SetMultiplier(3);
                sm.AddScore(900); // triggers OnScoreChanged so HUD refreshes
            }
            yield return new WaitForSeconds(0.4f);
            yield return TakeShot("03_ScoreMultiplier", label, w, h);

            // --- Shot 4: Game over screen ---
            if (sm != null) sm.AddScore(2500); // make the final score look impressive
            yield return new WaitForSeconds(0.2f);
            if (GameManager.Instance != null &&
                GameManager.Instance.CurrentState == GameState.Playing)
                GameManager.Instance.TriggerGameOver();
            yield return new WaitForSeconds(1.8f); // GameOverUI fade-in
            yield return TakeShot("04_GameOver", label, w, h);

            // --- Shot 5: Campaign level select ---
            yield return NavigateTo(Constants.SCENE_LEVEL_SELECT);
            yield return new WaitForSeconds(1.2f);
            yield return TakeShot("05_LevelSelect", label, w, h);
        }

        // Navigate to a scene via SceneLoader (with fade), fallback to direct async load
        private IEnumerator NavigateTo(string sceneName)
        {
            if (SceneLoader.Instance != null)
                SceneLoader.Instance.LoadScene(sceneName);
            else
            {
                var op = SceneManager.LoadSceneAsync(sceneName);
                yield return new WaitUntil(() => op != null && op.isDone);
            }
            yield return WaitForScene(sceneName);
        }

        // On first run we're in Bootstrap and AppController is already loading MainMenu.
        // On subsequent runs (multi-size) we navigate from whatever scene we're on.
        private IEnumerator GoToMainMenu()
        {
            if (SceneManager.GetActiveScene().name != Constants.SCENE_MAIN_MENU)
                yield return NavigateTo(Constants.SCENE_MAIN_MENU);
            else
                yield return WaitForScene(Constants.SCENE_MAIN_MENU);
        }

        // Waits until the active scene matches, then buffers for SceneLoader's fade-in (0.3 s default)
        private IEnumerator WaitForScene(string sceneName)
        {
            yield return new WaitUntil(() =>
                SceneManager.GetActiveScene().name == sceneName);
            yield return new WaitForSeconds(0.6f);
        }

        private IEnumerator TakeShot(string shotName, string label, int w, int h)
        {
            yield return new WaitForEndOfFrame();
            string path = Path.Combine(_outputDir, $"{shotName}_{label}_{w}x{h}.png");
            ScreenCapture.CaptureScreenshot(path);
            yield return null; // one extra frame for write flush
            Debug.Log($"[Screenshots] Saved: {Path.GetFileName(path)}");
        }
#endif
    }
}
