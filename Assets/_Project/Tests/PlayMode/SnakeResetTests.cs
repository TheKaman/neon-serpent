using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using NeonSerpent.Snake;
using NeonSerpent.Grid;

namespace NeonSerpent.Tests
{
    /// <summary>
    /// PlayMode tests for SnakeController state reset between campaign levels.
    /// Verifies that Initialize() correctly clears previous body positions from the grid,
    /// resets speed, direction, growth counter, and shield/ghost flags.
    ///
    /// These run in Play mode because SnakeController.StartMovement uses StartCoroutine.
    /// </summary>
    [TestFixture]
    [Category("Campaign")]
    [Category("Snake")]
    public class SnakeResetTests
    {
        private GameObject    _snakeGO;
        private SnakeController _snake;
        private GridSystem    _grid;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            var gridGO = new GameObject("Grid");
            _grid      = gridGO.AddComponent<GridSystem>();
            _grid.InitializeGrid(20, 20);

            _snakeGO = new GameObject("Snake");
            _snake   = _snakeGO.AddComponent<SnakeController>();
            SetPrivateField(_snake, "_grid",        _grid);
            SetPrivateField(_snake, "_startLength", 3);

            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            _snake.StopSnake();
            Object.DestroyImmediate(_snakeGO);
            Object.DestroyImmediate(_grid.gameObject);
            yield return null;
        }

        // ── Initialize: body placement ────────────────────────────────────────

        [UnityTest]
        public IEnumerator Initialize_PlacesBodyCellsOnGrid()
        {
            var startPos = new Vector2Int(5, 10);
            _snake.Initialize(startPos);
            yield return null;

            // Head at (5,10), body at (4,10) and (3,10) (facing right, extends left)
            Assert.AreEqual(GridCellType.Snake, _grid.GetCellType(new Vector2Int(5, 10)),
                "Head position must be Snake type after Initialize.");
            Assert.AreEqual(GridCellType.Snake, _grid.GetCellType(new Vector2Int(4, 10)),
                "Body segment must be Snake type after Initialize.");
            Assert.AreEqual(GridCellType.Snake, _grid.GetCellType(new Vector2Int(3, 10)),
                "Tail segment must be Snake type after Initialize.");
        }

        [UnityTest]
        public IEnumerator Initialize_SetsHeadPositionCorrectly()
        {
            var startPos = new Vector2Int(5, 10);
            _snake.Initialize(startPos);
            yield return null;

            Assert.AreEqual(startPos, _snake.HeadPosition);
        }

        [UnityTest]
        public IEnumerator Initialize_SetsLengthToStartLength()
        {
            _snake.Initialize(new Vector2Int(5, 10));
            yield return null;

            Assert.AreEqual(3, _snake.Length, "Length must equal _startLength after Initialize.");
        }

        // ── Initialize: grid cleanup from previous session ────────────────────

        [UnityTest]
        public IEnumerator Initialize_ClearsOldBodyFromGrid()
        {
            // First session: snake at position A
            var posA = new Vector2Int(5, 10);
            _snake.Initialize(posA);
            yield return null;

            // Simulate the snake moving one step right — body now covers (4,10),(5,10),(6,10)
            // We can verify the state by re-initialising at a different position and checking
            // that OLD cells are cleared.
            var posB = new Vector2Int(15, 15); // different area
            _snake.Initialize(posB);
            yield return null;

            // Old cells from the first session must be cleared
            Assert.AreEqual(GridCellType.Empty, _grid.GetCellType(new Vector2Int(5, 10)),
                "Grid cell from previous session's head position must be cleared on re-init.");
            Assert.AreEqual(GridCellType.Empty, _grid.GetCellType(new Vector2Int(4, 10)),
                "Grid cell from previous session's body must be cleared on re-init.");
            Assert.AreEqual(GridCellType.Empty, _grid.GetCellType(new Vector2Int(3, 10)),
                "Grid cell from previous session's tail must be cleared on re-init.");
        }

        [UnityTest]
        public IEnumerator Initialize_NewBodyRegisteredOnGrid()
        {
            var posB = new Vector2Int(15, 15);
            _snake.Initialize(posB);
            yield return null;

            Assert.AreEqual(GridCellType.Snake, _grid.GetCellType(new Vector2Int(15, 15)));
            Assert.AreEqual(GridCellType.Snake, _grid.GetCellType(new Vector2Int(14, 15)));
            Assert.AreEqual(GridCellType.Snake, _grid.GetCellType(new Vector2Int(13, 15)));
        }

        // ── Initialize: state reset ───────────────────────────────────────────

        [UnityTest]
        public IEnumerator Initialize_ResetsDirectionToRight()
        {
            _snake.Initialize(new Vector2Int(5, 10));
            // Buffer an up direction
            _snake.SetDirection(Vector2Int.up);
            yield return null;

            // Re-initialize: direction must reset
            _snake.Initialize(new Vector2Int(5, 10));
            yield return null;

            Assert.AreEqual(Vector2Int.right, _snake.CurrentDirection,
                "CurrentDirection must be Vector2Int.right after Initialize.");
        }

        [UnityTest]
        public IEnumerator Initialize_ResetsShieldFlag()
        {
            _snake.Initialize(new Vector2Int(5, 10));
            _snake.IsShielded = true;
            yield return null;

            _snake.Initialize(new Vector2Int(5, 10));
            yield return null;

            Assert.IsFalse(_snake.IsShielded, "IsShielded must be false after Initialize.");
        }

        [UnityTest]
        public IEnumerator Initialize_ResetsGhostFlag()
        {
            _snake.Initialize(new Vector2Int(5, 10));
            _snake.IsGhost = true;
            yield return null;

            _snake.Initialize(new Vector2Int(5, 10));
            yield return null;

            Assert.IsFalse(_snake.IsGhost, "IsGhost must be false after Initialize.");
        }

        [UnityTest]
        public IEnumerator Initialize_ResetsSpeedToDefaultSpeed()
        {
            _snake.Initialize(new Vector2Int(5, 10));
            // Simulate speed having increased
            _snake.IncrementSpeed(5f);
            yield return null;

            _snake.Initialize(new Vector2Int(5, 10));
            yield return null;

            Assert.AreEqual(Utilities.Constants.DEFAULT_SPEED, _snake.CurrentSpeed,
                "CurrentSpeed must reset to DEFAULT_SPEED on Initialize.");
        }

        // ── Grow / Shrink ─────────────────────────────────────────────────────

        [UnityTest]
        public IEnumerator Shrink_KeepsAtLeastTwoSegments()
        {
            _snake.Initialize(new Vector2Int(5, 10));
            yield return null;

            // Snake has 3 segments — try to shrink by 10 (more than the body allows)
            _snake.Shrink(10);
            yield return null;

            Assert.GreaterOrEqual(_snake.Length, 2,
                "Shrink must keep at least 2 segments (head + 1 body).");
        }

        // ── StartLength property ──────────────────────────────────────────────

        [Test]
        public void StartLength_ReturnsConfiguredValue()
        {
            SetPrivateField(_snake, "_startLength", 5);
            Assert.AreEqual(5, _snake.StartLength,
                "StartLength property must return the serialized _startLength value.");
        }

        // ── Helper ───────────────────────────────────────────────────────────

        private static void SetPrivateField(object obj, string fieldName, object value)
        {
            var field = obj.GetType().GetField(fieldName,
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field == null)
                throw new System.Exception($"Field '{fieldName}' not found on {obj.GetType().Name}.");
            field.SetValue(obj, value);
        }
    }
}
