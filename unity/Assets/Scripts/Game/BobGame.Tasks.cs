// Battle of the Brooms: Task Race, solo offline prototype (docs 16 and 19). The rental room's task points (L1Points in BobGame.Set.cs) open
// short minigames: T01 sweep along the path, T05 sort the rubbish, T08 clean the glass. One team of one, 120 s, 6 tasks; finishing them all wins.
// Not yet: long / common tasks (T04, T13), emergency stains, rival bar, server (TASK_BEGIN / TASK_LOCK), the other 13 minigames.
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Bob.Art;
using Bob.Modular;
using Bob.Rules;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Bob.Game
{
    public partial class BobGame
    {
        const float TaskRoundTime = 120f, TaskNear = 1.2f;

        sealed class TaskDef { public string Kind, Point, Name; public bool Done; }

        bool taskMode; float taskTime, taskStarted; bool taskEnded;
        float taskRoundTime = TaskRoundTime; // 4.6: scaled by the chapter band (120 / 105 / 90 s)
        readonly List<TaskDef> taskList = new List<TaskDef>();
        RectTransform taskUi, taskRows, taskBarFill; TMP_Text taskTimeText, taskCountText, taskBtnLabel; Button taskBtn;
        TaskDef taskNear, taskOpen;
        RectTransform miniCard, miniArea;
        System.Action<float> miniTick; // runs while a minigame card is open
        readonly List<RectTransform> miniParts = new List<RectTransform>();
        // S28 card chrome: one progress bar at the bottom, a match clock chip, and shared fx feedback
        RectTransform miniBarFill; TMP_Text miniBarPct, miniTimeChip, miniHint;
        float miniProg, miniProgShown, miniHapticAt;
        System.Action<Vector2> miniGhostStroke; // 4.5: the real sweep step the ghost demo replays once
        sealed class MiniFx { public Image img; public RectTransform rt; public Vector2 at; public float size, life, age, rise; }
        readonly List<MiniFx> miniFx = new List<MiniFx>();

        static string TaskName(string kind) => kind == "T01" ? "กวาดตามทาง" : kind == "T05" ? "แยกขยะ" : "เช็ดกระจก";
        /// <summary>4.6: the task family this hero works 25% faster / wider in, for the Home and HUD labels.</summary>
        static string SpecialtyName(string id) => id == "sweep" ? "กวาด" : id == "bubble" ? "ล้าง" : id == "spray" ? "ฉีดน้ำยา" : "ขัด";
        static Vector2 TaskPos(string id) { foreach (var p in L1Points) if (p.id == id) return new Vector2(p.x, RoomH - p.y); return new Vector2(RoomW / 2, RoomH / 2); }

        // ---------------------------------------------------------------- start / end

        void StartTaskRace()
        {
            seed = Random.Range(1, 8388608);
            Time.timeScale = 1; Sfx("ui_confirm");
            R = BobMatch.MakeRound(1, seed); stack.Clear(); hand.Clear(); tasks.Begin(R.Need, 9999);
            ResetRoomForRound(); foreach (var en in enemies) if (en.t != null) en.t.gameObject.SetActive(false);
            taskList.Clear();
            foreach (var (k, p) in new[] { ("T01", "S6"), ("T05", "S9"), ("T08", "S1"), ("T01", "S7"), ("T05", "S9"), ("T08", "S1") })
                taskList.Add(new TaskDef { Kind = k, Point = p, Name = TaskName(k) });
            taskTime = taskRoundTime = TaskRoundTime - (band - 1) * 15f; taskStarted = Time.time; taskEnded = false; taskOpen = null; taskNear = null; taskMode = true;
            phase = Phase.Build; BuildTaskUi(); RenderAll();
            Toast("Task Race: เดินไปที่วงงานแล้วกดทำงาน");
        }

        void ExitTaskRace()
        {
            CloseMini(false); taskMode = false;
            if (taskUi != null) { Destroy(taskUi.gameObject); taskUi = null; }
            foreach (Transform c in hudRoot) c.gameObject.SetActive(true);
            ToTitle();
        }

        void EndTaskRace(bool win)
        {
            if (taskEnded) return; taskEnded = true; CloseMini(false);
            Sfx(win ? "round_win" : "round_lose");
            int done = taskList.Count(t => t.Done); float used = taskRoundTime - taskTime;
            // 4.6: a band keeps its own best score (tasks are worth more than the seconds they took).
            int score = done * 1000 + (win ? 500 : 0) - Mathf.RoundToInt(used * 5f);
            int oldBest = TaskBest(band);
            bool record = score > oldBest;
            if (record) SaveTaskBest(band, score);
            var card = BobUi.Panel(taskUi, BobUi.Surface); BobUi.Pin(card.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(900, 540));
            var t1 = BobUi.Label(card.transform, win ? "ห้องสะอาดแล้ว!" : "หมดเวลา", 64, win ? BobUi.Good : BobUi.Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
            BobUi.Pin(t1.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -50), new Vector2(800, 90));
            var t2 = BobUi.Label(card.transform, $"งานเสร็จ {done}/{taskList.Count}  ·  ใช้เวลา {Mathf.RoundToInt(used)} วิ  ·  บท {band}", 36, BobUi.Ink, TextAnchor.MiddleCenter);
            BobUi.Pin(t2.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 40), new Vector2(800, 70));
            var t3 = BobUi.Label(card.transform, record ? $"สถิติใหม่! {score} คะแนน" : $"คะแนน {score}  ·  ดีสุด {oldBest}", 32, record ? BobUi.Good : BobUi.Muted, TextAnchor.MiddleCenter, FontStyle.Bold);
            BobUi.Pin(t3.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -25), new Vector2(800, 50));
            var b = BobUi.Button(card.transform, "กลับหน้าหลัก", BobUi.Sun, ExitTaskRace, 40);
            BobUi.Pin((RectTransform)b.transform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 50), new Vector2(480, 120));
            xp += (win ? 40 : 10) * band; SaveProfile();
        }

        // ---------------------------------------------------------------- HUD

        void BuildTaskUi()
        {
            if (taskUi != null) Destroy(taskUi.gameObject);
            taskUi = BobUi.Stretch(BobUi.Rect("TaskUi", hudRoot));
            var time = BobUi.Panel(taskUi, BobUi.Surface); BobUi.Pin(time.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -14), new Vector2(520, 100));
            taskTimeText = BobUi.Label(time.transform, Mathf.CeilToInt(taskRoundTime).ToString(), 56, BobUi.Ink, TextAnchor.MiddleCenter, FontStyle.Bold); BobUi.Stretch(taskTimeText.rectTransform);
            var bar = BobUi.Panel(taskUi, new Color(1, 1, 1, 0.85f)); BobUi.Pin(bar.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -126), new Vector2(520, 34));
            var fill = BobUi.Panel(bar.transform, BobUi.Good); taskBarFill = fill.rectTransform; taskBarFill.anchorMin = Vector2.zero; taskBarFill.anchorMax = new Vector2(0, 1); taskBarFill.offsetMin = taskBarFill.offsetMax = Vector2.zero;
            taskCountText = BobUi.Label(bar.transform, "0/6", 26, BobUi.Ink, TextAnchor.MiddleCenter, FontStyle.Bold); BobUi.Stretch(taskCountText.rectTransform);
            var list = BobUi.Panel(taskUi, BobUi.Surface); BobUi.Pin(list.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(14, -14), new Vector2(430, 40 + 66 * taskList.Count));
            var head = BobUi.Label(list.transform, "ใบงานห้องเช่า · บท " + band, 30, BobUi.Ink, TextAnchor.MiddleLeft, FontStyle.Bold); BobUi.Pin(head.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(20, -8), new Vector2(380, 44));
            taskRows = BobUi.Rect("Rows", list.transform); BobUi.Pin(taskRows, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, -58), new Vector2(430, 66 * taskList.Count));
            taskBtn = BobUi.Button(taskUi, "ทำงาน", BobUi.Sun, OpenNearTask, 44);
            BobUi.Pin((RectTransform)taskBtn.transform, Vector2.one * new Vector2(1, 0), new Vector2(1, 0), new Vector2(-60, 70), new Vector2(420, 170));
            taskBtnLabel = taskBtn.GetComponentInChildren<TMP_Text>();
            var exit = BobUi.Button(taskUi, "ออก", BobUi.Surface, ExitTaskRace, 28);
            BobUi.Pin((RectTransform)exit.transform, Vector2.one, Vector2.one, new Vector2(-14, -14), new Vector2(130, 90));
            RefreshTaskRows();
        }

        void RefreshTaskRows()
        {
            BobUi.Clear(taskRows);
            for (int i = 0; i < taskList.Count; i++)
            {
                var t = taskList[i]; bool open = t == taskOpen || t == taskNear;
                var row = BobUi.Panel(taskRows, t.Done ? new Color(0.78f, 0.93f, 0.8f) : open ? BobUi.Sun : new Color(1, 1, 1, 0.6f));
                BobUi.Pin(row.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(12, -i * 66), new Vector2(406, 60));
                var ic = BobUi.Img(row.transform, t.Done ? "icon_check" : "icon_timer", 40, t.Done ? BobUi.Good : BobUi.Muted); BobUi.Pin(ic.rectTransform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(26, 0), new Vector2(40, 40));
                var l = BobUi.Label(row.transform, t.Name + "  (" + t.Point + ")", 26, BobUi.Ink, TextAnchor.MiddleLeft); BobUi.Stretch(l.rectTransform, 60, 0, 8, 0);
            }
            int done = taskList.Count(x => x.Done);
            taskCountText.text = done + "/" + taskList.Count; taskBarFill.anchorMax = new Vector2((float)done / taskList.Count, 1);
        }

        // ---------------------------------------------------------------- loop

        void TickTasks(float dt)
        {
            // hide the cart game's HUD (everything except the joystick and this mode's own UI)
            foreach (Transform c in hudRoot) { bool keep = c == joyBase || c == taskUi; if (c.gameObject.activeSelf != keep) c.gameObject.SetActive(keep); }
            if (taskEnded || taskUi == null) return;
            taskTime -= dt; // the match clock keeps running while a task card is open (S28: match time, not work time)
            taskTimeText.text = Mathf.CeilToInt(Mathf.Max(0, taskTime)).ToString();
            taskTimeText.color = taskTime < 15f ? BobUi.Rose : BobUi.Ink;
            if (taskTime <= 0f) { EndTaskRace(false); return; }
            if (miniCard != null) { miniTick?.Invoke(dt); TickMiniChrome(dt); taskBtn.gameObject.SetActive(false); return; }
            // nearest open task point inside the interaction circle (1.2, doc 16 section 3)
            TaskDef best = null; float bd = TaskNear + 0.0001f;
            foreach (var t in taskList) if (!t.Done) { float d = Vector2.Distance(pos, TaskPos(t.Point)); if (d < bd) { bd = d; best = t; } }
            if (best != taskNear) { taskNear = best; RefreshTaskRows(); }
            taskBtn.gameObject.SetActive(taskNear != null);
            if (taskNear != null) taskBtnLabel.text = "ทำงาน: " + taskNear.Name;
        }

        void OnTaskBack() { if (miniCard != null) CloseMini(false); else ExitTaskRace(); }

        void OpenNearTask()
        {
            if (taskNear == null || miniCard != null) return;
            taskOpen = taskNear; Sfx("ui_tap");
            miniCard = BobUi.Panel(taskUi, new Color(1f, 0.99f, 0.95f)).rectTransform; BobUi.Pin(miniCard, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1400, 860));
            miniArea = BobUi.Rect("Area", miniCard); BobUi.Pin(miniArea, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -14), new Vector2(1300, 620));
            miniParts.Clear(); miniFx.Clear(); miniDrags.Clear();
            BuildMiniChrome(taskOpen.Name);
            if (taskOpen.Kind == "T01") StartSweep(); else if (taskOpen.Kind == "T05") StartSort(); else StartGlass();
            joyActive = false; RefreshTaskRows();
        }

        void CloseMini(bool done)
        {
            BobSfx.Loop("scrub_loop", false);
            if (miniCard != null) Destroy(miniCard.gameObject);
            miniCard = null; miniArea = null; miniTick = null; miniGhostStroke = null; miniParts.Clear(); miniFx.Clear();
            miniBarFill = null; miniBarPct = null; miniTimeChip = null; miniHint = null;
            miniProg = miniProgShown = 0f;
            if (done && taskOpen != null)
            {
                taskOpen.Done = true; MiniHaptic(); Sfx("ui_confirm"); Toast(taskOpen.Name + " เสร็จแล้ว"); taskNear = null;
                if (taskList.All(t => t.Done)) { taskOpen = null; RefreshTaskRows(); EndTaskRace(true); return; }
            }
            taskOpen = null; if (taskUi != null) RefreshTaskRows();
        }

        // ---------------------------------------------------------------- minigame helpers

        // dev bot: when botPtr is set the minigames read this pointer instead of the mouse
        bool botPtr, botDown, botPressed, botReleased; Vector2 botPos;
        List<(Vector2 from, Vector2 to)> miniDrags = new List<(Vector2, Vector2)>();

        bool MiniPointer(out Vector2 local, out bool down, out bool pressed, out bool released)
        {
            if (botPtr) { local = botPos; down = botDown; pressed = botPressed; released = botReleased; botPressed = botReleased = false; return true; }
            down = Input.GetMouseButton(0); pressed = Input.GetMouseButtonDown(0); released = Input.GetMouseButtonUp(0);
            return RectTransformUtility.ScreenPointToLocalPointInRectangle(miniArea, Input.mousePosition, null, out local);
        }

        bool IsHero(string id) => charId == id;

        // ---------------------------------------------------------------- minigame card chrome (S28: a visible verb, one bar, match clock)

        static string ClockText(float s) { s = Mathf.Max(0, s); return Mathf.FloorToInt(s / 60f) + ":" + Mathf.FloorToInt(s % 60f).ToString("00"); }

        void MiniHaptic()
        {
            if (Time.unscaledTime - miniHapticAt < 0.22f) return; // light tick, throttled so a long scrub does not buzz
            miniHapticAt = Time.unscaledTime;
            if (Application.isMobilePlatform) Handheld.Vibrate();
        }

        void BuildMiniChrome(string titleText)
        {
            // a stage behind the work piece so the card never reads as an empty sheet
            var stage = BobUi.Panel(miniArea, new Color(0.93f, 0.9f, 0.82f, 0.9f), "ui_card", "Stage");
            BobUi.Pin(stage.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -6), new Vector2(1150, 566));
            stage.raycastTarget = false;

            var title = BobUi.Label(miniCard, titleText, 42, BobUi.Ink, TextAnchor.MiddleLeft, FontStyle.Bold);
            BobUi.Pin(title.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(36, -18), new Vector2(720, 64));

            var chip = BobUi.Panel(miniCard, BobUi.Surface);
            BobUi.Pin(chip.rectTransform, Vector2.one, Vector2.one, new Vector2(-196, -22), new Vector2(320, 66));
            var clockIcon = BobUi.Img(chip.transform, "icon_timer", 36, BobUi.Ink);
            BobUi.Pin(clockIcon.rectTransform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(20, 0), new Vector2(36, 36));
            miniTimeChip = BobUi.Label(chip.transform, "เวลาแมตช์ " + ClockText(taskTime), 26, BobUi.Ink, TextAnchor.MiddleLeft, FontStyle.Bold);
            BobUi.Pin(miniTimeChip.rectTransform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(66, 0), new Vector2(240, 60));

            var x = BobUi.Button(miniCard, "×", BobUi.Surface, () => CloseMini(false), 54);
            BobUi.Pin((RectTransform)x.transform, Vector2.one, Vector2.one, new Vector2(-14, -14), new Vector2(144, 144));

            // ONE progress per card, at the bottom edge
            var barBg = BobUi.Panel(miniCard, new Color(0.86f, 0.84f, 0.78f));
            BobUi.Pin(barBg.rectTransform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 26), new Vector2(920, 34));
            var fill = BobUi.Panel(barBg.transform, BobUi.Good);
            miniBarFill = fill.rectTransform;
            miniBarFill.anchorMin = Vector2.zero; miniBarFill.anchorMax = new Vector2(0, 1);
            miniBarFill.offsetMin = miniBarFill.offsetMax = Vector2.zero;
            miniBarPct = BobUi.Label(barBg.transform, "0%", 22, BobUi.Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
            BobUi.Stretch(miniBarPct.rectTransform);
            miniProg = miniProgShown = 0f;
        }

        void MiniHint(string text)
        {
            if (miniHint != null) Destroy(miniHint.gameObject);
            miniHint = BobUi.Label(miniArea, text, 28, BobUi.Muted, TextAnchor.MiddleCenter, FontStyle.Bold);
            BobUi.Pin(miniHint.rectTransform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 6), new Vector2(760, 40));
        }

        void SetMiniProgress(float p)
        {
            miniProg = Mathf.Clamp01(p);
            if (miniHint != null && miniProg > 0.03f) miniHint.gameObject.SetActive(false);
        }

        void TickMiniChrome(float dt)
        {
            if (miniCard == null) return;
            if (miniTimeChip != null) miniTimeChip.text = "เวลาแมตช์ " + ClockText(taskTime);
            miniProgShown = Mathf.Lerp(miniProgShown, miniProg, 1f - Mathf.Exp(-9f * dt));
            if (Mathf.Abs(miniProg - miniProgShown) < 0.002f) miniProgShown = miniProg;
            if (miniBarFill != null) miniBarFill.anchorMax = new Vector2(Mathf.Clamp01(miniProgShown), 1);
            if (miniBarPct != null) miniBarPct.text = Mathf.RoundToInt(miniProgShown * 100f) + "%";
            TickMiniFx(dt);
        }

        void MiniFxAt(string sprite, Vector2 at, float size, float life, float rise)
        {
            if (miniArea == null) return;
            var img = BobUi.Img(miniArea, sprite, size, Color.white);
            img.name = "Fx"; img.raycastTarget = false;
            var rt = img.rectTransform; rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(size, size); rt.anchoredPosition = at; rt.SetAsLastSibling();
            miniFx.Add(new MiniFx { img = img, rt = rt, at = at, size = size, life = life, age = 0f, rise = rise });
        }

        void TickMiniFx(float dt)
        {
            for (int i = miniFx.Count - 1; i >= 0; i--)
            {
                var f = miniFx[i];
                if (f.rt == null) { miniFx.RemoveAt(i); continue; }
                f.age += dt;
                float k = f.age / f.life;
                if (k >= 1f) { Destroy(f.rt.gameObject); miniFx.RemoveAt(i); continue; }
                f.rt.anchoredPosition = f.at + new Vector2(0, f.rise * BobTween.EaseOutCubic(k));
                var c = f.img.color; c.a = 1f - k; f.img.color = c;
                float s = f.size * (0.7f + 0.6f * BobTween.EaseOutCubic(k));
                f.rt.sizeDelta = new Vector2(s, s);
            }
        }

        IEnumerator FadeResidue(RectTransform rt)
        {
            if (rt == null) yield break;
            var img = rt.GetComponent<Image>();
            Color c0 = img != null ? img.color : Color.white;
            for (float e = 0; e < 0.35f; e += Time.unscaledDeltaTime)
            {
                if (rt == null) yield break;
                float k = e / 0.35f;
                if (img != null) { var c = c0; c.a = c0.a * (1f - k); img.color = c; }
                rt.localScale = Vector3.one * (1f + 0.35f * k);
                yield return null;
            }
            if (rt != null) rt.gameObject.SetActive(false);
        }

        IEnumerator FlyIntoBin(RectTransform rt, Vector2 from, Vector2 to)
        {
            if (rt == null) yield break;
            for (float e = 0; e < 0.22f; e += Time.unscaledDeltaTime)
            {
                if (rt == null) yield break;
                float k = BobTween.EaseOutCubic(e / 0.22f);
                rt.anchoredPosition = Vector2.Lerp(from, to, k);
                rt.localScale = Vector3.one * (1f - 0.6f * k);
                yield return null;
            }
            if (rt != null) Destroy(rt.gameObject);
        }

        // sprite part inside the play area, registered so the dev bot can find it by name
        RectTransform MiniSymbol(string sprite, string name, float w, float h, Vector2 at, Color tint)
        {
            var img = BobUi.Img(miniArea, sprite, Mathf.Max(w, h), tint);
            img.name = name; img.preserveAspect = false; img.raycastTarget = false;
            var rt = img.rectTransform; rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(w, h); rt.anchoredPosition = at;
            miniParts.Add(rt);
            return rt;
        }

        RectTransform MiniTool(string sprite, float size)
        {
            var rt = MiniSymbol(sprite, "Tool", size, size, new Vector2(0, -9999), Color.white);
            rt.SetAsLastSibling();
            return rt;
        }

        // shop purchase changes the tool you see in S28 (docs 4.6); falls back to the free tool
        string ToolSprite(string id, string fallback) => owned != null && owned.Contains(id) ? "menu_equip_" + id : fallback;

        // ---------------------------------------------------------------- minigames

        // T01: sweep along a winding dust path; the broom is the verb and dust fades in a ring under it
        void StartSweep()
        {
            var dust = new List<(RectTransform rt, Vector2 at)>();
            float reach = IsHero("sweep") ? 96f : 78f; // hero bonus: wider path (+25%)
            const int n = 30;
            for (int i = 0; i < n; i++)
            {
                float t = i / (n - 1f);
                var at = new Vector2(Mathf.Lerp(-520, 520, t), Mathf.Sin(t * Mathf.PI * 2.4f) * 240f);
                dust.Add((MiniSymbol("dust_idle", "Dust", 116, 104, at, new Color(1, 1, 1, 0.92f)), at));
            }
            var broom = MiniTool(ToolSprite("broom", "part_broom"), 156);
            MiniHint("ลากกวาดฝุ่น");
            var cleared = new bool[n]; int done = 0; float sweepAt = 0f;

            bool SweepAt(Vector2 p)
            {
                bool hit = false;
                for (int i = 0; i < n; i++)
                    if (!cleared[i] && Vector2.Distance(dust[i].at, p) < reach)
                    {
                        cleared[i] = true; done++; hit = true;
                        StartCoroutine(FadeResidue(dust[i].rt));
                        if (done % 3 == 0) MiniFxAt("fx_sparkle", dust[i].at, 74f, 0.4f, 26f);
                        if (done == 1 || done % 5 == 0) MiniHaptic();
                    }
                if (hit && Time.unscaledTime - sweepAt > 0.16f)
                {
                    sweepAt = Time.unscaledTime; Sfx("broom_sweep");
                    MiniFxAt("fx_poof", p, 62f, 0.3f, 16f);
                }
                SetMiniProgress(done / (float)n);
                if (done >= Mathf.CeilToInt(n * 0.9f)) CloseMini(true);
                return hit;
            }

            miniTick = dt =>
            {
                if (!MiniPointer(out var p, out var down, out _, out _)) return;
                broom.anchoredPosition = p + new Vector2(44, -38);
                if (!down) return;
                SweepAt(p);
            };
            miniGhostStroke = p => SweepAt(p);

            // 4.5: the very first job ever shows a translucent broom doing one pass by itself. The player watches one
            // stroke (about 1.4 s), the ghost fades, and the rest of the job is real. No paragraph, no separate screen.
            if (TutorialDue()) StartCoroutine(GhostSweep());
        }

        // First-run guide: one ghost stroke, then hand over. Runs inside the real card so the match clock never stops.
        System.Collections.IEnumerator GhostSweep()
        {
            var ghost = BobUi.Img(miniArea, "part_broom", 156, new Color(1, 1, 1, 0.55f));
            ghost.name = "GhostHand"; ghost.raycastTarget = false;
            var rt = ghost.rectTransform; rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(-476, -38); rt.SetAsLastSibling();
            MiniHint("ลากตามไม้กวาด");
            var a = new Vector2(-520f, 0f); var b = new Vector2(-180f, 120f);
            for (float e = 0f; e < 1.4f; e += Time.unscaledDeltaTime)
            {
                if (miniCard == null) yield break;
                float k = Mathf.SmoothStep(0f, 1f, e / 1.4f);
                var p = Vector2.Lerp(a, b, k) + new Vector2(0f, 46f) * Mathf.Sin(k * Mathf.PI);
                rt.anchoredPosition = p + new Vector2(44, -38);
                miniGhostStroke?.Invoke(p);
                yield return null;
            }
            if (miniCard == null) yield break;
            for (float e = 0f; e < 0.3f; e += Time.unscaledDeltaTime)
            {
                if (ghost == null) break;
                var c = ghost.color; c.a = 0.55f * (1f - e / 0.3f); ghost.color = c;
                yield return null;
            }
            if (ghost != null) Destroy(ghost.gameObject);
            miniGhostStroke = null;
            TutorialDone();
            if (miniCard != null) MiniHint("ลากกวาดฝุ่น");
        }

        static bool TutorialDue()
        {
            try { return PlayerPrefs.GetInt("bob.tutorial", 0) == 0; } catch (System.Exception) { return false; }
        }

        static void TutorialDone()
        {
            try { PlayerPrefs.SetInt("bob.tutorial", 1); PlayerPrefs.Save(); } catch (System.Exception) { }
        }

        // T05: drag each piece into the matching bin; a correct drop flies in, a wrong one springs back
        void StartSort()
        {
            var cats = new[] { ("กระดาษ", new Color(0.35f, 0.65f, 0.95f)), ("พลาสติก", new Color(0.98f, 0.8f, 0.25f)), ("เศษอาหาร", new Color(0.4f, 0.75f, 0.45f)) };
            var bins = new List<(RectTransform rt, int cat, Vector2 at)>();
            for (int i = 0; i < 3; i++)
            {
                var at = new Vector2((i - 1) * 380f, -226f);
                var b = BobUi.Panel(miniArea, cats[i].Item2); BobUi.Pin(b.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), at, new Vector2(320, 214)); b.raycastTarget = false;
                var l = BobUi.Label(b.transform, cats[i].Item1, 40, BobUi.Ink, TextAnchor.MiddleCenter, FontStyle.Bold); BobUi.Stretch(l.rectTransform); l.raycastTarget = false;
                var lid = BobUi.Panel(b.transform, new Color(0, 0, 0, 0.18f)); BobUi.Pin(lid.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -10), new Vector2(240, 20)); lid.raycastTarget = false;
                bins.Add((b.rectTransform, i, at));
            }
            var items = new (string name, int cat)[] { ("กล่องนม", 0), ("ขวดน้ำ", 1), ("เปลือกกล้วย", 2), ("หนังสือพิมพ์", 0), ("ถุงพลาสติก", 1), ("ก้างปลา", 2) };
            var homes = new[] { new Vector2(-380, 210), new Vector2(0, 210), new Vector2(380, 210), new Vector2(-380, 66), new Vector2(0, 66), new Vector2(380, 66) };
            var live = new List<(RectTransform rt, int cat, Vector2 home)>();
            for (int i = 0; i < items.Length; i++)
            {
                var home = homes[i];
                var it = BobUi.Panel(miniArea, new Color(1, 1, 1, 1)); BobUi.Pin(it.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), home, new Vector2(232, 128)); it.raycastTarget = false;
                var tag = BobUi.Panel(it.transform, cats[items[i].cat].Item2); BobUi.Pin(tag.rectTransform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(8, 0), new Vector2(16, 96)); tag.raycastTarget = false;
                var l = BobUi.Label(it.transform, items[i].name, 30, BobUi.Ink, TextAnchor.MiddleCenter, FontStyle.Bold); BobUi.Stretch(l.rectTransform, 24, 0, 6, 0); l.raycastTarget = false;
                live.Add((it.rectTransform, items[i].cat, home));
                miniDrags.Add((home, bins[items[i].cat].at + new Vector2(0, 40)));
            }
            MiniHint("ลากลงถัง");
            // 4.6 specialty: the Press hero reads a bin 25% wider (scrub family).
            float binThreshold = IsHero("press") ? 237f : 190f;
            RectTransform held = null; int heldCat = -1; Vector2 heldHome = Vector2.zero;
            miniTick = dt =>
            {
                if (!MiniPointer(out var p, out var down, out var pressed, out var released)) return;
                if (pressed) foreach (var l in live) if (Mathf.Abs(l.rt.anchoredPosition.x - p.x) < 116 && Mathf.Abs(l.rt.anchoredPosition.y - p.y) < 64) { held = l.rt; heldCat = l.cat; heldHome = l.home; held.localScale = Vector3.one * 1.08f; held.SetAsLastSibling(); break; }
                if (held != null && down) held.anchoredPosition = p;
                if (held != null && released)
                {
                    RectTransform bin = null; int binCat = -1;
                    foreach (var b in bins) if (Mathf.Abs(b.at.x - p.x) < binThreshold && p.y < -110f) { bin = b.rt; binCat = b.cat; break; }
                    if (bin != null && binCat == heldCat)
                    {
                        var rt = held; var from = rt.anchoredPosition;
                        live.RemoveAll(l => l.rt == rt);
                        MiniFxAt("fx_sparkle", new Vector2((binCat - 1) * 380f, -170f), 96f, 0.42f, 34f);
                        Sfx("place_layer"); MiniHaptic();
                        StartCoroutine(FlyIntoBin(rt, from, new Vector2((binCat - 1) * 380f, -226f)));
                        SetMiniProgress(1f - live.Count / (float)items.Length);
                        if (live.Count == 0) CloseMini(true);
                    }
                    else
                    {
                        if (bin != null) Sfx("drop_invalid");
                        held.anchoredPosition = heldHome; held.localScale = Vector3.one;
                    }
                    held = null;
                }
            };
        }

        // T08: spray first (the stain turns wet), then wipe the glass; the stain fades in rings under the cloth
        void StartGlass()
        {
            var paneAt = new Vector2(0, 8);
            var pane = BobUi.Panel(miniArea, new Color(0.62f, 0.8f, 0.92f)); BobUi.Pin(pane.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), paneAt, new Vector2(910, 528)); pane.raycastTarget = false;
            var shine = BobUi.Panel(pane.transform, new Color(1, 1, 1, 0.16f)); BobUi.Pin(shine.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-120, 120), new Vector2(300, 520)); shine.raycastTarget = false;

            const int cols = 12, rows = 7; const float cell = 74f;
            var fog = new List<(RectTransform rt, Vector2 at, bool clear)>();
            for (int r = 0; r < rows; r++) for (int c = 0; c < cols; c++)
            {
                var at = paneAt + new Vector2((c - (cols - 1) / 2f) * cell, (r - (rows - 1) / 2f) * cell);
                var rt = MiniSymbol("stain_idle", "Grime", cell + 12, cell + 12, at, new Color(0.72f, 0.74f, 0.66f, 0.9f));
                fog.Add((rt, at, false));
            }
            bool sprayed = false, warned = false; float reach = IsHero("bubble") ? 108f : 88f; // hero bonus: wider wipe (+25%)
            var sprayIcon = BobUi.Img(miniArea, ToolSprite("spray", "part_spray"), 116, Color.white);
            BobUi.Pin(sprayIcon.rectTransform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(46, 132), new Vector2(116, 116));
            var spray = BobUi.Button(miniArea, "ฉีดน้ำยา", BobUi.Sun, () =>
            {
                if (sprayed) return;
                sprayed = true; Sfx("spray_psst"); MiniHaptic();
                foreach (var f in fog) { var img = f.rt.GetComponent<Image>(); if (img != null) img.color = new Color(0.5f, 0.7f, 0.88f, 0.94f); }
                for (int i = 0; i < 5; i++) MiniFxAt("fx_spray_mist", new Vector2(-250 + i * 130, 90 - i * 30), 150f, 0.45f, 22f);
                // 4.6 specialty: the Spray hero's first pass loosens a quarter of the grime outright.
                if (IsHero("spray"))
                    for (int i = 0; i < fog.Count; i += 4)
                        if (!fog[i].clear) { fog[i] = (fog[i].rt, fog[i].at, true); StartCoroutine(FadeResidue(fog[i].rt)); }
                MiniHint("ลากเช็ดให้ใส");
            }, 34);
            BobUi.Pin((RectTransform)spray.transform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(10, 0), new Vector2(220, 160));
            var cloth = MiniTool(ToolSprite("sponge", "part_sponge"), 150);
            MiniHint("ฉีดน้ำยา");
            float scrubAt = 0f;
            miniTick = dt =>
            {
                if (!MiniPointer(out var p, out var down, out _, out _)) return;
                cloth.anchoredPosition = p + new Vector2(30, -30);
                if (!down) { BobSfx.Loop("scrub_loop", false); return; }
                if (!sprayed) { if (!warned) { warned = true; Toast("ฉีดน้ำยาก่อน"); } return; }
                bool hit = false;
                for (int i = 0; i < fog.Count; i++)
                    if (!fog[i].clear && Vector2.Distance(fog[i].at, p) < reach)
                    {
                        fog[i] = (fog[i].rt, fog[i].at, true); hit = true;
                        StartCoroutine(FadeResidue(fog[i].rt));
                        if (Random.value < 0.25f) MiniFxAt("fx_sparkle", fog[i].at, 62f, 0.35f, 20f);
                    }
                if (hit)
                {
                    BobSfx.Loop("scrub_loop", true);
                    if (Time.unscaledTime - scrubAt > 0.2f) { scrubAt = Time.unscaledTime; MiniHaptic(); }
                }
                int left = fog.Count(f => !f.clear);
                SetMiniProgress(1f - left / (float)fog.Count);
                if (left <= Mathf.FloorToInt(fog.Count * 0.05f)) CloseMini(true);
            };
        }
    }
}
