// Battle of the Brooms: the Task Race HUD of docs 16 section 2 — the team task list ("ใบงาน"), the weight bar and
// the main context button of docs 24 section 10. Built from code like the other HUDs, with the tokens of docs 11.
//
// The list follows the round's plan order (T04 first). A row shows the task's category disc, its name and its
// weight; เสร็จ means done, ทำอยู่ means a player holds the TASK_LOCK, รอคิว means the task is still in the queue
// and no station has taken it yet (the restock of docs 24 section 14). The bar and the % use weights, the "3/6"
// next to the title uses task counts, exactly as docs 16 section 2.2 asks.
//
// Layout is placed by hand instead of layout groups: the card is authored in the editor (no layout pass runs
// outside play mode), and fixed row heights keep a wrapped Thai name from sliding into the next row.
using Bob.Game;
using Bob.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Bob.TaskRace
{
    [DisallowMultipleComponent]
    public class BobTaskRaceHud : MonoBehaviour
    {
        [SerializeField] TaskRaceRound round;
        [Tooltip("Display name in the TASK_LOCK toast (docs 16 section 2.1).")]
        [SerializeField] string playerName = "ฉัน";

        [Header("Layout in canvas units, 1920x1080 match height (docs 11)")]
        [SerializeField] float cardWidth = 560f;
        [SerializeField] float rowHeight = 54f;
        [SerializeField] float rowGap = 4f;
        [SerializeField] Vector2 cardInset = new Vector2(24f, 24f);
        [SerializeField] float toastSeconds = 2.2f;

        const float PadL = 18f, PadR = 18f, PadT = 14f, PadB = 14f;
        const float HeadH = 56f, BarH = 26f, BlockGap = 10f;
        const float FooterH = 104f;
        const float DiscW = 24f, StatusW = 96f, WeightW = 56f, ColGap = 8f;

        Canvas canvas;
        RectTransform page, card, fill, rowsRoot, footer;
        TMP_Text title, percent, footerText, toast;
        Button mainButton;
        TMP_Text mainLabel;
        int builtVersion = -1;
        float shownPercent = -1f;
        float toastUntil;

        public Canvas UiCanvas => canvas;
        /// <summary>Rows drawn in the task list; the verify log reads this.</summary>
        public int RowCount { get; private set; }

        static BobUiTheme T => BobUiTheme.Current;
        float ContentW => cardWidth - PadL - PadR;

        /// <summary>Scene builder / verify: point the HUD at a round without relying on Awake.</summary>
        public void Bind(TaskRaceRound value) => round = value;

        /// <summary>Build (or rebuild) the canvas. Safe to call in the editor so the scene carries the HUD.</summary>
        public void Build(Camera cam = null)
        {
            if (round == null) round = FindAnyObjectByType<TaskRaceRound>();
            if (canvas == null) CreateCanvas(cam);
            else if (canvas.worldCamera == null) canvas.worldCamera = cam != null ? cam : Camera.main;

            ClearPage();
            BuildCard();
            BuildMainButton();
            BuildToast();
            RenderRows();
        }

        void Awake()
        {
            if (round == null) round = FindAnyObjectByType<TaskRaceRound>();
            if (canvas == null) Build();
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
            RenderMain();
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
        }

        // ---------------------------------------------------------------- task list

        void BuildCard()
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
            fill.anchorMax = new Vector2(0f, 1f);
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
            for (int i = 0; i < tasks.Count; i++) BuildRow(tasks[i], i);
            FitCard(tasks.Count);
        }

        void BuildRow(TaskEntry entry, int index)
        {
            var row = BobUi.Rect("Task_" + entry.Spec.Id, rowsRoot);
            BobUi.Pin(row, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(0f, -index * (rowHeight + rowGap)), new Vector2(ContentW, rowHeight));

            var disc = BobUi.Disc(row, TaskSpot.CategoryColour(entry.Spec.Category), DiscW);
            BobUi.Pin((RectTransform)disc.transform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                Vector2.zero, new Vector2(DiscW, DiscW));

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

        string NameText(TaskEntry entry) =>
            entry.Done ? $"<color=#{Hex(T.success)}>✓</color> {entry.Spec.Name}"
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

        // ---------------------------------------------------------------- context button / toast

        void BuildMainButton()
        {
            mainButton = BobUi.Button(page, "ทำงาน", T.primary, OnMain, T.buttonLg, T.ink);
            BobUi.Pin((RectTransform)mainButton.transform, new Vector2(1f, 0f), new Vector2(1f, 0f),
                new Vector2(-cardInset.x, cardInset.y), new Vector2(460f, 150f));
            mainLabel = mainButton.GetComponentInChildren<TMP_Text>();
            mainButton.gameObject.SetActive(false);
        }

        void BuildToast()
        {
            toast = BobUi.Label(page, "", 34, T.danger, TextAnchor.MiddleCenter, FontStyle.Bold);
            BobUi.Pin(toast.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 210f), new Vector2(1000f, 70f));
            BobUi.OutlineText(toast, T.surface, 0.12f);
            toast.gameObject.SetActive(false);
        }

        void RenderMain()
        {
            if (mainButton == null) return;
            var near = TaskSpot.ActiveNear;
            bool show = near != null;
            if (mainButton.gameObject.activeSelf != show) mainButton.gameObject.SetActive(show);
            if (!show) return;
            mainLabel.text = near.IsOpen ? "กำลังทำ: " + near.TaskName : "ทำงาน: " + near.TaskName;
        }

        void OnMain()
        {
            var near = TaskSpot.ActiveNear;
            if (near == null) return;
            if (near.IsOpen) { near.Close(false); return; } // release the TASK_LOCK; the 10 s partial progress stays
            if (!near.TryOpen(playerName, out var message) && !string.IsNullOrEmpty(message)) ShowToast(message);
        }

        void ShowToast(string message)
        {
            if (toast == null) return;
            toast.text = message;
            toast.gameObject.SetActive(true);
            toastUntil = Time.unscaledTime + toastSeconds;
        }

        static string Hex(Color c) => ColorUtility.ToHtmlStringRGB(c);
    }
}
