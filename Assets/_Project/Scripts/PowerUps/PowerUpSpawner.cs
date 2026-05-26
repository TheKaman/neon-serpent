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

        private readonly List<PowerUpItem>              _activeItems    = new List<PowerUpItem>();
        // Maps each live item to the grid cell it occupies at spawn time.
        // Stored explicitly to avoid floating-point mismatch when back-converting
        // world position → grid via WorldToGrid (Bug 6).
        private readonly Dictionary<PowerUpItem, Vector2Int> _itemGridCells = new Dictionary<PowerUpItem, Vector2Int>();
        private Coroutine _spawnCoroutine;

        /// <summary>
        /// Begin the spawn cycle with the given set of allowed power-up types.
        /// If <paramref name="allowedTypes"/> is null or empty the previously configured
        /// <c>_allowedTypes</c> (set in the Inspector) is used, which matches Classic and
        /// Time Attack behaviour. For Campaign levels this is always passed explicitly from
        /// <c>LevelData.AllowedPowerUps</c> so each level's power-up palette is isolated.
        /// </summary>
        /// <remarks>
        /// Bug C fix: previously the allowed-types array was only overwritten when the new
        /// value was non-null and non-empty. This meant that if a later Campaign level had
        /// no allowed power-ups (empty array), the previous level's types would persist and
        /// the wrong power-ups would spawn.  The fix always resets to the Inspector-default
        /// (null) first, then applies the new value — so an empty or null array genuinely
        /// means "no power-ups" rather than "keep the old ones".
        /// </remarks>
        public void StartSpawning(PowerUpType[] allowedTypes = null)
        {
            // Always apply the incoming value (including null/empty) so each session starts
            // with a clean slate. Classic/TimeAttack callers pass null and rely on the
            // Inspector-serialised _allowedTypes field; that field is left unchanged here
            // because we only overwrite the runtime copy used by TrySpawn.
            if (allowedTypes != null)
                _allowedTypes = allowedTypes.Length > 0 ? allowedTypes : null;
            // If allowedTypes is null the caller wants "use Inspector default" — leave _allowedTypes as-is.

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
            _itemGridCells[item] = cell;   // record grid cell at spawn to avoid WorldToGrid float issues

            item.OnCollected += OnItemCollected;
            item.OnExpired   += OnItemExpired;
        }

        /// <summary>
        /// Finds the active power-up item occupying the given grid cell and calls Collect() on it.
        /// Called by GameSession when SnakeController reports it has entered a PowerUp cell.
        /// Returns true if an item was found and collected, false if the cell held no tracked item
        /// (e.g. already expired between the move and this call).
        /// </summary>
        public bool CollectAt(Vector2Int gridPos)
        {
            // Iterate in reverse so RemoveItem's List.Remove does not disrupt the search index.
            for (int i = _activeItems.Count - 1; i >= 0; i--)
            {
                PowerUpItem item = _activeItems[i];

                // Guard against a destroyed item left in the list (should not happen, but defensive).
                if (item == null)
                {
                    _activeItems.RemoveAt(i);
                    // Note: _itemGridCells keyed on the null reference — nothing to remove.
                    continue;
                }

                // Use the stored grid cell recorded at spawn time — avoids float rounding
                // that WorldToGrid back-conversion can introduce (Bug 6 fix).
                if (_itemGridCells.TryGetValue(item, out Vector2Int cell) && cell == gridPos)
                {
                    item.Collect(); // fires OnCollected → OnItemCollected → ApplyEffect + RemoveItem
                    return true;
                }
            }

            Debug.LogWarning($"[PowerUpSpawner] CollectAt({gridPos}): no active item found at that cell.");
            return false;
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

            // Use the recorded grid cell rather than back-converting world position,
            // which can produce a wrong cell due to floating-point precision (Bug 6 fix).
            if (_itemGridCells.TryGetValue(item, out Vector2Int cell))
            {
                _grid.ClearCell(cell);
                _itemGridCells.Remove(item);
            }
            else
            {
                // Fallback: cell was never recorded (should not happen under normal flow).
                Debug.LogWarning("[PowerUpSpawner] RemoveItem: no recorded grid cell for item — grid cell not cleared.");
            }

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
