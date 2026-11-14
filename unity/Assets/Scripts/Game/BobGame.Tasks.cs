// Battle of the Brooms: Task Race, solo offline prototype (docs 16 and 19). The rental room's task points (L1Points in BobGame.Set.cs) open
// short minigames: T01 sweep along the path, T05 sort the rubbish, T08 clean the glass. One team of one, 120 s, 6 tasks; finishing them all wins.
// Not yet: long / common tasks (T04, T13), emergency stains, rival bar, server (TASK_BEGIN / TASK_LOCK), the other 13 minigames.
using System.Collections.Generic;
using System.Linq;
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
        readonly List<TaskDef> taskList = new List<TaskDef>();
        RectTransform taskUi, taskRows, taskBarFill; TMP_Text taskTimeText, taskCountText, taskBtnLabel; Button taskBtn;
        TaskDef taskNear, taskOpen;
        RectTransform miniCard, miniArea;
        System.Action<float> miniTick; // runs while a minigame card is open
        readonly List<RectTransform> miniParts = new List<RectTransform>();

        static string TaskName(string kind) => kind == "T01" ? "กวาดตามทาง" : kind == "T05" ? "แยกขยะ" : "เช็ดกระจก";
        static Vector2 TaskPos(string id) { foreach (var p in L1Points) if (p.id == id) return new Vector2(p.x, RoomH - p.y); return new Vector2(RoomW / 2, RoomH / 2); }

        // ---------------------------------------------------------------- start / end

        void StartTaskRace()
        {
            matchId = "t" + System.DateTime.UtcNow.Ticks.ToString("x"); seed = Random.Range(1, 8388608);
            Time.timeScale = 1; Sfx("ui_confirm");
            R = BobMatch.MakeRound(1, seed); stack.Clear(); hand.Clear(); tasks.Begin(R.Need, 9999);
            ResetRoomForRound(); foreach (var en in enemies) if (en.t != null) en.t.gameObject.SetActive(false);
            taskList.Clear();
            foreach (var (k, p) in new[] { ("T01", "S6"), ("T05", "S9"), ("T08", "S1"), ("T01", "S7"), ("T05", "S9"), ("T08", "S1") })
                taskList.Add(new TaskDef { Kind = k, Point = p, Name = TaskName(k) });
            taskTime = TaskRoundTime; taskStarted = Time.time; taskEnded = false; taskOpen = null; taskNear = null; taskMode = true;
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
            int done = taskList.Count(t => t.Done); float used = TaskRoundTime - taskTime;
            var card = BobUi.Panel(taskUi, BobUi.Surface); BobUi.Pin(card.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(900, 520));
            var t1 = BobUi.Label(card.transform, win ? "ห้องสะอาดแล้ว!" : "หมดเวลา", 64, win ? BobUi.Good : BobUi.Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
            BobUi.Pin(t1.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -50), new Vector2(800, 90));
            var t2 = BobUi.Label(card.transform, $"งานเสร็จ {done}/{taskList.Count}  ·  ใช้เวลา {Mathf.RoundToInt(used)} วิ", 38, BobUi.Ink, TextAnchor.MiddleCenter);
            BobUi.Pin(t2.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 30), new Vector2(800, 70));
            var b = BobUi.Button(card.transform, "กลับหน้าหลัก", BobUi.Sun, ExitTaskRace, 40);
            BobUi.Pin((RectTransform)b.transform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 50), new Vector2(480, 120));
            xp += win ? 40 : 10; SaveProfile();
        }

        // ---------------------------------------------------------------- HUD

        void BuildTaskUi()
        {
            if (taskUi != null) Destroy(taskUi.gameObject);
            taskUi = BobUi.Stretch(BobUi.Rect("TaskUi", hudRoot));
            var time = BobUi.Panel(taskUi, BobUi.Surface); BobUi.Pin(time.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -14), new Vector2(520, 100));
            taskTimeText = BobUi.Label(time.transform, "120", 56, BobUi.Ink, TextAnchor.MiddleCenter, FontStyle.Bold); BobUi.Stretch(taskTimeText.rectTransform);
            var bar = BobUi.Panel(taskUi, new Color(1, 1, 1, 0.85f)); BobUi.Pin(bar.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -126), new Vector2(520, 34));
            var fill = BobUi.Panel(bar.transform, BobUi.Good); taskBarFill = fill.rectTransform; taskBarFill.anchorMin = Vector2.zero; taskBarFill.anchorMax = new Vector2(0, 1); taskBarFill.offsetMin = taskBarFill.offsetMax = Vector2.zero;
            taskCountText = BobUi.Label(bar.transform, "0/6", 26, BobUi.Ink, TextAnchor.MiddleCenter, FontStyle.Bold); BobUi.Stretch(taskCountText.rectTransform);
            var list = BobUi.Panel(taskUi, BobUi.Surface); BobUi.Pin(list.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(14, -14), new Vector2(430, 40 + 66 * taskList.Count));
            var head = BobUi.Label(list.transform, "ใบงานห้องเช่า", 30, BobUi.Ink, TextAnchor.MiddleLeft, FontStyle.Bold); BobUi.Pin(head.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(20, -8), new Vector2(380, 44));
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
            if (miniCard == null) taskTime -= dt;
            taskTimeText.text = Mathf.CeilToInt(Mathf.Max(0, taskTime)).ToString();
            taskTimeText.color = taskTime < 15f ? BobUi.Rose : BobUi.Ink;
            if (taskTime <= 0f) { EndTaskRace(false); return; }
            if (miniCard != null) { miniTick?.Invoke(dt); taskBtn.gameObject.SetActive(false); return; }
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
            var title = BobUi.Label(miniCard, taskOpen.Name, 46, BobUi.Ink, TextAnchor.MiddleCenter, FontStyle.Bold); BobUi.Pin(title.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -16), new Vector2(900, 70));
            var x = BobUi.Button(miniCard, "×", BobUi.Surface, () => CloseMini(false), 54); BobUi.Pin((RectTransform)x.transform, Vector2.one, Vector2.one, new Vector2(-14, -14), new Vector2(144, 144));
            miniArea = BobUi.Rect("Area", miniCard); BobUi.Pin(miniArea, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -30), new Vector2(1300, 640));
            miniParts.Clear(); miniDrags.Clear();
            if (taskOpen.Kind == "T01") StartSweep(); else if (taskOpen.Kind == "T05") StartSort(); else StartGlass();
            joyActive = false; RefreshTaskRows();
        }

        void CloseMini(bool done)
        {
            if (miniCard != null) Destroy(miniCard.gameObject);
            miniCard = null; miniArea = null; miniTick = null; miniParts.Clear();
            if (done && taskOpen != null)
            {
                taskOpen.Done = true; Sfx("ui_confirm"); Toast(taskOpen.Name + " เสร็จแล้ว"); taskNear = null;
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

        RectTransform MiniDisc(string name, Color c, float size, Vector2 at)
        {
            var d = BobUi.Disc(miniArea, c, size, name); d.raycastTarget = false;
            d.rectTransform.anchorMin = d.rectTransform.anchorMax = new Vector2(0.5f, 0.5f); d.rectTransform.anchoredPosition = at; miniParts.Add(d.rectTransform); return d.rectTransform;
        }

        bool IsHero(string id) => charId == id;

        // T01: sweep the dust that lies along a winding path; dragging the finger over it clears it (>= 90%)
        void StartSweep()
        {
            var dust = new List<RectTransform>(); float reach = IsHero("sweep") ? 90f : 72f; // hero bonus: wider path (+25%)
            for (int i = 0; i < 34; i++)
            {
                float t = i / 33f; var at = new Vector2(Mathf.Lerp(-540, 540, t), Mathf.Sin(t * Mathf.PI * 2.4f) * 190f);
                dust.Add(MiniDisc("Dust", new Color(0.62f, 0.5f, 0.36f, 0.95f), 58, at));
            }
            var broom = MiniDisc("Broom", new Color(0.95f, 0.65f, 0.25f, 0.9f), 70, Vector2.zero);
            var hint = BobUi.Label(miniArea, "ลากนิ้วตามฝุ่นให้หมด", 32, BobUi.Muted, TextAnchor.MiddleCenter); BobUi.Pin(hint.rectTransform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 8), new Vector2(900, 50));
            miniTick = dt =>
            {
                if (!MiniPointer(out var p, out var down, out _, out _)) return;
                broom.anchoredPosition = p;
                if (!down) return;
                foreach (var d in dust) if (d.gameObject.activeSelf && Vector2.Distance(d.anchoredPosition, p) < reach) { d.gameObject.SetActive(false); Sfx("pickup"); }
                int left = dust.Count(d => d.gameObject.activeSelf);
                if (left <= Mathf.FloorToInt(dust.Count * 0.1f)) CloseMini(true);
            };
        }

        // T05: drag every piece of rubbish into the right bin (shape word as well as colour: paper / plastic / food)
        void StartSort()
        {
            var cats = new[] { ("กระดาษ", new Color(0.35f, 0.65f, 0.95f)), ("พลาสติก", new Color(0.98f, 0.8f, 0.25f)), ("เศษอาหาร", new Color(0.4f, 0.75f, 0.45f)) };
            var bins = new List<(RectTransform rt, int cat)>();
            for (int i = 0; i < 3; i++)
            {
                var b = BobUi.Panel(miniArea, cats[i].Item2); BobUi.Pin(b.rectTransform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2((i - 1) * 420, 30), new Vector2(340, 190)); b.raycastTarget = false;
                var l = BobUi.Label(b.transform, cats[i].Item1, 40, BobUi.Ink, TextAnchor.MiddleCenter, FontStyle.Bold); BobUi.Stretch(l.rectTransform); l.raycastTarget = false;
                bins.Add((b.rectTransform, i));
            }
            var items = new (string name, int cat)[] { ("กล่องนม", 0), ("ขวดน้ำ", 1), ("เปลือกกล้วย", 2), ("หนังสือพิมพ์", 0), ("ถุงพลาสติก", 1), ("ก้างปลา", 2) };
            var live = new List<(RectTransform rt, int cat, Vector2 home)>();
            for (int i = 0; i < items.Length; i++)
            {
                var home = new Vector2(-520 + i * 208, 190f);
                var it = BobUi.Panel(miniArea, new Color(1, 1, 1, 1)); BobUi.Pin(it.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), home, new Vector2(190, 110)); it.raycastTarget = false;
                var l = BobUi.Label(it.transform, items[i].name, 28, BobUi.Ink, TextAnchor.MiddleCenter, FontStyle.Bold); BobUi.Stretch(l.rectTransform); l.raycastTarget = false;
                live.Add((it.rectTransform, items[i].cat, home)); miniDrags.Add((home, new Vector2((items[i].cat - 1) * 420f, -200f)));
            }
            RectTransform held = null; int heldCat = -1; Vector2 heldHome = Vector2.zero; 
            miniTick = dt =>
            {
                if (!MiniPointer(out var p, out var down, out var pressed, out var released)) return;
                if (pressed) foreach (var l in live) if (Mathf.Abs(l.rt.anchoredPosition.x - p.x) < 95 && Mathf.Abs(l.rt.anchoredPosition.y - p.y) < 55) { held = l.rt; heldCat = l.cat; heldHome = l.home; break; }
                if (held != null && down) held.anchoredPosition = p;
                if (held != null && released)
                {
                    bool ok = false, wrong = false;
                    // bins sit on the bottom edge of the area: centre y = -320 + 30 + 95 = -195, x = (i - 1) * 420
                    for (int i2 = 0; i2 < 3; i2++) if (Mathf.Abs((i2 - 1) * 420f - p.x) < 170f && p.y < -100f) { if (i2 == heldCat) ok = true; else wrong = true; }
                    if (ok) { live.RemoveAll(l => l.rt == held); Destroy(held.gameObject); Sfx("place_layer"); }
                    else { held.anchoredPosition = heldHome; if (wrong) Sfx("drop_invalid"); }
                    held = null;
                    if (live.Count == 0) CloseMini(true);
                }
            };
        }

        // T08: tap the spray bottle once, then scrub the fogged glass until at most 5% is left
        void StartGlass()
        {
            const int cols = 14, rows = 7; const float cell = 70f;
            var fog = new List<(Image img, Vector2 at, bool clear)>();
            var pane = BobUi.Panel(miniArea, new Color(0.7f, 0.87f, 0.97f)); BobUi.Pin(pane.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(60, 20), new Vector2(cols * cell + 20, rows * cell + 20)); pane.raycastTarget = false;
            for (int r = 0; r < rows; r++) for (int c = 0; c < cols; c++)
                {
                    var im = BobUi.Panel(pane.transform, new Color(1, 1, 1, 0.92f)); BobUi.Pin(im.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2((c - (cols - 1) / 2f) * cell, (r - (rows - 1) / 2f) * cell), new Vector2(cell + 2, cell + 2)); im.raycastTarget = false;
                    fog.Add((im, new Vector2(pane.rectTransform.anchoredPosition.x + (c - (cols - 1) / 2f) * cell, pane.rectTransform.anchoredPosition.y + (r - (rows - 1) / 2f) * cell), false));
                }
            bool sprayed = false; float reach = IsHero("bubble") ? 105f : 84f; // hero bonus: wider wipe (+25%)
            var spray = BobUi.Button(miniArea, "ฉีดน้ำยา", BobUi.Sun, () => { sprayed = true; Sfx("pickup"); foreach (var f in fog) f.img.color = new Color(0.9f, 0.97f, 1f, 0.92f); }, 34);
            BobUi.Pin((RectTransform)spray.transform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(10, 20), new Vector2(220, 160));
            var hint = BobUi.Label(miniArea, "ฉีดน้ำยาก่อน แล้วลากเช็ดให้ใส", 32, BobUi.Muted, TextAnchor.MiddleCenter); BobUi.Pin(hint.rectTransform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(60, 8), new Vector2(900, 50));
            bool warned = false;
            miniTick = dt =>
            {
                if (!MiniPointer(out var p, out var down, out _, out _) || !down) return;
                if (!sprayed) { if (!warned && Mathf.Abs(p.x) < 700 && p.x > -300) { warned = true; Toast("ฉีดน้ำยาก่อน"); } return; }
                for (int i = 0; i < fog.Count; i++)
                    if (!fog[i].clear && Vector2.Distance(fog[i].at, p) < reach) { fog[i] = (fog[i].img, fog[i].at, true); fog[i].img.color = new Color(1, 1, 1, 0f); }
                int left = fog.Count(f => !f.clear);
                if (left <= Mathf.FloorToInt(fog.Count * 0.05f)) CloseMini(true);
            };
        }
    }
}
