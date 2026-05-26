using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NeonSerpent.Grid;
using NeonSerpent.Utilities;

namespace NeonSerpent.Snake
{
    /// <summary>
    /// Owns all snake game logic: movement ticks, direction buffering, growth/shrink,
    /// and collision detection. Raises events consumed by visuals and game manager.
    /// Does NOT contain any rendering or UI logic.
    /// </summary>
    public class SnakeController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GridSystem _grid;

        [Header("Settings")]
        [SerializeField] private int _startLength = 3;

        // --- Body ---
        private readonly Queue<Vector2Int> _body = new Queue<Vector2Int>();
        private Vector2Int _headPosition;

        // --- Direction ---
        private Vector2Int _currentDirection = Vector2Int.right;
        private Vector2Int _bufferedDirection = Vector2Int.right;

        // --- Speed ---
        private float _currentSpeed = Constants.DEFAULT_SPEED;
        private float _speedMultiplier = 1f;
        private Coroutine _movementCoroutine;

        // --- State flags ---
        public bool IsShielded { get; set; }
        public bool IsGhost    { get; set; }

        // --- Events ---
        public event Action<Vector2Int>  OnMoved;           // new head position
        public event Action<Vector2Int>  OnAteFood;         // position of eaten food
        public event Action<Vector2Int>  OnAtePowerUp;      // position of picked-up power-up
        public event Action              OnHitWall;
        public event Action              OnHitSelf;
        /// <summary>Fired when the shield absorbs a fatal wall or self-collision hit.</summary>
        public event Action              OnShieldAbsorbed;
        /// <summary>Fired once when base speed first reaches MAX_SPEED (Frenzy Mode trigger).</summary>
        public event Action              OnMaxSpeedReached;

        // --- Public read-only state ---
        public IEnumerable<Vector2Int> Body             => _body;
        public Vector2Int              HeadPosition     => _headPosition;
        public Vector2Int              CurrentDirection => _currentDirection;
        public int                     Length           => _body.Count;
        public float                   CurrentSpeed     => _currentSpeed;
        /// <summary>
        /// The configured initial body length. Used by GameSession to calculate how much
        /// horizontal runway is needed when validating the snake's spawn position.
        /// </summary>
        public int                     StartLength      => _startLength;

        // --- Lifecycle ---

        private void OnDisable()
        {
            StopMovement();
        }

        // --- Public API ---

        /// <summary>Spawn the snake at the given position facing right.</summary>
        public void Initialize(Vector2Int startPos)
        {
            StopMovement();

            // Clear previous body positions from the grid before reinitialising
            foreach (var pos in _body)
                _grid.ClearCell(pos);

            _body.Clear();
            _headPosition      = startPos;
            _currentDirection  = Vector2Int.right;
            _bufferedDirection = Vector2Int.right;
            _currentSpeed      = Constants.DEFAULT_SPEED;
            _speedMultiplier   = 1f;
            _pendingGrowth     = 0;
            IsShielded         = false;
            IsGhost            = false;

            for (int i = _startLength - 1; i >= 0; i--)
            {
                var pos = new Vector2Int(startPos.x - i, startPos.y);
                _body.Enqueue(pos);
                _grid.SetCell(pos, GridCellType.Snake);
            }
            _headPosition = startPos;
            OnMoved?.Invoke(_headPosition); // refresh visuals immediately at start position
            StartMovement();
        }

        /// <summary>
        /// Buffer a direction change. The new direction is applied on the next movement tick.
        /// Ignores 180-degree reversals and duplicate inputs.
        /// The reversal check compares against <c>_bufferedDirection</c> (not <c>_currentDirection</c>)
        /// so that two rapid swipes in opposite directions within the same tick cannot cause
        /// an instant self-collision.
        /// </summary>
        public void SetDirection(Vector2Int dir)
        {
            // Ignore 180-degree reversal against the most recently buffered direction.
            // Using _bufferedDirection (not _currentDirection) prevents the double-swipe
            // death bug where Right → Left arrives before the move tick consumes Right.
            if (dir == -_bufferedDirection) return;
            _bufferedDirection = dir;
        }

        /// <summary>Grow the snake by the given number of segments on the next tick.</summary>
        public void Grow(int segments = 1)
        {
            // Growth is implicit: we skip removing the tail for `segments` ticks.
            // Implemented by a pending growth counter.
            _pendingGrowth += segments;
        }

        /// <summary>Remove tail segments (used by Shrink power-up).</summary>
        public void Shrink(int segments)
        {
            int toRemove = Mathf.Min(segments, _body.Count - 2); // keep at least head + 1
            for (int i = 0; i < toRemove; i++)
            {
                var tail = _body.Dequeue();
                _grid.ClearCell(tail);
            }
        }

        /// <summary>
        /// Immediately stops all snake movement. Call on game over / timer expiry so the
        /// snake does not keep moving after the session has ended.
        /// </summary>
        public void StopSnake() => StopMovement();

        /// <summary>
        /// Set the base speed directly. Used by GameSession after loading a LevelData
        /// to apply per-level InitialSpeed instead of always defaulting to Constants.DEFAULT_SPEED.
        /// </summary>
        public void SetSpeed(float speed)
        {
            _currentSpeed = Mathf.Clamp(speed, 1f, Constants.MAX_SPEED);
            RestartMovement();
        }

        /// <summary>Add a flat increment to the base speed (called each time food is eaten).</summary>
        public void IncrementSpeed(float increment)
        {
            bool wasAtMax = _currentSpeed >= Constants.MAX_SPEED;
            _currentSpeed = Mathf.Min(_currentSpeed + increment, Constants.MAX_SPEED);
            if (!wasAtMax && _currentSpeed >= Constants.MAX_SPEED)
                OnMaxSpeedReached?.Invoke();
            RestartMovement();
        }

        /// <summary>Apply a speed multiplier (stacks multiplicatively, call Remove to revert).</summary>
        public void ApplySpeedMultiplier(float multiplier)
        {
            _speedMultiplier *= multiplier;
            RestartMovement();
        }

        /// <summary>Remove a previously applied speed multiplier.</summary>
        public void RemoveSpeedMultiplier(float multiplier)
        {
            _speedMultiplier /= multiplier;
            RestartMovement();
        }

        // --- Internal ---

        private int _pendingGrowth;

        private void StartMovement()
        {
            StopMovement();
            _movementCoroutine = StartCoroutine(MovementTick());
        }

        private void StopMovement()
        {
            if (_movementCoroutine != null)
            {
                StopCoroutine(_movementCoroutine);
                _movementCoroutine = null;
            }
        }

        private void RestartMovement()
        {
            if (gameObject.activeInHierarchy)
                StartMovement();
        }

        private IEnumerator MovementTick()
        {
            while (true)
            {
                float interval = 1f / Mathf.Clamp(_currentSpeed * _speedMultiplier, 1f, Constants.MAX_SPEED);
                yield return new WaitForSeconds(interval);
                MoveOneStep();
            }
        }

        private void MoveOneStep()
        {
            _currentDirection = _bufferedDirection;
            Vector2Int nextPos = _headPosition + _currentDirection;

            // Wall collision
            if (!_grid.IsInBounds(nextPos) || _grid.GetCellType(nextPos) == GridCellType.Wall)
            {
                if (IsShielded)
                {
                    IsShielded = false;
                    OnShieldAbsorbed?.Invoke();
                    return; // absorb hit
                }
                StopMovement();
                OnHitWall?.Invoke();
                return;
            }

            GridCellType cellType = _grid.GetCellType(nextPos);

            // Self collision
            if (cellType == GridCellType.Snake && !IsGhost)
            {
                if (IsShielded)
                {
                    IsShielded = false;
                    OnShieldAbsorbed?.Invoke();
                    return;
                }
                StopMovement();
                OnHitSelf?.Invoke();
                return;
            }

            // Food
            if (cellType == GridCellType.Food)
            {
                _pendingGrowth++;
                OnAteFood?.Invoke(nextPos);
            }

            // Power-up
            if (cellType == GridCellType.PowerUp)
                OnAtePowerUp?.Invoke(nextPos);

            // Advance head
            _headPosition = nextPos;
            _body.Enqueue(nextPos);
            _grid.SetCell(nextPos, GridCellType.Snake);

            // Remove tail unless growing
            if (_pendingGrowth > 0)
                _pendingGrowth--;
            else
            {
                var tail = _body.Dequeue();
                _grid.ClearCell(tail);
            }

            OnMoved?.Invoke(_headPosition);
        }
    }
}
