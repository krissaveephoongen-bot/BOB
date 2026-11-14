// Battle of the Brooms: the red-white gate that closes each footbridge until the bridges open (docs 24 section 3.1).
// The gate is built in the local space of its GameObject, so BobBridgeBarrier can lower the whole thing 1.5 u in Y
// when the TimedBridgeRaid timer reaches 60 seconds. It sits on BridgeBarrier_Group, which stays active in both modes:
// the gate is the honest signal that nobody can cross yet.
using System.Collections.Generic;
using UnityEngine;

namespace Bob.TaskRace
{
    [ExecuteAlways]
    public class BobBridgeGate : MonoBehaviour
    {
        [Tooltip("Set by the assembler; the gate is built around this transform's local origin.")]
        public float width = 2f;
        public int seed = 1975;

        // Palette cells (16x16 grid, see art/aa/T_BOB_Palette.png).
        static readonly Vector2 Post = Cell(14, 1), Beam = Cell(15, 1), Cap = Cell(3, 0);
        static readonly Vector2 Red = Cell(5, 0), Cream = Cell(0, 0);

        GameObject root;
        Mesh mesh;
        readonly List<Vector3> verts = new List<Vector3>();
        readonly List<Vector3> normals = new List<Vector3>();
        readonly List<Vector2> uvs = new List<Vector2>();
        readonly List<Color> colors = new List<Color>();
        readonly List<int> tris = new List<int>();

        public void Configure(float w)
        {
            width = w;
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
            if (mat == null) { Debug.LogWarning("[BobBridgeGate] no palette material found."); return; }

            DestroySafe(root);
            root = null;
            verts.Clear(); normals.Clear(); uvs.Clear(); colors.Clear(); tris.Clear();

            float halfW = width * 0.5f;
            Vector3 c = Vector3.zero; // local: the parent (BobBridgeBarrier) carries the world position

            // Two square posts just inside the rails, capped like the bridge posts.
            for (int s = -1; s <= 1; s += 2)
            {
                float z = c.z + s * (halfW - 0.1f);
                Box(new Vector3(c.x, 0.5f, z), new Vector3(0.05f, 0.5f, 0.05f), 0f, Post, 1f);
                Box(new Vector3(c.x, 1.03f, z), new Vector3(0.072f, 0.03f, 0.072f), 0f, Cap, 1.02f);
            }

            // Three crossbars between them, red/cream/red so the block reads at play distance.
            var bars = new (float y, Vector2 cell)[]
            {
                (0.34f, Red), (0.64f, Cream), (0.94f, Red),
            };
            foreach (var (y, cell) in bars)
                Box(new Vector3(c.x, y, c.z), new Vector3(0.035f, 0.05f, halfW - 0.08f), 0f, cell, 1f);

            // A thin kick rail at the deck so the gate is not floating.
            Box(new Vector3(c.x, 0.12f, c.z), new Vector3(0.03f, 0.04f, halfW - 0.12f), 0f, Beam, 0.9f);

            mesh = new Mesh { name = "BridgeGate", hideFlags = HideFlags.DontSave };
            mesh.SetVertices(verts);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);
            mesh.SetColors(colors);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateBounds();

            var go = new GameObject(name + " (Gate)", typeof(MeshFilter), typeof(MeshRenderer))
            {
                hideFlags = HideFlags.HideAndDontSave,
            };
            go.transform.SetParent(transform, false);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;
            go.GetComponent<MeshFilter>().sharedMesh = mesh;
            var mr = go.GetComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            mr.receiveShadows = true;
            root = go;
        }

        void Box(Vector3 c, Vector3 half, float yaw, Vector2 cell, float shade)
        {
            var rot = Quaternion.Euler(0f, yaw, 0f);
            var ex = rot * Vector3.right * half.x;
            var ey = Vector3.up * half.y;
            var ez = rot * Vector3.forward * half.z;
            var topC = Shade(shade + 0.1f);
            var sideC = Shade(shade);
            var botC = Shade(shade * 0.72f);

            Face(c - ex - ey - ez, c + ex - ey - ez, c + ex + ey - ez, c - ex + ey - ez, cell, sideC, Vector3.back);
            Face(c - ex - ey + ez, c + ex - ey + ez, c + ex + ey + ez, c - ex + ey + ez, cell, sideC, Vector3.forward);
            Face(c - ex - ey - ez, c - ex - ey + ez, c - ex + ey + ez, c - ex + ey - ez, cell, sideC, Vector3.left);
            Face(c + ex - ey - ez, c + ex - ey + ez, c + ex + ey + ez, c + ex + ey - ez, cell, sideC, Vector3.right);
            Face(c - ex + ey - ez, c + ex + ey - ez, c + ex + ey + ez, c - ex + ey + ez, cell, topC, Vector3.up);
            Face(c - ex - ey - ez, c + ex - ey - ez, c + ex - ey + ez, c - ex - ey + ez, cell, botC, Vector3.down);
        }

        void Face(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector2 cell, Color tint, Vector3 outward)
        {
            var n = Vector3.Cross(b - a, c - a);
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
