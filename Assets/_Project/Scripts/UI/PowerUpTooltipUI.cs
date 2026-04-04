using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using NeonSerpent.PowerUps;

namespace NeonSerpent.UI
{
    /// <summary>
    /// Displays a brief tooltip the first time each power-up type is collected.
    /// Subscribes to PowerUpManager.OnEffectActivated.
    /// Each type is shown exactly once per device install, tracked via PlayerPrefs.
    /// Tooltips are queued so rapid back-to-back pickups don't overlap.
    /// Uses unscaled time so it displays correctly during the 3-2-1 countdown.
    /// </summary>
    public class PowerUpTooltipUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private CanvasGroup _panel;
        [SerializeField] private TMP_Text    _nameText;
        [SerializeField] private TMP_Text    _descText;

        [Header("Wiring")]
        [SerializeField] private PowerUpManager _powerUpManager;

        [Header("Timing")]
        [SerializeField] private float _fadeTime    = 0.3f;
        [SerializeField] private float _displayTime = 2.5f;

        // ── Tooltip data per power-up type ────────────────────────────────────
        private static readonly Dictionary<PowerUpType, TooltipData> _data =
            new Dictionary<PowerUpType, TooltipData>
            {
                { PowerUpType.SpeedBoost,      new TooltipData("SPEED BOOST",   "2\u00d7 speed for 5 seconds",               new Color(1.00f, 0.92f, 0.00f)) },
                { PowerUpType.Shield,          new TooltipData("SHIELD",        "Absorbs one fatal collision",               new Color(0.00f, 0.80f, 1.00f)) },
                { PowerUpType.ScoreMultiplier, new TooltipData("2\u00d7 SCORE", "Double points for 8 seconds",               new Color(1.00f, 0.40f, 1.00f)) },
                { PowerUpType.GhostMode,       new TooltipData("GHOST MODE",    "Pass through your own body for 6 seconds",  new Color(0.40f, 1.00f, 0.40f)) },
                { PowerUpType.ShrinkPill,      new TooltipData("SHRINK",        "Removes 3 tail segments instantly",         new Color(1.00f, 0.50f, 0.00f)) },
                { PowerUpType.Poison,          new TooltipData("POISONED!",     "Half speed \u2014 next 2 foods score zero", new Color(0.60f, 1.00f, 0.20f)) },
            };

        private const string PREFS_PREFIX = "ns_tooltip_seen_";

        private readonly Queue<PowerUpType> _queue = new Queue<PowerUpType>();
        private bool      _showing;
        private Coroutine _showCoroutine;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            if (_panel != null)
            {
                _panel.alpha          = 0f;
                _panel.interactable   = false;
                _panel.blocksRaycasts = false;
            }
        }

        private void OnEnable()
        {
            if (_powerUpManager != null)
                _powerUpManager.OnEffectActivated += HandleEffectActivated;
        }

        private void OnDisable()
        {
            if (_powerUpManager != null)
                _powerUpManager.OnEffectActivated -= HandleEffectActivated;
        }

        // ── Event handler ─────────────────────────────────────────────────────

        /// <summary>Called whenever a power-up becomes active. Queues a first-time tooltip.</summary>
        private void HandleEffectActivated(PowerUpType type, float duration)
        {
            string key = PREFS_PREFIX + (int)type;
            if (PlayerPrefs.GetInt(key, 0) != 0) return;   // already shown for this type

            PlayerPrefs.SetInt(key, 1);
            PlayerPrefs.Save();

            _queue.Enqueue(type);
            if (!_showing)
                ShowNext();
        }

        // ── Display logic ─────────────────────────────────────────────────────

        private void ShowNext()
        {
            if (_queue.Count == 0) { _showing = false; return; }
            _showing = true;
            if (_showCoroutine != null) StopCoroutine(_showCoroutine);
            _showCoroutine = StartCoroutine(ShowRoutine(_queue.Dequeue()));
        }

        private IEnumerator ShowRoutine(PowerUpType type)
        {
            if (!_data.TryGetValue(type, out TooltipData d))
            {
                ShowNext();
                yield break;
            }

            if (_nameText != null) { _nameText.text = d.Name; _nameText.color = d.Color; }
            if (_descText != null)   _descText.text  = d.Desc;

            yield return StartCoroutine(Fade(0f, 1f));
            yield return new WaitForSecondsRealtime(_displayTime);
            yield return StartCoroutine(Fade(1f, 0f));

            ShowNext();
        }

        private IEnumerator Fade(float from, float to)
        {
            if (_panel == null) yield break;

            // Tooltip is purely informational — never block raycasts so the pause
            // button remains tappable while a tooltip is visible.
            _panel.interactable   = false;
            _panel.blocksRaycasts = false;

            float elapsed = 0f;
            while (elapsed < _fadeTime)
            {
                elapsed    += Time.unscaledDeltaTime;
                _panel.alpha = Mathf.Lerp(from, to, elapsed / _fadeTime);
                yield return null;
            }
            _panel.alpha = to;
        }

        // ── Inner type ────────────────────────────────────────────────────────

        private readonly struct TooltipData
        {
            public readonly string Name;
            public readonly string Desc;
            public readonly Color  Color;

            public TooltipData(string name, string desc, Color color)
            {
                Name  = name;
                Desc  = desc;
                Color = color;
            }
        }
    }
}
