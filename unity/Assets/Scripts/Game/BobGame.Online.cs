// Battle of the Brooms: online mode of the scene, on top of the server-authoritative match 'bob_coop' (server/match.js).
// The server owns the round: room, clock, each team's shared cart, hands and scores. This file copies what the server
// says into the same fields the single-player game uses (R, stack, hand, budget, time, wins), so the HUD, cart and
// result screens work unchanged, and draws team-mates walking in the room. The other team plays the same room in its own
// copy, so only its progress shows (top bar). Abilities run on the server: each team has its own clock (Press),
// Bubble's shield covers the team cart, Spray's preview comes back to that player only.
// Flow: title "ออนไลน์" -> matchmaker (2-4 players) -> lobby (ready) -> rounds -> result.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bob.Art;
using Bob.Net;
using Bob.Rules;
using UnityEngine;
using UnityEngine.UI;

namespace Bob.Game
{
    public partial class BobGame
    {
        BobOnline mp;
        bool mpSearching, mpReady;
        string mpNote = "";
        float mpSendT;
        Vector2 mpSent;

        sealed class Mate { public string Id, Char, Anim; public SpriteRenderer Sr; public BobSpriteAnimator An; public TextMesh Name; public Vector2 To; public Transform Hand; public string HandKey; }
        readonly Dictionary<string, Mate> mates = new Dictionary<string, Mate>();

        /// <summary>Hand size: 3 solo, 2 in a team (the server enforces it online).</summary>
        int HandMax => mp != null ? BobRules.HandTeam : Hand;

        bool Online => mp != null;

        // ---------------------------------------------------------------- joining and leaving

        async void FindOnline()
        {
            if (!online || server == null) { Toast("ต้องเชื่อมต่อเซิร์ฟเวอร์ก่อน (ตั้งค่า > ที่อยู่เซิร์ฟเวอร์)"); Sfx("drop_invalid"); return; }
            if (mpSearching || mp != null) return;
            mpSearching = true; mpReady = false; mpNote = "กำลังหาผู้เล่นอีก 1-3 คน…";
            phase = Phase.Lobby; Sfx("ui_confirm"); RenderAll();
            try
            {
                await WithTimeout(server.EnsureConnectedAsync(), 6000);
                var o = await BobOnline.FindAsync(server, 2, 4);
                if (!mpSearching) { await o.LeaveAsync(); return; } // cancelled while the join was in flight
                AttachOnline(o);
            }
            catch (TaskCanceledException) { }
            catch (Exception e)
            {
                Debug.LogWarning("[BobGame] online: " + e.Message);
                if (mpSearching) { mpSearching = false; ToTitle(); Toast("หาห้องไม่สำเร็จ: " + e.Message); }
            }
        }

        /// <summary>Test / private-room entry: join a known match id (BobServer.CreateMatchRpcAsync makes one).</summary>
        async Task JoinOnlineAsync(string matchId)
        {
            await server.EnsureConnectedAsync();
            mpSearching = true; phase = Phase.Lobby; RenderAll();
            AttachOnline(await BobOnline.JoinAsync(server, matchId));
        }

        void AttachOnline(BobOnline o)
        {
            mp = o; mpSearching = false; mpNote = "";
            matchId = "mp" + (o.MatchId.Length > 20 ? o.MatchId.Substring(0, 20) : o.MatchId).Replace("-", "");
            mp.OnState += OnMpState;
            mp.OnPositions += OnMpPositions;
            mp.OnResult += OnMpResult;
            mp.OnPreview += pv => { preview = new ScoreResult { Total = pv.total, Pass = pv.pass }; Toast("เล็งเป้า! กองนี้จะได้ " + pv.total + " คะแนน"); RenderAll(); };
            mp.OnRefused += t => { Toast(t); Sfx("drop_invalid"); Debug.Log("[BobGame] server refused: " + t + " at " + pos + " (server " + (mp?.MePlayer != null ? new Vector2(mp.MePlayer.x, mp.MePlayer.y).ToString() : "?") + ")"); };
            roundNo = 0; wins = 0; losses = 0; results.Clear(); R = null;
            mp.Ready(charId, false); // tells the server our hero; the player taps Ready in the lobby
            if (mp.State != null) OnMpState(mp.State);
            RenderAll();
        }

        void LeaveOnline()
        {
            if (mpSearching && mp == null) { mpSearching = false; _ = BobOnline.CancelFindAsync(server); }
            mpSearching = false;
            if (mp == null) return;
            var o = mp; mp = null;
            _ = o.LeaveAsync();
            ClearMates();
        }

        void ToggleReady()
        {
            if (mp == null) return;
            mpReady = !mpReady; mp.Ready(charId, mpReady); Sfx(mpReady ? "ui_confirm" : "ui_cancel");
        }

        // ---------------------------------------------------------------- server -> scene

        void OnMpState(MpState s)
        {
            if (mp == null || s == null) return;
            var me = mp.MePlayer; if (me == null) return;
            var team = s.teams[me.team];
            if (s.phase == "lobby") { phase = Phase.Lobby; RenderAll(); return; }

            if (s.phase == "build" && s.round != roundNo)
            {
                // a new round: same room for everyone, positions reset by the server
                roundNo = s.round; seed = s.seed; R = ToRound(s.room);
                stack.Clear(); hand.Clear(); preview = null; shield = false; swapMode = false; abUsed = false; freeze = 0;
                ResetRoomForRound();
                pos = new Vector2(me.x, me.y); mpSent = pos;
                if (roundNo == 1) Log("match_start", $"{{\"seed\":{seed},\"char\":\"{charId}\",\"mode\":\"online\",\"players\":{s.players.Length}}}");
                phase = Phase.Build; Sfx("round_start"); Toast("รอบ " + roundNo + " เริ่มแล้ว! ช่วยกันซ้อนรถเข็นของทีม");
            }
            if (phase == Phase.Lobby && s.phase != "lobby" && s.phase != "build") return; // joined late: wait for the next state

            // the team's shared cart: fire the reaction effects when a team-mate's layer lands
            var newStack = (team.stack ?? new string[0]).ToList();
            if (!newStack.SequenceEqual(stack))
            {
                var before = Ev().Fx.Select(f => f.Text).ToList();
                bool grew = newStack.Count > stack.Count;
                stack.Clear(); stack.AddRange(newStack);
                if (grew) { Sfx("place_layer"); OnLayerPlaced(Ev().Fx.Where(f => f.Src != "enemy" && !before.Contains(f.Text)).ToList()); }
                else Sfx("lift_layer");
            }
            var newHand = (me.hand ?? new string[0]).ToList();
            if (newHand.Count > hand.Count) { Sfx("pickup"); PopHand(); }
            hand.Clear(); hand.AddRange(newHand);
            budget = team.budget; time = team.time; freeze = team.freeze; shield = team.shield;
            abUsed = me.abUsed; // the server logs ability_used itself
            SyncMates(s, me);
            RenderAll();
        }

        void OnMpPositions(MpPositions p)
        {
            if (mp == null) return;
            time = p.tt != null && p.tt.Length == 2 ? p.tt[mp.MyTeam] : p.time;
            if (freeze > 0) freeze = Mathf.Max(0, freeze - 0.1f);
            var me = mp.MePlayer;
            // the server is the truth: if it disagrees a lot (blocked move, lag), snap back
            if (me != null && phase == Phase.Build && Vector2.Distance(pos, new Vector2(me.x, me.y)) > 0.75f) { pos = new Vector2(me.x, me.y); mpSent = pos; target = null; }
            foreach (var q in p.p) if (mates.TryGetValue(q.id, out var m)) m.To = new Vector2(q.x, q.y);
        }

        void OnMpResult(MpResult r)
        {
            if (mp == null) return;
            int my = mp.MyTeam, other = 1 - my;
            var a = r.teams[my]; var b = r.teams[other];
            bool win = r.winner == my;
            wins = r.wins[my]; losses = r.wins[other];
            while (results.Count < r.round - 1) results.Add(false);
            results.Add(win);
            var myStack = a.stack.ToList(); var theirStack = b.stack.ToList();
            last = new LastRound
            {
                Ev = BobRules.EvalStack(myStack, R.Enemies, false),
                Stats = new[] { a.stats.clean, a.stats.gentle, a.stats.speed, a.stats.shine },
                Mine = ToScore(a.score),
                Rival = new RivalPlay { Stack = theirStack, Ev = BobRules.EvalStack(theirStack, R.Enemies, false), Score = ToScore(b.score) },
                Win = win, FromServer = true, Stack = myStack
            };
            Log("round_result", "{" +
                $"\"round\":{r.round},\"seed\":{seed},\"result\":\"{(win ? "win" : r.winner < 0 ? "draw" : "lose")}\",\"stack\":{JsonList(myStack)},\"my_total\":{a.score.total}," +
                $"\"rival_total\":{b.score.total},\"pass\":{B(a.score.pass)},\"rival_pass\":{B(b.score.pass)},\"rival_stack\":{JsonList(theirStack)}," +
                $"\"cost\":{a.cost},\"budget\":{budget},\"time_left\":{Mathf.RoundToInt(time)},\"ability_used\":false," +
                $"\"enemies\":{JsonList(R.Enemies)},\"char\":\"{charId}\",\"client\":\"unity\",\"mode\":\"online\"}}");
            OnRoundTested(win);
            Sfx(win ? "round_win" : "round_lose");
            phase = Phase.Test; RenderAll();
        }

        static ScoreResult ToScore(MpScore s) => new ScoreResult { Pass = s.pass, Test = s.test, Budget = s.budget, Time = s.time, Total = s.total };

        static Round ToRound(MpRoom j)
        {
            var r = new Round { Number = j.round, Enemies = j.enemies, Offer = j.offer };
            void Add(string k, int v) { if (v >= 0) r.Need[k] = v; }
            Add("clean", j.spec.need.clean); Add("gentle", j.spec.need.gentle); Add("speed", j.spec.need.speed); Add("shine", j.spec.need.shine);
            if (j.spec.notes != null) r.Notes.AddRange(j.spec.notes);
            return r;
        }

        // ---------------------------------------------------------------- scene -> server (intents)

        void OnlinePick(string id)
        {
            if (hand.Count >= HandMax) { Toast("มือเต็มแล้ว ถือได้ " + HandMax + " ชิ้น"); Sfx("drop_invalid"); return; }
            SendPosNow(); mp.Pick(id);
        }

        void OnlinePlace(int i)
        {
            var why = BobRules.WhyCannotPlace(stack, hand[i], BudgetLeft);
            if (why != null) { Toast(why); Sfx("drop_invalid"); return; }
            SendPosNow(); mp.Place(i);
        }

        void OnlineLift() { SendPosNow(); mp.Lift(); }

        void OnlineSubmit()
        {
            if (mp.MyCart != null && mp.MyCart.submitted) return;
            mp.Submit(); Sfx("ui_confirm"); Toast("ส่งรถเข็นแล้ว รอทีมคู่แข่ง…");
        }

        /// <summary>The hero's ability online: the server checks and applies it, these are only the local effects.</summary>
        void OnlineAbility()
        {
            switch (charId)
            {
                case "sweep":
                    if (stack.Count < 2) { Toast("ต้องมีอย่างน้อย 2 ชิ้นบนรถเข็น"); return; }
                    swapMode = !swapMode; Sfx("ui_tap"); RenderAll(); return; // the swap itself goes through SwapAt
                case "bubble": Toast("โล่ฟอง! กันปฏิกิริยาหักล้างที่แรงที่สุดให้ทั้งทีม"); Sfx("bubble_pop"); SpawnFx("fx_bubble_pop", PlayerPos + Vector2.up * 0.5f); break;
                case "spray": Sfx("spray_psst"); SpawnFx("fx_target", CartTop); break; // the score arrives as OnPreview
                case "press": Toast("รีดเวลา! หยุดนาฬิกาของทีม 8 วินาที"); Sfx("steam_hiss"); SpawnFx("fx_steam", PlayerPos + Vector2.up * 0.5f); break;
            }
            mp.Ability(0); Sfx("ability_activate");
        }

        void SendPosNow() { if (mp == null) return; mp.Move(pos.x, pos.y); mpSent = pos; mpSendT = 0; }

        /// <summary>Every frame while online: send our position ~10 times a second, move team-mates.</summary>
        void TickOnline()
        {
            if (mp == null) return;
            if (phase == Phase.Build)
            {
                mpSendT += Time.unscaledDeltaTime;
                if (mpSendT >= 0.1f && Vector2.Distance(pos, mpSent) > 0.01f) SendPosNow();
            }
            foreach (var m in mates.Values)
            {
                if (m.Sr == null) continue;
                Vector2 cur = m.Sr.transform.position;
                var next = Vector2.Lerp(cur, m.To, 1 - Mathf.Exp(-12f * Time.deltaTime));
                bool walking = Vector2.Distance(cur, m.To) > 0.02f;
                if (Mathf.Abs(next.x - cur.x) > 0.001f) m.Sr.flipX = next.x < cur.x;
                m.Sr.transform.position = next; m.Sr.sortingOrder = Order(next.y);
                var anim = m.HandKey.Length > 0 ? "carry" : walking ? "walk" : "idle";
                if (anim != m.Anim) { m.Anim = anim; var e = art.Find(m.Char + "_" + anim) ?? art.Find(m.Char + "_idle"); if (e != null) m.An.Play(e.frames, e.fps, e.loop); }
            }
        }

        // ---------------------------------------------------------------- team-mates in the room

        void SyncMates(MpState s, MpPlayer me)
        {
            var keep = new HashSet<string>();
            foreach (var p in s.players)
            {
                if (p.id == me.id || p.team != me.team) continue;
                keep.Add(p.id);
                if (!mates.TryGetValue(p.id, out var m) || m.Char != p.@char)
                {
                    if (m != null && m.Sr != null) Destroy(m.Sr.gameObject);
                    m = new Mate { Id = p.id, Char = p.@char, HandKey = "" };
                    m.Sr = MakeSprite("Mate_" + p.name, worldRoot, p.@char + "_idle", new Vector2(p.x, p.y), 0f, true);
                    var spr = art.Get(p.@char + "_idle");
                    m.Sr.transform.localScale = Vector3.one * (0.95f / Mathf.Max(0.01f, spr.bounds.size.y));
                    if (!art.Has(p.@char + "_idle")) { m.Sr.transform.localScale = new Vector3(0.3f, 0.45f, 1); m.Sr.color = BobUi.Hex(BobMatch.Char(p.@char).Color); }
                    m.An = m.Sr.gameObject.AddComponent<BobSpriteAnimator>();
                    float k = 1f / Mathf.Max(0.001f, m.Sr.transform.localScale.x);
                    m.Name = WorldLabel(string.IsNullOrEmpty(p.name) ? BobMatch.Char(p.@char).Name : p.name, Vector2.zero, 0.022f, BobUi.Ink, m.Sr.transform);
                    m.Name.transform.localPosition = new Vector3(0, -0.12f * k, 0); m.Name.transform.localScale = Vector3.one * k;
                    m.Hand = new GameObject("Hand").transform; m.Hand.SetParent(m.Sr.transform, false);
                    mates[p.id] = m;
                }
                m.To = new Vector2(p.x, p.y);
                if (s.phase == "build" && roundNo == s.round && Vector2.Distance(m.Sr.transform.position, m.To) > 1.5f) m.Sr.transform.position = m.To;
                var hk = string.Join(",", p.hand ?? new string[0]);
                if (hk != m.HandKey)
                {
                    m.HandKey = hk;
                    foreach (Transform c in m.Hand) Destroy(c.gameObject);
                    float k = 1f / Mathf.Max(0.001f, m.Sr.transform.localScale.x);
                    var held = p.hand ?? new string[0];
                    for (int i = 0; i < held.Length; i++)
                    {
                        var ic = MakeSprite("Held_" + held[i], m.Hand, "part_" + held[i], Vector2.zero, 0.24f, false);
                        ic.transform.localPosition = new Vector3((-0.12f + i * 0.24f) * k, 1.12f * k, 0); ic.transform.localScale *= k; ic.sortingOrder = 2500;
                    }
                }
            }
            foreach (var id in mates.Keys.Where(k => !keep.Contains(k)).ToList()) { if (mates[id].Sr != null) Destroy(mates[id].Sr.gameObject); mates.Remove(id); }
        }

        void ClearMates() { foreach (var m in mates.Values) if (m.Sr != null) Destroy(m.Sr.gameObject); mates.Clear(); }

        /// <summary>Top bar note: how far the other team is.</summary>
        string OnlineRivalNote()
        {
            if (mp?.State?.teams == null || mp.MePlayer == null) return "";
            var t = mp.State.teams[1 - mp.MyTeam]; var mine = mp.MyCart;
            return $"\n<size=18>ทีมคู่แข่ง {BobRules.CountLayers((t.stack ?? new string[0]).ToList())} ชั้น{(t.submitted ? " · ส่งแล้ว" : "")}{(mine != null && mine.submitted ? " · ทีมเราส่งแล้ว" : "")}</size>";
        }

        // ---------------------------------------------------------------- lobby screen

        void Lobby()
        {
            var S = screenRoot;
            ScreenBackdrop("lobby_condo");
            var card = Card(S, "Lobby", BobUi.Bg); BobUi.Pin(card, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 40), new Vector2(1100, 0));
            VStack(card, 46, 34, 16);
            card.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            BobUi.Label(card, "เล่นออนไลน์ · 2 ทีม", 48, BobUi.Ink, TextAnchor.MiddleCenter, FontStyle.Bold);

            if (mp == null || mp.State == null)
            {
                BobUi.Label(card, mpNote, 30, BobUi.Muted, TextAnchor.MiddleCenter);
                BobUi.Label(card, "จับคู่ 2-4 คน แบ่งเป็น 2 ทีม ทุกทีมได้ห้องเดียวกัน ทีมเดียวกันใช้รถเข็นคันเดียวกัน", 24, BobUi.Muted, TextAnchor.MiddleCenter);
                var spin = BobUi.Img(S, "icon_star", 90); BobUi.Pin(spin.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 260), new Vector2(90, 90));
                StartCoroutine(Spin(spin.transform));
            }
            else
            {
                var s = mp.State;
                for (int team = 0; team < 2; team++)
                {
                    var head = BobUi.Label(card, team == 0 ? "ทีมไม้กวาด" : "ทีมฟองน้ำ", 30, team == 0 ? BobUi.Teal : BobUi.Rose, TextAnchor.MiddleLeft, FontStyle.Bold);
                    BobUi.Size(head, 40, 40);
                    foreach (var p in s.players.Where(x => x.team == team))
                    {
                        bool me = p.id == mp.Me;
                        var row = Card(card, "Player", me ? BobUi.Hex("#FFF0C2") : BobUi.Surface, 1.4f); BobUi.Size(row, 96, 96);
                        var h = row.gameObject.AddComponent<HorizontalLayoutGroup>(); h.padding = new RectOffset(16, 20, 8, 8); h.spacing = 16;
                        h.childControlWidth = h.childControlHeight = true; h.childForceExpandWidth = false; h.childForceExpandHeight = false; h.childAlignment = TextAnchor.MiddleLeft;
                        BobUi.Img(row, p.@char + "_portrait", 80);
                        BobUi.Size(BobUi.Label(row, (me ? "คุณ · " : "") + BobMatch.Char(p.@char).Name + "  <color=#6B6E86>" + p.name + "</color>", 30, BobUi.Ink), 80, 80, 1);
                        var st = BobUi.Label(row, !p.online ? "หลุด" : p.ready ? "พร้อม ✓" : "รอ…", 28, p.ready ? BobUi.Good : BobUi.Muted, TextAnchor.MiddleRight, FontStyle.Bold);
                        st.supportRichText = true; BobUi.Size(st, 80, 80, 0, 160);
                    }
                }
                int n0 = s.players.Count(p => p.team == 0), n1 = s.players.Count(p => p.team == 1);
                BobUi.Label(card, n0 > 0 && n1 > 0 ? "เกมเริ่มเมื่อทุกคนกดพร้อม" : "รอผู้เล่นอีกทีม…", 24, BobUi.Muted, TextAnchor.MiddleCenter);
            }

            var leave = BobUi.Button(S, "ออก", BobUi.Surface, () => { LeaveOnline(); ToTitle(); }, 40);
            BobUi.Pin((RectTransform)leave.transform, new Vector2(0, 0), new Vector2(0, 0), new Vector2(48, 40), new Vector2(300, 130));
            if (mp != null && mp.State != null)
            {
                var ready = BobUi.Button(S, mpReady ? "ยกเลิกพร้อม" : "พร้อม!", mpReady ? BobUi.Surface : BobUi.Sun, ToggleReady, 56);
                BobUi.Pin((RectTransform)ready.transform, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-48, 40), new Vector2(440, 160));
                ready.GetComponentInChildren<Text>().fontStyle = FontStyle.Bold;
            }
        }
    }
}
