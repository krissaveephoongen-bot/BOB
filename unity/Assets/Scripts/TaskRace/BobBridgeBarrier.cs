// Battle of the Brooms: the red-white barrier that shuts each bridge until the TimedBridgeRaid clock reaches 60 s
// (docs 24 sections 3.1 and 12). One component per bridge, always active on BridgeBarrier_Group:
//   - a full-width BoxCollider stops the hero (and the NavMesh agent) at the gate,
//   - a NavMeshObstacle with carving blocks the raid bake at runtime,
//   - Shake() rattles the gate through the 5-second warning,
//   - Open() lowers the gate 1.5 u in Y over 0.6 s, then drops both blockers so the bridge NavMeshLink can carry
//     players across. Close() raises it again for a restart or a PureRace match.
// The gate mesh (BobBridgeGate) and this component share one GameObject, so lowering the transform moves both.
using UnityEngine;
using UnityEngine.AI;

namespace Bob.TaskRace
{
    [DisallowMultipleComponent]
    public class BobBridgeBarrier : MonoBehaviour
    {
        [Header("Gate")]
        [Tooltip("Deck width the gate spans (the bridge's Z width).")]
        [SerializeField, Min(0.5f)] float width = 2f;
        [Tooltip("How far the gate drops when the bridges open.")]
        [SerializeField, Min(0.1f)] float dropHeight = 1.5f;
        [Tooltip("Seconds for the drop.")]
        [SerializeField, Min(0.05f)] public float dropSeconds = 0.6f;
        [Tooltip("Seconds of warning shake before the drop.")]
        [SerializeField, Min(0f)] public float shakeSeconds = 5f;

        [Header("Blockers")]
        [SerializeField, Min(0.05f)] float thickness = 0.4f;
        [SerializeField, Min(0.2f)] float height = 1.2f;

        BoxCollider box;
        NavMeshObstacle obstacle;
        Vector3 closedLocal;
        bool closedCaptured;

        float drop;          // 0 = closed, dropHeight = fully lowered
        float dropVelocity;
        float shakeUntil;
        float shakePhase;

        public bool IsOpen { get; private set; }
        public float Width => width;

        public void Configure(float deckWidth, float drop = 1.5f, float seconds = 0.6f)
        {
            width = Mathf.Max(0.5f, deckWidth);
            dropHeight = Mathf.Max(0.1f, drop);
            dropSeconds = Mathf.Max(0.05f, seconds);
            EnsureParts();
        }

        void OnEnable()
        {
            EnsureParts();
            if (!closedCaptured)
            {
                closedLocal = transform.localPosition;
                closedCaptured = true;
            }
        }

        void OnValidate()
        {
            // Never AddComponent here: OnValidate runs from AddComponent itself, and adding during that callback
            // logs "SendMessage cannot be called during Awake/CheckConsistency/OnValidate". Just resize what exists.
            if (box == null) box = GetComponent<BoxCollider>();
            if (obstacle == null) obstacle = GetComponent<NavMeshObstacle>();
            if (box == null || obstacle == null) return;
            ApplyBlockerSize();
        }

        void EnsureParts()
        {
            if (box == null) box = GetComponent<BoxCollider>();
            if (box == null) box = gameObject.AddComponent<BoxCollider>();

            if (obstacle == null) obstacle = GetComponent<NavMeshObstacle>();
            if (obstacle == null) obstacle = gameObject.AddComponent<NavMeshObstacle>();
            ApplyBlockerSize();
        }

        void ApplyBlockerSize()
        {
            box.center = new Vector3(0f, height * 0.5f, 0f);
            box.size = new Vector3(thickness, height, width);

            obstacle.shape = NavMeshObstacleShape.Box;
            obstacle.center = box.center;
            obstacle.size = box.size;
            obstacle.carving = true;
            obstacle.carveOnlyStationary = true;
        }

        /// <summary>Instant closed state, used by the scene builder, a mode switch and a match reset.</summary>
        public void CloseInstant()
        {
            if (!closedCaptured) { closedLocal = transform.localPosition; closedCaptured = true; }
            drop = 0f; dropVelocity = 0f; IsOpen = false;
            transform.localPosition = closedLocal;
            SetBlockers(true);
        }

        /// <summary>Lower the gate over `dropSeconds`; both blockers switch off at the start of the drop.</summary>
        public void Open()
        {
            if (IsOpen) return;
            IsOpen = true;
            SetBlockers(false);
            if (!Application.isPlaying) { FinishDrop(); return; }
            drop = 0f; dropVelocity = 0f; // Update eases it down and FinishDrop snaps at the end
        }

        /// <summary>Editor / verify: the open state with no animation.</summary>
        public void OpenInstant()
        {
            IsOpen = true;
            SetBlockers(false);
            FinishDrop();
        }

        /// <summary>Rattle the gate in place for the warning window. The gate does not move down yet.</summary>
        public void Shake(float seconds)
        {
            if (seconds <= 0f) return;
            if (!Application.isPlaying) return;
            shakeUntil = Mathf.Max(shakeUntil, Time.time + seconds);
            shakePhase = 0f;
        }

        void Update()
        {
            if (!Application.isPlaying) return;

            if (IsOpen && drop < dropHeight)
            {
                drop = Mathf.SmoothDamp(drop, dropHeight, ref dropVelocity, dropSeconds * 0.35f, Mathf.Infinity, Time.deltaTime);
                transform.localPosition = closedLocal + Vector3.down * drop;
                if (dropHeight - drop < 0.01f) FinishDrop();
            }

            if (Time.time < shakeUntil && !IsOpen)
            {
                shakePhase += Time.deltaTime * 46f;
                float amp = Mathf.Lerp(0.035f, 0.008f, 1f - Mathf.Clamp01((shakeUntil - Time.time) / Mathf.Max(0.01f, shakeSeconds)));
                transform.localPosition = closedLocal + Vector3.right * (Mathf.Sin(shakePhase) * amp);
            }
            else if (Time.time >= shakeUntil && !IsOpen)
            {
                transform.localPosition = closedLocal;
            }
        }

        void FinishDrop()
        {
            drop = dropHeight;
            dropVelocity = 0f;
            transform.localPosition = closedLocal + Vector3.down * dropHeight;
        }

        void SetBlockers(bool on)
        {
            if (box != null) box.enabled = on;
            if (obstacle != null) obstacle.enabled = on;
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.9f, 0.4f, 0.2f, 0.5f);
            Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);
            Gizmos.DrawWireCube(new Vector3(0f, height * 0.5f, 0f), new Vector3(thickness, height, width));
        }
    }
}
