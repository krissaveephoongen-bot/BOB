// Battle of the Brooms: the twin-island Task Race map, one layout with two rules (docs 24). Left island = Team A,
// right island = Team B, the river channel between them. The prop/station layout is identical on both sides and only
// translated, never mirrored, so the mobile HUD (joystick bottom-left, action bottom-right, task list top-left) hides
// the same corners on both islands and never covers a task spot.
//
//   PureRace        the red-white gates stay shut for the whole 120 s match: each team keeps to its island.
//   TimedBridgeRaid the gates shake at 65 s remaining and drop 1.5 u at 60 s, the deck NavMeshLinks switch on, and the
//                   teams may cross to sabotage the opponent's clean stations.
//
// The match clock, the shared team point pools (100 per completed task, 150 for a 30 s shield) and the atomic shield
// purchase live here; the spots own the reach rules. The manager also owns the S28 camera focus (open -> zoom + shift
// right, close -> back) and the "joystick past 70% closes the card" rule.
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;

namespace Bob.TaskRace
{
    public enum TaskRaceMode { PureRace, TimedBridgeRaid }

    [DisallowMultipleComponent, DefaultExecutionOrder(-50)]
    public class TaskRaceModeManager : MonoBehaviour
    {
        [Header("Map groups (built by Tools > BOB > Task Race Map Assembler)")]
        [Tooltip("River bank colliders, fence and hedge props with NavMeshObstacle (carve). Active in PureRace only.")]
        public GameObject riverBarrierGroup;
        [Tooltip("North and south bridges with a NavMeshLink. Active in TimedBridgeRaid only.")]
        public GameObject bridgesGroup;
        [Tooltip("The red-white gates. Always active: they block until the TimedBridgeRaid clock reaches 60 s.")]
        public GameObject bridgeBarrierGroup;

        [Header("NavMesh (baked by the scene builder, one per mode)")]
        [Tooltip("The surface on TaskRaceMap. Swapped between the two bakes when the mode changes.")]
        public NavMeshSurface navMeshSurface;
        [Tooltip("Bake with the bank barrier on and the bridges off: two separate islands.")]
        public NavMeshData pureNavMesh;
        [Tooltip("Bake with the bridges on: the two islands are linked.")]
        public NavMeshData raidNavMesh;

        [Header("Mode")]
        public TaskRaceMode mode = TaskRaceMode.TimedBridgeRaid;
        [Tooltip("Which island the local player defends. Team A by default (the left island).")]
        public TeamId localTeam = TeamId.TeamA;

        [Header("Match clock (docs 24 section 12)")]
        [Tooltip("A match lasts 120 s. The clock runs in both modes.")]
        [SerializeField, Min(10f)] public float matchDuration = 120f;
        [Tooltip("Seconds remaining when the 5 s warning starts (barrier shake + HUD warning).")]
        [SerializeField, Min(1f)] public float warningAtRemaining = 65f;
        [Tooltip("Seconds remaining when the gates drop and the bridges open.")]
        [SerializeField, Min(1f)] public float openAtRemaining = 60f;
        [Tooltip("How far the red-white gates drop on Y when the bridges open.")]
        [SerializeField, Min(0.1f)] public float barrierDropHeight = 1.5f;
        [Tooltip("Seconds for the drop.")]
        [SerializeField, Min(0.05f)] public float barrierDropSeconds = 0.6f;
        [Tooltip("Length of the warning shake before the drop.")]
        [SerializeField, Min(0f)] public float warningShakeSeconds = 5f;

        [Header("Team points (shared per team)")]
        [Tooltip("Awarded on every Dirty -> Clean completion, including a re-clean after a sabotage.")]
        [SerializeField, Min(0)] public int pointsPerTaskComplete = 100;
        [Tooltip("Cost of the timed station shield.")]
        [SerializeField, Min(0)] public int shieldItemCost = 150;
        [Tooltip("Length of the purchased shield.")]
        [SerializeField, Min(1f)] public float shieldSeconds = 30f;
        [SerializeField] int teamAPoints, teamBPoints;

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

        static TaskRaceModeManager cached;

        /// <summary>The scene's manager. Edit-mode verify and the debug bar reach it without Awake running.</summary>
        public static TaskRaceModeManager Instance
        {
            get
            {
                if (cached == null) cached = FindAnyObjectByType<TaskRaceModeManager>();
                return cached;
            }
        }

        public static event Action<TaskRaceMode> ModeChanged;

        /// <summary>Raised after a pool changes: (team, new total). The HUD polls PointsOf as well.</summary>
        public event Action<TeamId, int> PointsChanged;
        /// <summary>65 s remaining: the barriers start to shake. The HUD shows the warning toast.</summary>
        public event Action BridgesWillOpen;
        /// <summary>The gates have dropped and the bridges are walkable.</summary>
        public event Action BridgesOpened;
        /// <summary>Any one-line message the HUD should toast.</summary>
        public event Action<string> ToastRequested;

        readonly List<BobBridgeBarrier> barriers = new List<BobBridgeBarrier>();
        TaskRaceRound round;
        bool warned, opened, cancelArmed = true;

        /// <summary>Seconds left on the match clock. The top HUD bar reads this.</summary>
        public float RemainingSeconds { get; private set; }
        public bool IsMatchOver => RemainingSeconds <= 0f;
        /// <summary>True once the gates have dropped: the only time sabotage is available.</summary>
        public bool AreBridgesOpen => opened;
        public bool BridgeRaid => mode == TaskRaceMode.TimedBridgeRaid;
        public int TeamAPoints => teamAPoints;
        public int TeamBPoints => teamBPoints;
        public IReadOnlyList<BobBridgeBarrier> Barriers => barriers;

        public int PointsOf(TeamId team) =>
            team == TeamId.TeamB ? teamBPoints : team == TeamId.TeamA ? teamAPoints : 0;

        public int LocalPoints => PointsOf(localTeam);

        void Awake()
        {
            cached = this;
            RemainingSeconds = matchDuration;
            if (cameraRig == null) cameraRig = FindFirstObjectByType<BobTaskRaceCamera>();
            CollectBarriers();
        }

        void OnDestroy()
        {
            if (cached == this) cached = null;
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
            TaskSpot.LocalTeam = localTeam;
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
            round = FindAnyObjectByType<TaskRaceRound>();
            ApplyMode();
            BeginMatch();
        }

        void Update()
        {
            if (Application.isPlaying && !IsMatchOver)
            {
                RemainingSeconds = Mathf.Max(0f, RemainingSeconds - Time.deltaTime);
                TickClock();
            }

            float tilt = ReadJoystick();
            if (tilt > cancelThreshold && cancelArmed)
            {
                cancelArmed = false;
                TaskSpot.CancelOpenSessions();
            }
            else if (tilt < cancelThreshold * 0.4f) cancelArmed = true;
        }

        // ---------------------------------------------------------------- match

        /// <summary>Restart the score and the clock: 120 s, gates shut, both pools at zero.</summary>
        public void BeginMatch()
        {
            teamAPoints = 0; teamBPoints = 0;
            RemainingSeconds = matchDuration;
            warned = opened = false;
            TaskSpot.LocalTeam = localTeam;
            CloseBarriers();
            PointsChanged?.Invoke(TeamId.TeamA, teamAPoints);
            PointsChanged?.Invoke(TeamId.TeamB, teamBPoints);
            if (Application.isPlaying) Debug.Log($"[TaskRace] match start: {matchDuration:0} s, {mode}, local team {localTeam}.");
        }

        void TickClock()
        {
            if (mode != TaskRaceMode.TimedBridgeRaid) return;

            if (!warned && RemainingSeconds <= warningAtRemaining)
            {
                warned = true;
                ShakeBarriers();
                BridgesWillOpen?.Invoke();
                ToastRequested?.Invoke("สะพานจะเปิดใน 5 วินาที!");
                if (Application.isPlaying)
                    Debug.Log($"[TaskRace] warning at {RemainingSeconds:0.0} s remaining: {barriers.Count} barrier(s) shaking.");
            }

            if (!opened && RemainingSeconds <= openAtRemaining) OpenBridges();
        }

        /// <summary>Drop the gates and switch the bridge NavMeshLinks on. Debug can call this at any time.</summary>
        public void OpenBridges()
        {
            if (opened) return;
            opened = true;
            if (bridgesGroup != null && !bridgesGroup.activeSelf) bridgesGroup.SetActive(true);
            EnsureBridgeLinks();
            foreach (var barrier in barriers) barrier.Open();
            BridgesOpened?.Invoke();
            ToastRequested?.Invoke("สะพานเปิดแล้ว! ข้ามไปป่วนจุดงานคู่แข่งได้!");
            if (Application.isPlaying)
                Debug.Log($"[TaskRace] bridges open at {RemainingSeconds:0.0} s remaining: {barriers.Count} gate(s) lowered " +
                          $"{barrierDropHeight:0.##} u over {barrierDropSeconds:0.##} s.");
        }

        public void CloseBarriers()
        {
            opened = false;
            warned = false;
            foreach (var barrier in barriers) barrier.CloseInstant();
        }

        void ShakeBarriers()
        {
            foreach (var barrier in barriers) barrier.Shake(warningShakeSeconds);
        }

        void CollectBarriers()
        {
            barriers.Clear();
            if (bridgeBarrierGroup != null)
                barriers.AddRange(bridgeBarrierGroup.GetComponentsInChildren<BobBridgeBarrier>(true));
            if (barriers.Count == 0)
                barriers.AddRange(FindObjectsByType<BobBridgeBarrier>(FindObjectsInactive.Include));
            foreach (var barrier in barriers)
                barrier.Configure(barrier.Width, barrierDropHeight, barrierDropSeconds);
        }

        // ---------------------------------------------------------------- points / items

        /// <summary>Every Dirty -> Clean completion runs through here (TaskSpot.Close).</summary>
        public void OnTaskCleaned(TaskSpot spot)
        {
            if (spot == null) return;
            var team = spot.OwnerTeam == TeamId.None ? localTeam : spot.OwnerTeam;
            AddPoints(team, pointsPerTaskComplete);
            if (Application.isPlaying) Debug.Log($"[TaskRace] +{pointsPerTaskComplete} to {team} ({PointsOf(team)}) for {spot.SpotId}.");
        }

        public void AddPoints(TeamId team, int amount)
        {
            if (amount == 0) return;
            if (team == TeamId.TeamB) teamBPoints = Mathf.Max(0, teamBPoints + amount);
            else if (team == TeamId.TeamA) teamAPoints = Mathf.Max(0, teamAPoints + amount);
            else return;
            PointsChanged?.Invoke(team, PointsOf(team));
        }

        /// <summary>
        /// Atomic shield purchase: one check-then-deduct step, so two taps in the same frame cannot spend the same
        /// 150 points twice. Returns false without changing anything when the pool, the station or the state is wrong.
        /// </summary>
        public bool TryPurchaseShield(TeamId team, TaskSpot targetSpot)
        {
            if (targetSpot == null) return false;
            if (team == TeamId.None) team = localTeam;
            if (targetSpot.OwnerTeam != team) return false;
            if (targetSpot.State != SpotState.Clean) return false;
            if (targetSpot.IsProtected || targetSpot.SabotageUsed) return false;
            if (PointsOf(team) < shieldItemCost) return false;

            AddPoints(team, -shieldItemCost);
            targetSpot.ApplyShield(shieldSeconds);
            ToastRequested?.Invoke($"ติดเกราะป้องกัน {shieldSeconds:0} วิแล้ว!");
            if (Application.isPlaying)
                Debug.Log($"[TaskRace] {team} bought a {shieldSeconds:0} s shield on {targetSpot.SpotId} for {shieldItemCost}.");
            return true;
        }

        /// <summary>The round drops the defending count by one and puts the same task back on the station.</summary>
        public void OnSpotSabotaged(TaskSpot spot)
        {
            if (spot == null) return;
            if (round == null) round = FindAnyObjectByType<TaskRaceRound>();
            round?.HandleSabotage(spot);
            ToastRequested?.Invoke($"คราบกลับมา! จุดงาน {spot.TaskName} ถูกคู่แข่งป่วน!");
            if (Application.isPlaying)
                Debug.Log($"[TaskRace] sabotage on {spot.SpotId} ({spot.OwnerTeam}): state Dirty again.");
        }

        // ---------------------------------------------------------------- mode

        public void SetMode(TaskRaceMode next)
        {
            mode = next;
            ApplyMode();
        }

        public void ToggleMode() => SetMode(BridgeRaid ? TaskRaceMode.PureRace : TaskRaceMode.TimedBridgeRaid);

        [ContextMenu("Mode: PureRace")]
        void ModePureRace() => SetMode(TaskRaceMode.PureRace);
        [ContextMenu("Mode: TimedBridgeRaid")]
        void ModeTimedBridgeRaid() => SetMode(TaskRaceMode.TimedBridgeRaid);

        /// <summary>Show the right group, carve/disable the barrier, and make sure the active mode has its components.</summary>
        public void ApplyMode()
        {
            // Awake does not run for scene objects in the editor, so the verify script and the debug bar need the
            // barrier list built here too (it is idempotent: Configure only resizes the existing blockers).
            CollectBarriers();
            bool pure = mode == TaskRaceMode.PureRace;
            if (riverBarrierGroup != null) riverBarrierGroup.SetActive(pure);
            if (bridgesGroup != null) bridgesGroup.SetActive(!pure);
            if (bridgeBarrierGroup != null) bridgeBarrierGroup.SetActive(true);

            if (pure) EnsureBarrierObstacles();
            else EnsureBridgeLinks();
            ApplyNavMesh(pure ? pureNavMesh : raidNavMesh);
            CloseBarriers();

            ModeChanged?.Invoke(mode);
            if (Application.isPlaying)
                Debug.Log($"[TaskRace] mode {mode}: barrier {(pure ? "on" : "off")}, bridges {(pure ? "off" : "on")}, gates closed.");
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

        // ---------------------------------------------------------------- TimedBridgeRaid: links between the islands

        void EnsureBridgeLinks()
        {
            if (bridgesGroup == null) return;
            foreach (Transform child in bridgesGroup.transform) EnsureBridgeLink(child.gameObject);
        }

        /// <summary>One link per bridge, spanning its longest horizontal axis at the walk surface.</summary>
        static void EnsureBridgeLink(GameObject bridge)
        {
            var walkable = bridge.transform.Find("Walkable");
            GameObject measure = walkable != null ? walkable.gameObject : bridge;
            if (!GetWorldBox(measure, out var size, out var centre)) return;

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

        // ---------------------------------------------------------------- debug test bar

        /// <summary>Debug bar: jump the clock. Jumping above the open point shuts the gates again for a repeat test.</summary>
        public void DebugSetRemaining(float seconds)
        {
            RemainingSeconds = Mathf.Clamp(seconds, 0f, matchDuration);
            if (RemainingSeconds > warningAtRemaining) { warned = false; }
            if (RemainingSeconds > openAtRemaining && opened)
            {
                CloseBarriers();
                if (Application.isPlaying) Debug.Log("[TaskRace] debug: clock reset above 60 s, gates closed again.");
            }
            // Update is the only other caller, so the debug bar and the verify script move the clock crossings here too.
            TickClock();
        }

        public void DebugAddTime(float seconds) => DebugSetRemaining(RemainingSeconds + seconds);

        public void DebugOpenBridges() => OpenBridges();

        public void DebugAddPoints(int amount) => AddPoints(localTeam, amount);

        /// <summary>Force a sabotage on the first clean enemy station (bridge state and lock are ignored).</summary>
        public bool DebugSabotageNearest()
        {
            var target = FindObjectsByType<TaskSpot>(FindObjectsInactive.Exclude)
                .Where(s => s.IsEnemySpot && s.State == SpotState.Clean && !s.SabotageUsed)
                .OrderBy(s => s.SpotId)
                .FirstOrDefault();
            if (target == null) return false;
            bool ok = target.DebugForceSabotage();
            if (Application.isPlaying) Debug.Log($"[TaskRace] debug sabotage {(ok ? "ok" : "failed")} on {target.SpotId}.");
            return ok;
        }

        /// <summary>Force a free shield on the nearest clean own station.</summary>
        public bool DebugShieldNearest()
        {
            var target = FindObjectsByType<TaskSpot>(FindObjectsInactive.Exclude)
                .Where(s => !s.IsEnemySpot && s.State == SpotState.Clean && !s.IsProtected)
                .OrderBy(s => s.SpotId)
                .FirstOrDefault();
            if (target == null) return false;
            target.ApplyShield(shieldSeconds);
            ToastRequested?.Invoke($"ติดเกราะป้องกัน {shieldSeconds:0} วิแล้ว!");
            return true;
        }

        public void DebugResetMatch()
        {
            BeginMatch();
            if (round == null) round = FindAnyObjectByType<TaskRaceRound>();
            round?.StartRound(UnityEngine.Random.Range(1, 100000));
            if (Application.isPlaying) Debug.Log("[TaskRace] debug reset: score 0, clock 120 s, gates shut, round restarted.");
        }

        void OnValidate()
        {
            cancelThreshold = Mathf.Clamp(cancelThreshold, 0.3f, 1f);
            matchDuration = Mathf.Max(10f, matchDuration);
            openAtRemaining = Mathf.Clamp(openAtRemaining, 1f, matchDuration);
            warningAtRemaining = Mathf.Clamp(warningAtRemaining, openAtRemaining, matchDuration);
            pointsPerTaskComplete = Mathf.Max(0, pointsPerTaskComplete);
            shieldItemCost = Mathf.Max(0, shieldItemCost);
            shieldSeconds = Mathf.Max(1f, shieldSeconds);
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
