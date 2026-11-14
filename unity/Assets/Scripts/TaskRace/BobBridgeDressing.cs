// Battle of the Brooms: the footbridge over the Task Race river gorge (docs 24 section 3.1). The imported Tripo
// bridge reads as two flat cream planks from the play camera, so this builds the deck in its place: planks with
// alternating grain, side stringers, railing posts with a top and mid rail, and piers that drop into the gorge.
// The deck top sits exactly on the walk plane (y = 0) so the collider the assembler laid on the same box matches.
using System.Collections.Generic;
using UnityEngine;

namespace Bob.TaskRace
{
    [ExecuteAlways]
    public class BobBridgeDressing : MonoBehaviour
    {
        public Vector3 centre;
        public float length = 20f;
        public float width = 1.9f;
        [Tooltip("How far the piers drop under the deck, in units.")]
        public float drop = 2.4f;
        [Tooltip("Piers stand just outside this half-width (the water channel).")]
        public float waterHalfWidth = 2.1f;
        public int seed = 1975;

        // Palette cells (16x16 grid, see art/aa/props/T_BOB_Palette.png).
        static readonly Vector2 Plank = Cell(0, 1), PlankWarm = Cell(10, 1);
        static readonly Vector2 Beam = Cell(15, 1), Post = Cell(14, 1), Cap = Cell(3, 0);

        GameObject root;
        Mesh mesh;
        readonly List<Vector3> verts = new List<Vector3>();
        readonly List<Vector3> normals = new List<Vector3>();
        readonly List<Vector2> uvs = new List<Vector2>();
        readonly List<Color> colors = new List<Color>();
        readonly List<int> tris = new List<int>();

        public void Configure(Vector3 centreWorld, float len, float w, float channel)
        {
            centre = centreWorld;
            length = len;
            width = w;
            waterHalfWidth = Mathf.Max(1.2f, channel * 0.5f + 0.15f);
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
            if (mat == null) { Debug.LogWarning("[BobBridgeDressing] no palette material found."); return; }

            DestroySafe(root);
            root = null;
            verts.Clear(); normals.Clear(); uvs.Clear(); colors.Clear(); tris.Clear();

            var rnd = new System.Random(seed);
            float halfL = length * 0.5f, halfW = width * 0.5f;
            float top = centre.y;
            // Deck top a hair over the grass plane: coplanar would z-fight with the Field slab the bridge crosses.
            float deck = top + 0.025f;

            // Deck: staggered planks with a hair of yaw so the walkway is not a printed stripe.
            for (float x = -halfL + 0.14f; x < halfL - 0.1f; x += 0.34f)
            {
                float jitter = ((float)rnd.NextDouble() - 0.5f) * 3f;
                float shade = 0.94f + (float)rnd.NextDouble() * 0.14f;
                var cell = rnd.NextDouble() < 0.3 ? PlankWarm : Plank;
                Box(new Vector3(centre.x + x, deck - 0.035f, centre.z),
                    new Vector3(0.16f, 0.035f, halfW - 0.05f), jitter, cell, shade);
            }

            // Stringers under the deck edges, then the railing posts and rails.
            for (int s = -1; s <= 1; s += 2)
            {
                float z = s * (halfW - 0.12f);
                Box(new Vector3(centre.x, deck - 0.16f, centre.z + z),
                    new Vector3(halfL, 0.075f, 0.1f), 0f, Beam, 0.9f);

                for (float x = -halfL + 0.3f; x <= halfL - 0.25f; x += 1.55f)
                {
                    Box(new Vector3(centre.x + x, top + 0.4f, centre.z + z),
                        new Vector3(0.05f, 0.4f, 0.05f), 0f, Post, 1f);
                    Box(new Vector3(centre.x + x, top + 0.82f, centre.z + z),
                        new Vector3(0.072f, 0.03f, 0.072f), 0f, Cap, 1.02f);
                }
                Box(new Vector3(centre.x, top + 0.84f, centre.z + z),
                    new Vector3(halfL - 0.12f, 0.035f, 0.045f), 0f, Beam, 1f);
                Box(new Vector3(centre.x, top + 0.46f, centre.z + z),
                    new Vector3(halfL - 0.12f, 0.024f, 0.032f), 0f, Beam, 0.95f);
            }

            // Piers in the gorge, with a cross beam under the stringers.
            for (int s = -1; s <= 1; s += 2)
            {
                float px = s * waterHalfWidth;
                for (int zs = -1; zs <= 1; zs += 2)
                    Box(new Vector3(centre.x + px, top - drop * 0.5f, centre.z + zs * (halfW - 0.2f)),
                        new Vector3(0.07f, drop * 0.5f, 0.07f), 0f, Post, 0.86f);
                Box(new Vector3(centre.x + px, top - 0.24f, centre.z),
                    new Vector3(0.1f, 0.055f, halfW), 0f, Beam, 0.9f);
            }

            mesh = new Mesh { name = "BridgeDressing", hideFlags = HideFlags.DontSave };
            mesh.SetVertices(verts);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);
            mesh.SetColors(colors);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateBounds();

            // Parent the mesh to this component instead of the scene root: the deck then lives and dies with its group,
            // so toggling the modes never leaves a stale copy behind.
            var go = new GameObject(name + " (Bridge)", typeof(MeshFilter), typeof(MeshRenderer))
            {
                hideFlags = HideFlags.HideAndDontSave,
            };
            go.transform.SetParent(transform, false);
            go.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
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

        /// <summary>Adds one face, flipping the winding so its normal always points the way it is seen from.</summary>
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
