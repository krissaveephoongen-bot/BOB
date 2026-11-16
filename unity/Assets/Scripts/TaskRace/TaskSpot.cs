// Battle of the Brooms: a task spot for the Task Race map (docs 16 section 3, F29). Drop it on a station prop
// (espresso machine, sink counter, washing machine, cafe table ...). It builds the ground ring and the compact world
// label itself, owns the TASK_LOCK, the 10-second partial-progress retention, the Dirty/Clean state, the one-time
// sabotage of a clean station and the timed shield, and raises events for the HUD and the round.
//
// The component never builds UI: the game listens to NearChanged / Opened / Closed / Blocked / Sabotaged and drives the
// main context button ("ทำงาน: <TaskName>" / "ป่วนจุดงาน: <TaskName>" / "ติดเกราะป้องกัน (-150 Pts)"), the S28 card and
// the toast. Activation and the card live in the game layer, the reach rules live here.
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Bob.TaskRace
{
    /// <summary>The two islands; a task spot, a point pool and a player all belong to one.</summary>
    public enum TeamId { None, TeamA, TeamB }

    /// <summary>The five task families of the F29 ground ring (docs 16 section 3).</summary>
    public enum TaskCategory { Sweep, Wash, Scrub, Spray, Everyone }

    /// <summary>A station is Dirty while it has work, Clean after its task is done (and until it is restocked or sabotaged).</summary>
    public enum SpotState { Dirty, Clean }

    [DisallowMultipleComponent]
    public class TaskSpot : MonoBehaviour
    {
        [Header("Task (docs 16 section 3)")]
        [Tooltip("Unique on the map, e.g. S3_A. The assembler appends _B when the Team A layout is cloned.")]
        [SerializeField] string spotId = "S1_A";
        [SerializeField] string taskName = "งาน";
        [Tooltip("Minigame id of docs 16 (T01..T16). The round writes it when it hands this station its task; the S28 card picks its interaction from it.")]
        [SerializeField] string taskId = "";
        [Tooltip("Off when the round has no task left for this station: no ring, no label, cannot be opened.")]
        [SerializeField] bool hasTask = true;
        [SerializeField] TaskCategory category = TaskCategory.Everyone;
        [Tooltip("Estimated duration shown in the preview label, in seconds.")]
        [SerializeField, Min(1f)] float expectedSeconds = 12f;

        [Header("Team / state")]
        [Tooltip("Left island = TeamA, right island = TeamB. Set on the clone by the assembler.")]
        [SerializeField] TeamId ownerTeam = TeamId.TeamA;
        [SerializeField] SpotState state = SpotState.Dirty;
        [Tooltip("One sabotage per station per match; a sabotaged station is permanently immune afterwards.")]
        public const int MaxSabotagesPerMatch = 1;
        [SerializeField, Min(0)] int _sabotageCount;
        [Tooltip("Completed passes a task needs before the station is clean. Stage 4 sets stain stations to 2.")]
        [SerializeField, Min(1)] int requiredPasses = 1;
        [SerializeField, Min(0)] int passesDone;

        [Header("Shield (timed)")]
        [SerializeField] bool isProtected;
        [SerializeField, Min(1f)] float shieldDuration = 30f;
        [SerializeField, Min(0f)] float _shieldTimer;

        [Header("Reach (flat X-Z world units)")]
        [Tooltip("Compact label shows inside this radius (docs 19: 2.6).")]
        [SerializeField] float previewRadius = 2.6f;
        [Tooltip("NEAR: ring highlights and the main button turns into the action (docs 16: 1.2).")]
        [SerializeField] float interactRadius = 1.2f;

        [Header("Player")]
        [Tooltip("Local hero. Empty = the map's local player / the GameObject tagged Player.")]
        [SerializeField] Transform player;
        [Tooltip("Display name used in the TASK_LOCK toast for other players (\"มายด์กำลังทำอยู่...\").")]
        [SerializeField] string playerName = "ฉัน";
        [Tooltip("Seconds a closed card keeps its partial progress (S28).")]
        [SerializeField] float retainedProgressSeconds = 10f;

        [Header("Ground ring / label (F29)")]
        [SerializeField] bool buildVisuals = true;
        [Tooltip("Height of the compact label above the ring anchor.")]
        [SerializeField] float labelHeight = 1.45f;
        [Tooltip("Floor point for the ring and label. Empty = this transform (put it on the floor, not on a counter top).")]
        [SerializeField] Transform ringAnchor;

        [Header("Floating category sign (4.3)")]
        [Tooltip("Optional sprite override; empty = the category icon under Resources/Icons.")]
        [SerializeField] Sprite iconSprite;
        [Tooltip("Height of the floating icon above the ring anchor.")]
        [SerializeField] float iconHeight = 2.05f;
        [Tooltip("World size of the icon sign.")]
        [SerializeField] float iconSize = 0.78f;

        [Header("Feedback")]
        [Tooltip("ping_pop for a successful sabotage (docs 12); optional until the SFX pack is imported.")]
        [SerializeField] AudioClip pingPop;

        const int RingSegments = 48;
        static readonly Color ShieldGold = new Color(1f, 0.78f, 0.22f);

        // ---------------------------------------------------------------- events for the game / HUD

        /// <summary>Raised when the hero enters or leaves the NEAR circle. Drives the main context button.</summary>
        public event Action<TaskSpot, bool> NearChanged;
        /// <summary>Raised after TASK_LOCK is taken and the S28 card may open.</summary>
        public event Action<TaskSpot> Opened;
        /// <summary>Raised after the card closes. true = task completed.</summary>
        public event Action<TaskSpot, bool> Closed;
        /// <summary>Raised when the spot cannot open (already done, or locked by another player). Show `message` as a toast.</summary>
        public event Action<TaskSpot, string> Blocked;
        /// <summary>Raised after a successful sabotage: the station is Dirty again.</summary>
        public event Action<TaskSpot> Sabotaged;
        /// <summary>Global hooks for the mode manager: camera focus on open, unfocus on close.</summary>
        public static event Action<TaskSpot> AnyOpened;
        public static event Action<TaskSpot> AnyClosed;
        public static event Action<TaskSpot> AnySabotaged;

        // ---------------------------------------------------------------- state

        static readonly List<TaskSpot> All = new List<TaskSpot>();
        static Transform localPlayer;
        static int reportFrame = -1;
        static float reportBest;
        static TaskSpot reportSpot;

        /// <summary>The closest spot inside its NEAR radius this frame; the HUD reads this for the main button.</summary>
        public static TaskSpot ActiveNear { get; private set; }
        /// <summary>The local player's team, set by the mode manager (TeamA by default).</summary>
        public static TeamId LocalTeam { get; set; } = TeamId.TeamA;

        public string SpotId => spotId;
        public string TaskName => taskName;
        /// <summary>Minigame id (T01..T16, docs 16) the round dealt to this station.</summary>
        public string TaskId => taskId;
        public TaskCategory Category => category;
        public float ExpectedSeconds => expectedSeconds;
        public float PreviewRadius => previewRadius;
        public float InteractRadius => interactRadius;
        public TeamId OwnerTeam => ownerTeam;
        public SpotState State => state;
        public bool IsProtected => isProtected;
        public float ShieldRemaining => isProtected ? Mathf.Max(0f, _shieldTimer) : 0f;
        public float ShieldDuration => shieldDuration;
        public int SabotageCount => _sabotageCount;
        public bool SabotageUsed => _sabotageCount >= MaxSabotagesPerMatch;
        /// <summary>Passes this task needs (stage 4 stains: 2) and the passes already banked.</summary>
        public int RequiredPasses => Mathf.Max(1, requiredPasses);
        public int PassesDone => passesDone;
        /// <summary>True when the last card closed as a completed pass (a two-pass first pass counts as completed).</summary>
        public bool LastCardCompleted { get; private set; }
        public bool HasTask => hasTask;
        public bool IsNear { get; private set; }
        public bool IsOpen { get; private set; }
        public bool IsLocked => IsOpen;
        public string LockedBy { get; private set; }
        /// <summary>Clean (task done). The round reads this like the old `Done` flag.</summary>
        public bool Done => state == SpotState.Clean;
        public float Progress { get; private set; }
        /// <summary>Progress still inside the 10-second retention window; 0 when it expired.</summary>
        public float RetainedProgress => retained && Time.unscaledTime < retainUntil ? Progress : 0f;
        public Transform Player => player;
        /// <summary>A clean station of the opponent can be sabotaged while the bridges are open.</summary>
        public bool IsEnemySpot => LocalTeam != ownerTeam;

        bool retained;
        float retainUntil;
        bool sabotageSession;

        LineRenderer ring;
        TextMeshPro label, lockBadge, shieldBadge;
        SpriteRenderer icon;
        readonly Vector3[] ringPoints = new Vector3[RingSegments];

        // ---------------------------------------------------------------- lifecycle

        void Awake()
        {
            if (string.IsNullOrEmpty(spotId)) spotId = name;
            if (buildVisuals) BuildVisuals();
        }

        void OnEnable()
        {
            if (!All.Contains(this)) All.Add(this);
        }

        void OnDisable()
        {
            All.Remove(this);
            if (ActiveNear == this) ActiveNear = null;
        }

        void Start()
        {
            if (player == null && localPlayer == null)
            {
                var go = FindPlayer();
                if (go != null) localPlayer = go.transform;
            }
        }

        void Update()
        {
            if (retained && Time.unscaledTime >= retainUntil) { retained = false; Progress = 0f; }
            if (isProtected) TickShield(Time.deltaTime);

            var t = ResolvePlayer();
            float dist = float.MaxValue;
            bool near = false;
            if (t != null)
            {
                var d = t.position - transform.position; d.y = 0f;
                dist = d.magnitude;
                // Clean stations stay "near" so the HUD can offer the shield / sabotage action there.
                near = hasTask && dist <= interactRadius;
            }

            if (near != IsNear) { IsNear = near; NearChanged?.Invoke(this, near); }
            Report(this, near, dist);
            RefreshVisuals(near, dist);
        }

        // ---------------------------------------------------------------- public API (game / HUD / server)

        /// <summary>Set by the map manager once the local hero exists.</summary>
        public static void SetLocalPlayer(Transform hero) => localPlayer = hero;

        /// <summary>Assembler: the left island is TeamA, the cloned right island TeamB.</summary>
        public void SetOwnerTeam(TeamId team) => ownerTeam = team;

        /// <summary>Try to take TASK_LOCK and open the S28 card. `who` is the player's display name.
        /// Returns false and raises Blocked when the spot is done or another player is working here.</summary>
        public bool TryOpen(string who, out string message)
        {
            who = string.IsNullOrEmpty(who) ? playerName : who;
            if (!hasTask)
            {
                message = "จุดนี้ไม่มีงานแล้ว";
                Blocked?.Invoke(this, message);
                return false;
            }
            if (state == SpotState.Clean)
            {
                message = taskName + " สะอาดแล้ว";
                Blocked?.Invoke(this, message);
                return false;
            }
            if (IsOpen)
            {
                if (LockedBy == who) { message = null; return false; }
                message = LockedBy + " กำลังทำอยู่ ไปจุดอื่นนะ";
                Blocked?.Invoke(this, message);
                return false;
            }
            // The 3-2-1 countdown and the result hold the clock: work started there would be free time.
            var m = TaskRaceModeManager.Instance;
            if (m != null && (m.IsCountingDown || m.LastResult.HasValue))
            {
                message = "รอสัญญาณ ลุย! ก่อน";
                Blocked?.Invoke(this, message);
                return false;
            }
            // Stations of the other island belong to the rival: only a sabotage may touch them.
            if (ownerTeam != TeamId.None && LocalTeam != TeamId.None && ownerTeam != LocalTeam)
            {
                message = "จุดนี้ของฝั่งคู่แข่ง";
                Blocked?.Invoke(this, message);
                return false;
            }

            IsOpen = true; LockedBy = who; sabotageSession = false; LastCardCompleted = false; message = null;
            Opened?.Invoke(this); AnyOpened?.Invoke(this);
            RefreshRing();
            return true;
        }

        /// <summary>Open with this spot's own player name.</summary>
        public bool Open() => TryOpen(playerName, out _);

        /// <summary>Close the card. Completed turns the spot Clean; otherwise the partial progress is kept for
        /// `retainedProgressSeconds` (S28: close with × or by pushing the joystick past 70%).</summary>
        public void Close(bool completed)
        {
            if (!IsOpen) return;
            IsOpen = false; LockedBy = null; sabotageSession = false;
            LastCardCompleted = completed;
            if (completed)
            {
                // Stage 4: a stain station banks the pass and stays Dirty until the second pass lands.
                if (RequiredPasses > 1 && passesDone + 1 < RequiredPasses)
                {
                    passesDone++;
                    state = SpotState.Dirty; Progress = 0f; retained = false;
                    TaskRaceModeManager.Instance?.OnTaskPass(this);
                    Closed?.Invoke(this, false); AnyClosed?.Invoke(this);
                    RefreshRing();
                    return;
                }
                passesDone = 0;
                state = SpotState.Clean; Progress = 1f; retained = false;
                // Clean/Dirty is the only place points are awarded: every completion, including a re-clean after
                // a sabotage, runs through here.
                TaskRaceModeManager.Instance?.OnTaskCleaned(this);
            }
            else if (retainedProgressSeconds > 0f)
            {
                retained = true; retainUntil = Time.unscaledTime + retainedProgressSeconds;
            }
            Closed?.Invoke(this, completed); AnyClosed?.Invoke(this);
            RefreshRing();
        }

        /// <summary>
        /// The AI rival's clean: the same state change as a completed card, without a TASK_LOCK or a card. Points and
        /// the round's count both run through the normal Closed path, so nothing else has to know who cleaned.
        /// </summary>
        public bool ForceComplete()
        {
            // A station somebody is working on is theirs: the rival waits for another one instead of wiping the card.
            if (IsOpen) return false;
            if (!hasTask || state == SpotState.Clean) return false;
            IsOpen = false; LockedBy = null; sabotageSession = false;
            LastCardCompleted = true;
            if (RequiredPasses > 1 && passesDone + 1 < RequiredPasses)
            {
                passesDone++;
                state = SpotState.Dirty; Progress = 0f; retained = false;
                TaskRaceModeManager.Instance?.OnTaskPass(this);
                Closed?.Invoke(this, false); AnyClosed?.Invoke(this);
                RefreshRing();
                return true;
            }
            passesDone = 0;
            state = SpotState.Clean; Progress = 1f; retained = false;
            TaskRaceModeManager.Instance?.OnTaskCleaned(this);
            Closed?.Invoke(this, true); AnyClosed?.Invoke(this);
            RefreshRing();
            return true;
        }

        /// <summary>HUD: show whether the opponent may sabotage this station right now (the bridges and the state are
        /// checked by the manager; `isProtected` is deliberately not part of "may try" so the tap can explain the shield).</summary>
        public bool CanSabotageNow
        {
            get
            {
                var m = TaskRaceModeManager.Instance;
                return m != null && m.AreBridgesOpen && IsEnemySpot && hasTask
                       && state == SpotState.Clean && !SabotageUsed;
            }
        }

        /// <summary>Enemy near a clean spot while the bridges are closed: the prompt stays hidden (docs: interact only
        /// when the bridges are open).</summary>
        public bool ShowSabotagePrompt => IsNear && CanSabotageNow;

        /// <summary>Owner near a clean spot that can still be shielded. Hidden once the sabotage is spent (permanent
        /// immunity for the rest of the match, docs 19).</summary>
        public bool ShowShieldPrompt
        {
            get
            {
                var m = TaskRaceModeManager.Instance;
                return IsNear && m != null && !IsEnemySpot && hasTask && state == SpotState.Clean
                       && !isProtected && !SabotageUsed;
            }
        }

        /// <summary>The shield prompt is shown but greyed while the pool is below the cost.</summary>
        public bool CanAffordShield
        {
            get
            {
                var m = TaskRaceModeManager.Instance;
                return m != null && m.PointsOf(LocalTeam) >= m.shieldItemCost;
            }
        }

        /// <summary>Try to take the sabotage TASK_LOCK. The HUD opens the S28 card when this returns true.</summary>
        public bool TrySabotage(string who, out string message) => TrySabotageAs(LocalTeam, who, out message);

        /// <summary>The same lock from a given attacker team: the solo bot raids Team A while the player is Team A, so the
        /// player-relative <see cref="IsEnemySpot"/> would refuse it.</summary>
        public bool TrySabotageAs(TeamId attacker, string who, out string message)
        {
            who = string.IsNullOrEmpty(who) ? playerName : who;
            message = null;
            var m = TaskRaceModeManager.Instance;
            if (m == null || !m.AreBridgesOpen) { message = "สะพานยังปิดอยู่ ข้ามไม่ได้"; return false; }
            if (attacker == TeamId.None || ownerTeam == attacker) { message = "จุดนี้เป็นของทีมเรา"; return false; }
            if (state != SpotState.Clean) return false;
            if (SabotageUsed) { message = "จุดนี้ถูกป่วนไปแล้ว"; return false; }
            if (isProtected) { message = "จุดงานนี้มีเกราะป้องกันอยู่!"; Blocked?.Invoke(this, message); return false; }
            if (IsOpen)
            {
                if (LockedBy == who) return false;
                message = LockedBy + " กำลังทำอยู่ ไปจุดอื่นนะ";
                Blocked?.Invoke(this, message);
                return false;
            }

            IsOpen = true; LockedBy = who; sabotageSession = true; LastCardCompleted = false;
            Opened?.Invoke(this); AnyOpened?.Invoke(this);
            RefreshRing();
            return true;
        }

        /// <summary>Card finished: the stain comes back, the count drops by one and the ring returns (docs 19).</summary>
        public bool CompleteSabotage()
        {
            if (!IsOpen || !sabotageSession) return false;
            IsOpen = false; LockedBy = null; sabotageSession = false;
            _sabotageCount = Mathf.Min(MaxSabotagesPerMatch, _sabotageCount + 1);
            state = SpotState.Dirty; Progress = 0f; retained = false;
            isProtected = false; _shieldTimer = 0f;

            Closed?.Invoke(this, false); AnyClosed?.Invoke(this);
            Sabotaged?.Invoke(this); AnySabotaged?.Invoke(this);
            TaskRaceModeManager.Instance?.OnSpotSabotaged(this);
            PlayPingPop();
            RefreshRing();
            return true;
        }

        /// <summary>HUD: buy the timed shield for the owner team through the manager's atomic pool check.</summary>
        public bool TryBuyShield(out string message)
        {
            message = null;
            var m = TaskRaceModeManager.Instance;
            if (m == null) { message = "ยังไม่มีตัวจัดการโหมด"; return false; }
            if (IsEnemySpot) { message = "จุดนี้เป็นของคู่แข่ง"; return false; }
            if (state != SpotState.Clean) { message = "ทำจุดงานให้สะอาดก่อน"; return false; }
            if (isProtected) { message = "จุดนี้มีเกราะอยู่แล้ว"; return false; }
            if (SabotageUsed) return false;
            if (!m.TryPurchaseShield(LocalTeam, this)) { message = $"แต้มทีมไม่พอ ({m.shieldItemCost} Pts)"; return false; }
            message = "ติดเกราะป้องกันแล้ว!";
            return true;
        }

        /// <summary>Manager: deduct happened; start the 30-second protection.</summary>
        public void ApplyShield(float duration)
        {
            shieldDuration = Mathf.Max(1f, duration);
            isProtected = true;
            _shieldTimer = shieldDuration;
            RefreshRing();
        }

        void TickShield(float dt)
        {
            _shieldTimer -= dt;
            if (_shieldTimer > 0f) return;
            isProtected = false; _shieldTimer = 0f;
            RefreshRing();
        }

        public void AddProgress(float delta)
        {
            if (Done) return;
            Progress = Mathf.Clamp01(Progress + delta);
        }

        /// <summary>S28 card: drive the bar from the card's own clock (0..1). The 10 s retention keeps it on close.</summary>
        public void SetProgress(float value)
        {
            if (Done) return;
            Progress = Mathf.Clamp01(value);
        }

        public void ResetTask()
        {
            if (IsOpen) Close(false);
            state = SpotState.Dirty; Progress = 0f; retained = false; passesDone = 0;
        }

        /// <summary>Round: put a sabotaged task back on its station (Dirty, ring on) without reconfiguring it.</summary>
        public void RestoreSabotagedTask()
        {
            state = SpotState.Dirty; Progress = 0f; retained = false; passesDone = 0;
            SetTaskActive(true);
            RefreshRing();
        }

        /// <summary>Stage 4: how many completed passes this task needs (1 by default, 2 for a stain station).</summary>
        public void SetRequiredPasses(int passes)
        {
            requiredPasses = Mathf.Max(1, passes);
            passesDone = 0;
        }

        /// <summary>Turn the station on or off for the round. Off hides the ring and label and refuses to open.</summary>
        public void SetTaskActive(bool on)
        {
            hasTask = on;
            if (on) return;
            if (IsOpen) Close(false);
            state = SpotState.Dirty; Progress = 0f; retained = false;
            if (IsNear) { IsNear = false; NearChanged?.Invoke(this, false); }
            if (label != null) label.gameObject.SetActive(false);
            if (lockBadge != null) lockBadge.gameObject.SetActive(false);
            if (shieldBadge != null) shieldBadge.gameObject.SetActive(false);
            if (icon != null) icon.gameObject.SetActive(false);
            if (ring != null) ring.enabled = false;
        }

        /// <summary>Joystick moved past 70% while a card was open (called by TaskRaceModeManager).</summary>
        public static void CancelOpenSessions()
        {
            for (int i = 0; i < All.Count; i++)
                if (All[i] != null && All[i].IsOpen) All[i].Close(false);
        }

        // ---------------------------------------------------------------- editor / assembler / verify

        public void SetSpotId(string id) { if (!string.IsNullOrEmpty(id)) spotId = id; }

        public void Configure(string id, string name, TaskCategory cat, float seconds, string minigameId = null)
        {
            spotId = id; taskName = name; category = cat; expectedSeconds = Mathf.Max(1f, seconds);
            if (!string.IsNullOrEmpty(minigameId)) taskId = minigameId;
            hasTask = true;
            RebuildVisuals();
        }

        /// <summary>Editor / verify: give the station a minigame without going through the round.</summary>
        public void SetTaskId(string id) { if (!string.IsNullOrEmpty(id)) taskId = id; }

        /// <summary>Verify / test hooks, never used by gameplay.</summary>
        public void DebugForceClean()
        {
            state = SpotState.Clean; Progress = 1f; retained = false;
            RefreshRing();
        }

        public bool DebugForceSabotage()
        {
            if (state != SpotState.Clean || SabotageUsed) return false;
            if (IsOpen) { IsOpen = false; LockedBy = null; sabotageSession = false; }
            _sabotageCount = Mathf.Min(MaxSabotagesPerMatch, _sabotageCount + 1);
            state = SpotState.Dirty; Progress = 0f; retained = false;
            Sabotaged?.Invoke(this); AnySabotaged?.Invoke(this);
            TaskRaceModeManager.Instance?.OnSpotSabotaged(this);
            RefreshRing();
            return true;
        }

        /// <summary>Rebuild the ring and labels after Configure (editor or runtime).</summary>
        public void RebuildVisuals()
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                var c = transform.GetChild(i);
                if (c.name == "F29_Ring" || c.name == "TaskLabel" || c.name == "LockBadge" || c.name == "ShieldBadge"
                    || c.name == "TaskIcon")
                    DestroyNow(c.gameObject);
            }
            ring = null; label = null; lockBadge = null; shieldBadge = null; icon = null;
            if (buildVisuals) BuildVisuals();
        }

        static void DestroyNow(GameObject go)
        {
            if (Application.isPlaying) UnityEngine.Object.Destroy(go);
            else DestroyImmediate(go);
        }

        // ---------------------------------------------------------------- visuals

        void BuildVisuals()
        {
            var anchor = ringAnchor != null ? ringAnchor : transform;
            var colour = CategoryColour(category);

            var ringGo = new GameObject("F29_Ring");
            ringGo.transform.SetParent(transform, false);
            ring = ringGo.AddComponent<LineRenderer>();
            ring.useWorldSpace = true;
            ring.loop = true;
            ring.positionCount = RingSegments;
            ring.startWidth = ring.endWidth = 0.12f;
            ring.numCapVertices = 2;
            ring.numCornerVertices = 2;
            ring.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            ring.receiveShadows = false;
            var shader = Shader.Find("Sprites/Default") ?? Shader.Find("Unlit/Color");
            if (shader != null) ring.material = new Material(shader);
            ring.startColor = ring.endColor = colour;
            ring.enabled = false;

            label = MakeLabel("TaskLabel", anchor, labelHeight,
                $"<color=#{ColorUtility.ToHtmlStringRGB(colour)}>●</color> {taskName} · ~{Mathf.RoundToInt(expectedSeconds)} วิ",
                Color.white, 1.6f, 12);
            lockBadge = MakeLabel("LockBadge", anchor, labelHeight + 0.42f, "", new Color(0.94f, 0.28f, 0.44f), 1.3f, 13);
            shieldBadge = MakeLabel("ShieldBadge", anchor, labelHeight + 0.84f, "",
                new Color(1f, 0.84f, 0.35f), 1.2f, 14);

            // Floating category sign: one icon per task family, big enough to read across the island and always on while
            // the station still has work. The compact name label stays a preview-radius extra.
            var iconGo = new GameObject("TaskIcon");
            iconGo.transform.SetParent(transform, false);
            icon = iconGo.AddComponent<SpriteRenderer>();
            icon.sprite = iconSprite != null ? iconSprite : CategoryIconSprite(category);
            icon.color = Color.white;
            icon.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            icon.receiveShadows = false;
            icon.sortingOrder = 12;
            var ls = transform.lossyScale;
            iconGo.transform.localScale = Vector3.one * iconSize / Mathf.Max(0.0001f, Mathf.Abs(ls.x));
            iconGo.SetActive(false);
        }

        /// <summary>The category icon that floats over a dirty station (Assets/Resources/Icons, 4.3).</summary>
        public static Sprite CategoryIconSprite(TaskCategory c)
        {
            string name = c switch
            {
                TaskCategory.Sweep => "icon_skill_sweep",
                TaskCategory.Wash => "icon_skill_bubble",
                TaskCategory.Scrub => "icon_skill_press",
                TaskCategory.Spray => "icon_skill_spray",
                _ => "icon_ability",
            };
            return Resources.Load<Sprite>("Icons/" + name);
        }

        TextMeshPro MakeLabel(string name, Transform anchor, float height, string text, Color colour, float size, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            // cleaned props carry a 100x root scale from the FBX importer: keep the label at world size 1
            var s = transform.lossyScale;
            go.transform.localScale = new Vector3(
                1f / Mathf.Max(0.0001f, Mathf.Abs(s.x)),
                1f / Mathf.Max(0.0001f, Mathf.Abs(s.y)),
                1f / Mathf.Max(0.0001f, Mathf.Abs(s.z)));
            var t = go.AddComponent<TextMeshPro>();
            var font = Resources.Load<TMP_FontAsset>("Fonts/TMP/IBMPlexSansThai-Medium SDF");
            if (font != null) t.font = font;
            t.text = text;
            t.fontSize = size;
            t.color = colour;
            t.alignment = TextAlignmentOptions.Center;
            t.textWrappingMode = TextWrappingModes.NoWrap;
            t.outlineColor = new Color(0.16f, 0.18f, 0.25f, 0.95f);
            t.outlineWidth = 0.18f;
            t.raycastTarget = false;
            var mr = t.GetComponent<MeshRenderer>();
            if (mr != null) mr.sortingOrder = order;
            go.transform.position = anchor.position + Vector3.up * height;
            go.SetActive(false);
            return t;
        }

        void RefreshVisuals(bool near, float dist)
        {
            if (ring == null || label == null) return;
            var anchor = ringAnchor != null ? ringAnchor : transform;
            var cam = BobCurvedWorld.SampleCamera;
            bool dirty = state == SpotState.Dirty;
            bool inPreview = hasTask && dirty && dist <= previewRadius;

            label.gameObject.SetActive(inPreview);
            if (inPreview)
            {
                label.transform.position = LabelPoint(anchor, labelHeight);
                if (cam != null) label.transform.rotation = cam.transform.rotation;
            }

            if (lockBadge != null)
            {
                lockBadge.gameObject.SetActive(IsOpen);
                if (IsOpen)
                {
                    lockBadge.text = "<b>" + Initial(LockedBy) + "</b> …";
                    lockBadge.transform.position = LabelPoint(anchor, labelHeight + 0.42f);
                    if (cam != null) lockBadge.transform.rotation = cam.transform.rotation;
                }
            }

            if (shieldBadge != null)
            {
                shieldBadge.gameObject.SetActive(isProtected && dist <= previewRadius);
                if (isProtected)
                {
                    shieldBadge.text = $"เกราะ {Mathf.CeilToInt(_shieldTimer)} วิ";
                    shieldBadge.transform.position = LabelPoint(anchor, labelHeight + 0.84f);
                    if (cam != null) shieldBadge.transform.rotation = cam.transform.rotation;
                }
            }

            // Clean stations are ring-less, except while shielded: the gold ring is what the opponent sees from across
            // the bridge, docs 19 "turn the F29 ground ring glowing Gold".
            // 4.3: a dirty station keeps its category ring on from any distance and breathes gently, so the nearest
            // task is identifiable in under a second without walking into the preview radius.
            bool showRing = hasTask && (dirty || isProtected);
            ring.enabled = showRing;
            if (showRing)
            {
                var colour = isProtected ? ShieldGold : CategoryColour(category);
                if (!isProtected)
                {
                    float pulse = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 3.2f);
                    float heat = near ? 0.42f + 0.26f * pulse : 0.16f + 0.12f * pulse;
                    colour = Color.Lerp(colour, new Color(1f, 0.86f, 0.5f), heat);
                }
                colour.a = near ? 0.95f : 0.74f;
                ring.startColor = ring.endColor = colour;

                var centre = anchor.position;
                for (int i = 0; i < RingSegments; i++)
                {
                    float a = i / (float)RingSegments * Mathf.PI * 2f;
                    var p = centre + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * interactRadius;
                    p.y = centre.y + 0.035f;
                    ringPoints[i] = p + Vector3.up * BobCurvedWorld.Drop(p);
                }
                ring.SetPositions(ringPoints);
            }

            // The floating category sign: always readable while the station has work, bobbing just enough to read as
            // "come here". It is the same icon the HUD job list uses, so the colour and the picture never disagree.
            bool showIcon = hasTask && dirty;
            if (icon != null)
            {
                icon.gameObject.SetActive(showIcon);
                if (showIcon)
                {
                    float phase = Mathf.Abs(transform.position.x) * 0.35f;
                    float bob = Mathf.Sin(Time.unscaledTime * 2.1f + phase) * 0.06f;
                    icon.transform.position = LabelPoint(anchor, iconHeight) + Vector3.up * bob;
                    if (cam != null) icon.transform.rotation = cam.transform.rotation;
                    var ls = transform.lossyScale;
                    float beat = 1f + 0.035f * Mathf.Sin(Time.unscaledTime * 4.4f + phase);
                    icon.transform.localScale = Vector3.one * iconSize / Mathf.Max(0.0001f, Mathf.Abs(ls.x)) * beat;
                }
            }
        }

        Vector3 LabelPoint(Transform anchor, float height)
        {
            var p = anchor.position;
            return new Vector3(p.x, p.y + height, p.z) + Vector3.up * BobCurvedWorld.Drop(p);
        }

        void RefreshRing()
        {
            if (ring == null) return;
            var colour = isProtected ? ShieldGold : CategoryColour(category);
            colour.a = 0.9f;
            ring.startColor = ring.endColor = colour;
        }

        void PlayPingPop()
        {
            if (pingPop == null) return;
            var source = GetComponent<AudioSource>();
            if (source == null) source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.PlayOneShot(pingPop);
        }

        // ---------------------------------------------------------------- helpers

        Transform ResolvePlayer() => player != null ? player : localPlayer;

        static void Report(TaskSpot spot, bool inside, float dist)
        {
            if (Time.frameCount != reportFrame)
            {
                reportFrame = Time.frameCount; reportBest = float.MaxValue; reportSpot = null;
            }
            if (inside && dist < reportBest) { reportBest = dist; reportSpot = spot; }
            ActiveNear = reportSpot;
        }

        static GameObject FindPlayer()
        {
            try { return GameObject.FindGameObjectWithTag("Player"); }
            catch (UnityException) { return null; }
        }

        static string Initial(string who) =>
            string.IsNullOrEmpty(who) ? "?" : who.Substring(0, 1).ToUpperInvariant();

        /// <summary>Ring colours from the five F29 categories (docs 16 section 3).</summary>
        public static Color CategoryColour(TaskCategory c)
        {
            switch (c)
            {
                case TaskCategory.Sweep: return Hex("#F4A340");
                case TaskCategory.Wash: return Hex("#4EA8DE");
                case TaskCategory.Scrub: return Hex("#EF476F");
                case TaskCategory.Spray: return Hex("#9B7EDE");
                default: return Hex("#1FA6A0");
            }
        }

        static Color Hex(string h) => ColorUtility.TryParseHtmlString(h, out var c) ? c : Color.white;

        void OnDrawGizmosSelected()
        {
            Gizmos.color = CategoryColour(category);
            Gizmos.DrawWireSphere(transform.position, interactRadius);
            Gizmos.color = new Color(Gizmos.color.r, Gizmos.color.g, Gizmos.color.b, 0.35f);
            Gizmos.DrawWireSphere(transform.position, previewRadius);
        }
    }
}
