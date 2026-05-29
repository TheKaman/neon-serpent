using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using NeonSerpent.Core;
using NeonSerpent.Utilities;
using NeonSerpent.Ads;
using NeonSerpent.SaveData;
using NeonSerpent.Leaderboard;

namespace NeonSerpent.UI
{
    /// <summary>
    /// Main menu screen. Wires button clicks to game mode selection and scene navigation.
    /// Plays a staggered entrance animation on load and shows the player's best score.
    /// </summary>
    public class MainMenuUI : MonoBehaviour
    {
        [Header("Panels (assign via Inspector or NeonSerpentSetup)")]
        [SerializeField] private CanvasGroup _titleGroup;
        [SerializeField] private CanvasGroup _buttonsGroup;

        [Header("Best Score")]
        [SerializeField] private TMP_Text _bestScoreText;

        [Header("Buttons")]
        [SerializeField] private Button _classicBtn;
        [SerializeField] private Button _timeAttackBtn;
        [SerializeField] private Button _campaignBtn;
        [SerializeField] private Button _leaderboardBtn;
        [SerializeField] private Button _shopBtn;
        [SerializeField] private Button _settingsBtn;

        [Header("Entrance Animation")]
        [SerializeField] private float _titleSlideDist  = 60f;
        [SerializeField] private float _titleFadeDur    = 0.45f;
        [SerializeField] private float _buttonsFadeDur  = 0.35f;
        [SerializeField] private float _buttonsDelay    = 0.25f;

        private void Start()
        {
            _classicBtn?.onClick.AddListener(    () => StartMode(GameMode.ClassicEndless));
            _timeAttackBtn?.onClick.AddListener( () => StartMode(GameMode.TimeAttack));
            _campaignBtn?.onClick.AddListener(   () => SceneLoader.Instance?.LoadScene(Constants.SCENE_LEVEL_SELECT));
            _leaderboardBtn?.onClick.AddListener(() => SceneLoader.Instance?.LoadScene(Constants.SCENE_LEADERBOARD));
            _shopBtn?.onClick.AddListener(       () => SceneLoader.Instance?.LoadScene(Constants.SCENE_SHOP));
            _settingsBtn?.onClick.AddListener(   () => SceneLoader.Instance?.LoadScene(Constants.SCENE_SETTINGS));

            RefreshBestScore();
            StartCoroutine(EntranceRoutine());
        }

        private void OnEnable()  => AdManager.Instance?.ShowBanner();
        private void OnDisable() => AdManager.Instance?.HideBanner();

        // ─────────────────────────────────────────────────────────
        // ENTRANCE ANIMATION
        // ─────────────────────────────────────────────────────────

        private IEnumerator EntranceRoutine()
        {
            // Title: slide down from above and fade in
            if (_titleGroup != null)
            {
                _titleGroup.alpha = 0f;
                var rt  = _titleGroup.GetComponent<RectTransform>();
                Vector2 endPos = rt != null ? rt.anchoredPosition : Vector2.zero;
                Vector2 startPos = endPos + Vector2.up * _titleSlideDist;
                if (rt != null) rt.anchoredPosition = startPos;

                float elapsed = 0f;
                while (elapsed < _titleFadeDur)
                {
                    elapsed += Time.unscaledDeltaTime;
                    float t  = Mathf.SmoothStep(0f, 1f, elapsed / _titleFadeDur);
                    _titleGroup.alpha = t;
                    if (rt != null) rt.anchoredPosition = Vector2.Lerp(startPos, endPos, t);
                    yield return null;
                }
                _titleGroup.alpha = 1f;
                if (rt != null) rt.anchoredPosition = endPos;
            }

            yield return new WaitForSecondsRealtime(_buttonsDelay);

            // Buttons panel: fade in
            if (_buttonsGroup != null)
            {
                _buttonsGroup.alpha = 0f;
                float elapsed = 0f;
                while (elapsed < _buttonsFadeDur)
                {
                    elapsed += Time.unscaledDeltaTime;
                    _buttonsGroup.alpha = Mathf.SmoothStep(0f, 1f, elapsed / _buttonsFadeDur);
                    yield return null;
                }
                _buttonsGroup.alpha = 1f;
            }
        }

        // ─────────────────────────────────────────────────────────
        // BEST SCORE
        // ─────────────────────────────────────────────────────────

        private void RefreshBestScore()
        {
            if (_bestScoreText == null || SaveManager.Instance == null) return;

            // Show the highest score across ALL three modes, not just Classic. Previously this
            // hardcoded LEADERBOARD_CLASSIC, so a Time Attack / Campaign player always saw
            // "BEST 0" on the menu even after high-scoring runs.
            long best = 0;
            best = System.Math.Max(best, TopScoreFor(Constants.LEADERBOARD_CLASSIC));
            best = System.Math.Max(best, TopScoreFor(Constants.LEADERBOARD_TIME_ATTACK));
            best = System.Math.Max(best, TopScoreFor(Constants.LEADERBOARD_CAMPAIGN));

            _bestScoreText.text = best > 0 ? $"BEST  {best:N0}" : string.Empty;
        }

        /// <summary>Returns the player's top local score for the given leaderboard, or 0 if none.</summary>
        private long TopScoreFor(string leaderboardId)
        {
            var scores = SaveManager.Instance.Data.GetLocalTopScores(leaderboardId, 1);
            return scores.Count > 0 ? scores[0].Score : 0;
        }

        private void StartMode(GameMode mode)
        {
            SceneLoader.Instance?.LoadScene(Constants.SCENE_GAME,
                () => GameManager.Instance?.StartGame(mode));
        }
    }
}
