// Battle of the Brooms: the Task Race HUD of docs 16 section 2 and docs 24 sections 10–12 — the team task list
// ("ใบงาน"), the weight bar, the match clock + "Team Points" bar, the S28 2D minigame card, the main context button
// and a lightweight debug test bar. Built from code like the other HUDs, with the tokens of docs 11.
//
// The list follows the round's plan order (T04 first). A row shows the task's category disc, its name and its
// weight; เสร็จ means done, ทำอยู่ means a player holds the TASK_LOCK, รอคิว means the task is still in the queue
// and no station has taken it yet (the restock of docs 24 section 14). The bar and the % use weights, the "3/6"
// next to the title uses task counts, exactly as docs 16 section 2.2 asks.
//
// The main button is context sensitive: ทำงาน on a dirty own station, ป่วนจุดงาน on a clean enemy station while the
// bridges are open, ติดเกราะป้องกัน (-150 Pts) on a clean own station, and a disabled สะอาดแล้ว otherwise. Any action
// that takes the TASK_LOCK opens the S28 card. The card is the 1400 x 860 frame of docs 11 with a 1240 x 620 play
// area filled by BobMinigameHost (docs 16 sections 5–6); T04 (cart), T12 (chairs) and sabotage keep the timed bar,
// because those two have no card and a sabotage is a 2.5–3.0 s hold. The manager closes any card when the joystick
// passes 70%.
//
// Layout is placed by hand instead of layout groups: the card is authored in the editor (no layout pass runs
// outside play mode), and fixed row heights keep a wrapped Thai name from sliding into the next row.
using System.Collections.Generic;
using Bob.Game;
using Bob.Rules;
using Bob.UI;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Bob.TaskRace
{
    [DisallowMultipleComponent]
    public class BobTaskRaceHud : MonoBehaviour
    {
        [SerializeField] TaskRaceRound round;
        [SerializeField] TaskRaceModeManager manager;
        [Tooltip("Display name in the TASK_LOCK toast (docs 16 section 2.1).")]
        [SerializeField] string playerName = "ฉัน";

        [Header("Art")]
        [Tooltip("Row category icons; the scene builder wires Assets/Art/BobArtLibrary.asset.")]
        [SerializeField] BobArtLibrary art;

        [Header("Layout in canvas units, 1920x1080 match height (docs 11)")]
        [SerializeField] float cardWidth = 560f;
        [SerializeField] float rowHeight = 54f;
        [SerializeField] float rowGap = 4f;
        [SerializeField] Vector2 cardInset = new Vector2(24f, 24f);
        [SerializeField] float toastSeconds = 2.2f;

        [Header("S28 card")]
        [Tooltip("A sabotage card takes 2.5–3.0 s (docs 24 section 12).")]
        [SerializeField, Range(2.5f, 3f)] float sabotageCardSeconds = 2.75f;
        [Tooltip("Normal task cards never run shorter than this, whatever the scaled card time is.")]
        [SerializeField, Min(1f)] float minimumTaskCardSeconds = 2.5f;

        [Header("Debug test bar")]
        [Tooltip("Editor / development builds only: a device build never draws the debug bar.")]
        [SerializeField] bool showDebugBar;

        [Header("Touch")]
        [Tooltip("Build the floating joystick and tap-to-walk zones under the HUD.")]
        [SerializeField] bool buildTouchControls = true;

        const float PadL = 18f, PadR = 18f, PadT = 14f, PadB = 14f;
        const float HeadH = 56f, BarH = 26f, BlockGap = 10f;
        const float FooterH = 104f;
        const float DiscW = 40f, StatusW = 96f, WeightW = 56f, ColGap = 8f;

        Canvas canvas;
        RectTransform page, card, fill, rowsRoot, footer;
        TMP_Text title, percent, footerText, toast;
        Button mainButton;
        TMP_Text mainLabel;

        RectTransform topBar, timeFill;
        TMP_Text timerText, pointsText, rivalText, teamPctText, modeText;

        Image cardDim, cardPanel, cardTrack;
        RectTransform cardFill;
        TMP_Text cardTitle, cardPercent, cardStep, cardHint, cardClock, cardChip;
        RectTransform cardChipRoot;
        Button cardClose;
        float cardChipUntil;
        BobMinigameHost mini;
        bool cardInteractive;

        BobTaskRaceTouch touch;
        RectTransform resultRoot;
        MatchResult shownResult;
        bool hasShownResult;

        Button pauseButton;
        RectTransform pauseRoot;

        // T16 emergency stain banner (docs 16 section 4): a red card at the bottom-left, clear of the task list and
        // the action button, with the 25 s grace bar. It never covers the main button or the joystick.
        RectTransform emergencyRoot, emergencyFill;
        RectTransform emergencyArrow;
        Image emergencyArrowImg;
        TMP_Text emergencyTitle, emergencySub;
        Button emergencyButton;
        BobEmergencyStains stains;
        bool cardEmergency;

        RectTransform debugBar, debugToggle;
        TMP_Text debugModeLabel;

        // P03 Quick Ping (docs 11 section 5A.5): the round button above the main action, the six-word fan it opens and
        // the short bubble over the sender's head. Built at runtime from the palette — no icon art, no new sound file.
        RectTransform pingButton, pingRoot, pingHub, pingBubble;
        Image pingButtonImg;
        TMP_Text pingHubLabel, pingBubbleLabel;
        CanvasGroup pingBubbleGroup;
        readonly List<Image> pingDiscs = new List<Image>();
        readonly List<TMP_Text> pingDiscLabels = new List<TMP_Text>();
        BobTaskRaceHero pingHero;
        bool pingWheelOpen, pingHolding;
        int pingHover = -1;
        Vector2 pingOrigin;
        float pingBubbleUntil;

        // docs 11 HUD-MateTray: one chip per offline team-mate on the free right column, saying what that mate is doing
        // ("walking to S3", "cleaning", "carrying a chair"). Tapping a chip calls the mates to the job beside the
        // player. An online room draws the real bodies instead, so the tray is offline-only.
        const float MateChipW = 300f, MateChipH = 92f, MateChipGap = 8f, MateTrayTop = 500f;
        RectTransform mateTray;
        TMP_Text mateHint;
        readonly List<RectTransform> mateChips = new List<RectTransform>();
        readonly List<TMP_Text> mateLines = new List<TMP_Text>();
        readonly List<TaskRaceBotController> mateOrder = new List<TaskRaceBotController>();
        TaskRaceBotController[] mateScan;
        float mateScanAt;

        // docs 16 section 2.5 star 2: the task ids whose card was closed before the job was done, so the chapter can
        // forgive the ones that were finished later (a close is not a loss until the clock ends).
        readonly HashSet<string> abandonedCards = new HashSet<string>();

        int builtVersion = -1;
        float shownPercent = -1f;
        float toastUntil;

        TaskSpot cardSpot;
        bool cardSabotage;
        float cardElapsed, cardDuration, cardStartProgress;

        TaskRaceModeManager hooked;

        public Canvas UiCanvas => canvas;
        /// <summary>Rows drawn in the task list; the verify log reads this.</summary>
        public int RowCount { get; private set; }
        public bool CardOpen => cardSpot != null || cardEmergency;
        /// <summary>The result overlay (S27) is up.</summary>
        public bool ResultOpen => resultRoot != null;
        /// <summary>The pause card is up: the manager holds the clock at zero.</summary>
        public bool PauseOpen => pauseRoot != null && pauseRoot.gameObject.activeSelf;
        /// <summary>Verify: the ⏸ button is drawn (it hides while a card, the result or the pause card is up).</summary>
        public bool PauseButtonVisible => pauseButton != null && pauseButton.gameObject.activeSelf;
        public MatchResult ShownResult => shownResult;
        /// <summary>Verify: the two small lines under the play area.</summary>
        public string CardStep => cardStep != null ? cardStep.text : "";
        public string CardHint => cardHint != null ? cardHint.text : "";

        static BobUiTheme T => BobUiTheme.Current;
        float ContentW => cardWidth - PadL - PadR;
        /// <summary>The debug test bar is authoring-only: a device build never draws it, whatever the scene says.</summary>
        bool DebugBarAllowed => showDebugBar && (Application.isEditor || Debug.isDebugBuild);

        /// <summary>Scene builder / verify: point the HUD at a round without relying on Awake.</summary>
        public void Bind(TaskRaceRound value) => round = value;

        /// <summary>Scene builder: the art library behind the row icons (the Task Race scene has no BobGame to set it).</summary>
        public void BindArt(BobArtLibrary value) { art = value; if (art != null) BobUi.Art = art; }

        /// <summary>Build (or rebuild) the canvas. Safe to call in the editor so the scene carries the HUD.</summary>
        public void Build(Camera cam = null)
        {
            if (art != null) BobUi.Art = art;
            if (round == null) round = FindAnyObjectByType<TaskRaceRound>();
            if (manager == null) manager = FindAnyObjectByType<TaskRaceModeManager>();
            Hook();

            if (canvas == null) CreateCanvas(cam);
            else if (canvas.worldCamera == null) canvas.worldCamera = cam != null ? cam : Camera.main;

            // The scene carries a canvas built by the scene builder; its field references are not serialized, so a
            // fresh build replaces it. Without this the authored HUD stays visible underneath the new one.
            foreach (var stale in GetComponentsInChildren<Canvas>(true))
            {
                if (stale == canvas) continue;
                if (Application.isPlaying) Destroy(stale.gameObject); else DestroyImmediate(stale.gameObject);
            }

            ClearPage();
            if (buildTouchControls) BuildTouch(cam != null ? cam : canvas.worldCamera);
            BuildTaskList();
            BuildTopBar();
            BuildPause();
            BuildMainButton();
            BuildMinigameCard();
            BuildToast();
            BuildPing();
            BuildMateTray();
            BuildEmergency();
            BuildDebugBar();
            RenderRows();
            RenderTopBar();
            RenderMain();
            HideCard();
            HideResult();
            HookEmergency();
        }

        /// <summary>
        /// T16 (docs 16 section 4) lives in play mode only: an edit-mode build must not add a scene object, and the
        /// verify shots never spawn a stain. The component owns the schedule; the HUD owns the red banner and the card.
        /// </summary>
        void HookEmergency()
        {
            if (!Application.isPlaying || round == null || manager == null) return;
            stains = BobEmergencyStains.Ensure(round, manager);
            stains.Message -= ShowToast;
            stains.Message += ShowToast;
            // T12 chair toasts (grab / put down / all home) come through a static event: the component is created
            // and destroyed with the station's card, while the HUD exists for the whole round.
            if (Application.isPlaying)
            {
                BobChairTask.AnyMessage -= ShowToast;
                BobChairTask.AnyMessage += ShowToast;
            }
        }

        void Awake()
        {
            if (round == null) round = FindAnyObjectByType<TaskRaceRound>();
            if (manager == null) manager = FindAnyObjectByType<TaskRaceModeManager>();
            Hook();
            if (canvas == null) Build();
        }

        void OnDisable()
        {
            if (hooked != null)
            {
                hooked.ToastRequested -= ShowToast;
                hooked.MatchEnded -= OnMatchEnded;
                hooked.MatchStarted -= OnMatchStarted;
            }
            hooked = null;
            if (stains != null) stains.Message -= ShowToast;
            BobChairTask.AnyMessage -= ShowToast;
        }

        void Hook()
        {
            if (hooked == manager) return;
            if (hooked != null)
            {
                hooked.ToastRequested -= ShowToast;
                hooked.MatchEnded -= OnMatchEnded;
                hooked.MatchStarted -= OnMatchStarted;
            }
            hooked = manager;
            if (hooked != null)
            {
                hooked.ToastRequested += ShowToast;
                hooked.MatchEnded += OnMatchEnded;
                hooked.MatchStarted += OnMatchStarted;
            }
        }

        void Update()
        {
            if (canvas == null) return;
            if (Input.GetKeyDown(KeyCode.Escape)) OnBackPressed();
            if (round != null && round.Version != builtVersion)
            {
                builtVersion = round.Version;
                RenderRows();
            }
            RenderBar();
            RenderTopBar();
            RenderMain();
            RenderEmergency();
            RenderPing();
            RenderMateTray();
            RenderCard();
            RenderPause();
            if (manager != null)
            {
                if (manager.LastResult.HasValue && !hasShownResult) ShowResult(manager.LastResult.Value);
                else if (!manager.LastResult.HasValue && resultRoot != null) HideResult();
            }
            if (toast != null && toast.gameObject.activeSelf && Time.unscaledTime >= toastUntil)
                toast.gameObject.SetActive(false);
        }

        void OnMatchEnded(MatchResult result) => ShowResult(result);

        /// <summary>A new chapter starts its own tally: the ids that were abandoned last time mean nothing now.</summary>
        void OnMatchStarted() => abandonedCards.Clear();

        // ---------------------------------------------------------------- canvas

        void CreateCanvas(Camera cam)
        {
            var go = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            go.transform.SetParent(transform, false);
            canvas = go.GetComponent<Canvas>();
            // Screen-space camera, not overlay: the verify shots render through their own camera into a texture,
            // and an overlay canvas never appears in those.
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.planeDistance = 5f;
            canvas.worldCamera = cam != null ? cam : Camera.main;
            canvas.sortingOrder = 10;

            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 1f;

            page = BobUi.Stretch(BobUi.Rect("Page", go.transform));
        }

        void ClearPage()
        {
            if (page == null) return;
            for (int i = page.childCount - 1; i >= 0; i--)
            {
                var go = page.GetChild(i).gameObject;
                go.SetActive(false);
                if (Application.isPlaying) Destroy(go); else DestroyImmediate(go);
            }
            card = fill = rowsRoot = footer = topBar = timeFill = debugBar = debugToggle = null;
            mainButton = null; mainLabel = null;
            timerText = pointsText = rivalText = teamPctText = modeText = null;
            cardDim = cardPanel = cardTrack = null; cardFill = null;
            cardTitle = cardPercent = cardStep = cardHint = cardClock = cardChip = null; cardClose = null;
            cardChipRoot = null;
            cardChipUntil = 0f;
            mini = null; cardInteractive = false;
            touch = null; resultRoot = null; hasShownResult = false;
            pauseButton = null; pauseRoot = null;
            emergencyRoot = emergencyFill = null; emergencyArrow = null; emergencyArrowImg = null;
            emergencyTitle = emergencySub = null; emergencyButton = null;
            stains = null; cardEmergency = false;
            debugModeLabel = null;
            pingButton = pingRoot = pingHub = pingBubble = null;
            pingButtonImg = null; pingHubLabel = pingBubbleLabel = null; pingBubbleGroup = null;
            pingDiscs.Clear(); pingDiscLabels.Clear(); pingHero = null;
            pingWheelOpen = pingHolding = false; pingHover = -1; pingBubbleUntil = 0f;
            mateTray = null; mateHint = null; mateScan = null; mateScanAt = 0f;
            mateChips.Clear(); mateLines.Clear(); mateOrder.Clear();
        }

        void BuildTouch(Camera cam)
        {
            touch = GetComponent<BobTaskRaceTouch>();
            if (touch == null) touch = gameObject.AddComponent<BobTaskRaceTouch>();
            touch.Build(page, cam);
        }

        // ---------------------------------------------------------------- task list

        void BuildTaskList()
        {
            card = (RectTransform)BobUi.Panel(page, T.surface, null, "TaskList").transform;
            BobUi.Pin(card, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(cardInset.x, -cardInset.y), new Vector2(cardWidth, 400f));

            title = BobUi.Label(card, "ใบงาน", 46, T.ink, TextAnchor.MiddleLeft, FontStyle.Bold);
            BobUi.Pin(title.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(PadL, -PadT), new Vector2(ContentW, HeadH));

            var track = BobUi.Panel(card, T.surfaceCream, null, "Track");
            BobUi.Pin((RectTransform)track.transform, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(PadL, -(PadT + HeadH + BlockGap)), new Vector2(ContentW - StatusW - ColGap, BarH));
            fill = BobUi.Rect("Fill", track.transform);
            fill.anchorMin = new Vector2(0f, 0f);
            fill.anchorMax = new Vector2(1f, 1f);
            fill.offsetMin = fill.offsetMax = Vector2.zero;
            var fillImg = fill.gameObject.AddComponent<Image>();
            fillImg.color = T.primary;
            fillImg.raycastTarget = false;

            percent = BobUi.Label(card, "0%", 34, T.ink, TextAnchor.MiddleRight, FontStyle.Bold);
            BobUi.Pin(percent.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(-PadR, -(PadT + HeadH + BlockGap)), new Vector2(StatusW, BarH));

            rowsRoot = BobUi.Rect("Rows", card);
            BobUi.Pin(rowsRoot, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(PadL, -(RowTop)), new Vector2(ContentW, rowHeight));

            footer = BobUi.Rect("Summary", card);
            footerText = BobUi.Label(footer, "", 26, T.inkMuted, TextAnchor.UpperLeft);
            BobUi.Stretch(footerText.rectTransform);
        }

        float RowTop => PadT + HeadH + BlockGap + BarH + BlockGap;

        /// <summary>The one-line hint under the task list: what to do next, never the debug summary.</summary>
        string AdviceLine()
        {
            if (round == null) return "";
            int left = Mathf.Max(0, round.TaskCount - round.DoneCount);
            if (manager == null) return $"เหลือ {left} งาน";
            if (!manager.BridgeRaid) return $"เหลือ {left} งาน · Pure Race: สะพานปิดทั้งแมตช์";
            if (manager.AreBridgesOpen) return $"เหลือ {left} งาน · สะพานเปิดแล้ว ป่วนจุดงานคู่แข่งได้";
            return $"เหลือ {left} งาน · เดินใกล้จุดงานแล้วกด ทำงาน";
        }

        void RenderRows()
        {
            if (rowsRoot == null) return;
            for (int i = rowsRoot.childCount - 1; i >= 0; i--)
            {
                var go = rowsRoot.GetChild(i).gameObject;
                go.SetActive(false);
                if (Application.isPlaying) Destroy(go); else DestroyImmediate(go);
            }

            if (round == null)
            {
                RowCount = 0;
                title.text = "ใบงาน";
                footerText.text = "กำลังเตรียมงาน…";
                FitCard(0);
                return;
            }

            title.text = $"ใบงาน · ทีมเรา {round.DoneCount}/{round.TaskCount}";
            footerText.text = AdviceLine();
            NoteRecoveredJobs();

            var tasks = round.Tasks;
            RowCount = tasks.Count;
            int nextIndex = -1;
            for (int i = 0; i < tasks.Count; i++) if (!tasks[i].Done) { nextIndex = i; break; } // highlight the next job
            for (int i = 0; i < tasks.Count; i++) BuildRow(tasks[i], i, i == nextIndex);
            FitCard(tasks.Count);
        }

        /// <summary>
        /// docs 16 section 2.5 star 2: a close is forgiven the moment that job is finished by anyone on the team. The
        /// list is rebuilt on every round Version change, which is exactly when a job can go Done, so this needs no
        /// timer of its own.
        /// </summary>
        void NoteRecoveredJobs()
        {
            if (abandonedCards.Count == 0 || round == null || manager == null || manager.Series == null) return;
            List<string> recovered = null;
            foreach (var entry in round.Tasks)
            {
                if (entry == null || !entry.Done) continue;
                if (!abandonedCards.Contains(entry.Spec.Id)) continue;
                (recovered ??= new List<string>()).Add(entry.Spec.Id);
            }
            if (recovered == null) return;
            foreach (var id in recovered)
            {
                abandonedCards.Remove(id);
                manager.Series.NoteCardRecovered();
                Debug.Log($"[TaskRace] abandoned job finished later: {id}");
            }
        }

        void BuildRow(TaskEntry entry, int index, bool next)
        {
            var row = BobUi.Rect("Task_" + entry.Spec.Id, rowsRoot);
            BobUi.Pin(row, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(0f, -index * (rowHeight + rowGap)), new Vector2(ContentW, rowHeight));

            if (next) // the eye lands on the one job to do next
            {
                var hi = BobUi.Panel(row, new Color(T.accent.r, T.accent.g, T.accent.b, 0.16f), null, "Next");
                BobUi.Stretch((RectTransform)hi.transform);
                hi.raycastTarget = false;
                var stripe = BobUi.Panel(row, T.accent, null, "NextStripe");
                BobUi.Pin((RectTransform)stripe.transform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                    Vector2.zero, new Vector2(6f, rowHeight));
                stripe.raycastTarget = false;
            }

            // category icon instead of a bare colour dot (clear of the 6 px "next" stripe)
            var icon = BobUi.Img(row, CategoryIcon(entry.Spec.Category), 28, TaskSpot.CategoryColour(entry.Spec.Category));
            BobUi.Pin(icon.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(9f, 0f), new Vector2(28f, 28f));

            float nameW = ContentW - DiscW - ColGap - StatusW - ColGap - WeightW - ColGap;
            var name = BobUi.Label(row, NameText(entry), 32, NameColour(entry));
            name.textWrappingMode = TextWrappingModes.NoWrap;
            name.overflowMode = TextOverflowModes.Ellipsis;
            BobUi.Pin(name.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(DiscW + ColGap, 0f), new Vector2(nameW, rowHeight));

            var status = BobUi.Label(row, StatusText(entry), 26, StatusColour(entry), TextAnchor.MiddleRight);
            BobUi.Pin(status.rectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                new Vector2(-(WeightW + ColGap), 0f), new Vector2(StatusW, rowHeight));

            var weight = BobUi.Label(row, "×" + entry.Spec.Weight, 28,
                entry.Done ? T.inkMuted : TaskSpot.CategoryColour(entry.Spec.Category),
                TextAnchor.MiddleRight, FontStyle.Bold);
            BobUi.Pin(weight.rectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                Vector2.zero, new Vector2(WeightW, rowHeight));
        }

        void FitCard(int rows)
        {
            float rowsH = rows > 0 ? rows * rowHeight + (rows - 1) * rowGap : rowHeight;
            float footerTop = RowTop + rowsH + BlockGap;
            float height = footerTop + FooterH + PadB;
            card.sizeDelta = new Vector2(cardWidth, height);

            BobUi.Pin(footer, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(PadL, -footerTop), new Vector2(ContentW, FooterH));
        }

        static string CategoryIcon(TaskCategory c) => c switch
        {
            TaskCategory.Sweep => "icon_skill_sweep",
            TaskCategory.Wash => "icon_skill_bubble",
            TaskCategory.Scrub => "icon_skill_press",
            TaskCategory.Spray => "icon_skill_spray",
            _ => "icon_ability",
        };

        string NameText(TaskEntry entry) =>
            entry.Done ? $"<color=#{Hex(T.success)}>✓</color> <s>{entry.Spec.Name}</s>"
            : entry.Locked ? $"<color=#{Hex(T.accent)}>…</color> {entry.Spec.Name}"
            : entry.Spec.Name;

        string StatusText(TaskEntry entry) =>
            entry.Done ? "เสร็จ" : entry.Locked ? "ทำอยู่" : entry.Spot == null ? "รอคิว" : "";

        Color NameColour(TaskEntry entry) =>
            entry.Done ? T.inkMuted : entry.Locked ? T.accent : entry.Spot == null ? T.inkMuted : T.ink;

        Color StatusColour(TaskEntry entry) =>
            entry.Done ? T.success : entry.Locked ? T.accent : T.inkMuted;

        void RenderBar()
        {
            if (fill == null || percent == null) return;
            float target = round != null && round.TotalWeight > 0 ? round.DoneWeight / (float)round.TotalWeight : 0f;
            if (shownPercent < 0f) shownPercent = target;
            else shownPercent = Mathf.MoveTowards(shownPercent, target, Time.unscaledDeltaTime * 0.75f); // docs 16: the bar steps, never snaps

            var anchor = fill.anchorMax;
            anchor.x = Mathf.Clamp01(shownPercent);
            fill.anchorMax = anchor;

            percent.text = Mathf.RoundToInt(shownPercent * 100f) + "%";
            var fillImg = fill.GetComponent<Image>();
            if (fillImg != null) fillImg.color = Color.Lerp(T.primary, T.success, Mathf.Clamp01(shownPercent));
        }

        // ---------------------------------------------------------------- match bar (clock + points)

        void BuildTopBar()
        {
            topBar = (RectTransform)BobUi.Panel(page, T.surface, null, "TopBar").transform;
            // Clear of the task card on the left: drawn at the screen centre the two cream panels overlapped and
            // the seam read as a rendering slip.
            BobUi.Pin(topBar, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(cardWidth + cardInset.x + 12f, -cardInset.y), new Vector2(940f, 124f));

            // the clock is the loudest thing on screen (S28/HUD: "see it in half a second")
            timerText = BobUi.Label(topBar, "เวลา 02:00", 48, T.ink, TextAnchor.MiddleLeft, FontStyle.Bold);
            BobUi.Pin(timerText.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(PadL, -8f), new Vector2(330f, 54f));

            modeText = BobUi.Label(topBar, "Bridge Raid", 22, T.inkMuted, TextAnchor.MiddleLeft);
            BobUi.Pin(modeText.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(PadL + 2f, -64f), new Vector2(470f, 26f));

            pointsText = BobUi.Label(topBar, "คะแนนทีม 0", 30, T.ink, TextAnchor.MiddleRight, FontStyle.Bold);
            BobUi.Pin(pointsText.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(-PadR, -8f), new Vector2(330f, 40f));

            rivalText = BobUi.Label(topBar, "คู่แข่ง 0", 24, T.inkMuted, TextAnchor.MiddleRight);
            BobUi.Pin(rivalText.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(-PadR, -50f), new Vector2(380f, 26f));

            teamPctText = BobUi.Label(topBar, "ทีมเรา 0%", 22, T.inkMuted, TextAnchor.MiddleRight, FontStyle.Bold);
            BobUi.Pin(teamPctText.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(-PadR, -78f), new Vector2(310f, 26f));

            var track = BobUi.Panel(topBar, T.surfaceCream, null, "ClockTrack");
            BobUi.Pin((RectTransform)track.transform, new Vector2(0f, 0f), new Vector2(0f, 0f),
                new Vector2(PadL, 8f), new Vector2(940f - PadL - PadR, 8f));
            timeFill = BobUi.Rect("ClockFill", track.transform);
            timeFill.anchorMin = new Vector2(0f, 0f);
            timeFill.anchorMax = new Vector2(1f, 1f);
            timeFill.offsetMin = timeFill.offsetMax = Vector2.zero;
            var timeImg = timeFill.gameObject.AddComponent<Image>();
            timeImg.color = T.primary;
            timeImg.raycastTarget = false;
        }

        void RenderTopBar()
        {
            if (topBar == null) return;
            if (manager == null)
            {
                timerText.text = "เวลา 02:00";
                pointsText.text = "คะแนนทีม 0";
                if (rivalText != null) rivalText.text = "คู่แข่ง 0";
                if (teamPctText != null) teamPctText.text = "ทีมเรา 0%";
                modeText.text = "ไม่มี TaskRaceModeManager";
                return;
            }

            float r = manager.RemainingSeconds;
            int secs = Mathf.CeilToInt(r);
            timerText.text = $"เวลา {secs / 60:00}:{secs % 60:00}";
            timerText.color = r <= 10f ? T.danger : manager.BridgeRaid && r <= manager.warningAtRemaining ? T.warn : T.ink;

            pointsText.text = "คะแนนทีม " + manager.LocalPoints;
            if (rivalText != null)
                rivalText.text = round != null
                    ? $"คู่แข่ง {manager.RivalPoints} · เสร็จ {round.DoneCountOf(manager.OtherTeam)}/{round.TaskCount}"
                    : "คู่แข่ง " + manager.RivalPoints;
            if (teamPctText != null)
            {
                float p = round != null && round.TotalWeight > 0 ? round.DoneWeight / (float)round.TotalWeight : 0f;
                teamPctText.text = "ทีมเรา " + Mathf.RoundToInt(p * 100f) + "%";
            }
            if (footerText != null) footerText.text = AdviceLine();
            if (debugModeLabel != null) debugModeLabel.text = manager.BridgeRaid ? "โหมด: Raid" : "โหมด: Pure";

            if (!manager.BridgeRaid) modeText.text = "Pure Race · สะพานปิดทั้งแมตช์";
            else if (manager.AreBridgesOpen) modeText.text = "Bridge Raid · สะพานเปิดแล้ว";
            else if (r <= manager.warningAtRemaining)
                modeText.text = "สะพานจะเปิดใน " + Mathf.Max(0, Mathf.CeilToInt(r - manager.openAtRemaining)) + " วิ";
            else modeText.text = "Bridge Raid · สะพานเปิดที่ " + Mathf.RoundToInt(manager.openAtRemaining) + " วิ";

            if (timeFill != null)
            {
                var anchor = timeFill.anchorMax;
                anchor.x = manager.matchDuration > 0f ? Mathf.Clamp01(r / manager.matchDuration) : 0f;
                timeFill.anchorMax = anchor;
                var img = timeFill.GetComponent<Image>();
                if (img != null) img.color = r <= 10f ? T.danger : r <= manager.warningAtRemaining ? T.warn : T.primary;
            }
        }

        // ---------------------------------------------------------------- pause / exit

        void BuildPause()
        {
            // The top-right corner is the one spot no island prop or task row reaches on a 20:9 phone.
            pauseButton = BobUi.Button(page, "หยุด", T.surfaceCream, PauseMatch, 26, T.ink);
            BobUi.Pin((RectTransform)pauseButton.transform, new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(-cardInset.x, -cardInset.y), new Vector2(104f, 56f));

            pauseRoot = BobUi.Stretch(BobUi.Rect("Pause", page));
            var dim = pauseRoot.gameObject.AddComponent<Image>();
            dim.color = T.dim;
            dim.raycastTarget = true;

            var panel = BobUi.Panel(pauseRoot, T.surface, null, "PauseCard");
            var p = (RectTransform)panel.transform;
            BobUi.Pin(p, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(900f, 560f));

            var title = BobUi.Label(p, "หยุดเกม", 72, T.ink, TextAnchor.MiddleCenter, FontStyle.Bold);
            BobUi.Pin(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -36f), new Vector2(820f, 96f));

            var sub = BobUi.Label(p, "นาฬิกาและคู่แข่งหยุดชั่วคราว", 30, T.inkMuted, TextAnchor.MiddleCenter);
            BobUi.Pin(sub.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -140f), new Vector2(820f, 48f));

            var resume = BobUi.Button(p, "เล่นต่อ", T.primary, ResumeMatch, T.buttonLg, T.ink);
            BobUi.Pin((RectTransform)resume.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 156f), new Vector2(760f, 140f));

            var quit = BobUi.Button(p, "ออกจากการแข่งขัน", T.surfaceCream, QuitMatch, T.buttonLg, T.ink);
            BobUi.Pin((RectTransform)quit.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 20f), new Vector2(760f, 120f));

            pauseRoot.gameObject.SetActive(false);
        }

        /// <summary>Esc / Android back: the card closes first, then pause toggles; the result overlay keeps the key.</summary>
        void OnBackPressed()
        {
            if (ResultOpen) return;
            if (pingWheelOpen) { ClosePingWheel(); return; }
            if (CardOpen) { TaskSpot.CancelOpenSessions(); HideCard(); return; }
            if (PauseOpen) ResumeMatch();
            else PauseMatch();
        }

        void PauseMatch()
        {
            if (manager == null || pauseRoot == null) return;
            if (!manager.SetPaused(true)) return;
            pauseRoot.gameObject.SetActive(true);
            pauseRoot.SetAsLastSibling();
            if (toast != null) toast.transform.SetAsLastSibling();
            pauseButton.gameObject.SetActive(false);
        }

        void ResumeMatch()
        {
            if (pauseRoot != null) pauseRoot.gameObject.SetActive(false);
            if (manager != null) manager.SetPaused(false);
            RenderPause();
        }

        void QuitMatch()
        {
            ResumeMatch(); // the lobby must never open onto a frozen Time.timeScale
            HideCard();
            BackToLobby();
        }

        void RenderPause()
        {
            if (pauseButton == null) return;
            bool show = !PauseOpen && !CardOpen && !ResultOpen && (manager == null || !manager.LastResult.HasValue);
            if (pauseButton.gameObject.activeSelf != show) pauseButton.gameObject.SetActive(show);
        }

        // ---------------------------------------------------------------- context button

        void BuildMainButton()
        {
            mainButton = BobUi.Button(page, "ทำงาน", T.primary, OnMain, T.buttonLg, T.ink);
            BobUi.Pin((RectTransform)mainButton.transform, new Vector2(1f, 0f), new Vector2(1f, 0f),
                new Vector2(-cardInset.x, cardInset.y), new Vector2(460f, 150f));
            mainLabel = mainButton.GetComponentInChildren<TMP_Text>();
            mainButton.gameObject.SetActive(false);
        }

        void RenderMain()
        {
            if (mainButton == null) return;
            var near = TaskSpot.ActiveNear;
            // T12: the chairs are carried in the world, further out than a station's own reach, so the button can be
            // up with no station near at all.
            var chairTask = BobChairTask.Current;
            string chairLabel = null; bool chairCan = false;
            bool chairPrompt = chairTask != null && chairTask.PromptFor(near, out chairLabel, out chairCan);
            bool show = near != null || chairPrompt;
            if (mainButton.gameObject.activeSelf != show) mainButton.gameObject.SetActive(show);
            if (!show) return;

            if (chairPrompt)
            {
                mainLabel.text = chairLabel;
                PaintMain(chairCan ? T.secondary : T.surfaceCream, chairCan ? Color.white : T.inkMuted);
                mainButton.interactable = chairCan;
                return;
            }

            if (near.IsOpen)
            {
                bool sabotage = near.IsEnemySpot && near.State == SpotState.Clean;
                mainLabel.text = (sabotage ? "กำลังป่วน: " : "กำลังทำ: ") + near.TaskName;
                PaintMain(T.primary, T.ink);
                mainButton.interactable = true;
            }
            else if (near.CanSabotageNow)
            {
                mainLabel.text = "ป่วนจุดงาน: " + near.TaskName;
                PaintMain(T.brandRed, Color.white);
                mainButton.interactable = true;
            }
            else if (near.ShowShieldPrompt)
            {
                int cost = manager != null ? manager.shieldItemCost : 150;
                mainLabel.text = $"ติดเกราะป้องกัน (-{cost} Pts)";
                PaintMain(T.secondary, Color.white);
                mainButton.interactable = near.CanAffordShield;
            }
            else if (near.State == SpotState.Clean)
            {
                mainLabel.text = "สะอาดแล้ว: " + near.TaskName;
                PaintMain(T.surfaceCream, T.inkMuted);
                mainButton.interactable = false;
            }
            else
            {
                mainLabel.text = "ทำงาน: " + near.TaskName;
                PaintMain(T.primary, T.ink);
                mainButton.interactable = true;
            }
        }

        void PaintMain(Color bg, Color fg)
        {
            if (mainButton == null) return;
            var img = mainButton.targetGraphic as Image ?? mainButton.GetComponent<Image>();
            if (img != null) img.color = bg;
            if (mainLabel != null) mainLabel.color = fg;
        }

        void OnMain()
        {
            // T12 takes the press before the card path: grab the chair at his feet or put the one in hand into a slot.
            // It runs before the "no station near" return because the chairs stand outside the station's reach.
            var chairTask = BobChairTask.Current;
            if (chairTask != null && chairTask.TryPress(TaskSpot.ActiveNear)) return;

            var near = TaskSpot.ActiveNear;
            if (near == null) return;

            if (near.IsOpen) { near.Close(false); HideCard(); return; } // release the TASK_LOCK; the 10 s progress stays

            if (near.CanSabotageNow)
            {
                if (near.TrySabotage(playerName, out var sabotageMessage)) BeginCard(near, true);
                else if (!string.IsNullOrEmpty(sabotageMessage)) ShowToast(sabotageMessage);
                return;
            }

            if (near.ShowShieldPrompt)
            {
                if (!near.TryBuyShield(out var shieldMessage) && !string.IsNullOrEmpty(shieldMessage)) ShowToast(shieldMessage);
                return; // the success toast comes from TaskRaceModeManager.TryPurchaseShield
            }

            if (near.State != SpotState.Dirty) return;
            if (!near.TryOpen(playerName, out var message))
            {
                if (!string.IsNullOrEmpty(message)) ShowToast(message);
                return;
            }
            BeginCard(near, false);
        }

        // ---------------------------------------------------------------- S28 card

        void BuildMinigameCard()
        {
            cardDim = BobUi.Panel(page, T.dim, null, "S28Dim");
            BobUi.Stretch((RectTransform)cardDim.transform);
            cardDim.raycastTarget = true;
            // The stick stays above the dim so "push it past 70% to close the card" works during a task.
            if (touch != null) touch.KeepJoystickOnTop();

            cardPanel = BobUi.Panel(page, T.surface, null, "S28Card");
            var panel = (RectTransform)cardPanel.transform;
            BobUi.Pin(panel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1400f, 860f));
            var cardOutline = cardPanel.gameObject.AddComponent<Outline>();
            cardOutline.effectColor = new Color(T.ink.r, T.ink.g, T.ink.b, 0.28f);
            cardOutline.effectDistance = new Vector2(4f, -4f);

            cardTitle = BobUi.Label(panel, "ทำงาน", 54, T.ink, TextAnchor.MiddleLeft, FontStyle.Bold);
            BobUi.Pin(cardTitle.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(40f, -12f), new Vector2(780f, 62f));

            cardClock = BobUi.Label(panel, "02:00 · 0", 34, T.inkMuted, TextAnchor.MiddleRight, FontStyle.Bold);
            BobUi.Pin(cardClock.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(-196f, -16f), new Vector2(400f, 54f));

            cardClose = BobUi.Button(panel, "×", T.surfaceCream, CloseCard, 56, T.ink);
            BobUi.Pin((RectTransform)cardClose.transform, new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(-20f, -20f), new Vector2(160f, 160f));

            // The 1240 x 620 play area and the pointer surface (docs 11 S28, docs 16 section 5).
            mini = cardPanel.gameObject.AddComponent<BobMinigameHost>();
            mini.Build(panel);
            cardClose.transform.SetAsLastSibling(); // the play area must not swallow the × at its top-right corner

            cardTrack = BobUi.Panel(panel, new Color(0.94f, 0.87f, 0.73f), null, "S28Track");
            BobUi.Pin((RectTransform)cardTrack.transform, new Vector2(0f, 0f), new Vector2(0f, 0f),
                new Vector2(40f, 40f), new Vector2(1320f, 56f));
            var trackOutline = cardTrack.gameObject.AddComponent<Outline>();
            trackOutline.effectColor = new Color(T.ink.r, T.ink.g, T.ink.b, 0.22f);
            trackOutline.effectDistance = new Vector2(2f, -2f);
            cardFill = BobUi.Rect("Fill", cardTrack.transform);
            cardFill.anchorMin = new Vector2(0f, 0f);
            cardFill.anchorMax = new Vector2(0f, 1f);
            cardFill.offsetMin = cardFill.offsetMax = Vector2.zero;
            var fillImg = cardFill.gameObject.AddComponent<Image>();
            fillImg.color = T.primary;
            fillImg.raycastTarget = false;

            cardStep = BobUi.Label(panel, "ขั้น 1/1", 30, T.ink, TextAnchor.MiddleLeft, FontStyle.Bold);
            BobUi.Pin(cardStep.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f),
                new Vector2(40f, 102f), new Vector2(300f, 40f));

            cardHint = BobUi.Label(panel, "", 30, T.inkMuted, TextAnchor.MiddleCenter);
            BobUi.Pin(cardHint.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 102f), new Vector2(820f, 40f));

            cardPercent = BobUi.Label(panel, "0%", 30, T.ink, TextAnchor.MiddleRight, FontStyle.Bold);
            BobUi.Pin(cardPercent.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 0f),
                new Vector2(-40f, 102f), new Vector2(180f, 40f));

            // One-time, two-second chip: how to close the card (docs 16: teach in the doing). It sits on the
            // instruction line at the foot of the play area, never on the play area itself, and is its own
            // container because a uGUI child draws over its parent's text (the plate used to bury the label).
            cardChipRoot = BobUi.Rect("CardChip", panel);
            BobUi.Pin(cardChipRoot, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 100f), new Vector2(620f, 48f));
            var chipBg = BobUi.Panel(cardChipRoot, T.ink, null, "ChipBg");
            BobUi.Stretch((RectTransform)chipBg.transform);
            chipBg.raycastTarget = false;
            cardChip = BobUi.Label(cardChipRoot, "กด × เพื่อปิด · งานค้างไว้ 10 วิ", 26, T.surface, TextAnchor.MiddleCenter, FontStyle.Bold);
            BobUi.Stretch((RectTransform)cardChip.transform, 12f, 0f, 12f, 0f);
            cardChipRoot.gameObject.SetActive(false);

            HideCard();
        }

        /// <summary>Open the S28 card: a live minigame for the 13 card tasks, a timed bar for T04/T12 and sabotage.</summary>
        void BeginCard(TaskSpot spot, bool sabotage)
        {
            if (spot == null || cardPanel == null) return;
            cardEmergency = false;
            cardSpot = spot;
            cardSabotage = sabotage;
            cardElapsed = 0f;
            // The cart's buffs reach every later card, so the round's buff list is handed over before the card builds.
            if (mini != null) mini.SetRoundBuffs(round != null ? round.CartBuffs : null);
            cardInteractive = !sabotage && mini != null &&
                              mini.Begin(spot, spot.TaskId, round != null ? round.Difficulty : 1);

            if (cardChipRoot != null)
            {
                cardChipUntil = Time.unscaledTime + 2f;
                cardChipRoot.gameObject.SetActive(true);
            }
            if (cardChip != null) cardChip.text = "กด × เพื่อปิด · งานค้างไว้ 10 วิ";

            if (cardInteractive)
            {
                cardStartProgress = 0f;
                cardDuration = 0f;
                cardTitle.text = "ทำงาน: " + spot.TaskName;
                SetCardFill(0f);
            }
            else
            {
                cardStartProgress = sabotage ? 0f : Mathf.Clamp01(spot.RetainedProgress);
                float full = sabotage ? sabotageCardSeconds : Mathf.Max(minimumTaskCardSeconds, spot.ExpectedSeconds);
                cardDuration = Mathf.Max(0.1f, full * Mathf.Max(0.1f, 1f - cardStartProgress));
                if (!sabotage) spot.SetProgress(cardStartProgress);

                cardTitle.text = (sabotage ? "ป่วนจุดงาน: " : "ทำงาน: ") + spot.TaskName;
                if (mini != null)
                    mini.ShowPlaceholder(sabotage ? "กำลังป่วนจุดงาน..." :
                        spot.TaskId == "T04" ? "จัดรถเข็นในห้อง แล้วกลับมาล็อกสูตร" :
                        spot.TaskId == "T12" ? "เดินจับเก้าอี้ไปวางที่โต๊ะ" : "งานนี้ทำในห้อง");
                cardTrack.GetComponent<Image>().color = new Color(0.94f, 0.87f, 0.73f);
                SetCardFill(cardStartProgress);
            }

            if (cardStep != null) cardStep.text = cardInteractive ? mini.StepText : "ขั้น 1/1";
            if (cardHint != null) cardHint.text = cardInteractive ? mini.Hint : "";
            UpdateCardClock();
            cardDim.gameObject.SetActive(true);
            cardPanel.gameObject.SetActive(true);
            cardPanel.transform.SetAsLastSibling();
            if (toast != null) toast.transform.SetAsLastSibling();
        }

        void RenderCard() => TickCard(Time.unscaledDeltaTime);

        /// <summary>One card tick, also used by the verify shot so the frame can be captured in the editor.</summary>
        void TickCard(float dt)
        {
            if (cardEmergency) { TickEmergencyCard(dt); return; }
            if (cardSpot == null) return;
            if (!cardSpot.IsOpen) { HideCard(); return; } // joystick cancel / another close path
            // The round is over (or the next one is counting down): no work, no card, no silent completion.
            if (manager != null && (manager.IsMatchOver || manager.LastResult.HasValue || manager.IsCountingDown))
            {
                HideCard();
                return;
            }

            cardElapsed += dt;
            UpdateCardClock();
            if (cardChipRoot != null && cardChipRoot.gameObject.activeSelf && Time.unscaledTime >= cardChipUntil)
                cardChipRoot.gameObject.SetActive(false);

            if (cardInteractive)
            {
                bool done = mini.Tick(dt);
                float live = mini.Progress;
                cardSpot.SetProgress(Mathf.Min(live, 0.995f));
                SetCardFill(live);
                if (cardStep != null) cardStep.text = mini.StepText;
                if (cardHint != null) cardHint.text = mini.Hint;
                if (!done) return;
                // T04: hand the locked stack to the round, which decides the team buff (docs 16 section 4).
                if (cardSpot.TaskId == "T04" && round != null)
                {
                    bool? before = round.CartPassed;
                    round.NoteCartResult(mini.CartStats);
                    // docs 16 section 2.5 star 3: the first lock of the round is the one that counts, so the series
                    // hears only the transition the round itself accepted.
                    if (!before.HasValue && round.CartPassed.HasValue && manager != null && manager.Series != null)
                        manager.Series.NoteCart(round.CartPassed.Value);
                    ShowToast(mini.CartPassed
                        ? $"รถเข็นผ่านสูตร {mini.CartPassCount}/4 · ทีมได้บัฟ"
                        : $"รถเข็นล็อกแล้ว แต่ผ่าน {mini.CartPassCount}/4 · ยังไม่ได้บัฟ");
                }
                cardSpot.Close(true);
                HideCard();
                return;
            }

            float progress = Mathf.Clamp01(cardStartProgress + cardElapsed / Mathf.Max(0.01f, cardDuration));
            if (!cardSabotage) cardSpot.SetProgress(progress);
            SetCardFill(progress);

            if (progress < 1f) return;
            if (cardSabotage) cardSpot.CompleteSabotage();
            else cardSpot.Close(true);
            HideCard();
        }

        void UpdateCardClock()
        {
            if (cardClock == null) return;
            float r = manager != null ? manager.RemainingSeconds : 0f;
            string time = $"{Mathf.FloorToInt(r / 60f):0}:{Mathf.FloorToInt(r % 60f):00}";
            cardClock.text = manager != null ? $"{time} · {manager.LocalPoints}" : time;
        }

        void SetCardFill(float progress)
        {
            if (cardFill == null) return;
            var anchor = cardFill.anchorMax;
            anchor.x = Mathf.Clamp01(progress);
            cardFill.anchorMax = anchor;
            if (cardPercent != null) cardPercent.text = Mathf.RoundToInt(progress * 100f) + "%";
            var img = cardFill.GetComponent<Image>();
            if (img != null) img.color = cardSabotage ? T.brandRed : T.primary;
        }

        void CloseCard()
        {
            if (cardSpot != null) cardSpot.Close(false);
            HideCard();
        }

        void HideCard()
        {
            // Leaving the panel without the × (result, quit, round handover) must release the station's TASK_LOCK,
            // otherwise the ring keeps saying "กำลังทำ" with no card to finish.
            if (cardSpot != null && cardSpot.IsOpen) cardSpot.Close(false);
            // docs 16 section 2.5 star 2: a card closed while the station is still dirty is a job left half done. It
            // only costs the star if nobody finishes it before the chapter ends, so the id is remembered and forgiven
            // in RenderRows the moment that job goes Done.
            // One count per task, not per close: docs 16 section 2.5 asks for tasks left uncollected, so closing the
            // same job twice is still one job (HashSet.Add says whether this is the first close of that id).
            if (cardSpot != null && !cardSabotage && cardSpot.State == SpotState.Dirty &&
                !string.IsNullOrEmpty(cardSpot.TaskId) && abandonedCards.Add(cardSpot.TaskId))
            {
                if (manager != null && manager.Series != null) manager.Series.NoteCardClosed();
                Debug.Log($"[TaskRace] card closed unfinished: {cardSpot.TaskId} ({cardSpot.SpotId})");
            }
            cardSpot = null;
            cardSabotage = false;
            cardEmergency = false;
            cardInteractive = false;
            if (mini != null) mini.Cancel();
            if (cardChipRoot != null) cardChipRoot.gameObject.SetActive(false);
            if (cardDim != null) cardDim.gameObject.SetActive(false);
            if (cardPanel != null) cardPanel.gameObject.SetActive(false);
        }

        // ---------------------------------------------------------------- match result (S27)

        /// <summary>
        /// Score, the two task counts and the paid reward, with play-again and back-to-lobby. A story chapter is
        /// decided by a whole series, so a closed one gets the chapter card of A3 instead (stars + next chapter).
        /// </summary>
        void ShowResult(MatchResult r)
        {
            HideCard();
            Kill(resultRoot);
            resultRoot = BobUi.Stretch(BobUi.Rect("Result", page));
            var dim = resultRoot.gameObject.AddComponent<Image>();
            dim.color = T.dim;
            dim.raycastTarget = true;

            var panel = BobUi.Panel(resultRoot, T.surface, null, "ResultCard");
            var p = (RectTransform)panel.transform;
            BobUi.Pin(p, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1080f, 760f));

            var series = manager != null ? manager.Series : null;
            // An online room closes as a best-of-three (docs 27 section 10.7): its own card, not the chapter card —
            // the room has no chapter, no stars and no next chapter of the story ladder.
            if (Bob.Game.MatchRequest.Has && Bob.Game.MatchRequest.Online) BuildOnlineCard(p, r);
            else if (series != null && series.Over) BuildChapterCard(p, r, series);
            else BuildMatchCard(p, r);

            shownResult = r;
            hasShownResult = true;
        }

        /// <summary>
        /// The tagline chip of docs 29 section 6 item 7 ("งานสะอาดต้องทำสามรอบ", settled by the owner on 16 Nov 2026).
        /// It explains best-of-three in one line, so it rides on both result cards; the caller decides where the
        /// free band is, because the match card and the chapter card have different ones. Auto-sized down to 18pt:
        /// the chapter chip is only 300u wide and the settled line is twenty Thai glyphs, so a fixed 24pt could clip.
        /// </summary>
        void SloganChip(RectTransform p, Vector2 anchor, Vector2 pivot, Vector2 pos, float width)
        {
            var chip = BobUi.Panel(p, T.surfaceCream, null, "Slogan");
            BobUi.Pin((RectTransform)chip.transform, anchor, pivot, pos, new Vector2(width, 52f));
            var text = BobUi.Label(chip.transform, BobChapters.Slogan, 24, T.inkMuted, TextAnchor.MiddleCenter);
            text.enableAutoSizing = true; text.fontSizeMin = 18; text.fontSizeMax = 24;
            BobUi.Stretch(text.rectTransform, 10f, 4f, 10f, 4f);
        }

        /// <summary>A PvP round or the P3 overlay without a decided series: one round's score and reward.</summary>
        void BuildMatchCard(RectTransform p, MatchResult r)
        {
            string heading = r.Won ? "ชนะ!" : r.Draw ? "เสมอ" : "แพ้";
            var colour = r.Won ? T.success : r.Draw ? T.ink : T.danger;
            var title = BobUi.Label(p, heading, 96, colour, TextAnchor.MiddleCenter, FontStyle.Bold);
            BobUi.Pin(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -28f), new Vector2(1000f, 130f));

            bool localIsB = manager != null && manager.localTeam == TeamId.TeamB;
            int mine = localIsB ? r.TeamBPoints : r.TeamAPoints;
            int theirs = localIsB ? r.TeamAPoints : r.TeamBPoints;
            int myTasks = localIsB ? r.TasksB : r.TasksA;
            int rivalTasks = localIsB ? r.TasksA : r.TasksB;

            var score = BobUi.Label(p, $"เรา {mine} : {theirs} คู่แข่ง", 60, T.ink, TextAnchor.MiddleCenter, FontStyle.Bold);
            BobUi.Pin(score.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -168f), new Vector2(1000f, 84f));

            var tasks = BobUi.Label(p, $"งานเสร็จ {myTasks}/{r.TaskCount} · คู่แข่ง {rivalTasks}/{r.TaskCount}",
                38, T.inkMuted, TextAnchor.MiddleCenter);
            BobUi.Pin(tasks.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -252f), new Vector2(1000f, 60f));

            RewardBlock(p, r, -326f);
            // Band between the reward block (-326..-474) and the buttons (-560..-720): the only centred gap on
            // this card, so the tagline sits there rather than beside the score line.
            SloganChip(p, new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, -517f), 460f);

            var again = BobUi.Button(p, "เล่นอีกครั้ง", T.primary, RestartFromResult, T.buttonLg, T.ink);
            BobUi.Pin((RectTransform)again.transform, new Vector2(1f, 0f), new Vector2(1f, 0f),
                new Vector2(-40f, 40f), new Vector2(480f, 160f));
            var home = BobUi.Button(p, "กลับเมนู", T.surfaceCream, BackToLobby, T.buttonLg, T.ink);
            BobUi.Pin((RectTransform)home.transform, new Vector2(0f, 0f), new Vector2(0f, 0f),
                new Vector2(40f, 40f), new Vector2(480f, 160f));
        }

        // ---------------------------------------------------------------- online series result (docs 27 10.7)

        /// <summary>
        /// The end of an online best-of-three: the shared standing the room applied, the last round's line, and one
        /// next step — the host opens a fresh series, everybody else waits for it or goes home.
        /// </summary>
        void BuildOnlineCard(RectTransform p, MatchResult r)
        {
            var room = Bob.Ugs.BobOnlineRoom.Instance;
            var s = room != null && room.Series != null ? room.Series : manager != null ? manager.Series : null;
            bool abandoned = s != null && s.Abandoned;
            bool won = s != null && s.LocalTookSeries;
            bool draw = s != null && !won && !abandoned && s.LocalWins == s.RivalWins;

            string heading = abandoned ? "ยกเลิกซีรีส์" : won ? "ชนะซีรีส์!" : draw ? "เสมอซีรีส์" : "แพ้ซีรีส์";
            var colour = abandoned ? T.inkMuted : won ? T.success : draw ? T.ink : T.danger;
            var title = BobUi.Label(p, heading, 92, colour, TextAnchor.MiddleCenter, FontStyle.Bold);
            BobUi.Pin(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -28f), new Vector2(1000f, 120f));

            var score = BobUi.Label(p, s != null ? $"ชนะ {s.LocalWins} – {s.RivalWins}" : "ซีรีส์", 64, T.ink,
                TextAnchor.MiddleCenter, FontStyle.Bold);
            BobUi.Pin(score.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -160f), new Vector2(900f, 80f));

            var line = s != null ? s.LastRound : null;
            string last = line.HasValue
                ? $"รอบ {line.Value.Number}/{StorySeries.Rounds} · " +
                  (line.Value.Outcome == RoundOutcome.Win ? "เราชนะ" : line.Value.Outcome == RoundOutcome.Loss ? "เราแพ้" : "เสมอ") +
                  $" · งาน {line.Value.LocalTasks}:{line.Value.RivalTasks}"
                : "ยังไม่จบรอบแรก";
            var sub = BobUi.Label(p, last, 34, T.inkMuted, TextAnchor.MiddleCenter);
            BobUi.Pin(sub.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -242f), new Vector2(1000f, 54f));
            if (abandoned)
            {
                var why = BobUi.Label(p, "มีผู้เล่นออกจากห้องกลางซีรีส์ · ไม่มีผู้ชนะ", 28, T.inkMuted, TextAnchor.MiddleCenter);
                BobUi.Pin(why.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                    new Vector2(0f, -294f), new Vector2(1000f, 48f));
            }

            RewardBlock(p, r, -326f);
            SloganChip(p, new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, -517f), 460f);

            bool host = room != null && room.InRoom && room.IsHost;
            if (host)
            {
                var again = BobUi.Button(p, "เริ่มซีรีส์ใหม่", T.primary, StartNewOnlineSeries, T.buttonLg, T.ink);
                BobUi.Pin((RectTransform)again.transform, new Vector2(1f, 0f), new Vector2(1f, 0f),
                    new Vector2(-40f, 40f), new Vector2(480f, 160f));
            }
            else
            {
                var note = BobUi.Label(p, "รอเจ้าของห้องเริ่มซีรีส์ใหม่", 28, T.inkMuted, TextAnchor.MiddleRight);
                BobUi.Pin(note.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 0f),
                    new Vector2(-40f, 92f), new Vector2(560f, 60f));
            }
            var home = BobUi.Button(p, "กลับเมนู", T.surfaceCream, BackToLobby, T.buttonLg, T.ink);
            BobUi.Pin((RectTransform)home.transform, new Vector2(0f, 0f), new Vector2(0f, 0f),
                new Vector2(40f, 40f), new Vector2(480f, 160f));
        }

        /// <summary>Host only: open a fresh best-of-three in the same room, on a new round-one seed.</summary>
        void StartNewOnlineSeries()
        {
            var room = Bob.Ugs.BobOnlineRoom.Instance;
            if (room == null || !room.InRoom || !room.IsHost) return;
            room.StartMatch(Random.Range(1, 100000), Mathf.Max(2, room.Players));
        }

        // ---------------------------------------------------------------- chapter result (A3)

        /// <summary>
        /// The end of a story chapter: the series score, 0-3 stars with the three things each star asks for, the
        /// reward, and "บทถัดไป" for the ladder. The last chapter shows the end-of-story line in that slot instead,
        /// and a still-locked next chapter says the level it wants — the card never ends on a dead button either way.
        /// </summary>
        void BuildChapterCard(RectTransform p, MatchResult r, StorySeries s)
        {
            // 60 taller than before (900): a passed chapter closes with its own line of story, and the last chapter
            // closes the whole story in three lines (docs 29 section 5), which needs its own band above the buttons.
            p.sizeDelta = new Vector2(1080f, 900f);

            var chapter = BobChapters.Saved;
            var next = BobChapters.Next(chapter);
            int level = BobChapters.LevelFromXp(BobTaskRaceProfile.Xp);
            bool nextOpen = next != null && BobChapters.Unlocked(next, level);
            bool won = s.LocalTookSeries;

            string heading = won ? "ผ่านบท!" : s.LocalWins == s.RivalWins ? "เสมอบท" : "ยังไม่ผ่าน";
            var colour = won ? T.success : s.LocalWins == s.RivalWins ? T.ink : T.danger;
            var title = BobUi.Label(p, heading, 92, colour, TextAnchor.MiddleCenter, FontStyle.Bold);
            BobUi.Pin(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -12f), new Vector2(1000f, 104f));

            var name = BobUi.Label(p, $"บท {chapter.Order} · {chapter.Title}", 34, T.inkMuted, TextAnchor.MiddleCenter);
            BobUi.Pin(name.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -120f), new Vector2(1000f, 40f));

            var score = BobUi.Label(p, $"ชนะ {s.LocalWins} – {s.RivalWins}", 64, T.ink, TextAnchor.MiddleCenter, FontStyle.Bold);
            BobUi.Pin(score.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -166f), new Vector2(900f, 72f));

            StarIcons(p, s.Stars);
            // Beside the star row, on the left: the stars only take the middle 324u, and the band below the score
            // line (which ends at -238) belongs to nobody. A centred chip there would have collided with the asks line.
            SloganChip(p, new Vector2(0f, 1f), new Vector2(0f, 0.5f), new Vector2(48f, -294f), 300f);

            string Halo(bool on) => on ? $"<color=#{ColorUtility.ToHtmlStringRGB(T.success)}>✓</color>"
                                       : $"<color=#{ColorUtility.ToHtmlStringRGB(T.inkMuted)}>✕</color>";
            // The three team quality stars of docs 16 section 2.5 sit after the chapter asks. They never decide the
            // chapter — they report how the rounds were played — and each one only appears when it was in play, so a
            // chapter with no stains and no cart is not padded with lines about them.
            var quality = "";
            if (s.EmergencySpawned > 0)
                quality += $" · {Halo(s.EmergencyClean)} คราบ {s.EmergencyCleared}/{s.EmergencySpawned}";
            if (s.CardsAbandoned > 0)
                quality += $" · {Halo(s.NoAbandoned)} ไม่ทิ้งงาน";
            if (s.CartRuns > 0)
                quality += $" · {Halo(s.CartClean)} รถเข็น {s.CartPasses}/{s.CartRuns}";
            var asks = BobUi.Label(p,
                $"{Halo(won)} ชนะบท      {Halo(s.CleanSweep)} ไม่เสียรอบ      {Halo(s.ChallengeOffered)} ชนะขาด 3 งานขึ้นไป{quality}",
                26, T.inkMuted, TextAnchor.MiddleCenter);
            asks.enableAutoSizing = true; asks.fontSizeMin = 16; asks.fontSizeMax = 26;
            BobUi.Pin(asks.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -352f), new Vector2(1080f, 40f));

            RewardBlock(p, r, -400f);

            // Docs 29 section 5: a passed chapter says what it left behind. The last chapter replaces the next-chapter
            // line with the three closing lines of the story, so the ladder ends on an ending and not on a dead note.
            bool finale = next == null && won;
            if (won && !string.IsNullOrEmpty(chapter.ClearedLine))
            {
                var cleared = BobUi.Label(p, chapter.ClearedLine, 24, T.inkMuted, TextAnchor.MiddleCenter);
                cleared.enableAutoSizing = true; cleared.fontSizeMin = 20; cleared.fontSizeMax = 26;
                BobUi.Pin(cleared.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                    new Vector2(0f, -596f), new Vector2(1020f, 52f));
            }

            if (finale)
            {
                var endTitle = BobUi.Label(p, BobChapters.FinaleTitle, 30, T.success, TextAnchor.MiddleCenter, FontStyle.Bold);
                BobUi.Pin(endTitle.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                    new Vector2(0f, -596f), new Vector2(1020f, 40f));
                var endLine = BobUi.Label(p, BobChapters.FinaleLine, 26, T.ink, TextAnchor.MiddleCenter);
                BobUi.Pin(endLine.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                    new Vector2(0f, -642f), new Vector2(1020f, 34f));
                var endNote = BobUi.Label(p, BobChapters.FinaleNote, 21, T.inkMuted, TextAnchor.MiddleCenter);
                BobUi.Pin(endNote.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                    new Vector2(0f, -684f), new Vector2(1020f, 28f));
            }
            else
            {
                var note = BobUi.Label(p, NextNote(next, nextOpen, level), 24,
                    next == null || nextOpen ? T.inkMuted : T.danger, TextAnchor.MiddleCenter);
                BobUi.Pin(note.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                    new Vector2(0f, -662f), new Vector2(1020f, 36f));
            }

            var home = BobUi.Button(p, "กลับเมนู", T.surfaceCream, BackToLobby, 36, T.ink);
            BobUi.Pin((RectTransform)home.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(next != null ? -324f : -170f, 36f), new Vector2(next != null ? 300f : 340f, 132f));

            var again = BobUi.Button(p, "เล่นอีกครั้ง", T.surfaceCream, RestartFromResult, 36, T.ink);
            BobUi.Pin((RectTransform)again.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(next != null ? 0f : 170f, 36f), new Vector2(next != null ? 300f : 340f, 132f));

            if (next == null) return; // chapter 6: the closing lines of docs 29 already sit above the two buttons
            var onward = BobUi.Button(p, "บทถัดไป", nextOpen ? T.primary : T.surfaceCream,
                nextOpen ? (UnityAction)(() => GoToNextChapter(next)) : null, 36, nextOpen ? T.ink : T.inkMuted);
            BobUi.Pin((RectTransform)onward.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(324f, 36f), new Vector2(300f, 132f));
            onward.interactable = nextOpen;
        }

        /// <summary>The line above the chapter buttons: the next chapter's gate (level, then stars), or the end of the story.</summary>
        static string NextNote(ChapterDefinition next, bool open, int level)
        {
            if (next == null) return "จบเนื้อเรื่องหลักแล้ว · เล่นซ้ำเพื่อเก็บดาวให้ครบ";
            if (open) return $"{next.Title} · LV {next.UnlockLevel}";
            if (level < next.UnlockLevel)
                return $"บทถัดไปปลดที่ LV {next.UnlockLevel} · คุณ LV {level} — เล่นซ้ำเพื่อเก็บ XP";
            // The level is there but the previous chapter has not earned its opening stars yet.
            int have = BobChapters.Stars(next.Order - 1);
            return $"ต้องมีดาวบท {next.Order - 1} ครบ {BobChapters.StarsToOpen} ดวง · ตอนนี้ {have}/{BobChapters.StarsToOpen} — เล่นซ้ำบทก่อนหน้า";
        }

        /// <summary>
        /// Three stars off the art library, filled for the ones this chapter earned. The Thai fonts carry no ★ glyph,
        /// so a text row would draw three empty boxes; icon_star / icon_star_empty are the real sprites.
        /// </summary>
        void StarIcons(RectTransform p, int stars)
        {
            const float size = 96f, gap = 18f;
            float x = -(size + gap);
            for (int i = 0; i < 3; i++)
            {
                var star = BobUi.Img(p, i < stars ? "icon_star" : "icon_star_empty", size);
                BobUi.Pin(star.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f),
                    new Vector2(x, -294f), new Vector2(size, size));
                x += size + gap;
            }
        }

        /// <summary>Coins, XP, the leaderboard score and the best points; the chapter card and the match card share this.</summary>
        void RewardBlock(RectTransform p, MatchResult r, float top)
        {
            var reward = BobUi.Panel(p, T.surfaceCream, null, "Reward");
            BobUi.Pin((RectTransform)reward.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, top), new Vector2(960f, 148f));
            var line = BobUi.Label(reward.transform, $"+{r.Coins} เหรียญ     +{r.Xp} XP", 50, T.ink,
                TextAnchor.MiddleCenter, FontStyle.Bold);
            BobUi.Stretch(line.rectTransform, 20f, 62f, 20f, 0f);
            // The score the leaderboard receives (docs 16 2.6), beside the local best so the two numbers can be told apart.
            int score = manager != null ? manager.LastScore : 0;
            string tail = r.NewBest ? $"สถิติใหม่! {r.BestPoints} แต้ม" : $"สถิติสูงสุด {r.BestPoints} แต้ม";
            var best = BobUi.Label(reward.transform, score > 0 ? $"คะแนน {score} · {tail}" : tail,
                28, r.NewBest ? T.accent : T.inkMuted, TextAnchor.MiddleCenter);
            BobUi.Stretch(best.rectTransform, 20f, 12f, 20f, 84f);
        }

        /// <summary>Save the pick and reload the match scene, which applies the chapter's D and clock in Awake.</summary>
        void GoToNextChapter(ChapterDefinition next)
        {
            int level = BobChapters.LevelFromXp(BobTaskRaceProfile.Xp);
            if (!BobChapters.Unlocked(next, level)) return;
            BobChapters.Select(next, level);
            HideResult();
            HideCard();
            // A chapter is always one hero, three rounds — say so explicitly, so a Duo/Team request left in the static
            // MatchRequest can never turn the next chapter into a single round on the wrong island (docs 24 15.22).
            Bob.Game.MatchRequest.Story();
            if (Application.CanStreamedLevelBeLoaded("TaskRace")) SceneManager.LoadScene("TaskRace");
            else Debug.LogWarning("[TaskRace] the TaskRace scene is not in Build Settings: cannot open the next chapter.");
        }

        void HideResult()
        {
            hasShownResult = false;
            Kill(resultRoot);
            resultRoot = null;
        }

        void RestartFromResult()
        {
            HideResult();
            HideCard();
            if (manager != null) manager.RestartMatch();
            else if (round != null) round.StartRound(Random.Range(1, 100000));
        }

        void BackToLobby()
        {
            // Leaving an online round must also release the room: otherwise the lobby would hold a dead player for 30 s
            // and the relay allocation would stay allocated (docs 27).
            if (Bob.Game.MatchRequest.Has && Bob.Game.MatchRequest.Online)
            {
                var room = Bob.Ugs.BobOnlineRoom.Instance;
                if (room != null) _ = room.LeaveAsync();
            }
            if (Application.CanStreamedLevelBeLoaded("Game")) SceneManager.LoadScene("Game");
            else Debug.LogWarning("[TaskRace] the Game scene is not in Build Settings: cannot return to the lobby.");
        }

        static void Kill(RectTransform rt)
        {
            if (rt == null) return;
            if (Application.isPlaying) Destroy(rt.gameObject); else DestroyImmediate(rt.gameObject);
        }

        // ---------------------------------------------------------------- verify hooks

        /// <summary>Verify: draw the S27 overlay from the current score and capture it (rewards are not paid).</summary>
        public void DebugShowResult()
        {
            if (manager == null) return;
            manager.EndMatch(record: false); // raises MatchEnded, which draws the overlay
            if (!hasShownResult && manager.LastResult.HasValue) ShowResult(manager.LastResult.Value);
        }

        public void DebugCloseResult() => HideResult();

        /// <summary>Verify: drop the debug bar from the shot so the render shows the player's HUD.</summary>
        public void SetDebugBarVisible(bool visible)
        {
            if (debugBar != null) debugBar.gameObject.SetActive(visible);
            if (debugToggle != null) debugToggle.gameObject.SetActive(false);
        }

        /// <summary>Verify: open the S28 card on an already locked station and capture it in the editor.</summary>
        public bool DebugOpenCard(TaskSpot spot)
        {
            if (spot == null || cardPanel == null) return false;
            BeginCard(spot, false);
            return cardInteractive;
        }

        /// <summary>Verify: run the card for a moment without a device (the play area animates, nothing is clicked).</summary>
        public void DebugAdvanceCard(float seconds) => TickCard(Mathf.Max(0f, seconds));

        /// <summary>Verify: fire one feedback ring and squash one item, then read what is animating.</summary>
        public string DebugProbeCard() => mini != null ? mini.DebugFeedbackProbe() : "no-host";
        public string DebugCardFx => mini != null ? mini.DebugFeedbackState : "no-host";

        /// <summary>Verify: drop the card without completing the task.</summary>
        public void DebugCloseCard() => HideCard();

        /// <summary>Verify: open the pause card exactly as the ⏸ button does, so the clock really stops.</summary>
        public bool DebugShowPause() { PauseMatch(); return PauseOpen; }

        /// <summary>Verify: เล่นต่อ, the clock runs again.</summary>
        public void DebugClosePause() => ResumeMatch();

        // ---------------------------------------------------------------- toast

        void BuildToast()
        {
            toast = BobUi.Label(page, "", 34, T.danger, TextAnchor.MiddleCenter, FontStyle.Bold);
            BobUi.Pin(toast.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 210f), new Vector2(1100f, 70f));
            BobUi.OutlineText(toast, T.surface, 0.12f);
            toast.gameObject.SetActive(false);
        }

        void ShowToast(string message)
        {
            if (toast == null || string.IsNullOrEmpty(message)) return;
            toast.text = message;
            toast.gameObject.SetActive(true);
            toast.transform.SetAsLastSibling();
            toastUntil = Time.unscaledTime + toastSeconds;
        }

        // ---------------------------------------------------------------- emergency stain (T16)

        /// <summary>
        /// The red banner of docs 16 section 4: who showed up, at which station, and the 25 s grace. It sits at the
        /// bottom centre — the one strip the task list, the action button and the joystick all leave free — and it is
        /// the second way into the card (the world model is the first). Hidden until a stain is up.
        /// </summary>
        void BuildEmergency()
        {
            emergencyRoot = (RectTransform)BobUi.Panel(page, T.surface, null, "Emergency").transform;
            BobUi.Pin(emergencyRoot, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 24f), new Vector2(720f, 132f));

            var stripe = BobUi.Panel(emergencyRoot, T.brandRed, null, "EmergencyStripe");
            BobUi.Pin((RectTransform)stripe.transform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                Vector2.zero, new Vector2(8f, 132f));
            stripe.raycastTarget = false;

            emergencyTitle = BobUi.Label(emergencyRoot, "คราบฉุกเฉิน", 34, T.brandRed, TextAnchor.MiddleLeft, FontStyle.Bold);
            BobUi.Pin(emergencyTitle.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(28f, -12f), new Vector2(520f, 44f));

            emergencySub = BobUi.Label(emergencyRoot, "", 26, T.inkMuted, TextAnchor.MiddleLeft);
            emergencySub.textWrappingMode = TextWrappingModes.NoWrap;
            emergencySub.overflowMode = TextOverflowModes.Ellipsis;
            BobUi.Pin(emergencySub.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(28f, -58f), new Vector2(490f, 34f));

            emergencyButton = BobUi.Button(emergencyRoot, "ปราบ", T.brandRed, OpenEmergencyCard, 32, Color.white);
            BobUi.Pin((RectTransform)emergencyButton.transform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                new Vector2(-18f, 8f), new Vector2(160f, 86f));

            var track = BobUi.Panel(emergencyRoot, T.surfaceCream, null, "EmergencyTrack");
            BobUi.Pin((RectTransform)track.transform, new Vector2(0f, 0f), new Vector2(0f, 0f),
                new Vector2(28f, 14f), new Vector2(632f, 10f));
            emergencyFill = BobUi.Rect("EmergencyFill", track.transform);
            emergencyFill.anchorMin = new Vector2(0f, 0f);
            emergencyFill.anchorMax = new Vector2(1f, 1f);
            emergencyFill.offsetMin = emergencyFill.offsetMax = Vector2.zero;
            var img = emergencyFill.gameObject.AddComponent<Image>();
            img.color = T.brandRed;
            img.raycastTarget = false;

            // The screen-edge arrow of docs 16 section 5 (art row T16): when the stain's station is off-camera the
            // player gets a direction instead of a hunt. Built here, shown by RenderEmergency, never on the bar.
            emergencyArrow = BobUi.Rect("EmergencyArrow", page);
            emergencyArrow.anchorMin = emergencyArrow.anchorMax = new Vector2(0.5f, 0.5f);
            emergencyArrow.pivot = new Vector2(0.5f, 0.5f);
            emergencyArrow.anchoredPosition = Vector2.zero;
            emergencyArrow.sizeDelta = new Vector2(88f, 88f);
            emergencyArrowImg = emergencyArrow.gameObject.AddComponent<Image>();
            emergencyArrowImg.sprite = ArrowSprite();
            emergencyArrowImg.color = T.brandRed;
            emergencyArrowImg.raycastTarget = false;
            emergencyArrow.gameObject.SetActive(false);

            emergencyRoot.gameObject.SetActive(false);
        }

        static Sprite arrowSprite;

        /// <summary>
        /// A triangle pointing up, built once at runtime: the project ships no arrow icon and the TMP faces carry no
        /// arrow glyph, so the marker is a texture like the minigame FX sprites rather than a character.
        /// </summary>
        static Sprite ArrowSprite()
        {
            if (arrowSprite != null) return arrowSprite;
            const int n = 64;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
            var px = new Color32[n * n];
            for (int y = 0; y < n; y++)
            {
                float up = (float)y / (n - 1);              // 0 at the bottom row, 1 at the tip
                float half = 0.54f * (1f - up);             // the body widens towards the bottom
                for (int x = 0; x < n; x++)
                {
                    float d = Mathf.Abs(x - (n - 1) * 0.5f) / (n - 1);
                    px[y * n + x] = d <= half ? new Color32(255, 255, 255, 255) : new Color32(0, 0, 0, 0);
                }
            }
            tex.SetPixels32(px);
            tex.Apply();
            arrowSprite = Sprite.Create(tex, new Rect(0f, 0f, n, n), new Vector2(0.5f, 0.5f), 100f);
            return arrowSprite;
        }

        /// <summary>Point the arrow at the stain while its station is off-camera; hide it once the stain is in frame.</summary>
        void RenderEmergencyArrow(bool show)
        {
            if (emergencyArrow == null) return;
            bool edge = false;
            Vector2 pos = Vector2.zero;
            float angle = 0f;
            if (show && stains != null && stains.HasTarget)
            {
                var cam = canvas != null && canvas.worldCamera != null ? canvas.worldCamera : Camera.main;
                if (cam != null)
                {
                    Vector3 vp = cam.WorldToViewportPoint(stains.TargetPosition);
                    var dir = new Vector2(vp.x - 0.5f, vp.y - 0.5f);
                    if (vp.z < 0f) dir = -dir; // behind the camera: point the long way round
                    bool inView = vp.z > 0f && Mathf.Abs(dir.x) <= 0.40f && Mathf.Abs(dir.y) <= 0.38f;
                    if (!inView && dir.sqrMagnitude > 0.0001f)
                    {
                        var area = page != null && page.rect.width > 1f ? page.rect.size : new Vector2(1920f, 1080f);
                        float halfW = area.x * 0.5f - 76f, halfH = area.y * 0.5f - 100f;
                        float t = Mathf.Min(halfW / Mathf.Max(0.0001f, Mathf.Abs(dir.x)),
                                            halfH / Mathf.Max(0.0001f, Mathf.Abs(dir.y)));
                        pos = dir * t;
                        angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;
                        edge = true;
                    }
                }
            }
            if (emergencyArrow.gameObject.activeSelf != edge) emergencyArrow.gameObject.SetActive(edge);
            if (!edge) return;
            emergencyArrow.anchoredPosition = pos;
            emergencyArrow.localEulerAngles = new Vector3(0f, 0f, angle);
            if (emergencyArrowImg != null)
            {
                float pulse = 0.72f + 0.28f * Mathf.Sin(Time.time * 7f);
                var c = stains.Remaining <= 8f ? T.danger : T.brandRed;
                c.a = Mathf.Clamp01(pulse);
                emergencyArrowImg.color = c;
            }
        }

        void RenderEmergency()
        {
            if (emergencyRoot == null) return;
            bool show = stains != null && stains.Active && !ResultOpen && !PauseOpen && !CardOpen;
            if (emergencyRoot.gameObject.activeSelf != show) emergencyRoot.gameObject.SetActive(show);
            RenderEmergencyArrow(show);
            if (!show) return;

            emergencyTitle.text = "คราบฉุกเฉิน · " + stains.EnemyName;
            emergencySub.text = $"เหลือ {Mathf.CeilToInt(stains.Remaining)} วิ · {stains.TaskName}";
            if (emergencyFill != null)
            {
                var anchor = emergencyFill.anchorMax;
                anchor.x = stains.GraceSeconds > 0f ? Mathf.Clamp01(stains.Remaining / stains.GraceSeconds) : 0f;
                emergencyFill.anchorMax = anchor;
                var img = emergencyFill.GetComponent<Image>();
                if (img != null) img.color = stains.Remaining <= 8f ? T.danger : T.brandRed;
            }
        }

        /// <summary>Open the T16 card: the weakness minigame of the stain, at one difficulty step below the round.</summary>
        void OpenEmergencyCard()
        {
            if (stains == null || !stains.Active || mini == null || CardOpen) return;
            cardEmergency = true;
            cardSpot = null;
            cardSabotage = false;
            cardElapsed = 0f;
            cardStartProgress = 0f;
            cardDuration = 0f;
            mini.SetRoundBuffs(round != null ? round.CartBuffs : null);
            cardInteractive = mini.BeginEmergency(stains.MinigameId, stains.Difficulty);

            if (cardChipRoot != null)
            {
                cardChipUntil = Time.unscaledTime + 2f;
                cardChipRoot.gameObject.SetActive(true);
            }
            if (cardChip != null) cardChip.text = "กด × เพื่อปิด · คราบรอถึงหมดเวลา";

            cardTitle.text = "ปราบคราบ: " + stains.EnemyName;
            if (cardInteractive)
            {
                SetCardFill(0f);
                if (cardStep != null) cardStep.text = mini.StepText;
                if (cardHint != null) cardHint.text = mini.Hint;
            }
            else
            {
                mini.ShowPlaceholder("ปราบคราบ");
            }
            UpdateCardClock();
            cardDim.gameObject.SetActive(true);
            cardPanel.gameObject.SetActive(true);
            cardPanel.transform.SetAsLastSibling();
            if (toast != null) toast.transform.SetAsLastSibling();
        }

        /// <summary>The emergency card ticks on the same unscaled clock, but the 25 s keeps running underneath it.</summary>
        void TickEmergencyCard(float dt)
        {
            if (stains == null || !stains.Active) { HideCard(); return; } // the grace ran out while the card was open
            if (manager != null && (manager.IsMatchOver || manager.LastResult.HasValue || manager.IsCountingDown))
            {
                HideCard();
                return;
            }

            cardElapsed += dt;
            UpdateCardClock();
            if (cardChipRoot != null && cardChipRoot.gameObject.activeSelf && Time.unscaledTime >= cardChipUntil)
                cardChipRoot.gameObject.SetActive(false);

            bool done = mini.Tick(dt);
            SetCardFill(mini.Progress);
            if (cardStep != null) cardStep.text = mini.StepText;
            if (cardHint != null) cardHint.text = mini.Hint;
            if (!done) return;
            stains.Clear();
            HideCard();
        }

        // ---------------------------------------------------------------- quick ping (P03)

        /// <summary>The six words of docs 11 section 5A.5. They are statements and thanks, never blame.</summary>
        static readonly string[] PingWords =
        {
            "ช่วยวางอันนี้", "ฉันไปหยิบเอง", "ยกชั้นบนออกที", "ใกล้หมดเวลา!", "เยี่ยม!", "ขอบคุณ",
        };

        /// <summary>
        /// The F13 icons the wheel was specified with (docs 11 section 5A.5: "วงล้อ 6 ไอคอน"). They are real sprites from
        /// the UI set rather than new art: check = put it there, lightning = I will fetch it myself, up arrow = take the
        /// top shelf off, stopwatch = time is short, star = nice, heart = thanks. The words stay underneath, because the
        /// pictures only carry the meaning once the team has used them a few times.
        /// </summary>
        static readonly string[] PingIcons =
        {
            "icon_check", "stat_speed", "icon_lift", "icon_timer", "icon_star", "icon_heart",
        };

        const float PingButtonSize = 132f, PingDiscSize = 184f, PingRadius = 258f;
        /// <summary>Icon size inside a chip, and how far above the words it sits.</summary>
        const float PingIconSize = 56f, PingIconLift = 34f;
        const float PingFanStart = 195f, PingFanStep = 42f, PingDeadZone = 34f;
        /// <summary>Centre of the fan in page-local units: a thumb-slide up and left of the button, clear of the bar.</summary>
        static readonly Vector2 PingWheelCentre = new Vector2(600f, -110f);

        Camera PingCamera => canvas != null && canvas.worldCamera != null ? canvas.worldCamera : Camera.main;

        /// <summary>The ping exists where a team-mate can hear it; solo hides the button instead of offering a dead word.</summary>
        bool PingAllowed => (round != null && round.Players > 1) ||
                            (Bob.Game.MatchRequest.Has && Bob.Game.MatchRequest.Online);

        void BuildPing()
        {
            // The button sits on the thumb arc above the main action: right edge, above the stain bar, below the clock.
            pingButtonImg = BobUi.Disc(page, T.secondary, PingButtonSize, "PingButton");
            pingButton = (RectTransform)pingButtonImg.transform;
            BobUi.Pin(pingButton, new Vector2(1f, 0f), new Vector2(1f, 0f),
                new Vector2(-cardInset.x, cardInset.y + 150f + 14f), new Vector2(PingButtonSize, PingButtonSize));
            var caption = BobUi.Label(pingButton, "ปิง", 30, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            BobUi.Stretch(caption.rectTransform);
            pingButton.gameObject.AddComponent<PingPad>().Init(this);

            // A full-page catcher under the chips: a tap anywhere else closes the wheel. It is invisible but blocks the
            // walk tap for the moment the wheel is open, which is what a menu must do.
            pingRoot = BobUi.Stretch(BobUi.Rect("PingWheel", page));
            var catcher = pingRoot.gameObject.AddComponent<Image>();
            catcher.color = new Color(0f, 0f, 0f, 0.001f);
            catcher.raycastTarget = true;
            pingRoot.gameObject.AddComponent<PingCatcher>().Init(this);

            // The hub names the hovered word at reading size: the thumb is over the chip it picked, so the words are
            // also written where the eye lands. This is the piece that makes the six chips legible on a phone.
            pingHub = (RectTransform)BobUi.Panel(pingRoot, T.surface, null, "PingHub").transform;
            BobUi.Pin(pingHub, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), PingWheelCentre, new Vector2(300f, 110f));
            var hubOutline = pingHub.gameObject.AddComponent<Outline>();
            hubOutline.effectColor = new Color(T.ink.r, T.ink.g, T.ink.b, 0.25f);
            hubOutline.effectDistance = new Vector2(3f, -3f);
            pingHubLabel = BobUi.Label(pingHub, "เลื่อนนิ้วไปที่คำ\nแล้วปล่อย", 24, T.inkMuted, TextAnchor.MiddleCenter);
            pingHubLabel.enableAutoSizing = true; pingHubLabel.fontSizeMin = 18f; pingHubLabel.fontSizeMax = 26f;
            BobUi.Stretch(pingHubLabel.rectTransform, 12f, 6f, 12f, 6f);

            for (int i = 0; i < PingWords.Length; i++)
            {
                float a = (PingFanStart - i * PingFanStep) * Mathf.Deg2Rad;
                var at = PingWheelCentre + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * PingRadius;
                var disc = BobUi.Disc(pingRoot, T.surface, PingDiscSize, "Ping_" + i);
                BobUi.Pin((RectTransform)disc.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                    at, new Vector2(PingDiscSize, PingDiscSize));
                var outline = disc.gameObject.AddComponent<Outline>();
                outline.effectColor = new Color(T.ink.r, T.ink.g, T.ink.b, 0.22f);
                outline.effectDistance = new Vector2(2f, -2f);
                // Icon on top, word underneath: the chip is a disc of 184u, so the picture takes the upper half and the
                // label keeps the lower 80u (two lines at 20pt). A missing sprite tints white and reads as a blank disc,
                // which is why the words never leave.
                var icon = BobUi.Img(disc.transform, PingIcons[i], PingIconSize);
                BobUi.Pin(icon.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                    new Vector2(0f, PingIconLift), new Vector2(PingIconSize, PingIconSize));
                var text = BobUi.Label(disc.transform, PingWords[i], 20, T.ink, TextAnchor.MiddleCenter, FontStyle.Bold);
                text.enableAutoSizing = true; text.fontSizeMin = 15f; text.fontSizeMax = 20f;
                text.textWrappingMode = TextWrappingModes.Normal;
                BobUi.Stretch(text.rectTransform, 8f, 12f, 8f, 92f);
                disc.gameObject.AddComponent<PingOption>().Init(this, i);
                pingDiscs.Add(disc);
                pingDiscLabels.Add(text);
            }

            // The bubble over the hero's head, 2.5 s (docs 11 section 5A.5). It follows the body while it is up.
            var plate = BobUi.Panel(page, T.surface, null, "PingBubble");
            pingBubble = (RectTransform)plate.transform;
            BobUi.Pin(pingBubble, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(460f, 84f));
            var bubbleOutline = plate.gameObject.AddComponent<Outline>();
            bubbleOutline.effectColor = new Color(T.ink.r, T.ink.g, T.ink.b, 0.3f);
            bubbleOutline.effectDistance = new Vector2(3f, -3f);
            pingBubbleLabel = BobUi.Label(pingBubble, "", 30, T.ink, TextAnchor.MiddleCenter, FontStyle.Bold);
            pingBubbleLabel.enableAutoSizing = true; pingBubbleLabel.fontSizeMin = 22f; pingBubbleLabel.fontSizeMax = 30f;
            BobUi.Stretch(pingBubbleLabel.rectTransform, 16f, 6f, 16f, 6f);
            pingBubbleGroup = plate.gameObject.AddComponent<CanvasGroup>();
            pingBubble.gameObject.SetActive(false);

            pingRoot.gameObject.SetActive(false);
        }

        void RenderPing()
        {
            if (pingButton == null) return;
            bool show = PingAllowed && !PauseOpen && !ResultOpen && !CardOpen;
            if (pingButton.gameObject.activeSelf != show) pingButton.gameObject.SetActive(show);
            if (!show && pingWheelOpen) ClosePingWheel();
            RenderPingBubble();
        }

        void OpenPingWheel()
        {
            if (pingRoot == null) return;
            pingWheelOpen = true;
            pingRoot.gameObject.SetActive(true);
            pingRoot.SetAsLastSibling();
            if (pingButtonImg != null) pingButtonImg.color = T.primary;
            pingHover = -2;   // out of range, so the reset below always repaints the chips left over from last time
            SetPingHover(-1);
        }

        void ClosePingWheel()
        {
            pingWheelOpen = false;
            pingHolding = false;
            if (pingButtonImg != null) pingButtonImg.color = T.secondary;
            if (pingHover != -1) { pingHover = -2; SetPingHover(-1); }
            if (pingRoot != null) pingRoot.gameObject.SetActive(false);
        }

        /// <summary>Highlight one chip and name it in the hub; -1 returns the hint.</summary>
        void SetPingHover(int index)
        {
            if (index == pingHover) return;
            pingHover = index;
            for (int i = 0; i < pingDiscs.Count; i++)
            {
                bool on = i == index;
                if (pingDiscs[i] != null) pingDiscs[i].color = on ? T.secondary : T.surface;
                if (i < pingDiscLabels.Count && pingDiscLabels[i] != null)
                    pingDiscLabels[i].color = on ? Color.white : T.ink;
            }
            if (pingHubLabel == null) return;
            bool named = index >= 0 && index < PingWords.Length;
            pingHubLabel.text = named ? PingWords[index] : "เลื่อนนิ้วไปที่คำ\nแล้วปล่อย";
            pingHubLabel.color = named ? T.ink : T.inkMuted;
        }

        int NearestPingDisc(Vector2 local)
        {
            int best = -1;
            float bestD = PingDiscSize * 0.85f;
            for (int i = 0; i < pingDiscs.Count; i++)
            {
                if (pingDiscs[i] == null) continue;
                float d = (pingDiscs[i].rectTransform.anchoredPosition - local).magnitude;
                if (d <= bestD) { bestD = d; best = i; }
            }
            return best;
        }

        // The pad and the chips call back into these: a hold-and-slide, or a tap on the button and then a tap on a chip.

        void PingDown(PointerEventData e)
        {
            if (pingRoot == null) return;
            if (pingWheelOpen) { ClosePingWheel(); return; } // a second press on the button is always "close"
            OpenPingWheel();
            pingHolding = true;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(page, e.position, PingCamera, out var local))
                pingOrigin = local;
            Bob.Art.BobSfx.Play("ui_tap", 0.6f);
        }

        void PingDrag(PointerEventData e)
        {
            if (!pingWheelOpen || !pingHolding) return;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(page, e.position, PingCamera, out var local)) return;
            // The first few units are the press itself, not a pick: the chip nearest the button would otherwise win
            // before the thumb has moved at all.
            if ((local - pingOrigin).magnitude < PingDeadZone) { SetPingHover(-1); return; }
            SetPingHover(NearestPingDisc(local));
        }

        void PingRelease(PointerEventData e)
        {
            if (!pingWheelOpen) return;
            pingHolding = false;
            if (pingHover >= 0) SendPing(pingHover);
            // No chip under the thumb: the wheel stays up, and the next tap on a chip sends (the tap-then-tap path).
        }

        void PingChoose(int index) => SendPing(index);

        void SendPing(int index)
        {
            if (index < 0 || index >= PingWords.Length) return;
            ClosePingWheel();

            // A call beside a job also points the offline team-mates at it for twelve seconds (TaskRaceRound.PingSpot),
            // so the wheel is a real co-op control, not only a line of text.
            var spot = PingTarget();
            if (spot != null && round != null) round.PingSpot(spot);

            if (pingBubbleLabel != null)
            {
                pingBubbleLabel.text = spot != null ? $"{PingWords[index]} · {spot.TaskName}" : PingWords[index];
                if (pingBubbleGroup != null) pingBubbleGroup.alpha = 1f;
                pingBubble.gameObject.SetActive(true);
                pingBubble.SetAsLastSibling();
            }
            pingBubbleUntil = Time.unscaledTime + 2.5f;
            Bob.Art.BobSfx.Play("ping_pop", 0.8f);
            Debug.Log($"[TaskRacePing] \"{PingWords[index]}\"{(spot != null ? " @ " + spot.SpotId : "")} · {TaskSpot.LocalTeam}");
        }

        /// <summary>The station a call is about: the one under the hero, else the nearest own dirty job within 8 units.</summary>
        TaskSpot PingTarget()
        {
            if (round == null) return null;
            var near = TaskSpot.ActiveNear;
            if (near != null && near.OwnerTeam == TaskSpot.LocalTeam && near.HasTask &&
                near.State == SpotState.Dirty && !near.IsOpen)
                return near;

            var hero = PingHero();
            if (hero == null) return null;
            TaskSpot best = null;
            float bestD = 8f;
            foreach (var spot in round.StationsOf(TaskSpot.LocalTeam))
            {
                if (spot == null || !spot.HasTask || spot.State != SpotState.Dirty || spot.IsOpen) continue;
                float d = Vector3.Distance(spot.transform.position, hero.transform.position);
                if (d <= bestD) { bestD = d; best = spot; }
            }
            return best;
        }

        BobTaskRaceHero PingHero()
        {
            if (pingHero == null) pingHero = FindAnyObjectByType<BobTaskRaceHero>();
            return pingHero;
        }

        /// <summary>Keep the bubble over the head, clamped inside the safe page, fading out over its last 0.35 s.</summary>
        void RenderPingBubble()
        {
            if (pingBubble == null) return;
            bool live = Time.unscaledTime < pingBubbleUntil && !CardOpen && !PauseOpen && !ResultOpen;
            var hero = live ? PingHero() : null;
            var cam = PingCamera;
            if (!live || hero == null || cam == null)
            {
                if (pingBubble.gameObject.activeSelf) pingBubble.gameObject.SetActive(false);
                return;
            }

            var screen = cam.WorldToScreenPoint(hero.transform.position + Vector3.up * 2.15f);
            if (screen.z <= 0f ||
                !RectTransformUtility.ScreenPointToLocalPointInRectangle(page, screen, cam, out var local))
            {
                if (pingBubble.gameObject.activeSelf) pingBubble.gameObject.SetActive(false);
                return;
            }

            if (page != null && page.rect.width > 1f)
            {
                var half = page.rect.size * 0.5f;
                var size = pingBubble.rect.size;
                local.x = Mathf.Clamp(local.x, -half.x + size.x * 0.5f + 12f, half.x - size.x * 0.5f - 12f);
                local.y = Mathf.Clamp(local.y, -half.y + size.y * 0.5f + 150f, half.y - size.y * 0.5f - 120f);
            }
            pingBubble.anchoredPosition = local;
            if (!pingBubble.gameObject.activeSelf) pingBubble.gameObject.SetActive(true);
            if (pingBubbleGroup != null)
                pingBubbleGroup.alpha = Mathf.Clamp01((pingBubbleUntil - Time.unscaledTime) / 0.35f);
        }

        /// <summary>Verify: open the wheel as the button does, so a shot can show the six words without a finger.</summary>
        public bool DebugOpenPing()
        {
            if (pingRoot == null) return false;
            OpenPingWheel();
            SetPingHover(0);
            return pingWheelOpen;
        }
        public void DebugClosePing() => ClosePingWheel();

        // ---------------------------------------------------------------- mate tray (docs 11 HUD-MateTray)

        /// <summary>The right column between the top bar and the ping button is empty: the tray lives there.</summary>
        void BuildMateTray()
        {
            mateTray = BobUi.Rect("MateTray", page);
            BobUi.Pin(mateTray, new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(-cardInset.x, -MateTrayTop), new Vector2(MateChipW, MateChipH));

            mateHint = BobUi.Label(mateTray, "แตะเพื่อเรียกมาช่วยงานใกล้ตัว", 20, T.inkMuted, TextAnchor.MiddleRight);
            BobUi.Pin(mateHint.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(0f, 34f), new Vector2(MateChipW, 26f));

            mateTray.gameObject.SetActive(false);
        }

        /// <summary>A chip exists where an offline team-mate exists; an online room draws the real bodies instead.</summary>
        bool MateTrayAllowed => round != null && round.Players > 1 &&
                                !(Bob.Game.MatchRequest.Has && Bob.Game.MatchRequest.Online);

        static bool IsMate(TaskRaceBotController mate) =>
            mate != null && mate.isActiveAndEnabled && !mate.raidEnemy && mate.team == TaskSpot.LocalTeam;

        void RenderMateTray()
        {
            if (mateTray == null) return;

            if (Time.unscaledTime >= mateScanAt)
            {
                mateScanAt = Time.unscaledTime + 0.75f;
                mateScan = FindObjectsByType<TaskRaceBotController>(FindObjectsSortMode.None);
                System.Array.Sort(mateScan, (a, b) => string.CompareOrdinal(a != null ? a.name : "", b != null ? b.name : ""));
                mateOrder.Clear();
                foreach (var m in mateScan) if (IsMate(m)) mateOrder.Add(m);
            }

            if (mateChips.Count != mateOrder.Count) RebuildMateChips(mateOrder.Count);

            bool show = MateTrayAllowed && mateOrder.Count > 0 && !PauseOpen && !ResultOpen;
            if (mateTray.gameObject.activeSelf != show) mateTray.gameObject.SetActive(show);
            if (!show) return;

            for (int i = 0; i < mateOrder.Count && i < mateLines.Count; i++)
            {
                var line = MateLine(mateOrder[i]);
                if (mateLines[i].text != line) mateLines[i].text = line;
            }
            // the hint hangs under the last chip, so one and two mates both read as a stack
            BobUi.Pin(mateHint.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(0f, -(mateOrder.Count * (MateChipH + MateChipGap)) - 4f), new Vector2(MateChipW, 26f));
        }

        void RebuildMateChips(int count)
        {
            // rebuild only the chips: the hint label is the one child that stays
            for (int i = mateTray.childCount - 1; i >= 0; i--)
            {
                var go = mateTray.GetChild(i).gameObject;
                if (mateHint != null && go == mateHint.gameObject) continue;
                go.SetActive(false);
                if (Application.isPlaying) Destroy(go); else DestroyImmediate(go);
            }
            mateChips.Clear(); mateLines.Clear();

            for (int i = 0; i < count; i++)
            {
                var chip = (RectTransform)BobUi.Panel(mateTray, T.surface, null, "Mate_" + (i + 1)).transform;
                BobUi.Pin(chip, new Vector2(1f, 1f), new Vector2(1f, 1f),
                    new Vector2(0f, -i * (MateChipH + MateChipGap)), new Vector2(MateChipW, MateChipH));

                var avatar = BobUi.Disc(chip, i == 0 ? T.primary : T.secondary, 64f, "Avatar");
                BobUi.Pin((RectTransform)avatar.transform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                    new Vector2(14f, 0f), new Vector2(64f, 64f));
                var number = BobUi.Label(avatar.transform, (i + 1).ToString(), 30, Color.white,
                    TextAnchor.MiddleCenter, FontStyle.Bold);
                BobUi.Stretch(number.rectTransform);

                var who = BobUi.Label(chip, $"เพื่อน {i + 1}", 26, T.ink, TextAnchor.MiddleLeft, FontStyle.Bold);
                who.textWrappingMode = TextWrappingModes.NoWrap;
                who.overflowMode = TextOverflowModes.Ellipsis;
                BobUi.Pin(who.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f),
                    new Vector2(90f, -12f), new Vector2(MateChipW - 104f, 30f));

                var line = BobUi.Label(chip, "", 20, T.inkMuted, TextAnchor.MiddleLeft);
                line.textWrappingMode = TextWrappingModes.NoWrap;
                line.overflowMode = TextOverflowModes.Ellipsis;
                BobUi.Pin(line.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f),
                    new Vector2(90f, -46f), new Vector2(MateChipW - 104f, 34f));

                chip.gameObject.AddComponent<MateChip>().Init(this);
                mateChips.Add(chip);
                mateLines.Add(line);
            }
        }

        /// <summary>One line of "what is that mate doing", straight from the bot's job (never guessed from position).</summary>
        string MateLine(TaskRaceBotController mate)
        {
            if (mate == null) return "";
            switch (mate.State)
            {
                case TaskRaceBotController.MateState.Carrying: return "กำลังยกเก้าอี้ไปช่อง";
                case TaskRaceBotController.MateState.Working: return "กำลังทำ: " + SpotName(mate.Spot);
                case TaskRaceBotController.MateState.Walking: return "ไป: " + SpotName(mate.Spot);
                case TaskRaceBotController.MateState.Sabotaging: return "ป่วนจุดงานคู่แข่ง";
                default: return "ว่าง";
            }
        }

        static string SpotName(TaskSpot spot) =>
            spot == null ? "-" : string.IsNullOrEmpty(spot.TaskName) ? spot.SpotId : spot.TaskName;

        /// <summary>
        /// Tapping a chip is the co-op verb of docs 11 HUD-MateTray: "come help with this". It pings the job beside the
        /// player, and the ping is what the bots obey (TaskRaceRound.PingSpot), so the tap moves a real body.
        /// </summary>
        void CallMateHelp()
        {
            var spot = PingTarget();
            if (spot == null)
            {
                ShowToast("ยืนใกล้จุดงานก่อน แล้วค่อยเรียกเพื่อน");
                Bob.Art.BobSfx.Play("drop_invalid", 0.5f);
                return;
            }
            if (round != null) round.PingSpot(spot);
            ShowToast($"เรียกเพื่อนมาช่วย: {SpotName(spot)}");
            Bob.Art.BobSfx.Play("ui_confirm", 0.7f);
            Debug.Log($"[TaskRaceMateTray] call for help at {spot.SpotId} · {TaskSpot.LocalTeam}");
        }

        /// <summary>Verify: the tray is up with N chips (0 when solo or online).</summary>
        public bool DebugMateTray => mateTray != null && mateTray.gameObject.activeSelf;
        public int DebugMateChips => mateChips.Count;
        /// <summary>Verify: tap a chip without a finger.</summary>
        public void DebugCallMateHelp() => CallMateHelp();

        sealed class MateChip : MonoBehaviour, IPointerClickHandler
        {
            BobTaskRaceHud owner;
            public void Init(BobTaskRaceHud hud) => owner = hud;
            public void OnPointerClick(PointerEventData e) => owner.CallMateHelp();
        }

        sealed class PingPad : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
        {
            BobTaskRaceHud owner;
            public void Init(BobTaskRaceHud hud) => owner = hud;
            public void OnPointerDown(PointerEventData e) => owner.PingDown(e);
            public void OnDrag(PointerEventData e) => owner.PingDrag(e);
            public void OnPointerUp(PointerEventData e) => owner.PingRelease(e);
        }

        sealed class PingOption : MonoBehaviour, IPointerClickHandler
        {
            BobTaskRaceHud owner;
            int index;
            public void Init(BobTaskRaceHud hud, int i) { owner = hud; index = i; }
            public void OnPointerClick(PointerEventData e) => owner.PingChoose(index);
        }

        sealed class PingCatcher : MonoBehaviour, IPointerClickHandler
        {
            BobTaskRaceHud owner;
            public void Init(BobTaskRaceHud hud) => owner = hud;
            public void OnPointerClick(PointerEventData e) => owner.ClosePingWheel();
        }

        // ---------------------------------------------------------------- debug test bar

        void BuildDebugBar()
        {
            if (!DebugBarAllowed) return;

            debugBar = (RectTransform)BobUi.Panel(page, new Color(T.surface.r, T.surface.g, T.surface.b, 0.94f), null, "DebugBar").transform;
            BobUi.Pin(debugBar, new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(-cardInset.x, -120f), new Vector2(756f, 190f));

            var grid = BobUi.Grid(debugBar, new Vector2(176f, 52f), 4, 8f);
            BobUi.Stretch((RectTransform)grid.transform, 8f, 8f, 8f, 8f);

            debugModeLabel = AddDebugButton(grid.transform, "โหมด: Raid", OnDebugMode);
            AddDebugButton(grid.transform, "เวลา -10", OnDebugBack);
            AddDebugButton(grid.transform, "→ 65 วิ", OnDebugSkipToWarning);
            AddDebugButton(grid.transform, "เปิดสะพาน", OnDebugBridges);
            AddDebugButton(grid.transform, "+150 แต้ม", OnDebugPoints);
            AddDebugButton(grid.transform, "ป่วน", OnDebugSabotage);
            AddDebugButton(grid.transform, "เกราะ", OnDebugShield);
            AddDebugButton(grid.transform, "ด่าน +1", OnDebugStage);
            AddDebugButton(grid.transform, "รีเซ็ต", OnDebugReset);
            AddDebugButton(grid.transform, "ซ่อน", HideDebug);

            debugToggle = (RectTransform)BobUi.Button(page, "DBG", T.surfaceCream, ShowDebug, 20, T.ink).transform;
            BobUi.Pin(debugToggle, new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(-cardInset.x, -120f), new Vector2(84f, 40f));
            debugToggle.gameObject.SetActive(false);
        }

        TMP_Text AddDebugButton(Transform parent, string label, UnityAction action)
        {
            var button = BobUi.Button(parent, label, T.surfaceCream, action, 22, T.ink);
            return button.GetComponentInChildren<TMP_Text>();
        }

        void HideDebug()
        {
            if (debugBar != null) debugBar.gameObject.SetActive(false);
            if (debugToggle != null) debugToggle.gameObject.SetActive(true);
        }

        void ShowDebug()
        {
            if (debugBar != null) debugBar.gameObject.SetActive(true);
            if (debugToggle != null) debugToggle.gameObject.SetActive(false);
        }

        void OnDebugMode()
        {
            if (manager == null) return;
            manager.ToggleMode();
            if (debugModeLabel != null) debugModeLabel.text = manager.BridgeRaid ? "โหมด: Raid" : "โหมด: Pure";
        }

        void OnDebugBack() => manager?.DebugAddTime(-10f);

        /// <summary>Jump to the last second before the 5 s warning, so the bridge sequence can be watched from the start.</summary>
        void OnDebugSkipToWarning() => manager?.DebugSetRemaining(Mathf.Max(0f, manager.warningAtRemaining));

        void OnDebugBridges() => manager?.DebugOpenBridges();

        void OnDebugPoints() => manager?.DebugAddPoints(150);

        /// <summary>Cycle the six stages in place: clock, bridge moment, points and the stage rules all follow.</summary>
        void OnDebugStage()
        {
            if (manager == null) return;
            int next = (manager.stage != null ? manager.stage.Order % 6 : 0) + 1;
            manager.DebugApplyStage(next);
        }

        void OnDebugSabotage()
        {
            if (manager == null) return;
            if (!manager.DebugSabotageNearest()) ShowToast("ยังไม่มีจุดงานสะอาดของคู่แข่งให้ป่วน");
        }

        void OnDebugShield()
        {
            if (manager == null) return;
            if (!manager.DebugShieldNearest()) ShowToast("ยังไม่มีจุดงานสะอาดของทีมเราให้ติดเกราะ");
        }

        void OnDebugReset()
        {
            HideCard();
            if (manager != null) manager.DebugResetMatch();
            else if (round != null) round.StartRound(Random.Range(1, 100000));
        }

        static string Hex(Color c) => ColorUtility.ToHtmlStringRGB(c);
    }
}
