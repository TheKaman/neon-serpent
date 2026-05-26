using NUnit.Framework;
using UnityEngine;
using NeonSerpent.Grid;

namespace NeonSerpent.Tests
{
    /// <summary>
    /// EditMode unit tests for GridSystem — verifies cell state management, wall placement,
    /// bounds checking, and the validated start-position logic added in the Bug A fix.
    /// All tests create a GridSystem via <c>AddComponent</c> so no scene is required.
    /// </summary>
    [TestFixture]
    [Category("Campaign")]
    [Category("Grid")]
    public class GridSystemTests
    {
        private GameObject _go;
        private GridSystem _grid;

        [SetUp]
        public void Setup()
        {
            _go   = new GameObject("GridTest");
            _grid = _go.AddComponent<GridSystem>();
            // Initialize to a known size — 20×20 is the default campaign grid.
            _grid.InitializeGrid(20, 20);
        }

        [TearDown]
        public void Teardown()
        {
            Object.DestroyImmediate(_go);
        }

        // ── Bounds ────────────────────────────────────────────────────────────

        [Test]
        public void IsInBounds_ValidCell_ReturnsTrue()
        {
            Assert.IsTrue(_grid.IsInBounds(new Vector2Int(0,  0)));
            Assert.IsTrue(_grid.IsInBounds(new Vector2Int(19, 19)));
            Assert.IsTrue(_grid.IsInBounds(new Vector2Int(10, 10)));
        }

        [Test]
        public void IsInBounds_OutsideCell_ReturnsFalse()
        {
            Assert.IsFalse(_grid.IsInBounds(new Vector2Int(-1,  0)));
            Assert.IsFalse(_grid.IsInBounds(new Vector2Int(20,  0)));
            Assert.IsFalse(_grid.IsInBounds(new Vector2Int( 0, 20)));
            Assert.IsFalse(_grid.IsInBounds(new Vector2Int( 0, -1)));
        }

        [Test]
        public void GetCellType_OutOfBounds_ReturnsWall()
        {
            Assert.AreEqual(GridCellType.Wall, _grid.GetCellType(new Vector2Int(-1, 0)));
            Assert.AreEqual(GridCellType.Wall, _grid.GetCellType(new Vector2Int(20, 0)));
        }

        // ── InitializeGrid ────────────────────────────────────────────────────

        [Test]
        public void InitializeGrid_AllCellsStartEmpty()
        {
            _grid.InitializeGrid(10, 10);
            for (int x = 0; x < 10; x++)
                for (int y = 0; y < 10; y++)
                    Assert.AreEqual(GridCellType.Empty, _grid.GetCellType(new Vector2Int(x, y)),
                        $"Cell ({x},{y}) should be Empty after InitializeGrid.");
        }

        [Test]
        public void InitializeGrid_UpdatesDimensions()
        {
            _grid.InitializeGrid(16, 18);
            Assert.AreEqual(16, _grid.Width);
            Assert.AreEqual(18, _grid.Height);
        }

        [Test]
        public void InitializeGrid_ClearsExistingCells()
        {
            // Place some cells, then reinitialise — everything should be wiped.
            _grid.SetCell(new Vector2Int(5, 5), GridCellType.Snake);
            _grid.SetCell(new Vector2Int(3, 3), GridCellType.Wall);
            _grid.InitializeGrid(20, 20);
            Assert.AreEqual(GridCellType.Empty, _grid.GetCellType(new Vector2Int(5, 5)));
            Assert.AreEqual(GridCellType.Empty, _grid.GetCellType(new Vector2Int(3, 3)));
        }

        // ── SetCell / ClearCell ───────────────────────────────────────────────

        [Test]
        public void SetCell_ThenGetCell_ReturnsCorrectType()
        {
            _grid.SetCell(new Vector2Int(5, 5), GridCellType.Food);
            Assert.AreEqual(GridCellType.Food, _grid.GetCellType(new Vector2Int(5, 5)));
        }

        [Test]
        public void ClearCell_SetsBackToEmpty()
        {
            _grid.SetCell(new Vector2Int(7, 7), GridCellType.Snake);
            _grid.ClearCell(new Vector2Int(7, 7));
            Assert.AreEqual(GridCellType.Empty, _grid.GetCellType(new Vector2Int(7, 7)));
        }

        [Test]
        public void SetCell_OutOfBounds_DoesNotThrow()
        {
            // Should log a warning and silently do nothing — must not throw.
            Assert.DoesNotThrow(() => _grid.SetCell(new Vector2Int(-1, 0), GridCellType.Food));
            Assert.DoesNotThrow(() => _grid.SetCell(new Vector2Int(20, 0), GridCellType.Food));
        }

        // ── SetWalls ──────────────────────────────────────────────────────────

        [Test]
        public void SetWalls_PlacesWallCells()
        {
            _grid.SetWalls(new[] { new Vector2Int(5, 5), new Vector2Int(6, 5) });
            Assert.AreEqual(GridCellType.Wall, _grid.GetCellType(new Vector2Int(5, 5)));
            Assert.AreEqual(GridCellType.Wall, _grid.GetCellType(new Vector2Int(6, 5)));
        }

        [Test]
        public void SetWalls_OutOfBoundsPositionsAreSkipped()
        {
            // Should not throw — out-of-bounds wall positions are ignored.
            Assert.DoesNotThrow(() =>
                _grid.SetWalls(new[] { new Vector2Int(-1, 0), new Vector2Int(25, 0) }));
        }

        [Test]
        public void InitializeGrid_AfterSetWalls_ClearsAllWalls()
        {
            _grid.SetWalls(new[] { new Vector2Int(5, 5) });
            _grid.InitializeGrid(20, 20);
            Assert.AreEqual(GridCellType.Empty, _grid.GetCellType(new Vector2Int(5, 5)),
                "InitializeGrid must clear wall cells from the previous level.");
        }

        // ── GetValidatedStartPosition (Bug A fix) ─────────────────────────────

        [Test]
        public void GetValidatedStartPosition_WhenNoClearanceNeeded_ReturnsPreferred()
        {
            // No walls placed — preferred position should be returned unchanged.
            var preferred = new Vector2Int(5, 10);
            var result    = _grid.GetValidatedStartPosition(preferred, 3);
            Assert.AreEqual(preferred, result);
        }

        [Test]
        public void GetValidatedStartPosition_WhenPreferredIsOnWall_ReturnsAlternative()
        {
            // Level_4_1 scenario: walls at (4,10) and (5,10).
            // Snake with startLength=3 would place body at (3,10),(4,10),(5,10).
            _grid.SetWalls(new[] { new Vector2Int(4, 10), new Vector2Int(5, 10) });

            var preferred = new Vector2Int(5, 10);
            var result    = _grid.GetValidatedStartPosition(preferred, 3);

            // Result must not be a wall cell and must be in bounds.
            Assert.IsFalse(result == preferred,
                "Expected a different position because preferred overlaps walls.");
            Assert.IsTrue(_grid.IsInBounds(result),
                "Validated start position must be in bounds.");

            // Verify the full spawn run is clear.
            for (int i = 0; i < 3; i++)
            {
                var cell = new Vector2Int(result.x - i, result.y);
                Assert.IsTrue(_grid.IsInBounds(cell),
                    $"Body segment at ({cell}) is out of bounds.");
                Assert.AreNotEqual(GridCellType.Wall, _grid.GetCellType(cell),
                    $"Body segment at ({cell}) overlaps a wall.");
            }
        }

        [Test]
        public void GetValidatedStartPosition_WithSnakeLength1_AcceptsAnyEmptyCell()
        {
            _grid.SetWalls(new[] { new Vector2Int(5, 10) });

            // With snakeLength=1 we only need the head cell itself to be clear.
            var result = _grid.GetValidatedStartPosition(new Vector2Int(5, 10), 1);
            Assert.AreNotEqual(new Vector2Int(5, 10), result,
                "Start position should avoid the wall at (5,10).");
            Assert.AreEqual(GridCellType.Empty, _grid.GetCellType(result));
        }

        [Test]
        public void GetValidatedStartPosition_WhenPreferredIsOutOfBounds_ReturnsSafeCell()
        {
            // Preferred position outside the grid — should find a valid in-bounds cell.
            var result = _grid.GetValidatedStartPosition(new Vector2Int(-1, -1), 3);
            Assert.IsTrue(_grid.IsInBounds(result),
                "Fallback position must be inside the grid.");
        }

        // ── GetRandomEmptyCell ────────────────────────────────────────────────

        [Test]
        public void GetRandomEmptyCell_OnFreshGrid_ReturnsInBoundsCell()
        {
            var cell = _grid.GetRandomEmptyCell();
            Assert.IsTrue(_grid.IsInBounds(cell),
                "GetRandomEmptyCell on a fresh grid must return an in-bounds position.");
        }

        [Test]
        public void GetRandomEmptyCell_WhenGridIsFull_ReturnsSentinel()
        {
            // Fill the entire 5×5 grid with Snake cells.
            _grid.InitializeGrid(5, 5);
            for (int x = 0; x < 5; x++)
                for (int y = 0; y < 5; y++)
                    _grid.SetCell(new Vector2Int(x, y), GridCellType.Snake);

            var cell = _grid.GetRandomEmptyCell();
            Assert.AreEqual(new Vector2Int(-1, -1), cell,
                "GetRandomEmptyCell must return (-1,-1) when every cell is occupied.");
        }
    }
}
