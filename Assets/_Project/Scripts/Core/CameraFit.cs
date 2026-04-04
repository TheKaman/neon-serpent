using UnityEngine;

namespace NeonSerpent.Core
{
    /// <summary>
    /// Adjusts the main camera's orthographic size at runtime so the full game grid
    /// (plus a configurable border) is always visible regardless of screen aspect ratio.
    /// Attach to the Main Camera in the Game scene. Call <see cref="FitToGrid"/> after
    /// any level that changes the grid dimensions (e.g. Campaign levels with custom sizes).
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class CameraFit : MonoBehaviour
    {
        [Header("Grid Dimensions (cells)")]
        [SerializeField] private int _gridWidth  = 20;
        [SerializeField] private int _gridHeight = 20;

        [Header("Border (extra cells of padding around the grid)")]
        [SerializeField] private float _borderCells = 1f;

        private Camera _camera;

        private void Awake()
        {
            _camera = GetComponent<Camera>();
            FitToGrid(_gridWidth, _gridHeight);
        }

        /// <summary>
        /// Recalculates and applies the orthographic size needed to show a grid of the
        /// given dimensions plus the configured border on all sides.
        /// Call this whenever the level changes the grid size.
        /// </summary>
        /// <param name="width">Grid width in cells.</param>
        /// <param name="height">Grid height in cells.</param>
        public void FitToGrid(int width, int height)
        {
            if (_camera == null) _camera = GetComponent<Camera>();

            _gridWidth  = width;
            _gridHeight = height;

            // Half-height required to show the full grid vertically (plus border).
            float requiredHeightSize = (height / 2f) + _borderCells;

            // Half-height required to show the full grid horizontally given the current
            // aspect ratio. If the screen is wider than the grid, this will be smaller
            // than requiredHeightSize. If narrower (tall phone), it will be larger.
            float requiredWidthSize = (width / 2f + _borderCells) / _camera.aspect;

            // Take the larger value so the full grid fits in the smaller screen dimension.
            _camera.orthographicSize = Mathf.Max(requiredHeightSize, requiredWidthSize);
        }

#if UNITY_EDITOR
        // Re-fit in the Editor when the Game View aspect ratio changes.
        private void Update()
        {
            FitToGrid(_gridWidth, _gridHeight);
        }
#endif
    }
}
