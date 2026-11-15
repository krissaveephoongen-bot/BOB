// A2 of the journey pass: 3-2-1-ลุย! before the match clock starts. The manager holds RemainingSeconds while this
// runs, so nobody can bank a task on a clock that has not started yet. It owns its own canvas exactly like
// BobSeriesBanner (no GraphicRaycaster, no raycast targets), so the shared battle HUD keeps its layout and the A1
// pause/exit work in BobTaskRaceHud.cs never has to know about this file.
//
// The timing is a pure function (StepAt/TextAt) so Bob/Run Countdown Check can prove the 3 -> 2 -> 1 -> ลุย! order
// without entering Play mode. The 60 fps walk in that check is the contract: four steps, then finished.
using System;
using Bob.Art;
using Bob.Game;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Bob.TaskRace
{
    [DisallowMultipleComponent]
    public sealed class BobRaceCountdown : MonoBehaviour
    {
        /// <summary>The four beats, in order. The last one is the go signal.</summary>
        public static readonly string[] Steps = { "3", "2", "1", "ลุย!" };
        /// <summary>Seconds each number stays on screen (the legacy 2D countdown used 0.6 s).</summary>
        public const float StepSeconds = 0.75f;
        /// <summary>Seconds "ลุย!" stays up before the clock is released.</summary>
        public const float GoSeconds = 0.45f;
        /// <summary>Length of the whole countdown: 3 x 0.75 + 0.45 = 2.7 s.</summary>
        public static float TotalSeconds => StepSeconds * (Steps.Length - 1) + GoSeconds;

        /// <summary>The step showing after `elapsed` seconds, or -1 once the countdown has finished.</summary>
        public static int StepAt(float elapsed)
        {
            if (elapsed < 0f) return 0;
            if (elapsed >= TotalSeconds) return -1;
            int index = (int)(elapsed / StepSeconds);
            return index < Steps.Length ? index : Steps.Length - 1;
        }

        /// <summary>The text showing after `elapsed` seconds, or null once the countdown has finished.</summary>
        public static string TextAt(float elapsed)
        {
            int index = StepAt(elapsed);
            return index < 0 ? null : Steps[index];
        }

        /// <summary>Attach once, at match start. Returns the existing one if a previous round made it.</summary>
        public static BobRaceCountdown Attach(TaskRaceModeManager manager)
        {
            if (manager == null) return null;
            var view = manager.GetComponent<BobRaceCountdown>();
            if (view != null) return view;
            view = manager.gameObject.AddComponent<BobRaceCountdown>();
            view.manager = manager;
            return view;
        }

        TaskRaceModeManager manager;
        Action finished;
        RectTransform page, numberRect;
        TMP_Text number, hint;
        Canvas canvas;
        float startedAt;
        int shown = -1;
        bool running;

        /// <summary>True from Begin until the last step has faded: the manager holds the clock meanwhile.</summary>
        public bool Running => running;

        void Awake() { if (manager == null) manager = GetComponent<TaskRaceModeManager>(); }

        /// <summary>Play the whole sequence once. `onFinished` runs after the last step has left the screen.</summary>
        public void Begin(Action onFinished)
        {
            finished = onFinished;
            running = true;
            startedAt = Time.time; // scaled: a future pause (A1) freezes the countdown with the clock
            shown = -1;
            Ensure();
            page.gameObject.SetActive(true);
            canvas.enabled = true;
            Show(StepAt(0f));
        }

        /// <summary>Drop the countdown without calling onFinished (match ended underneath it).</summary>
        public void Cancel()
        {
            if (!running) return;
            running = false;
            finished = null;
            if (canvas != null) canvas.enabled = false;
        }

        void Update()
        {
            if (!running) return;
            int step = StepAt(Time.time - startedAt);
            if (step < 0) { Finish(); return; }
            if (step != shown) Show(step);
        }

        void Finish()
        {
            running = false;
            if (canvas != null) canvas.enabled = false;
            var done = finished;
            finished = null;
            done?.Invoke();
        }

        void Show(int index)
        {
            if (index < 0 || index >= Steps.Length) return;
            shown = index;
            bool go = index == Steps.Length - 1;
            if (number != null)
            {
                number.text = Steps[index];
                number.color = go ? BobUi.Good : BobUi.Rose;
                number.fontSize = go ? 190f : 280f;
                numberRect.anchoredPosition = new Vector2(0f, go ? 0f : 10f);
                StartCoroutine(BobTween.Pop(numberRect, 0.3f, 1.35f));
            }
            if (hint != null)
            {
                hint.text = go ? "เริ่ม!" : "เตรียมตัว";
                hint.color = go ? BobUi.Good : BobUi.Muted;
            }
            Tone(go ? "round_start" : "timer_tick");
            if (Application.isPlaying && manager != null)
                Debug.Log($"[TaskRace] countdown {index + 1}/{Steps.Length}: \"{Steps[index]}\" at clock {manager.RemainingSeconds:0.#} s.");
        }

        /// <summary>The legacy 2D countdown tones; silent when the scene was opened without the audio prefab.</summary>
        static void Tone(string clip)
        {
            if (FindAnyObjectByType<BobSfx>() == null) return;
            BobSfx.Play(clip);
        }

        // ---------------------------------------------------------------- canvas

        void Ensure()
        {
            if (canvas != null) return;
            var go = new GameObject("CountdownCanvas", typeof(Canvas), typeof(CanvasScaler));
            go.transform.SetParent(transform, false);
            canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 45; // above the series pill (40), below nothing that matters
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 1f;

            page = BobUi.Stretch(BobUi.Rect("Page", go.transform));

            hint = BobUi.Label(page, "เตรียมตัว", 40, BobUi.Muted, TextAnchor.MiddleCenter, FontStyle.Bold);
            BobUi.Pin(hint.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 210f), new Vector2(760f, 60f));
            BobUi.OutlineText(hint, BobUi.Surface, 0.18f);

            number = BobUi.Label(page, Steps[0], 280, BobUi.Rose, TextAnchor.MiddleCenter, FontStyle.Bold);
            numberRect = BobUi.Pin(number.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, 10f), new Vector2(900f, 460f));
            BobUi.OutlineText(number, BobUi.Ink, 0.3f);

            canvas.enabled = false;
        }
    }
}
