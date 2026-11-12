// Battle of the Brooms: the screens. Top bar (round, time, budget, score), side panel (room spec + live stat bars,
// the cart stack, hand, the nearby station, ability + submit), and modal cards for title / intro / test / result.
// Same content and wording as prototype/index.html, built with uGUI from code.
using System.Collections.Generic;
using System.Linq;
using Bob.Rules;
using UnityEngine;
using UnityEngine.UI;

namespace Bob.Game
{
    public partial class BobGame
    {
        const float HudTopFraction = 0.075f, HudBottomFraction = 0.05f, PanelLeftFraction = 0.6f, BarScale = 12f;

        static readonly Dictionary<string, string> StatTh = new Dictionary<string, string>
            { { "clean", "ขจัดคราบ" }, { "gentle", "ถนอมพื้นผิว" }, { "speed", "เร็ว" }, { "shine", "ความเงา" } };
        static readonly Dictionary<Family, string> FamTh = new Dictionary<Family, string>
            { { Family.Tool, "อุปกรณ์" }, { Family.Chem, "น้ำยา" }, { Family.Energy, "แรงทำงาน" }, { Family.Method, "วิธีใช้" } };

        RectTransform canvasRt, topBar, panelRoot, panelContent, modalRoot, modalCard, toastRt;
        Text hudRound, hudTime, hudBudget, hudScore, hudCoins, hudNet, hintText, toastText;
        Image hudTimeBg;
        float toastUntil;
        bool lastWide;

        // ---------------------------------------------------------------- build once

        void BuildHud()
        {
            var cgo = new GameObject("HUD", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = cgo.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 10;
            var scaler = cgo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight; scaler.matchWidthOrHeight = 1f;
            canvasRt = (RectTransform)cgo.transform;
            var safe = BobUi.Stretch(BobUi.Rect("Safe", canvasRt));
            safe.gameObject.AddComponent<SafeArea>();

            // top bar
            topBar = BobUi.Rect("TopBar", safe);
            topBar.anchorMin = new Vector2(0, 1 - HudTopFraction); topBar.anchorMax = Vector2.one; topBar.offsetMin = topBar.offsetMax = Vector2.zero;
            var tb = topBar.gameObject.AddComponent<HorizontalLayoutGroup>();
            tb.padding = new RectOffset(18, 18, 10, 10); tb.spacing = 10; tb.childAlignment = TextAnchor.MiddleLeft;
            tb.childControlWidth = true; tb.childControlHeight = true; tb.childForceExpandWidth = false; tb.childForceExpandHeight = true;
            var logo = BobUi.Panel(topBar, BobUi.Rose, "ui_button", "Logo");
            var lt = BobUi.Label(logo.transform, "BOB", 34, BobUi.Surface, TextAnchor.MiddleCenter, FontStyle.Bold); BobUi.Stretch(lt.rectTransform);
            BobUi.Size(logo, -1, -1, -1, 110); logo.rectTransform.localRotation = Quaternion.Euler(0, 0, 2);
            var sub = BobUi.Label(topBar, "สมรภูมิปัดกวาด · Unity v0.1", 22, BobUi.Muted); BobUi.Size(sub, -1, -1, 1);
            hudRound = Pill(out _); hudTime = Pill(out hudTimeBg); hudBudget = Pill(out _); hudScore = Pill(out _); hudCoins = Pill(out _); hudNet = Pill(out _);

            // hint under the room
            var hint = BobUi.Rect("Hint", safe);
            hint.anchorMin = Vector2.zero; hint.anchorMax = new Vector2(PanelLeftFraction, HudBottomFraction); hint.offsetMin = new Vector2(18, 0); hint.offsetMax = Vector2.zero;
            hintText = BobUi.Label(hint, "", 22, BobUi.Muted); BobUi.Stretch(hintText.rectTransform);

            // side panel (scrolls when it does not fit)
            panelRoot = BobUi.Rect("Panel", safe);
            var bg = panelRoot.gameObject.AddComponent<Image>(); bg.color = new Color(0, 0, 0, 0);
            panelRoot.gameObject.AddComponent<RectMask2D>();
            var sr = panelRoot.gameObject.AddComponent<ScrollRect>(); sr.horizontal = false; sr.movementType = ScrollRect.MovementType.Clamped; sr.scrollSensitivity = 30;
            panelContent = BobUi.Rect("Content", panelRoot);
            panelContent.anchorMin = new Vector2(0, 1); panelContent.anchorMax = Vector2.one; panelContent.pivot = new Vector2(0.5f, 1); panelContent.offsetMin = panelContent.offsetMax = Vector2.zero;
            var pv = panelContent.gameObject.AddComponent<VerticalLayoutGroup>();
            pv.spacing = 10; pv.padding = new RectOffset(8, 18, 4, 18);
            pv.childControlWidth = true; pv.childControlHeight = true; pv.childForceExpandWidth = true; pv.childForceExpandHeight = false;
            panelContent.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            sr.content = panelContent; sr.viewport = panelRoot;

            // toast over the room
            toastRt = BobUi.Rect("Toast", safe);
            var timg = toastRt.gameObject.AddComponent<Image>(); timg.color = BobUi.Ink; timg.raycastTarget = false;
            if (art.Has("ui_panel")) { timg.sprite = art.Get("ui_panel"); timg.type = Image.Type.Sliced; timg.pixelsPerUnitMultiplier = 1.4f; timg.color = BobUi.Ink; }
            toastText = BobUi.Label(toastRt, "", 24, BobUi.Surface, TextAnchor.MiddleCenter); BobUi.Stretch(toastText.rectTransform, 20, 4, 20, 4);
            var fit = toastRt.gameObject.AddComponent<HorizontalLayoutGroup>(); fit.padding = new RectOffset(26, 26, 8, 8); fit.childControlWidth = fit.childControlHeight = true;
            var csf = toastRt.gameObject.AddComponent<ContentSizeFitter>(); csf.horizontalFit = csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            toastRt.pivot = new Vector2(0.5f, 0); toastRt.gameObject.SetActive(false);

            // modal
            modalRoot = BobUi.Stretch(BobUi.Rect("Modal", safe));
            var dim = modalRoot.gameObject.AddComponent<Image>(); dim.color = BobUi.Dim;
            modalCard = BobUi.Rect("Card", modalRoot);
            modalCard.anchorMin = modalCard.anchorMax = new Vector2(0.5f, 0.5f); modalCard.pivot = new Vector2(0.5f, 0.5f);
            var cimg = modalCard.gameObject.AddComponent<Image>(); cimg.color = BobUi.Bg;
            if (art.Has("ui_panel")) { cimg.sprite = art.Get("ui_panel"); cimg.type = Image.Type.Sliced; cimg.pixelsPerUnitMultiplier = 1.4f; }
            var cv = modalCard.gameObject.AddComponent<VerticalLayoutGroup>();
            cv.padding = new RectOffset(40, 40, 32, 34); cv.spacing = 14;
            cv.childControlWidth = true; cv.childControlHeight = true; cv.childForceExpandWidth = true; cv.childForceExpandHeight = false;
            var mf = modalCard.gameObject.AddComponent<ContentSizeFitter>(); mf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            Layout();
        }

        Text Pill(out Image bg)
        {
            bg = BobUi.Panel(topBar, BobUi.Surface, "ui_panel", "Pill");
            var h = bg.gameObject.AddComponent<HorizontalLayoutGroup>(); h.padding = new RectOffset(18, 18, 2, 2); h.childControlWidth = h.childControlHeight = true;
            var t = BobUi.Label(bg.transform, "", 22, BobUi.Ink, TextAnchor.MiddleCenter);
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            return t;
        }

        void Layout()
        {
            bool wide = Screen.width >= Screen.height; lastWide = wide;
            if (wide) { panelRoot.anchorMin = new Vector2(PanelLeftFraction, 0); panelRoot.anchorMax = new Vector2(1, 1 - HudTopFraction); }
            else { panelRoot.anchorMin = Vector2.zero; panelRoot.anchorMax = new Vector2(1, 0.5f); }
            panelRoot.offsetMin = panelRoot.offsetMax = Vector2.zero;
            toastRt.anchorMin = toastRt.anchorMax = wide ? new Vector2(PanelLeftFraction / 2, HudBottomFraction + 0.04f) : new Vector2(0.5f, 0.53f);
            toastRt.anchoredPosition = Vector2.zero;
            modalCard.sizeDelta = new Vector2(wide ? 1180 : 980, 0);
        }

        // ---------------------------------------------------------------- per frame

        void TickHud()
        {
            if ((Screen.width >= Screen.height) != lastWide) Layout();
            if (R != null && (phase == Phase.Build || phase == Phase.Scoring))
            {
                hudTime.text = "เวลา " + Mathf.CeilToInt(time) + (freeze > 0 ? " (หยุด " + Mathf.CeilToInt(freeze) + ")" : "");
                bool warn = time <= 15 && phase == Phase.Build;
                hudTimeBg.color = warn ? BobUi.Rose : BobUi.Surface; hudTime.color = warn ? BobUi.Surface : BobUi.Ink;
            }
            if (toastRt.gameObject.activeSelf && Time.time > toastUntil) toastRt.gameObject.SetActive(false);
            var h = Hint();
            if (hintText.text != h) hintText.text = h;
        }

        void Toast(string msg)
        {
            toastText.text = msg; toastRt.gameObject.SetActive(true); toastUntil = Time.time + 1.8f;
        }

        string Hint()
        {
            if (phase != Phase.Build) return "";
            if (hand.Count == 0 && stack.Count == 0) return "แตะในห้องหรือกด WASD เพื่อเดินไปที่จุดเก็บของ แล้วแตะชิ้นส่วนเพื่อหยิบ";
            if (hand.Count > 0 && !nearCart) return "ถือของอยู่ เดินไปที่รถเข็นกลางห้องเพื่อวาง";
            if (nearCart && hand.Count > 0) return "แตะชิ้นส่วนในมือ (หรือกด 1-3) เพื่อวางบนรถเข็น ลำดับการซ้อนมีผล";
            return "ดูแถบค่าทางขวา เส้นสีชมพูคือเป้าของสเปก · Q ยกชั้นบนออก · F ใช้ ability · Enter ส่งทดสอบ";
        }

        // ---------------------------------------------------------------- render on change

        void RenderHud()
        {
            if (panelContent == null) return;
            hudRound.transform.parent.gameObject.SetActive(R != null);
            hudTime.transform.parent.gameObject.SetActive(R != null);
            hudBudget.transform.parent.gameObject.SetActive(R != null);
            hudScore.transform.parent.gameObject.SetActive(R != null);
            if (R != null)
            {
                hudRound.text = "รอบ " + roundNo + "/3";
                hudBudget.text = "งบ " + BudgetLeft + "/" + budget;
                hudScore.text = "เรา " + wins + " : " + losses + " คู่แข่ง";
                hudTime.text = "เวลา " + Mathf.CeilToInt(time);
            }
            hudCoins.text = "เหรียญ " + coins;
            hudNet.text = online ? "● ออนไลน์" : "○ ออฟไลน์";
            hudNet.color = online ? BobUi.Good : BobUi.Muted;

            RenderPanel();
            RenderModal();
        }

        void RenderPanel()
        {
            BobUi.Clear(panelContent);
            var P = panelContent;
            if (R == null)
            {
                var how = BobUi.Box(P, "วิธีเล่น");
                BobUi.Label(how.transform, "1. เดินไปที่จุดเก็บของ แล้วแตะชิ้นส่วนเพื่อหยิบ (ถือได้ 3 ชิ้น)\n2. เดินไปที่รถเข็นกลางห้อง แล้วแตะชิ้นส่วนในมือเพื่อวาง ซ้อนได้ 5 ชั้น + วิธีใช้ 2 อย่าง\n3. แตะชั้นบนสุดเพื่อยกออก ลำดับการซ้อนมีผลต่อค่าสถานะ\n4. กดส่งชุดเข้าทดสอบเมื่อพร้อม ชนะคู่แข่ง 2 ใน 3 รอบ", 21, BobUi.Ink);
                var all = BobUi.Box(P, "ชิ้นส่วนทั้งหมด", "ตัวเลขท้ายคือราคา");
                var g = BobUi.Grid(all.transform, Cell(2), 2);
                foreach (var id in BobRules.Parts.Keys) Chip(g.transform, id, null, false, false);
                return;
            }

            var e = Ev();
            // room spec + live stat bars
            var spec = BobUi.Box(P, "โจทย์ห้อง", string.Join(" · ", R.Notes));
            var tags = BobUi.HBox(spec.transform, 6, "Tags"); tags.childForceExpandWidth = false; BobUi.Size(tags, 36, 36);
            foreach (var en in R.Enemies) Tag(tags.transform, BobMatch.Enemy(en).Name, BobUi.Ink, BobUi.Surface);
            foreach (var kv in R.Need) Tag(tags.transform, StatTh[kv.Key] + " " + kv.Value + "+", BobUi.Bg, BobUi.Ink);
            for (int i = 0; i < 4; i++) StatBar(spec.transform, BobRules.StatKeys[i], e.Stats[i]);
            foreach (var f in e.Fx.Where(f => f.At == -1)) FxLine(spec.transform, f, 0);
            if (preview != null) BobUi.Label(spec.transform, "เล็งเป้า: กองนี้ได้ " + preview.Total + " คะแนน" + (preview.Pass ? " (ผ่านสเปก)" : " (ยังไม่ผ่าน)"), 20, BobUi.Teal, TextAnchor.MiddleLeft, FontStyle.Bold);

            // cart
            var cart = BobUi.Box(P, "รถเข็น", $"ชั้น {e.Layers}/{BobRules.MaxLayers} · วิธีใช้ {BobRules.CountMethods(stack)}/{BobRules.MaxMethods}" + (nearCart ? "" : " · เดินมาใกล้เพื่อวาง"));
            for (int k = e.Layers; k < BobRules.MaxLayers; k++) Slot(cart.transform, "ชั้นว่าง", 34);
            for (int i = stack.Count - 1; i >= 0; i--)
            {
                int idx = i; bool top = i == stack.Count - 1, canSwap = swapMode && i < stack.Count - 1;
                if (canSwap) Chip(cart.transform, stack[i], () => SwapAt(idx), true, false, "สลับกับชั้นบน");
                else if (top) Chip(cart.transform, stack[i], LiftTop, true, true, "แตะเพื่อยกออก");
                else Chip(cart.transform, stack[i], null, false, false);
                foreach (var f in e.Fx.Where(f => f.At == i)) FxLine(cart.transform, f, 28);
            }
            if (swapMode) BobUi.Label(cart.transform, "แตะชิ้นที่ต้องการสลับกับชิ้นบนของมัน", 19, BobUi.Muted);

            // hand
            var hb = BobUi.Box(P, "ในมือ", $"{hand.Count}/{Hand} · แตะเพื่อวางบนรถเข็น");
            var hg = BobUi.Grid(hb.transform, Cell(3), 3);
            for (int i = 0; i < Hand; i++)
            {
                int idx = i;
                if (i < hand.Count) Chip(hg.transform, hand[i], () => Place(idx), true, false);
                else Slot(hg.transform, "ว่าง", 58);
            }

            // station nearby
            int si = near == null ? -1 : System.Array.IndexOf(StationIds, near);
            var sb = BobUi.Box(P, si >= 0 ? StationNames[si] : "จุดเก็บของ", si >= 0 ? "แตะเพื่อหยิบ" : "");
            if (si >= 0)
            {
                var items = near == "method" ? BobRules.Methods : R.Offer.Where(id => BobRules.Parts[id].Family == (Family)si).ToArray();
                var sg = BobUi.Grid(sb.transform, Cell(2), 2);
                foreach (var id in items) { var pid = id; Chip(sg.transform, id, () => PickUp(pid), phase == Phase.Build, false); }
            }
            else BobUi.Label(sb.transform, "เดินไปที่ชั้นวางอุปกรณ์ ตู้น้ำยา จุดแรงทำงาน หรือกระดานวิธีใช้", 20, BobUi.Muted);

            // actions
            var act = BobUi.HBox(P, 10, "Actions"); BobUi.Size(act, 72, 72);
            var h = Hero;
            var ab = BobUi.Button(act.transform, h.Name + ": " + h.Ability + (abUsed ? " (ใช้แล้ว)" : swapMode ? " (ยกเลิก)" : ""), BobUi.Sun, UseAbility, 23);
            ab.interactable = phase == Phase.Build && !abUsed;
            var go = BobUi.Button(act.transform, "ส่งชุดเข้าทดสอบ", BobUi.Teal, Submit, 24, Color.white);
            go.interactable = phase == Phase.Build && e.Layers > 0;
        }

        /// <summary>Grid cell that fills the panel width (panel padding 26 + box padding 32).</summary>
        Vector2 Cell(int cols)
        {
            float w = panelRoot.rect.width > 100 ? panelRoot.rect.width - 58 : 666;
            return new Vector2(Mathf.Max(150, (w - 6 * (cols - 1)) / cols), 58);
        }

        void Tag(Transform parent, string text, Color bg, Color fg)
        {
            var img = BobUi.Panel(parent, bg, "ui_panel", "Tag");
            var h = img.gameObject.AddComponent<HorizontalLayoutGroup>(); h.padding = new RectOffset(14, 14, 2, 2); h.childControlWidth = h.childControlHeight = true;
            var t = BobUi.Label(img.transform, text, 19, fg, TextAnchor.MiddleCenter); t.horizontalOverflow = HorizontalWrapMode.Overflow;
        }

        void StatBar(Transform parent, string key, int v)
        {
            bool hasT = R.Need.TryGetValue(key, out var target);
            var row = BobUi.HBox(parent, 10, "Stat"); row.childForceExpandWidth = false; BobUi.Size(row, 30, 30);
            if (art.Has("stat_" + key))
            {
                var ic = BobUi.Rect("Icon", row.transform).gameObject.AddComponent<Image>(); ic.sprite = art.Get("stat_" + key); ic.preserveAspect = true;
                BobUi.Size(ic, 28, 28, 0, 28);
            }
            var name = BobUi.Label(row.transform, StatTh[key], 20); BobUi.Size(name, -1, -1, 0, 120);
            var bar = BobUi.Panel(row.transform, BobUi.Bg, "ui_bar_bg", "Bar"); BobUi.Size(bar, 22, 22, 1);
            var fill = BobUi.Panel(bar.transform, hasT && v < target ? BobUi.Sun : BobUi.Teal, null, "Fill");
            fill.rectTransform.anchorMin = Vector2.zero; fill.rectTransform.anchorMax = new Vector2(Mathf.Clamp01(v / BarScale), 1);
            fill.rectTransform.offsetMin = new Vector2(3, 3); fill.rectTransform.offsetMax = new Vector2(0, -3);
            if (hasT)
            {
                var mk = BobUi.Panel(bar.transform, BobUi.Rose, null, "Target");
                mk.rectTransform.anchorMin = new Vector2(Mathf.Clamp01(target / BarScale), 0); mk.rectTransform.anchorMax = new Vector2(Mathf.Clamp01(target / BarScale), 1);
                mk.rectTransform.offsetMin = new Vector2(-2, -3); mk.rectTransform.offsetMax = new Vector2(2, 3);
            }
            var val = BobUi.Label(row.transform, v + (hasT ? " / " + target : ""), 21, hasT ? (v >= target ? BobUi.Good : BobUi.Bad) : BobUi.Ink, TextAnchor.MiddleRight, FontStyle.Bold);
            BobUi.Size(val, -1, -1, 0, 80);
        }

        void FxLine(Transform parent, Effect f, int indent)
        {
            var t = BobUi.Label(parent, new string(' ', indent / 7) + (f.Good ? "+ " : "- ") + f.Text + (f.Off ? " (โล่ฟองกันไว้)" : ""), 18,
                f.Off ? BobUi.Muted : f.Good ? BobUi.Good : BobUi.Bad);
            t.rectTransform.offsetMin = new Vector2(indent, 0);
        }

        void Slot(Transform parent, string text, float h)
        {
            var img = BobUi.Panel(parent, new Color(BobUi.Line.r, BobUi.Line.g, BobUi.Line.b, 0.6f), "ui_panel", "Slot");
            BobUi.Size(img, h, h);
            var t = BobUi.Label(img.transform, text, 18, BobUi.Muted, TextAnchor.MiddleCenter); BobUi.Stretch(t.rectTransform);
        }

        /// <summary>A part chip: family colour dot, part icon, Thai name + stat changes, cost.</summary>
        void Chip(Transform parent, string id, UnityEngine.Events.UnityAction onClick, bool enabled, bool highlight, string note = null)
        {
            var p = BobRules.Parts[id];
            var img = BobUi.Panel(parent, highlight ? BobUi.Hex("#FFF0C2") : BobUi.Surface, "ui_panel", "Chip_" + id);
            BobUi.Size(img, 54, 54);
            var b = img.gameObject.AddComponent<Button>(); b.interactable = enabled && onClick != null;
            var cb = b.colors; cb.disabledColor = Color.white; b.colors = cb;
            if (onClick != null) b.onClick.AddListener(onClick);
            var row = BobUi.HBox(img.transform, 8, "Row"); BobUi.Stretch((RectTransform)row.transform, 12, 4, 12, 4);
            row.childForceExpandWidth = false; row.childForceExpandHeight = false; row.childAlignment = TextAnchor.MiddleLeft;
            var dot = BobUi.Panel(row.transform, BobUi.Family(p.Family), null, "Dot"); BobUi.Size(dot, 16, 16, 0, 16);
            if (art.Has("part_" + id)) { var ic = BobUi.Rect("Icon", row.transform).gameObject.AddComponent<Image>(); ic.sprite = art.Get("part_" + id); ic.preserveAspect = true; BobUi.Size(ic, 40, 40, 0, 40); }
            var stats = string.Join(" ", Enumerable.Range(0, 4).Where(i => p.S[i] != 0).Select(i => StatTh[BobRules.StatKeys[i]].Substring(0, 2) + (p.S[i] > 0 ? "+" : "") + p.S[i]));
            var nm = BobUi.Label(row.transform, p.Name + "  <size=15><color=#6B6E86>" + (note ?? stats) + "</color></size>", 21);
            nm.supportRichText = true; nm.horizontalOverflow = HorizontalWrapMode.Wrap; BobUi.Size(nm, -1, -1, 1);
            var co = BobUi.Label(row.transform, p.Cost.ToString(), 21, BobUi.Ink, TextAnchor.MiddleRight, FontStyle.Bold); BobUi.Size(co, -1, -1, 0, 40);
        }

        // ---------------------------------------------------------------- modal cards

        void RenderModal()
        {
            bool show = phase != Phase.Build;
            modalRoot.gameObject.SetActive(show);
            if (!show) return;
            BobUi.Clear(modalCard);
            var C = modalCard;
            Text H1(string s) { var t = BobUi.Label(C, s, 38, BobUi.Ink, TextAnchor.MiddleLeft, FontStyle.Bold); return t; }
            Text Pp(string s, int size = 24) => BobUi.Label(C, s, size, BobUi.Ink);
            HorizontalLayoutGroup Actions() { var a = BobUi.HBox(C, 12, "Actions"); BobUi.Size(a, 76, 76); return a; }

            switch (phase)
            {
                case Phase.Title:
                {
                    H1("ศึกใหญ่ที่อาวุธคือไม้ถูกับฟองน้ำ");
                    Pp("เดินไปหยิบชิ้นส่วน แล้วซ้อนบนรถเข็นไม่เกิน 5 ชั้น ลำดับมีผล ชุดของคุณจะถูกทดสอบกับห้องเดียวกันแข่งกับทีมคู่แข่ง ชนะ 2 ใน 3 รอบ");
                    Pp("<b>เลือกตัวละคร</b> แต่ละตัวมี ability ใช้ได้รอบละครั้ง").supportRichText = true;
                    var g = BobUi.Grid(C, new Vector2(530, 130), 2, 12);
                    foreach (var c in BobMatch.Chars)
                    {
                        var cid = c.Id; bool sel = charId == cid;
                        var img = BobUi.Panel(g.transform, sel ? BobUi.Sun : BobUi.Surface, "ui_panel", "Pick_" + cid);
                        img.gameObject.AddComponent<Button>().onClick.AddListener(() => SelectChar(cid));
                        var row = BobUi.HBox(img.transform, 12); BobUi.Stretch((RectTransform)row.transform, 14, 10, 14, 10); row.childForceExpandWidth = false;
                        var port = BobUi.Rect("Portrait", row.transform).gameObject.AddComponent<Image>();
                        port.sprite = art.Get(cid + "_portrait"); port.preserveAspect = true; if (!art.Has(cid + "_portrait")) port.color = BobUi.Hex(c.Color);
                        BobUi.Size(port, 100, 100, 0, 100);
                        var t = BobUi.Label(row.transform, $"<b><size=28>{c.Name} · {c.Ability}</size></b>\n{c.Desc}", 21); t.supportRichText = true; BobUi.Size(t, -1, -1, 1);
                    }
                    Pp(netNote, 20).color = online ? BobUi.Good : BobUi.Muted;
                    BobUi.Button(Actions().transform, "เริ่มแมตช์", BobUi.Teal, StartMatch, 28, Color.white);
                    break;
                }
                case Phase.Loading:
                    H1("กำลังเตรียมห้อง…");
                    Pp(online ? "ขอโจทย์ห้องรอบ " + roundNo + " จากเซิร์ฟเวอร์" : "สุ่มห้องในเครื่อง");
                    break;
                case Phase.Intro:
                {
                    H1($"รอบ {roundNo}: {string.Join(" + ", R.Enemies.Select(x => BobMatch.Enemy(x).Name))} บุกห้องเช่า");
                    foreach (var en in R.Enemies) { var E = BobMatch.Enemy(en); Pp($"<b>{E.Name} ({E.Thai})</b> {E.Tip}").supportRichText = true; }
                    Pp("<b>สเปกที่ต้องผ่าน:</b> " + string.Join(" · ", R.Need.Select(kv => StatTh[kv.Key] + " " + kv.Value + " ขึ้นไป")) +
                       (R.Notes.Count > 0 ? " (" + string.Join(", ", R.Notes) + ")" : "")).supportRichText = true;
                    Pp($"งบรอบนี้ {budget} · เวลา {RoundTime:0} วินาที · เดินด้วยการแตะในห้องหรือปุ่ม WASD", 21).color = BobUi.Muted;
                    BobUi.Button(Actions().transform, "ลุย!", BobUi.Teal, BeginBuild, 30, Color.white);
                    break;
                }
                case Phase.Scoring:
                    H1("ส่งชุดเข้าทดสอบ…");
                    Pp(online ? "เซิร์ฟเวอร์กำลังคิดคะแนน" : "คิดคะแนนในเครื่อง");
                    break;
                case Phase.Test:
                {
                    var L = last;
                    H1(L.Win ? "ชัยชนะ! ห้องสะอาดกว่า" : "คู่แข่งทำได้ดีกว่ารอบนี้").color = L.Win ? BobUi.Good : BobUi.Bad;
                    var tbl = BobUi.Grid(C, new Vector2(270, 38), 4, 2);
                    void Row(string a, string b, string c, string d, bool bold = false)
                    {
                        foreach (var (s, i) in new[] { a, b, c, d }.Select((s, i) => (s, i)))
                            BobUi.Label(tbl.transform, s, bold ? 26 : 22, BobUi.Ink, i == 0 ? TextAnchor.MiddleLeft : TextAnchor.MiddleRight, bold ? FontStyle.Bold : FontStyle.Normal);
                    }
                    Row("รอบ " + roundNo, "ทีมเรา", "คู่แข่ง", "เป้า", true);
                    foreach (var kv in R.Need)
                    {
                        int k = System.Array.IndexOf(BobRules.StatKeys, kv.Key);
                        Row(StatTh[kv.Key], L.Stats[k].ToString(), L.Rival.Ev.Stats[k].ToString(), kv.Value.ToString());
                    }
                    Row("ผ่านสเปก", L.Mine.Pass ? "ผ่าน" : "ไม่ผ่าน", L.Rival.Score.Pass ? "ผ่าน" : "ไม่ผ่าน", "");
                    Row("ผลทดสอบ (70%)", Js(L.Mine.Test), Js(L.Rival.Score.Test), "");
                    Row("งบที่เหลือ (20%)", Js(L.Mine.Budget), Js(L.Rival.Score.Budget), "");
                    Row("เวลาที่เหลือ (10%)", Js(L.Mine.Time), Js(L.Rival.Score.Time), "");
                    Row("คะแนนรอบ", L.Mine.Total.ToString(), L.Rival.Score.Total.ToString(), "", true);
                    string Names(IEnumerable<string> st) { var n = string.Join(" → ", st.Select(id => BobRules.Parts[id].Name)); return n.Length > 0 ? n : "(ว่าง)"; }
                    Pp("<b>กองของเรา:</b> " + Names(L.Stack), 22).supportRichText = true;
                    Pp("<b>กองของคู่แข่ง:</b> " + Names(L.Rival.Stack), 22).supportRichText = true;
                    Pp(L.FromServer ? "คะแนนของเราคิดโดยเซิร์ฟเวอร์ Nakama" : "คะแนนคิดในเครื่อง (ไม่ได้ต่อเซิร์ฟเวอร์)", 19).color = BobUi.Muted;
                    BobUi.Button(Actions().transform, MatchOver ? "ดูผลแมตช์" : "รอบถัดไป", BobUi.Teal, AfterTest, 28, Color.white);
                    break;
                }
                case Phase.Result:
                {
                    bool w = wins > losses;
                    H1((w ? "ชนะแมตช์! " : "แพ้แมตช์ ") + wins + " : " + losses).color = w ? BobUi.Good : BobUi.Bad;
                    Pp((w ? "ทุกคราบโบกมือลา" : "คราบยังอยู่ แต่ทีมได้ประสบการณ์") + $" ได้รับ {gain} เหรียญ (ชนะ 30 / แพ้ 20 ตาม GDD)");
                    Pp("เล่นคนเดียวแข่งกับคู่แข่งจำลอง ใช้ทดสอบว่าแกนซ้อนชั้นสนุกหรือไม่ ตัวเลขทั้งหมดยังไม่ได้ปรับสมดุล", 20).color = BobUi.Muted;
                    var a = Actions();
                    BobUi.Button(a.transform, "เล่นอีกแมตช์", BobUi.Teal, StartMatch, 26, Color.white);
                    BobUi.Button(a.transform, "เปลี่ยนตัวละคร", BobUi.Surface, ToTitle, 26);
                    break;
                }
            }
        }

        static string Js(double x) => ((int)System.Math.Floor(x + 0.5)).ToString(); // JavaScript Math.round
    }

    /// <summary>Keeps the UI inside the phone's safe area (notches, rounded corners).</summary>
    public class SafeArea : MonoBehaviour
    {
        Rect applied;
        void Update()
        {
            var s = Screen.safeArea;
            if (s == applied || Screen.width == 0 || Screen.height == 0) return;
            applied = s;
            var r = (RectTransform)transform;
            r.anchorMin = new Vector2(s.xMin / Screen.width, s.yMin / Screen.height);
            r.anchorMax = new Vector2(s.xMax / Screen.width, s.yMax / Screen.height);
            r.offsetMin = r.offsetMax = Vector2.zero;
        }
    }
}
