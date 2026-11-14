// Battle of the Brooms: the twin-island Task Race map, one layout with two rules (docs 24). Left island = Team A,
// right island = Team B, the river channel between them. The prop/station layout is identical on both sides and only
// translated, never mirrored, so the mobile HUD (joystick bottom-left, action bottom-right, task list top-left) hides
// the same corners on both islands and never covers a task spot.
//
//   PureRace   barrier on  : the river bank colliders + hedges carve the NavMesh. No crossing; each team keeps to its island.
//   BridgeRaid barrier off : the north and south bridges switch on and link the two NavMesh islands, so a team can cross
//                            and contest TASK_LOCK on the opponent's stations.
//
// The manager also owns the S28 camera focus (open -> zoom + shift right, close -> back) and the "joystick past 70%
// closes the card" rule.
using System;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;
using Unity.AI.Navigation;

namespace Bob.TaskRace
{
    public enum TaskRaceMode { PureRace, BridgeRaid }

    [DisallowMultipleComponent, DefaultExecutionOrder(-50)]
    public class TaskRaceModeManager : MonoBehaviour
    {
        [Header("Map groups (built by Tools > BOB > Task Race Map Assembler)")]
        [Tooltip("River bank colliders, fence and hedge props with NavMeshObstacle (carve). Active in PureRace only.")]
        public GameObject riverBarrierGroup;
        [Tooltip("North and south bridges with a NavMeshLink. Active in BridgeRaid only.")]
        public GameObject bridgesGroup;

        [Header("NavMesh (baked by the scene builder, one per mode)")]
        [Tooltip("The surface on TaskRaceMap. Swapped between the two bakes when the mode changes.")]
        public NavMeshSurface navMeshSurface;
        [Tooltip("Bake with the bank barrier on and the bridges off: two separate islands.")]
        public NavMeshData pureNavMesh;
        [Tooltip("Bake with the bridges on: the two islands are linked.")]
        public NavMeshData raidNavMesh;

        [Header("Mode")]
        public TaskRaceMode mode = TaskRaceMode.PureRace;

        [Header("Camera")]
        [Tooltip("Filled automatically from the scene when empty.")]
        public BobTaskRaceCamera cameraRig;

        [Header("Joystick")]
        [Tooltip("The HUD joystick as a vector (length 1 = full tilt). Empty = keyboard axes for desktop testing.")]
        public Func<Vector2> joystickInput;
        [Tooltip("S28: joystick magnitude that closes an open minigame card immediately.")]
        [Range(0.3f, 1f)] public float cancelThreshold = 0.7f;

        [Header("Layout (optional, for inspection)")]
        public Transform teamA, teamB;

        public static TaskRaceModeManager Instance { get; private set; }
        public static event Action<TaskRaceMode> ModeChanged;

        bool cancelArmed = true;

        public bool BridgeRaid => mode == TaskRaceMode.BridgeRaid;

        void Awake()
        {
            Instance = this;
            if (cameraRig == null) cameraRig = FindFirstObjectByType<BobTaskRaceCamera>();
        }

        void OnEnable()
        {
            TaskSpot.AnyOpened += OnSpotOpened;
            TaskSpot.AnyClosed += OnSpotClosed;
        }

        void OnDisable()
        {
            TaskSpot.AnyOpened -= OnSpotOpened;
            TaskSpot.AnyClosed -= OnSpotClosed;
        }

        void Start()
        {
            if (cameraRig != null && cameraRig.target != null) TaskSpot.SetLocalPlayer(cameraRig.target);
            else
            {
                try
                {
                    var hero = GameObject.FindGameObjectWithTag("Player");
                    if (hero != null) TaskSpot.SetLocalPlayer(hero.transform);
                }
                catch (UnityException) { }
            }
            if (joystickInput == null)
            {
                var hero = FindAnyObjectByType<BobTaskRaceHero>();
                if (hero != null) joystickInput = () => hero.InputVector;
            }
            ApplyMode();
        }

        void Update()
        {
            float tilt = ReadJoystick();
            if (tilt > cancelThreshold && cancelArmed)
            {
                cancelArmed = false;
                TaskSpot.CancelOpenSessions();
            }
            else if (tilt < cancelThreshold * 0.4f) cancelArmed = true;
        }

        // ---------------------------------------------------------------- mode

        public void SetMode(TaskRaceMode next)
        {
            mode = next;
            ApplyMode();
        }

        public void ToggleMode() => SetMode(BridgeRaid ? TaskRaceMode.PureRace : TaskRaceMode.BridgeRaid);

        [ContextMenu("Mode: PureRace")]
        void ModePureRace() => SetMode(TaskRaceMode.PureRace);
        [ContextMenu("Mode: BridgeRaid")]
        void ModeBridgeRaid() => SetMode(TaskRaceMode.BridgeRaid);

        /// <summary>Show the right group, carve/disable the barrier, and make sure the active mode has its components.</summary>
        public void ApplyMode()
        {
            bool pure = mode == TaskRaceMode.PureRace;
            if (riverBarrierGroup != null) riverBarrierGroup.SetActive(pure);
            if (bridgesGroup != null) bridgesGroup.SetActive(!pure);

            if (pure) EnsureBarrierObstacles();
            else EnsureBridgeLinks();
            ApplyNavMesh(pure ? pureNavMesh : raidNavMesh);

            ModeChanged?.Invoke(mode);
            if (Application.isPlaying)
                Debug.Log($"[TaskRace] mode {mode}: barrier {(pure ? "on" : "off")}, bridges {(pure ? "off" : "on")}");
        }

        // ---------------------------------------------------------------- PureRace: carved bank barrier

        /// <summary>Swap the baked NavMesh to the one that matches the mode (two islands vs two linked islands).</summary>
        void ApplyNavMesh(NavMeshData data)
        {
            if (navMeshSurface == null || data == null || navMeshSurface.navMeshData == data) return;
            navMeshSurface.RemoveData();
            navMeshSurface.navMeshData = data;
            navMeshSurface.AddData();
            if (Application.isPlaying) Debug.Log($"[TaskRace] navmesh -> {data.name}");
        }

        void EnsureBarrierObstacles()
        {
            if (riverBarrierGroup == null) return;
            var colliders = riverBarrierGroup.GetComponentsInChildren<Collider>(true);
            int made = 0;
            foreach (var col in colliders)
            {
                if (col.isTrigger) continue; // trigger banks are for detection, not for blocking
                if (EnsureObstacle(col.gameObject)) made++;
            }
            if (Application.isPlaying && colliders.Length == 0)
                Debug.LogWarning("[TaskRace] RiverBarrier_Group has no colliders; PureRace cannot block the river.");
            if (Application.isPlaying && made > 0)
                Debug.Log($"[TaskRace] {made} barrier collider(s) now carve the NavMesh.");
        }

        static bool EnsureObstacle(GameObject go)
        {
            var obstacle = go.GetComponent<NavMeshObstacle>();
            bool added = obstacle == null;
            if (added) obstacle = go.AddComponent<NavMeshObstacle>();
            obstacle.shape = NavMeshObstacleShape.Box;
            obstacle.carving = true;
            obstacle.carveOnlyStationary = true;

            GetWorldBox(go, out var worldSize, out var worldCentre);
            var scale = go.transform.lossyScale;
            obstacle.size = new Vector3(
                worldSize.x / Mathf.Max(0.0001f, Mathf.Abs(scale.x)),
                worldSize.y / Mathf.Max(0.0001f, Mathf.Abs(scale.y)),
                worldSize.z / Mathf.Max(0.0001f, Mathf.Abs(scale.z)));
            obstacle.center = go.transform.InverseTransformPoint(worldCentre);
            return added;
        }

        // ---------------------------------------------------------------- BridgeRaid: links between the islands

        void EnsureBridgeLinks()
        {
            if (bridgesGroup == null) return;
            foreach (Transform child in bridgesGroup.transform) EnsureBridgeLink(child.gameObject);
        }

        /// <summary>One link per bridge, spanning its longest horizontal axis at the walk surface.</summary>
        static void EnsureBridgeLink(GameObject bridge)
        {
            if (!GetWorldBox(bridge, out var size, out var centre)) return;

            bool alongX = size.x >= size.z;
            float half = (alongX ? size.x : size.z) * 0.5f;
            float y = centre.y + size.y * 0.5f + 0.05f;
            var a = alongX ? new Vector3(centre.x - half, y, centre.z) : new Vector3(centre.x, y, centre.z - half);
            var b = alongX ? new Vector3(centre.x + half, y, centre.z) : new Vector3(centre.x, y, centre.z + half);

            var start = Marker(bridge.transform, "LinkStart", a);
            var end = Marker(bridge.transform, "LinkEnd", b);

            var link = bridge.GetComponent<NavMeshLink>();
            if (link == null) link = bridge.AddComponent<NavMeshLink>();
            link.startPoint = bridge.transform.InverseTransformPoint(start.position);
            link.endPoint = bridge.transform.InverseTransformPoint(end.position);
            link.width = 1.6f;
            link.bidirectional = true;
            link.costModifier = 0f;
            link.activated = true;
        }

        static Transform Marker(Transform parent, string name, Vector3 worldPos)
        {
            var t = parent.Find(name);
            if (t == null)
            {
                var go = new GameObject(name);
                go.transform.SetParent(parent, false);
                t = go.transform;
            }
            t.position = worldPos;
            return t;
        }

        /// <summary>Combined collider/renderer bounds in world space; false when there is nothing to measure.</summary>
        static bool GetWorldBox(GameObject go, out Vector3 size, out Vector3 centre)
        {
            size = Vector3.zero; centre = Vector3.zero;
            var col = go.GetComponent<Collider>();
            if (col != null)
            {
                if (col is BoxCollider box)
                {
                    var s = go.transform.lossyScale;
                    size = new Vector3(Mathf.Abs(box.size.x * s.x), Mathf.Abs(box.size.y * s.y), Mathf.Abs(box.size.z * s.z));
                    centre = go.transform.TransformPoint(box.center);
                    return true;
                }
                centre = col.bounds.center; size = col.bounds.size;
                return true;
            }
            var renderers = go.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0) return false;
            var bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            centre = bounds.center; size = bounds.size;
            return true;
        }

        // ---------------------------------------------------------------- input + camera

        float ReadJoystick()
        {
            if (joystickInput != null) return Mathf.Clamp01(joystickInput().magnitude);
            return Mathf.Clamp01(new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")).magnitude);
        }

        void OnSpotOpened(TaskSpot spot)
        {
            if (cameraRig != null) cameraRig.SetMinigameFocus(true);
        }

        void OnSpotClosed(TaskSpot spot)
        {
            if (cameraRig != null) cameraRig.SetMinigameFocus(false);
        }

        void OnValidate()
        {
            cancelThreshold = Mathf.Clamp(cancelThreshold, 0.3f, 1f);
        }

        void OnDrawGizmos()
        {
            if (teamA != null)
            {
                Gizmos.color = new Color(0.25f, 0.65f, 0.95f, 0.5f);
                Gizmos.DrawWireCube(teamA.position, new Vector3(8f, 0.2f, 8f));
            }
            if (teamB != null)
            {
                Gizmos.color = new Color(0.95f, 0.45f, 0.35f, 0.5f);
                Gizmos.DrawWireCube(teamB.position, new Vector3(8f, 0.2f, 8f));
            }
        }
    }
}
