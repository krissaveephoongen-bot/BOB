// Battle of the Brooms: moving water (docs 24 section 15.15). The waterfall and the river already carry the right
// colours; what was missing was motion. Two transparent quads over the existing surfaces, each with a generated streak
// texture whose UV offset scrolls, plus the light mist at the plunge pool. No texture asset, no shader, no material to
// author - the same 64x64 sheet serves both quads and Sprites/Default is opaque-free.
using System;
using UnityEngine;

namespace Bob.TaskRace
{
    public class BobWaterFlow : MonoBehaviour
    {
        [Tooltip("UV units per second along the river (the water runs toward the falls, +Z).")]
        public float riverSpeed = 0.35f;
        [Tooltip("UV units per second down the falls.")]
        public float fallsSpeed = 1.15f;
        [Range(0f, 1f)] public float riverOpacity = 0.30f;
        [Range(0f, 1f)] public float fallsOpacity = 0.45f;
        [Tooltip("Mist puffs per second at the plunge pool.")]
        public int mistPerSecond = 8;

        Material riverMat, fallsMat;
        Texture2D streaks;
        bool built;

        void Start() => Build();

        public void Build()
        {
            if (built) return;
            built = true;
            streaks = Streaks();
            BuildRiver();
            BuildFalls();
            BuildMist();
        }

        void Update()
        {
            // The river runs north into the falls, so its sheet slides the other way from the falling one.
            if (riverMat != null) riverMat.mainTextureOffset = new Vector2(0f, -Time.time * riverSpeed);
            if (fallsMat != null) fallsMat.mainTextureOffset = new Vector2(0f, Time.time * fallsSpeed);
        }

        void BuildRiver()
        {
            GameObject river = null;
            foreach (var t in FindObjectsByType<Transform>(FindObjectsInactive.Exclude))
                if (t.name.IndexOf("RiverChannel", StringComparison.Ordinal) >= 0) { river = t.gameObject; break; }
            if (river == null) return;

            if (!WorldBounds(river, out var b)) return;
            riverMat = BobAmbientFx.SpriteMaterial(streaks);
            riverMat.color = new Color(1f, 1f, 1f, riverOpacity);

            var quad = Quad("RiverFlow", riverMat);
            quad.transform.position = new Vector3(b.center.x, b.max.y + 0.02f, b.center.z);
            quad.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            quad.transform.localScale = new Vector3(b.size.x * 0.92f, b.size.z * 0.92f, 1f);
        }

        void BuildFalls()
        {
            var falls = DioramaAmbient.Live<BobWaterfallDressing>();
            foreach (var f in falls)
            {
                float z = f.centre.z - f.depth * 0.45f - 0.12f;
                float h = Mathf.Max(0.8f, f.topY - f.waterY);
                float w = Mathf.Max(1.5f, f.halfWidth * 0.62f);

                if (fallsMat == null)
                {
                    fallsMat = BobAmbientFx.SpriteMaterial(streaks);
                    fallsMat.color = new Color(1f, 1f, 1f, fallsOpacity);
                }

                // Unity's Quad faces -Z, which is the play camera's side of the falls.
                var quad = Quad("FallsFlow", fallsMat);
                quad.transform.position = new Vector3(f.centre.x, f.waterY + h * 0.5f, z);
                quad.transform.localScale = new Vector3(w, h, 1f);
            }
        }

        void BuildMist()
        {
            var falls = DioramaAmbient.Live<BobWaterfallDressing>();
            foreach (var f in falls)
            {
                var ps = BobAmbientFx.New("WaterMist", transform, BobAmbientFx.Dot);
                ps.transform.position = new Vector3(f.centre.x, f.waterY + 0.12f, f.centre.z - f.depth * 0.45f - 0.55f);

                var main = ps.main;
                main.startLifetime = new ParticleSystem.MinMaxCurve(1.1f, 1.9f);
                main.startSpeed = new ParticleSystem.MinMaxCurve(0.25f, 0.55f);
                main.startSize = new ParticleSystem.MinMaxCurve(0.35f, 0.75f);
                main.startColor = Color.white;
                main.gravityModifier = -0.01f;
                main.maxParticles = 28;

                var emission = ps.emission;
                emission.rateOverTime = mistPerSecond;

                var shape = ps.shape;
                shape.shapeType = ParticleSystemShapeType.Cone;
                shape.angle = 32f;
                shape.radius = 0.55f;
                shape.rotation = new Vector3(-90f, 0f, 0f);

                var colour = ps.colorOverLifetime;
                colour.enabled = true;
                colour.color = BobAmbientFx.AlphaFade(Color.white, 0.5f);

                var size = ps.sizeOverLifetime;
                size.enabled = true;
                size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 0.7f, 1f, 1.8f));

                ps.Play();
            }
        }

        GameObject Quad(string name, Material material)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.name = name;
            go.transform.SetParent(transform, false);
            var collider = go.GetComponent<Collider>();
            if (collider != null) Destroy(collider);
            var r = go.GetComponent<MeshRenderer>();
            r.sharedMaterial = material;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            return go;
        }

        static bool WorldBounds(GameObject go, out Bounds bounds)
        {
            var renderers = go.GetComponentsInChildren<MeshRenderer>(true);
            bounds = default;
            bool any = false;
            foreach (var r in renderers)
            {
                if (r == null) continue;
                if (!any) { bounds = r.bounds; any = true; }
                else bounds.Encapsulate(r.bounds);
            }
            return any;
        }

        /// <summary>Vertical foam streaks: low-frequency noise down the sheet so a scroll never shows a seam.</summary>
        static Texture2D Streaks()
        {
            const int w = 32, h = 64;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, true)
            {
                name = "WaterStreaks",
                wrapMode = TextureWrapMode.Repeat,
                hideFlags = HideFlags.HideAndDontSave,
            };
            var px = new Color32[w * h];
            var rnd = new System.Random(7);
            var column = new float[w];
            for (int x = 0; x < w; x++) column[x] = (float)rnd.NextDouble();
            float[] rowNoise = new float[h];
            for (int y = 0; y < h; y++) rowNoise[y] = (float)rnd.NextDouble();

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float n = Mathf.PerlinNoise(x * 0.22f, y * 0.045f) * 0.65f + column[x] * 0.35f;
                    float a = Mathf.Clamp01((n - 0.42f) * 2.2f);
                    a *= 0.55f + rowNoise[y] * 0.45f;
                    px[y * w + x] = new Color32(255, 255, 255, (byte)(a * 255f));
                }
            }
            tex.SetPixels32(px);
            tex.Apply(true, true);
            return tex;
        }
    }
}
