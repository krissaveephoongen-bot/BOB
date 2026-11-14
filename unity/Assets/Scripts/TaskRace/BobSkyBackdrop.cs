// Battle of the Brooms: the written sky behind a Task Race map (docs 24). The islands are small and the camera is low,
// so the horizon is always in frame; a flat clear colour made the map float on a blue sheet. This hangs one unlit quad
// 70 units in front of the camera, refitted to the frustum every frame, with a warm horizon, a blue zenith and a few
// pastel clouds — the same written-sky approach as the main game's diorama, kept here because the Task Race camera moves.
using UnityEngine;

namespace Bob.TaskRace
{
    [ExecuteAlways, RequireComponent(typeof(Camera))]
    public class BobSkyBackdrop : MonoBehaviour
    {
        [Tooltip("Distance in front of the camera. Must stay below the far clip and behind the map.")]
        public float depth = 70f;
        [Tooltip("Extra width so a changed aspect ratio never shows an edge.")]
        public float padding = 1.12f;
        // The play camera looks down at the island, so the bottom of the quad is below the horizon: keep it a pale blue
        // haze and let the blue deepen upward, matching the reference's flat pastel sky.
        public Color horizon = new Color(0.80f, 0.90f, 0.98f);
        public Color middle = new Color(0.62f, 0.83f, 0.97f);
        public Color zenith = new Color(0.38f, 0.64f, 0.91f);
        [Range(0f, 1f)] public float cloudAmount = 0.8f;

        const int W = 512, H = 288;
        Transform quad;
        Mesh mesh;
        Texture2D texture;

        void OnEnable() => Ensure();
        void LateUpdate() => Fit();

        void Ensure()
        {
            if (quad != null) return;
            if (texture == null)
            {
                texture = BuildTexture();
                texture.hideFlags = HideFlags.DontSave;
            }

            var go = new GameObject(name + " (Sky)", typeof(MeshFilter), typeof(MeshRenderer))
            {
                hideFlags = HideFlags.HideAndDontSave,
            };
            mesh = QuadMesh();
            go.GetComponent<MeshFilter>().sharedMesh = mesh;
            var mr = go.GetComponent<MeshRenderer>();
            mr.sharedMaterial = SkyMaterial();
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;

            quad = go.transform;
            Fit();
        }

        Material SkyMaterial()
        {
            var shader = Resources.Load<Shader>("Shaders/BobUnlit") ?? Shader.Find("Unlit/Texture");
            var m = new Material(shader) { mainTexture = texture };
            m.hideFlags = HideFlags.DontSave;
            return m;
        }

        /// <summary>Size the quad to the camera's current FOV and aspect and park it `depth` units in front. It is a
        /// scene root rather than a camera child so destroying the camera cannot destroy the quad mid-callback.</summary>
        public void Fit()
        {
            if (quad == null) return;
            var cam = GetComponent<Camera>();
            float h = 2f * depth * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad) * padding;
            float w = h * Mathf.Max(1f, cam.aspect);
            var t = cam.transform;
            quad.SetPositionAndRotation(t.position + t.forward * depth, t.rotation);
            quad.localScale = new Vector3(w, h, 1f);
        }

        void OnDisable()
        {
            if (quad != null) DestroySafe(quad.gameObject);
            if (mesh != null) DestroySafe(mesh);
            if (texture != null) DestroySafe(texture);
            quad = null; mesh = null; texture = null;
        }

        static void DestroySafe(Object o)
        {
            if (o == null) return;
            if (Application.isPlaying) Destroy(o); else DestroyImmediate(o);
        }

        static Mesh QuadMesh()
        {
            // faces -Z (towards the camera); the sky material is Cull Off anyway
            var m = new Mesh { name = "SkyQuad", hideFlags = HideFlags.DontSave };
            m.vertices = new[]
            {
                new Vector3(-0.5f, -0.5f, 0f), new Vector3(0.5f, -0.5f, 0f),
                new Vector3(-0.5f, 0.5f, 0f), new Vector3(0.5f, 0.5f, 0f),
            };
            m.uv = new[] { new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 1f), new Vector2(1f, 1f) };
            m.triangles = new[] { 0, 2, 1, 2, 3, 1 };
            m.RecalculateNormals();
            m.RecalculateBounds();
            return m;
        }

        Texture2D BuildTexture()
        {
            var tex = new Texture2D(W, H, TextureFormat.RGBA32, false)
            {
                name = "T_BOB_Sky",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
            };
            var px = new Color[W * H];
            for (int y = 0; y < H; y++)
            {
                float v = y / (H - 1f);
                var baseC = v < 0.5f
                    ? Color.Lerp(horizon, middle, v / 0.5f)
                    : Color.Lerp(middle, zenith, (v - 0.5f) / 0.5f);
                for (int x = 0; x < W; x++)
                {
                    float dither = (Hash01(x, y) - 0.5f) * 0.012f;
                    px[y * W + x] = new Color(baseC.r + dither, baseC.g + dither, baseC.b + dither, 1f);
                }
            }

            var rnd = new System.Random(11);
            int clouds = Mathf.RoundToInt(7f * Mathf.Clamp01(cloudAmount));
            for (int i = 0; i < clouds; i++)
            {
                float cx = 0.08f + (float)rnd.NextDouble() * 0.84f;
                float cy = 0.74f + (float)rnd.NextDouble() * 0.22f;
                float s = 0.6f + (float)rnd.NextDouble() * 0.8f;
                for (int b = 0; b < 4; b++)
                    Puff(px, (cx + (b - 1.5f) * 0.05f * s) * W, (cy + (b % 2) * 0.017f * s) * H,
                        0.075f * s * W, 0.030f * s * H, 0.85f * cloudAmount);
            }
            tex.SetPixels(px);
            tex.Apply(true);
            return tex;
        }

        static void Puff(Color[] px, float cx, float cy, float rx, float ry, float alpha)
        {
            var top = new Color(1f, 0.99f, 0.95f);
            var belly = new Color(0.90f, 0.93f, 0.96f);
            int x0 = Mathf.Max(0, Mathf.FloorToInt(cx - rx)), x1 = Mathf.Min(W - 1, Mathf.CeilToInt(cx + rx));
            int y0 = Mathf.Max(0, Mathf.FloorToInt(cy - ry)), y1 = Mathf.Min(H - 1, Mathf.CeilToInt(cy + ry));
            for (int y = y0; y <= y1; y++)
            for (int x = x0; x <= x1; x++)
            {
                float dx = (x - cx) / rx, dy = (y - cy) / ry;
                float d = dx * dx + dy * dy;
                if (d >= 1f) continue;
                float a = Mathf.Pow(1f - d, 1.6f) * alpha;
                px[y * W + x] = Color.Lerp(px[y * W + x], Color.Lerp(belly, top, Mathf.Clamp01(dy * 0.5f + 0.55f)), a);
            }
        }

        static float Hash01(int x, int y)
        {
            unchecked
            {
                uint h = (uint)(x * 374761393 + y * 668265263);
                h = (h ^ (h >> 13)) * 1274126177u;
                return ((h ^ (h >> 16)) & 0xFFFFFF) / (float)0xFFFFFF;
            }
        }
    }
}
