// Battle of the Brooms, diorama pass: the play set is a chunk of world on a wooden pedestal, in the bright toy look of the
// references (round little worlds, floating display pieces): a rounded grass island over layered rock, a garden of trees,
// bushes, flowers, rocks and mushrooms around the rental room, a written sky, and hand-written surface textures (grass, deck
// planks, rock strata, checker floor, wallpaper). All procedural, so the art pack needs no new image files.
using System.Collections.Generic;
using Bob.Modular;
using UnityEngine;

namespace Bob.Game
{
    public partial class BobGame
    {
        // island bounds (grass top); the room deck inside is DeckX0..DeckZ1, the rect the room props were placed in
        internal const float DioX0 = -4.45f, DioX1 = 18.0f, DioZ0 = -3.65f, DioZ1 = 11.15f;
        internal const float DioCx = (DioX0 + DioX1) * 0.5f, DioCz = (DioZ0 + DioZ1) * 0.5f;
        internal const float DioW = DioX1 - DioX0, DioD = DioZ1 - DioZ0;
        const float DeckX0 = -2.75f, DeckX1 = 16.95f, DeckZ0 = -2.5f, DeckZ1 = 10.0f;
        const float DeckCx = (DeckX0 + DeckX1) * 0.5f, DeckCz = (DeckZ0 + DeckZ1) * 0.5f;
        const float DeckW = DeckX1 - DeckX0, DeckD = DeckZ1 - DeckZ0;
        const float IslandR = 2.8f;

        static readonly Color LeafDeep = new Color(0.34f, 0.55f, 0.33f), LeafMid = new Color(0.45f, 0.68f, 0.40f), LeafLite = new Color(0.57f, 0.78f, 0.47f);
        static readonly Color PetalA = new Color(0.98f, 0.98f, 0.94f), PetalB = new Color(0.99f, 0.83f, 0.44f), PetalC = new Color(0.97f, 0.68f, 0.76f);
        static readonly Color Stone = new Color(0.66f, 0.65f, 0.61f), Moss = new Color(0.48f, 0.65f, 0.38f), StemGreen = new Color(0.42f, 0.61f, 0.34f);

        Transform skyQuad;
        Texture2D texGrass, texDeck, texRock, texPlinth, texChecker, texWall, texSky;

        // ---------------------------------------------------------------- noise (tileable value noise)

        static float Hash01(int x, int y)
        {
            unchecked
            {
                int h = x * 374761393 + y * 668265263;
                h = (h ^ (h >> 13)) * 1274126177;
                return ((h ^ (h >> 16)) & 0x7fffffff) / 2147483647f;
            }
        }

        static float Fade(float t) => t * t * (3f - 2f * t);

        /// <summary>Value noise that wraps every px / py cells, so a texture built on it tiles without a seam.</summary>
        static float VNoise(float x, float y, int px, int py)
        {
            int x0 = Mathf.FloorToInt(x), y0 = Mathf.FloorToInt(y);
            int xa = ((x0 % px) + px) % px, xb = (xa + 1) % px;
            int ya = ((y0 % py) + py) % py, yb = (ya + 1) % py;
            float fx = Fade(x - x0), fy = Fade(y - y0);
            float a = Hash01(xa, ya), b = Hash01(xb, ya), c = Hash01(xa, yb), d = Hash01(xb, yb);
            return Mathf.Lerp(Mathf.Lerp(a, b, fx), Mathf.Lerp(c, d, fx), fy);
        }

        static float Fbm(float x, float y, int px, int py, int octaves)
        {
            float sum = 0f, amp = 0.5f, norm = 0f;
            for (int i = 0; i < octaves; i++)
            {
                sum += VNoise(x, y, px, py) * amp; norm += amp;
                x *= 2f; y *= 2f; px *= 2; py *= 2; amp *= 0.5f;
            }
            return sum / norm;
        }

        static Color Mul(Color c, float k) => new Color(c.r * k, c.g * k, c.b * k, 1f);

        static Texture2D NewTex(int w, int h, bool repeat)
        {
            return new Texture2D(w, h, TextureFormat.RGBA32, true)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = repeat ? TextureWrapMode.Repeat : TextureWrapMode.Clamp,
                anisoLevel = 4,
            };
        }

        // ---------------------------------------------------------------- written textures

        Texture2D GrassTex() { if (texGrass == null) texGrass = BuildGrass(); return texGrass; }
        Texture2D DeckTex() { if (texDeck == null) texDeck = BuildDeck(); return texDeck; }
        Texture2D RockTex() { if (texRock == null) texRock = BuildRock(); return texRock; }
        Texture2D PlinthTex() { if (texPlinth == null) texPlinth = BuildPlinth(); return texPlinth; }
        Texture2D CheckerTex() { if (texChecker == null) texChecker = BuildChecker(); return texChecker; }
        Texture2D WallTex() { if (texWall == null) texWall = BuildWallpaper(); return texWall; }
        Texture2D SkyTex() { if (texSky == null) texSky = BuildSkyTexture(); return texSky; }

        static Texture2D BuildGrass()
        {
            const int N = 256;
            var tex = NewTex(N, N, true);
            var px = new Color[N * N];
            var deep = new Color(0.38f, 0.58f, 0.33f); var mid = new Color(0.50f, 0.71f, 0.40f); var lite = new Color(0.64f, 0.82f, 0.48f);
            for (int y = 0; y < N; y++) for (int x = 0; x < N; x++)
            {
                float u = (x + 0.5f) / N, v = (y + 0.5f) / N;
                float patch = Fbm(u * 5f, v * 5f, 5, 5, 3);
                float fine = Fbm(u * 22f, v * 22f, 22, 22, 2);
                float s = Mathf.Clamp01(patch * 0.78f + fine * 0.42f - 0.10f);
                px[y * N + x] = s < 0.5f ? Color.Lerp(deep, mid, s * 2f) : Color.Lerp(mid, lite, (s - 0.5f) * 2f);
            }
            var rnd = new System.Random(7);
            for (int i = 0; i < 1200; i++) // short blades
            {
                int x = rnd.Next(N), y = rnd.Next(N);
                var blade = rnd.Next(2) == 0 ? new Color(0.71f, 0.87f, 0.52f) : new Color(0.33f, 0.52f, 0.29f);
                for (int k = 0; k < 3; k++) { int yy = (y + k) % N; px[yy * N + x] = Color.Lerp(px[yy * N + x], blade, 0.5f); }
            }
            var petals = new[] { new Color(0.99f, 0.97f, 0.90f), new Color(0.99f, 0.85f, 0.44f), new Color(0.98f, 0.75f, 0.81f) };
            for (int i = 0; i < 26; i++) // little daisies
            {
                int x = rnd.Next(N), y = rnd.Next(N); var petal = petals[rnd.Next(petals.Length)];
                for (int dy = -1; dy <= 1; dy++) for (int dx = -1; dx <= 1; dx++)
                {
                    if (dx == 0 && dy == 0) continue;
                    if (Mathf.Abs(dx) + Mathf.Abs(dy) > 1 && rnd.Next(3) > 0) continue;
                    int xx = ((x + dx) % N + N) % N, yy = ((y + dy) % N + N) % N;
                    px[yy * N + xx] = petal;
                }
                px[y * N + x] = new Color(0.99f, 0.72f, 0.30f);
            }
            tex.SetPixels(px); tex.Apply(true); return tex;
        }

        static Texture2D BuildDeck()
        {
            const int N = 256, planks = 4, ph = N / planks;
            var tex = NewTex(N, N, true);
            var px = new Color[N * N];
            var baseC = new Color(0.80f, 0.61f, 0.39f);
            var rnd = new System.Random(11);
            for (int p = 0; p < planks; p++)
            {
                float tint = (float)rnd.NextDouble() * 0.10f - 0.05f;
                int seamX = rnd.Next(50, N - 50);
                float knotX = rnd.Next(20, N - 20), knotY = p * ph + ph * 0.5f;
                bool knot = p % 2 == 0;
                for (int y = p * ph; y < (p + 1) * ph; y++) for (int x = 0; x < N; x++)
                {
                    float u = x / (float)N, v = y / (float)N;
                    float grain = Fbm(u * 16f, v * 96f, 16, 96, 2);
                    float wob = Fbm(u * 6f, v * 30f, 6, 30, 2);
                    float ring = Mathf.Sin((y * 0.45f + wob * 9f) * Mathf.PI) * 0.5f + 0.5f;
                    var c = Mul(baseC, 0.95f + tint + (grain - 0.5f) * 0.16f + ring * 0.05f);
                    int edge = Mathf.Min(y - p * ph, (p + 1) * ph - 1 - y);
                    if (edge == 0) c = Mul(c, 0.60f); else if (edge == 1) c = Mul(c, 0.82f); else if (edge == 2) c = Mul(c, 0.94f);
                    if (Mathf.Abs(x - seamX) < 1) c = Mul(c, 0.72f);
                    if (knot)
                    {
                        float d = (x - knotX) * (x - knotX) / 190f + (y - knotY) * (y - knotY) / 26f;
                        if (d < 1f) c = Mul(c, 1f - (1f - d) * 0.32f);
                    }
                    px[y * N + x] = c;
                }
            }
            tex.SetPixels(px); tex.Apply(true); return tex;
        }

        /// <summary>Rock mottle for the island chunk. The strata read comes from the stacked geometry bands; this only keeps
        /// each band from shading as one flat colour.</summary>
        static Texture2D BuildRock()
        {
            const int N = 128;
            var tex = NewTex(N, N, true);
            var px = new Color[N * N];
            for (int y = 0; y < N; y++) for (int x = 0; x < N; x++)
            {
                float u = (x + 0.5f) / N, v = (y + 0.5f) / N;
                float n = Fbm(u * 6f, v * 6f, 6, 6, 3);
                float fine = Fbm(u * 24f, v * 24f, 24, 24, 2);
                float k = 0.86f + (n - 0.5f) * 0.18f + (fine - 0.5f) * 0.08f;
                if (fine > 0.70f) k -= 0.07f; // grit
                px[y * N + x] = new Color(k, k * 0.99f, k * 0.96f, 1f);
            }
            tex.SetPixels(px); tex.Apply(true); return tex;
        }

        /// <summary>Small vertical planks for the base of the display piece (four planks per tile, dark seams).</summary>
        static Texture2D BuildPlinth()
        {
            const int N = 128, planks = 4, pw = N / planks;
            var tex = NewTex(N, N, true);
            var px = new Color[N * N];
            var rnd = new System.Random(31);
            for (int p = 0; p < planks; p++)
            {
                float tint = (float)rnd.NextDouble() * 0.10f - 0.05f;
                for (int x = p * pw; x < (p + 1) * pw; x++) for (int y = 0; y < N; y++)
                {
                    float u = x / (float)N, v = y / (float)N;
                    float grain = Fbm(u * 40f, v * 6f, 40, 6, 2);
                    float k = 0.93f + tint + (grain - 0.5f) * 0.10f;
                    int edge = Mathf.Min(x - p * pw, (p + 1) * pw - 1 - x);
                    if (edge == 0) k = 0.62f; else if (edge == 1) k = 0.80f;
                    px[y * N + x] = new Color(k, k, k, 1f);
                }
            }
            tex.SetPixels(px); tex.Apply(true); return tex;
        }

        static Texture2D BuildChecker()
        {
            const int N = 128, cells = 4, cs = N / cells;
            var tex = NewTex(N, N, true);
            var px = new Color[N * N];
            var a = new Color(0.97f, 0.94f, 0.87f); var b = new Color(0.55f, 0.76f, 0.70f); var grout = new Color(0.79f, 0.73f, 0.63f);
            for (int y = 0; y < N; y++) for (int x = 0; x < N; x++)
            {
                int cx = x / cs, cy = y / cs;
                var c = (cx + cy) % 2 == 0 ? a : b;
                int ex = Mathf.Min(x - cx * cs, (cx + 1) * cs - 1 - x), ey = Mathf.Min(y - cy * cs, (cy + 1) * cs - 1 - y);
                if (ex < 2 || ey < 2) c = Color.Lerp(c, grout, 0.55f);
                float n = (Hash01(x, y) - 0.5f) * 0.035f;
                px[y * N + x] = new Color(c.r + n, c.g + n, c.b + n, 1f);
            }
            tex.SetPixels(px); tex.Apply(true); return tex;
        }

        static Texture2D BuildWallpaper()
        {
            const int N = 256;
            var tex = NewTex(N, N, true);
            var px = new Color[N * N];
            var paper = new Color(0.96f, 0.92f, 0.82f); var stripe = new Color(0.91f, 0.85f, 0.72f);
            var leaf = new Color(0.50f, 0.73f, 0.67f); var leafLite = new Color(0.62f, 0.81f, 0.73f);
            for (int y = 0; y < N; y++) for (int x = 0; x < N; x++)
            {
                var c = (x % 64) < 16 ? stripe : paper;
                int dx = x % 64 - 32, dy = (y + ((x / 64) % 2) * 32) % 64 - 32;
                int dy2 = dy * 9 / 16; // the quad stretches the tile ~1.8x across, so draw the sprigs tall to come out round
                int d2 = dx * dx + dy2 * dy2;
                if (d2 < 40) c = leaf;
                else if (d2 < 62) c = leafLite;
                float n = (Hash01(x, y) - 0.5f) * 0.02f;
                px[y * N + x] = new Color(c.r + n, c.g + n, c.b + n, 1f);
            }
            tex.SetPixels(px); tex.Apply(true); return tex;
        }

        static Texture2D BuildSkyTexture()
        {
            const int W = 1024, H = 576;
            var tex = NewTex(W, H, false);
            var px = new Color[W * H];
            var horizon = new Color(1.00f, 0.95f, 0.84f); var middle = new Color(0.74f, 0.88f, 0.94f); var zenith = new Color(0.40f, 0.66f, 0.87f);
            for (int y = 0; y < H; y++)
            {
                float v = y / (H - 1f);
                var baseC = v < 0.42f ? Color.Lerp(horizon, middle, v / 0.42f) : Color.Lerp(middle, zenith, (v - 0.42f) / 0.58f);
                for (int x = 0; x < W; x++)
                {
                    float u = x / (W - 1f);
                    var c = baseC;
                    float sx = (u - 0.20f) * 1.75f, sy = v - 0.88f;
                    c = Color.Lerp(c, new Color(1f, 0.97f, 0.84f), Mathf.Clamp01(1f - Mathf.Sqrt(sx * sx + sy * sy) * 3.2f) * 0.9f);
                    float dither = (Hash01(x, y) - 0.5f) * 0.010f;
                    px[y * W + x] = new Color(c.r + dither, c.g + dither, c.b + dither, 1f);
                }
            }
            var rnd = new System.Random(5);
            for (int i = 0; i < 11; i++)
            {
                float cx = 0.08f + (float)rnd.NextDouble() * 0.84f;
                float cy = 0.80f + (float)rnd.NextDouble() * 0.17f;
                float s = 0.55f + (float)rnd.NextDouble() * 0.75f;
                for (int b = 0; b < 4; b++)
                    CloudPuff(px, W, H, (cx + (b - 1.5f) * 0.048f * s) * W, (cy + (b % 2) * 0.016f * s) * H, 0.070f * s * W, 0.028f * s * H, 0.9f);
            }
            // a low sea of cloud under the display piece, so the empty band around the base is sky, not blank paper
            for (int i = 0; i < 10; i++)
            {
                float cx = i * 0.11f + (float)rnd.NextDouble() * 0.05f;
                float cy = 0.09f + (float)rnd.NextDouble() * 0.07f;
                float s = 1.1f + (float)rnd.NextDouble() * 0.9f;
                for (int b = 0; b < 5; b++)
                    CloudPuff(px, W, H, (cx + (b - 2f) * 0.045f * s) * W, (cy + (b % 2) * 0.012f * s) * H, 0.105f * s * W, 0.036f * s * H, 0.42f);
            }
            tex.SetPixels(px); tex.Apply(true); return tex;
        }

        static void CloudPuff(Color[] px, int W, int H, float cx, float cy, float rx, float ry, float alpha)
        {
            var top = new Color(1f, 0.99f, 0.95f); var belly = new Color(0.90f, 0.92f, 0.94f);
            int x0 = Mathf.Max(0, Mathf.FloorToInt(cx - rx)), x1 = Mathf.Min(W - 1, Mathf.CeilToInt(cx + rx));
            int y0 = Mathf.Max(0, Mathf.FloorToInt(cy - ry)), y1 = Mathf.Min(H - 1, Mathf.CeilToInt(cy + ry));
            for (int y = y0; y <= y1; y++) for (int x = x0; x <= x1; x++)
            {
                float dx = (x - cx) / rx, dy = (y - cy) / ry;
                float d = dx * dx + dy * dy; if (d >= 1f) continue;
                float a = Mathf.Pow(1f - d, 1.6f) * alpha;
                px[y * W + x] = Color.Lerp(px[y * W + x], Color.Lerp(belly, top, Mathf.Clamp01(dy * 0.5f + 0.55f)), a);
            }
        }

        // ---------------------------------------------------------------- meshes: rounded plates and stacked rings

        /// <summary>Perimeter of a rounded rectangle, counter-clockwise seen from above, corner arcs connected by straights.</summary>
        static List<Vector2> RoundRect(float w, float d, float r, int seg)
        {
            float hw = Mathf.Max(0.01f, w * 0.5f - r), hd = Mathf.Max(0.01f, d * 0.5f - r);
            r = Mathf.Min(r, Mathf.Min(w, d) * 0.49f);
            var pts = new List<Vector2>(4 * (seg + 1));
            for (int c = 0; c < 4; c++)
            {
                float cx = (c == 0 || c == 3) ? hw : -hw, cy = c < 2 ? hd : -hd;
                int a0 = c * 90;
                for (int i = 0; i <= seg; i++)
                {
                    float a = (a0 + 90f * i / seg) * Mathf.Deg2Rad;
                    pts.Add(new Vector2(cx + Mathf.Cos(a) * r, cy + Mathf.Sin(a) * r));
                }
            }
            return pts;
        }

        /// <summary>A flat fan over a rounded rectangle, UVs in world units so the texture tiles at `tile` units.</summary>
        static Mesh RoundedTop(float w, float d, float r, float tile, int seg = 6)
        {
            var pts = RoundRect(w, d, r, seg);
            int n = pts.Count;
            var verts = new Vector3[n + 1]; var uvs = new Vector2[n + 1]; var nrm = new Vector3[n + 1];
            verts[0] = Vector3.zero; uvs[0] = new Vector2(0.5f, 0.5f); nrm[0] = Vector3.up;
            for (int i = 0; i < n; i++)
            {
                verts[i + 1] = new Vector3(pts[i].x, 0f, pts[i].y);
                uvs[i + 1] = new Vector2(0.5f + pts[i].x / tile, 0.5f + pts[i].y / tile);
                nrm[i + 1] = Vector3.up;
            }
            var tris = new int[n * 3];
            for (int i = 0; i < n; i++)
            {
                int a = 1 + i, b = 1 + (i + 1) % n;
                tris[i * 3] = 0; tris[i * 3 + 1] = b; tris[i * 3 + 2] = a;
            }
            var m = new Mesh { name = "RoundedTop" };
            m.vertices = verts; m.uv = uvs; m.normals = nrm; m.triangles = tris; m.RecalculateBounds();
            return m;
        }

        /// <summary>Stacked rounded rings: the side wall of an island or a pedestal. v runs bottom (v0) to top (v1).</summary>
        static Mesh RoundedPrism(float w, float d, float r, float[] ys, float[] scales, float uTiles, bool cap = true, float v0 = 0f, float v1 = 1f, int seg = 6)
        {
            int n = 4 * (seg + 1);
            var top = RoundRect(w * scales[0], d * scales[0], r * scales[0], seg);
            var cum = new float[n + 1]; float total = 0f;
            for (int i = 0; i < n; i++) { total += (top[(i + 1) % n] - top[i]).magnitude; cum[i + 1] = total; }
            float yTop = ys[0], yBot = ys[ys.Length - 1];
            var verts = new List<Vector3>(); var uvs = new List<Vector2>(); var tris = new List<int>();
            for (int L = 0; L < ys.Length; L++)
            {
                var ring = RoundRect(w * scales[L], d * scales[L], r * scales[L], seg);
                float v = Mathf.Lerp(v0, v1, Mathf.InverseLerp(yBot, yTop, ys[L]));
                for (int i = 0; i <= n; i++)
                {
                    int k = i % n;
                    verts.Add(new Vector3(ring[k].x, ys[L], ring[k].y));
                    uvs.Add(new Vector2(cum[i] / total * uTiles, v));
                }
            }
            int stride = n + 1;
            for (int L = 0; L + 1 < ys.Length; L++)
                for (int i = 0; i < n; i++)
                {
                    int a = L * stride + i, b = a + 1, c = a + stride, dd = c + 1;
                    tris.Add(a); tris.Add(b); tris.Add(c);
                    tris.Add(b); tris.Add(dd); tris.Add(c);
                }
            if (cap)
            {
                int baseIdx = verts.Count;
                verts.Add(new Vector3(0f, ys[0], 0f)); uvs.Add(new Vector2(0.5f, 0.5f));
                for (int i = 0; i < n; i++)
                {
                    var p = top[i];
                    verts.Add(new Vector3(p.x, ys[0], p.y));
                    uvs.Add(new Vector2(0.5f + p.x / Mathf.Max(0.01f, w), 0.5f + p.y / Mathf.Max(0.01f, d)));
                }
                for (int i = 0; i < n; i++)
                {
                    int a = baseIdx + 1 + i, b = baseIdx + 1 + (i + 1) % n;
                    tris.Add(baseIdx); tris.Add(b); tris.Add(a);
                }
            }
            var m = new Mesh { name = "RoundedPrism" };
            m.SetVertices(verts); m.SetUVs(0, uvs); m.SetTriangles(tris, 0); m.RecalculateNormals(); m.RecalculateBounds();
            return m;
        }

        GameObject Part(string name, Mesh mesh, Texture tex, Color tint, Vector3 pos, Transform parent, float outlinePx = 0f)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = BobToon.Toon(tint, tex);
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            if (outlinePx > 0f) BobToon.Outline(go, outlinePx);
            return go;
        }

        /// <summary>One stacked band of the island / plinth, with the outline line that makes the layers read as cut cake.
        /// `cap` is off for the grass lip: its top face would sit above the deck and hide it.</summary>
        void Band(string name, float[] ys, float[] scales, Texture tex, Color tint, float uTiles, float outlinePx, bool cap = true)
        {
            var mesh = RoundedPrism(DioW, DioD, IslandR, ys, scales, uTiles, cap);
            Part(name, mesh, tex, tint, new Vector3(DioCx, 0f, DioCz), setRoot, outlinePx);
        }

        /// <summary>An upright textured quad on the back wall (the room's wallpaper); the quad primitive faces the camera.</summary>
        void Upright(string name, Vector3 centre, Vector2 size, Texture tex, Vector2 tiling)
        {
            var q = GameObject.CreatePrimitive(PrimitiveType.Quad); q.name = name; q.transform.SetParent(setRoot, false);
            var col = q.GetComponent<Collider>(); if (col != null) Destroy(col);
            q.transform.position = centre; q.transform.rotation = Quaternion.identity;
            q.transform.localScale = new Vector3(size.x, size.y, 1f);
            var m = BobToon.Toon(Color.white, tex); m.mainTextureScale = tiling;
            q.GetComponent<MeshRenderer>().sharedMaterial = m;
        }

        // ---------------------------------------------------------------- the set

        /// <summary>The display piece: grass island, a cut-cake stack of soil and rock, the wooden deck and the plinth,
        /// plus the written sky.</summary>
        void BuildDiorama()
        {
            BuildSky();
            var grass = GrassTex(); var deck = DeckTex(); var rock = RockTex(); var plinth = PlinthTex();

            // grass top and lip, then soil and rock layers: the strata are geometry, so they read at any zoom (a written
            // band texture this small just averaged to one brown tone)
            Part("IslandGrass", RoundedTop(DioW, DioD, IslandR, 2.5f), grass, Color.white, new Vector3(DioCx, -0.02f, DioCz), setRoot, 2f);
            Band("IslandLip", new[] { -0.02f, -0.24f }, new[] { 1f, 1f }, grass, Color.white, 9f, 2.5f, false);
            Band("Soil", new[] { -0.24f, -0.50f }, new[] { 1f, 0.995f }, rock, new Color(0.52f, 0.36f, 0.24f), 6f, 2.5f);
            Band("RockA", new[] { -0.50f, -1.00f }, new[] { 0.995f, 0.985f }, rock, new Color(0.87f, 0.73f, 0.52f), 6f, 2.5f);
            Band("RockB", new[] { -1.00f, -1.52f }, new[] { 0.985f, 0.97f }, rock, new Color(0.64f, 0.55f, 0.47f), 6f, 2.5f);
            Band("RockC", new[] { -1.52f, -2.06f }, new[] { 0.97f, 0.94f }, rock, new Color(0.81f, 0.64f, 0.46f), 6f, 2.5f);
            Band("RockD", new[] { -2.06f, -2.62f }, new[] { 0.94f, 0.90f }, rock, new Color(0.58f, 0.50f, 0.44f), 6f, 2.5f);
            Band("RockTaper", new[] { -2.62f, -3.20f }, new[] { 0.90f, 0.84f }, rock, new Color(0.74f, 0.59f, 0.44f), 6f, 2.5f);
            Band("RockTip", new[] { -3.20f, -3.66f }, new[] { 0.84f, 0.80f }, rock, new Color(0.54f, 0.46f, 0.41f), 6f, 2.5f);
            Band("PlinthLip", new[] { -3.66f, -3.86f }, new[] { 0.82f, 0.84f }, plinth, new Color(0.97f, 0.91f, 0.77f), 0.12f, 2f);
            Band("PlinthBody", new[] { -3.86f, -4.62f }, new[] { 0.84f, 0.84f }, plinth, new Color(0.56f, 0.38f, 0.24f), 0.55f, 2f);
            Band("PlinthFoot", new[] { -4.62f, -4.84f }, new[] { 0.86f, 0.88f }, plinth, new Color(0.90f, 0.78f, 0.58f), 0.12f, 2f);

            Part("Deck", RoundedTop(DeckW, DeckD, 1.9f, 3.2f), deck, Color.white, new Vector3(DeckCx, -0.008f, DeckCz), setRoot);
        }

        void BuildSky()
        {
            var q = GameObject.CreatePrimitive(PrimitiveType.Quad); q.name = "Sky";
            var col = q.GetComponent<Collider>(); if (col != null) Destroy(col);
            q.transform.SetParent(cam.transform, false);
            q.transform.localRotation = Quaternion.identity;
            var sh = Resources.Load<Shader>("Shaders/BobUnlit");
            var m = sh != null ? new Material(sh) : BobToon.Toon(Color.white);
            m.mainTexture = SkyTex();
            var mr = q.GetComponent<MeshRenderer>(); mr.sharedMaterial = m;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; mr.receiveShadows = false;
            skyQuad = q.transform;
        }

        /// <summary>Sized from the camera's field of view so the written sky always covers the view, clouds and sun
        /// included: the quad hangs 70 units in front of the camera, 12% wider than the frustum there.</summary>
        void FitSky()
        {
            if (skyQuad == null) return;
            const float depth = 70f;
            float h = 2f * depth * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad) * 1.12f;
            float w = h * Mathf.Max(1f, cam.aspect);
            skyQuad.localPosition = new Vector3(0f, 0f, depth);
            skyQuad.localScale = new Vector3(w, h, 1f);
        }

        // ---------------------------------------------------------------- garden (procedural stand-ins for the missing models)

        void Tree(float x, float z, float h, float yaw) => Tree(setRoot, new Vector3(x, 0f, z), h, yaw);

        void Tree(Transform parent, Vector3 at, float h, float yaw)
        {
            var g = new GameObject("Tree"); g.transform.SetParent(parent, false);
            g.transform.localPosition = at; g.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            float trunkH = h * 0.38f, r = 0.055f * h + 0.045f;
            BobToon.Shape(PrimitiveType.Cylinder, g.transform, "Trunk", new Vector3(0f, trunkH * 0.5f, 0f), new Vector3(r * 2f, trunkH * 0.5f, r * 2f), new Color(0.52f, 0.37f, 0.25f), true, 2f);
            BobToon.Shape(PrimitiveType.Sphere, g.transform, "CanopyA", new Vector3(0f, h * 0.68f, 0f), Vector3.one * (0.54f * h), LeafDeep, true, 2.6f);
            BobToon.Shape(PrimitiveType.Sphere, g.transform, "CanopyB", new Vector3(0.13f * h, h * 0.86f, -0.03f * h), Vector3.one * (0.40f * h), LeafMid, true, 2.6f);
            BobToon.Shape(PrimitiveType.Sphere, g.transform, "CanopyC", new Vector3(-0.09f * h, h * 0.99f, 0.05f * h), Vector3.one * (0.30f * h), LeafLite, true, 2.6f);
        }

        void Bush(float x, float z, float s, float yaw)
        {
            var g = new GameObject("Bush"); g.transform.SetParent(setRoot, false);
            g.transform.localPosition = new Vector3(x, 0f, z); g.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            BobToon.Shape(PrimitiveType.Sphere, g.transform, "LeafA", new Vector3(0f, 0.20f * s, 0f), new Vector3(0.92f * s, 0.60f * s, 0.92f * s), LeafDeep, true, 2.4f);
            BobToon.Shape(PrimitiveType.Sphere, g.transform, "LeafB", new Vector3(0.24f * s, 0.28f * s, 0.08f * s), new Vector3(0.60f * s, 0.50f * s, 0.60f * s), LeafMid, true, 2.4f);
            BobToon.Shape(PrimitiveType.Sphere, g.transform, "LeafC", new Vector3(-0.20f * s, 0.26f * s, -0.06f * s), new Vector3(0.54f * s, 0.44f * s, 0.54f * s), LeafLite, true, 2.4f);
            for (int i = 0; i < 3; i++)
                BobToon.Shape(PrimitiveType.Sphere, g.transform, "Berry", new Vector3(Mathf.Cos(i * 2.2f) * 0.24f * s, (0.34f + 0.08f * (i % 2)) * s, Mathf.Sin(i * 2.2f) * 0.20f * s), Vector3.one * (0.08f * s), new Color(0.95f, 0.55f, 0.55f));
        }

        void Flower(float x, float z, float s, int tone, float yaw)
        {
            var g = new GameObject("Flower"); g.transform.SetParent(setRoot, false);
            g.transform.localPosition = new Vector3(x, 0f, z); g.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            var petal = tone % 3 == 0 ? PetalA : tone % 3 == 1 ? PetalB : PetalC;
            BobToon.Shape(PrimitiveType.Cylinder, g.transform, "Stem", new Vector3(0f, 0.10f * s, 0f), new Vector3(0.045f * s, 0.10f * s, 0.045f * s), StemGreen);
            BobToon.Shape(PrimitiveType.Sphere, g.transform, "Head", new Vector3(0f, 0.24f * s, 0f), new Vector3(0.20f * s, 0.13f * s, 0.20f * s), petal);
            BobToon.Shape(PrimitiveType.Sphere, g.transform, "Eye", new Vector3(0f, 0.26f * s, -0.02f * s), Vector3.one * (0.07f * s), new Color(0.98f, 0.74f, 0.30f));
        }

        void Rock(float x, float z, float s, float yaw)
        {
            var g = new GameObject("Rock"); g.transform.SetParent(setRoot, false);
            g.transform.localPosition = new Vector3(x, 0f, z); g.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            BobToon.Shape(PrimitiveType.Sphere, g.transform, "RockA", new Vector3(0f, 0.16f * s, 0f), new Vector3(0.80f * s, 0.50f * s, 0.72f * s), Stone, true, 2.2f);
            BobToon.Shape(PrimitiveType.Sphere, g.transform, "RockB", new Vector3(0.26f * s, 0.10f * s, 0.10f * s), new Vector3(0.44f * s, 0.32f * s, 0.42f * s), new Color(0.75f, 0.73f, 0.68f), true, 2.2f);
            BobToon.Shape(PrimitiveType.Sphere, g.transform, "Moss", new Vector3(-0.04f * s, 0.36f * s, 0f), new Vector3(0.52f * s, 0.14f * s, 0.46f * s), Moss);
        }

        void Mushroom(float x, float z, float s, float yaw)
        {
            var g = new GameObject("Mushroom"); g.transform.SetParent(setRoot, false);
            g.transform.localPosition = new Vector3(x, 0f, z); g.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            var cream = new Color(0.96f, 0.92f, 0.82f);
            BobToon.Shape(PrimitiveType.Cylinder, g.transform, "Stem", new Vector3(0f, 0.11f * s, 0f), new Vector3(0.16f * s, 0.11f * s, 0.16f * s), cream);
            BobToon.Shape(PrimitiveType.Sphere, g.transform, "Cap", new Vector3(0f, 0.24f * s, 0f), new Vector3(0.44f * s, 0.26f * s, 0.44f * s), new Color(0.88f, 0.40f, 0.34f), true, 2f);
            for (int i = 0; i < 3; i++)
                BobToon.Shape(PrimitiveType.Sphere, g.transform, "Dot", new Vector3(Mathf.Cos(i * 2.1f) * 0.13f * s, 0.31f * s, Mathf.Sin(i * 2.1f) * 0.13f * s), Vector3.one * (0.07f * s), cream);
        }

        void Lantern(float x, float z, float yaw)
        {
            var g = new GameObject("Lantern"); g.transform.SetParent(setRoot, false);
            g.transform.localPosition = new Vector3(x, 0f, z); g.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            BobToon.Shape(PrimitiveType.Cylinder, g.transform, "Post", new Vector3(0f, 0.45f, 0f), new Vector3(0.11f, 0.45f, 0.11f), new Color(0.40f, 0.30f, 0.22f), true, 2f);
            BobToon.Shape(PrimitiveType.Sphere, g.transform, "Lamp", new Vector3(0f, 1.00f, 0f), new Vector3(0.30f, 0.34f, 0.30f), new Color(1f, 0.92f, 0.62f), true, 2f);
            BobToon.Shape(PrimitiveType.Cylinder, g.transform, "Hat", new Vector3(0f, 1.20f, 0f), new Vector3(0.44f, 0.07f, 0.44f), new Color(0.88f, 0.52f, 0.40f), true, 2f);
        }

        /// <summary>Planting on the grass ring around the deck; the ring is outside the play rect so nothing blocks a task point.</summary>
        void DressDioramaRim()
        {
            // behind the room wall: tall canopies rise above it and give the piece its depth
            Tree(1.1f, 10.55f, 3.30f, 20f); Tree(4.4f, 10.80f, 2.95f, 55f); Tree(7.6f, 10.45f, 3.40f, 5f);
            Tree(10.9f, 10.75f, 3.00f, 70f); Tree(13.9f, 10.50f, 3.30f, 40f);
            Bush(2.7f, 10.35f, 0.85f, 15f); Bush(9.2f, 10.40f, 0.80f, 80f); Bush(12.5f, 10.55f, 0.75f, 30f);

            // left garden
            Tree(-3.75f, 9.00f, 3.15f, 120f); Tree(-3.90f, 5.80f, 2.70f, 200f); Tree(-3.60f, 1.40f, 2.95f, 300f);
            Bush(-3.15f, 7.50f, 0.95f, 40f); Bush(-3.25f, 3.60f, 0.85f, 150f); Bush(-3.40f, -0.60f, 0.90f, 260f);
            Rock(-3.30f, 10.20f, 0.85f, 25f); Mushroom(-2.95f, 6.30f, 0.80f, 0f);
            Flower(-3.10f, 8.30f, 0.95f, 0, 0f); Flower(-3.30f, 6.60f, 0.85f, 1, 30f); Flower(-3.00f, 4.70f, 0.90f, 2, 60f);
            Flower(-3.35f, 2.40f, 0.85f, 0, 15f); Flower(-3.05f, 0.30f, 0.95f, 1, 45f); Flower(-3.20f, -1.40f, 0.80f, 2, 75f);

            // right garden (narrow: canopies lean over the deck)
            Tree(17.55f, 8.70f, 3.10f, 250f); Tree(17.55f, 2.10f, 2.60f, 320f);
            Bush(17.30f, 6.20f, 0.80f, 10f); Bush(17.35f, 0.10f, 0.80f, 190f); Bush(17.25f, 9.70f, 0.70f, 100f);
            Flower(17.20f, 4.30f, 0.90f, 0, 0f); Flower(17.40f, -1.20f, 0.85f, 2, 40f);
            Lantern(17.30f, 4.90f, 300f);

            // front garden: bushes and flowers only, tall trees would cover the kitchen and TV
            Bush(-1.60f, -3.00f, 0.95f, 20f); Bush(2.00f, -3.10f, 0.85f, 70f); Bush(6.50f, -3.05f, 0.90f, 130f);
            Bush(10.50f, -3.10f, 0.85f, 210f); Bush(14.00f, -3.00f, 0.95f, 280f);
            Rock(4.30f, -3.20f, 0.85f, 60f); Rock(12.30f, -3.15f, 0.70f, 150f);
            Mushroom(8.60f, -3.00f, 0.85f, 30f); Mushroom(0.60f, -3.10f, 0.75f, 80f);
            Flower(-1.00f, -3.25f, 0.95f, 1, 10f); Flower(0.90f, -3.30f, 0.90f, 0, 40f); Flower(3.10f, -3.25f, 0.85f, 2, 70f);
            Flower(5.30f, -3.30f, 0.90f, 1, 100f); Flower(8.00f, -3.25f, 0.85f, 0, 130f); Flower(11.40f, -3.30f, 0.90f, 2, 160f);
            Flower(13.40f, -3.25f, 0.85f, 1, 190f); Flower(15.60f, -3.30f, 0.90f, 0, 220f);
            Lantern(-2.30f, -3.05f, 0f); Lantern(16.40f, -3.05f, 0f);
        }
    }
}
