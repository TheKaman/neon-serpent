using UnityEngine;
using NeonSerpent.Utilities;

namespace NeonSerpent.Grid
{
    /// <summary>
    /// Renders the neon grid background as a procedural mesh of lines.
    /// Attach to a child GameObject of the GridSystem with a MeshFilter and MeshRenderer.
    /// The material should use an Unlit/Color or URP Unlit shader with the grid line color.
    /// </summary>
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class NeonGridRenderer : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GridSystem _grid;

        [Header("Appearance")]
        [SerializeField] private float _lineWidth = 0.04f;
        [SerializeField] private Color _lineColor = new Color(0.1f, 0.25f, 0.12f, 1f);

        private MeshFilter   _meshFilter;
        private MeshRenderer _meshRenderer;

        private void Awake()
        {
            _meshFilter   = GetComponent<MeshFilter>();
            _meshRenderer = GetComponent<MeshRenderer>();
        }

        private void Start()
        {
            // Wait a frame so GridSystem has initialized
            BuildGridMesh();
            CenterCamera();
        }

        /// <summary>Rebuild the grid mesh (call after grid dimensions change).</summary>
        public void BuildGridMesh()
        {
            int w = _grid.Width;
            int h = _grid.Height;

            // Each line segment = a thin quad (4 verts, 2 tris)
            // Lines: (w+1) vertical + (h+1) horizontal
            int lineCount  = (w + 1) + (h + 1);
            var vertices   = new Vector3[lineCount * 4];
            var triangles  = new int[lineCount * 6];
            var colors     = new Color[lineCount * 4];

            int vi = 0; int ti = 0;

            float half = _lineWidth * 0.5f;
            float totalW = w * Constants.CELL_SIZE;
            float totalH = h * Constants.CELL_SIZE;

            // Vertical lines
            for (int x = 0; x <= w; x++)
            {
                float px = x * Constants.CELL_SIZE - half;
                vertices[vi + 0] = new Vector3(px,        -half,   0);
                vertices[vi + 1] = new Vector3(px + _lineWidth, -half, 0);
                vertices[vi + 2] = new Vector3(px,        totalH + half, 0);
                vertices[vi + 3] = new Vector3(px + _lineWidth, totalH + half, 0);
                AddQuadTris(triangles, ref ti, vi);
                for (int i = 0; i < 4; i++) colors[vi + i] = _lineColor;
                vi += 4;
            }

            // Horizontal lines
            for (int y = 0; y <= h; y++)
            {
                float py = y * Constants.CELL_SIZE - half;
                vertices[vi + 0] = new Vector3(-half,       py,                  0);
                vertices[vi + 1] = new Vector3(totalW + half, py,                0);
                vertices[vi + 2] = new Vector3(-half,       py + _lineWidth,     0);
                vertices[vi + 3] = new Vector3(totalW + half, py + _lineWidth,   0);
                AddQuadTris(triangles, ref ti, vi);
                for (int i = 0; i < 4; i++) colors[vi + i] = _lineColor;
                vi += 4;
            }

            var mesh       = new Mesh();
            mesh.name      = "NeonGrid";
            mesh.vertices  = vertices;
            mesh.triangles = triangles;
            mesh.colors    = colors;
            mesh.RecalculateBounds();

            _meshFilter.mesh = mesh;
        }

        private void AddQuadTris(int[] tris, ref int ti, int vi)
        {
            tris[ti++] = vi + 0; tris[ti++] = vi + 2; tris[ti++] = vi + 1;
            tris[ti++] = vi + 1; tris[ti++] = vi + 2; tris[ti++] = vi + 3;
        }

        /// <summary>Center the camera on the grid and fit it to the screen.</summary>
        private void CenterCamera()
        {
            Camera cam = Camera.main;
            if (cam == null) return;

            float gridW = _grid.Width  * Constants.CELL_SIZE;
            float gridH = _grid.Height * Constants.CELL_SIZE;

            // Center camera on grid
            cam.transform.position = new Vector3(gridW * 0.5f - Constants.CELL_SIZE * 0.5f,
                                                  gridH * 0.5f - Constants.CELL_SIZE * 0.5f,
                                                  -10f);

            // Fit orthographic size with a small margin
            float margin = 1.5f;
            float aspect = (float)Screen.width / Screen.height;
            cam.orthographicSize = Mathf.Max(gridH * 0.5f + margin,
                                             gridW * 0.5f / aspect + margin);
        }
    }
}
