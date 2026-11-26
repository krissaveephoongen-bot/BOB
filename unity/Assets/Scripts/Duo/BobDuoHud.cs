// Battle of the Brooms: the M1 duo slice HUD (docs 32 section 6, wireframe frames B1/B2). Landscape 1920x1080, built
// from code like every other screen in the project. Lobby panel first, match view behind it, result panel on top.
using System;
using System.Collections.Generic;
using Bob.Game;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Bob.Duo
{
    public sealed class BobDuoHud : MonoBehaviour
    {
        sealed class Slot
        {
            public GameObject panel;
            public TMP_Text title, role, hint, partner, state;
            public Image meter, partnerBar;
            public RectTransform pad;
            public GameObject rhythmRing;
            public BobDuoHoldArea hold;
            public BobDuoRhythmPad rhythm;
            public BobDuoDragPad drag;
        }

        Canvas canvas;
        BobDuoMatch match;
        GameObject lobby, matchRoot, resultRoot;
        TMP_Text clock, phase, progress, code, status, resultTitle, resultLine;
        Image[] pips = new Image[BobDuoRules.TaskCount];
        readonly Slot mine = new Slot(), theirs = new Slot();
        Button startButton, hostButton, joinButton;
        string typedCode = "";
        bool busy;
        bool typingCode;
        int LocalSlot => match.LocalSlot;
        DuoGesture lastGesture;

        public void Build(Canvas target, BobDuoMatch owner)
        {
            canvas = target;
            match = owner;
            BuildLobby();
            BuildMatch();
            BuildResult();
            ShowLobby(true);
        }

        void BuildLobby()
        {
            lobby = Frame("Lobby"); // Frame already fills with BobUi.Bg; no second backdrop
            var card = BobUi.Panel(lobby.transform, BobUi.Surface);
            card.raycastTarget = false;
            BobUi.Pin((RectTransform)card.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1100, 720));
            var c = card.transform;
            var top = new Vector2(0.5f, 1f);

            var title = BobUi.Label(c, "ห้องคู่ · 2 คน", 64, BobUi.Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
            BobUi.Pin(title.rectTransform, top, top, new Vector2(0, -36), new Vector2(1000, 84));

            var subtitle = BobUi.Label(c, "หนึ่งห้อง 6 งาน 4 นาที · สองเครื่อง สองบทบาท", 26, BobUi.Muted, TextAnchor.MiddleCenter);
            BobUi.Pin(subtitle.rectTransform, top, top, new Vector2(0, -128), new Vector2(1000, 40));

            hostButton = BobUi.Button(c, "สร้างห้อง", BobUi.Sun, HostRoom, 32);
            BobUi.Pin((RectTransform)hostButton.transform, top, top, new Vector2(-190, -200), new Vector2(340, 104));
            joinButton = BobUi.Button(c, "ใส่รหัสห้อง", BobUi.Surface, JoinRoom, 32, BobUi.Ink);
            BobUi.Pin((RectTransform)joinButton.transform, top, top, new Vector2(190, -200), new Vector2(340, 104));

            code = BobUi.Label(c, "", 52, BobUi.Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
            BobUi.Pin(code.rectTransform, top, top, new Vector2(0, -324), new Vector2(900, 70));

            status = BobUi.Label(c, "", 24, BobUi.Muted, TextAnchor.MiddleCenter);
            BobUi.Pin(status.rectTransform, top, top, new Vector2(0, -400), new Vector2(980, 44));

            var back = BobUi.Button(lobby.transform, "กลับ", BobUi.Surface, HomePressed, 24, BobUi.Ink);
            BobUi.Pin((RectTransform)back.transform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(64, -48), new Vector2(150, 76));

            startButton = BobUi.Button(c, "เริ่มเมื่อครบ 2 คน", BobUi.Teal, StartPressed, 40, BobUi.Surface);
            BobUi.Pin((RectTransform)startButton.transform, top, top, new Vector2(0, -462), new Vector2(460, 104));
            startButton.interactable = false;

            var solo = BobUi.Button(c, "ทดลองคนเดียว (คู่เป็นบอท)", BobUi.Surface, SoloPressed, 26, BobUi.Ink);
            BobUi.Pin((RectTransform)solo.transform, top, top, new Vector2(0, -586), new Vector2(460, 88));

            var foot = BobUi.Label(c, "แนวนอน 19.5:9 · ทีมเดียวกันเห็นความคืบหน้ากัน · ไม่มีป่วนในรอบนี้", 22, BobUi.Muted, TextAnchor.MiddleCenter);
            BobUi.Pin(foot.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0, 28), new Vector2(1000, 34));
        }

        void BuildMatch()
        {
            matchRoot = Frame("Match");
            var bar = BobUi.Panel(matchRoot.transform, BobUi.Surface);
            bar.raycastTarget = false;
            BobUi.Pin((RectTransform)bar.transform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(64, -48), new Vector2(1792, 96));
            clock = BobUi.Label(bar.transform, "4:00", 52, BobUi.Ink, TextAnchor.MiddleLeft, FontStyle.Bold);
            BobUi.Pin(clock.rectTransform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(28, 0), new Vector2(180, 76));
            phase = BobUi.Label(bar.transform, "ด้วยกัน", 30, BobUi.Teal, TextAnchor.MiddleLeft, FontStyle.Bold);
            BobUi.Pin(phase.rectTransform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(220, 0), new Vector2(320, 76));
            progress = BobUi.Label(bar.transform, "งาน 1/6 · 0%", 26, BobUi.Muted, TextAnchor.MiddleRight);
            BobUi.Pin(progress.rectTransform, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-28, 0), new Vector2(520, 76));

            var card = BobUi.Panel(matchRoot.transform, BobUi.Surface);
            card.raycastTarget = false;
            BobUi.Pin((RectTransform)card.transform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(64, -176), new Vector2(1080, 700));
            mine.panel = card.gameObject;
            mine.title = BobUi.Label(card.transform, "คราบติดพื้น", 56, BobUi.Ink, TextAnchor.MiddleLeft, FontStyle.Bold);
            BobUi.Pin(mine.title.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(40, -32), new Vector2(1000, 70));
            mine.role = BobUi.Label(card.transform, "ฉีดน้ำยาให้ทั่ว", 34, BobUi.Teal, TextAnchor.MiddleLeft, FontStyle.Bold);
            BobUi.Pin(mine.role.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(40, -110), new Vector2(1000, 50));
            mine.hint = BobUi.Label(card.transform, "", 24, BobUi.Muted);
            BobUi.Pin(mine.hint.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(40, -162), new Vector2(1000, 40));

            // The gesture area: 940x330 in the middle of the card.
            var pad = BobUi.Panel(card.transform, BobUi.Bg);
            BobUi.Pin((RectTransform)pad.transform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -220), new Vector2(940, 330));
            mine.pad = (RectTransform)pad.transform;

            // The ring pulses with the beat and is only shown on rhythm tasks; it must never eat the pad's press.
            var ring = BobUi.Disc(pad.transform, BobUi.Line, 180f, "BeatRing");
            ring.raycastTarget = false;
            BobUi.Pin((RectTransform)ring.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(180, 180));
            mine.rhythmRing = ring.gameObject;

            var padNote = BobUi.Label(pad.transform, "พื้นที่ทำภารกิจ · กดค้าง / แตะตามจังหวะ / ลาก", 24, BobUi.Muted, TextAnchor.MiddleCenter);
            padNote.raycastTarget = false;
            BobUi.Pin(padNote.rectTransform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 18), new Vector2(900, 40));

            // Each adapter is called by uGUI independently, so all three can live on the same pressable panel; the HUD
            // enables only the one the current role asks for.
            mine.hold = pad.gameObject.AddComponent<BobDuoHoldArea>();
            mine.rhythm = pad.gameObject.AddComponent<BobDuoRhythmPad>();
            mine.drag = pad.gameObject.AddComponent<BobDuoDragPad>();
            mine.rhythm.Tapped += good => { if (good) match.Tap(LocalSlot); };

            var track = BobUi.Panel(card.transform, BobUi.Line);
            track.raycastTarget = false;
            BobUi.Pin((RectTransform)track.transform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 42), new Vector2(940, 34));
            mine.meter = BobUi.Panel(track.transform, BobUi.Teal);
            mine.meter.raycastTarget = false;
            BobUi.Stretch((RectTransform)mine.meter.transform, 0, 0, 0, 0);
            mine.meter.type = Image.Type.Filled;
            mine.meter.fillMethod = Image.FillMethod.Horizontal;
            mine.meter.fillAmount = 0f;

            var side = BobUi.Panel(matchRoot.transform, BobUi.Surface);
            side.raycastTarget = false;
            BobUi.Pin((RectTransform)side.transform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-64, -176), new Vector2(630, 700));
            var s = side.transform;
            var st = new Vector2(0f, 1f);
            void At(TMP_Text label, float top, float height, int size, TextAnchor align)
            {
                label.fontSize = size;
                label.alignment = BobUi.ToTmp(align);
                BobUi.Pin(label.rectTransform, st, st, new Vector2(28, -top), new Vector2(560, height));
            }

            var mateLabel = BobUi.Label(s, "คู่หูของคุณ", 30, BobUi.Muted, TextAnchor.MiddleLeft, FontStyle.Bold);
            At(mateLabel, 24, 40, 30, TextAnchor.MiddleLeft);
            theirs.panel = side.gameObject;
            theirs.title = BobUi.Label(s, "เพื่อน", 44, BobUi.Ink, TextAnchor.MiddleLeft, FontStyle.Bold);
            At(theirs.title, 72, 60, 44, TextAnchor.MiddleLeft);
            theirs.role = BobUi.Label(s, "", 28, BobUi.Teal, TextAnchor.MiddleLeft);
            At(theirs.role, 140, 44, 28, TextAnchor.MiddleLeft);
            theirs.hint = BobUi.Label(s, "", 22, BobUi.Muted, TextAnchor.UpperLeft);
            At(theirs.hint, 190, 56, 22, TextAnchor.UpperLeft);
            theirs.state = BobUi.Label(s, "ยังไม่เริ่ม", 24, BobUi.Muted, TextAnchor.MiddleLeft);
            At(theirs.state, 252, 36, 24, TextAnchor.MiddleLeft);

            var pbar = BobUi.Panel(s, BobUi.Line);
            pbar.raycastTarget = false;
            BobUi.Pin((RectTransform)pbar.transform, st, st, new Vector2(28, -302), new Vector2(560, 30));
            theirs.partnerBar = BobUi.Panel(pbar.transform, BobUi.Good);
            theirs.partnerBar.raycastTarget = false;
            BobUi.Stretch((RectTransform)theirs.partnerBar.transform);
            theirs.partnerBar.type = Image.Type.Filled;
            theirs.partnerBar.fillMethod = Image.FillMethod.Horizontal;
            theirs.partnerBar.fillAmount = 0f;

            var pipsLabel = BobUi.Label(s, "งานทั้ง 6", 26, BobUi.Muted, TextAnchor.MiddleLeft, FontStyle.Bold);
            At(pipsLabel, 360, 36, 26, TextAnchor.MiddleLeft);
            for (int i = 0; i < pips.Length; i++)
            {
                var pip = BobUi.Panel(s, BobUi.Line);
                pip.raycastTarget = false;
                BobUi.Pin((RectTransform)pip.transform, st, st, new Vector2(28 + i * 88, -408), new Vector2(64, 40));
                pips[i] = pip;
            }

            var sideFoot = BobUi.Label(s, "ทำครบ 6 งานก่อนชนะ\nหมดเวลาเทียบ % ความสะอาด", 22, BobUi.Muted, TextAnchor.UpperLeft);
            BobUi.Pin(sideFoot.rectTransform, st, st, new Vector2(28, -500), new Vector2(560, 110));
        }

        void BuildResult()
        {
            resultRoot = Frame("Result");
            var card = BobUi.Panel(resultRoot.transform, BobUi.Surface);
            card.raycastTarget = false;
            BobUi.Pin((RectTransform)card.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1000, 620));
            var c = card.transform;
            var top = new Vector2(0.5f, 1f);

            resultTitle = BobUi.Label(c, "จบรอบ", 64, BobUi.Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
            BobUi.Pin(resultTitle.rectTransform, top, top, new Vector2(0, -56), new Vector2(880, 90));
            resultLine = BobUi.Label(c, "", 30, BobUi.Muted, TextAnchor.MiddleCenter);
            BobUi.Pin(resultLine.rectTransform, top, top, new Vector2(0, -178), new Vector2(880, 110));

            var again = BobUi.Button(c, "เล่นอีกครั้ง", BobUi.Sun, AgainPressed, 40);
            BobUi.Pin((RectTransform)again.transform, top, top, new Vector2(0, -330), new Vector2(460, 104));
            var home = BobUi.Button(c, "กลับหน้าหลัก", BobUi.Surface, HomePressed, 28, BobUi.Ink);
            BobUi.Pin((RectTransform)home.transform, top, top, new Vector2(0, -462), new Vector2(460, 88));
        }

        GameObject Frame(string name)
        {
            var panel = BobUi.Rect(name, canvas.transform);
            BobUi.Stretch(panel);
            var backdrop = panel.gameObject.AddComponent<Image>();
            backdrop.color = BobUi.Bg;
            backdrop.raycastTarget = false;
            return panel.gameObject;
        }

        void Update()
        {
            ListenToRoom();

            var room = Bob.Ugs.BobOnlineRoom.Instance;
            bool inRoom = room != null && room.InRoom;

            if (keyboard != null)
            {
                if (keyboard.active)
                {
                    string typed = CleanCode(keyboard.text);
                    if (typed != typedCode) typedCode = typed;
                }
                else
                {
                    keyboard = null;
                    SubmitJoin();
                }
            }
            else if (typingCode && !busy)
            {
                // hardware keyboard fallback (the editor, a desktop build); the phone uses the touch keyboard above
                string typed = Input.inputString;
                if (!string.IsNullOrEmpty(typed))
                {
                    if (typed.IndexOf('\b') >= 0) typedCode = typedCode.Length > 0 ? typedCode.Substring(0, typedCode.Length - 1) : "";
                    typedCode = CleanCode(typedCode + typed.Replace("\b", ""));
                    if (typedCode.Length < 6) status.text = "พิมพ์รหัส " + typedCode.Length + "/6";
                    if (typedCode.Length == 6) SubmitJoin();
                }
            }

            if (roomTask != null && roomTask.IsCompleted)
            {
                bool ok = roomTask.Status == System.Threading.Tasks.TaskStatus.RanToCompletion && roomTask.Result;
                roomTask = null;
                busy = false;
                status.text = ok ? "อยู่ในห้องแล้ว" : "เข้าห้องไม่สำเร็จ · ลองใหม่";
            }

            if (inRoom)
            {
                code.text = room.Code;
                status.text = room.Players + "/2 คน · " + (room.IsHost ? "คุณเป็นเจ้าของห้อง" : "รอเจ้าของห้องเริ่ม");
                hostButton.interactable = false;
                joinButton.interactable = false;
                startButton.interactable = room.IsHost && room.Players >= 2;
            }
            else
            {
                if (!busy) status.text = status.text == "" ? "สร้างห้องหรือใส่รหัสจากเพื่อน" : status.text;
                hostButton.interactable = !busy;
                joinButton.interactable = !busy;
                startButton.interactable = false;
            }
        }

        static string CleanCode(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return "";
            var sb = new System.Text.StringBuilder(6);
            foreach (char c in raw.ToUpperInvariant())
                if (sb.Length < 6 && ((c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9'))) sb.Append(c);
            return sb.ToString();
        }

        Bob.Ugs.BobOnlineRoom listened;

        void ListenToRoom()
        {
            var room = Bob.Ugs.BobOnlineRoom.Instance;
            if (room == listened) return;
            if (listened != null) listened.DuoStarted -= OnDuoStarted;
            listened = room;
            if (listened != null) listened.DuoStarted += OnDuoStarted;
        }

        void OnDuoStarted()
        {
            ShowHud();
            match.BeginOnline();
        }

        void OnDestroy()
        {
            if (listened != null) listened.DuoStarted -= OnDuoStarted;
        }

        // The touch keyboard is not always reachable (the editor, a desktop build), so both paths land here.
        void SubmitJoin()
        {
            if (busy || typedCode.Length != 6) return;
            typingCode = false;
            busy = true;
            status.text = "กำลังเข้าห้อง " + typedCode + "…";
            roomTask = Bob.Ugs.BobOnlineRoom.Ensure().JoinAsync(HeroId(), typedCode);
        }

        public void ShowLobby(bool show)
        {
            lobby.SetActive(show);
            matchRoot.SetActive(!show);
            resultRoot.SetActive(false);
        }

        public void ShowHud()
        {
            lobby.SetActive(false);
            matchRoot.SetActive(true);
            resultRoot.SetActive(false);
        }

        public void ShowResult(bool won, float percent, string detail)
        {
            lobby.SetActive(false);
            matchRoot.SetActive(false);
            resultRoot.SetActive(true);
            resultTitle.text = won ? "สะอาดครบ!" : "หมดเวลา";
            resultTitle.color = won ? BobUi.Good : BobUi.Ink;
            resultLine.text = detail + " · ความสะอาด " + Mathf.RoundToInt(percent) + "%";
        }

        public void RefreshHeader()
        {
            if (clock != null) clock.text = BobDuoRules.Clock(0f);
        }

        /// <summary>The check presses the pad, not the match: RefreshMatch stays the one place that forwards a hold.</summary>
        public void HoldForCheck(bool down) => mine.hold.SetHeld(down);

        public void RefreshMatch(BobDuoMatch live)
        {
            if (live == null) return;
            int index = Mathf.Clamp(live.TaskIndex, 0, BobDuoRules.TaskCount - 1);
            int slot = live.LocalSlot;
            var def = BobDuoRules.Task(index);
            var task = live.Tasks[index];

            clock.text = BobDuoRules.Clock(live.Elapsed);
            phase.text = BobDuoRules.PhaseThai(live.Phase);
            progress.text = "งาน " + (index + 1) + "/6 · " + Mathf.RoundToInt(live.Percent) + "%";

            mine.title.text = def.Name;
            mine.role.text = BobDuoRules.RoleFor(index, slot);
            mine.hint.text = BobDuoRules.HintFor(index, slot);
            mine.meter.fillAmount = task.Charge(slot);

            theirs.role.text = BobDuoRules.RoleFor(index, 1 - slot);
            theirs.hint.text = BobDuoRules.HintFor(index, 1 - slot);
            theirs.state.text = task.Charge(1 - slot) >= 1f ? "ฝั่งนี้พร้อม" : "กำลังทำอยู่";
            theirs.partnerBar.fillAmount = task.Charge(1 - slot);

            for (int i = 0; i < pips.Length; i++)
                pips[i].color = live.Tasks[i].Done ? BobUi.Good : i == index ? BobUi.Sun : BobUi.Line;

            var gesture = BobDuoRules.GestureFor(index, slot);
            if (gesture != lastGesture) { mine.hold.Release(); lastGesture = gesture; }
            mine.hold.enabled = gesture == DuoGesture.Hold;
            mine.drag.enabled = gesture == DuoGesture.Drag;
            mine.rhythm.enabled = gesture == DuoGesture.Rhythm;
            if (mine.rhythmRing != null)
            {
                bool onBeatPad = gesture == DuoGesture.Rhythm;
                mine.rhythmRing.SetActive(onBeatPad);
                if (onBeatPad)
                {
                    float beat = BobDuoRhythmPad.BeatProgress;            // 0 at the beat, 1 just before the next
                    mine.rhythmRing.transform.localScale = Vector3.one * (1f + 0.1f * (1f - beat));
                }
            }

            if (gesture == DuoGesture.Hold) match.Hold(slot, mine.hold.Held);
            else if (gesture == DuoGesture.Drag)
            {
                float units = mine.drag.Consume();
                if (units > 0f) match.Drag(slot, units);
            }
        }

        // The lobby fills a real UGS room; the host's start is heard through the room's DuoStarted event (ListenToRoom).
        System.Threading.Tasks.Task<bool> roomTask;
        TouchScreenKeyboard keyboard;

        void HostRoom()
        {
            if (busy) return;
            busy = true;
            status.text = "กำลังสร้างห้อง…";
            var room = Bob.Ugs.BobOnlineRoom.Ensure();
            roomTask = room.HostAsync(HeroId(), 2);
        }

        void JoinRoom()
        {
            if (busy) return;
            typingCode = true;
            try { keyboard = TouchScreenKeyboard.Open(typedCode, TouchScreenKeyboardType.Default, false, false, true, false, "รหัสห้อง 6 ตัว"); }
            catch (System.Exception) { keyboard = null; }
            typedCode = "";
            status.text = "พิมพ์รหัส 6 ตัวจากเจ้าของห้อง";
        }

        void StartPressed()
        {
            var room = Bob.Ugs.BobOnlineRoom.Instance;
            if (room == null || !room.InRoom || !room.IsHost || room.Players < 2) return;
            if (!room.StartDuo(2)) { status.text = "เริ่มไม่ได้ · ต้องมี 2 คนพอดี"; return; }
            match.BeginOnline();
        }

        void SoloPressed()
        {
            match.BeginSoloSmoke();
            ShowHud();
        }

        void AgainPressed()
        {
            var room = Bob.Ugs.BobOnlineRoom.Instance;
            if (room != null && room.InRoom)
            {
                if (!room.IsHost) { resultLine.text = "รอเจ้าของห้องเริ่มรอบใหม่"; return; }
                room.StartDuo(2);
                match.BeginOnline();
                return;
            }
            match.BeginSoloSmoke();
            ShowHud();
        }

        void HomePressed()
        {
            var room = Bob.Ugs.BobOnlineRoom.Instance;
            if (room != null && room.InRoom) _ = room.LeaveAsync();
            Bob.Game.BobSceneLoader.Load("Game", "กลับหน้าหลัก");
        }

        static string HeroId()
        {
            var pick = Bob.Game.MatchRequest.Has && Bob.Game.MatchRequest.Helpers.Length > 0
                ? Bob.Game.MatchRequest.Helpers[0]
                : "sweep";
            return string.IsNullOrEmpty(pick) ? "sweep" : pick;
        }
    }
}
