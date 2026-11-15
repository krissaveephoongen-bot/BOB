// Battle of the Brooms: the shared kit for the ambient particles (docs 24 section 15.15). One generated soft dot + one
// unlit sprite material per texture + a ParticleSystem factory with the cheap baseline every emitter shares. Nothing
// here is an asset: the dot is 64x64 built once at runtime, so the ambient pass adds no import weight to the APK.
using UnityEngine;

namespace Bob.TaskRace
{
    public static class BobAmbientFx
    {
        static Texture2D dot;

        /// <summary>A 64x64 soft white blob with mipmaps: dust motes, steam and mist all sample this one texture.</summary>
        public static Texture2D Dot
        {
            get
            {
                if (dot != null) return dot;
                const int size = 64;
                dot = new Texture2D(size, size, TextureFormat.RGBA32, true)
                {
                    name = "AmbientDot",
                    wrapMode = TextureWrapMode.Clamp,
                    hideFlags = HideFlags.HideAndDontSave,
                };
                var px = new Color32[size * size];
                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        float dx = (x + 0.5f) / size * 2f - 1f;
                        float dy = (y + 0.5f) / size * 2f - 1f;
                        float r = Mathf.Clamp01(1f - Mathf.Sqrt(dx * dx + dy * dy));
                        float a = r * r * (3f - 2f * r);
                        px[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255f));
                    }
                }
                dot.SetPixels32(px);
                dot.Apply(true, true);
                return dot;
            }
        }

        /// <summary>Sprites/Default is the one built-in shader in this project with alpha blending (BOB/Unlit is opaque).</summary>
        public static Material SpriteMaterial(Texture2D tex)
        {
            var shader = Shader.Find("Sprites/Default") ?? Shader.Find("Unlit/Transparent");
            var m = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
            if (tex != null) m.mainTexture = tex;
            return m;
        }

        /// <summary>The Leonardo FX cut-outs already live under Resources/Textures/Minigame (docs 25).</summary>
        public static Texture2D Resource(string name) => Resources.Load<Texture2D>("Textures/Minigame/" + name);

        /// <summary>A looping world-space billboard emitter with the renderer and the boring modules already set.</summary>
        public static ParticleSystem New(string name, Transform parent, Texture2D tex)
        {
            var go = new GameObject(name) { hideFlags = HideFlags.HideAndDontSave };
            go.transform.SetParent(parent, false);
            var ps = go.AddComponent<ParticleSystem>();

            var main = ps.main;
            main.loop = true;
            main.prewarm = true;
            main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var r = go.GetComponent<ParticleSystemRenderer>();
            r.renderMode = ParticleSystemRenderMode.Billboard;
            r.material = SpriteMaterial(tex);
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            return ps;
        }

        /// <summary>Fade in fast, hold `peak`, fade out at the end of the lifetime.</summary>
        public static Gradient AlphaFade(Color colour, float peak)
        {
            var g = new Gradient();
            g.SetKeys(
                new[] { new GradientColorKey(colour, 0f), new GradientColorKey(colour, 1f) },
                new[]
                {
                    new GradientAlphaKey(0f, 0f),
                    new GradientAlphaKey(peak, 0.18f),
                    new GradientAlphaKey(peak, 0.65f),
                    new GradientAlphaKey(0f, 1f),
                });
            return g;
        }
    }
}
