// Battle of the Brooms: what makes the Task Race field read as a place instead of a green plate — mown patches, clumps
// of short grass, daisies and a few stones scattered over each island (docs 24 section 3.1). Generated once per island
// into a single mesh with palette-cell UVs (one draw call, one material) and seeded, so a rebuilt scene looks the same.
// The material is BOB/Dressing, which renders two-sided with no outline pass: thin blades were picking up the dark
// outline colour on their back faces and reading as brown thorns. Stations keep a clear ring of 1.7 u and the bridge
// decks are masked out so no blade pokes through a walkway.
using System.Collections.Generic;
using UnityEngine;

namespace Bob.TaskRace
{
    [ExecuteAlways]
    public class BobFieldDressing : MonoBehaviour
    {
        public Vector3 centre;
        public float halfX = 5f, halfZ = 4f;
        public int tufts = 850, flowers = 90, rocks = 16, patches = 12;
        public int seed = 1975;
        [Tooltip("No dressing within this radius of a task station.")]
        public float keepClear = 1.7f;

        // Palette cells (16x16 grid, see art/aa/props/T_BOB_Palette.png).
        static readonly Vector2 Grass = Cell(8, 2), GrassDark = Cell(9, 2), Stone = Cell(10, 2);
        static readonly Vector2 White = Cell(4, 0), Gold = Cell(5, 2);

        GameObject root;
        Mesh mesh;

        /// <summary>The one renderer of this field. DioramaAmbient turns its wind on through a property block.</summary>
        public MeshRenderer Renderer => root != null ? root.GetComponent<MeshRenderer>() : null;
        public static readonly int WindId = Shader.PropertyToID("_Wind");

        readonly List<Vector3> verts = new List<Vector3>();
        readonly List<Vector3> normals = new List<Vector3>();
        readonly List<Vector2> uvs = new List<Vector2>();
        readonly List<Color> colors = new List<Color>();
        readonly List<int> tris = new List<int>();
        readonly List<Vector4> keepOut = new List<Vector4>();

        void OnEnable() => Build();

        void OnDisable()
        {
            DestroySafe(root);
            DestroySafe(mesh);
            root = null; mesh = null;
        }

        public void Build()
        {
            var mat = Resources.Load<Material>("Materials/M_BOB_Dressing");
            if (mat == null) { Debug.LogWarning("[BobFieldDressing] Resources/Materials/M_BOB_Dressing is missing."); return; }

            DestroySafe(root);
            root = null;
            verts.Clear(); normals.Clear(); uvs.Clear(); colors.Clear(); tris.Clear();

            var spots = new List<Vector3>();
            foreach (var s in Object.FindObjectsByType<TaskSpot>(FindObjectsInactive.Exclude))
                spots.Add(new Vector3(s.transform.position.x, 0f, s.transform.position.z));

            CollectBridgeKeepOuts();

            var rnd = new System.Random(seed);
            var taken = new List<Vector3>();

            // Darker mown patches first, flat on the field, so the lawn has large shapes under the fine grass.
            for (int i = 0; i < patches; i++)
            {
                if (!Sample(rnd, 0.96f, spots, taken, 1.1f, out var p)) continue;
                AddPatch(rnd, p);
                taken.Add(p);
            }
            for (int i = 0; i < rocks; i++)
            {
                if (!Sample(rnd, 0.92f, spots, taken, 0.85f, out var p)) continue;
                AddRock(rnd, p);
                taken.Add(p);
            }
            for (int i = 0; i < flowers; i++)
            {
                if (!Sample(rnd, 1f, spots, taken, 0.5f, out var p)) continue;
                AddFlower(rnd, p);
                taken.Add(p);
            }
            for (int i = 0; i < tufts; i++)
            {
                if (!Sample(rnd, 1f, spots, taken, 0.2f, out var p)) continue;
                AddTuft(rnd, p);
                taken.Add(p);
            }

            mesh = new Mesh { name = "FieldDressing", hideFlags = HideFlags.DontSave };
            mesh.SetVertices(verts);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);
            mesh.SetColors(colors);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateBounds();

            var go = new GameObject(name + " (Dressing)", typeof(MeshFilter), typeof(MeshRenderer))
            {
                hideFlags = HideFlags.HideAndDontSave,
            };
            go.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            go.GetComponent<MeshFilter>().sharedMesh = mesh;
            var mr = go.GetComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = true;
            root = go;
        }

        void CollectBridgeKeepOuts()
        {
            keepOut.Clear();
            var map = transform.parent;
            var bridges = map != null ? map.Find("Bridges_Group") : null;
            if (bridges == null) return;
            foreach (var c in bridges.GetComponentsInChildren<Collider>(true))
            {
                var b = c.bounds;
                keepOut.Add(new Vector4(b.min.x - 0.25f, b.min.z - 0.25f, b.max.x + 0.25f, b.max.z + 0.25f));
            }
        }

        bool Sample(System.Random rnd, float fill, List<Vector3> spots, List<Vector3> taken, float spacing, out Vector3 p)
        {
            for (int attempt = 0; attempt < 24; attempt++)
            {
                float x = (float)(rnd.NextDouble() * 2 - 1) * halfX;
                float z = (float)(rnd.NextDouble() * 2 - 1) * halfZ;
                if ((x / halfX) * (x / halfX) + (z / halfZ) * (z / halfZ) > fill * fill) continue;
                var c = new Vector3(centre.x + x, 0f, centre.z + z);
                bool blocked = false;
                foreach (var s in spots)
                    if (new Vector2(s.x - c.x, s.z - c.z).sqrMagnitude < keepClear * keepClear) { blocked = true; break; }
                if (blocked) continue;
                foreach (var r in keepOut)
                    if (c.x > r.x && c.x < r.z && c.z > r.y && c.z < r.w) { blocked = true; break; }
                if (blocked) continue;
                foreach (var t in taken)
                    if (new Vector2(t.x - c.x, t.z - c.z).sqrMagnitude < spacing * spacing) { blocked = true; break; }
                if (blocked) continue;
                p = c;
                return true;
            }
            p = Vector3.zero;
            return false;
        }

        void AddPatch(System.Random rnd, Vector3 p)
        {
            int sides = 6 + rnd.Next(3);
            float radius = 0.45f + (float)rnd.NextDouble() * 0.7f;
            // A mown tone, not a shadow: the first pass shaded 0.86 of the dark grass cell and the polygons read as
            // camouflage blotches. Keeping the shade at or above 1 makes them gentle mower stripes.
            float shade = 0.97f + (float)rnd.NextDouble() * 0.11f;
            // Alpha 0: the shader reads vertex alpha as wind weight, and a mown patch is painted on the lawn.
            var tint = new Color(shade, shade, shade, 0f);
            var c = p + Vector3.up * 0.012f;
            var ring = new Vector3[sides];
            for (int i = 0; i < sides; i++)
            {
                float a = i / (float)sides * Mathf.PI * 2f;
                float r = radius * (0.72f + (float)rnd.NextDouble() * 0.4f);
                ring[i] = c + new Vector3(Mathf.Cos(a) * r, 0f, Mathf.Sin(a) * r);
            }
            for (int i = 0; i < sides; i++)
            {
                var a = ring[i];
                var b = ring[(i + 1) % sides];
                int v = verts.Count;
                verts.Add(c); verts.Add(a); verts.Add(b);
                for (int k = 0; k < 3; k++) { normals.Add(Vector3.up); uvs.Add(GrassDark); }
                colors.Add(tint); colors.Add(tint); colors.Add(tint);
                tris.Add(v); tris.Add(v + 1); tris.Add(v + 2);
            }
        }

        void AddTuft(System.Random rnd, Vector3 p)
        {
            int blades = 6 + rnd.Next(4);
            for (int i = 0; i < blades; i++)
            {
                float yaw = (float)(rnd.NextDouble() * 360.0);
                float h = 0.08f + (float)rnd.NextDouble() * 0.11f;
                float w = 0.016f + (float)rnd.NextDouble() * 0.024f;
                float lean = 0.015f + (float)rnd.NextDouble() * 0.07f;
                var dir = Quaternion.Euler(0f, yaw, 0f) * Vector3.forward;
                var side = Quaternion.Euler(0f, yaw + 90f, 0f) * Vector3.forward;
                var b = p + new Vector3((float)(rnd.NextDouble() - 0.5) * 0.12f, 0f, (float)(rnd.NextDouble() - 0.5) * 0.12f);
                float baseShade = 0.74f + (float)rnd.NextDouble() * 0.14f;
                float tipShade = 1.08f + (float)rnd.NextDouble() * 0.16f;
                Blade(b, dir, side * w, h, lean, rnd.NextDouble() < 0.35 ? GrassDark : Grass, baseShade, tipShade);
            }
        }

        /// <summary>One tapered triangle. The dressing shader is two-sided, so this reads from both camera sides.</summary>
        void Blade(Vector3 p, Vector3 dir, Vector3 side, float h, float lean, Vector2 cell, float baseShade, float tipShade)
        {
            var tip = p + dir * lean + Vector3.up * h;
            var n = (Vector3.up * 0.8f - dir * 0.2f).normalized;
            int i = verts.Count;
            verts.Add(p - side); verts.Add(p + side); verts.Add(tip);
            for (int k = 0; k < 3; k++) { normals.Add(n); uvs.Add(cell); }
            // Alpha is the wind weight: roots pinned (0), tips free (1), so a gust bends the blade instead of sliding it.
            colors.Add(new Color(baseShade, baseShade, baseShade, 0f));
            colors.Add(new Color(baseShade, baseShade, baseShade, 0f));
            colors.Add(new Color(tipShade, tipShade, tipShade, 1f));
            tris.Add(i); tris.Add(i + 1); tris.Add(i + 2);
        }

        void AddFlower(System.Random rnd, Vector3 p)
        {
            float yaw = (float)(rnd.NextDouble() * 360.0);
            float h = 0.13f + (float)rnd.NextDouble() * 0.08f;
            var dir = Quaternion.Euler(0f, yaw, 0f) * Vector3.forward;
            var side = Quaternion.Euler(0f, yaw + 90f, 0f) * Vector3.forward;
            Blade(p, dir, side * 0.008f, h, 0.01f, GrassDark, 0.62f, 0.95f);

            var head = p + dir * 0.01f + Vector3.up * (h * 0.97f);
            float r = 0.035f + (float)rnd.NextDouble() * 0.02f;
            // Three crossed petal fans + a gold eye: at play distance this reads as a white daisy from any yaw.
            for (int k = 0; k < 3; k++)
            {
                var a = Quaternion.Euler(0f, yaw + k * 60f, 0f) * Vector3.forward;
                var left = head - a * r;
                var right = head + a * r;
                var up = head + Vector3.up * (r * 0.75f);
                int v = verts.Count;
                verts.Add(left); verts.Add(right); verts.Add(up);
                for (int m = 0; m < 3; m++) { normals.Add(Vector3.up); uvs.Add(White); }
                colors.Add(Color.white); colors.Add(Color.white); colors.Add(Color.white);
                tris.Add(v); tris.Add(v + 1); tris.Add(v + 2);
            }
            var eye = head + Vector3.up * 0.012f;
            FlatDisc(eye, r * 0.44f, side, dir, Gold, Color.white);
        }

        void AddRock(System.Random rnd, Vector3 p)
        {
            float yaw = (float)(rnd.NextDouble() * 360.0);
            float w = 0.16f + (float)rnd.NextDouble() * 0.24f;
            float h = w * (0.45f + (float)rnd.NextDouble() * 0.35f);
            var rot = Quaternion.Euler(0f, yaw, 0f);
            // A six-face lump sunk a third into the grass; no collider, so the hero walks over it.
            var c = p + Vector3.up * (h * 0.3f);
            var ex = rot * Vector3.right * (w * 0.5f);
            var ez = rot * Vector3.forward * (w * 0.38f);
            var ey = Vector3.up * (h * 0.5f);
            float shade = 1f + (float)rnd.NextDouble() * 0.12f;
            var tint = new Color(shade, shade, shade, 0f);
            var bottom = new[] { c - ex - ey - ez, c + ex - ey - ez, c + ex - ey + ez, c - ex - ey + ez };
            var top = new[] { c - ex * 0.8f + ey - ez * 0.8f, c + ex * 0.8f + ey - ez * 0.8f,
                              c + ex * 0.8f + ey + ez * 0.8f, c - ex * 0.8f + ey + ez * 0.8f };
            Quad(top[0], top[1], top[2], top[3], Stone, tint, tint);
            for (int i = 0; i < 4; i++)
            {
                int j = (i + 1) % 4;
                Quad(bottom[i], bottom[j], top[j], top[i], Stone, tint, tint);
            }
        }

        /// <summary>A flat quad facing up, for the gold eye of a daisy.</summary>
        void FlatDisc(Vector3 c, float radius, Vector3 a, Vector3 b, Vector2 cell, Color tint)
        {
            var p0 = c - a * radius - b * radius;
            var p1 = c + a * radius - b * radius;
            var p2 = c + a * radius + b * radius;
            var p3 = c - a * radius + b * radius;
            int i = verts.Count;
            verts.Add(p0); verts.Add(p1); verts.Add(p2); verts.Add(p3);
            for (int k = 0; k < 4; k++) { normals.Add(Vector3.up); uvs.Add(cell); colors.Add(tint); }
            tris.Add(i); tris.Add(i + 1); tris.Add(i + 2);
            tris.Add(i); tris.Add(i + 2); tris.Add(i + 3);
        }

        void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector2 cell, Color bottomShade, Color topShade)
        {
            int i = verts.Count;
            var n = Vector3.Cross(b - a, c - a).normalized;
            verts.Add(a); verts.Add(b); verts.Add(c); verts.Add(d);
            for (int k = 0; k < 4; k++) { normals.Add(n); uvs.Add(cell); }
            colors.Add(bottomShade); colors.Add(bottomShade); colors.Add(topShade); colors.Add(topShade);
            tris.Add(i); tris.Add(i + 2); tris.Add(i + 1);
            tris.Add(i); tris.Add(i + 3); tris.Add(i + 2);
        }

        static Vector2 Cell(int x, int y) => new Vector2((x + 0.5f) / 16f, 1f - (y + 0.5f) / 16f);

        static void DestroySafe(Object o)
        {
            if (o == null) return;
            if (Application.isPlaying) Object.Destroy(o); else Object.DestroyImmediate(o);
        }
    }
}
