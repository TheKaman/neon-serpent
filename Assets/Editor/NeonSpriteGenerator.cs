// NeonSpriteGenerator.cs
// Generates pixel-art sprites using Signed Distance Fields (SDF).
// Called from NeonSerpentSetup.CreateSprites().

using System;
using System.IO;
using UnityEngine;
using UnityEditor;

public static class NeonSpriteGenerator
{
    private const int   SZ   = 32;
    private const float HALF = SZ * 0.5f;   // 16.0
    private const float AA   = 0.8f;         // antialiasing band width (pixels)

    // ── Menu entry — regenerate sprites without rebuilding scenes ───────────────

    [MenuItem("NeonSerpent/Regenerate Sprites")]
    public static void RegenerateAll()
    {
        MakeSnakeHead();
        MakeSnakeBody();
        MakeFoodNormal();
        MakeFoodBonus();
        MakeFoodPoison();
        MakePUSpeed();
        MakePUShield();
        MakePUMultiplier();
        MakePUGhost();
        MakePUShrink();
        MakePUPoison();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[NeonSpriteGenerator] All 11 sprites regenerated.");
        EditorUtility.DisplayDialog("Sprites Updated",
            "All 11 pixel-art sprites have been regenerated.\n" +
            "Existing prefabs will pick up the new art automatically.",
            "OK");
    }

    // ── Public sprite factories ──────────────────────────────────────────────────

    public static Sprite MakeSnakeHead()      => Build("SnakeHead",     DrawSnakeHead);
    public static Sprite MakeSnakeBody()      => Build("SnakeBody",     DrawSnakeBody);
    public static Sprite MakeFoodNormal()     => Build("FoodNormal",    DrawFoodNormal);
    public static Sprite MakeFoodBonus()      => Build("FoodBonus",     DrawFoodBonus);
    public static Sprite MakeFoodPoison()     => Build("FoodPoison",    DrawFoodPoison);
    public static Sprite MakePUSpeed()        => Build("PU_SpeedBoost", DrawPUSpeed);
    public static Sprite MakePUShield()       => Build("PU_Shield",     DrawPUShield);
    public static Sprite MakePUMultiplier()   => Build("PU_Multiplier", DrawPUMultiplier);
    public static Sprite MakePUGhost()        => Build("PU_Ghost",      DrawPUGhost);
    public static Sprite MakePUShrink()       => Build("PU_Shrink",     DrawPUShrink);
    public static Sprite MakePUPoison()       => Build("PU_Poison",     DrawPUPoison);

    // ── Draw functions ───────────────────────────────────────────────────────────

    static void DrawSnakeHead(Color[] p)
    {
        var body   = new Color(0f,   0.95f, 0.45f);
        var shine  = new Color(0.4f, 1f,    0.7f,  0.35f);
        var white  = Color.white;
        var black  = Color.black;
        var tongue = new Color(1f,   0.15f, 0.3f);

        // Body — rounded rectangle
        Fill(p, (x, y) => RRect(x, y, HALF, HALF, 12f, 12f, 4.5f), body);
        // Shine highlight (upper-left)
        Fill(p, (x, y) => Circ(x, y, 10f, 23f, 4f), shine);
        // Eyes on the right side (head faces right)
        Fill(p, (x, y) => Circ(x, y, 22f, 23f, 3f), white);
        Fill(p, (x, y) => Circ(x, y, 22f, 10f, 3f), white);
        Fill(p, (x, y) => Circ(x, y, 23f, 23f, 1.5f), black);
        Fill(p, (x, y) => Circ(x, y, 23f, 10f, 1.5f), black);
        // Forked tongue poking right
        Fill(p, (x, y) => Rect(x, y, 28.5f, 16.5f, 4f,  0.7f), tongue);
        Fill(p, (x, y) => Rect(x, y, 30.5f, 19f,   2f,  0.7f), tongue);
        Fill(p, (x, y) => Rect(x, y, 30.5f, 14f,   2f,  0.7f), tongue);
    }

    static void DrawSnakeBody(Color[] p)
    {
        var body  = new Color(0f,   0.75f, 0.35f);
        var scale = new Color(0f,   0.55f, 0.25f, 0.6f);
        var shine = new Color(0.3f, 1f,    0.6f,  0.2f);

        Fill(p, (x, y) => RRect(x, y, HALF, HALF, 11.5f, 11.5f, 4f), body);
        Fill(p, (x, y) => Diamond(x, y, HALF, HALF, 5f), scale);
        Fill(p, (x, y) => Circ(x, y, 11f, 23f, 3.5f), shine);
    }

    static void DrawFoodNormal(Color[] p)
    {
        var red   = new Color(0.95f, 0.18f, 0.18f);
        var shine = new Color(1f,    0.6f,  0.6f,  0.9f);
        var stem  = new Color(0.25f, 0.12f, 0f);

        Fill(p, (x, y) => Circ(x, y, HALF, HALF - 1f, 11.5f), red);
        Fill(p, (x, y) => Circ(x, y, 21f, 22f, 3f), shine);
        Fill(p, (x, y) => Rect(x, y, HALF, 27f, 1.2f, 2.5f), stem);
    }

    static void DrawFoodBonus(Color[] p)
    {
        var gold  = new Color(1f, 0.85f, 0f);
        var shine = new Color(1f, 1f,    0.65f, 0.9f);
        var inner = new Color(0.8f, 0.6f, 0f, 0.35f);

        // Large diamond = gem shape
        Fill(p, (x, y) => Diamond(x, y, HALF, HALF, 14f), gold);
        // Darker inner facet for depth
        Fill(p, (x, y) => Diamond(x, y, HALF, HALF, 6f), inner);
        // Shine
        Fill(p, (x, y) => Circ(x, y, 20f, 23f, 2.5f), shine);
    }

    static void DrawFoodPoison(Color[] p)
    {
        var purple = new Color(0.55f, 0f, 0.85f);
        var yellow = new Color(0.95f, 0.9f, 0f, 0.95f);

        Fill(p, (x, y) => Circ(x, y, HALF, HALF, 12f), purple);
        // X symbol
        Fill(p, (x, y) => RotRect(x, y, HALF, HALF, 9f, 1.8f,  45f), yellow);
        Fill(p, (x, y) => RotRect(x, y, HALF, HALF, 9f, 1.8f, -45f), yellow);
    }

    static void DrawPUSpeed(Color[] p)
    {
        var orange = new Color(1f, 0.55f, 0f);

        // Lightning bolt: two parallelograms forming a Z
        FillPoly(p, new Vector2[] {
            new Vector2(11, 30), new Vector2(21, 30),
            new Vector2(18, 15), new Vector2(8,  15)
        }, orange);
        FillPoly(p, new Vector2[] {
            new Vector2(13, 15), new Vector2(23, 15),
            new Vector2(20, 4),  new Vector2(10, 4)
        }, orange);
    }

    static void DrawPUShield(Color[] p)
    {
        var blue  = new Color(0.1f, 0.6f, 1f);
        var light = new Color(0.5f, 0.85f, 1f, 0.55f);

        // Rounded top portion
        Fill(p, (x, y) => RRect(x, y, HALF, 21f, 9.5f, 7f, 4f), blue);
        // Pointed bottom triangle
        FillPoly(p, new Vector2[] {
            new Vector2(6.5f, 16f), new Vector2(25.5f, 16f), new Vector2(16f, 4.5f)
        }, blue);
        // Inner highlight
        Fill(p, (x, y) => RRect(x, y, HALF, 21f, 6f, 4.5f, 3f), light);
    }

    static void DrawPUMultiplier(Color[] p)
    {
        var pink  = new Color(1f, 0.1f, 0.75f);

        // Pink circle background
        Fill(p, (x, y) => Circ(x, y, HALF, HALF, 13f), pink);
        // White × symbol
        Fill(p, (x, y) => RotRect(x, y, HALF, HALF, 9f, 2.5f,  45f), Color.white);
        Fill(p, (x, y) => RotRect(x, y, HALF, HALF, 9f, 2.5f, -45f), Color.white);
    }

    static void DrawPUGhost(Color[] p)
    {
        var lavender = new Color(0.62f, 0.55f, 0.95f);
        var white    = Color.white;
        var dark     = new Color(0.2f, 0.1f, 0.4f);

        // Head (round top)
        Fill(p, (x, y) => Circ(x, y, HALF, 22f, 9.5f), lavender);
        // Body
        Fill(p, (x, y) => Rect(x, y, HALF, 13f, 9.5f, 9f), lavender);
        // Three wavy bumps at the bottom
        Fill(p, (x, y) => Circ(x, y, 10f,  5f, 4.5f), lavender);
        Fill(p, (x, y) => Circ(x, y, HALF, 5f, 4.5f), lavender);
        Fill(p, (x, y) => Circ(x, y, 22f,  5f, 4.5f), lavender);
        // Eyes
        Fill(p, (x, y) => Circ(x, y, 13f, 23f, 2.8f), white);
        Fill(p, (x, y) => Circ(x, y, 19f, 23f, 2.8f), white);
        Fill(p, (x, y) => Circ(x, y, 13.5f, 22.5f, 1.3f), dark);
        Fill(p, (x, y) => Circ(x, y, 19.5f, 22.5f, 1.3f), dark);
    }

    static void DrawPUShrink(Color[] p)
    {
        var green = new Color(0.25f, 1f, 0.15f);

        // Shaft
        Fill(p, (x, y) => Rect(x, y, HALF, 20f, 4f, 8f), green);
        // Arrowhead pointing down
        FillPoly(p, new Vector2[] {
            new Vector2(8f, 13f), new Vector2(24f, 13f), new Vector2(16f, 4f)
        }, green);
    }

    static void DrawPUPoison(Color[] p)
    {
        var bg   = new Color(0.5f, 0f,   0.7f);
        var bone = new Color(0.9f, 0.9f, 0.87f);
        var dark = new Color(0.3f, 0f,   0.45f);

        // Background circle
        Fill(p, (x, y) => Circ(x, y, HALF, HALF, 14f), bg);
        // Skull head
        Fill(p, (x, y) => Circ(x, y, HALF, 21f, 8f), bone);
        // Jaw
        Fill(p, (x, y) => RRect(x, y, HALF, 12f, 5.5f, 2.5f, 2f), bone);
        // Eye sockets
        Fill(p, (x, y) => Circ(x, y, 13f, 22f, 2.5f), dark);
        Fill(p, (x, y) => Circ(x, y, 19f, 22f, 2.5f), dark);
        // Nose
        Fill(p, (x, y) => Diamond(x, y, HALF, 18f, 1.5f), dark);
        // Tooth gaps
        Fill(p, (x, y) => Rect(x, y, 13f,  11.5f, 0.8f, 1.5f), dark);
        Fill(p, (x, y) => Rect(x, y, HALF, 11.5f, 0.8f, 1.5f), dark);
        Fill(p, (x, y) => Rect(x, y, 19f,  11.5f, 0.8f, 1.5f), dark);
    }

    // ── SDF primitives (negative = inside shape) ─────────────────────────────────

    static float Circ(float px, float py, float cx, float cy, float r)
        => Mathf.Sqrt((px - cx) * (px - cx) + (py - cy) * (py - cy)) - r;

    static float Rect(float px, float py, float cx, float cy, float hw, float hh)
        => Mathf.Max(Mathf.Abs(px - cx) - hw, Mathf.Abs(py - cy) - hh);

    static float RRect(float px, float py, float cx, float cy, float hw, float hh, float r)
    {
        float dx = Mathf.Max(Mathf.Abs(px - cx) - hw + r, 0f);
        float dy = Mathf.Max(Mathf.Abs(py - cy) - hh + r, 0f);
        return Mathf.Sqrt(dx * dx + dy * dy) - r;
    }

    static float Diamond(float px, float py, float cx, float cy, float r)
        => Mathf.Abs(px - cx) + Mathf.Abs(py - cy) - r;

    static float RotRect(float px, float py, float cx, float cy, float len, float wid, float deg)
    {
        float a  = deg * Mathf.Deg2Rad;
        float lx = (px - cx) * Mathf.Cos(a) + (py - cy) * Mathf.Sin(a);
        float ly = -(px - cx) * Mathf.Sin(a) + (py - cy) * Mathf.Cos(a);
        return Rect(lx, ly, 0, 0, len, wid);
    }

    // ── Rasteriser ───────────────────────────────────────────────────────────────

    // Alpha-composite color over the pixel buffer wherever sdf <= 0 (with AA softness).
    static void Fill(Color[] pixels, Func<float, float, float> sdf, Color col)
    {
        for (int y = 0; y < SZ; y++)
        for (int x = 0; x < SZ; x++)
        {
            float d     = sdf(x + 0.5f, y + 0.5f);
            float alpha = Mathf.Clamp01((-d + AA) / AA) * col.a;
            if (alpha <= 0f) continue;

            int   i   = y * SZ + x;
            Color src = pixels[i];
            float oa  = alpha + src.a * (1f - alpha);
            if (oa < 0.001f) continue;

            pixels[i] = new Color(
                (col.r * alpha + src.r * src.a * (1f - alpha)) / oa,
                (col.g * alpha + src.g * src.a * (1f - alpha)) / oa,
                (col.b * alpha + src.b * src.a * (1f - alpha)) / oa,
                oa);
        }
    }

    // Ray-casting polygon fill (works for non-convex polygons).
    static void FillPoly(Color[] pixels, Vector2[] pts, Color col)
    {
        float minX = SZ, minY = SZ, maxX = 0, maxY = 0;
        foreach (var pt in pts)
        {
            minX = Mathf.Min(minX, pt.x); minY = Mathf.Min(minY, pt.y);
            maxX = Mathf.Max(maxX, pt.x); maxY = Mathf.Max(maxY, pt.y);
        }
        int x0 = Mathf.Max(0, (int)minX - 1), y0 = Mathf.Max(0, (int)minY - 1);
        int x1 = Mathf.Min(SZ - 1, (int)maxX + 1), y1 = Mathf.Min(SZ - 1, (int)maxY + 1);

        for (int y = y0; y <= y1; y++)
        for (int x = x0; x <= x1; x++)
        {
            if (!InsidePoly(x + 0.5f, y + 0.5f, pts)) continue;
            int   i   = y * SZ + x;
            float a   = col.a;
            Color src = pixels[i];
            float oa  = a + src.a * (1f - a);
            if (oa < 0.001f) continue;
            pixels[i] = new Color(
                (col.r * a + src.r * src.a * (1f - a)) / oa,
                (col.g * a + src.g * src.a * (1f - a)) / oa,
                (col.b * a + src.b * src.a * (1f - a)) / oa,
                oa);
        }
    }

    static bool InsidePoly(float px, float py, Vector2[] pts)
    {
        int  n      = pts.Length;
        bool inside = false;
        for (int i = 0, j = n - 1; i < n; j = i++)
        {
            float xi = pts[i].x, yi = pts[i].y;
            float xj = pts[j].x, yj = pts[j].y;
            if ((yi > py) != (yj > py) &&
                px < (xj - xi) * (py - yi) / (yj - yi) + xi)
                inside = !inside;
        }
        return inside;
    }

    // ── Asset pipeline ───────────────────────────────────────────────────────────

    static Sprite Build(string name, Action<Color[]> drawFn)
    {
        EnsureFolder("Assets/_Project/Textures/Sprites");

        var pixels = new Color[SZ * SZ];   // all transparent
        drawFn(pixels);

        string path = $"Assets/_Project/Textures/Sprites/{name}.png";

        var tex = new Texture2D(SZ, SZ, TextureFormat.RGBA32, false);
        tex.SetPixels(pixels);
        tex.Apply();
        File.WriteAllBytes(Path.GetFullPath(path), tex.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(tex);

        AssetDatabase.ImportAsset(path);
        var imp = (TextureImporter)AssetImporter.GetAtPath(path);
        imp.textureType         = TextureImporterType.Sprite;
        imp.spritePixelsPerUnit = SZ;
        imp.filterMode          = FilterMode.Point;
        imp.textureCompression  = TextureImporterCompression.Uncompressed;
        imp.alphaIsTransparency = true;
        imp.SaveAndReimport();

        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    static void EnsureFolder(string path)
    {
        var parts = path.Split('/');
        string cur = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = cur + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(cur, parts[i]);
            cur = next;
        }
    }
}
