using UnityEngine;
using NeonSerpent.Grid;
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
        [Header("References")]
        [SerializeField] private GridSystem   _grid;
        [SerializeField] private ScoreManager _scoreManager;

        [Header("Prefabs")]
        [SerializeField] private FoodItem _normalFoodPrefab;
        [SerializeField] private FoodItem _bonusFoodPrefab;
        [SerializeField] private FoodItem _poisonFoodPrefab;

        [Header("Spawn Settings")]
        [SerializeField, Range(0f, 1f)] private float _bonusSpawnChance  = 0.15f;
        [SerializeField, Range(0f, 1f)] private float _poisonSpawnChance = 0.10f;

        private FoodItem _currentNormalFood;
        private FoodItem _currentBonusFood;

        private int _eatsSinceLastBonus;

        /// <summary>Begin spawning. Call after the grid is initialized.</summary>
        public void StartSpawning()
        {
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
            FoodType eaten = FoodType.Normal;
            if (_currentNormalFood != null && _grid.WorldToGrid(_currentNormalFood.transform.position) == pos)
            {
                eaten = FoodType.Normal;
                Despawn(_currentNormalFood);
                _currentNormalFood = null;
            }
            else if (_currentBonusFood != null && _grid.WorldToGrid(_currentBonusFood.transform.position) == pos)
            {
                eaten = _currentBonusFood.Type;
                Despawn(_currentBonusFood);
                _currentBonusFood = null;
            }

            int points = eaten == FoodType.Bonus ? Constants.SCORE_BONUS_FOOD : Constants.SCORE_NORMAL_FOOD;
            _scoreManager.AddScore(points);

            // Spawn replacement
            SpawnNormalFood();
            TrySpawnBonusOrPoison();
        }

        private void SpawnNormalFood()
        {
            if (_currentNormalFood != null) return;
            Vector2Int cell = _grid.GetRandomEmptyCell();
            if (cell.x < 0) return;
            _currentNormalFood = Spawn(_normalFoodPrefab, cell);
        }

        private void TrySpawnBonusOrPoison()
        {
            if (_currentBonusFood != null) return;

            float roll = Random.value;
            if (roll < _poisonSpawnChance)
            {
                Vector2Int cell = _grid.GetRandomEmptyCell();
                if (cell.x >= 0)
                {
                    _currentBonusFood = Spawn(_poisonFoodPrefab, cell);
                    _currentBonusFood.OnExpired += f => { Despawn(f); _currentBonusFood = null; };
                }
            }
            else if (roll < _poisonSpawnChance + _bonusSpawnChance)
            {
                Vector2Int cell = _grid.GetRandomEmptyCell();
                if (cell.x >= 0)
                {
                    _currentBonusFood = Spawn(_bonusFoodPrefab, cell);
                    _currentBonusFood.OnExpired += f => { Despawn(f); _currentBonusFood = null; };
                }
            }
        }

        private FoodItem Spawn(FoodItem prefab, Vector2Int cell)
        {
            var item = Instantiate(prefab, _grid.GridToWorld(cell), Quaternion.identity, transform);
            _grid.SetCell(cell, GridCellType.Food);
            return item;
        }

        private void Despawn(FoodItem item)
        {
            if (item == null) return;
            _grid.ClearCell(_grid.WorldToGrid(item.transform.position));
            Destroy(item.gameObject);
        }

        private void DespawnAll()
        {
            Despawn(_currentNormalFood);
            Despawn(_currentBonusFood);
            _currentNormalFood = null;
            _currentBonusFood  = null;
        }
    }
}
