using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using NeonSerpent.PowerUps;

namespace NeonSerpent.UI
{
    /// <summary>
    /// Displays up to 3 active power-up slots on the right side of the HUD.
    /// Each slot shows the power-up name and a fill bar shrinking as the effect expires.
    /// Subscribes to PowerUpManager.OnEffectActivated / OnEffectDeactivated.
    /// </summary>
    public class PowerUpHUD : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PowerUpManager _powerUpManager;

        [Header("Slot Prefab (auto-created by NeonSerpentSetup)")]
        [SerializeField] private GameObject _slotPrefab;

        [Header("Container — vertical stack anchored right side")]
        [SerializeField] private RectTransform _container;

        // Maximum simultaneous slots displayed
        private const int MAX_SLOTS = 3;

        // Colour palette per power-up type
        private static readonly Dictionary<PowerUpType, Color> _typeColors = new Dictionary<PowerUpType, Color>
        {
            { PowerUpType.SpeedBoost,      new Color(1.0f, 0.6f, 0.0f) },
            { PowerUpType.Shield,          new Color(0.0f, 0.6f, 1.0f) },
            { PowerUpType.ScoreMultiplier, new Color(1.0f, 0.1f, 0.8f) },
            { PowerUpType.GhostMode,       new Color(0.5f, 0.5f, 0.9f) },
            { PowerUpType.ShrinkPill,      new Color(0.3f, 1.0f, 0.0f) },
            { PowerUpType.Poison,          new Color(0.5f, 0.0f, 0.7f) },
        };

        private class Slot
        {
            public GameObject Root;
            public TMP_Text   NameText;
            public Image      FillBar;
            public float      TotalDuration;
            public bool       Active;
        }

        private readonly Dictionary<PowerUpType, Slot> _activeSlots = new Dictionary<PowerUpType, Slot>();
        private readonly Queue<Slot>                    _slotPool    = new Queue<Slot>();

        #region Unity Lifecycle

        private void Awake()
        {
            // Pre-populate pool
            for (int i = 0; i < MAX_SLOTS; i++)
                _slotPool.Enqueue(CreateSlot());
        }

        private void OnEnable()
        {
            if (_powerUpManager != null)
            {
                _powerUpManager.OnEffectActivated   += HandleActivated;
                _powerUpManager.OnEffectDeactivated += HandleDeactivated;
            }
        }

        private void OnDisable()
        {
            if (_powerUpManager != null)
            {
                _powerUpManager.OnEffectActivated   -= HandleActivated;
                _powerUpManager.OnEffectDeactivated -= HandleDeactivated;
            }
        }

        private void Update()
        {
            if (_powerUpManager == null) return;

            foreach (var kvp in _activeSlots)
            {
                PowerUpType type = kvp.Key;
                Slot        slot = kvp.Value;

                if (!slot.Active || slot.TotalDuration <= 0f) continue;

                float remaining = _powerUpManager.GetRemainingTime(type);
                slot.FillBar.fillAmount = slot.TotalDuration > 0f
                    ? remaining / slot.TotalDuration
                    : 1f;
            }
        }

        #endregion

        #region Event Handlers

        /// <summary>Show a slot for the newly activated power-up.</summary>
        private void HandleActivated(PowerUpType type, float duration)
        {
            // Instant effects (duration == 0) have nothing to display
            if (duration <= 0f) return;
            // Reuse slot if same type re-applied before expiry
            if (_activeSlots.TryGetValue(type, out var existing))
            {
                RefreshSlot(existing, type, duration);
                return;
            }

            if (_slotPool.Count == 0) return; // All slots in use — drop silently

            var slot = _slotPool.Dequeue();
            _activeSlots[type] = slot;
            RefreshSlot(slot, type, duration);
        }

        /// <summary>Hide and recycle the slot when the effect expires.</summary>
        private void HandleDeactivated(PowerUpType type)
        {
            if (!_activeSlots.TryGetValue(type, out var slot)) return;

            slot.Active = false;
            slot.Root.SetActive(false);
            _activeSlots.Remove(type);
            _slotPool.Enqueue(slot);
        }

        #endregion

        #region Slot Helpers

        private void RefreshSlot(Slot slot, PowerUpType type, float duration)
        {
            slot.TotalDuration = duration;
            slot.Active        = true;
            slot.Root.SetActive(true);

            slot.NameText.text = FormatName(type);

            Color c = _typeColors.TryGetValue(type, out var col) ? col : Color.white;
            slot.FillBar.color    = c;
            slot.FillBar.fillAmount = 1f;
        }

        private Slot CreateSlot()
        {
            GameObject root;

            if (_slotPrefab != null)
            {
                root = Instantiate(_slotPrefab, _container);
            }
            else
            {
                // Runtime fallback: build a minimal slot procedurally
                root = new GameObject("PowerUpSlot", typeof(RectTransform));
                root.transform.SetParent(_container, false);

                var bg = new GameObject("Background", typeof(Image));
                bg.transform.SetParent(root.transform, false);
                var bgImg = bg.GetComponent<Image>();
                bgImg.color = new Color(0f, 0f, 0f, 0.6f);
                StretchToParent(bgImg.rectTransform);

                var fill = new GameObject("FillBar", typeof(Image));
                fill.transform.SetParent(root.transform, false);
                var fillImg = fill.GetComponent<Image>();
                fillImg.type       = Image.Type.Filled;
                fillImg.fillMethod = Image.FillMethod.Horizontal;
                fillImg.fillAmount = 1f;
                fillImg.color      = Color.white;
                StretchToParent(fillImg.rectTransform);

                var nameGO  = new GameObject("NameText", typeof(TextMeshProUGUI));
                nameGO.transform.SetParent(root.transform, false);
                var nameTmp = nameGO.GetComponent<TextMeshProUGUI>();
                nameTmp.fontSize  = 14f;
                nameTmp.alignment = TextAlignmentOptions.MidlineLeft;
                nameTmp.color     = Color.white;
                StretchToParent(nameTmp.rectTransform, 6f, 2f);

                var slotRT = root.GetComponent<RectTransform>();
                slotRT.sizeDelta = new Vector2(0f, 36f);

                var s = new Slot
                {
                    Root     = root,
                    NameText = nameTmp,
                    FillBar  = fillImg
                };
                root.SetActive(false);
                return s;
            }

            // If a prefab was supplied, locate the expected named children
            var slot = new Slot
            {
                Root     = root,
                NameText = root.GetComponentInChildren<TMP_Text>(),
                FillBar  = root.GetComponentInChildren<Image>()
            };
            root.SetActive(false);
            return slot;
        }

        private static void StretchToParent(RectTransform rt, float padLeft = 0f, float padTop = 0f)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(padLeft, 0f);
            rt.offsetMax = new Vector2(0f, -padTop);
        }

        private static string FormatName(PowerUpType type) => type switch
        {
            PowerUpType.SpeedBoost      => "SPEED",
            PowerUpType.Shield          => "SHIELD",
            PowerUpType.ScoreMultiplier => "2x SCORE",
            PowerUpType.GhostMode       => "GHOST",
            PowerUpType.ShrinkPill      => "SHRINK",
            PowerUpType.Poison          => "POISON",
            _                           => type.ToString().ToUpper()
        };

        #endregion
    }
}
