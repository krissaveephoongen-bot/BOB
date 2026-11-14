// Battle of the Brooms: Task Race map, curved-world control (docs 24). Physics, colliders, NavMesh and the gameplay
// plane stay flat in X-Z; BOB/CurvedWorld only bends the drawn vertices. The bend radius is driven by one global
// float, _BobCurvature, so every curved material in the scene follows the same mini-planet shape.
using UnityEngine;

namespace Bob.TaskRace
{
    /// <summary>Static access to the curved-world global and a matching CPU sampler for world labels and ground rings.
    /// The CPU formula must stay identical to BobCurvedWorld.hlsl so UI that cannot be curved by the shader sits exactly
    /// on the bent ground.</summary>
    public static class BobCurvedWorld
    {
        public const float DefaultCurvature = 0.018f;

        static readonly int CurvatureId = Shader.PropertyToID("_BobCurvature");
        static Camera sampleCamera;

        /// <summary>Camera the bend is centred on. Defaults to Camera.main; BobTaskRaceCamera sets it explicitly.</summary>
        public static Camera SampleCamera
        {
            get
            {
                if (sampleCamera != null) return sampleCamera;
                sampleCamera = Camera.main;
                return sampleCamera;
            }
            set => sampleCamera = value;
        }

        public static float Curvature => Shader.GetGlobalFloat(CurvatureId);

        public static void SetGlobal(float value) => Shader.SetGlobalFloat(CurvatureId, Mathf.Max(0f, value));

        /// <summary>Vertical offset (<= 0) the shader applies to a world point at this curvature.</summary>
        public static float Drop(Vector3 worldPos)
        {
            var cam = SampleCamera;
            if (cam == null) return 0f;
            var p = cam.transform.position;
            float dx = worldPos.x - p.x, dz = worldPos.z - p.z;
            return -Curvature * (dx * dx + dz * dz);
        }

        /// <summary>The point the player actually sees where `worldPos` was authored.</summary>
        public static Vector3 Curve(Vector3 worldPos) => worldPos + Vector3.up * Drop(worldPos);

        /// <summary>A material on BOB/CurvedWorld; null when the shader is missing.</summary>
        public static Material CreateMaterial(Color color)
        {
            var shader = Resources.Load<Shader>("Shaders/BobCurvedWorld") ?? Shader.Find("BOB/CurvedWorld");
            if (shader == null) return null;
            var m = new Material(shader) { color = color };
            return m;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void ResetGlobal() => SetGlobal(DefaultCurvature);
    }

    /// <summary>Put one on the map root. It blends the global curvature to its value at runtime and mirrors the value in
    /// the Scene view when `applyInEditMode` is on (off by default: props are authored on the flat plane).</summary>
    [ExecuteAlways, DisallowMultipleComponent]
    public class BobCurvedWorldController : MonoBehaviour
    {
        [Tooltip("Squared-distance factor of the drop. 0.018 is the Ever Planet look at a 8.5-unit camera height.")]
        [Range(0f, 0.08f)] public float curvature = BobCurvedWorld.DefaultCurvature;
        [Tooltip("Curvature units per second while blending to a new value.")]
        [Min(0.001f)] public float blendSpeed = 0.25f;
        [Tooltip("Bend the Scene view too. Keep this off while placing props: only the visuals move, not the colliders.")]
        public bool applyInEditMode = false;

        static readonly int Id = Shader.PropertyToID("_BobCurvature");
        float shown;

        void OnEnable()
        {
            shown = curvature;
            if (Application.isPlaying || applyInEditMode) BobCurvedWorld.SetGlobal(shown);
        }

        void OnValidate()
        {
            curvature = Mathf.Clamp(curvature, 0f, 0.08f);
            if (!Application.isPlaying && applyInEditMode) { shown = curvature; Shader.SetGlobalFloat(Id, curvature); }
        }

        void Update()
        {
            if (!Application.isPlaying && !applyInEditMode) return;
            if (Mathf.Approximately(shown, curvature)) return;
            shown = Mathf.MoveTowards(shown, curvature, blendSpeed * Time.unscaledDeltaTime);
            BobCurvedWorld.SetGlobal(shown);
        }

        /// <summary>Blend to a new radius at runtime (0 flattens the map, e.g. for a menu flyover).</summary>
        public void SetCurvature(float value) => curvature = Mathf.Clamp(value, 0f, 0.08f);

        [ContextMenu("Curvature: flat (0)")]
        void CurvatureFlat() => SetCurvature(0f);
        [ContextMenu("Curvature: gentle (0.010)")]
        void CurvatureGentle() => SetCurvature(0.010f);
        [ContextMenu("Curvature: default (0.018)")]
        void CurvatureDefault() => SetCurvature(BobCurvedWorld.DefaultCurvature);
        [ContextMenu("Curvature: strong (0.026)")]
        void CurvatureStrong() => SetCurvature(0.026f);
    }
}
