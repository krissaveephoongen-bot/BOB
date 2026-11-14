// Battle of the Brooms: a task spot for the Task Race map (docs 16 section 3, F29). Drop it on a station prop
// (espresso machine, sink counter, washing machine, cafe table ...). It builds the ground ring and the compact world
// label itself, owns the TASK_LOCK and the 10-second partial-progress retention, and raises events for the HUD.
//
// The component never builds UI: the game listens to NearChanged / Opened / Closed / Blocked and drives the main
// context button ("ทำงาน: <TaskName>"), the S28 minigame card and the toast. Activation and the card live in the game
// layer, the reach rules live here.
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Bob.TaskRace
{
    /// <summary>The five task families of the F29 ground ring (docs 16 section 3).</summary>
    public enum TaskCategory { Sweep, Wash, Scrub, Spray, Everyone }

    [DisallowMultipleComponent]
    public class TaskSpot : MonoBehaviour
    {
        [Header("Task (docs 16 section 3)")]
        [Tooltip("Unique on the map, e.g. S3_A. The assembler appends _B when the Team A layout is cloned.")]
        [SerializeField] string spotId = "S1_A";
        [SerializeField] string taskName = "งาน";
        [Tooltip("Off when the round has no task left for this station: no ring, no label, cannot be opened.")]
        [SerializeField] bool hasTask = true;
        [SerializeField] TaskCategory category = TaskCategory.Everyone;
        [Tooltip("Estimated duration shown in the preview label, in seconds.")]
        [SerializeField, Min(1f)] float expectedSeconds = 12f;

        [Header("Reach (flat X-Z world units)")]
        [Tooltip("Compact label shows inside this radius.")]
        [SerializeField] float previewRadius = 2.6f;
        [Tooltip("NEAR: ring highlights and the main button turns into ทำงาน: <name> (docs 16: 1.2).")]
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

        const int RingSegments = 48;

        // ---------------------------------------------------------------- events for the game / HUD

        /// <summary>Raised when the hero enters or leaves the NEAR circle. Drives the main context button.</summary>
        public event Action<TaskSpot, bool> NearChanged;
        /// <summary>Raised after TASK_LOCK is taken and the S28 card may open.</summary>
        public event Action<TaskSpot> Opened;
        /// <summary>Raised after the card closes. true = task completed.</summary>
        public event Action<TaskSpot, bool> Closed;
        /// <summary>Raised when the spot cannot open (already done, or locked by another player). Show `message` as a toast.</summary>
        public event Action<TaskSpot, string> Blocked;
        /// <summary>Global hooks for the mode manager: camera focus on open, unfocus on close.</summary>
        public static event Action<TaskSpot> AnyOpened;
        public static event Action<TaskSpot> AnyClosed;

        // ---------------------------------------------------------------- state

        static readonly List<TaskSpot> All = new List<TaskSpot>();
        static Transform localPlayer;
        static int reportFrame = -1;
        static float reportBest;
        static TaskSpot reportSpot;

        /// <summary>The closest spot inside its NEAR radius this frame; the HUD reads this for the main button.</summary>
        public static TaskSpot ActiveNear { get; private set; }

        public string SpotId => spotId;
        public string TaskName => taskName;
        public TaskCategory Category => category;
        public float ExpectedSeconds => expectedSeconds;
        public float PreviewRadius => previewRadius;
        public float InteractRadius => interactRadius;
        public bool IsNear { get; private set; }
        public bool IsOpen { get; private set; }
        public bool IsLocked => IsOpen;
        public string LockedBy { get; private set; }
        public bool Done { get; private set; }
        public float Progress { get; private set; }
        /// <summary>Progress still inside the 10-second retention window; 0 when it expired.</summary>
        public float RetainedProgress => retained && Time.unscaledTime < retainUntil ? Progress : 0f;
        public Transform Player => player;

        bool retained;
        float retainUntil;

        LineRenderer ring;
        TextMeshPro label, lockBadge;
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

            var t = ResolvePlayer();
            float dist = float.MaxValue;
            bool near = false;
            if (t != null)
            {
                var d = t.position - transform.position; d.y = 0f;
                dist = d.magnitude;
                near = hasTask && !Done && dist <= interactRadius;
            }

            if (near != IsNear) { IsNear = near; NearChanged?.Invoke(this, near); }
            Report(this, near, dist);
            RefreshVisuals(near, dist);
        }

        // ---------------------------------------------------------------- public API (game / HUD / server)

        /// <summary>Set by the map manager once the local hero exists.</summary>
        public static void SetLocalPlayer(Transform hero) => localPlayer = hero;

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
            if (Done)
            {
                message = taskName + " เสร็จแล้ว";
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

            IsOpen = true; LockedBy = who; message = null;
            Opened?.Invoke(this); AnyOpened?.Invoke(this);
            RefreshRing();
            return true;
        }

        /// <summary>Open with this spot's own player name.</summary>
        public bool Open() => TryOpen(playerName, out _);

        /// <summary>Close the card. Completed sets the spot clean; otherwise the partial progress is kept for
        /// `retainedProgressSeconds` (S28: close with × or by pushing the joystick past 70%).</summary>
        public void Close(bool completed)
        {
            if (!IsOpen) return;
            IsOpen = false; LockedBy = null;
            if (completed)
            {
                Done = true; Progress = 1f; retained = false;
            }
            else if (retainedProgressSeconds > 0f)
            {
                retained = true; retainUntil = Time.unscaledTime + retainedProgressSeconds;
            }
            Closed?.Invoke(this, completed); AnyClosed?.Invoke(this);
            RefreshRing();
        }

        public void AddProgress(float delta)
        {
            if (Done) return;
            Progress = Mathf.Clamp01(Progress + delta);
        }

        public void ResetTask()
        {
            if (IsOpen) Close(false);
            Done = false; Progress = 0f; retained = false;
        }

        /// <summary>Turn the station on or off for the round. Off hides the ring and label and refuses to open.</summary>
        public void SetTaskActive(bool on)
        {
            hasTask = on;
            if (on) return;
            if (IsOpen) Close(false);
            Done = false; Progress = 0f; retained = false;
            if (IsNear) { IsNear = false; NearChanged?.Invoke(this, false); }
            if (label != null) label.gameObject.SetActive(false);
            if (lockBadge != null) lockBadge.gameObject.SetActive(false);
            if (ring != null) ring.enabled = false;
        }

        /// <summary>Joystick moved past 70% while a card was open (called by TaskRaceModeManager).</summary>
        public static void CancelOpenSessions()
        {
            for (int i = 0; i < All.Count; i++)
                if (All[i] != null && All[i].IsOpen) All[i].Close(false);
        }

        // ---------------------------------------------------------------- editor / assembler

        public void SetSpotId(string id) { if (!string.IsNullOrEmpty(id)) spotId = id; }

        public void Configure(string id, string name, TaskCategory cat, float seconds)
        {
            spotId = id; taskName = name; category = cat; expectedSeconds = Mathf.Max(1f, seconds);
            hasTask = true;
            RebuildVisuals();
        }

        /// <summary>Rebuild the ring and labels after Configure (editor or runtime).</summary>
        public void RebuildVisuals()
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                var c = transform.GetChild(i);
                if (c.name == "F29_Ring" || c.name == "TaskLabel" || c.name == "LockBadge")
                    DestroyNow(c.gameObject);
            }
            ring = null; label = null; lockBadge = null;
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
            ring.startWidth = ring.endWidth = 0.09f;
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
            bool inPreview = hasTask && !Done && dist <= previewRadius;

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
                    lockBadge.text = "<b>" + Initial(LockedBy) + "</b> ⋯";
                    lockBadge.transform.position = LabelPoint(anchor, labelHeight + 0.42f);
                    if (cam != null) lockBadge.transform.rotation = cam.transform.rotation;
                }
            }

            bool showRing = hasTask && !Done && (inPreview || IsOpen);
            ring.enabled = showRing;
            if (!showRing) return;

            var colour = CategoryColour(category);
            if (near)
            {
                float pulse = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 6f);
                colour = Color.Lerp(colour, new Color(1f, 0.82f, 0.4f), 0.35f + 0.25f * pulse);
            }
            colour.a = 0.9f;
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

        Vector3 LabelPoint(Transform anchor, float height)
        {
            var p = anchor.position;
            return new Vector3(p.x, p.y + height, p.z) + Vector3.up * BobCurvedWorld.Drop(p);
        }

        void RefreshRing()
        {
            if (ring == null) return;
            var colour = CategoryColour(category);
            colour.a = 0.9f;
            ring.startColor = ring.endColor = colour;
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
