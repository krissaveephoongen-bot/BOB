// Battle of the Brooms: the single-player match from prototype/index.html, as a Unity scene.
// Flow: title (pick a hero) -> 3 rounds of [intro -> build -> test] vs a simulated rival -> result.
// The room comes from the local Nakama server (bob_make_round) and our stack is scored by it (bob_score_stack);
// without a server the same rules run offline (BobRules / BobMatch, checked against the server by unity-check/).
// This file: game state and rules flow. BobGame.World.cs: room, hero, input. BobGame.Hud.cs: the uGUI screens.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bob.Art;
using Bob.Net;
using Bob.Rules;
using UnityEngine;

namespace Bob.Game
{
    public partial class BobGame : MonoBehaviour
    {
        [Header("Art (filled by Bob > Build Game Scene)")]
        public BobArtLibrary art;

        [Header("Local Nakama server")]
        public bool useServer = true;
        [Tooltip("127.0.0.1 in the Editor. On a phone, use your PC's LAN IP (e.g. 192.168.1.20).")]
        public string host = "127.0.0.1";
        public int port = 7350;
        public string serverKey = "defaultkey";

        public const float RoundTime = 100f, FreezeTime = 8f;
        public const int Hand = BobRules.HandSolo;

        enum Phase { Title, Loading, Intro, Countdown, Build, Scoring, Test, Result, Lobby }

        sealed class LastRound
        {
            public EvalResult Ev; public int[] Stats; public ScoreResult Mine; public RivalPlay Rival; public bool Win, FromServer;
            public List<string> Stack;
        }

        Phase phase = Phase.Title;
        string charId = "sweep", matchId;
        int seed, roundNo, wins, losses, carry, rivalCarry, coins, budget, gain;
        Round R;
        readonly List<string> stack = new List<string>(), hand = new List<string>();
        float time, freeze;
        bool abUsed, shield, swapMode;
        ScoreResult preview;
        LastRound last;
        readonly List<bool> results = new List<bool>();
        bool soundOn = true, musicOn = true, connecting;

        BobServer server;
        bool online;
        string netNote = "กำลังเชื่อมต่อเซิร์ฟเวอร์…";

        bool sfxReady;

        // ---------------------------------------------------------------- lifecycle

        void Awake()
        {
            if (art == null) { art = ScriptableObject.CreateInstance<BobArtLibrary>(); Debug.LogWarning("[BobGame] no art library: run Bob > Build Game Scene"); }
            BobUi.Art = art;
            try
            {
                var c = PlayerPrefs.GetString("bob.char", "sweep"); if (BobMatch.Chars.Any(x => x.Id == c)) charId = c;
                coins = PlayerPrefs.GetInt("bob.coins", 0);
                soundOn = PlayerPrefs.GetInt("bob.sound", 1) == 1; musicOn = PlayerPrefs.GetInt("bob.music", 1) == 1;
                var h = PlayerPrefs.GetString("bob.host", ""); if (!string.IsNullOrWhiteSpace(h)) host = h.Trim();
            }
            catch (Exception) { }
            Application.targetFrameRate = 60;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            // landscape only, either way round
            Screen.autorotateToPortrait = false; Screen.autorotateToPortraitUpsideDown = false;
            Screen.autorotateToLandscapeLeft = true; Screen.autorotateToLandscapeRight = true;
            if (!Application.isEditor) Screen.orientation = ScreenOrientation.AutoRotation;
        }

        void Start()
        {
            sfxReady = FindFirstObjectByType<BobSfx>() != null;
            AudioListener.volume = soundOn ? 1 : 0;
            if (sfxReady) BobSfx.Music(musicOn);
            BuildWorld();
            BuildHud();
            RenderAll();
            Connect();
            if (AutoRequested()) StartCoroutine(AutoPlay());
        }

        async void Connect()
        {
            if (!useServer) { netNote = "ออฟไลน์: คิดคะแนนในเครื่อง"; RenderAll(); return; }
            connecting = true; online = false; netNote = "กำลังเชื่อมต่อ " + host + "…"; RenderAll();
            try
            {
                server = new BobServer("http", host, port, serverKey);
                await WithTimeout(server.SignInAsync(SystemInfo.deviceUniqueIdentifier + "-bob-game"), 5000);
                await WithTimeout(server.PingAsync(), 4000);
                online = true; netNote = "เซิร์ฟเวอร์ Nakama ออนไลน์ (" + host + ")";
            }
            catch (Exception e)
            {
                online = false; netNote = "ออฟไลน์: คิดคะแนนในเครื่อง";
                Debug.LogWarning("[BobGame] server not reachable, playing offline: " + e.Message);
            }
            connecting = false;
            RenderAll();
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape)) OnBack();
            TickInput();
            TickOnline();
            if (phase == Phase.Build && overlay == Overlay.None && !Online) // online, the server runs the clock
            {
                if (freeze > 0) freeze = Mathf.Max(0, freeze - Time.deltaTime); else time -= Time.deltaTime;
                if (time <= 0) { time = 0; if (stack.Count == 0) Toast("หมดเวลา! ส่งรถเข็นเปล่า"); Submit(); }
            }
            TickWorld();
            TickHud();
        }

        // ---------------------------------------------------------------- helpers

        static async Task<T> WithTimeout<T>(Task<T> t, int ms)
        {
            if (await Task.WhenAny(t, Task.Delay(ms)) != t) throw new TimeoutException("server did not answer in " + ms + " ms");
            return await t;
        }
        static async Task WithTimeout(Task t, int ms)
        {
            if (await Task.WhenAny(t, Task.Delay(ms)) != t) throw new TimeoutException("server did not answer in " + ms + " ms");
            await t;
        }

        void Sfx(string name) { if (sfxReady && soundOn) BobSfx.Play(name); }

        static void Buzz()
        {
#if UNITY_ANDROID || UNITY_IOS
            Handheld.Vibrate();
#endif
        }

        void SetSound(bool on) { soundOn = on; AudioListener.volume = on ? 1 : 0; PlayerPrefs.SetInt("bob.sound", on ? 1 : 0); PlayerPrefs.Save(); }
        void SetMusic(bool on) { musicOn = on; if (sfxReady) BobSfx.Music(on); PlayerPrefs.SetInt("bob.music", on ? 1 : 0); PlayerPrefs.Save(); }
        void SetHost(string h)
        {
            h = (h ?? "").Trim(); if (h.Length == 0) return;
            host = h; PlayerPrefs.SetString("bob.host", h); PlayerPrefs.Save();
            Connect();
        }

        /// <summary>Android back button / Esc: close overlay, pause during a round, else go home.</summary>
        void OnBack()
        {
            if (overlay != Overlay.None) SetOverlay(phase == Phase.Build && overlay == Overlay.Settings ? Overlay.Pause : Overlay.None);
            else if (phase == Phase.Build) SetOverlay(Overlay.Pause);
            else if (phase == Phase.Intro || phase == Phase.Test || phase == Phase.Result) ToTitle();
        }

        EvalResult Ev() => BobRules.EvalStack(stack, R?.Enemies, shield);
        int BudgetLeft => budget - BobRules.StackCost(stack);
        CharInfo Hero => BobMatch.Char(charId);

        static string JsonList(IEnumerable<string> xs) => "[" + string.Join(",", xs.Select(x => "\"" + x + "\"")) + "]";
        static string B(bool b) => b ? "true" : "false";

        void Log(string evt, string dataJson)
        {
            if (!online || server == null) return;
            _ = LogAsync(evt, dataJson);
        }
        async Task LogAsync(string evt, string dataJson)
        {
            try { await server.LogEventAsync(evt, matchId, dataJson); }
            catch (Exception e) { Debug.LogWarning("[BobGame] analytics " + evt + ": " + e.Message); }
        }

        // ---------------------------------------------------------------- match flow

        void SelectChar(string id)
        {
            charId = id;
            try { PlayerPrefs.SetString("bob.char", id); } catch (Exception) { }
            Sfx("ui_tap"); RefreshHero(); RenderAll();
        }

        void StartMatch()
        {
            if (Online) { LeaveOnline(); FindOnline(); return; } // "play again" online = find a new match
            matchId = "u" + DateTime.UtcNow.Ticks.ToString("x") + UnityEngine.Random.Range(0, 0xFFFF).ToString("x");
            seed = UnityEngine.Random.Range(1, 8388608);
            roundNo = 0; wins = 0; losses = 0; carry = 0; rivalCarry = 0; results.Clear();
            Time.timeScale = 1;
            Sfx("ui_confirm");
            Log("match_start", $"{{\"seed\":{seed},\"char\":\"{charId}\"}}");
            NextRound();
        }

        async void NextRound()
        {
            roundNo++;
            phase = Phase.Loading; RenderAll();
            Round room = null;
            if (online)
            {
                try { room = ParseRoom(await WithTimeout(server.MakeRoundAsync(seed, roundNo), 5000)); }
                catch (Exception e) { Debug.LogWarning("[BobGame] bob_make_round failed, using the offline room: " + e.Message); }
            }
            R = room ?? BobMatch.MakeRound(roundNo, seed);
            stack.Clear(); hand.Clear();
            time = RoundTime; budget = BobRules.RoundBudget + carry / 2;
            abUsed = false; shield = false; swapMode = false; freeze = 0; preview = null;
            ResetRoomForRound();
            phase = Phase.Intro; RenderAll();
        }

        void BeginBuild()
        {
            if (phase != Phase.Intro) return;
            phase = Phase.Countdown; RenderAll();
            StartCoroutine(Countdown());
        }

        System.Collections.IEnumerator Countdown()
        {
            countdownRt.gameObject.SetActive(true);
            foreach (var s in new[] { "3", "2", "1", "ลุย!" })
            {
                countdownText.text = s; Sfx(s == "ลุย!" ? "round_start" : "timer_tick");
                StartCoroutine(BobTween.Pop(countdownRt, 0.3f, 1.3f));
                yield return new WaitForSeconds(s == "ลุย!" ? 0.5f : 0.6f);
            }
            countdownRt.gameObject.SetActive(false);
            phase = Phase.Build; RenderAll();
        }

        void PickUp(string id)
        {
            if (phase != Phase.Build) return;
            if (Online) { OnlinePick(id); return; }
            if (hand.Count >= HandMax) { Toast("มือเต็มแล้ว ถือได้ " + HandMax + " ชิ้น"); Sfx("drop_invalid"); return; }
            hand.Add(id); Sfx("pickup"); PopHand(); RenderAll();
        }

        void Place(int i)
        {
            if (phase != Phase.Build || i < 0 || i >= hand.Count) return;
            if (!Online && nearTable && !nearCart) { StashOnTable(i); return; }
            if (!nearCart) { Toast("เดินไปที่รถเข็นก่อน"); Sfx("drop_invalid"); return; }
            if (Online) { OnlinePlace(i); return; }
            var id = hand[i];
            var why = BobRules.WhyCannotPlace(stack, id, BudgetLeft);
            if (why != null) { Toast(why); Sfx("drop_invalid"); return; }
            var before = Ev().Fx.Select(f => f.Text).ToList();
            stack.Add(id); hand.RemoveAt(i); preview = null;
            var after = Ev();
            Sfx("place_layer");
            OnLayerPlaced(after.Fx.Where(f => f.Src != "enemy" && !before.Contains(f.Text)).ToList());
            RenderAll();
        }

        void LiftTop()
        {
            if (phase != Phase.Build || stack.Count == 0) return;
            if (!nearCart) { Toast("เดินไปที่รถเข็นก่อน"); return; }
            if (Online) { OnlineLift(); return; }
            var id = stack[stack.Count - 1]; stack.RemoveAt(stack.Count - 1);
            if (hand.Count < HandMax) hand.Add(id); else Toast("มือเต็ม ชิ้นนี้ถูกวางคืนชั้น");
            preview = null; Sfx("lift_layer"); RenderAll();
        }

        void SwapAt(int i) // swap layer i with the one above it (Sweep's ability)
        {
            if (i < 0 || i >= stack.Count - 1) return;
            var a = stack.ToList(); (a[i], a[i + 1]) = (a[i + 1], a[i]);
            bool bad = BobRules.IsMethod(a[0]);
            for (int k = 1; k < a.Count; k++) if (BobRules.IsMethod(a[k]) && BobRules.IsMethod(a[k - 1])) bad = true;
            if (bad) { Toast("สลับแล้ววิธีใช้ไม่มีชิ้นส่วนรองรับ"); Sfx("drop_invalid"); return; }
            stack.Clear(); stack.AddRange(a);
            swapMode = false; MarkAbilityUsed(); preview = null;
            Toast("จัดทัพ! สลับชั้นแล้ว"); Sfx("ability_activate");
            OnLayerPlaced(new List<Effect>());
            RenderAll();
        }

        void MarkAbilityUsed()
        {
            if (!abUsed) Log("ability_used", $"{{\"round\":{roundNo},\"ability\":\"{Hero.Ability}\",\"char\":\"{charId}\"}}");
            abUsed = true;
        }

        void UseAbility()
        {
            if (phase != Phase.Build || abUsed) { if (Online && phase == Phase.Build) Toast("ความสามารถพิเศษยังไม่เปิดในโหมดออนไลน์"); return; }
            switch (charId)
            {
                case "sweep":
                    if (stack.Count < 2) { Toast("ต้องมีอย่างน้อย 2 ชิ้นบนรถเข็น"); return; }
                    swapMode = !swapMode; Sfx("ui_tap"); RenderAll(); return;
                case "bubble":
                    shield = true; Toast("โล่ฟอง! กันปฏิกิริยาหักล้างที่แรงที่สุด"); Sfx("bubble_pop"); SpawnFx("fx_bubble_pop", PlayerPos + Vector2.up * 0.5f); break;
                case "spray":
                    preview = BobRules.TotalScore(Ev(), R.Need, BudgetLeft, budget, time / RoundTime);
                    Toast("เล็งเป้า! กองนี้จะได้ " + preview.Total + " คะแนน"); Sfx("spray_psst"); SpawnFx("fx_target", CartTop); break;
                case "press":
                    freeze = FreezeTime; Toast("รีดเวลา! หยุดนาฬิกา 8 วินาที"); Sfx("steam_hiss"); SpawnFx("fx_steam", PlayerPos + Vector2.up * 0.5f); break;
            }
            MarkAbilityUsed(); Sfx("ability_activate"); RenderAll();
        }

        async void Submit()
        {
            if (phase != Phase.Build) return;
            if (Online) { OnlineSubmit(); return; }
            phase = Phase.Scoring; RenderAll();
            var e = Ev(); int left = BudgetLeft; double tf = Mathf.Clamp01(time / RoundTime);
            var mine = BobRules.TotalScore(e, R.Need, left, budget, tf);
            int[] stats = (int[])e.Stats.Clone();
            bool fromServer = false;
            if (online)
            {
                try
                {
                    var rep = JsonUtility.FromJson<ScoreReply>(await WithTimeout(
                        server.ScoreStackAsync(seed, roundNo, JsonList(stack), budget, tf, shield, matchId, charId), 6000));
                    mine = new ScoreResult { Pass = rep.score.pass, Test = rep.score.test, Budget = rep.score.budget, Time = rep.score.time, Total = rep.score.total };
                    stats = new[] { rep.stats.clean, rep.stats.gentle, rep.stats.speed, rep.stats.shine };
                    fromServer = true;
                    if (!stats.SequenceEqual(e.Stats)) Debug.LogWarning("[BobGame] client and server rules differ for " + JsonList(stack));
                }
                catch (Exception ex) { Debug.LogWarning("[BobGame] bob_score_stack failed, scoring offline: " + ex.Message); }
            }
            int rb = BobRules.RoundBudget + rivalCarry / 2;
            var rv = BobMatch.Rival(R, seed, rb);
            bool win = mine.Total > rv.Score.Total || (mine.Total == rv.Score.Total && left >= rb - rv.Ev.Cost);
            if (win) wins++; else losses++;
            results.Add(win);
            carry = Mathf.Max(0, left); rivalCarry = Mathf.Max(0, rb - rv.Ev.Cost);
            last = new LastRound { Ev = e, Stats = stats, Mine = mine, Rival = rv, Win = win, FromServer = fromServer, Stack = stack.ToList() };
            Log("round_result", "{" +
                $"\"round\":{roundNo},\"seed\":{seed},\"result\":\"{(win ? "win" : "lose")}\",\"stack\":{JsonList(stack)},\"my_total\":{mine.Total}," +
                $"\"rival_total\":{rv.Score.Total},\"pass\":{B(mine.Pass)},\"rival_pass\":{B(rv.Score.Pass)},\"rival_stack\":{JsonList(rv.Stack)}," +
                $"\"cost\":{e.Cost},\"budget\":{budget},\"time_left\":{Mathf.RoundToInt(time)},\"ability_used\":{B(abUsed)}," +
                $"\"enemies\":{JsonList(R.Enemies)},\"char\":\"{charId}\",\"client\":\"unity\"}}");
            OnRoundTested(win);
            Sfx(win ? "round_win" : "round_lose");
            phase = Phase.Test; RenderAll();
        }

        bool MatchOver => wins >= 2 || losses >= 2 || roundNo >= 3;

        void AfterTest()
        {
            if (Online && !MatchOver) { Toast("รอเซิร์ฟเวอร์เริ่มรอบถัดไป…"); return; }
            if (MatchOver)
            {
                gain = wins > losses ? 30 : 20; coins += gain;
                try { PlayerPrefs.SetInt("bob.coins", coins); PlayerPrefs.Save(); } catch (Exception) { }
                Log("match_end", $"{{\"wins\":{wins},\"losses\":{losses},\"rounds\":{roundNo},\"coins\":{gain},\"char\":\"{charId}\",\"mode\":\"{(Online ? "online" : "solo")}\"}}");
                Sfx("coin");
                phase = Phase.Result; RenderAll();
            }
            else NextRound();
        }

        void ToTitle() { LeaveOnline(); Time.timeScale = 1; StopAllCoroutines(); countdownRt.gameObject.SetActive(false); phase = Phase.Title; R = null; ResetRoomForRound(); Sfx("ui_cancel"); RenderAll(); }

        void RenderAll() { RenderWorld(); RenderHud(); }

        // ---------------------------------------------------------------- server JSON (JsonUtility needs plain classes)

        [Serializable] class Need { public int clean = -1, gentle = -1, speed = -1, shine = -1; }
        [Serializable] class Spec { public Need need; public string[] notes; }
        [Serializable] class RoomJson { public int round; public string[] enemies; public string[] offer; public Spec spec; }
        [Serializable] class StatsJson { public int clean, gentle, speed, shine; }
        [Serializable] class ScoreJson { public bool pass; public double test, budget, time; public int total; }
        [Serializable] class ScoreReply { public StatsJson stats; public ScoreJson score; public int cost; }

        static Round ParseRoom(string json)
        {
            var j = JsonUtility.FromJson<RoomJson>(json);
            if (j?.offer == null || j.enemies == null || j.spec?.need == null) throw new FormatException("bad room json");
            var r = new Round { Number = j.round, Enemies = j.enemies, Offer = j.offer };
            void Add(string k, int v) { if (v >= 0) r.Need[k] = v; }
            Add("clean", j.spec.need.clean); Add("gentle", j.spec.need.gentle); Add("speed", j.spec.need.speed); Add("shine", j.spec.need.shine);
            if (j.spec.notes != null) r.Notes.AddRange(j.spec.notes);
            return r;
        }
    }
}
