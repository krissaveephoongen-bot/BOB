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
// that takes the TASK_LOCK opens the S28 card; the card fills automatically, and the manager closes it when the
// joystick passes 70%.
//
// Layout is placed by hand instead of layout groups: the card is authored in the editor (no layout pass runs
// outside play mode), and fixed row heights keep a wrapped Thai name from sliding into the next row.
using Bob.Game;
using Bob.UI;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
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
        [SerializeField] bool showDebugBar = true;

        const float PadL = 18f, PadR = 18f, PadT = 14f, PadB = 14f;
        const float HeadH = 56f, BarH = 26f, BlockGap = 10f;
        const float FooterH = 104f;
        const float DiscW = 24f, StatusW = 96f, WeightW = 56f, ColGap = 8f;

        Canvas canvas;
        RectTransform page, card, fill, rowsRoot, footer;
        TMP_Text title, percent, footerText, toast;
        Button mainButton;
        TMP_Text mainLabel;

        RectTransform topBar, timeFill;
        TMP_Text timerText, pointsText, rivalText, teamPctText, modeText;

        Image cardDim, cardPanel, cardTrack;
        RectTransform cardFill;
        TMP_Text cardTitle, cardSub, cardPercent, cardChip;
        Button cardClose;
        float cardChipUntil;

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

        static BobUiTheme T => BobUiTheme.Current;
        float ContentW => cardWidth - PadL - PadR;

        /// <summary>Scene builder / verify: point the HUD at a round without relying on Awake.</summary>
        public void Bind(TaskRaceRound value) => round = value;

        /// <summary>Build (or rebuild) the canvas. Safe to call in the editor so the scene carries the HUD.</summary>
        public void Build(Camera cam = null)
        {
            if (round == null) round = FindAnyObjectByType<TaskRaceRound>();
            if (manager == null) manager = FindAnyObjectByType<TaskRaceModeManager>();
            Hook();

            if (canvas == null) CreateCanvas(cam);
            else if (canvas.worldCamera == null) canvas.worldCamera = cam != null ? cam : Camera.main;

            ClearPage();
            BuildTaskList();
            BuildTopBar();
            BuildMainButton();
            BuildMinigameCard();
            BuildToast();
            BuildDebugBar();
            RenderRows();
            HideCard();
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
            if (hooked != null) hooked.ToastRequested -= ShowToast;
            hooked = null;
        }

        void Hook()
        {
            if (hooked == manager) return;
            if (hooked != null) hooked.ToastRequested -= ShowToast;
            hooked = manager;
            if (hooked != null) hooked.ToastRequested += ShowToast;
        }

        void Update()
        {
            if (canvas == null) return;
            if (round != null && round.Version != builtVersion)
            {
                builtVersion = round.Version;
                RenderRows();
            }
            RenderBar();
            RenderTopBar();
            RenderMain();
            RenderCard();
            if (toast != null && toast.gameObject.activeSelf && Time.unscaledTime >= toastUntil)
                toast.gameObject.SetActive(false);
        }

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
            cardDim = cardPanel = cardTrack = null; cardFill = null; cardTitle = cardSub = cardPercent = cardChip = null; cardClose = null;
            debugModeLabel = null;
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
                footerText.text = "ยังไม่มีรอบ (TaskRaceRound)";
                FitCard(0);
                return;
            }

            title.text = $"ใบงาน · ทีมฟ้า {round.DoneCount}/{round.TaskCount}";
            footerText.text = round.Summary;

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

            // category icon instead of a bare colour dot
            var icon = BobUi.Img(row, CategoryIcon(entry.Spec.Category), 30, TaskSpot.CategoryColour(entry.Spec.Category));
            BobUi.Pin(icon.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(4f, 0f), new Vector2(30f, 30f));

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
            BobUi.Pin(topBar, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -cardInset.y), new Vector2(940f, 104f));

            // the clock is the loudest thing on screen (S28/HUD: "see it in half a second")
            timerText = BobUi.Label(topBar, "เวลา 02:00", 48, T.ink, TextAnchor.MiddleLeft, FontStyle.Bold);
            BobUi.Pin(timerText.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(PadL, -10f), new Vector2(330f, 56f));

            modeText = BobUi.Label(topBar, "Bridge Raid", 22, T.inkMuted, TextAnchor.MiddleLeft);
            BobUi.Pin(modeText.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(PadL + 2f, -66f), new Vector2(470f, 28f));

            pointsText = BobUi.Label(topBar, "คะแนนทีม 0", 30, T.ink, TextAnchor.MiddleRight, FontStyle.Bold);
            BobUi.Pin(pointsText.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(-PadR, -10f), new Vector2(310f, 40f));

            rivalText = BobUi.Label(topBar, "คู่แข่ง 0", 24, T.inkMuted, TextAnchor.MiddleRight);
            BobUi.Pin(rivalText.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(-PadR, -52f), new Vector2(310f, 30f));

            teamPctText = BobUi.Label(topBar, "ทีมเรา 0%", 24, T.inkMuted, TextAnchor.MiddleRight, FontStyle.Bold);
            BobUi.Pin(teamPctText.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 0f),
                new Vector2(-PadR, 18f), new Vector2(310f, 28f));

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
            if (rivalText != null) rivalText.text = "คู่แข่ง " + manager.RivalPoints;
            if (teamPctText != null)
            {
                float p = round != null && round.TotalWeight > 0 ? round.DoneWeight / (float)round.TotalWeight : 0f;
                teamPctText.text = "ทีมเรา " + Mathf.RoundToInt(p * 100f) + "%";
            }
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

            cardPanel = BobUi.Panel(page, T.surface, null, "S28Card");
            var panel = (RectTransform)cardPanel.transform;
            BobUi.Pin(panel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1000f, 430f));

            cardTitle = BobUi.Label(panel, "ทำงาน", 46, T.ink, TextAnchor.MiddleLeft, FontStyle.Bold);
            BobUi.Pin(cardTitle.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(36f, -28f), new Vector2(840f, 64f));

            cardSub = BobUi.Label(panel, "", 28, T.inkMuted, TextAnchor.UpperLeft);
            BobUi.Pin(cardSub.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(36f, -100f), new Vector2(900f, 70f));

            // one-time, two-second chip: how to close the card (docs 16: teach in the doing)
            cardChip = BobUi.Label(panel, "ลากจอยเพื่อปิดการ์ด", 26, T.surface, TextAnchor.MiddleCenter, FontStyle.Bold);
            BobUi.Pin(cardChip.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 22f), new Vector2(460f, 42f));
            var chipBg = BobUi.Panel(cardChip.transform, T.ink, null, "ChipBg");
            BobUi.Stretch((RectTransform)chipBg.transform, -24f, -6f, -24f, -6f);
            chipBg.transform.SetAsFirstSibling();
            chipBg.raycastTarget = false;
            cardChip.transform.SetAsLastSibling();
            cardChip.gameObject.SetActive(false);

            cardTrack = BobUi.Panel(panel, T.surfaceCream, null, "S28Track");
            BobUi.Pin((RectTransform)cardTrack.transform, new Vector2(0f, 0f), new Vector2(0f, 0f),
                new Vector2(36f, 92f), new Vector2(928f, 46f));
            cardFill = BobUi.Rect("Fill", cardTrack.transform);
            cardFill.anchorMin = new Vector2(0f, 0f);
            cardFill.anchorMax = new Vector2(0f, 1f);
            cardFill.offsetMin = cardFill.offsetMax = Vector2.zero;
            var fillImg = cardFill.gameObject.AddComponent<Image>();
            fillImg.color = T.primary;
            fillImg.raycastTarget = false;

            cardPercent = BobUi.Label(panel, "0%", 30, T.ink, TextAnchor.MiddleRight, FontStyle.Bold);
            BobUi.Pin(cardPercent.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 0f),
                new Vector2(-36f, 148f), new Vector2(160f, 40f));

            cardClose = BobUi.Button(panel, "×", T.surfaceCream, CloseCard, 42, T.ink);
            BobUi.Pin((RectTransform)cardClose.transform, new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(-24f, -20f), new Vector2(72f, 72f));

            HideCard();
        }

        /// <summary>Open the S28 card for a task (auto-fill over its card time) or a sabotage (2.5–3.0 s).</summary>
        void BeginCard(TaskSpot spot, bool sabotage)
        {
            if (spot == null || cardPanel == null) return;
            cardSpot = spot;
            cardSabotage = sabotage;
            cardElapsed = 0f;
            cardStartProgress = sabotage ? 0f : Mathf.Clamp01(spot.RetainedProgress);
            float full = sabotage ? sabotageCardSeconds : Mathf.Max(minimumTaskCardSeconds, spot.ExpectedSeconds);
            cardDuration = Mathf.Max(0.1f, full * Mathf.Max(0.1f, 1f - cardStartProgress));
            if (!sabotage) spot.SetProgress(cardStartProgress);

            cardTitle.text = (sabotage ? "ป่วนจุดงาน: " : "ทำงาน: ") + spot.TaskName;
            cardSub.text = sabotage
                ? "ขยับจอยเกิน 70% หรือกด × เพื่อยกเลิก"
                : $"งานนี้ใช้เวลาประมาณ {spot.ExpectedSeconds:0} วินาที · ขยับจอยเกิน 70% เพื่อยกเลิก";
            cardTrack.GetComponent<Image>().color = T.surfaceCream;
            if (cardChip != null)
            {
                cardChipUntil = Time.unscaledTime + 2f;
                cardChip.gameObject.SetActive(true);
            }
            SetCardFill(cardStartProgress);
            cardDim.gameObject.SetActive(true);
            cardPanel.gameObject.SetActive(true);
            cardPanel.transform.SetAsLastSibling();
            if (toast != null) toast.transform.SetAsLastSibling();
        }

        void RenderCard()
        {
            if (cardSpot == null) return;
            if (!cardSpot.IsOpen) { HideCard(); return; } // joystick cancel / another close path

            cardElapsed += Time.unscaledDeltaTime;
            if (cardChip != null && cardChip.gameObject.activeSelf && Time.unscaledTime >= cardChipUntil)
                cardChip.gameObject.SetActive(false);
            float progress = Mathf.Clamp01(cardStartProgress + cardElapsed / Mathf.Max(0.01f, cardDuration));
            if (!cardSabotage) cardSpot.SetProgress(progress);
            SetCardFill(progress);

            if (progress < 1f) return;
            if (cardSabotage) cardSpot.CompleteSabotage();
            else cardSpot.Close(true);
            HideCard();
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
            if (cardChip != null) cardChip.gameObject.SetActive(false);
            if (cardDim != null) cardDim.gameObject.SetActive(false);
            if (cardPanel != null) cardPanel.gameObject.SetActive(false);
        }

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
            if (!showDebugBar) return;

            debugBar = (RectTransform)BobUi.Panel(page, new Color(T.surface.r, T.surface.g, T.surface.b, 0.94f), null, "DebugBar").transform;
            BobUi.Pin(debugBar, new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(-cardInset.x, -120f), new Vector2(756f, 122f));

            var grid = BobUi.Grid(debugBar, new Vector2(176f, 52f), 4, 8f);
            BobUi.Stretch((RectTransform)grid.transform, 8f, 8f, 8f, 8f);

            debugModeLabel = AddDebugButton(grid.transform, "โหมด: Raid", OnDebugMode);
            AddDebugButton(grid.transform, "เวลา -10", OnDebugBack);
            AddDebugButton(grid.transform, "เปิดสะพาน", OnDebugBridges);
            AddDebugButton(grid.transform, "+100 แต้ม", OnDebugPoints);
            AddDebugButton(grid.transform, "ป่วน", OnDebugSabotage);
            AddDebugButton(grid.transform, "เกราะ", OnDebugShield);
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

        void OnDebugBridges() => manager?.DebugOpenBridges();

        void OnDebugPoints() => manager?.DebugAddPoints(100);

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
