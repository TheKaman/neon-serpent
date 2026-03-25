using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NeonSerpent.Grid;
using NeonSerpent.Utilities;

namespace NeonSerpent.PowerUps
{
    /// <summary>
    /// Periodically spawns power-up items on the grid.
    /// Enforces the MAX_ACTIVE_POWERUPS cap. Notifies PowerUpManager when collected.
    /// </summary>
    public class PowerUpSpawner : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GridSystem      _grid;
        [SerializeField] private PowerUpManager  _powerUpManager;

        [Header("Prefabs — assign one per PowerUpType in matching order")]
        [SerializeField] private PowerUpItem[] _prefabs; // index matches PowerUpType enum

        [Header("Spawn Settings")]
        [SerializeField] private float _spawnIntervalMin = 8f;
        [SerializeField] private float _spawnIntervalMax = 15f;
        [SerializeField] private PowerUpType[] _allowedTypes;

        private readonly List<PowerUpItem> _activeItems = new List<PowerUpItem>();
        private Coroutine _spawnCoroutine;

        /// <summary>Begin the spawn cycle. Pass allowed types from LevelData.</summary>
        public void StartSpawning(PowerUpType[] allowedTypes = null)
        {
            if (allowedTypes != null && allowedTypes.Length > 0)
                _allowedTypes = allowedTypes;

            StopSpawning();
            _spawnCoroutine = StartCoroutine(SpawnCycle());
        }

        /// <summary>Stop spawning and remove all active power-up items.</summary>
        public void StopSpawning()
        {
            if (_spawnCoroutine != null)
            {
                StopCoroutine(_spawnCoroutine);
                _spawnCoroutine = null;
            }
            DespawnAll();
        }

        private IEnumerator SpawnCycle()
        {
            // Initial delay before first spawn
            yield return new WaitForSeconds(Random.Range(_spawnIntervalMin, _spawnIntervalMax));

            while (true)
            {
                if (_activeItems.Count < Constants.MAX_ACTIVE_POWERUPS && _allowedTypes?.Length > 0)
                    TrySpawn();

                yield return new WaitForSeconds(Random.Range(_spawnIntervalMin, _spawnIntervalMax));
            }
        }

        private void TrySpawn()
        {
            Vector2Int cell = _grid.GetRandomEmptyCell();
            if (cell.x < 0) return;

            PowerUpType type = _allowedTypes[Random.Range(0, _allowedTypes.Length)];
            int prefabIndex  = (int)type;
            if (prefabIndex >= _prefabs.Length || _prefabs[prefabIndex] == null)
            {
                Debug.LogWarning($"[PowerUpSpawner] No prefab for {type}");
                return;
            }

            var item = Instantiate(_prefabs[prefabIndex], _grid.GridToWorld(cell), Quaternion.identity, transform);
            _grid.SetCell(cell, GridCellType.PowerUp);
            _activeItems.Add(item);

            item.OnCollected += OnItemCollected;
            item.OnExpired   += OnItemExpired;
        }

        private void OnItemCollected(PowerUpItem item)
        {
            _powerUpManager.ApplyEffect(item.Type);
            RemoveItem(item);
        }

        private void OnItemExpired(PowerUpItem item) => RemoveItem(item);

        private void RemoveItem(PowerUpItem item)
        {
            item.OnCollected -= OnItemCollected;
            item.OnExpired   -= OnItemExpired;
            _grid.ClearCell(_grid.WorldToGrid(item.transform.position));
            _activeItems.Remove(item);
            Destroy(item.gameObject);
        }

        private void DespawnAll()
        {
            foreach (var item in new List<PowerUpItem>(_activeItems))
                RemoveItem(item);
        }
    }
}
