// The story-series strip of docs 24 section 15.3 P3: a pill at the bottom of the screen with the round and the
// chapter score, and a card that names the round's winner while the next one counts down. It owns its own canvas so
// the shared battle HUD file keeps its layout and nothing here can swallow a touch (no GraphicRaycaster, no targets).
using Bob.Game;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Bob.TaskRace
{
    [DisallowMultipleComponent]
    public sealed class BobSeriesBanner : MonoBehaviour
    {
        /// <summary>Attach once, at match start, when the manager runs a story chapter.</summary>
        public static BobSeriesBanner Attach(TaskRaceModeManager manager)
        {
            if (manager == null) return null;
            var banner = manager.GetComponent<BobSeriesBanner>();
            if (banner != null) return banner;
            banner = manager.gameObject.AddComponent<BobSeriesBanner>();
            banner.manager = manager;
            return banner;
        }

        TaskRaceModeManager manager;
        RectTransform card;
        Image pill;
        TMP_Text pillText, cardTitle, cardScore, cardHint, cardTag;
        float cardUntil;

        void Awake() { if (manager == null) manager = GetComponent<TaskRaceModeManager>(); }

        void OnEnable()
        {
            if (manager == null) return;
            manager.MatchStarted += OnRoundStarted;
            manager.RoundEnded += OnRoundEnded;
            manager.MatchEnded += OnMatchEnded;
        }

        void OnDisable()
        {
            if (manager == null) return;
            manager.MatchStarted -= OnRoundStarted;
            manager.RoundEnded -= OnRoundEnded;
            manager.MatchEnded -= OnMatchEnded;
        }

        void Start()
        {
            Build();
            RefreshPill();
        }

        void Update()
        {
            if (card == null || !card.gameObject.activeSelf) return;
            float left = Mathf.Max(0f, cardUntil - Time.unscaledTime);
            cardHint.text = $"รอบถัดไปเริ่มใน {Mathf.CeilToInt(left)}…";
            if (left <= 0f) card.gameObject.SetActive(false);
        }

        // ---------------------------------------------------------------- state

        void OnRoundStarted() { if (card != null) card.gameObject.SetActive(false); RefreshPill(); }

        void OnRoundEnded(SeriesRound line)
        {
            if (card == null) return;
            card.gameObject.SetActive(true);
            cardUntil = Time.unscaledTime + Mathf.Max(0.5f, manager.nextRoundDelay);
            var outcome = line.Outcome == RoundOutcome.Win ? "เราชนะรอบนี้"
                        : line.Outcome == RoundOutcome.Loss ? "เราแพ้รอบนี้" : "รอบนี้เสมอ";
            cardTitle.text = $"จบรอบ {line.Number}/{StorySeries.Rounds} · {outcome}";
            cardTitle.color = line.Outcome == RoundOutcome.Win ? BobUi.Good
                            : line.Outcome == RoundOutcome.Loss ? BobUi.Bad : BobUi.Ink;
            cardScore.text = $"เรา {line.LocalWins} - {line.RivalWins} คู่แข่ง";
            cardHint.text = $"รอบถัดไปเริ่มใน {Mathf.CeilToInt(manager.nextRoundDelay)}…";
            if (cardTag != null)
                cardTag.gameObject.SetActive(manager.Series != null && manager.Series.ChallengeOffered);
            RefreshPill();
        }

        void OnMatchEnded(MatchResult result) { if (card != null) card.gameObject.SetActive(false); RefreshPill(); }

        void RefreshPill()
        {
            if (pillText == null) return;
            var series = manager != null ? manager.Series : null;
            if (pill != null) pill.gameObject.SetActive(series != null);
            if (series == null) { pillText.text = ""; return; }
            pillText.text = series.Over
                ? $"จบซีรีส์ · เรา {series.LocalWins} - {series.RivalWins} คู่แข่ง"
                : $"รอบ {series.Round}/{StorySeries.Rounds} · เรา {series.LocalWins} - {series.RivalWins} คู่แข่ง";
        }

        // ---------------------------------------------------------------- canvas

        void Build()
        {
            var go = new GameObject("SeriesCanvas", typeof(Canvas), typeof(CanvasScaler));
            go.transform.SetParent(transform, false);
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 40;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 1f;

            var page = BobUi.Stretch(BobUi.Rect("Page", go.transform));

            var dim = BobUi.Panel(page, new Color(0f, 0f, 0f, 0.35f), null, "SeriesDim");
            BobUi.Stretch((RectTransform)dim.transform);
            dim.raycastTarget = false;
            card = BobUi.Panel(page, BobUi.Surface, null, "SeriesCard").rectTransform;
            BobUi.Pin(card, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(760f, 380f));
            card.GetComponent<Image>().raycastTarget = false;

            cardTitle = BobUi.Label(card, "", 44, BobUi.Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
            BobUi.Pin(cardTitle.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -56f), new Vector2(700f, 70f));
            cardTag = BobUi.Label(card, "ด่านท้าทายปลดแล้ว!", 26, BobUi.Teal, TextAnchor.MiddleCenter, FontStyle.Bold);
            BobUi.Pin(cardTag.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -128f), new Vector2(700f, 40f));
            cardScore = BobUi.Label(card, "", 56, BobUi.Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
            BobUi.Pin(cardScore.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 0f), new Vector2(700f, 90f));
            cardHint = BobUi.Label(card, "", 30, BobUi.Muted, TextAnchor.MiddleCenter);
            BobUi.Pin(cardHint.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 44f), new Vector2(700f, 44f));

            foreach (var text in new[] { cardTitle, cardTag, cardScore, cardHint }) text.raycastTarget = false;
            card.gameObject.SetActive(false);

            // The pill sits between the joystick and the action button, clear of the task card and the top bar.
            pill = BobUi.Panel(page, BobUi.Surface, null, "SeriesPill");
            BobUi.Pin((RectTransform)pill.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 34f), new Vector2(560f, 62f));
            pill.raycastTarget = false;
            pillText = BobUi.Label(pill.transform, "", 26, BobUi.Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
            BobUi.Stretch(pillText.rectTransform, 12, 12, 0, 0);
            pillText.raycastTarget = false;
        }
    }
}
