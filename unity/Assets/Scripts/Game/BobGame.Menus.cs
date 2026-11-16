// Battle of the Brooms: the screens around the match (UX spec section 4): splash, Home / lobby (S10),
// mode select (S11), hero select with the 15 s timer (S16), shop with a purchase confirm (S30 + P02).
// Everything uses the real art in the project (Leonardo menu icons, room pictures, hero portraits); where the spec needs
// a picture that does not exist yet (wardrobe, friends, recipe book icons) the button is simply left out and listed in
// art/docs/13_Placeholder_Inventory.md.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Bob.Art;
using Bob.Rules;
using Bob.Ugs;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Bob.Game
{
    public partial class BobGame
    {
        // ---------------------------------------------------------------- profile (device-local)

        string mode = "solo";
        int xp, gems, xpGain;
        int band = 1; // difficulty band of the selected chapter: 1 = chapters 1-2, 2 = 3-4, 3 = 5-6
        readonly HashSet<string> owned = new HashSet<string>();
        float heroTimer; bool heroTimerOn; TMP_Text heroTimerText;
        Image pendingShopRoot;

        int Level => BobChapters.LevelFromXp(xp);
        static readonly string[] HomeBackdrops = { "lobby_rental_room", "lobby_condo", "lobby_coffee_shop", "lobby_kitchen" };

        sealed class ModeInfo
        {
            public string Id, Name, Players, Length, Lock, Scene, Blurb;
            /// <summary>Players per side the round list is sized for; must match the scene's own baked value.</summary>
            public int ListPlayers;
            /// <summary>Story = three rounds, best of three; every other mode is one timed round.</summary>
            public bool Series;
            /// <summary>Team: the screen offers 2v2 / 3v3 and fills the empty slots with offline teammates.</summary>
            public bool Team;
            public Color Color;
        }
        // S11 (docs 11 section 4): the modes whose scene actually exists and loads. Party/Community stay locked with the
        // reason on the card; Story is the chapter ladder, the other three are one timed round on their own map
        // (docs 24 section 13: 3 / 4 / 6 stations per side).
        static readonly ModeInfo[] Modes =
        {
            new ModeInfo { Id = "story",     Name = "Story",     Players = "1 คน",            Length = "3 รอบ",   Scene = "TaskRace",      ListPlayers = 1, Series = true,
                           Blurb = "บทที่เลือก · 3 รอบ", Color = new Color(0.98f, 0.62f, 0.28f) },
            new ModeInfo { Id = "solo",      Name = "Solo",      Players = "1 คน",            Length = "2 นาที",  Scene = "TaskRace",      ListPlayers = 1,
                           Blurb = "เกาะเล็ก 3 สถานี/ฝั่ง", Color = new Color(0.40f, 0.71f, 0.95f) },
            new ModeInfo { Id = "duo",       Name = "Duo",       Players = "1 พบ 1",          Length = "2 นาที",  Scene = "TaskRace_Duo",  ListPlayers = 2,
                           Blurb = "เกาะกลาง 4 สถานี/ฝั่ง", Color = new Color(0.93f, 0.28f, 0.38f) },
            new ModeInfo { Id = "team",      Name = "Team",      Players = "2 พบ 2 · 3 พบ 3", Length = "2 นาที",  Scene = "TaskRace_Team", ListPlayers = 3, Team = true,
                           Blurb = "เกาะใหญ่ 6 สถานี/ฝั่ง", Color = new Color(0.24f, 0.72f, 0.52f) },
            new ModeInfo { Id = "party",     Name = "Party",     Players = "3-7 คน",         Length = "8 นาที",  Lock = "เร็วๆ นี้: ต้องมีห้องออนไลน์", Color = new Color(0.58f, 0.46f, 0.88f) },
            new ModeInfo { Id = "community", Name = "Community", Players = "10-15 คน",       Length = "10 นาที", Lock = "เร็วๆ นี้: เปิดเป็นรอบเวลา",   Color = new Color(0.55f, 0.58f, 0.66f) },
        };
        ModeInfo CurrentMode => Modes.FirstOrDefault(m => m.Id == mode) ?? Modes[0];

        // Chapters (docs 24 §15.3 P0): the six ChapterDefinition assets in Resources/Chapters, the pick saved on the
        // device, and the best score per difficulty band. Home shows the pick, S13 changes it.
        ChapterDefinition Chapter => BobChapters.Selected(Level);
        int TaskBest(int b) { try { return PlayerPrefs.GetInt("bob.taskbest." + b, 0); } catch (Exception) { return 0; } }
        void SaveTaskBest(int b, int score)
        {
            try { if (score > TaskBest(b)) { PlayerPrefs.SetInt("bob.taskbest." + b, score); PlayerPrefs.Save(); } } catch (Exception) { }
        }

        static readonly (string id, string name, string sprite, int price)[] ShopItems =
        {
            ("broom",  "ไม้กวาดฟาง",     "menu_equip_broom",  120),
            ("mop",    "ไม้ถูพื้นนุ่ม",   "menu_equip_mop",    160),
            ("sponge", "ฟองน้ำยิ้ม",      "menu_equip_sponge", 100),
            ("spray",  "ขวดสเปรย์ชมพู",  "menu_equip_spray",  140),
            ("hat",    "หมวกถังน้ำ",      "menu_equip_hat",    200),
        };

        /// <summary>4.6: where an owned item actually shows up, spelled out before the player spends coins.</summary>
        static string ItemUse(string id) => id switch
        {
            "broom" => "เปลี่ยนไม้กวาดในมินิเกมกวาดฝุ่น",
            "sponge" => "เปลี่ยนฟองน้ำในมินิเกมเช็ดกระจก",
            "spray" => "เปลี่ยนขวดสเปรย์ในมินิเกมเช็ดกระจก",
            "mop" => "คอลเลกชันสำหรับห้องของฉัน",
            _ => "หมวกของตัวละครบนหน้า Home",
        };

        void LoadProfile()
        {
            try
            {
                mode = PlayerPrefs.GetString("bob.mode", "solo"); if (Modes.All(m => m.Id != mode)) mode = "solo";
                xp = PlayerPrefs.GetInt("bob.xp", 0); gems = PlayerPrefs.GetInt("bob.gems", 0);
                band = Mathf.Clamp(PlayerPrefs.GetInt("bob.band", 1), 1, 3);
                band = BobChapters.Selected(Level).Band; // the chapter is the truth; a stale prefs band follows it
                foreach (var s in PlayerPrefs.GetString("bob.owned", "").Split(',')) if (s.Length > 0) owned.Add(s);
            }
            catch (Exception) { }
        }

        void SaveProfile()
        {
            try
            {
                PlayerPrefs.SetString("bob.mode", mode); PlayerPrefs.SetInt("bob.xp", xp); PlayerPrefs.SetInt("bob.gems", gems);
                PlayerPrefs.SetInt("bob.band", band);
                PlayerPrefs.SetInt("bob.coins", coins); PlayerPrefs.SetString("bob.owned", string.Join(",", owned)); PlayerPrefs.Save();
            }
            catch (Exception) { }
        }

        // ---------------------------------------------------------------- boot flow: splash -> (login once) -> home

        IEnumerator BootFlow()
        {
            phase = Phase.Splash; RenderAll();
            yield return new WaitForSecondsRealtime(1.6f);
            bool guest = false; try { guest = PlayerPrefs.GetInt("bob.guest", 0) == 1; } catch (Exception) { }
            phase = guest ? Phase.Title : Phase.Login; RenderAll();
        }

        void Splash()
        {
            ScreenBackdrop("lobby_rental_room");
            var logo = BobUi.Img(screenRoot, "menu_logo", 360); BobUi.Pin(logo.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 40), new Vector2(360, 360));
            StartCoroutine(BobTween.Pop(logo.transform, 0.5f, 1.15f));
            var t = H1(screenRoot, "สมรภูมิปัดกวาด", 64, BobUi.Surface); BobUi.OutlineText(t, BobUi.Ink, 0.3f);
            BobUi.Pin(t.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -210), new Vector2(1100, 100));
            Sfx("broom_sweep");
        }

        // ---------------------------------------------------------------- S02 login (UGS anonymous guest)

        // The screen owns the sign-in (docs 11 S02, docs 24 section 15.21): pressing เล่นเลย asks Unity Gaming Services
        // for an anonymous player, and a failed network must never lock the offline game away — the same screen offers
        // an offline entry so solo story play always works.
        bool signingIn, signInFailed;
        string signInNote;

        void Login()
        {
            ScreenBackdrop("lobby_condo");
            var panel = Card(screenRoot, "LoginPanel", BobUi.Bg);
            BobUi.Pin(panel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(780, 0));
            VStack(panel, 54, 46, 20);
            panel.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var logo = BobUi.Img(panel, "menu_logo", 190); BobUi.Size(logo, 190, 190, 0, 190);
            BobUi.Label(panel, "ยินดีต้อนรับนักปัดกวาด", 46, BobUi.Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
            BobUi.Label(panel, "เข้าแบบผู้เยี่ยมชม ไม่ต้องสมัคร · ความคืบหน้าเก็บไว้ในเครื่องนี้", 26, BobUi.Muted, TextAnchor.MiddleCenter);

            var primary = BobUi.Button(panel, signingIn ? "กำลังเชื่อมต่อ…" : signInFailed ? "ลองอีกครั้ง" : "เล่นเลย",
                BobUi.Sun, PressGuest, 44);
            BobUi.Size(primary, 148, 168);
            primary.GetComponentInChildren<TMP_Text>().fontStyle = FontStyles.Bold;
            primary.interactable = !signingIn;

            if (signInFailed)
            {
                // The services are down (or the device is offline): the offline game is still complete on its own.
                var offline = BobUi.Button(panel, "เล่นแบบออฟไลน์", BobUi.Surface, PressOffline, 32, BobUi.Ink);
                BobUi.Size(offline, 144, 144);
            }

            if (!string.IsNullOrEmpty(signInNote))
                BobUi.Label(panel, signInNote, 24, signInFailed ? BobUi.Bad : BobUi.Muted, TextAnchor.MiddleCenter);

            BobUi.Label(panel, "บัญชีจริง (Google / Apple) จะมาในเวอร์ชันถัดไป", 22, BobUi.Muted, TextAnchor.MiddleCenter);
        }

        void PressGuest()
        {
            if (signingIn) return;
            Sfx("ui_confirm");
            StartCoroutine(GuestSignIn());
        }

        IEnumerator GuestSignIn()
        {
            signingIn = true; signInFailed = false; signInNote = "กำลังเชื่อมต่อเซิร์ฟเวอร์…"; RenderAll();

            var ugs = UgsBootstrapManager.Instance;
            if (ugs == null)
            {
                var host = new GameObject("UGS");
                ugs = host.AddComponent<UgsBootstrapManager>();
                ugs.signInOnAwake = false; // this screen asks for the sign-in; the manager's Start must not race it
            }

            // A scene-placed manager may already be signing in (signInOnAwake); give it a moment instead of asking twice.
            float settle = Time.realtimeSinceStartup + 3f;
            while (!ugs.IsReady && string.IsNullOrEmpty(ugs.LastError) && Time.realtimeSinceStartup < settle) yield return null;

            if (!ugs.IsReady)
            {
                var kick = ugs.KickoffAsync();
                float deadline = Time.realtimeSinceStartup + 10f;
                while (!kick.IsCompleted && Time.realtimeSinceStartup < deadline) yield return null;
                if (!kick.IsCompleted) Debug.LogWarning("[Login] UGS sign-in still running after 10 s");
            }

            signingIn = false;
            if (ugs.IsReady && !string.IsNullOrEmpty(ugs.PlayerId)) { EnterAsGuest(ugs.PlayerId); yield break; }

            signInFailed = true;
            signInNote = string.IsNullOrEmpty(ugs.LastError)
                ? "เชื่อมต่อเซิร์ฟเวอร์ไม่สำเร็จ · เล่นออฟไลน์ได้เลย"
                : "เชื่อมต่อไม่สำเร็จ (" + Shorten(ugs.LastError) + ") · เล่นออฟไลน์ได้เลย";
            Debug.LogWarning("[Login] guest sign-in failed: " + (ugs.LastError ?? "timeout"));
            RenderAll();
        }

        static string Shorten(string s) => string.IsNullOrEmpty(s) ? "" : s.Length <= 48 ? s : s.Substring(0, 45) + "…";

        void EnterAsGuest(string playerId)
        {
            try
            {
                PlayerPrefs.SetInt("bob.guest", 1);
                PlayerPrefs.SetString("bob.playerId", playerId ?? "");
                PlayerPrefs.Save();
            }
            catch (Exception) { }
            signInFailed = false; signInNote = null;
            Debug.Log("[Login] guest player " + playerId);
            Sfx("ui_confirm");
            phase = Phase.Title; RenderAll();
        }

        /// <summary>Services unreachable: enter the offline game, and keep the guest flag so the screen is not a gate.</summary>
        void PressOffline()
        {
            try { PlayerPrefs.SetInt("bob.guest", 1); PlayerPrefs.Save(); } catch (Exception) { }
            signInFailed = false; signInNote = null;
            Sfx("ui_confirm");
            phase = Phase.Title; RenderAll();
        }

        // ---------------------------------------------------------------- shared pieces

        /// <summary>Top bar of the out-of-match screens: back or profile on the left, currency on the right.</summary>
        void TopBar(Transform p, string title, bool back, Action onBack = null)
        {
            if (back)
            {
                var b = BobUi.Button(p, "", BobUi.Surface, () => { Sfx("ui_cancel"); if (onBack != null) onBack(); else GoHome(); }, 24);
                BobUi.Pin((RectTransform)b.transform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(24, -24), new Vector2(112, 96));
                b.GetComponentInChildren<TMP_Text>().text = "";
                var x = BobUi.Img(b.transform, "menu_close", 70); BobUi.Pin(x.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(70, 70));
                var h = H1(p, title, 56, BobUi.Surface); BobUi.OutlineText(h, BobUi.Ink, 0.3f);
                BobUi.Pin(h.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(150, -24), new Vector2(900, 96)); h.alignment = TextAlignmentOptions.Left;
            }
            Chip(p, "item_coin", coins.ToString(), new Vector2(-24 - (back ? 0 : 130), -24));
            Chip(p, "item_gem", gems.ToString(), new Vector2(-24 - 250 - (back ? 0 : 130), -24));
        }

        void Chip(Transform p, string icon, string value, Vector2 pos)
        {
            var c = Card(p, "Chip", BobUi.Surface, 1f); BobUi.Pin(c, Vector2.one, Vector2.one, pos, new Vector2(230, 96));
            BobUi.Pin(BobUi.Img(c, icon, 64).rectTransform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(16, 0), new Vector2(64, 64));
            var t = BobUi.Label(c, value, 40, BobUi.Ink, TextAnchor.MiddleRight, FontStyle.Bold); BobUi.Stretch(t.rectTransform, 90, 0, 22, 0);
        }

        /// <summary>Width of one card so `n` cards plus gaps fit the screen (narrow tablets included), at most `max`.</summary>
        float CardW(int n, float gap, float max, float margin = 100f) => Mathf.Min(max, (canvasRt.rect.width - margin - (n - 1) * gap) / n);

        void GoHome() { Time.timeScale = 1; heroTimerOn = false; phase = Phase.Title; RenderAll(); }

        // ---------------------------------------------------------------- S10 Home

        void Home()
        {
            int idx = (DateTime.Now.Hour / 6) % HomeBackdrops.Length; // the picture changes with the time of day
            ScreenBackdrop(HomeBackdrops[idx]);
            var S = screenRoot;
            // profile chip (top left)
            var prof = Card(S, "Profile", BobUi.Surface, 1f); BobUi.Pin(prof, new Vector2(0, 1), new Vector2(0, 1), new Vector2(24, -24), new Vector2(470, 110));
            var pf = BobUi.Img(prof, charId + "_portrait", 86); BobUi.Pin(pf.rectTransform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(12, 0), new Vector2(86, 86));
            var pn = BobUi.Label(prof, $"{Hero.Name}  ระดับ {Level}", 34, BobUi.Ink, TextAnchor.MiddleLeft, FontStyle.Bold); BobUi.Pin(pn.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(112, -10), new Vector2(340, 48));
            var bar = BobUi.Rect("Xp", prof); BobUi.Pin(bar, new Vector2(0, 0), new Vector2(0, 0), new Vector2(112, 22), new Vector2(330, 22)); bar.gameObject.AddComponent<Image>().color = BobUi.Hex("#E9DDB8");
            var fill = BobUi.Rect("Fill", bar); fill.gameObject.AddComponent<Image>().color = BobUi.Teal;
            fill.anchorMin = Vector2.zero; fill.anchorMax = new Vector2(Mathf.Clamp01((xp % 200) / 200f), 1); fill.offsetMin = fill.offsetMax = Vector2.zero;
            // currency + settings (top right)
            var gear = BobUi.Button(S, "", BobUi.Surface, () => SetOverlay(Overlay.Settings), 24);
            BobUi.Pin((RectTransform)gear.transform, Vector2.one, Vector2.one, new Vector2(-24, -24), new Vector2(112, 96)); gear.GetComponentInChildren<TMP_Text>().text = "";
            BobUi.Pin(BobUi.Img(gear.transform, "menu_settings", 70).rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(70, 70));
            Chip(S, "item_coin", coins.ToString(), new Vector2(-150, -24));
            Chip(S, "item_gem", gems.ToString(), new Vector2(-150 - 244, -24));

            // hero on a stage (tap = choose hero)
            var stage = BobUi.Disc(S, new Color(1, 1, 1, 0.28f), 440, "Stage"); BobUi.Pin(stage.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 0), new Vector2(440, 440));
            var hero = BobUi.Rect("Hero", S); BobUi.Pin(hero, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 30), new Vector2(440, 440));
            var himg = hero.gameObject.AddComponent<Image>(); himg.preserveAspect = true;
            var idle = art.Find(charId + "_idle");
            if (idle != null) { var an = hero.gameObject.AddComponent<BobSpriteAnimator>(); an.Play(idle.frames, idle.fps, true); } else himg.color = BobUi.Hex(Hero.Color);
            var hb = hero.gameObject.AddComponent<Button>(); hb.onClick.AddListener(() => { Sfx("ui_tap"); OpenHeroSelect(false); });
            var hn = H1(S, Hero.Name, 60, BobUi.Surface); BobUi.OutlineText(hn, BobUi.Ink, 0.3f);
            BobUi.Pin(hn.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -225), new Vector2(700, 80));
            var hs = BobUi.Label(S, "แตะตัวละครเพื่อเปลี่ยน · ถนัด" + SpecialtyName(charId) + " +25% · " + Hero.Ability, 24, BobUi.Surface, TextAnchor.MiddleCenter); BobUi.OutlineText(hs, BobUi.Ink, 0.25f);
            BobUi.Pin(hs.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -280), new Vector2(1000, 44));

            // Dock (bottom left): only buttons that open a screen that works. A4 removed "บ้าน", which only raised a
            // "เร็วๆ นี้" toast, so the dock is one square again (the layout group keeps the single child at 190).
            var dock = BobUi.Pin(BobUi.Rect("Dock", S), Vector2.zero, Vector2.zero, new Vector2(30, 30), new Vector2(190, 190));
            var dh = dock.gameObject.AddComponent<HorizontalLayoutGroup>(); dh.spacing = 14; dh.childControlWidth = dh.childControlHeight = true;
            DockButton(dock, "menu_shop", "ร้านค้า", () => { Phase p = Phase.Shop; phase = p; Sfx("ui_confirm"); RenderAll(); });

            // Task Race is the only match (docs 24): the chip above PLAY is the chapter the player picked in S13, tap
            // opens the stage select. It carries the values the TaskRace scene applies (difficulty + clock), so what
            // this card says is exactly what the match runs.
            var chapter = Chapter;
            var mb = BobUi.Button(S, "", BobUi.Hex("#FFF0C2"), () => { Sfx("ui_tap"); phase = Phase.Chapters; RenderAll(); }, 22);
            BobUi.Pin((RectTransform)mb.transform, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-590, 40), new Vector2(300, 190));
            mb.GetComponentInChildren<TMP_Text>().text = "";
            // B6 (docs 13): the stage card art fills the chip, and a warm veil sits over it so the Thai labels stay
            // readable. With no art the chip keeps its flat cream fill and the veil is simply the fill.
            var stageArt = BobUi.Rect("StageArt", mb.transform); BobUi.Stretch(stageArt);
            var stageImg = stageArt.gameObject.AddComponent<Image>();
            stageImg.sprite = BobUi.Art != null && BobUi.Art.Has("stage_card") ? BobUi.Art.Get("stage_card") : null;
            stageImg.preserveAspect = false; stageImg.raycastTarget = false;
            if (stageImg.sprite == null) stageImg.enabled = false;
            var veil = BobUi.Rect("Veil", mb.transform); BobUi.Stretch(veil);
            veil.gameObject.AddComponent<Image>().color = new Color(1f, 0.988f, 0.94f, 0.66f);
            BobUi.Pin(BobUi.Img(mb.transform, chapter.Backdrop, 62).rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(10, -10), new Vector2(74, 62));
            BobUi.Pin(BobUi.Label(mb.transform, $"บท {chapter.Order}", 34, BobUi.Ink, TextAnchor.MiddleLeft, FontStyle.Bold).rectTransform,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(96f, -18f), new Vector2(190f, 46f));
            BobUi.Pin(BobUi.Label(mb.transform, chapter.Title, 26, BobUi.Ink, TextAnchor.MiddleCenter, FontStyle.Bold).rectTransform,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -78f), new Vector2(280f, 40f));
            BobUi.Pin(BobUi.Label(mb.transform, $"D{chapter.Difficulty} · 3 รอบ · ดีสุด {Bob.TaskRace.BobTaskRaceProfile.BestPoints}", 22, BobUi.Muted, TextAnchor.MiddleCenter).rectTransform,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -120f), new Vector2(280f, 34f));
            BobUi.Pin(BobUi.Label(mb.transform, "แตะเพื่อเลือกด่าน", 20, BobUi.Teal, TextAnchor.MiddleCenter).rectTransform,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -154f), new Vector2(280f, 30f));
            var play = BobUi.Button(S, "เล่น", BobUi.Sun, PressPlay, 76);
            BobUi.Pin((RectTransform)play.transform, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-40, 30), new Vector2(520, 200));
            play.GetComponentInChildren<TMP_Text>().fontStyle = FontStyles.Bold;
            StartCoroutine(Breathe(play.transform));

            // S11 entry (docs 11 S11): Solo / Duo / Team load their own map at runtime, so the way in is on the lobby and
            // not the editor any more. Story stays behind the เล่น button above (the chapter picked on the card).
            var modes = BobUi.Button(S, "โหมด · Solo / Duo / Team", BobUi.Surface, () =>
            {
                Sfx("ui_tap");
                phase = Phase.Modes; RenderAll();
            }, 28);
            BobUi.Pin((RectTransform)modes.transform, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-40, 246), new Vector2(520, 96));
        }

        /// <summary>One square dock button: the icon sits over the label in the bottom thumb zone. Only screens that work get one (A4).</summary>
        void DockButton(Transform dock, string icon, string label, Action onClick)
        {
            var b = BobUi.Button(dock, "", BobUi.Surface, () => onClick(), 24);
            var t = b.GetComponentInChildren<TMP_Text>(); t.text = label; t.fontSize = 24; t.alignment = TextAlignmentOptions.Bottom; BobUi.Stretch(t.rectTransform, 4, 8, 4, 6);
            var ic = BobUi.Img(b.transform, icon, 100); BobUi.Pin(ic.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -16), new Vector2(100, 100));
        }

        void PressPlay()
        {
            Sfx("ui_confirm");
            // The lobby is only the shell: the match lives in its own scene (docs 24), so a match can re-enter the
            // lobby with fresh player prefs (coins and XP paid by the result overlay). Home's play and S13's start are
            // the story chapter: three rounds on the solo map, handed to the scene through MatchRequest.
            MatchRequest.Story();
            mode = "story"; SaveProfile();
            LoadMatchScene("TaskRace");
        }

        /// <summary>Open a match scene, or explain on screen why this build cannot (the scene is not in Build Settings).</summary>
        void LoadMatchScene(string scene)
        {
            if (Application.CanStreamedLevelBeLoaded(scene)) { SceneManager.LoadScene(scene); return; }
            Debug.LogWarning("[BobGame] " + scene + " is not in Build Settings: no match to start.");
            Toast("ยังไม่มีฉากแข่งขัน (" + scene + ")");
        }

        // ---------------------------------------------------------------- S13 chapter select (docs 24 section 15.3 P0)

        /// <summary>
        /// Six cards straight from Resources/Chapters: lock level, place, difficulty and clock, plus the best score of
        /// the band. Picking one saves it; PLAY in Home loads the TaskRace scene, which reads the same pick.
        /// </summary>
        void ChapterSelect()
        {
            var selected = Chapter;
            ScreenBackdrop(selected.Backdrop);
            MenuBackgroundVideo.Attach(screenRoot);
            TopBar(screenRoot, "เลือกด่าน", true);

            var all = BobChapters.All;
            float gap = 20f, cw = CardW(3, gap, 560), ch = 330f;
            for (int i = 0; i < all.Count; i++)
            {
                var chapter = all[i];
                var stageCfg = BobStages.Get(chapter.Order);
                bool unlocked = BobChapters.Unlocked(chapter, Level), isSel = chapter.Order == selected.Order;
                int col = i % 3, row = i / 3;
                var pos = new Vector2((col - 1) * (cw + gap), (row == 0 ? 1f : -1f) * (ch + gap) * 0.5f + 25f);

                var card = BobUi.Button(screenRoot, "", isSel ? BobUi.Hex("#FFF0C2") : BobUi.Surface, () => PickChapter(chapter), 22);
                BobUi.Pin((RectTransform)card.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), pos, new Vector2(cw, ch));
                card.GetComponentInChildren<TMP_Text>().text = "";

                var thumb = BobUi.Img(card.transform, chapter.Backdrop, 140);
                BobUi.Pin(thumb.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -14), new Vector2(cw - 28, 140));
                if (!unlocked) thumb.color = new Color(1, 1, 1, 0.35f);

                // A3: the chapter's best star count sits on the picture, bottom-right, so it never reflows the card.
                // Sprites, not ★: neither Thai font has the glyph.
                int best = BobChapters.Stars(chapter.Order);
                for (int s = 0; s < 3; s++)
                {
                    var star = BobUi.Img(card.transform, s < best ? "icon_star" : "icon_star_empty", 28);
                    BobUi.Pin(star.rectTransform, new Vector2(1, 1), new Vector2(1, 1),
                        new Vector2(-16f - (2 - s) * 32f, -112f), new Vector2(28, 28));
                }

                var badge = BobUi.Disc(card.transform, unlocked ? BobUi.Sun : BobUi.Muted, 70, "Badge");
                BobUi.Pin((RectTransform)badge.transform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(18, -18), new Vector2(70, 70));
                var bn = BobUi.Label(badge.transform, chapter.Order.ToString(), 38, unlocked ? BobUi.Ink : Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
                BobUi.Stretch(bn.rectTransform);

                BobUi.Pin(BobUi.Label(card.transform, chapter.Title, 34, unlocked ? BobUi.Ink : BobUi.Muted, TextAnchor.MiddleCenter, FontStyle.Bold).rectTransform,
                    new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -164), new Vector2(cw - 24, 44));
                BobUi.Pin(BobUi.Label(card.transform, $"{chapter.Place} · ดีสุด {Bob.TaskRace.BobTaskRaceProfile.BestPoints}", 20, BobUi.Muted, TextAnchor.MiddleCenter).rectTransform,
                    new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -202), new Vector2(cw - 24, 32));
                bool levelOk = Level >= chapter.UnlockLevel;
                BobUi.Pin(BobUi.Label(card.transform,
                        unlocked ? $"{chapter.Tag} · {stageCfg.Tag}"
                        : levelOk ? "ล็อก · ดาวบทก่อนไม่ครบ"
                        : $"ล็อก LV {chapter.UnlockLevel}",
                    22, unlocked ? BobUi.Teal : BobUi.Bad, TextAnchor.MiddleCenter, FontStyle.Bold).rectTransform,
                    new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -236), new Vector2(cw - 24, 32));
                BobUi.Pin(BobUi.Label(card.transform, string.Join(" · ", chapter.RuleCards), 18, BobUi.Muted, TextAnchor.MiddleCenter).rectTransform,
                    new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -270), new Vector2(cw - 24, 46));

                if (isSel) BobUi.Pin(BobUi.Img(card.transform, "icon_check", 42, BobUi.Good).rectTransform, Vector2.one, Vector2.one, new Vector2(-16, -16), new Vector2(42, 42));
                if (unlocked) continue;
                var veil = BobUi.Rect("Lock", card.transform); BobUi.Stretch(veil);
                veil.gameObject.AddComponent<Image>().color = new Color(0.10f, 0.11f, 0.17f, 0.42f);
                string need = levelOk
                    ? $"ต้องมีดาวบท {(chapter.Order - 1)} ครบ {BobChapters.StarsToOpen} ดวง"
                    : $"ต้องมี LV {chapter.UnlockLevel}";
                BobUi.Stretch(BobUi.Label(veil, need, 30, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold).rectTransform);
            }

            var note = BobUi.Label(screenRoot, "ด่านกำหนดความยาก (D) นาฬิกา และเวลาสะพาน · กฎพิเศษของด่านบังคับใช้แล้ว (ผิวบอบบาง คราบสองรอบ ชิ้นส่วนสุ่ม โบนัสปิดด่าน)", 22, BobUi.Surface, TextAnchor.MiddleLeft);
            BobUi.OutlineText(note, BobUi.Ink, 0.25f);
            BobUi.Pin(note.rectTransform, Vector2.zero, Vector2.zero, new Vector2(40, 40), new Vector2(1100, 46));

            var go = BobUi.Button(screenRoot, "เริ่มด่านนี้", BobUi.Sun, PressPlay, 52);
            BobUi.Pin((RectTransform)go.transform, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-40, 30), new Vector2(420, 170));
            go.GetComponentInChildren<TMP_Text>().fontStyle = FontStyles.Bold;
        }

        void PickChapter(ChapterDefinition chapter)
        {
            if (!BobChapters.Unlocked(chapter, Level))
            {
                Toast(Level < chapter.UnlockLevel
                    ? "ปลดบทนี้ที่ระดับ " + chapter.UnlockLevel
                    : $"ต้องมีดาวบท {chapter.Order - 1} ครบ {BobChapters.StarsToOpen} ดวงก่อน");
                Sfx("drop_invalid");
                return;
            }
            BobChapters.Select(chapter, Level);
            band = chapter.Band;
            SaveProfile(); Sfx("ui_confirm"); RenderAll();
        }

        // ---------------------------------------------------------------- S11 mode select

        void ModeSelect()
        {
            ScreenBackdrop("lobby_coffee_shop");
            MenuBackgroundVideo.Attach(screenRoot);
            TopBar(screenRoot, "เลือกโหมด", true);
            float mw = CardW(6, 16, 290); var row = BobUi.Pin(BobUi.Rect("Modes", screenRoot), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 10), new Vector2(6 * mw + 5 * 16, 600));
            var h = row.gameObject.AddComponent<HorizontalLayoutGroup>(); h.spacing = 16; h.childControlWidth = h.childControlHeight = true; h.childForceExpandWidth = h.childForceExpandHeight = true;
            foreach (var m in Modes)
            {
                var mm = m; bool locked = m.Lock != null, sel = m.Id == mode;
                var card = BobUi.Button(row, "", sel ? BobUi.Hex("#FFF0C2") : BobUi.Surface, () => PressMode(mm), 24);
                card.GetComponentInChildren<TMP_Text>().text = "";
                var band = BobUi.Rect("Band", card.transform); BobUi.Pin(band, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -14), new Vector2(250, 190));
                band.gameObject.AddComponent<Image>().color = locked ? new Color(0.7f, 0.7f, 0.74f) : m.Color;
                var big = BobUi.Label(band, m.Name.Substring(0, 1), 150, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold); BobUi.Stretch(big.rectTransform);
                var nm = BobUi.Label(card.transform, m.Name, 46, BobUi.Ink, TextAnchor.MiddleCenter, FontStyle.Bold); BobUi.Pin(nm.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -230), new Vector2(260, 64));
                // Long names tie with the card width ("Community" is the worst case), so the two text blocks scale instead of clipping.
                nm.enableAutoSizing = true; nm.fontSizeMin = 30; nm.fontSizeMax = 46;
                var inf = BobUi.Label(card.transform, m.Players + "\n" + m.Length, 30, BobUi.Muted, TextAnchor.UpperCenter); BobUi.Pin(inf.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -310), new Vector2(260, 100));
                inf.enableAutoSizing = true; inf.fontSizeMin = 20; inf.fontSizeMax = 30;
                string status = locked ? "ล็อก" : m.Id == "story" ? "เลือกบทก่อน" : m.Team ? "จัดทีม 2v2 · 3v3" : "จัดทีม";
                var st = BobUi.Label(card.transform, status, 26, locked ? BobUi.Muted : BobUi.Good, TextAnchor.MiddleCenter, FontStyle.Bold);
                BobUi.Pin(st.rectTransform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 30), new Vector2(260, 52));
                if (locked) { var li = BobUi.Img(card.transform, "icon_cross", 56, BobUi.Muted); BobUi.Pin(li.rectTransform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-12, -12), new Vector2(56, 56)); }
            }
            var note = BobUi.Label(screenRoot, "Story = บทที่เลือก 3 รอบ · Solo / Duo / Team = รอบเดียว 2 นาที · ช่องเพื่อนในทีมเป็นบอทออฟไลน์จนกว่าห้องออนไลน์จะมา", 22, BobUi.Surface, TextAnchor.MiddleLeft);
            BobUi.OutlineText(note, BobUi.Ink, 0.25f);
            BobUi.Pin(note.rectTransform, Vector2.zero, Vector2.zero, new Vector2(40, 40), new Vector2(1500, 46));
        }

        void PressMode(ModeInfo m)
        {
            if (m.Lock != null) { Toast(m.Lock); Sfx("drop_invalid"); return; }
            if (m.Id == "story") { Sfx("ui_confirm"); phase = Phase.Chapters; RenderAll(); return; }
            OpenTeamSetup(m);
        }

        // ---------------------------------------------------------------- S14 team select (the room before a match)

        // The room of docs 11 S14 without a network: pick your hero, and for Team pick 2v2 or 3v3 and the heroes that fill
        // the other slots. Those slots are offline teammates (BobTaskRaceTeammates) until docs 27's lobby seats friends.
        ModeInfo pendingMode;
        int teamSize = 3;
        readonly string[] partner = { "bubble", "spray" };

        void OpenTeamSetup(ModeInfo m)
        {
            pendingMode = m; Sfx("ui_tap");
            phase = Phase.Team; RenderAll();
        }

        /// <summary>Tasks one team is dealt in this mode: the round director's own composition table, so the card and
        /// the match can never disagree (docs 16 section 2.2).</summary>
        static int TasksFor(int players, int difficulty)
        {
            Bob.TaskRace.TaskRaceRound.CompositionFor(players, difficulty, out int common, out int longs, out int shorts);
            return common + longs + shorts;
        }

        /// <summary>The heroes of the player's side, you first: the partner picks, skipping anyone already in the team.</summary>
        List<string> Lineup(int slots)
        {
            var ids = BobMatch.Chars.Select(c => c.Id).ToList();
            var line = new List<string> { charId };
            foreach (var pick in partner)
            {
                if (line.Count >= slots) break;
                string next = ids.Contains(pick) && !line.Contains(pick) ? pick : ids.FirstOrDefault(id => !line.Contains(id));
                if (next != null) line.Add(next);
            }
            while (line.Count < slots) { var next = ids.FirstOrDefault(id => !line.Contains(id)); if (next == null) break; line.Add(next); }
            return line;
        }

        /// <summary>Tap a partner slot: the next roster hero that is not you and not already in the team.</summary>
        void CyclePartner(int slot)
        {
            var line = Lineup(teamSize);
            string current = slot < line.Count ? line[slot] : null;
            var ids = BobMatch.Chars.Select(c => c.Id).ToList();
            int at = Mathf.Max(0, ids.IndexOf(current ?? ""));
            for (int k = 1; k <= ids.Count; k++)
            {
                var next = ids[(at + k) % ids.Count];
                if (next == charId || line.Contains(next)) continue;
                partner[Mathf.Clamp(slot - 1, 0, partner.Length - 1)] = next;
                Sfx("ui_tap"); RenderAll(); return;
            }
        }

        void TeamSetup()
        {
            var m = pendingMode ?? Modes.First(x => x.Id == "duo");
            var chapter = Chapter;
            int slots = m.Team ? Mathf.Clamp(teamSize, 2, 3) : 1;
            int listPlayers = m.Team ? slots : m.ListPlayers;
            var line = Lineup(slots);

            ScreenBackdrop("lobby_kitchen");
            TopBar(screenRoot, "จัดทีม · " + m.Name, true, () => { phase = Phase.Modes; RenderAll(); });

            // --- your side: the slots, then the hero picker
            var left = Card(screenRoot, "Team", BobUi.Surface);
            BobUi.Pin(left, new Vector2(0, 1), new Vector2(0, 1), new Vector2(40, -140), new Vector2(1060, 800));
            BobUi.Pin(BobUi.Label(left, "ทีมเรา", 42, BobUi.Ink, TextAnchor.UpperLeft, FontStyle.Bold).rectTransform,
                new Vector2(0, 1), new Vector2(0, 1), new Vector2(28, -18), new Vector2(400, 56));
            BobUi.Pin(BobUi.Label(left, slots == 1 ? "คุณคนเดียว" : $"คุณ + เพื่อนบอท {slots - 1} คน", 24, BobUi.Muted, TextAnchor.UpperRight).rectTransform,
                new Vector2(1, 1), new Vector2(1, 1), new Vector2(-28, -22), new Vector2(560, 40));

            float sw = slots >= 3 ? 300f : 340f, sgap = 26f;
            for (int i = 0; i < slots; i++)
            {
                string hid = line[i];
                bool mine = i == 0;
                var slot = Card(left, "Slot", mine ? BobUi.Hex("#FFF0C2") : BobUi.Hex("#F4EEDC"), 1.1f);
                BobUi.Pin(slot, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2((i - (slots - 1) / 2f) * (sw + sgap), -84), new Vector2(sw, 300));
                BobUi.Pin(BobUi.Img(slot, hid + "_portrait", 200).rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -14), new Vector2(200, 200));
                BobUi.Pin(BobUi.Label(slot, BobMatch.Char(hid).Name, 30, BobUi.Ink, TextAnchor.MiddleCenter, FontStyle.Bold).rectTransform,
                    new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -216), new Vector2(sw - 16, 36));
                BobUi.Pin(BobUi.Label(slot, mine ? "คุณ" : "เพื่อนบอท · แตะเพื่อเปลี่ยน", 20, mine ? BobUi.Teal : BobUi.Muted, TextAnchor.MiddleCenter, FontStyle.Bold).rectTransform,
                    new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -254), new Vector2(sw - 16, 30));
                if (!mine)
                {
                    // the slot is a button: tap to walk the partner through the rest of the roster
                    var b = slot.gameObject.AddComponent<Button>();
                    int s = i;
                    b.onClick.AddListener(() => CyclePartner(s));
                }
            }

            BobUi.Pin(BobUi.Label(left, "ตัวละครของคุณ", 28, BobUi.Muted, TextAnchor.UpperLeft, FontStyle.Bold).rectTransform,
                new Vector2(0, 1), new Vector2(0, 1), new Vector2(28, -404), new Vector2(500, 40));
            var chars = BobMatch.Chars;
            float cw = 230f, cgap = 20f;
            for (int i = 0; i < chars.Length; i++)
            {
                var c = chars[i]; bool sel = c.Id == charId;
                var card = BobUi.Button(left, "", sel ? BobUi.Hex("#FFF0C2") : BobUi.Surface, () => SelectChar(c.Id), 22);
                BobUi.Pin((RectTransform)card.transform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(28 + i * (cw + cgap), -450), new Vector2(cw, 200));
                card.GetComponentInChildren<TMP_Text>().text = "";
                BobUi.Pin(BobUi.Img(card.transform, c.Id + "_portrait", 116).rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -10), new Vector2(116, 116));
                BobUi.Pin(BobUi.Label(card.transform, c.Name, 28, BobUi.Ink, TextAnchor.MiddleCenter, FontStyle.Bold).rectTransform,
                    new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 54), new Vector2(cw - 10, 34));
                BobUi.Pin(BobUi.Label(card.transform, c.Ability, 20, BobUi.Teal, TextAnchor.MiddleCenter).rectTransform,
                    new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 18), new Vector2(cw - 10, 30));
                if (sel) BobUi.Pin(BobUi.Img(card.transform, "icon_check", 34, BobUi.Good).rectTransform, Vector2.one, Vector2.one, new Vector2(-8, -8), new Vector2(34, 34));
            }
            BobUi.Pin(BobUi.Label(left, "กติกากลางรอบเหมือนกันทุกโหมด · สกิลฮีโร่ยังไม่มีผลใน Task Race (Phase 2)", 20, BobUi.Muted, TextAnchor.LowerLeft).rectTransform,
                new Vector2(0, 0), new Vector2(0, 0), new Vector2(28, 20), new Vector2(1000, 40));

            // --- the right: what this pick actually runs
            var right = Card(screenRoot, "Summary", BobUi.Bg);
            BobUi.Pin(right, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-40, -140), new Vector2(760, 0));
            VStack(right, 34, 30, 12);
            right.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            BobUi.Label(right, m.Name, 64, m.Color, TextAnchor.MiddleLeft, FontStyle.Bold);
            BobUi.Label(right, m.Blurb, 24, BobUi.Muted, TextAnchor.MiddleLeft);
            BobUi.Label(right, $"งานต่อทีม · {TasksFor(listPlayers, chapter.Difficulty)} งาน", 28, BobUi.Ink);
            BobUi.Label(right, $"นาฬิกา · {BobStages.Get(chapter.Order).ClockSeconds:0} วิ · สะพานเปิด {BobStages.Get(chapter.Order).BridgeOpenAtRemaining:0} วิ", 26, BobUi.Ink);
            BobUi.Label(right, $"บทที่ใช้ · บท {chapter.Order} “{chapter.Title}” · D{chapter.Difficulty}", 24, BobUi.Muted);
            if (m.Team)
            {
                BobUi.Label(right, "ขนาดทีม", 26, BobUi.Muted, TextAnchor.MiddleLeft, FontStyle.Bold);
                var sizes = BobUi.HBox(right, 14, "Sizes"); BobUi.Size(sizes, 104, 110);
                foreach (int n in new[] { 2, 3 })
                {
                    int size = n;
                    var b = BobUi.Button(sizes.transform, size == 2 ? "2 พบ 2" : "3 พบ 3", teamSize == size ? BobUi.Sun : BobUi.Surface,
                        () => { teamSize = size; Sfx("ui_tap"); RenderAll(); }, 34);
                    b.GetComponentInChildren<TMP_Text>().fontStyle = FontStyles.Bold;
                }
            }
            BobUi.Label(right, m.Team
                ? "ช่องเพื่อนเป็นบอทในเครื่องนี้: ช่วยเก็บงานฝั่งเรา เดินช้ากว่าคุณและไม่ข้ามไปป่วน · เพื่อนจริงจะมาเมื่อเปิดห้องออนไลน์ (Phase 2)"
                : "โหมดนี้เล่นคนเดียว รอบเดียว 2 นาที · คู่แข่งเป็นแบบจำลองฝีมือ (pace model)", 20, BobUi.Muted, TextAnchor.UpperLeft);

            var go = BobUi.Button(right, "เริ่มแมตช์", BobUi.Sun, StartPendingMatch, 52);
            BobUi.Size(go, 150, 160);
            go.GetComponentInChildren<TMP_Text>().fontStyle = FontStyles.Bold;
        }

        void StartPendingMatch()
        {
            var m = pendingMode ?? Modes.First(x => x.Id == "duo");
            int slots = m.Team ? Mathf.Clamp(teamSize, 2, 3) : 1;
            var helpers = m.Team ? Lineup(slots).Skip(1).ToArray() : new string[0];
            Sfx("ui_confirm");
            // The scene reads this in Awake (TaskRaceModeManager.ApplyMatchRequest): team size, series and the offline
            // teammates. Writing it here, right before the load, keeps the pick and the match in one place.
            MatchRequest.Set(m.Id, m.Team ? slots : m.ListPlayers, m.Series, helpers);
            mode = m.Id; SaveProfile();
            LoadMatchScene(m.Scene);
        }

        // ---------------------------------------------------------------- S16 hero select (15 s when starting a match)

        bool heroForMatch;
        void OpenHeroSelect(bool forMatch)
        {
            heroForMatch = forMatch; heroTimer = 15f; heroTimerOn = forMatch;
            phase = Phase.Heroes; RenderAll();
        }

        void HeroSelect()
        {
            ScreenBackdrop("lobby_rental_room");
            TopBar(screenRoot, "เลือกตัวละคร", true);
            float hw = CardW(4, 18, 380); var row = BobUi.Pin(BobUi.Rect("Heroes", screenRoot), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -10), new Vector2(4 * hw + 3 * 18, 600));
            var h = row.gameObject.AddComponent<HorizontalLayoutGroup>(); h.spacing = 18; h.childControlWidth = h.childControlHeight = true; h.childForceExpandWidth = h.childForceExpandHeight = true;
            foreach (var c in BobMatch.Chars)
            {
                var cid = c.Id; bool sel = cid == charId;
                var card = BobUi.Button(row, "", sel ? BobUi.Hex("#FFF0C2") : BobUi.Surface, () => { SelectChar(cid); }, 24);
                card.GetComponentInChildren<TMP_Text>().text = "";
                // B6 (docs 13): the illustrated card fills the whole card. The art is text-free and keeps a calm spotlight
                // in the middle for the portrait; a cream panel under the text keeps the Thai labels readable.
                var art = BobUi.Rect("CardArt", card.transform); BobUi.Stretch(art);
                var ai = art.gameObject.AddComponent<Image>();
                ai.sprite = BobUi.Art != null && BobUi.Art.Has("card_" + cid) ? BobUi.Art.Get("card_" + cid) : null;
                ai.preserveAspect = false; ai.raycastTarget = false;
                var col = BobUi.Hex(c.Color);
                if (ai.sprite == null) ai.color = new Color(col.r, col.g, col.b, 0.35f); // old tint if the art is missing
                var panel = BobUi.Rect("TextPanel", card.transform);
                BobUi.Pin(panel, new Vector2(0.5f, 0), new Vector2(0.5f, 0), Vector2.zero, new Vector2(hw, 272));
                panel.gameObject.AddComponent<Image>().color = new Color(1f, 0.992f, 0.96f, 0.96f);
                var pi = BobUi.Img(card.transform, cid + "_portrait", 300); BobUi.Pin(pi.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -30), new Vector2(300, 300));
                if (sel) BobUi.Pin(BobUi.Img(card.transform, "icon_check", 44, BobUi.Good).rectTransform, Vector2.one, Vector2.one, new Vector2(-14, -14), new Vector2(44, 44));
                var nm = BobUi.Label(card.transform, c.Name, 50, BobUi.Ink, TextAnchor.MiddleCenter, FontStyle.Bold); BobUi.Pin(nm.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -355), new Vector2(340, 66));
                var ab = BobUi.Label(card.transform, c.Ability, 34, BobUi.Hex(c.Color), TextAnchor.MiddleCenter, FontStyle.Bold); BobUi.Pin(ab.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -420), new Vector2(340, 52));
                var ds = BobUi.Label(card.transform, c.Desc, 24, BobUi.Muted, TextAnchor.UpperCenter); BobUi.Pin(ds.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -476), new Vector2(330, 110));
                if (sel) card.transform.localScale = Vector3.one * 1.04f;
            }
            if (heroForMatch)
            {
                var tp = Card(screenRoot, "Timer", BobUi.Surface, 1f); BobUi.Pin(tp, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -24), new Vector2(250, 96));
                BobUi.Pin(BobUi.Img(tp, "icon_timer", 60).rectTransform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(20, 0), new Vector2(60, 60));
                heroTimerText = BobUi.Label(tp, "15", 52, BobUi.Ink, TextAnchor.MiddleRight, FontStyle.Bold); BobUi.Stretch(heroTimerText.rectTransform, 80, 0, 24, 0);
            }
            var go = BobUi.Button(screenRoot, heroForMatch ? "ยืนยัน" : "เลือก", BobUi.Sun, ConfirmHero, 56);
            BobUi.Pin((RectTransform)go.transform, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-40, 30), new Vector2(420, 180));
            go.GetComponentInChildren<TMP_Text>().fontStyle = FontStyles.Bold;
        }

        void ConfirmHero()
        {
            heroTimerOn = false; Sfx("ui_confirm");
            if (heroForMatch) StartMatch(); else GoHome();
        }

        void TickMenus()
        {
            if (phase == Phase.Heroes && heroTimerOn)
            {
                heroTimer -= Time.unscaledDeltaTime;
                if (heroTimerText != null)
                {
                    int s = Mathf.CeilToInt(Mathf.Max(0, heroTimer)); heroTimerText.text = s.ToString();
                    heroTimerText.color = s <= 5 ? BobUi.Bad : BobUi.Ink;
                    if (s != lastTick && s <= 5) { lastTick = s; Sfx("timer_tick"); }
                }
                if (heroTimer <= 0) { Toast("หมดเวลา เลือกตัวละครให้แล้ว"); ConfirmHero(); }
            }
        }

        // ---------------------------------------------------------------- S30 shop + P02 confirm

        void Shop()
        {
            ScreenBackdrop("lobby_coffee_shop");
            TopBar(screenRoot, "ร้านค้า", true);
            var tabs = BobUi.Label(screenRoot, "อุปกรณ์ทำความสะอาด", 40, BobUi.Surface, TextAnchor.MiddleLeft, FontStyle.Bold); BobUi.OutlineText(tabs, BobUi.Ink, 0.3f);
            BobUi.Pin(tabs.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(40, -150), new Vector2(900, 60));
            float sw = CardW(5, 16, 330); var row = BobUi.Pin(BobUi.Rect("Items", screenRoot), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -20), new Vector2(5 * sw + 4 * 16, 560));
            var h = row.gameObject.AddComponent<HorizontalLayoutGroup>(); h.spacing = 16; h.childControlWidth = h.childControlHeight = true; h.childForceExpandWidth = h.childForceExpandHeight = true;
            foreach (var it in ShopItems)
            {
                var item = it; bool has = owned.Contains(it.id);
                var card = BobUi.Button(row, "", has ? BobUi.Hex("#E6F4E4") : BobUi.Surface, () => AskBuy(item.id), 24);
                card.GetComponentInChildren<TMP_Text>().text = "";
                var ic = BobUi.Img(card.transform, it.sprite, 250); BobUi.Pin(ic.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -24), new Vector2(250, 300));
                var nm = BobUi.Label(card.transform, it.name, 34, BobUi.Ink, TextAnchor.MiddleCenter, FontStyle.Bold); BobUi.Pin(nm.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -340), new Vector2(310, 100));
                var pr = Card(card.transform, "Price", has ? BobUi.Good : BobUi.Sun, 1f); BobUi.Pin(pr, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 24), new Vector2(250, 80));
                BobUi.Pin(BobUi.Img(pr, "item_coin", 52).rectTransform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(14, 0), new Vector2(52, 52));
                var pt = BobUi.Label(pr, has ? "มีแล้ว" : it.price.ToString(), 38, has ? Color.white : BobUi.Ink, TextAnchor.MiddleRight, FontStyle.Bold); BobUi.Stretch(pt.rectTransform, 70, 0, 18, 0);
            }
            var fn = BobUi.Label(screenRoot, "ของแต่งตัวใช้ในตู้เสื้อผ้า (เร็วๆ นี้) ไม่มีกล่องสุ่ม ราคาแสดงชัดเจนเสมอ", 26, BobUi.Surface, TextAnchor.MiddleCenter); BobUi.OutlineText(fn, BobUi.Ink, 0.25f);
            BobUi.Pin(fn.rectTransform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 40), new Vector2(1500, 50));
        }

        void AskBuy(string id)
        {
            var it = ShopItems.First(x => x.id == id);
            if (owned.Contains(id)) { Toast("มีของชิ้นนี้แล้ว"); Sfx("ui_tap"); return; }
            Sfx("ui_card_flip");
            var dim = BobUi.Rect("Confirm", screenRoot); BobUi.Stretch(dim); var di = dim.gameObject.AddComponent<Image>(); di.color = new Color(0.08f, 0.09f, 0.14f, 0.7f);
            var card = Card(dim, "Dialog", BobUi.Bg); BobUi.Pin(card, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(900, 0));
            VStack(card, 50, 40, 16); card.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            BobUi.Label(card, "ยืนยันการซื้อ", 52, BobUi.Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
            var ic = BobUi.Img(card, it.sprite, 200); BobUi.Size(ic, 200, 200, 0, 200);
            BobUi.Label(card, it.name, 40, BobUi.Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
            BobUi.Label(card, ItemUse(it.id), 26, BobUi.Muted, TextAnchor.MiddleCenter);
            int left = coins - it.price; bool can = left >= 0;
            BobUi.Label(card, $"ราคา {it.price} เหรียญ · มี {coins} · เหลือ {(can ? left.ToString() : "ไม่พอ")}", 30, can ? BobUi.Muted : BobUi.Bad, TextAnchor.MiddleCenter);
            var btns = BobUi.HBox(card, 18, "Buttons"); BobUi.Size(btns, 144, 144); btns.childForceExpandHeight = true;
            var no = BobUi.Button(btns.transform, "ยกเลิก", BobUi.Surface, () => { Sfx("ui_cancel"); Destroy(dim.gameObject); }, 40);
            var yes = BobUi.Button(btns.transform, can ? "ซื้อ" : "เหรียญไม่พอ", can ? BobUi.Sun : BobUi.Surface, () =>
            {
                if (coins < it.price) return;
                coins -= it.price; owned.Add(id); SaveProfile(); Sfx("coin"); Toast("ซื้อแล้ว: " + it.name);
                Destroy(dim.gameObject); RenderScreen();
            }, 40);
            yes.interactable = can; yes.GetComponentInChildren<TMP_Text>().fontStyle = FontStyles.Bold;
        }
    }
}
