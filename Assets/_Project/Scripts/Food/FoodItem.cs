using System;
using System.Collections;
using UnityEngine;

namespace NeonSerpent.Food
{
    /// <summary>
    /// Placed on food prefabs. Notifies FoodSpawner when collected or expired.
    /// Does not modify game state directly — raises events consumed by FoodSpawner.
    /// Runs a pulse/rotation animation while active.
    /// </summary>
    public class FoodItem : MonoBehaviour
    {
        [SerializeField] private FoodType _type = FoodType.Normal;
        [SerializeField] private float    _bonusDespawnTime = 8f;

        public FoodType Type => _type;

        public event Action<FoodItem> OnCollected;
        public event Action<FoodItem> OnExpired;

        private Coroutine      _despawnCoroutine;
        private Coroutine      _animCoroutine;
        private SpriteRenderer _spriteRenderer;

        // Pulse parameters per food type
        private const float NORMAL_PULSE_SPEED    = 2.5f;
        private const float NORMAL_SCALE_MIN      = 0.85f;
        private const float NORMAL_SCALE_MAX      = 1.15f;
        private const float BONUS_PULSE_SPEED     = 4f;
        private const float BONUS_SCALE_MIN       = 0.7f;
        private const float BONUS_SCALE_MAX       = 1.3f;
        private const float POISON_ROTATION_SPEED = 45f; // degrees per second

        private void Awake()
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();
        }

        private void OnEnable()
        {
            // Reset transform and renderer to a guaranteed clean state on every enable
            transform.localScale    = Vector3.one;
            transform.localRotation = Quaternion.identity;

            // Always restore full visibility — prevents leftover tint from stale prefab data
            if (_spriteRenderer != null)
                _spriteRenderer.color = Color.white;

            if (_type == FoodType.Bonus)
                _despawnCoroutine = StartCoroutine(DespawnRoutine());

            _animCoroutine = _type == FoodType.Poison
                ? StartCoroutine(RotateRoutine())
                : StartCoroutine(PulseRoutine());
        }

        private void OnDisable()
        {
            if (_despawnCoroutine != null)
            {
                StopCoroutine(_despawnCoroutine);
                _despawnCoroutine = null;
            }
            if (_animCoroutine != null)
            {
                StopCoroutine(_animCoroutine);
                _animCoroutine = null;
            }

            // Reset visual state so the object is clean when returned to pool
            transform.localScale    = Vector3.one;
            transform.localRotation = Quaternion.identity;
            if (_spriteRenderer != null)
            {
                var c = _spriteRenderer.color;
                c.a = 1f;
                _spriteRenderer.color = c;
            }
        }

        /// <summary>Called externally when the snake head overlaps this cell.</summary>
        public void Collect()
        {
            OnCollected?.Invoke(this);
        }

        // ─────────────────────────────────────────────────────────
        // ANIMATIONS
        // ─────────────────────────────────────────────────────────

        /// <summary>
        /// Scale and alpha pulse for Normal and Bonus food types.
        /// Normal: 0.85-1.15 at 2.5 Hz. Bonus: 0.7-1.3 at 4 Hz.
        /// </summary>
        private IEnumerator PulseRoutine()
        {
            float speed    = _type == FoodType.Bonus ? BONUS_PULSE_SPEED    : NORMAL_PULSE_SPEED;
            float scaleMin = _type == FoodType.Bonus ? BONUS_SCALE_MIN      : NORMAL_SCALE_MIN;
            float scaleMax = _type == FoodType.Bonus ? BONUS_SCALE_MAX      : NORMAL_SCALE_MAX;

            while (true)
            {
                // sin oscillates -1..1 → remap to 0..1
                float t = (Mathf.Sin(Time.time * speed * Mathf.PI * 2f) + 1f) * 0.5f;

                float scale = Mathf.Lerp(scaleMin, scaleMax, t);
                transform.localScale = new Vector3(scale, scale, 1f);

                yield return null;
            }
        }

        /// <summary>
        /// Slow rotation for Poison food type (45 degrees per second on Z axis).
        /// </summary>
        private IEnumerator RotateRoutine()
        {
            while (true)
            {
                transform.Rotate(0f, 0f, POISON_ROTATION_SPEED * Time.deltaTime);

                yield return null;
            }
        }

        // ─────────────────────────────────────────────────────────
        // DESPAWN
        // ─────────────────────────────────────────────────────────

        private IEnumerator DespawnRoutine()
        {
            // Countdown with visual urgency: shift colour from white → orange → red
            // over the last 3 seconds so the player knows to sprint for the bonus.
            const float WARN_WINDOW = 3f;
            float elapsed = 0f;

            while (elapsed < _bonusDespawnTime)
            {
                elapsed += Time.deltaTime;

                float remaining = _bonusDespawnTime - elapsed;
                if (remaining < WARN_WINDOW && _spriteRenderer != null)
                {
                    float t = 1f - (remaining / WARN_WINDOW); // 0 = white, 1 = red
                    _spriteRenderer.color = Color.Lerp(Color.white, new Color(1f, 0.15f, 0.15f), t);
                }

                yield return null;
            }

            OnExpired?.Invoke(this);
        }
    }
}
