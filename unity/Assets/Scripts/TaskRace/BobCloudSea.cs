// Battle of the Brooms: the pastel sea of cloud under a Task Race map (docs 24). Builds one curved sheet below the
// play plane with a written cloud texture; lives on the map's Systems object so every mode scene carries it and the
// verify cameras see it. Shadows are off and the mesh is hidden from the hierarchy.
using UnityEngine;

namespace Bob.TaskRace
{
    [ExecuteAlways]
    public class BobCloudSea : MonoBehaviour
    {
        [Tooltip("Height of the cloud sheet below the play plane, in units.")]
        public float seaLevel = -5.5f;
        [Tooltip("Half-width of the sheet. Wide enough that its rim sits past the fog line in the wide shot.")]
        public float radius = 42f;
        public Color top = new Color(1f, 0.985f, 0.97f);
        public Color belly = new Color(0.80f, 0.85f, 0.97f);
        [Range(0f, 1f)] public float opacity = 0.9f;

        const int Cells = 20, Tex = 512;
        GameObject sheet;
        Mesh mesh;
        Texture2D texture;
        Material material;

        void OnEnable() => Ensure();

        void Ensure()
        {
            if (sheet != null) return;
            if (texture == null)
            {
                texture = BuildTexture();
                texture.hideFlags = HideFlags.DontSave;
            }

            mesh = GridMesh();
            var go = new GameObject(name + " (CloudSea)", typeof(MeshFilter), typeof(MeshRenderer))
            {
                hideFlags = HideFlags.HideAndDontSave,
            };
            go.transform.SetPositionAndRotation(transform.position + Vector3.up * seaLevel, Quaternion.identity);
            go.transform.localScale = new Vector3(radius * 2f, 1f, radius * 2f);
            go.GetComponent<MeshFilter>().sharedMesh = mesh;

            var shader = Resources.Load<Shader>("Shaders/BobCloudSea") ?? Shader.Find("BOB/CloudSea");
            if (shader != null)
            {
                material = new Material(shader) { mainTexture = texture, hideFlags = HideFlags.DontSave };
                material.SetColor("_Color", Color.white);
                material.SetFloat("_Opacity", opacity);
            }
            var mr = go.GetComponent<MeshRenderer>();
            if (material != null) mr.sharedMaterial = material;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
            sheet = go;
        }

        void OnDisable()
        {
            DestroySafe(sheet);
            DestroySafe(mesh);
            DestroySafe(texture);
            DestroySafe(material);
            sheet = null; mesh = null; texture = null; material = null;
        }

        static void DestroySafe(Object o)
        {
            if (o == null) return;
            if (Application.isPlaying) Destroy(o); else DestroyImmediate(o);
        }

        static Mesh GridMesh()
        {
            int n = Cells + 1;
            var m = new Mesh { name = "CloudSea", hideFlags = HideFlags.DontSave };
            var verts = new Vector3[n * n];
            var uv = new Vector2[n * n];
            for (int z = 0; z < n; z++)
            for (int x = 0; x < n; x++)
            {
                float fx = x / (float)Cells - 0.5f, fz = z / (float)Cells - 0.5f;
                verts[z * n + x] = new Vector3(fx, 0f, fz);
                uv[z * n + x] = new Vector2(x / (float)Cells, z / (float)Cells);
            }
            var tris = new int[Cells * Cells * 6];
            int t = 0;
            for (int z = 0; z < Cells; z++)
            for (int x = 0; x < Cells; x++)
            {
                int i0 = z * n + x, i1 = i0 + 1, i2 = i0 + n, i3 = i2 + 1;
                tris[t++] = i0; tris[t++] = i2; tris[t++] = i1;
                tris[t++] = i1; tris[t++] = i2; tris[t++] = i3;
            }
            m.vertices = verts;
            m.uv = uv;
            m.triangles = tris;
            m.RecalculateNormals();
            m.bounds = new Bounds(Vector3.zero, new Vector3(2f, 1f, 2f));
            return m;
        }

        Texture2D BuildTexture()
        {
            var tex = new Texture2D(Tex, Tex, TextureFormat.RGBA32, false)
            {
                name = "T_BOB_CloudSea",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
            };
            // Three puff layers: large cumulus shapes carry the read, the finer layers only break up their edges.
            var coarse = Puffs(7, 9, 0.34f);
            var mid = Puffs(19, 18, 0.13f);
            var fine = Puffs(41, 40, 0.055f);
            var px = new Color[Tex * Tex];
            for (int y = 0; y < Tex; y++)
            for (int x = 0; x < Tex; x++)
            {
                float u = x / (Tex - 1f), v = y / (Tex - 1f);
                float n = Density(u, v, coarse, mid, fine);
                // Fake a light from the upper left: compare the density just toward it, so each mound gets a bright
                // crest and a soft blue belly instead of one flat white sheet.
                float lit = Density(u - 0.025f, v + 0.02f, coarse, mid, fine);
                float relief = Mathf.Clamp01((n - lit) * 2.6f + 0.5f);
                var c = Color.Lerp(belly, top, Mathf.Clamp01(0.32f + n * 0.5f + relief * 0.32f));
                // Rim fade: the sheet dissolves into the sky instead of ending on a straight edge.
                float r = Mathf.Clamp01(new Vector2(u - 0.5f, v - 0.5f).magnitude * 2f);
                float a = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((1f - r) / 0.25f)) * Mathf.Lerp(0.82f, 1f, n);
                px[y * Tex + x] = new Color(c.r, c.g, c.b, a);
            }
            tex.SetPixels(px);
            tex.Apply(true);
            return tex;
        }

        static Vector3[] Puffs(int seed, int count, float size)
        {
            var rnd = new System.Random(seed);
            var puffs = new Vector3[count];
            for (int i = 0; i < count; i++)
                puffs[i] = new Vector3((float)rnd.NextDouble(), (float)rnd.NextDouble(),
                    size * (0.7f + (float)rnd.NextDouble() * 0.8f));
            return puffs;
        }

        static float Layer(float u, float v, Vector3[] puffs)
        {
            float sum = 0f;
            for (int i = 0; i < puffs.Length; i++)
            {
                float d = new Vector2(u - puffs[i].x, v - puffs[i].y).magnitude / puffs[i].z;
                if (d < 1f) sum += 1f - d;
            }
            return Mathf.Clamp01(sum * 0.5f);
        }

        static float Density(float u, float v, Vector3[] coarse, Vector3[] mid, Vector3[] fine)
            => Mathf.Clamp01(Layer(u, v, coarse) * 0.62f + Layer(u, v, mid) * 0.26f + Layer(u, v, fine) * 0.12f);
    }
}
