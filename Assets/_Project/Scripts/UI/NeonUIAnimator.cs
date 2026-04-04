using UnityEngine;
using TMPro;

namespace NeonSerpent.UI
{
    /// <summary>
    /// Pulses the color of a TMP_Text element between two colors using a sin wave.
    /// Add to any text element that should have a neon glow animation effect.
    /// Runs in Update — lightweight, no allocations.
    /// </summary>
    public class NeonUIAnimator : MonoBehaviour
    {
        [SerializeField] private TMP_Text _text;
        [SerializeField] private Color    _colorA = new Color(0f, 1f, 0.8f);
        [SerializeField] private Color    _colorB = Color.white;
        [SerializeField] private float    _speed  = 1.5f;

        private void Awake()
        {
            // Auto-resolve the text component if not assigned in the Inspector
            if (_text == null)
                _text = GetComponent<TMP_Text>();
        }

        private void Update()
        {
            if (_text == null) return;

            // t oscillates smoothly between 0 and 1 using a sin wave
            float t = (Mathf.Sin(Time.time * _speed * Mathf.PI) + 1f) * 0.5f;
            _text.color = Color.Lerp(_colorA, _colorB, t);
        }
    }
}
