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

        [Header("Entrance Animation")]
        [SerializeField] private float _titleSlideDist  = 60f;
        [SerializeField] private float _titleFadeDur    = 0.45f;
        [SerializeField] private float _buttonsFadeDur  = 0.35f;
        [SerializeField] private float _buttonsDelay    = 0.25f;

        private void Start()
        {
            WireButton("ClassicBtn",     () => StartMode(GameMode.ClassicEndless));
            WireButton("TimeAttackBtn",  () => StartMode(GameMode.TimeAttack));
            WireButton("CampaignBtn",    () => SceneLoader.Instance?.LoadScene(Constants.SCENE_LEVEL_SELECT));
            WireButton("LeaderboardBtn", () => SceneLoader.Instance?.LoadScene(Constants.SCENE_LEADERBOARD));
            WireButton("ShopBtn",        () => SceneLoader.Instance?.LoadScene(Constants.SCENE_SHOP));
            WireButton("SettingsBtn",    () => SceneLoader.Instance?.LoadScene(Constants.SCENE_SETTINGS));

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

            var scores = SaveManager.Instance.Data.GetLocalTopScores(Constants.LEADERBOARD_CLASSIC, 1);
            long best  = scores.Count > 0 ? scores[0].Score : 0;
            _bestScoreText.text = best > 0 ? $"BEST  {best:N0}" : string.Empty;
        }

        // ─────────────────────────────────────────────────────────
        // BUTTON WIRING
        // ─────────────────────────────────────────────────────────

        private void WireButton(string btnName, UnityEngine.Events.UnityAction action)
        {
            var btn = GetComponentInParent<Canvas>()?.GetComponentInChildren<Transform>()
                      ?.Find(btnName)?.GetComponent<Button>();

            if (btn == null)
            {
                var allButtons = FindObjectsByType<Button>(FindObjectsSortMode.None);
                foreach (var b in allButtons)
                    if (b.gameObject.name == btnName) { btn = b; break; }
            }

            btn?.onClick.AddListener(action);
        }

        private void StartMode(GameMode mode)
        {
            SceneLoader.Instance?.LoadScene(Constants.SCENE_GAME,
                () => GameManager.Instance?.StartGame(mode));
        }
    }
}
