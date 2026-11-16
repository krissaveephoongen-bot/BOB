// Battle of the Brooms: the single-player match from prototype/index.html, as a Unity scene.
// Flow: title (pick a hero) -> 3 rounds of [intro -> build -> test] vs a simulated rival -> result.
// The rules run on the phone (BobRules / BobMatch); UGS only carries the profile, leaderboard and rooms.
// This file: game state and rules flow. BobGame.World.cs: room, hero, input. BobGame.Hud.cs: the uGUI screens.
using System;
using System.Collections.Generic;
using System.Linq;
using Bob.Art;
using Bob.Modular;
using Bob.Rules;
using TMPro;
using UnityEngine;

namespace Bob.Game
{
    public partial class BobGame : MonoBehaviour
    {
        [Header("Art (filled by Bob > Build Game Scene)")]
        public BobArtLibrary art;

        public const float RoundTime = 100f, FreezeTime = 8f;
        public const int Hand = BobRules.HandSolo;

        enum Phase { Title, Loading, Intro, Countdown, Build, Scoring, Test, Result, Splash, Login, Modes, Heroes, Shop, Chapters, Team }

        sealed class LastRound
        {
            public EvalResult Ev; public int[] Stats; public ScoreResult Mine; public RivalPlay Rival; public bool Win;
            public List<string> Stack;
        }

        Phase phase = Phase.Title;
        string charId = "sweep";
        int seed, roundNo, wins, losses, carry, rivalCarry, coins, budget, gain;
        Round R;
        readonly List<string> stack = new List<string>(), hand = new List<string>();
        TaskManager tasks;   // the round clock (Modular/TaskManager.cs)
        float time { get => tasks != null ? tasks.TimeLeft : 0f; set { if (tasks != null) tasks.TimeLeft = value; } }
        float freeze { get => tasks != null ? tasks.Freeze : 0f; set { if (tasks != null) tasks.Freeze = value; } }
        bool abUsed, shield, swapMode;
        ScoreResult preview;
        LastRound last;
        readonly List<bool> results = new List<bool>();
        bool soundOn = true, musicOn = true;

        int HandMax => Hand;

        bool sfxReady;

        // ---------------------------------------------------------------- lifecycle

        void Awake()
        {
            tasks = gameObject.AddComponent<TaskManager>(); tasks.warnAt = 15f;
            if (art == null) { art = ScriptableObject.CreateInstance<BobArtLibrary>(); Debug.LogWarning("[BobGame] no art library: run Bob > Build Game Scene"); }
            BobUi.Art = art;
            try
            {
                var c = PlayerPrefs.GetString("bob.char", "sweep"); if (BobMatch.Chars.Any(x => x.Id == c)) charId = c;
                coins = PlayerPrefs.GetInt("bob.coins", 0);
                soundOn = PlayerPrefs.GetInt("bob.sound", 1) == 1; musicOn = PlayerPrefs.GetInt("bob.music", 1) == 1;
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
            LoadProfile();
            BuildWorld();
            BuildHud();
            RenderAll();
            if (AutoRequested()) StartCoroutine(AutoPlay()); else StartCoroutine(BootFlow());
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape)) OnBack();
            TickInput();
            TickMenus(); UpdateMusic();
            if (taskMode && phase == Phase.Build) TickTasks(Time.deltaTime);
            else if (phase == Phase.Build && overlay == Overlay.None)
            {
                tasks.Tick(Time.deltaTime);
                if (tasks.TimeUp) { if (stack.Count == 0) Toast("หมดเวลา! ส่งรถเข็นเปล่า"); Submit(); }
            }
            TickWorld();
            TickHud();
        }

        // ---------------------------------------------------------------- music per screen

        string musicNow;
        /// <summary>menu music out of the match, game music while building, the tense track for the last seconds, result music after.</summary>
        void UpdateMusic()
        {
            if (!sfxReady) return;
            string want;
            switch (phase)
            {
                case Phase.Intro: case Phase.Countdown: case Phase.Scoring: want = "game"; break;
                case Phase.Build: want = tasks != null && tasks.Warning ? "tense" : "game"; break;
                case Phase.Test: case Phase.Result: want = "result"; break;
                default: want = "menu"; break;
            }
            if (want != musicNow) { musicNow = want; BobSfx.Track(want); }
        }

        // ---------------------------------------------------------------- helpers

        void Sfx(string name) { if (sfxReady && soundOn) BobSfx.Play(name); }

        static void Buzz()
        {
#if UNITY_ANDROID || UNITY_IOS
            Handheld.Vibrate();
#endif
        }

        void SetSound(bool on) { soundOn = on; AudioListener.volume = on ? 1 : 0; PlayerPrefs.SetInt("bob.sound", on ? 1 : 0); PlayerPrefs.Save(); }
        void SetMusic(bool on) { musicOn = on; if (sfxReady) BobSfx.Music(on); PlayerPrefs.SetInt("bob.music", on ? 1 : 0); PlayerPrefs.Save(); }

        /// <summary>Android back button / Esc: close overlay, pause during a round, else go home.</summary>
        void OnBack()
        {
            if (taskMode) { OnTaskBack(); return; }
            if (overlay != Overlay.None) SetOverlay(phase == Phase.Build && overlay == Overlay.Settings ? Overlay.Pause : Overlay.None);
            else if (phase == Phase.Build) SetOverlay(Overlay.Pause);
            else if (phase == Phase.Intro || phase == Phase.Test || phase == Phase.Result) ToTitle();
            else if (phase == Phase.Team) { phase = Phase.Modes; RenderAll(); }
            else if (phase == Phase.Modes || phase == Phase.Heroes || phase == Phase.Shop || phase == Phase.Chapters) GoHome();
        }

        EvalResult Ev() => BobRules.EvalStack(stack, R?.Enemies, shield);
        int BudgetLeft => budget - BobRules.StackCost(stack);
        CharInfo Hero => BobMatch.Char(charId);

        // ---------------------------------------------------------------- match flow

        void SelectChar(string id)
        {
            charId = id;
            try { PlayerPrefs.SetString("bob.char", id); } catch (Exception) { }
            Sfx("ui_tap"); RefreshHero(); RenderAll();
        }

        void StartMatch()
        {
            seed = UnityEngine.Random.Range(1, 8388608);
            roundNo = 0; wins = 0; losses = 0; carry = 0; rivalCarry = 0; results.Clear();
            abUsed = false; // 4.6: one hero ability per match, not per round
            Time.timeScale = 1;
            Sfx("ui_confirm");
            NextRound();
        }

        void NextRound()
        {
            roundNo++;
            phase = Phase.Loading; RenderAll();
            R = BobMatch.MakeRound(roundNo, seed);
            stack.Clear(); hand.Clear();
            tasks.Begin(R.Need, RoundTime); budget = BobRules.RoundBudget + carry / 2;
            shield = false; swapMode = false; freeze = 0; preview = null;
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
            if (hand.Count >= HandMax) { Toast("มือเต็มแล้ว ถือได้ " + HandMax + " ชิ้น"); Sfx("drop_invalid"); return; }
            hand.Add(id); Sfx("pickup"); PopHand(); RenderAll();
        }

        void Place(int i)
        {
            if (phase != Phase.Build || i < 0 || i >= hand.Count) return;
            if (nearTable && !nearCart) { StashOnTable(i); return; }
            if (!nearCart) { Toast("เดินไปที่รถเข็นก่อน"); Sfx("drop_invalid"); return; }
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
            abUsed = true;
        }

        void UseAbility()
        {
            if (phase != Phase.Build || abUsed) return;
            switch (charId)
            {
                case "sweep":
                    if (stack.Count < 2) { Toast("ต้องมีอย่างน้อย 2 ชิ้นบนรถเข็น"); return; }
                    swapMode = !swapMode; Sfx("ui_tap"); RenderAll(); return;
                case "bubble":
                    shield = true; Toast("โล่ฟอง! กันปฏิกิริยาหักล้างที่แรงที่สุด"); Sfx("bubble_pop"); SpawnFx("fx_bubble_pop", Raise(PlayerPos, 0.5f)); break;
                case "spray":
                    preview = BobRules.TotalScore(Ev(), R.Need, BudgetLeft, budget, time / RoundTime);
                    Toast("เล็งเป้า! กองนี้จะได้ " + preview.Total + " คะแนน"); Sfx("spray_psst"); SpawnFx("fx_target", CartTop); break;
                case "press":
                    tasks.FreezeFor(FreezeTime); Toast("รีดเวลา! หยุดนาฬิกา 8 วินาที"); Sfx("steam_hiss"); SpawnFx("fx_steam", Raise(PlayerPos, 0.5f)); break;
            }
            MarkAbilityUsed(); Sfx("ability_activate"); RenderAll();
        }

        void Submit()
        {
            if (phase != Phase.Build) return;
            phase = Phase.Scoring; RenderAll();
            var e = Ev(); int left = BudgetLeft; double tf = Mathf.Clamp01(time / RoundTime);
            var mine = BobRules.TotalScore(e, R.Need, left, budget, tf);
            int[] stats = (int[])e.Stats.Clone();
            int rb = BobRules.RoundBudget + rivalCarry / 2;
            var rv = BobMatch.Rival(R, seed, rb);
            bool win = mine.Total > rv.Score.Total || (mine.Total == rv.Score.Total && left >= rb - rv.Ev.Cost);
            if (win) wins++; else losses++;
            results.Add(win);
            carry = Mathf.Max(0, left); rivalCarry = Mathf.Max(0, rb - rv.Ev.Cost);
            last = new LastRound { Ev = e, Stats = stats, Mine = mine, Rival = rv, Win = win, Stack = stack.ToList() };
            OnRoundTested(win);
            Sfx(win ? "round_win" : "round_lose");
            phase = Phase.Test; RenderAll();
        }

        bool MatchOver => wins >= 2 || losses >= 2 || roundNo >= 3;

        void AfterTest()
        {
            if (MatchOver)
            {
                gain = wins > losses ? 30 : 20; coins += gain;
                xpGain = 60 + wins * 40; xp += xpGain;
                SaveProfile();
                Sfx("coin");
                phase = Phase.Result; RenderAll();
            }
            else NextRound();
        }

        void ToTitle() { Time.timeScale = 1; StopAllCoroutines(); countdownRt.gameObject.SetActive(false); phase = Phase.Title; R = null; ResetRoomForRound(); Sfx("ui_cancel"); RenderAll(); }

        void RenderAll() { RenderWorld(); RenderHud(); }
    }
}
