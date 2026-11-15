// Battle of the Brooms: the living-diorama pass (docs 24 section 15.15). One component on the Systems object turns the
// map from a lit model into a place that breathes: the warm sun/ambient rig, wind on the big greenery, wind on the
// grass (through the dressing shader), golden dust motes over the islands, steam over the wash stations, moving water
// and a bloom/exposure grade on the play camera. Everything is built in code so the scene YAML stays untouched and a
// scene rebuild cannot lose it. TaskRaceModeManager.Awake calls Ensure().
//
// Anti over-engineering: no Animator, no Timeline, no URP, no new asset. Six ParticleSystems with under 200 live
// particles total, one extra transparent quad per water surface, and two property floats.
using System.Collections.Generic;
using UnityEngine;

namespace Bob.TaskRace
{
    [DisallowMultipleComponent]
    public class DioramaAmbient : MonoBehaviour
    {
        [Header("Post grade (the built-in-pipeline stand-in for a URP volume)")]
        [Range(0f, 1f)] public float bloom = 0.35f;
        [Range(0.6f, 2f)] public float bloomThreshold = 1.05f;
        // +0.15 EV is the URP recipe, but this scene's unlit sky already sits at 0.85 luma: measured, +0.15 turned 60% of
        // the wide shot near-white and pushed the pastel blue to mint. +0.03 keeps the lift without the wash.
        [Range(-1f, 1f)] public float postExposure = 0.03f;
        [Range(0.5f, 2f)] public float saturation = 1.10f;

        [Header("Ambient motion")]
        [Tooltip("Dust motes per second over both islands.")]
        public float dustPerSecond = 6f;
        [Tooltip("Steam puffs per second over each wash station.")]
        public float steamPerSecond = 3.5f;

        static DioramaAmbient preview;
        readonly List<ParticleSystem> systems = new List<ParticleSystem>();
        bool built;

        void Awake() => Build();

        /// <summary>Called by TaskRaceModeManager.Awake: one ambient pass per scene, never two.</summary>
        public static DioramaAmbient Ensure(GameObject host)
        {
            if (host == null) return null;
            var amb = host.GetComponent<DioramaAmbient>();
            return amb != null ? amb : host.AddComponent<DioramaAmbient>();
        }

        public void Build()
        {
            if (built) return;
            built = true;

            if (GetComponent<DioramaLightingRig>() == null) gameObject.AddComponent<DioramaLightingRig>().Apply();

            var root = new GameObject("AmbientFX") { hideFlags = HideFlags.HideAndDontSave };
            root.transform.SetParent(transform, false);

            GrassWind(true);
            BuildDust(root.transform);
            BuildSteam(root.transform);
            root.AddComponent<BobWaterFlow>().Build();

            if (Application.isPlaying)
            {
                BuildWind();
                BuildJuice();
            }

            Grade();
            if (Application.isPlaying)
                Debug.Log($"[DioramaAmbient] built: {systems.Count} emitters, wind on the grass and the greenery, " +
                          $"bloom {bloom:0.00} @ {bloomThreshold:0.00}, exposure {postExposure:+0.00;-0.00}.");
        }

        // ---------------------------------------------------------------- wind

        /// <summary>Big greenery sways on the CPU; the grass sways in the shader (one vertex-colour weight). The rocks,
        /// the counters and the pots themselves are left alone, so nothing slides across the floor.</summary>
        void BuildWind()
        {
            foreach (var t in FindObjectsByType<Transform>(FindObjectsInactive.Exclude))
            {
                if (t.GetComponent<AmbientWindSway>() != null) continue;
                string n = t.name;
                if (n.StartsWith("SM_bush_green")) Sway(t, 2.2f, 0.95f);
                else if (n.StartsWith("SM_plant_pot") || n.StartsWith("SM_pot_blue") || n.StartsWith("SM_pot_ceramic_gold"))
                    Sway(t, 1.0f, 1.35f);
            }
        }

        static void Sway(Transform t, float amplitude, float speed)
        {
            var s = t.gameObject.AddComponent<AmbientWindSway>();
            s.amplitude = amplitude;
            s.speed = speed;
        }

        /// <summary>The field dressing is one mesh for the whole island: a single property float animates every blade.</summary>
        static void GrassWind(bool on)
        {
            var block = new MaterialPropertyBlock();
            foreach (var field in Live<BobFieldDressing>())
            {
                var r = field.Renderer;
                if (r == null) continue;
                r.GetPropertyBlock(block);
                block.SetFloat(BobFieldDressing.WindId, on ? 1f : 0f);
                r.SetPropertyBlock(block);
            }
        }

        /// <summary>The dressings are built at runtime with HideAndDontSave, which FindObjectsByType skips (docs 24
        /// section 15.14). FindObjectsOfTypeAll sees them; the scene check drops the prefab/asset copies.</summary>
        public static List<T> Live<T>() where T : Component
        {
            var found = new List<T>();
            foreach (var c in Resources.FindObjectsOfTypeAll<T>())
                if (c != null && c.gameObject.scene.IsValid()) found.Add(c);
            return found;
        }

        // ---------------------------------------------------------------- particles

        void BuildDust(Transform root)
        {
            if (!IslandBox(out var min, out var max)) return;

            var ps = BobAmbientFx.New("DustMotes", root, BobAmbientFx.Dot);
            var main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(9f, 15f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.02f, 0.07f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.12f, 0.24f);
            main.startColor = new Color(1f, 0.96f, 0.82f, 0.9f);
            main.gravityModifier = -0.004f;
            main.maxParticles = 110;

            var emission = ps.emission;
            emission.rateOverTime = dustPerSecond;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(max.x - min.x + 2f, 3.2f, max.z - min.z + 2f);
            shape.position = new Vector3((min.x + max.x) * 0.5f, 1.9f, (min.z + max.z) * 0.5f);

            // All three axes must be the same curve mode or Unity refuses to simulate: "curves must all be in the same mode".
            var vel = ps.velocityOverLifetime;
            vel.enabled = true;
            vel.space = ParticleSystemSimulationSpace.World;
            vel.x = new ParticleSystem.MinMaxCurve(-0.03f, 0.08f);
            vel.y = new ParticleSystem.MinMaxCurve(0f, 0f);
            vel.z = new ParticleSystem.MinMaxCurve(-0.02f, 0.05f);

            var colour = ps.colorOverLifetime;
            colour.enabled = true;
            colour.color = BobAmbientFx.AlphaFade(new Color(1f, 0.96f, 0.82f), 0.85f);

            var size = ps.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 0.6f, 1f, 1.15f));

            ps.Play();
            systems.Add(ps);
        }

        void BuildSteam(Transform root)
        {
            foreach (var spot in FindObjectsByType<TaskSpot>(FindObjectsInactive.Exclude))
            {
                if (spot.Category != TaskCategory.Wash) continue;

                var ps = BobAmbientFx.New("Steam_" + spot.SpotId, spot.transform, BobAmbientFx.Dot);
                ps.transform.localPosition = Vector3.up * 1.08f;

                var main = ps.main;
                main.startLifetime = new ParticleSystem.MinMaxCurve(1.9f, 3.1f);
                main.startSpeed = new ParticleSystem.MinMaxCurve(0.05f, 0.14f);
                main.startSize = new ParticleSystem.MinMaxCurve(0.14f, 0.34f);
                main.startColor = Color.white;
                main.gravityModifier = -0.02f;
                main.maxParticles = 26;

                var emission = ps.emission;
                emission.rateOverTime = steamPerSecond;

                var shape = ps.shape;
                shape.shapeType = ParticleSystemShapeType.Cone;
                shape.angle = 14f;
                shape.radius = 0.09f;
                shape.rotation = new Vector3(-90f, 0f, 0f);
                shape.position = Vector3.zero;

                var vel = ps.velocityOverLifetime;
                vel.enabled = true;
                vel.space = ParticleSystemSimulationSpace.World;
                vel.x = new ParticleSystem.MinMaxCurve(0f, 0f);
                vel.y = new ParticleSystem.MinMaxCurve(0.16f, 0.26f);
                vel.z = new ParticleSystem.MinMaxCurve(0f, 0f);

                var colour = ps.colorOverLifetime;
                colour.enabled = true;
                colour.color = BobAmbientFx.AlphaFade(Color.white, 0.45f);

                var size = ps.sizeOverLifetime;
                size.enabled = true;
                size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 0.55f, 1f, 1.9f));

                ps.Play();
                systems.Add(ps);
            }
        }

        /// <summary>The two grass slabs, so the motes hug the islands instead of a box around every station.</summary>
        static bool IslandBox(out Vector3 min, out Vector3 max)
        {
            min = new Vector3(-6f, 0f, -6f);
            max = new Vector3(6f, 0f, 6f);

            bool any = false;
            foreach (var field in Live<BobFieldDressing>())
            {
                var r = field.Renderer;
                if (r == null) continue;
                var b = r.bounds;
                min = any ? Vector3.Min(min, b.min) : b.min;
                max = any ? Vector3.Max(max, b.max) : b.max;
                any = true;
            }
            return any;
        }

        // ---------------------------------------------------------------- camera / completion

        void Grade()
        {
            foreach (var grade in FindObjectsByType<BobPostGrade>(FindObjectsInactive.Exclude))
            {
                grade.bloom = bloom;
                grade.bloomThreshold = bloomThreshold;
                grade.postExposure = postExposure;
                grade.saturation = saturation;
            }
        }

        void BuildJuice()
        {
            foreach (var spot in FindObjectsByType<TaskSpot>(FindObjectsInactive.Exclude))
                if (spot.GetComponent<BobStationJuice>() == null) spot.gameObject.AddComponent<BobStationJuice>();
        }

        // ---------------------------------------------------------------- editor / verify preview

        /// <summary>Build the whole ambient pass outside play mode so the verify renders show what the player sees.</summary>
        public static DioramaAmbient Preview()
        {
            if (preview != null) return preview;
            var go = new GameObject("DioramaAmbient_Preview") { hideFlags = HideFlags.HideAndDontSave };
            preview = go.AddComponent<DioramaAmbient>();
            preview.Build();
            return preview;
        }

        /// <summary>Fast-forward the emitters so the first rendered frame already has dust, steam and mist in it.</summary>
        public static void Warm(float seconds)
        {
            if (preview == null) return;
            foreach (var ps in preview.GetComponentsInChildren<ParticleSystem>(true))
            {
                ps.Simulate(seconds, true, true);
                if (ps.particleCount == 0)
                {
                    Debug.Log($"[DioramaAmbient] {ps.name}: no live particles after {seconds:0.#}s");
                    continue;
                }
                var buffer = new ParticleSystem.Particle[ps.particleCount];
                int n = ps.GetParticles(buffer);
                var r = ps.GetComponent<ParticleSystemRenderer>();
                Debug.Log($"[DioramaAmbient] {ps.name}: {n} live at {buffer[0].position}, " +
                          $"{r?.sharedMaterial?.shader?.name ?? "no material"}, tex={r?.sharedMaterial?.mainTexture?.name}, " +
                          $"bounds={r?.bounds}, enabled={r?.enabled}, alpha={buffer[0].GetCurrentColor(ps).a:0.00}, " +
                          $"startSize={buffer[0].GetCurrentSize(ps):0.00}");
            }
        }

        public static void PreviewTearDown()
        {
            if (preview == null) return;
            GrassWind(false);
            DestroyImmediate(preview.gameObject);
            preview = null;
        }
    }
}
