using System;
using UnityEngine;

namespace NeonSerpent.PowerUps
{
    /// <summary>
    /// Component on power-up prefabs. Notifies PowerUpSpawner when collected or expired.
    /// Handles bob/pulse animation in Update.
    /// </summary>
    public class PowerUpItem : MonoBehaviour
    {
        [SerializeField] private PowerUpType _type;
        [SerializeField] private float _bobAmplitude = 0.06f;
        [SerializeField] private float _bobFrequency = 2f;

        public PowerUpType Type => _type;

        public event Action<PowerUpItem> OnCollected;
        public event Action<PowerUpItem> OnExpired;

        private Vector3 _originPos;
        private Coroutine _despawnCoroutine;

        private void OnEnable()
        {
            _originPos = transform.position;
            _despawnCoroutine = StartCoroutine(DespawnRoutine());
        }

        private void OnDisable()
        {
            if (_despawnCoroutine != null)
            {
                StopCoroutine(_despawnCoroutine);
                _despawnCoroutine = null;
            }
        }

        private void Update()
        {
            // Bob animation
            float y = _originPos.y + Mathf.Sin(Time.time * _bobFrequency) * _bobAmplitude;
            transform.position = new Vector3(_originPos.x, y, _originPos.z);
        }

        /// <summary>Called by PowerUpSpawner when the snake head enters this cell.</summary>
        public void Collect()
        {
            OnCollected?.Invoke(this);
        }

        private System.Collections.IEnumerator DespawnRoutine()
        {
            yield return new UnityEngine.WaitForSeconds(NeonSerpent.Utilities.Constants.POWERUP_DESPAWN_TIME);
            OnExpired?.Invoke(this);
        }
    }
}
