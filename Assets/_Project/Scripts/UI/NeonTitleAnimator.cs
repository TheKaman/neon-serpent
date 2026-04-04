using System.Collections;
using UnityEngine;
using TMPro;

namespace NeonSerpent.UI
{
    /// <summary>
    /// Neon sign effect for the game title: slow color cycle between two neon hues
    /// plus random brief flickers that simulate a real gas-tube neon sign.
    /// Replaces the simpler NeonUIAnimator on the title text.
    /// </summary>
    public class NeonTitleAnimator : MonoBehaviour
    {
        [SerializeField] private TMP_Text _text;
        [SerializeField] private Color    _colorA     = new Color(0f, 1f, 0.4f);   // neon green
        [SerializeField] private Color    _colorB     = new Color(0f, 0.9f, 1f);   // neon cyan
        [SerializeField] private float    _cycleSpeed = 0.5f;

        private Coroutine _flickerCoroutine;

        private void Awake()
        {
            if (_text == null)
                _text = GetComponent<TMP_Text>();
        }

        private void OnEnable()
        {
            if (_text != null)
                _text.alpha = 1f;

            _flickerCoroutine = StartCoroutine(FlickerRoutine());
        }

        private void OnDisable()
        {
            if (_flickerCoroutine != null)
            {
                StopCoroutine(_flickerCoroutine);
                _flickerCoroutine = null;
            }

            if (_text != null)
                _text.alpha = 1f;
        }

        private void Update()
        {
            if (_text == null) return;

            float t = (Mathf.Sin(Time.unscaledTime * _cycleSpeed * Mathf.PI * 2f) + 1f) * 0.5f;
            _text.color = Color.Lerp(_colorA, _colorB, t);
        }

        /// <summary>
        /// Waits a random interval then performs 1–3 rapid alpha dips to simulate
        /// a neon tube briefly losing charge. Runs indefinitely while the object is enabled.
        /// </summary>
        private IEnumerator FlickerRoutine()
        {
            while (true)
            {
                // Hold steady for 3–9 seconds between flicker bursts
                yield return new WaitForSecondsRealtime(Random.Range(3f, 9f));

                int bursts = Random.Range(1, 4);
                for (int i = 0; i < bursts; i++)
                {
                    // Dip to near-black
                    if (_text != null) _text.alpha = Random.Range(0.05f, 0.25f);
                    yield return new WaitForSecondsRealtime(Random.Range(0.03f, 0.09f));

                    // Recover
                    if (_text != null) _text.alpha = 1f;
                    yield return new WaitForSecondsRealtime(Random.Range(0.02f, 0.07f));
                }
            }
        }
    }
}
