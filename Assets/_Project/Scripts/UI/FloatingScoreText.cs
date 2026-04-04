using System.Collections;
using UnityEngine;
using TMPro;

namespace NeonSerpent.UI
{
    /// <summary>
    /// A single floating score popup. Spawned by FloatingTextSpawner, animates upward,
    /// fades out, then destroys itself. Designed for pooling if performance demands it.
    /// </summary>
    [RequireComponent(typeof(TMP_Text))]
    public class FloatingScoreText : MonoBehaviour
    {
        [SerializeField] private float _lifetime  = 0.9f;
        [SerializeField] private float _riseSpeed = 1.5f;

        private TMP_Text  _text;
        private Coroutine _animCoroutine;

        private void Awake()
        {
            _text = GetComponent<TMP_Text>();
        }

        /// <summary>Set the displayed value and start the float-up animation.</summary>
        public void Populate(long points, bool isCombo = false)
        {
            _text.text  = isCombo ? $"COMBO +{points:N0}!" : $"+{points:N0}";
            _text.color = isCombo
                ? new Color(1f, 0.8f, 0f)   // gold for combo
                : new Color(0f, 1f, 0.8f);  // cyan for normal

            if (_animCoroutine != null) StopCoroutine(_animCoroutine);
            _animCoroutine = StartCoroutine(Animate());
        }

        private IEnumerator Animate()
        {
            float elapsed  = 0f;
            Color startCol = _text.color;
            Vector3 pos    = transform.position;

            while (elapsed < _lifetime)
            {
                elapsed += Time.deltaTime;
                float t  = elapsed / _lifetime;

                transform.position = pos + Vector3.up * (_riseSpeed * elapsed);
                startCol.a         = Mathf.Lerp(1f, 0f, t);
                _text.color        = startCol;

                yield return null;
            }

            _animCoroutine = null;
            Destroy(gameObject);
        }
    }
}
