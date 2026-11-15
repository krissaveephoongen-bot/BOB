// Battle of the Brooms: the waterfall head of the Task Race river (docs 24 section 3.1). The imported Tripo cliff is
// one mesh that mixes grey rock with a huge glassy water slab, and at play scale it read as a sheet of glass hanging
// over the gorge. This replaces it with layered rock outcrops and an opaque pastel falls: a foam lip, a curved ribbon
// of water with white edge foam, splash shards and mist puffs, and a pool that meets the river.
using System.Collections.Generic;
using UnityEngine;

namespace Bob.TaskRace
{
    [ExecuteAlways]
    public class BobWaterfallDressing : MonoBehaviour
    {
        public Vector3 centre;
        public float halfWidth = 3.2f;
        public float topY = 2.2f;
        public float waterY = -1.35f;
        public float depth = 1.5f;
        public int seed = 1975;

        // Palette cells (16x16 grid, see art/aa/props/T_BOB_Palette.png).
        static readonly Vector2 Stone = Cell(10, 2), Slate = Cell(2, 2), DarkSlate = Cell(3, 2);
        static readonly Vector2 Moss = Cell(8, 2), PaleWater = Cell(7, 1), Water = Cell(11, 2);
        static readonly Vector2 Foam = Cell(4, 0);

        GameObject root;
        Mesh mesh;
        readonly List<Vector3> verts = new List<Vector3>();
        readonly List<Vector3> normals = new List<Vector3>();
        readonly List<Vector2> uvs = new List<Vector2>();
        readonly List<Color> colors = new List<Color>();
        readonly List<int> tris = new List<int>();

        public void Configure(Vector3 centreWorld, float channel, float top)
        {
            centre = centreWorld;
            // 4.3: the headwall came in a step smaller and the falls a shade paler, so the waterfall frames the river
            // instead of competing with the task signs for attention on a phone screen.
            halfWidth = Mathf.Max(1.5f, channel * 0.4f + 0.45f);
            topY = Mathf.Max(1.4f, top - 0.2f);
            waterY = -1.33f;
            Build();
        }

        void OnEnable() => Build();

        void OnDisable()
        {
            DestroySafe(root);
            DestroySafe(mesh);
            root = null; mesh = null;
        }

        public void Build()
        {
            var mat = Resources.Load<Material>("Materials/M_BOB_Dressing")
                      ?? Resources.Load<Material>("Materials/M_BOB_CurvedMap");
            if (mat == null) { Debug.LogWarning("[BobWaterfallDressing] no palette material found."); return; }

            DestroySafe(root);
            root = null;
            verts.Clear(); normals.Clear(); uvs.Clear(); colors.Clear(); tris.Clear();

            var rnd = new System.Random(seed);
            float z = centre.z;
            float hw = halfWidth;

            // Rock headwall: three staggered rows of slim tapered prisms, plus a skirt of rock hanging under the lip.
            // Slate only — the first pass used Stone accents and a wide pale shelf, and from the play camera those read
            // as cream pillars and a concrete block.
            int columns = Mathf.Max(9, Mathf.RoundToInt(hw * 3.6f));
            for (int row = 0; row < 4; row++)
            {
                bool skirt = row == 3;
                float rowZ = z + (skirt ? -0.1f : row == 0 ? -0.05f : row == 1 ? 0.75f : 1.5f);
                int n = Mathf.Max(6, columns - row * 3);
                for (int i = 0; i < n; i++)
                {
                    float u = (i + 0.5f) / n;
                    float x = centre.x + Mathf.Lerp(-hw, hw, u) + ((float)rnd.NextDouble() - 0.5f) * 0.3f;
                    float r = (hw * 2f / n) * (0.46f + (float)rnd.NextDouble() * 0.16f);
                    float baseH = skirt ? 1.8f : row == 0 ? 1.3f : row == 1 ? 1.9f : 1.7f;
                    float h = baseH * (0.85f + (float)rnd.NextDouble() * 0.35f);
                    float y0 = skirt ? -2.1f : row == 0 ? -0.2f : row == 1 ? 0.45f : 1f;
                    var cell = rnd.NextDouble() < 0.4 ? DarkSlate : Slate;
                    // Wet rock in shade: Slate at full brightness turns cream under the warm key light at 10 u.
                    float shade = 0.68f + (float)rnd.NextDouble() * 0.16f;
                    var c = new Vector3(x, y0, rowZ);
                    Rock(c, r, r * 1.15f, h * 0.5f, (float)rnd.NextDouble() * 360f, cell, shade);
                    if (!skirt && rnd.NextDouble() < 0.6)
                        Rock(new Vector3(x + (float)(rnd.NextDouble() - 0.5) * 0.3f, c.y + h * 0.47f, rowZ),
                             r * 0.72f, r * 0.8f, 0.08f, (float)rnd.NextDouble() * 360f, Moss, 0.9f);
                }
            }

            // A narrow slate shelf so the water has a lip to pour over.
            var lipY = topY;
            Box(new Vector3(centre.x, lipY + 0.04f, z + 0.3f),
                new Vector3(Mathf.Min(hw * 0.55f, 1.35f), 0.09f, depth * 0.4f), Slate, 0.78f);

            // Falls: three interleaved ribbons, centred just in front of the wall, leaning out as they drop.
            float fallZ = z - depth * 0.45f;
            Ribbon(centre.x - 0.18f, centre.x + 0.18f, fallZ, lipY, waterY, 10, Water, 0.98f);
            Ribbon(centre.x - 0.62f, centre.x - 0.16f, fallZ - 0.06f, lipY - 0.08f, waterY + 0.08f, 10, PaleWater, 0.96f);
            Ribbon(centre.x + 0.16f, centre.x + 0.62f, fallZ - 0.06f, lipY - 0.08f, waterY + 0.08f, 10, PaleWater, 0.96f);
            Ribbon(centre.x - 0.74f, centre.x - 0.58f, fallZ + 0.04f, lipY - 0.04f, waterY + 0.3f, 8, Foam, 1.08f);
            Ribbon(centre.x + 0.58f, centre.x + 0.74f, fallZ + 0.04f, lipY - 0.04f, waterY + 0.3f, 8, Foam, 1.08f);

            // Foam at the lip: a bright crest and two small side spills.
            Box(new Vector3(centre.x, lipY - 0.02f, fallZ + 0.02f), new Vector3(0.62f, 0.06f, 0.16f), Foam, 1.1f);
            Box(new Vector3(centre.x, lipY - 0.04f, fallZ - 0.1f), new Vector3(0.3f, 0.05f, 0.12f), Foam, 1.05f);

            // Splash: a pool disc, a foam ring, shards and three mist puffs.
            float poolZ = fallZ - 0.55f;
            Disc(new Vector3(centre.x, waterY + 0.02f, poolZ), 1.75f, 12, Water, 1f, (float)rnd.NextDouble() * 360f);
            Disc(new Vector3(centre.x, waterY + 0.03f, poolZ + 0.2f), 1.1f, 10, Water, 1.06f, 0f);
            for (int i = 0; i < 9; i++)
            {
                float a = (float)rnd.NextDouble() * 360f;
                float r = 0.5f + (float)rnd.NextDouble() * 1.1f;
                var p = new Vector3(centre.x + Mathf.Cos(a * Mathf.Deg2Rad) * r, waterY + 0.12f,
                                    poolZ + Mathf.Sin(a * Mathf.Deg2Rad) * r * 0.7f);
                Shard(p, 0.1f + (float)rnd.NextDouble() * 0.16f, 0.18f + (float)rnd.NextDouble() * 0.3f,
                      a, Foam, 1.05f);
            }
            for (int i = 0; i < 4; i++)
            {
                var c = new Vector3(centre.x + ((float)rnd.NextDouble() - 0.5f) * 1.7f, waterY + 0.35f,
                                    poolZ - 0.2f + ((float)rnd.NextDouble() - 0.5f) * 0.6f);
                float r = 0.16f + (float)rnd.NextDouble() * 0.14f;
                Rock(c, r, r * 0.9f, r * 0.8f, (float)rnd.NextDouble() * 360f, Foam, 1.12f);
            }

            // Wet boulders flanking the plunge pool, so the falls end in rock and not on a drawn line.
            for (int i = 0; i < 4; i++)
            {
                float side = i % 2 == 0 ? -1f : 1f;
                float r = 0.26f + (float)rnd.NextDouble() * 0.2f;
                var c = new Vector3(centre.x + side * (0.95f + (float)rnd.NextDouble() * 0.5f),
                                    waterY + r * 0.2f,
                                    poolZ + 0.15f + ((float)rnd.NextDouble() - 0.5f) * 0.5f);
                Rock(c, r, r * 1.1f, r * 0.85f, (float)rnd.NextDouble() * 360f,
                     i % 3 == 0 ? DarkSlate : Slate, 0.84f + (float)rnd.NextDouble() * 0.1f);
            }

            mesh = new Mesh { name = "WaterfallDressing", hideFlags = HideFlags.DontSave };
            mesh.SetVertices(verts);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);
            mesh.SetColors(colors);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateBounds();

            var go = new GameObject(name + " (Waterfall)", typeof(MeshFilter), typeof(MeshRenderer))
            {
                hideFlags = HideFlags.HideAndDontSave,
            };
            go.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            go.GetComponent<MeshFilter>().sharedMesh = mesh;
            var mr = go.GetComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            mr.receiveShadows = true;
            root = go;
        }

        /// <summary>A tapered six-sided rock with a shaded lower band and a lit cap.</summary>
        void Rock(Vector3 c, float rTop, float rBottom, float halfH, float yaw, Vector2 cell, float shade)
        {
            int sides = 6;
            var bottom = new Vector3[sides];
            var mid = new Vector3[sides];
            var top = new Vector3[sides];
            for (int i = 0; i < sides; i++)
            {
                float a = (yaw + i * 360f / sides) * Mathf.Deg2Rad;
                var dir = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                bottom[i] = c + dir * rBottom + Vector3.down * halfH;
                mid[i] = c + dir * Mathf.Lerp(rBottom, rTop, 0.45f) + Vector3.up * (halfH * 0.1f);
                top[i] = c + dir * rTop + Vector3.up * halfH;
            }
            for (int i = 0; i < sides; i++)
            {
                int j = (i + 1) % sides;
                var outward = ((bottom[i] + bottom[j]) * 0.5f - c);
                outward.y = 0f;
                Face(bottom[i], bottom[j], mid[j], mid[i], cell, Shade(shade * 0.82f), outward);
                Face(mid[i], mid[j], top[j], top[i], cell, Shade(shade), outward);
                Face(top[i], top[j], c + Vector3.up * halfH, c + Vector3.up * halfH, cell, Shade(shade + 0.1f), Vector3.up);
            }
        }

        /// <summary>A vertical water sheet, curving outward as it falls; `z` is the top of the fall.</summary>
        void Ribbon(float x0, float x1, float zTop, float yTop, float yBottom, int segments, Vector2 cell, float shade)
        {
            float cx = centre.x;
            for (int i = 0; i < segments; i++)
            {
                float t0 = i / (float)segments, t1 = (i + 1) / (float)segments;
                float y0 = Mathf.Lerp(yTop, yBottom, t0), y1 = Mathf.Lerp(yTop, yBottom, t1);
                float z0 = zTop - 0.55f * t0 * t0, z1 = zTop - 0.55f * t1 * t1;
                float w0 = Mathf.Lerp(1f, 1.25f, t0), w1 = Mathf.Lerp(1f, 1.25f, t1);
                float s = shade * Mathf.Lerp(1.05f, 0.92f, t0);
                Face(new Vector3(cx + (x0 - cx) * w0, y0, z0), new Vector3(cx + (x1 - cx) * w1, y0, z0),
                     new Vector3(cx + (x1 - cx) * w1, y1, z1), new Vector3(cx + (x0 - cx) * w0, y1, z1),
                     cell, Shade(s), Vector3.back);
            }
        }

        /// <summary>A flat irregular disc on the water plane, facing up.</summary>
        void Disc(Vector3 c, float radius, int sides, Vector2 cell, float shade, float yaw)
        {
            var tint = Shade(shade);
            for (int i = 0; i < sides; i++)
            {
                float a0 = (yaw + i * 360f / sides) * Mathf.Deg2Rad;
                float a1 = (yaw + (i + 1) * 360f / sides) * Mathf.Deg2Rad;
                var p0 = c + new Vector3(Mathf.Cos(a0), 0f, Mathf.Sin(a0)) * radius;
                var p1 = c + new Vector3(Mathf.Cos(a1), 0f, Mathf.Sin(a1)) * radius;
                Face(c, p0, p1, p1, cell, tint, Vector3.up);
            }
        }

        /// <summary>A small upright splash triangle.</summary>
        void Shard(Vector3 p, float w, float h, float yaw, Vector2 cell, float shade)
        {
            var dir = Quaternion.Euler(0f, yaw, 0f) * Vector3.forward;
            var side = Quaternion.Euler(0f, yaw + 90f, 0f) * Vector3.forward * w;
            Face(p - side, p + side, p + dir * (w * 0.3f) + Vector3.up * h, p + dir * (w * 0.3f) + Vector3.up * h,
                 cell, Shade(shade), Vector3.up);
        }

        void Box(Vector3 c, Vector3 half, Vector2 cell, float shade)
        {
            var ex = Vector3.right * half.x;
            var ey = Vector3.up * half.y;
            var ez = Vector3.forward * half.z;
            var topC = Shade(shade + 0.1f);
            var sideC = Shade(shade);
            Face(c - ex - ey - ez, c + ex - ey - ez, c + ex + ey - ez, c - ex + ey - ez, cell, sideC, Vector3.back);
            Face(c - ex - ey + ez, c + ex - ey + ez, c + ex + ey + ez, c - ex + ey + ez, cell, sideC, Vector3.forward);
            Face(c - ex - ey - ez, c - ex - ey + ez, c - ex + ey + ez, c - ex + ey - ez, cell, sideC, Vector3.left);
            Face(c + ex - ey - ez, c + ex - ey + ez, c + ex + ey + ez, c + ex + ey - ez, cell, sideC, Vector3.right);
            Face(c - ex + ey - ez, c + ex + ey - ez, c + ex + ey + ez, c - ex + ey + ez, cell, topC, Vector3.up);
        }

        /// <summary>Adds one quad, flipping the winding so its normal always points the way it is seen from.</summary>
        void Face(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector2 cell, Color tint, Vector3 outward)
        {
            var n = Vector3.Cross(b - a, c - a);
            if (n.sqrMagnitude < 1e-8f) n = Vector3.Cross(c - a, d - a);
            if (Vector3.Dot(n, outward) < 0f) { var t = b; b = d; d = t; n = -n; }
            n.Normalize();
            int i = verts.Count;
            verts.Add(a); verts.Add(b); verts.Add(c); verts.Add(d);
            for (int k = 0; k < 4; k++) { normals.Add(n); uvs.Add(cell); colors.Add(tint); }
            tris.Add(i); tris.Add(i + 1); tris.Add(i + 2);
            tris.Add(i); tris.Add(i + 2); tris.Add(i + 3);
        }

        static Color Shade(float v) => new Color(v, v, v, 1f);
        static Vector2 Cell(int x, int y) => new Vector2((x + 0.5f) / 16f, 1f - (y + 0.5f) / 16f);

        static void DestroySafe(Object o)
        {
            if (o == null) return;
            if (Application.isPlaying) Object.Destroy(o); else Object.DestroyImmediate(o);
        }
    }
}
