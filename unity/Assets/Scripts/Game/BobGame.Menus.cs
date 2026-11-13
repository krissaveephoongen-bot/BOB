// Battle of the Brooms: the screens around the match (UX spec section 4): splash, login (guest), Home / lobby (S10),
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
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Bob.Game
{
    public partial class BobGame
    {
        // ---------------------------------------------------------------- profile (device-local)

        string mode = "solo";
        int xp, gems, xpGain;
        readonly HashSet<string> owned = new HashSet<string>();
        float heroTimer; bool heroTimerOn; TMP_Text heroTimerText;
        Image pendingShopRoot;

        int Level => 1 + xp / 200;
        static readonly string[] HomeBackdrops = { "lobby_rental_room", "lobby_condo", "lobby_coffee_shop", "lobby_kitchen" };

        sealed class ModeInfo { public string Id, Name, Players, Length, Lock; public Color Color; }
        static readonly ModeInfo[] Modes =
        {
            new ModeInfo { Id = "story",     Name = "Story",     Players = "1-4 คน", Length = "ต่อบท",   Lock = "เร็วๆ นี้: โหมดเนื้อเรื่อง", Color = new Color(0.98f, 0.62f, 0.28f) },
            new ModeInfo { Id = "solo",      Name = "Solo",      Players = "1 คน",   Length = "6 นาที",  Lock = null,                          Color = new Color(0.40f, 0.71f, 0.95f) },
            new ModeInfo { Id = "duo",       Name = "Duo",       Players = "2 คน",   Length = "6 นาที",  Lock = "เร็วๆ นี้: ต้องผูกคู่ก่อน",   Color = new Color(0.93f, 0.28f, 0.38f) },
            new ModeInfo { Id = "friends",   Name = "Friends",   Players = "2-4 คน", Length = "6 นาที",  Lock = null,                          Color = new Color(0.24f, 0.72f, 0.52f) },
            new ModeInfo { Id = "party",     Name = "Party",     Players = "3-7 คน", Length = "8 นาที",  Lock = "เร็วๆ นี้: ปลดที่ระดับ 6",   Color = new Color(0.58f, 0.46f, 0.88f) },
            new ModeInfo { Id = "community", Name = "Community", Players = "10-15 คน", Length = "10 นาที", Lock = "เร็วๆ นี้: เปิดเป็นรอบเวลา", Color = new Color(0.55f, 0.58f, 0.66f) },
        };
        ModeInfo CurrentMode => Modes.FirstOrDefault(m => m.Id == mode) ?? Modes[1];

        static readonly (string id, string name, string sprite, int price)[] ShopItems =
        {
            ("broom",  "ไม้กวาดฟาง",     "menu_equip_broom",  120),
            ("mop",    "ไม้ถูพื้นนุ่ม",   "menu_equip_mop",    160),
            ("sponge", "ฟองน้ำยิ้ม",      "menu_equip_sponge", 100),
            ("spray",  "ขวดสเปรย์ชมพู",  "menu_equip_spray",  140),
            ("hat",    "หมวกถังน้ำ",      "menu_equip_hat",    200),
        };

        void LoadProfile()
        {
            try
            {
                mode = PlayerPrefs.GetString("bob.mode", "solo"); if (Modes.All(m => m.Id != mode)) mode = "solo";
                xp = PlayerPrefs.GetInt("bob.xp", 0); gems = PlayerPrefs.GetInt("bob.gems", 0);
                foreach (var s in PlayerPrefs.GetString("bob.owned", "").Split(',')) if (s.Length > 0) owned.Add(s);
            }
            catch (Exception) { }
        }

        void SaveProfile()
        {
            try
            {
                PlayerPrefs.SetString("bob.mode", mode); PlayerPrefs.SetInt("bob.xp", xp); PlayerPrefs.SetInt("bob.gems", gems);
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

        void Login()
        {
            ScreenBackdrop("lobby_condo");
            var panel = Card(screenRoot, "LoginPanel", BobUi.Bg); BobUi.Pin(panel, new Vector2(0.72f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(680, 0));
            VStack(panel, 50, 44, 22); panel.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var lg = BobUi.Img(panel, "menu_logo", 190); BobUi.Size(lg, 190, 190, 0, 190);
            BobUi.Label(panel, "ยินดีต้อนรับ", 52, BobUi.Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
            var guest = BobUi.Button(panel, "เล่นเลย (Guest)", BobUi.Sun, () =>
            {
                try { PlayerPrefs.SetInt("bob.guest", 1); PlayerPrefs.Save(); } catch (Exception) { }
                Sfx("ui_confirm"); phase = Phase.Title; RenderAll();
            }, 44);
            BobUi.Size(guest, 144, 160); guest.GetComponentInChildren<TMP_Text>().fontStyle = FontStyles.Bold;
            var g = BobUi.Button(panel, "เข้าด้วย Google (เร็วๆ นี้)", BobUi.Surface, null, 30, BobUi.Muted); BobUi.Size(g, 144, 144); g.interactable = false;
            var a = BobUi.Button(panel, "เข้าด้วย Apple (เร็วๆ นี้)", BobUi.Surface, null, 30, BobUi.Muted); BobUi.Size(a, 144, 144); a.interactable = false;
            BobUi.Label(panel, "นโยบายความเป็นส่วนตัว · ข้อตกลง", 24, BobUi.Muted, TextAnchor.MiddleCenter);
        }

        // ---------------------------------------------------------------- shared pieces

        /// <summary>Top bar of the out-of-match screens: back or profile on the left, currency on the right.</summary>
        void TopBar(Transform p, string title, bool back)
        {
            if (back)
            {
                var b = BobUi.Button(p, "", BobUi.Surface, () => { Sfx("ui_cancel"); GoHome(); }, 24);
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
            // server state (top centre)
            var net = Card(S, "Net", BobUi.Surface, 1f); BobUi.Pin(net, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(-60, -30), new Vector2(330, 72));
            var nt = BobUi.Label(net, online ? "ออนไลน์" : connecting ? "กำลังเชื่อมต่อ…" : "ออฟไลน์", 26, online ? BobUi.Good : BobUi.Muted, TextAnchor.MiddleCenter); BobUi.Stretch(nt.rectTransform, 40, 0, 0, 0);
            BobUi.Pin(BobUi.Img(net, online ? "icon_check" : connecting ? "icon_timer" : "icon_cross", 34, online ? BobUi.Good : BobUi.Muted).rectTransform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(22, 0), new Vector2(34, 34));

            // hero on a stage (tap = choose hero)
            var stage = BobUi.Disc(S, new Color(1, 1, 1, 0.28f), 440, "Stage"); BobUi.Pin(stage.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 0), new Vector2(440, 440));
            var hero = BobUi.Rect("Hero", S); BobUi.Pin(hero, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 30), new Vector2(440, 440));
            var himg = hero.gameObject.AddComponent<Image>(); himg.preserveAspect = true;
            var idle = art.Find(charId + "_idle");
            if (idle != null) { var an = hero.gameObject.AddComponent<BobSpriteAnimator>(); an.Play(idle.frames, idle.fps, true); } else himg.color = BobUi.Hex(Hero.Color);
            var hb = hero.gameObject.AddComponent<Button>(); hb.onClick.AddListener(() => { Sfx("ui_tap"); OpenHeroSelect(false); });
            var hn = H1(S, Hero.Name, 60, BobUi.Surface); BobUi.OutlineText(hn, BobUi.Ink, 0.3f);
            BobUi.Pin(hn.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -225), new Vector2(700, 80));
            var hs = BobUi.Label(S, "แตะตัวละครเพื่อเปลี่ยน · " + Hero.Ability, 26, BobUi.Surface, TextAnchor.MiddleCenter); BobUi.OutlineText(hs, BobUi.Ink, 0.25f);
            BobUi.Pin(hs.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -280), new Vector2(900, 44));

            // dock (bottom left): only buttons that have real icons and a working screen
            var dock = BobUi.Pin(BobUi.Rect("Dock", S), Vector2.zero, Vector2.zero, new Vector2(30, 30), new Vector2(2 * 190, 190));
            var dh = dock.gameObject.AddComponent<HorizontalLayoutGroup>(); dh.spacing = 14; dh.childControlWidth = dh.childControlHeight = true;
            DockButton(dock, "menu_shop", "ร้านค้า", () => { Phase p = Phase.Shop; phase = p; Sfx("ui_confirm"); RenderAll(); }, false);
            DockButton(dock, "menu_home", "บ้าน", () => { Toast("เร็วๆ นี้: แต่งบ้านและห้องของฉัน"); Sfx("ui_tap"); }, true);

            // mode selector + PLAY (bottom right, thumb zone)
            var m = CurrentMode;
            var mb = BobUi.Button(S, m.Name + "  ▾".Replace("▾", ""), BobUi.Surface, () => { Sfx("ui_tap"); phase = Phase.Modes; RenderAll(); }, 36);
            BobUi.Pin((RectTransform)mb.transform, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-590, 40), new Vector2(300, 160));
            var play = BobUi.Button(S, "เล่น", BobUi.Sun, PressPlay, 76);
            BobUi.Pin((RectTransform)play.transform, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-40, 30), new Vector2(520, 200));
            play.GetComponentInChildren<TMP_Text>().fontStyle = FontStyles.Bold;
            StartCoroutine(Breathe(play.transform));
        }

        void DockButton(Transform dock, string icon, string label, Action onClick, bool dim)
        {
            var b = BobUi.Button(dock, "", dim ? new Color(1, 0.99f, 0.95f, 0.7f) : BobUi.Surface, () => onClick(), 24);
            var t = b.GetComponentInChildren<TMP_Text>(); t.text = label; t.fontSize = 24; t.alignment = TextAlignmentOptions.Bottom; BobUi.Stretch(t.rectTransform, 4, 8, 4, 6);
            var ic = BobUi.Img(b.transform, icon, 100); BobUi.Pin(ic.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -16), new Vector2(100, 100));
            if (dim) ic.color = new Color(1, 1, 1, 0.6f);
        }

        void PressPlay()
        {
            var m = CurrentMode;
            if (m.Lock != null) { Toast(m.Lock); Sfx("drop_invalid"); return; }
            Sfx("ui_confirm");
            if (m.Id == "friends") { FindOnline(); return; }
            OpenHeroSelect(true);
        }

        // ---------------------------------------------------------------- S11 mode select

        void ModeSelect()
        {
            ScreenBackdrop("lobby_coffee_shop");
            TopBar(screenRoot, "เลือกโหมด", true);
            float mw = CardW(6, 16, 290); var row = BobUi.Pin(BobUi.Rect("Modes", screenRoot), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 10), new Vector2(6 * mw + 5 * 16, 600));
            var h = row.gameObject.AddComponent<HorizontalLayoutGroup>(); h.spacing = 16; h.childControlWidth = h.childControlHeight = true; h.childForceExpandWidth = h.childForceExpandHeight = true;
            foreach (var m in Modes)
            {
                var mm = m; bool locked = m.Lock != null, sel = m.Id == mode;
                var card = BobUi.Button(row, "", sel ? BobUi.Hex("#FFF0C2") : BobUi.Surface, () =>
                {
                    if (mm.Lock != null) { Toast(mm.Lock); Sfx("drop_invalid"); return; }
                    mode = mm.Id; SaveProfile(); Sfx("ui_confirm"); phase = Phase.Title; RenderAll();
                }, 24);
                card.GetComponentInChildren<TMP_Text>().text = "";
                var band = BobUi.Rect("Band", card.transform); BobUi.Pin(band, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -14), new Vector2(250, 190));
                band.gameObject.AddComponent<Image>().color = locked ? new Color(0.7f, 0.7f, 0.74f) : m.Color;
                var big = BobUi.Label(band, m.Name.Substring(0, 1), 150, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold); BobUi.Stretch(big.rectTransform);
                var nm = BobUi.Label(card.transform, m.Name, 46, BobUi.Ink, TextAnchor.MiddleCenter, FontStyle.Bold); BobUi.Pin(nm.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -230), new Vector2(260, 64));
                var inf = BobUi.Label(card.transform, m.Players + "\n" + m.Length, 30, BobUi.Muted, TextAnchor.UpperCenter); BobUi.Pin(inf.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -310), new Vector2(260, 100));
                var st = BobUi.Label(card.transform, locked ? "ล็อก" : sel ? "เลือกอยู่" : "เล่นได้", 28, locked ? BobUi.Muted : BobUi.Good, TextAnchor.MiddleCenter, FontStyle.Bold);
                BobUi.Pin(st.rectTransform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 30), new Vector2(260, 52));
                if (locked) { var li = BobUi.Img(card.transform, "icon_cross", 56, BobUi.Muted); BobUi.Pin(li.rectTransform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-12, -12), new Vector2(56, 56)); }
            }
            var ok = BobUi.Button(screenRoot, "เลือก", BobUi.Sun, () => { Sfx("ui_confirm"); GoHome(); }, 56);
            BobUi.Pin((RectTransform)ok.transform, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-40, 30), new Vector2(420, 180));
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
                var bg = BobUi.Rect("Tint", card.transform); BobUi.Pin(bg, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -14), new Vector2(340, 330));
                var col = BobUi.Hex(c.Color); bg.gameObject.AddComponent<Image>().color = new Color(col.r, col.g, col.b, 0.35f);
                var pi = BobUi.Img(card.transform, cid + "_portrait", 300); BobUi.Pin(pi.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -30), new Vector2(300, 300));
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
