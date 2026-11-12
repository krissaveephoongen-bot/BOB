// Battle of the Brooms co-op: the uGUI screens, built from code like the single-player HUD (Game/BobGame.Hud.cs).
// Sizes follow art/docs/11_UX_UI_Spec.md: canvas 1920x1080 match height, buttons >= 144u (160u in a match),
// main action bottom-right, safe area + 24u, send = hold 0.6 s (BobHoldButton), tokens from BobUiTheme.
using System;
using System.Linq;
using Bob.Game;
using Bob.Net;
using Bob.Rules;
using Bob.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Bob.Coop
{
    public partial class BobCoopGame
    {
        RectTransform hudRoot, safe, page, ctxPanel, handRow, toastRt;
        Text timerText, toastText;
        float toastUntil;
        string ctxKey;
        BobUiTheme T => BobUiTheme.Current;

        void BuildHud()
        {
            var cgo = new GameObject("CoopHUD", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = cgo.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 10;
            var scaler = cgo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight; scaler.matchWidthOrHeight = 1f;
            hudRoot = (RectTransform)cgo.transform;
            safe = BobSafeArea.Create(hudRoot, T.safeInset);

            toastRt = BobUi.Panel(hudRoot, T.ink, null, "Toast").rectTransform;
            BobUi.Pin(toastRt, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -150), new Vector2(1100, 110));
            toastText = BobUi.Label(toastRt, "", T.body, Color.white, TextAnchor.MiddleCenter);
            BobUi.Stretch(toastText.rectTransform, 20, 6, 20, 6);
            toastRt.gameObject.SetActive(false);
            RenderHud();
        }

        void Toast(string msg) { if (toastRt == null) return; toastText.text = msg; toastRt.gameObject.SetActive(true); toastRt.SetAsLastSibling(); toastUntil = Time.time + 2.2f; }

        void LateUpdate()
        {
            if (toastRt != null && toastRt.gameObject.activeSelf && Time.time > toastUntil) toastRt.gameObject.SetActive(false);
            if (screen == View.Build) { var key = ContextKey(); if (key != ctxKey) RenderContext(); }
        }

        // ---------------------------------------------------------------- screens

        void RenderHud()
        {
            if (safe == null) return;
            if (page != null) Destroy(page.gameObject);
            page = BobUi.Stretch(BobUi.Rect("Page", safe));
            timerText = null; ctxPanel = null; handRow = null; ctxKey = null;
            switch (screen)
            {
                case View.Connecting: case View.Searching: Center(status, "กลับ", screen == View.Searching ? (Action)LeaveMatch : null); break;
                case View.Offline: case View.Menu: BuildMenu(); break;
                case View.Lobby: BuildLobby(); break;
                case View.Build: BuildMatchHud(); break;
                case View.Test: BuildTestOverlay(); break;
                case View.Result: BuildResult(); break;
            }
            BobHitArea.EnsureAll(page, screen == View.Build ? T.hitInMatch : T.hitMin);
        }

        void Center(string text, string button, Action onButton)
        {
            var box = BobUi.VBox(page, T.spaceM, 40, "Center");
            BobUi.Pin((RectTransform)box.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1100, 400));
            box.childAlignment = TextAnchor.MiddleCenter;
            BobUi.Size(BobUi.Label(box.transform, text, T.h2, T.ink, TextAnchor.MiddleCenter), 160, 160);
            if (onButton != null) BobUi.Size(BobUi.Button(box.transform, button, BobUi.Surface, () => onButton(), T.button), 144, 144);
        }

        Button Btn(Transform parent, string text, Color bg, Action onClick, float h = 160, float w = -1, int size = 0)
        {
            var b = BobUi.Button(parent, text, bg, () => onClick(), size > 0 ? size : T.button);
            BobUi.Size(b, h, h, w < 0 ? 1 : 0, w < 0 ? -1 : w);
            return b;
        }

        // S11/S14 (simplified): quick match, create a room, join by code, server address
        void BuildMenu()
        {
            var col = BobUi.VBox(page, T.spaceS, 0, "Menu");
            BobUi.Pin((RectTransform)col.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1200, 960));
            BobUi.Size(BobUi.Label(col.transform, "เล่นออนไลน์กับเพื่อน", T.h1, T.ink, TextAnchor.MiddleCenter, FontStyle.Bold), 110, 110);
            BobUi.Size(BobUi.Label(col.transform, status, T.caption, T.inkMuted, TextAnchor.MiddleCenter), 60, 60);
            if (screen == View.Menu)
            {
                Btn(col.transform, "จับคู่อัตโนมัติ (2-4 คน)", BobUi.Sun, QuickMatch, 170, -1, T.buttonLg);
                Btn(col.transform, "สร้างห้องส่วนตัว", BobUi.Surface, CreateRoom);
                var row = BobUi.HBox(col.transform, T.spaceS, "Join"); BobUi.Size(row, 160, 160);
                var code = Field(row.transform, "รหัสห้อง", "");
                Btn(row.transform, "เข้าห้อง", BobUi.Surface, () => JoinRoom(code.text), 160, 300);
            }
            var hostRow = BobUi.HBox(col.transform, T.spaceS, "Host"); BobUi.Size(hostRow, 150, 150);
            var hostField = Field(hostRow.transform, "IP เซิร์ฟเวอร์", host);
            Btn(hostRow.transform, screen == View.Offline ? "ลองใหม่" : "เชื่อมต่อ", BobUi.Surface, () => SetHost(hostField.text), 150, 300);
            Btn(col.transform, "เล่นคนเดียว", BobUi.Surface, () => { if (Application.CanStreamedLevelBeLoaded("Game")) SceneManager.LoadScene("Game"); else Toast("ไม่มีฉาก Game ใน Build Settings"); }, 144);
        }

        InputField Field(Transform parent, string placeholder, string value)
        {
            var bg = BobUi.Panel(parent, BobUi.Surface, "ui_panel", "Field");
            BobUi.Size(bg, 150, 150, 1);
            var text = BobUi.Label(bg.transform, value, T.body, T.ink); BobUi.Stretch(text.rectTransform, 24, 8, 24, 8);
            text.supportRichText = false;
            var ph = BobUi.Label(bg.transform, placeholder, T.body, T.inkMuted); BobUi.Stretch(ph.rectTransform, 24, 8, 24, 8);
            var input = bg.gameObject.AddComponent<InputField>();
            input.textComponent = text; input.placeholder = ph; input.text = value; input.targetGraphic = bg;
            return input;
        }

        // S14 room / lobby: two team columns, hero picker, switch team, ready
        void BuildLobby()
        {
            var s = net?.State;
            var top = BobUi.HBox(page, T.spaceS, "Top");
            BobUi.Pin((RectTransform)top.transform, new Vector2(0, 1), new Vector2(0, 1), Vector2.zero, new Vector2(1800, 150));
            Btn(top.transform, "← ออก", BobUi.Surface, LeaveMatch, 150, 220);
            var code = net?.MatchId ?? "";
            BobUi.Size(BobUi.Label(top.transform, "รหัสห้อง: " + code, T.caption, T.ink), 150, 150, 1);
            Btn(top.transform, "คัดลอกรหัส", BobUi.Surface, () => { GUIUtility.systemCopyBuffer = code; Toast("คัดลอกรหัสห้องแล้ว ส่งให้เพื่อนได้เลย"); }, 150, 320);

            var teams = BobUi.HBox(page, T.spaceL, "Teams");
            var tr = (RectTransform)teams.transform; tr.anchorMin = new Vector2(0, 0.3f); tr.anchorMax = new Vector2(1, 0.83f); tr.offsetMin = tr.offsetMax = Vector2.zero;
            for (int t = 0; t < 2; t++)
            {
                var box = BobUi.Box(teams.transform, t == 0 ? "ทีม 1" : "ทีม 2", t == (net?.MyTeam ?? -1) ? "ทีมของคุณ" : null);
                if (s?.players != null)
                    foreach (var p in s.players.Where(p => p.team == t))
                    {
                        var row = BobUi.HBox(box.transform, T.spaceS, "Player"); BobUi.Size(row, 110, 110); row.childForceExpandWidth = false;
                        var face = BobUi.Img(row.transform, p.@char + "_portrait", 100, T.Hero(p.@char));
                        BobUi.Size(face, 100, 100, 0, 100);
                        var name = (p.id == net.Me ? "คุณ" : p.name) + " · " + BobMatch.Char(p.@char).Name;
                        BobUi.Size(BobUi.Label(row.transform, name, T.body, T.ink), 100, 100, 1);
                        // ready is shown with a word, not colour alone (UX spec 9)
                        BobUi.Size(BobUi.Label(row.transform, p.ready ? "✓ พร้อม" : "รอ…", T.body, p.ready ? T.success : T.inkMuted, TextAnchor.MiddleRight, FontStyle.Bold), 100, 100, 0, 200);
                    }
            }

            var heroes = BobUi.HBox(page, T.spaceS, "Heroes");
            var hr = (RectTransform)heroes.transform; hr.anchorMin = new Vector2(0, 0.12f); hr.anchorMax = new Vector2(0.62f, 0.28f); hr.offsetMin = hr.offsetMax = Vector2.zero;
            foreach (var c in BobMatch.Chars)
            {
                var id = c.Id;
                var b = Btn(heroes.transform, c.Name, id == charId ? BobUi.Sun : BobUi.Surface, () => { PickChar(id); RenderHud(); });
                b.interactable = !MeReady;
            }

            var bottom = BobUi.HBox(page, T.spaceS, "Bottom");
            BobUi.Pin((RectTransform)bottom.transform, new Vector2(1, 0), new Vector2(1, 0), Vector2.zero, new Vector2(1000, 180));
            Btn(bottom.transform, "ย้ายทีม", BobUi.Surface, SwitchTeam, 180, 300);
            Btn(bottom.transform, MeReady ? "ยกเลิกพร้อม" : "พร้อม ✓", MeReady ? BobUi.Surface : BobUi.Sun, ToggleReady, 180, 520, T.buttonLg);
            int n = s?.players?.Length ?? 0, ready = s?.players?.Count(p => p.ready) ?? 0;
            var hint = n < 2 ? "รอเพื่อนอีกอย่างน้อย 1 คน (ส่งรหัสห้องให้เพื่อน)" : ready < n ? $"พร้อมแล้ว {ready}/{n} คน เริ่มเมื่อทุกคนพร้อม" : "กำลังเริ่ม…";
            var h = BobUi.Label(page, hint, T.caption, T.inkMuted, TextAnchor.MiddleRight);
            BobUi.Pin(h.rectTransform, new Vector2(1, 0), new Vector2(1, 0), new Vector2(0, 190), new Vector2(1000, 60));
        }

        // S22 in-match HUD for co-op
        void BuildMatchHud()
        {
            var s = net?.State; var team = net?.MyCart; if (s == null || team == null) return;

            // the whole room under the HUD: taps walk there (the joystick zone is drawn on top of it)
            var catcher = BobUi.Stretch(BobUi.Rect("WorldTap", page));
            var ci = catcher.gameObject.AddComponent<Image>(); ci.color = new Color(0, 0, 0, 0);
            catcher.gameObject.AddComponent<CoopPad>().Init(this, false);
            var joy = BobUi.Rect("JoystickZone", page);
            joy.anchorMin = Vector2.zero; joy.anchorMax = new Vector2(0.4f, 0.6f); joy.offsetMin = joy.offsetMax = Vector2.zero;
            var ji = joy.gameObject.AddComponent<Image>(); ji.color = new Color(0, 0, 0, 0);
            joy.gameObject.AddComponent<CoopPad>().Init(this, true);

            // top bar: round, timer, budget, wins
            var bar = BobUi.HBox(page, T.spaceM, "TopBar"); bar.childForceExpandWidth = false;
            BobUi.Pin((RectTransform)bar.transform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), Vector2.zero, new Vector2(1400, 110));
            var left = team.budget - BobRules.StackCost(team.stack ?? new string[0]);
            BobUi.Size(BobUi.Label(bar.transform, $"รอบ {s.round}/3", T.h2, T.ink, TextAnchor.MiddleCenter, FontStyle.Bold), 110, 110, 0, 260);
            timerText = BobUi.Label(bar.transform, "", T.h1, T.ink, TextAnchor.MiddleCenter, FontStyle.Bold); BobUi.Size(timerText, 110, 110, 0, 240);
            BobUi.Size(BobUi.Label(bar.transform, $"งบ {left}", T.h2, left < 0 ? T.danger : T.ink, TextAnchor.MiddleCenter), 110, 110, 0, 260);
            var wins = s.teams.Length > 1 ? $"ชนะ {s.teams[net.MyTeam].wins} : {s.teams[1 - net.MyTeam].wins}" : "";
            BobUi.Size(BobUi.Label(bar.transform, wins, T.h2, T.ink, TextAnchor.MiddleCenter), 110, 110, 0, 320);
            var leave = Btn(page, "ออก", BobUi.Surface, LeaveMatch, 144, 200);
            BobUi.Pin((RectTransform)leave.transform, new Vector2(0, 1), new Vector2(0, 1), Vector2.zero, new Vector2(200, 144));
            RenderTimer();

            // order ticket: what the room needs vs what our cart gives now (live bars, GDD 9)
            var ticket = BobUi.Box(page, "ใบงาน", s.room?.enemies != null ? string.Join(" ", s.room.enemies.Select(e => BobMatch.Enemy(e).Name)) : null);
            var trt = (RectTransform)ticket.transform; trt.anchorMin = new Vector2(0, 0.62f); trt.anchorMax = new Vector2(0.24f, 0.86f); trt.offsetMin = trt.offsetMax = Vector2.zero;
            var ev = BobRules.EvalStack(team.stack ?? new string[0], s.room?.enemies);
            var need = s.room?.spec?.need;
            string[] names = { "ขจัดคราบ", "ถนอมพื้นผิว", "เร็ว", "ความเงา" };
            int[] want = need == null ? new[] { -1, -1, -1, -1 } : new[] { need.clean, need.gentle, need.speed, need.shine };
            for (int i = 0; i < 4; i++)
            {
                var ok = want[i] < 0 || ev.Stats[i] >= want[i];
                var line = $"{names[i]} {ev.Stats[i]}" + (want[i] >= 0 ? $" / {want[i]} {(ok ? "✓" : "✗")}" : "");
                BobUi.Size(BobUi.Label(ticket.transform, line, T.caption, ok ? T.ink : T.danger), 44, 44);
            }

            // cart card: the shared stack, bottom layer first, and who voted to send
            var cart = BobUi.Box(page, "รถเข็นของทีม", $"{BobRules.CountLayers(team.stack ?? new string[0])}/5 ชั้น");
            var crt = (RectTransform)cart.transform; crt.anchorMin = new Vector2(0.76f, 0.5f); crt.anchorMax = new Vector2(1, 0.86f); crt.offsetMin = crt.offsetMax = Vector2.zero;
            var stack = team.stack ?? new string[0];
            for (int i = stack.Length - 1; i >= 0; i--) BobUi.Size(BobUi.Label(cart.transform, $"{i + 1}. {PartName(stack[i])}", T.caption, T.ink), 44, 44);
            if (stack.Length == 0) BobUi.Size(BobUi.Label(cart.transform, "ยังว่าง เดินไปหยิบของที่ชั้นวาง", T.caption, T.inkMuted), 44, 44);

            // hand: tap a part to put it on the cart / table / a team-mate, whichever is in reach
            handRow = (RectTransform)BobUi.HBox(page, T.spaceS, "Hand").transform;
            BobUi.Pin(handRow, new Vector2(0.5f, 0), new Vector2(0.5f, 0), Vector2.zero, new Vector2(560, 190));

            // context actions (station parts, cart, table) on the right, above the send button
            ctxPanel = (RectTransform)BobUi.VBox(page, T.spaceS, 0, "Context").transform;
            ctxPanel.anchorMin = new Vector2(1, 0); ctxPanel.anchorMax = new Vector2(1, 0); ctxPanel.pivot = new Vector2(1, 0);
            ctxPanel.anchoredPosition = new Vector2(0, T.submitSize.y + T.spaceM); ctxPanel.sizeDelta = new Vector2(560, 520);
            ctxPanel.GetComponent<VerticalLayoutGroup>().childAlignment = TextAnchor.LowerRight;

            // send: hold 0.6 s; in a team it is a vote (half the team sends the cart)
            int mates = s.players.Count(p => p.team == net.MyTeam && p.online), votes = s.players.Count(p => p.team == net.MyTeam && p.online && p.vote);
            var send = BobUi.Button(page, team.submitted ? "ส่งแล้ว รออีกทีม" : $"กดค้างเพื่อส่ง ({votes}/{mates})", T.success, null, T.buttonLg, Color.white);
            BobUi.Pin((RectTransform)send.transform, new Vector2(1, 0), new Vector2(1, 0), Vector2.zero, T.submitSize);
            send.interactable = !team.submitted && !(net.MePlayer?.vote ?? false);
            var hold = BobHoldButton.Attach(send.targetGraphic, BobUi.Circle, T.goodSpark, VoteSend, T.submitHold);
            hold.interactable = send.interactable;

            RenderContext();
        }

        string ContextKey()
        {
            var me = net?.MePlayer; var team = net?.MyCart; if (me == null || team == null) return "";
            var (spot, fam) = Nearby(); var mate = MateInReach();
            return spot + fam + "|" + string.Join(",", me.hand ?? new string[0]) + "|" + string.Join(",", team.table ?? new string[0]) + "|" + team.pusher + "|" + (mate?.id ?? "") + "|" + team.submitted;
        }

        /// <summary>The parts of the hand and the right-hand actions depend on where I stand; rebuilt when that changes.</summary>
        void RenderContext()
        {
            ctxKey = ContextKey();
            if (ctxPanel == null || handRow == null || net?.State == null) return;
            BobUi.Clear(ctxPanel); BobUi.Clear(handRow);
            var me = net.MePlayer; var team = net.MyCart; var s = net.State;
            var (spot, fam) = Nearby();
            var mate = MateInReach();

            var hand = me.hand ?? new string[0];
            for (int i = 0; i < BobCoopLayout.Hand; i++)
            {
                if (i < hand.Length)
                {
                    int k = i; var id = hand[i];
                    var verb = spot == Spot.Cart ? "วางบนรถ" : spot == Spot.Table ? "พักบนโต๊ะ" : mate != null ? "ส่งให้ " + Short(mate.name) : "ถืออยู่";
                    var b = Btn(handRow, PartName(id) + "\n" + verb, spot == Spot.Cart ? BobUi.Sun : BobUi.Surface, () => UseHand(k), 190, 270, T.caption);
                    var icon = BobUi.Img(b.transform, "part_" + id, 80); icon.transform.SetAsFirstSibling();
                    BobUi.Pin(icon.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -8), new Vector2(80, 80));
                }
                else BobUi.Size(BobUi.Panel(handRow, new Color(1, 1, 1, 0.35f), "ui_panel", "Empty"), 190, 190, 0, 270);
            }

            if (spot == Spot.Station && fam != null && !team.submitted)
            {
                var ids = fam == "method" ? BobRules.Methods : (s.room?.offer ?? new string[0]).Where(id => BobRules.Parts[id].Family.ToString().ToLowerInvariant() == fam).ToArray(); // station id = family name
                foreach (var id in ids)
                {
                    var p = BobRules.Parts[id]; var pid = id;
                    Btn(ctxPanel, $"หยิบ {p.Name} ({p.Cost})", BobUi.Surface, () => Pick(pid), 120, 520, T.caption);
                }
            }
            if (spot == Spot.Cart && !team.submitted)
            {
                if ((team.stack?.Length ?? 0) > 0) Btn(ctxPanel, "ยกชั้นบนสุดออก", BobUi.Surface, Lift, 160, 520);
                Btn(ctxPanel, Pushing ? "ปล่อยรถเข็น" : "จับเข็นรถ", BobUi.Surface, TogglePush, 160, 520);
            }
            else if (Pushing) Btn(ctxPanel, "ปล่อยรถเข็น", BobUi.Surface, TogglePush, 160, 520);
            if (spot == Spot.Table)
            {
                var items = team.table ?? new string[0];
                for (int k = 0; k < items.Length; k++) { int kk = k; Btn(ctxPanel, "หยิบ " + PartName(items[k]) + " จากโต๊ะ", BobUi.Surface, () => TakeFromTable(kk), 140, 520, T.caption); }
                if (items.Length == 0) BobUi.Size(BobUi.Label(ctxPanel, "โต๊ะว่าง แตะของในมือเพื่อพักไว้ให้เพื่อน", T.caption, T.inkMuted, TextAnchor.MiddleRight), 90, 90);
            }
            BobHitArea.EnsureAll(ctxPanel, T.hitInMatch); BobHitArea.EnsureAll(handRow, T.hitInMatch);
        }

        void RenderTimer()
        {
            if (timerText == null || net?.State == null) return;
            var t = Mathf.CeilToInt(Mathf.Max(0, net.State.time));
            timerText.text = $"{t / 60}:{t % 60:00}";
            timerText.color = t <= 10 ? T.danger : T.ink;
        }

        // S25: split screen, left = our team, right = the other team; no buttons (watch only)
        void BuildTestOverlay()
        {
            var r = lastResult; if (r?.teams == null || net == null) { Center("กำลังทดสอบ…", null, null); return; }
            for (int side = 0; side < 2; side++)
            {
                int team = side == 0 ? net.MyTeam : 1 - net.MyTeam;
                var tr = r.teams.FirstOrDefault(x => x.team == team); if (tr == null) continue;
                var box = BobUi.Box(page, side == 0 ? "ทีมเรา" : "ทีมคู่แข่ง", tr.score != null ? (tr.score.pass ? "ผ่านสเปก ✓" : "ไม่ผ่านสเปก ✗") : null);
                var rt = (RectTransform)box.transform;
                rt.anchorMin = new Vector2(side * 0.5f + 0.02f, 0.7f); rt.anchorMax = new Vector2(side * 0.5f + 0.48f, 1); rt.offsetMin = rt.offsetMax = Vector2.zero;
                BobUi.Size(BobUi.Label(box.transform, $"คะแนน {tr.score?.total ?? 0}", T.h2, T.ink, TextAnchor.MiddleLeft, FontStyle.Bold), 70, 70);
                if (tr.stats != null)
                    BobUi.Size(BobUi.Label(box.transform, $"ขจัดคราบ {tr.stats.clean} · ถนอม {tr.stats.gentle} · เร็ว {tr.stats.speed} · เงา {tr.stats.shine}", T.caption, T.ink), 50, 50);
                BobUi.Size(BobUi.Label(box.transform, tr.stack != null && tr.stack.Length > 0 ? string.Join(" → ", tr.stack.Select(PartName)) : "รถเข็นว่าง", T.caption, T.inkMuted), 50, 50);
            }
            var msg = r.winner < 0 ? "เสมอ!" : r.winner == net.MyTeam ? "ทีมเราชนะรอบนี้!" : "ทีมคู่แข่งชนะรอบนี้";
            var big = BobUi.Label(page, msg + (r.over ? "  · จบแมตช์" : "  · รอบต่อไปเริ่มในไม่กี่วินาที"), T.h2, T.ink, TextAnchor.MiddleCenter, FontStyle.Bold);
            BobUi.Pin(big.rectTransform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 20), new Vector2(1600, 110));
        }

        // S27 (simplified): match result, everyone in the team gets the same reward
        void BuildResult()
        {
            var r = lastResult; int my = net?.MyTeam ?? 0;
            int a = r?.wins != null && r.wins.Length > 1 ? r.wins[my] : 0, b = r?.wins != null && r.wins.Length > 1 ? r.wins[1 - my] : 0;
            var title = a > b ? "ทีมเราชนะ!" : a < b ? "ทีมคู่แข่งชนะ เก่งมากทั้งสองทีม" : "เสมอกัน!";
            Center($"{title}\nรอบที่ชนะ {a} : {b}\nรางวัลแบ่งเท่ากันทุกคนในทีม", "กลับเมนู", LeaveMatch);
        }

        // ---------------------------------------------------------------- joystick + tap-to-walk (UX spec CTRL-Joystick, CTRL-TapToMove)

        sealed class CoopPad : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
        {
            BobCoopGame game; bool stick; Vector2 origin; bool dragged;
            public void Init(BobCoopGame g, bool isStick) { game = g; stick = isStick; }
            public void OnPointerDown(PointerEventData e) { origin = e.position; dragged = false; }
            public void OnDrag(PointerEventData e)
            {
                if (!stick) return;
                float full = 100f * Screen.height / 1080f; // 100u = full speed
                var d = (e.position - origin) / Mathf.Max(1, full);
                if (d.magnitude > 0.12f) dragged = true; // 12% dead zone
                game.moveInput = d.magnitude > 1 ? d.normalized : (d.magnitude < 0.12f ? Vector2.zero : d);
            }
            public void OnPointerUp(PointerEventData e)
            {
                if (stick) game.moveInput = Vector2.zero;
                if (!dragged && (e.position - origin).magnitude < 36f * Screen.height / 1080f) game.TapWorld(e.position);
            }
        }
    }
}
