using System;
using System.Collections;
using UnityEngine;

namespace NeonSerpent.PowerUps
{
    /// <summary>
    /// Component on power-up prefabs. Notifies PowerUpSpawner when collected or expired.
    /// Handles spawn bounce-in animation, continuous rotation, and hover bob in Update.
    /// </summary>
    public class PowerUpItem : MonoBehaviour
    {
        [SerializeField] private PowerUpType _type;

        // These are superseded by the new hover logic below but kept for
        // backwards compatibility with any Inspector overrides.
        [SerializeField] private float _bobAmplitude = 0.1f;
        [SerializeField] private float _bobFrequency = 2f;

        public PowerUpType Type => _type;

        public event Action<PowerUpItem> OnCollected;
        public event Action<PowerUpItem> OnExpired;

        private Vector3   _originPos;
        private bool      _spawnComplete;
        private Coroutine _despawnCoroutine;
        private Coroutine _spawnCoroutine;

        private const float ROTATION_SPEED = 90f; // degrees per second on Z axis

        private void OnEnable()
        {
            _originPos    = transform.position;
            _spawnComplete = false;

            transform.localScale = Vector3.zero;

            _despawnCoroutine = StartCoroutine(DespawnRoutine());
            _spawnCoroutine   = StartCoroutine(SpawnBounceRoutine());
        }

        private void OnDisable()
        {
            if (_despawnCoroutine != null)
            {
                StopCoroutine(_despawnCoroutine);
                _despawnCoroutine = null;
            }
            if (_spawnCoroutine != null)
            {
                StopCoroutine(_spawnCoroutine);
                _spawnCoroutine = null;
            }

            // Reset visual state for clean re-use
            transform.localScale    = Vector3.one;
            transform.localRotation = Quaternion.identity;
        }

        private void Update()
        {
            if (!_spawnComplete) return;

            // Continuous Z rotation
            transform.Rotate(0f, 0f, ROTATION_SPEED * Time.deltaTime, Space.Self);

            // Hover bob — offset Y from spawn origin
            float y = _originPos.y + Mathf.Sin(Time.time * _bobFrequency) * _bobAmplitude;
            var pos = transform.position;
            transform.position = new Vector3(pos.x, y, pos.z);
        }

        /// <summary>Called by PowerUpSpawner when the snake head enters this cell.</summary>
        public void Collect()
        {
            OnCollected?.Invoke(this);
        }

        // ─────────────────────────────────────────────────────────
        // ANIMATIONS
        // ─────────────────────────────────────────────────────────

        /// <summary>
        /// Bounce-in spawn: scale from 0 → 1.2 → 1.0 over 0.3 seconds.
        /// </summary>
        private IEnumerator SpawnBounceRoutine()
        {
            const float totalTime  = 0.3f;
            const float overshootT = 0.7f; // fraction of time spent reaching overshoot peak

            float elapsed = 0f;

            // Phase 1: 0 → 1.2 over the first 70% of the duration
            float phase1Duration = totalTime * overshootT;
            while (elapsed < phase1Duration)
            {
                elapsed += Time.deltaTime;
                float t  = Mathf.Clamp01(elapsed / phase1Duration);
                float s  = Mathf.Lerp(0f, 1.2f, t);
                transform.localScale = new Vector3(s, s, 1f);
                yield return null;
            }

            // Phase 2: 1.2 → 1.0 over the remaining 30%
            elapsed = 0f;
            float phase2Duration = totalTime * (1f - overshootT);
            while (elapsed < phase2Duration)
            {
                elapsed += Time.deltaTime;
                float t  = Mathf.Clamp01(elapsed / phase2Duration);
                float s  = Mathf.Lerp(1.2f, 1.0f, t);
                transform.localScale = new Vector3(s, s, 1f);
                yield return null;
            }

            transform.localScale = Vector3.one;
            _spawnComplete        = true;
            _spawnCoroutine       = null;
        }

        // ─────────────────────────────────────────────────────────
        // DESPAWN
        // ─────────────────────────────────────────────────────────

        private IEnumerator DespawnRoutine()
        {
            yield return new WaitForSeconds(NeonSerpent.Utilities.Constants.POWERUP_DESPAWN_TIME);
            OnExpired?.Invoke(this);
        }
    }
}
