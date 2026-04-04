using System;
using UnityEngine;
using NeonSerpent.Grid;
using NeonSerpent.PowerUps;
using NeonSerpent.Scoring;
using NeonSerpent.Utilities;

namespace NeonSerpent.Food
{
    /// <summary>
    /// Manages spawning, tracking, and despawning of all food items on the grid.
    /// Listens to SnakeController.OnAteFood to know when to spawn the next food item.
    /// </summary>
    public class FoodSpawner : MonoBehaviour
    {
        /// <summary>Fired after food is consumed. Carries the FoodType that was eaten.</summary>
        public event Action<FoodType> OnFoodEaten;
        [Header("References")]
        [SerializeField] private GridSystem    _grid;
        [SerializeField] private ScoreManager  _scoreManager;
        [SerializeField] private PowerUpManager _powerUpManager;

        [Header("Prefabs")]
        [SerializeField] private FoodItem _normalFoodPrefab;
        [SerializeField] private FoodItem _bonusFoodPrefab;
        [SerializeField] private FoodItem _poisonFoodPrefab;

        [Header("Spawn Settings")]
        [SerializeField, Range(0f, 1f)] private float _bonusSpawnChance  = 0.15f;
        [SerializeField, Range(0f, 1f)] private float _poisonSpawnChance = 0.10f;

        private FoodItem   _currentNormalFood;
        private Vector2Int _currentNormalFoodCell;
        private FoodItem   _currentBonusFood;
        private Vector2Int _currentBonusFoodCell;

        private int _eatsSinceLastBonus;
        // Minimum normal-food eats required between bonus/poison spawns.
        // Prevents back-to-back poison or bonus items that feel unfair.
        private const int MIN_EATS_BEFORE_BONUS = 3;

        /// <summary>Begin spawning. Call after the grid is initialized.</summary>
        public void StartSpawning()
        {
            _eatsSinceLastBonus = 0;
            SpawnNormalFood();
        }

        /// <summary>Remove all food and stop spawning (call on game over / level reset).</summary>
        public void StopSpawning()
        {
            DespawnAll();
        }

        /// <summary>Called by GameManager/SnakeController bridge when food is eaten.</summary>
        public void HandleFoodEaten(Vector2Int pos)
        {
            FoodType eaten;

            if (_currentNormalFood != null && _currentNormalFoodCell == pos)
            {
                eaten = FoodType.Normal;
                Destroy(_currentNormalFood.gameObject);
                _grid.ClearCell(_currentNormalFoodCell);
                _currentNormalFood = null;
            }
            else if (_currentBonusFood != null && _currentBonusFoodCell == pos)
            {
                eaten = _currentBonusFood.Type;
                Destroy(_currentBonusFood.gameObject);
                _grid.ClearCell(_currentBonusFoodCell);
                _currentBonusFood = null;
            }
            else
            {
                // The eaten position does not match any tracked food cell.
                // This can happen if food expired and was removed between the move
                // tick and this call. Do nothing — no score, no respawn, no event.
                Debug.LogWarning($"[FoodSpawner] HandleFoodEaten({pos}): position matches no tracked food cell. Ignoring.");
                return;
            }

            // Notify listeners (e.g. GameSession) of what food type was just eaten
            OnFoodEaten?.Invoke(eaten);

            // Track consecutive normal-food eats for the bonus/poison spawn cooldown.
            // Any non-normal eat resets the counter so the cooldown restarts fresh.
            if (eaten == FoodType.Normal)
                _eatsSinceLastBonus++;
            else
                _eatsSinceLastBonus = 0;

            // Poison food triggers the poison effect on the snake (half speed, zero next score)
            if (eaten == FoodType.Poison)
                _powerUpManager?.ApplyEffect(PowerUpType.Poison);

            int points = eaten == FoodType.Bonus  ? Constants.SCORE_BONUS_FOOD
                       : eaten == FoodType.Poison ? 0
                       : Constants.SCORE_NORMAL_FOOD;

            if (_scoreManager == null)
            {
                Debug.LogWarning("[FoodSpawner] _scoreManager is null — score not awarded.");
            }
            else
            {
                _scoreManager.AddScore(points);
            }

            // Spawn replacement
            SpawnNormalFood();
            TrySpawnBonusOrPoison();
        }

        private void SpawnNormalFood()
        {
            if (_currentNormalFood != null) return;
            Vector2Int cell = _grid.GetRandomEmptyCell();
            if (cell.x < 0) return;
            _currentNormalFood     = Spawn(_normalFoodPrefab, cell);
            _currentNormalFoodCell = cell;
        }

        private void TrySpawnBonusOrPoison()
        {
            // Enforce a cooldown: require MIN_EATS_BEFORE_BONUS consecutive normal-food
            // eats before another bonus or poison item is allowed to spawn.
            if (_currentBonusFood != null || _eatsSinceLastBonus < MIN_EATS_BEFORE_BONUS) return;

            float roll = UnityEngine.Random.value;
            if (roll < _poisonSpawnChance)
            {
                Vector2Int cell = _grid.GetRandomEmptyCell();
                if (cell.x >= 0)
                {
                    _currentBonusFood     = Spawn(_poisonFoodPrefab, cell);
                    _currentBonusFoodCell = cell;
                    _currentBonusFood.OnExpired += OnBonusFoodExpired;
                    _eatsSinceLastBonus = 0; // restart cooldown from this spawn
                }
            }
            else if (roll < _poisonSpawnChance + _bonusSpawnChance)
            {
                Vector2Int cell = _grid.GetRandomEmptyCell();
                if (cell.x >= 0)
                {
                    _currentBonusFood     = Spawn(_bonusFoodPrefab, cell);
                    _currentBonusFoodCell = cell;
                    _currentBonusFood.OnExpired += OnBonusFoodExpired;
                    _eatsSinceLastBonus = 0; // restart cooldown from this spawn
                }
            }
        }

        private FoodItem Spawn(FoodItem prefab, Vector2Int cell)
        {
            var item = Instantiate(prefab, _grid.GridToWorld(cell), Quaternion.identity, transform);
            _grid.SetCell(cell, GridCellType.Food);
            return item;
        }

        private void OnBonusFoodExpired(FoodItem item)
        {
            item.OnExpired -= OnBonusFoodExpired;
            Despawn(_currentBonusFoodCell);
            if (item != null) Destroy(item.gameObject);
            _currentBonusFood = null;
        }

        private void Despawn(Vector2Int cell)
        {
            _grid.ClearCell(cell);
        }

        private void DespawnAll()
        {
            if (_currentNormalFood != null)
            {
                Destroy(_currentNormalFood.gameObject);
                _grid.ClearCell(_currentNormalFoodCell);
                _currentNormalFood = null;
            }
            if (_currentBonusFood != null)
            {
                // Unsubscribe before destroying so the OnExpired coroutine cannot fire
                // on the destroyed object in the same frame and cause MissingReferenceException.
                _currentBonusFood.OnExpired -= OnBonusFoodExpired;
                Destroy(_currentBonusFood.gameObject);
                _grid.ClearCell(_currentBonusFoodCell);
                _currentBonusFood = null;
            }
        }
    }
}
