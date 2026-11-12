// Battle of the Brooms: the online co-op scene (Assets/Scenes/Coop.unity, menu Bob > Build Coop Scene).
// 2-4 players, two teams, one shared cart per team, server-authoritative (server/match.js 'bob_coop'):
//   menu (quick match / create room / join by code) -> lobby (team, hero, ready) -> 3 rounds of
//   [build: walk, pick, pass, park on the table, push the cart, place, lift, vote to send] -> [split-screen test] -> result.
// The phone only sends intents through Net/BobOnline.cs; everything shown comes from the server's STATE / POS / RESULT.
// This file: flow, networking and input. BobCoopGame.World.cs: the two rooms, players, carts, split view.
// BobCoopGame.Hud.cs: the uGUI screens. Layout numbers: BobCoopLayout.cs (same as the server).
// UNCOMPILED IN UNITY: written in a cloud session and type-checked with unity-typecheck/ only. See README "Co-op scene".
using System;
using System.Linq;
using System.Threading.Tasks;
using Bob.Art;
using Bob.Game;
using Bob.Net;
using Bob.Rules;
using UnityEngine;

namespace Bob.Coop
{
    public partial class BobCoopGame : MonoBehaviour
    {
        [Header("Art (filled by Bob > Build Coop Scene)")]
        public BobArtLibrary art;

        [Header("Nakama server")]
        [Tooltip("127.0.0.1 in the Editor. On a phone, your PC's LAN IP. Saved in PlayerPrefs bob.host (shared with the Game scene).")]
        public string host = "127.0.0.1";
        public int port = 7350;
        public string serverKey = "defaultkey";

        enum View { Connecting, Offline, Menu, Searching, Lobby, Build, Test, Result }
        View screen = View.Connecting;

        BobServer server;
        BobOnline net;
        string charId = "sweep", status = "";
        MpResult lastResult;
        int shownRound = -1;
        float testShownAt;

        // local prediction for my own hero (the server corrects us through POS)
        Vector2 myPos; bool havePos;
        Vector2 moveInput; Vector2? tapTarget;
        float sendT;
        const float SendEvery = 0.1f, SnapDist = 0.6f;

        void Awake()
        {
            if (art == null) { art = ScriptableObject.CreateInstance<BobArtLibrary>(); Debug.LogWarning("[BobCoop] no art library: run Bob > Build Coop Scene"); }
            BobUi.Art = art;
            try
            {
                var c = PlayerPrefs.GetString("bob.char", "sweep"); if (BobMatch.Chars.Any(x => x.Id == c)) charId = c;
                var h = PlayerPrefs.GetString("bob.host", ""); if (!string.IsNullOrWhiteSpace(h)) host = h.Trim();
            }
            catch (Exception) { }
            Application.targetFrameRate = 60;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            Screen.autorotateToPortrait = false; Screen.autorotateToPortraitUpsideDown = false;
            Screen.autorotateToLandscapeLeft = true; Screen.autorotateToLandscapeRight = true;
        }

        void Start()
        {
            BuildWorld();
            BuildHud();
            Connect();
        }

        async void OnDestroy()
        {
            try { if (net != null) await net.LeaveAsync(); if (server != null) await server.CloseAsync(); } catch (Exception) { }
        }

        // ---------------------------------------------------------------- connection and matches

        async void Connect()
        {
            Go(View.Connecting, "กำลังเชื่อมต่อ " + host + "…");
            try
            {
                server = new BobServer("http", host, port, serverKey);
                await WithTimeout(server.SignInAsync(SystemInfo.deviceUniqueIdentifier + "-bob-coop"), 5000);
                await WithTimeout(server.ConnectAsync(), 5000);
                Go(View.Menu, "ออนไลน์ (" + host + ")");
            }
            catch (Exception e)
            {
                Debug.LogWarning("[BobCoop] cannot reach the server: " + e.Message);
                Go(View.Offline, "ต่อเซิร์ฟเวอร์ไม่ได้ ตรวจ IP แล้วลองใหม่ (เล่นออนไลน์ต้องมีเซิร์ฟเวอร์)");
            }
        }

        void SetHost(string h)
        {
            h = (h ?? "").Trim(); if (h.Length == 0) return;
            host = h; try { PlayerPrefs.SetString("bob.host", h); PlayerPrefs.Save(); } catch (Exception) { }
            Connect();
        }

        async void QuickMatch()
        {
            Go(View.Searching, "กำลังหาผู้เล่น 2-4 คน…"); Sfx("ui_confirm");
            try { Attach(await BobOnline.FindAsync(server)); }
            catch (Exception e) { Fail("หาแมตช์ไม่สำเร็จ", e); }
        }

        async void CreateRoom()
        {
            Go(View.Searching, "กำลังสร้างห้อง…"); Sfx("ui_confirm");
            try { Attach(await BobOnline.CreateAsync(server)); }
            catch (Exception e) { Fail("สร้างห้องไม่สำเร็จ", e); }
        }

        async void JoinRoom(string code)
        {
            code = (code ?? "").Trim(); if (code.Length == 0) { Toast("ใส่รหัสห้องก่อน"); return; }
            Go(View.Searching, "กำลังเข้าห้อง…"); Sfx("ui_confirm");
            try { Attach(await BobOnline.JoinAsync(server, code)); }
            catch (Exception e) { Fail("เข้าห้องไม่สำเร็จ (รหัสผิด ห้องเต็ม หรือเริ่มไปแล้ว)", e); }
        }

        void Fail(string msg, Exception e) { Debug.LogWarning("[BobCoop] " + msg + ": " + e.Message); Go(View.Menu, msg); Sfx("drop_invalid"); }

        void Attach(BobOnline o)
        {
            net = o;
            net.OnState += OnState;
            net.OnPositions += OnPositions;
            net.OnResult += OnResult;
            net.OnRefused += t => { Toast(t); Sfx("drop_invalid"); Buzz(); };
            lastResult = null; shownRound = -1; havePos = false;
            Go(View.Lobby, "");
            Sfx("lobby_join");
            net.Ready(charId, false);
            if (net.State != null) OnState(net.State);
        }

        async void LeaveMatch()
        {
            var o = net; net = null;
            if (o != null) { o.OnState -= OnState; o.OnPositions -= OnPositions; o.OnResult -= OnResult; try { await o.LeaveAsync(); } catch (Exception) { } }
            SetSplit(false);
            Go(View.Menu, ""); Sfx("ui_cancel");
        }

        // ---------------------------------------------------------------- server messages

        void OnState(MpState s)
        {
            switch (s.phase)
            {
                case "lobby": if (screen != View.Lobby) Go(View.Lobby, ""); break;
                case "build":
                    if (shownRound != s.round)
                    {
                        shownRound = s.round; havePos = false; tapTarget = null;
                        Go(View.Build, ""); SetSplit(false); ResetRooms(s); Sfx("round_start");
                    }
                    break;
                case "test": if (screen == View.Build) ShowTest(); break;
                case "result": if (screen != View.Result && screen != View.Test) ShowTest(); break;
            }
            var me = net?.MePlayer;
            if (me != null && !havePos) { myPos = new Vector2(me.x, me.y); havePos = true; }
            RenderWorld(); RenderHud();
        }

        void OnPositions(MpPositions p)
        {
            var me = net?.MePlayer;
            if (me != null && havePos && Vector2.Distance(myPos, new Vector2(me.x, me.y)) > SnapDist) myPos = new Vector2(me.x, me.y); // corrected
            RenderTimer();
        }

        void OnResult(MpResult r)
        {
            lastResult = r;
            bool win = net != null && r.winner == net.MyTeam;
            Sfx(r.winner < 0 ? "reaction_good" : win ? "round_win" : "round_lose");
            ShowTest();
        }

        void ShowTest()
        {
            if (screen == View.Test) return;
            testShownAt = Time.time;
            Go(View.Test, ""); SetSplit(true); ShowTestRooms(lastResult);
            RenderHud();
        }

        // ---------------------------------------------------------------- per frame: input and movement

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape)) OnBack();
            if (screen == View.Test && lastResult != null && lastResult.over && Time.time - testShownAt > 8f) { Go(View.Result, ""); SetSplit(false); RenderHud(); }
            if (screen == View.Build && net?.State != null && net.MePlayer != null) TickMove(Time.deltaTime);
            TickWorld(Time.deltaTime);
        }

        void OnBack()
        {
            if (screen == View.Lobby || screen == View.Result || screen == View.Searching) LeaveMatch();
            else if (screen == View.Build) Toast("ออกกลางแมตช์ได้จากปุ่ม ออก (เพื่อนเล่นต่อได้)");
        }

        bool Pushing => net?.MyCart != null && net.MyCart.pusher == net.Me;

        void TickMove(float dt)
        {
            var dir = ReadMoveInput();
            if (dir.sqrMagnitude < 0.0001f && tapTarget.HasValue)
            {
                var d = tapTarget.Value - myPos;
                if (d.magnitude < 0.06f) tapTarget = null; else dir = d.normalized;
            }
            if (dir.sqrMagnitude > 1) dir.Normalize();
            if (dir.sqrMagnitude > 0.0001f && !net.MyCart.submitted)
            {
                var speed = BobCoopLayout.Speed * (Pushing ? BobCoopLayout.PushSpeed : 1f);
                var before = myPos;
                var p = BobCoopLayout.Solve(myPos.x + dir.x * speed * dt, myPos.y + dir.y * speed * dt);
                myPos = new Vector2(p.x, p.y);
                if (Pushing) PredictCart(myPos - before);
                walking = true;
            }
            else walking = false;
            sendT -= dt;
            if (sendT <= 0 && walking) { sendT = SendEvery; net.Move(myPos.x, myPos.y); }
        }

        Vector2 ReadMoveInput()
        {
            var k = new Vector2((Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow) ? 1 : 0) - (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow) ? 1 : 0),
                                (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow) ? 1 : 0) - (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow) ? 1 : 0));
            if (k.sqrMagnitude > 0) { tapTarget = null; return k; }
            if (moveInput.sqrMagnitude > 0) tapTarget = null;
            return moveInput; // the on-screen joystick (Hud)
        }

        // ---------------------------------------------------------------- what the player is next to

        enum Spot { None, Station, Cart, Table }

        /// <summary>The closest useful thing in reach of my hero, and for a station its family.</summary>
        (Spot spot, string family) Nearby()
        {
            if (net?.MyCart == null) return (Spot.None, null);
            float best = BobCoopLayout.Near; var r = (Spot.None, (string)null);
            var cart = BobCoopLayout.CartBox(net.MyCart.cx, net.MyCart.cy).Dist(myPos.x, myPos.y);
            if (cart <= best) { best = cart; r = (Spot.Cart, null); }
            var table = BobCoopLayout.Table.Dist(myPos.x, myPos.y);
            if (table < best) { best = table; r = (Spot.Table, null); }
            for (int i = 0; i < 4; i++)
            {
                var d = BobCoopLayout.Stations[i].Dist(myPos.x, myPos.y);
                if (d < best) { best = d; r = (Spot.Station, BobCoopLayout.StationIds[i]); }
            }
            return r;
        }

        /// <summary>The nearest online team-mate within passing range, or null.</summary>
        MpPlayer MateInReach()
        {
            if (net?.State?.players == null) return null;
            MpPlayer best = null; float bd = BobCoopLayout.PassNear;
            foreach (var p in net.State.players)
            {
                if (p.id == net.Me || p.team != net.MyTeam || !p.online) continue;
                var d = Vector2.Distance(myPos, new Vector2(p.x, p.y));
                if (d <= bd) { bd = d; best = p; }
            }
            return best;
        }

        // ---------------------------------------------------------------- intents (the server answers with STATE or a refusal toast)

        void Pick(string id) { net.Move(myPos.x, myPos.y); net.Pick(id); Sfx("pickup"); }

        /// <summary>Tap on a hand slot: put it where it makes sense from here (cart, table, team-mate), in that order.</summary>
        void UseHand(int i)
        {
            if (net?.MePlayer?.hand == null || i >= net.MePlayer.hand.Length) return;
            net.Move(myPos.x, myPos.y);
            var near = Nearby().spot;
            if (near == Spot.Cart) { net.Place(i); Sfx("place_layer"); return; }
            if (near == Spot.Table) { net.TablePut(i); Sfx("table_put"); return; }
            var mate = MateInReach();
            if (mate != null) { net.Pass(i, mate.id); Sfx("pass_part"); return; }
            Toast("เดินไปที่รถเข็น โต๊ะพักของ หรือเพื่อนในทีมก่อน"); Sfx("drop_invalid");
        }

        void Lift() { net.Move(myPos.x, myPos.y); net.Lift(); Sfx("lift_layer"); }
        void TogglePush() { net.Move(myPos.x, myPos.y); net.Push(!Pushing); Sfx(Pushing ? "ui_cancel" : "cart_grab"); }
        void TakeFromTable(int k) { net.Move(myPos.x, myPos.y); net.TableTake(k); Sfx("pickup"); }
        void VoteSend() { net.Submit(); Sfx("vote_tick"); Buzz(); }
        void PickChar(string id) { charId = id; try { PlayerPrefs.SetString("bob.char", id); } catch (Exception) { } net?.Ready(id, MeReady); Sfx("ui_tap"); }
        bool MeReady => net?.MePlayer?.ready ?? false;
        void ToggleReady() { net?.Ready(charId, !MeReady); Sfx(MeReady ? "ui_cancel" : "ready_on"); }
        void SwitchTeam() { if (net?.MePlayer == null) return; net.Ready(charId, false, 1 - net.MyTeam); Sfx("ui_tap"); }

        // ---------------------------------------------------------------- helpers

        void Go(View s, string note) { screen = s; if (note != null) status = note; RenderHud(); }

        static void Sfx(string name) { if (FindFirstObjectByType<BobSfx>() != null) BobSfx.Play(name); }

        static void Buzz()
        {
#if UNITY_ANDROID || UNITY_IOS
            if (PlayerPrefs.GetInt("bob.vibrate", 1) == 1) Handheld.Vibrate();
#endif
        }

        static async Task WithTimeout(Task t, int ms)
        {
            if (await Task.WhenAny(t, Task.Delay(ms)) != t) throw new TimeoutException("no answer in " + ms + " ms");
            await t;
        }

        static string PartName(string id) => BobRules.Parts.TryGetValue(id, out var p) ? p.Name : id;
    }
}
