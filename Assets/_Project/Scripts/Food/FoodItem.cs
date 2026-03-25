using System;
using UnityEngine;

namespace NeonSerpent.Food
{
    /// <summary>
    /// Placed on food prefabs. Notifies FoodSpawner when collected or expired.
    /// Does not modify game state directly — raises events consumed by FoodSpawner.
    /// </summary>
    public class FoodItem : MonoBehaviour
    {
        [SerializeField] private FoodType _type = FoodType.Normal;
        [SerializeField] private float    _bonusDespawnTime = 8f;

        public FoodType Type => _type;

        public event Action<FoodItem> OnCollected;
        public event Action<FoodItem> OnExpired;

        private Coroutine _despawnCoroutine;

        private void OnEnable()
        {
            if (_type == FoodType.Bonus)
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

        /// <summary>Called externally when the snake head overlaps this cell.</summary>
        public void Collect()
        {
            OnCollected?.Invoke(this);
        }

        private System.Collections.IEnumerator DespawnRoutine()
        {
            yield return new WaitForSeconds(_bonusDespawnTime);
            OnExpired?.Invoke(this);
        }
    }
}
