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
using Bob.Game;
using Bob.Rules;
using Bob.UI;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
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

        RectTransform debugBar, debugToggle;
        TMP_Text debugModeLabel;

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
        public bool CardOpen => cardSpot != null;
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
            BuildDebugBar();
            RenderRows();
            RenderTopBar();
            RenderMain();
            HideCard();
            HideResult();
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
            }
            hooked = null;
        }

        void Hook()
        {
            if (hooked == manager) return;
            if (hooked != null)
            {
                hooked.ToastRequested -= ShowToast;
                hooked.MatchEnded -= OnMatchEnded;
            }
            hooked = manager;
            if (hooked != null)
            {
                hooked.ToastRequested += ShowToast;
                hooked.MatchEnded += OnMatchEnded;
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
            debugModeLabel = null;
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

            var tasks = round.Tasks;
            RowCount = tasks.Count;
            int nextIndex = -1;
            for (int i = 0; i < tasks.Count; i++) if (!tasks[i].Done) { nextIndex = i; break; } // highlight the next job
            for (int i = 0; i < tasks.Count; i++) BuildRow(tasks[i], i, i == nextIndex);
            FitCard(tasks.Count);
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
            : entry.Locked ? $"<color=#{Hex(T.accent)}>⋯</color> {entry.Spec.Name}"
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
            bool show = near != null;
            if (mainButton.gameObject.activeSelf != show) mainButton.gameObject.SetActive(show);
            if (!show) return;

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
                new Vector2(-156f, -16f), new Vector2(400f, 54f));

            cardClose = BobUi.Button(panel, "×", T.surfaceCream, CloseCard, 48, T.ink);
            BobUi.Pin((RectTransform)cardClose.transform, new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(-20f, -20f), new Vector2(120f, 120f));

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
                new Vector2(0f, 100f), new Vector2(508f, 48f));
            var chipBg = BobUi.Panel(cardChipRoot, T.ink, null, "ChipBg");
            BobUi.Stretch((RectTransform)chipBg.transform);
            chipBg.raycastTarget = false;
            cardChip = BobUi.Label(cardChipRoot, "ลากจอยเพื่อปิดการ์ด", 26, T.surface, TextAnchor.MiddleCenter, FontStyle.Bold);
            BobUi.Stretch((RectTransform)cardChip.transform, 12f, 0f, 12f, 0f);
            cardChipRoot.gameObject.SetActive(false);

            HideCard();
        }

        /// <summary>Open the S28 card: a live minigame for the 13 card tasks, a timed bar for T04/T12 and sabotage.</summary>
        void BeginCard(TaskSpot spot, bool sabotage)
        {
            if (spot == null || cardPanel == null) return;
            cardSpot = spot;
            cardSabotage = sabotage;
            cardElapsed = 0f;
            cardInteractive = !sabotage && mini != null &&
                              mini.Begin(spot, spot.TaskId, round != null ? round.Difficulty : 1);

            if (cardChipRoot != null)
            {
                cardChipUntil = Time.unscaledTime + 2f;
                cardChipRoot.gameObject.SetActive(true);
            }

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
            if (cardSpot == null) return;
            if (!cardSpot.IsOpen) { HideCard(); return; } // joystick cancel / another close path

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
            cardSpot = null;
            cardSabotage = false;
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
            if (series != null && series.Over) BuildChapterCard(p, r, series);
            else BuildMatchCard(p, r);

            shownResult = r;
            hasShownResult = true;
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

            var again = BobUi.Button(p, "เล่นอีกครั้ง", T.primary, RestartFromResult, T.buttonLg, T.ink);
            BobUi.Pin((RectTransform)again.transform, new Vector2(1f, 0f), new Vector2(1f, 0f),
                new Vector2(-40f, 40f), new Vector2(480f, 160f));
            var home = BobUi.Button(p, "กลับเมนู", T.surfaceCream, BackToLobby, T.buttonLg, T.ink);
            BobUi.Pin((RectTransform)home.transform, new Vector2(0f, 0f), new Vector2(0f, 0f),
                new Vector2(40f, 40f), new Vector2(480f, 160f));
        }

        // ---------------------------------------------------------------- chapter result (A3)

        /// <summary>
        /// The end of a story chapter: the series score, 0-3 stars with the three things each star asks for, the
        /// reward, and "บทถัดไป" for the ladder. The last chapter shows the end-of-story line in that slot instead,
        /// and a still-locked next chapter says the level it wants — the card never ends on a dead button either way.
        /// </summary>
        void BuildChapterCard(RectTransform p, MatchResult r, StorySeries s)
        {
            // 80 taller than the match card: the next-chapter line needs its own band above the buttons, or it
            // lands inside the reward panel.
            p.sizeDelta = new Vector2(1080f, 840f);

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

            string Halo(bool on) => on ? $"<color=#{ColorUtility.ToHtmlStringRGB(T.success)}>✓</color>"
                                       : $"<color=#{ColorUtility.ToHtmlStringRGB(T.inkMuted)}>✕</color>";
            var asks = BobUi.Label(p,
                $"{Halo(won)} ชนะบท      {Halo(s.CleanSweep)} ไม่เสียรอบ      {Halo(s.ChallengeOffered)} ชนะขาด ≥ 3 งาน",
                26, T.inkMuted, TextAnchor.MiddleCenter);
            BobUi.Pin(asks.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -352f), new Vector2(1020f, 40f));

            RewardBlock(p, r, -400f);

            var note = BobUi.Label(p, NextNote(next, nextOpen, level), 24,
                next == null || nextOpen ? T.inkMuted : T.danger, TextAnchor.MiddleCenter);
            BobUi.Pin(note.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -600f), new Vector2(1020f, 36f));

            var home = BobUi.Button(p, "กลับเมนู", T.surfaceCream, BackToLobby, 36, T.ink);
            BobUi.Pin((RectTransform)home.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(next != null ? -324f : -170f, 36f), new Vector2(next != null ? 300f : 340f, 132f));

            var again = BobUi.Button(p, "เล่นอีกครั้ง", T.surfaceCream, RestartFromResult, 36, T.ink);
            BobUi.Pin((RectTransform)again.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(next != null ? 0f : 170f, 36f), new Vector2(next != null ? 300f : 340f, 132f));

            if (next == null) return; // chapter 6: the end-of-story line already sits above the two buttons
            var onward = BobUi.Button(p, "บทถัดไป", nextOpen ? T.primary : T.surfaceCream,
                nextOpen ? (UnityAction)(() => GoToNextChapter(next)) : null, 36, nextOpen ? T.ink : T.inkMuted);
            BobUi.Pin((RectTransform)onward.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(324f, 36f), new Vector2(300f, 132f));
            onward.interactable = nextOpen;
        }

        /// <summary>The line above the chapter buttons: the next chapter's level gate, or the end of the story.</summary>
        static string NextNote(ChapterDefinition next, bool open, int level)
        {
            if (next == null) return "จบเนื้อเรื่องหลักแล้ว · เล่นซ้ำเพื่อเก็บดาวให้ครบ";
            if (open) return $"{next.Title} · LV {next.UnlockLevel}";
            return $"บทถัดไปปลดที่ LV {next.UnlockLevel} · คุณ LV {level} — เล่นซ้ำเพื่อเก็บ XP";
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

        /// <summary>Coins, XP and the best score; the chapter card and the match card share this block.</summary>
        void RewardBlock(RectTransform p, MatchResult r, float top)
        {
            var reward = BobUi.Panel(p, T.surfaceCream, null, "Reward");
            BobUi.Pin((RectTransform)reward.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, top), new Vector2(960f, 148f));
            var line = BobUi.Label(reward.transform, $"+{r.Coins} เหรียญ     +{r.Xp} XP", 50, T.ink,
                TextAnchor.MiddleCenter, FontStyle.Bold);
            BobUi.Stretch(line.rectTransform, 20f, 62f, 20f, 0f);
            var best = BobUi.Label(reward.transform,
                r.NewBest ? $"สถิติใหม่! {r.BestPoints} แต้ม" : $"สถิติสูงสุด {r.BestPoints} แต้ม",
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
            AddDebugButton(grid.transform, "⏭ 65 วิ", OnDebugSkipToWarning);
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
