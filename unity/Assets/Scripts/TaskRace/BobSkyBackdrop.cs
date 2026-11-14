// Battle of the Brooms: the written sky behind a Task Race map (docs 24). The islands are small and the camera is low,
// so the horizon is always in frame; a flat clear colour made the map float on a blue sheet.
// The sky is a mesh authored in clip space ([-1,1] NDC) and drawn by BOB/SkyNDC, so it covers the frame exactly for
// any FOV/aspect — a quad at a fixed world depth showed its own edge in the wide shot.
using UnityEngine;

namespace Bob.TaskRace
{
    [ExecuteAlways, RequireComponent(typeof(Camera))]
    public class BobSkyBackdrop : MonoBehaviour
    {
        // The play camera looks down at the island, so the bottom of the screen is below the horizon: keep it a pale
        // blue haze and let the blue deepen upward, matching the reference's flat pastel sky.
        public Color horizon = new Color(0.82f, 0.91f, 0.99f);
        public Color middle = new Color(0.68f, 0.86f, 0.98f);
        public Color zenith = new Color(0.47f, 0.72f, 0.95f);
        [Range(0f, 1f)] public float cloudAmount = 0.8f;

        const int W = 512, H = 288;
        Transform quad;
        Mesh mesh;
        Texture2D texture;
        Material material;

        void OnEnable() => Ensure();

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
        }

        Material SkyMaterial()
        {
            var shader = Resources.Load<Shader>("Shaders/BobSkyNDC") ?? Shader.Find("BOB/SkyNDC");
            if (shader == null)
            {
                Debug.LogWarning("[BobSkyBackdrop] BOB/SkyNDC not found; the sky is the camera clear colour.");
                shader = Shader.Find("Unlit/Texture");
            }
            material = new Material(shader) { mainTexture = texture };
            material.hideFlags = HideFlags.DontSave;
            return material;
        }

        /// <summary>The quad is in clip space; kept so older scene scripts that call Fit() still compile.</summary>
        public void Fit() { }

        void OnDisable()
        {
            if (quad != null) DestroySafe(quad.gameObject);
            if (mesh != null) DestroySafe(mesh);
            if (texture != null) DestroySafe(texture);
            if (material != null) DestroySafe(material);
            quad = null; mesh = null; texture = null; material = null;
        }

        static void DestroySafe(Object o)
        {
            if (o == null) return;
            if (Application.isPlaying) Destroy(o); else DestroyImmediate(o);
        }

        static Mesh QuadMesh()
        {
            // Already in NDC: BOB/SkyNDC outputs v.vertex.xy directly, so the transform is irrelevant.
            var m = new Mesh { name = "SkyNDC", hideFlags = HideFlags.DontSave };
            m.vertices = new[]
            {
                new Vector3(-1f, -1f, 0f), new Vector3(1f, -1f, 0f),
                new Vector3(-1f, 1f, 0f), new Vector3(1f, 1f, 0f),
            };
            m.uv = new[] { new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 1f), new Vector2(1f, 1f) };
            m.triangles = new[] { 0, 2, 1, 2, 3, 1 };
            // A huge bound keeps the renderer from being frustum-culled; the shader ignores the transform anyway.
            m.bounds = new Bounds(Vector3.zero, Vector3.one * 1e6f);
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
                // A pale warm haze sits on the horizon line so the island rims and the sky meet softly.
                float haze = Mathf.Exp(-Mathf.Pow((v - 0.46f) * 9f, 2f));
                baseC = Color.Lerp(baseC, new Color(0.95f, 0.97f, 1f), haze * 0.25f);
                for (int x = 0; x < W; x++)
                {
                    float dither = (Hash01(x, y) - 0.5f) * 0.012f;
                    px[y * W + x] = new Color(baseC.r + dither, baseC.g + dither, baseC.b + dither, 1f);
                }
            }

            var rnd = new System.Random(11);
            int clouds = Mathf.RoundToInt(9f * Mathf.Clamp01(cloudAmount));
            for (int i = 0; i < clouds; i++)
            {
                float cx = 0.04f + (float)rnd.NextDouble() * 0.92f;
                float cy = 0.66f + (float)rnd.NextDouble() * 0.30f;
                float s = 0.45f + (float)rnd.NextDouble() * 0.95f;
                for (int b = 0; b < 5; b++)
                    Puff(px, (cx + (b - 2f) * 0.043f * s) * W, (cy + (b % 2) * 0.016f * s) * H,
                        0.062f * s * W, 0.026f * s * H, 0.8f * cloudAmount);
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
