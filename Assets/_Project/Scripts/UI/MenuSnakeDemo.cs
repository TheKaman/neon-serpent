using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using NeonSerpent.Utilities;

namespace NeonSerpent.UI
{
    /// <summary>
    /// Decorative background snake for the main menu.
    /// Draws an animated neon snake that slithers around a virtual grid
    /// rendered entirely with UI Images — no GridSystem or SnakeController required.
    /// </summary>
    public class MenuSnakeDemo : MonoBehaviour
    {
        [Header("Grid")]
        [SerializeField] private int   _cols        = 14;
        [SerializeField] private int   _rows        = 22;
        [SerializeField] private float _cellSize    = 52f;

        [Header("Snake")]
        [SerializeField] private int   _length      = 16;
        [SerializeField] private float _moveInterval = 0.14f;

        private readonly List<Vector2Int> _cells     = new();
        private readonly List<Vector2Int> _prevCells = new();
        private Vector2Int                _dir       = Vector2Int.right;
        private float                     _elapsed;

        private readonly List<RectTransform> _rects  = new();
        private readonly List<Image>         _images = new();

        private void Awake()
        {
            BuildPool();
            InitSnake();
        }

        private void BuildPool()
        {
            for (int i = 0; i < _length; i++)
            {
                var go  = new GameObject($"DemoSeg{i}", typeof(RectTransform), typeof(Image));
                go.transform.SetParent(transform, false);

                var rt = go.GetComponent<RectTransform>();
                float segSize      = _cellSize * 0.72f;
                rt.sizeDelta       = new Vector2(segSize, segSize);
                rt.anchorMin       = new Vector2(0.5f, 0.5f);
                rt.anchorMax       = new Vector2(0.5f, 0.5f);
                rt.pivot           = new Vector2(0.5f, 0.5f);

                var img   = go.GetComponent<Image>();
                img.color = Color.clear;

                _rects.Add(rt);
                _images.Add(img);
            }
        }

        private void InitSnake()
        {
            _cells.Clear();
            _prevCells.Clear();

            int startX = _cols / 2;
            int startY = _rows / 2;
            for (int i = 0; i < _length; i++)
                _cells.Add(new Vector2Int(Mathf.Max(0, startX - i), startY));

            _prevCells.AddRange(_cells);
            _dir     = Vector2Int.right;
            _elapsed = 0f;
        }

        private void Update()
        {
            _elapsed += Time.unscaledDeltaTime;

            if (_elapsed >= _moveInterval)
            {
                _elapsed -= _moveInterval;
                Step();
            }

            RefreshVisuals(Mathf.Clamp01(_elapsed / _moveInterval));
        }

        private void Step()
        {
            _prevCells.Clear();
            _prevCells.AddRange(_cells);

            // Randomly turn ~25 % of steps
            float roll = Random.value;
            if (roll < 0.25f)
            {
                _dir = roll < 0.125f
                    ? new Vector2Int(-_dir.y,  _dir.x)   // left turn
                    : new Vector2Int( _dir.y, -_dir.x);  // right turn
            }

            Vector2Int next = _cells[0] + _dir;

            // Steer away from walls before committing
            if (!InBounds(next))
            {
                _dir = FindSafeDir();
                next = _cells[0] + _dir;
            }

            _cells.Insert(0, next);
            if (_cells.Count > _length)
                _cells.RemoveAt(_cells.Count - 1);
        }

        private Vector2Int FindSafeDir()
        {
            Vector2Int head = _cells[0];
            var dirs = new[] { Vector2Int.right, Vector2Int.up, Vector2Int.left, Vector2Int.down };

            // Fisher-Yates shuffle
            for (int i = dirs.Length - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (dirs[i], dirs[j]) = (dirs[j], dirs[i]);
            }

            foreach (var d in dirs)
                if (InBounds(head + d)) return d;

            return _dir; // fallback — should never reach here on a non-1-cell grid
        }

        private bool InBounds(Vector2Int c) =>
            c.x >= 0 && c.x < _cols && c.y >= 0 && c.y < _rows;

        private Vector2 CellToLocal(Vector2Int c)
        {
            // Anchored to the center of this RectTransform
            float ox = (_cols - 1) * _cellSize * -0.5f;
            float oy = (_rows - 1) * _cellSize * -0.5f;
            return new Vector2(c.x * _cellSize + ox, c.y * _cellSize + oy);
        }

        private void RefreshVisuals(float t)
        {
            int count = Mathf.Min(_cells.Count, _length);
            for (int i = 0; i < _rects.Count; i++)
            {
                if (i >= count)
                {
                    _images[i].color = Color.clear;
                    continue;
                }

                // Smooth interpolation between previous and current grid positions
                Vector2Int prevCell = i < _prevCells.Count ? _prevCells[i] : _cells[i];
                _rects[i].anchoredPosition = Vector2.Lerp(
                    CellToLocal(prevCell), CellToLocal(_cells[i]), t);

                // Quadratic falloff: head bright, tail nearly invisible
                float fade  = 1f - (float)i / count;
                float alpha = fade * fade * 0.45f;
                Color col   = i == 0 ? NeonColorPalette.SnakeHead : NeonColorPalette.SnakeBody;
                _images[i].color = new Color(col.r, col.g, col.b, alpha);
            }
        }
    }
}
