// Battle of the Brooms: Task Race camera (docs 24). A smooth third-person follow camera for the local hero on the twin
// islands: 45 degrees of pitch above the horizon (the angle docs 20 section 4 C4 settled on) and 5.2 units behind, so the
// lens sits 5.2 units up — the height is derived from the angle, never typed twice. 40 degree perspective lens. The rig
// puts the hero at roughly a fifth of the screen height, so the character reads at phone size and the nearest task sign
// is findable within a second. It replaces the fixed diorama camera only where it is added (the task-race map scene),
// and it runs in LateUpdate so it always wins over a code-built camera that re-frames in Update.
//
// S28 focus: while the minigame card is open, SetMinigameFocus(true) tightens the lens to 85% and slides the look target
// 1.2 units right, so the hero and the workstation stay visible on the left of the card instead of behind it.
using UnityEngine;

namespace Bob.TaskRace
{
    [RequireComponent(typeof(Camera))]
    public class BobTaskRaceCamera : MonoBehaviour
    {
        [Header("Follow")]
        [Tooltip("The local player. Left empty, the component looks for a GameObject tagged Player.")]
        public Transform target;
        [Tooltip("Pitch above the horizon in degrees. 45 is the recorded decision (docs 20 section 4 C4).")]
        [Range(20f, 80f)] public float pitch = 45f;
        [Tooltip("Horizontal distance behind the target. The camera height comes from the pitch, never from a second field.")]
        public float distance = 5.2f;
        [Tooltip("Seconds for the position to settle.")] public float followTime = 0.22f;
        [Tooltip("Seconds for the look target to settle.")] public float lookTime = 0.14f;
        [Tooltip("Yaw around Y. 0 looks north (+Z), which is the twin-island layout's forward.")] [Range(-180f, 180f)] public float yaw;
        [Tooltip("Height above the target's origin the camera aims at.")] public float lookHeight = 0.72f;

        [Header("Lens")]
        public float fieldOfView = 40f;

        [Header("Minigame focus (S28)")]
        [Tooltip("When a minigame card is open: tighter lens, look target shifted right so the hero stays on the left.")]
        [SerializeField] bool minigameFocus;
        [Tooltip("Field-of-view multiplier while the card is open (0.85 = zoomed in).")] public float focusZoom = 0.85f;
        [Tooltip("World-X offset of the look target while the card is open; positive = screen right.")] public float focusShiftX = 1.2f;
        [Tooltip("Seconds for the focus zoom and shift to settle.")] public float focusTime = 0.35f;

        Camera cam;
        Vector3 positionVelocity, lookVelocity, lookPoint;
        float fovVelocity, shift, shiftVelocity;
        bool snapped;

        public bool MinigameFocus => minigameFocus;
        public Transform Target => target;
        /// <summary>Camera height above the target, derived from the pitch so the two can never drift apart.</summary>
        public float Height => Mathf.Max(0.5f, Mathf.Tan(pitch * Mathf.Deg2Rad) * distance);
        public float Pitch => pitch;
        public float Distance => distance;

        void Awake()
        {
            cam = GetComponent<Camera>();
            cam.orthographic = false;
            cam.fieldOfView = fieldOfView;
            if (target == null)
            {
                var go = FindPlayer();
                if (go != null) target = go.transform;
            }
        }

        void LateUpdate()
        {
            if (cam == null) return;
            if (target == null) target = FindPlayer()?.transform;
            if (target == null) return;

            float dt = Time.unscaledDeltaTime;
            float wantFov = fieldOfView * (minigameFocus ? focusZoom : 1f);
            float wantShift = minigameFocus ? focusShiftX : 0f;
            var wantLook = target.position + Vector3.up * lookHeight + Vector3.right * wantShift;
            var wantPos = target.position + Quaternion.Euler(0f, yaw, 0f) * new Vector3(0f, Height, -distance);

            if (!snapped) Snap(wantPos, wantLook, wantFov, wantShift);

            shift = Mathf.SmoothDamp(shift, wantShift, ref shiftVelocity, focusTime, Mathf.Infinity, dt);
            lookPoint = Vector3.SmoothDamp(lookPoint, wantLook, ref lookVelocity, lookTime, Mathf.Infinity, dt);
            transform.position = Vector3.SmoothDamp(transform.position, wantPos, ref positionVelocity, followTime, Mathf.Infinity, dt);
            transform.rotation = Quaternion.LookRotation(lookPoint - transform.position, Vector3.up);
            cam.fieldOfView = Mathf.SmoothDamp(cam.fieldOfView, wantFov, ref fovVelocity, focusTime, Mathf.Infinity, dt);

            // The camera is the bend centre of BOB/CurvedWorld; keep the CPU sampler in step for world labels.
            BobCurvedWorld.SampleCamera = cam;
        }

        /// <summary>Open / close the S28 card focus. The zoom and the right shift both ease in, never snap.</summary>
        public void SetMinigameFocus(bool isOpen) => minigameFocus = isOpen;

        public void ToggleMinigameFocus() => SetMinigameFocus(!minigameFocus);

        /// <summary>Follow a new hero and place the camera behind it immediately (spawn, respawn).</summary>
        public void SetTarget(Transform t, bool snap = true)
        {
            target = t;
            if (snap) SnapToTarget();
            else snapped = false;
        }

        /// <summary>Hard-cut to the framed shot without a smooth dip.</summary>
        public void SnapToTarget()
        {
            if (target == null) return;
            float wantFov = fieldOfView * (minigameFocus ? focusZoom : 1f);
            float wantShift = minigameFocus ? focusShiftX : 0f;
            var wantLook = target.position + Vector3.up * lookHeight + Vector3.right * wantShift;
            var wantPos = target.position + Quaternion.Euler(0f, yaw, 0f) * new Vector3(0f, Height, -distance);
            Snap(wantPos, wantLook, wantFov, wantShift);
        }

        void Snap(Vector3 position, Vector3 look, float fov, float shiftX)
        {
            transform.position = position;
            lookPoint = look;
            transform.rotation = Quaternion.LookRotation(look - position, Vector3.up);
            shift = shiftX;
            if (cam == null) cam = GetComponent<Camera>();
            if (cam != null) { cam.orthographic = false; cam.fieldOfView = fov; }
            positionVelocity = lookVelocity = Vector3.zero;
            shiftVelocity = fovVelocity = 0f;
            snapped = true;
        }

        static GameObject FindPlayer()
        {
            try { return GameObject.FindGameObjectWithTag("Player"); }
            catch (UnityException) { return null; } // tag not defined yet: assign Target in the Inspector
        }

        void OnValidate()
        {
            pitch = Mathf.Clamp(pitch, 20f, 80f);
            distance = Mathf.Max(0.5f, distance);
            fieldOfView = Mathf.Clamp(fieldOfView, 10f, 90f);
            focusZoom = Mathf.Clamp(focusZoom, 0.5f, 1.5f);
            followTime = Mathf.Max(0.01f, followTime);
            lookTime = Mathf.Max(0.01f, lookTime);
            focusTime = Mathf.Max(0.01f, focusTime);
        }
    }
}
