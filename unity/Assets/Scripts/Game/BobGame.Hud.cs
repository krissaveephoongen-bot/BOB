// Battle of the Brooms: mobile HUD and screens (landscape, thumb-zone layout, every touch target >= 96 units ≈ 10 mm).
//
//  ┌ order ticket ┐   ┌ round pips · TIMER · score ┐            ┌ budget · pause ┐
//  │ enemies+stats│                                              │  cart stack    │
//  │              │          the room (tap to walk)             │  (lift / swap) │
//  │  joystick    │      station shelf pops up next to station   │ [ability]      │
//  │   zone       │          [hand slot][hand slot][hand slot]   │ [  SUBMIT  ]   │
//
// Screens: main menu (hero showcase + PLAY), room ticket before each round, 3-2-1, round result with count-up, match
// result, pause and settings (sound, music).
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Bob.Art;
using Bob.Modular;
using Bob.Ugs;
using Bob.Rules;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Bob.Game
{
    public partial class BobGame
    {
        // Screen split (fractions of the safe area). The room camera fills the middle.
        public const float SideCol = 0.15f, TopStrip = 0.10f, BottomStrip = 0.17f;

        static readonly Dictionary<string, string> StatTh = new Dictionary<string, string>
            { { "clean", "ขจัดคราบ" }, { "gentle", "ถนอมผิว" }, { "speed", "ความเร็ว" }, { "shine", "ความเงา" } };
        // One-line ability blurbs for the in-round button (the full text is on the menu).
        static readonly Dictionary<string, string> AbilityShort = new Dictionary<string, string>
            { { "sweep", "สลับ 2 ชั้นที่ติดกัน" }, { "bubble", "กันปฏิกิริยาเสีย 1 คู่" }, { "spray", "ดูคะแนนล่วงหน้า" }, { "press", "หยุดนาฬิกา 8 วิ" } };

        RectTransform canvasRt, hudRoot, orderBody, cartBody, handRow, stationPop, joyBase, joyKnob,
            modalRoot, screenRoot, overlayRoot, toastRt, countdownRt;
        TMP_Text timerText, scoreText, budgetText, hintText, toastText, countdownText;
        Image timerBg;
        readonly Image[] pips = new Image[3];
        Button abilityBtn, submitBtn;
        TMP_Text abilityLabel;
        float toastUntil;
        RectTransform cartCardRt;
        string popFor;
        int lastTick = -1;
        enum Overlay { None, Pause, Settings, Board, Daily, Wardrobe }
        Overlay overlay = Overlay.None;
        // The board card reads a snapshot, never the SDK: LoadBoard fills it in the background and re-renders.
        BobBoardSnapshot boardSnap;
        bool boardLoading;
        // The daily card holds the server's reply, never a locally invented seed (docs 31 section 2.2).
        BobDailyReply dailyReply;
        bool dailyBusy;
        // The same card's board: yesterday's runs never mix in, so this reads BobDaily.DailyBoardName(day) only.
        BobBoardSnapshot dailySnap;
        bool dailyBoardBusy;
        // Which wardrobe tab is open (0 = titles, 1 = frame colours, 2 = the sewing table); kept while the card is
        // open, reset on reload.
        int wardrobeTab;
        // The look being tried on at the sewing table (four layer indices); null until the wardrobe opens.
        int[] sewPicks;

        // ================================================================ build once

        void BuildHud()
        {
            var cgo = new GameObject("HUD", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = cgo.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 10;
            var scaler = cgo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight; scaler.matchWidthOrHeight = 1f; // landscape: keep heights constant
            canvasRt = (RectTransform)cgo.transform;
            var safe = BobUi.Stretch(BobUi.Rect("Safe", canvasRt));
            safe.gameObject.AddComponent<SafeArea>();

            hudRoot = BobUi.Stretch(BobUi.Rect("HUD", safe));
            BuildTopBar(); BuildOrderTicket(); BuildCartCard(); BuildHand(); BuildActionButtons(); BuildJoystick();
            BindHudController();

            stationPop = BobUi.Rect("StationShelf", hudRoot);
            var spImg = stationPop.gameObject.AddComponent<Image>(); spImg.sprite = art.Get("ui_panel"); spImg.type = Image.Type.Sliced; spImg.pixelsPerUnitMultiplier = 1.2f; spImg.color = BobUi.Surface;
            var spv = stationPop.gameObject.AddComponent<VerticalLayoutGroup>(); spv.padding = new RectOffset(18, 18, 12, 16); spv.spacing = 8;
            spv.childControlWidth = spv.childControlHeight = true; spv.childForceExpandHeight = false;
            var spf = stationPop.gameObject.AddComponent<ContentSizeFitter>(); spf.horizontalFit = spf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            stationPop.gameObject.SetActive(false);

            // hint just above the hand tray
            var hint = BobUi.Pin(BobUi.Rect("Hint", hudRoot), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 150), new Vector2(1000, 40));
            hintText = BobUi.Label(hint, "", 22, BobUi.Ink, TextAnchor.MiddleCenter); BobUi.Stretch(hintText.rectTransform);
            BobUi.OutlineText(hintText, new Color(1, 1, 1, 0.8f), 0.25f);

            // toast
            toastRt = BobUi.Pin(BobUi.Rect("Toast", safe), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -150), Vector2.zero);
            var timg = toastRt.gameObject.AddComponent<Image>(); timg.sprite = art.Get("ui_panel"); timg.type = Image.Type.Sliced; timg.pixelsPerUnitMultiplier = 1.4f; timg.color = BobUi.Ink; timg.raycastTarget = false;
            var tl = toastRt.gameObject.AddComponent<HorizontalLayoutGroup>(); tl.padding = new RectOffset(34, 34, 14, 14); tl.childControlWidth = tl.childControlHeight = true;
            var tf = toastRt.gameObject.AddComponent<ContentSizeFitter>(); tf.horizontalFit = tf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            toastText = BobUi.Label(toastRt, "", 28, BobUi.Surface, TextAnchor.MiddleCenter); toastText.textWrappingMode = TextWrappingModes.NoWrap;
            toastRt.gameObject.SetActive(false);

            // 3-2-1
            countdownRt = BobUi.Pin(BobUi.Rect("Countdown", safe), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(700, 300));
            countdownText = BobUi.Label(countdownRt, "", 200, BobUi.Rose, TextAnchor.MiddleCenter, FontStyle.Bold); BobUi.Stretch(countdownText.rectTransform);
            BobUi.OutlineText(countdownText, BobUi.Ink, 0.25f);
            countdownRt.gameObject.SetActive(false);

            // full-screen screens and overlays
            modalRoot = BobUi.Stretch(BobUi.Rect("Screens", safe));
            var dim = modalRoot.gameObject.AddComponent<Image>(); dim.color = new Color(0.12f, 0.13f, 0.2f, 0.7f);
            screenRoot = BobUi.Stretch(BobUi.Rect("Screen", modalRoot));
            overlayRoot = BobUi.Stretch(BobUi.Rect("Overlay", safe));
            var odim = overlayRoot.gameObject.AddComponent<Image>(); odim.color = new Color(0.12f, 0.13f, 0.2f, 0.7f);
            overlayRoot.gameObject.SetActive(false);
        }

        RectTransform Card(Transform parent, string name, Color? color = null, float ppu = 1.2f)
        {
            var r = BobUi.Rect(name, parent);
            var img = r.gameObject.AddComponent<Image>(); img.sprite = art.Get("ui_panel"); img.type = Image.Type.Sliced; img.pixelsPerUnitMultiplier = ppu; img.color = color ?? BobUi.Surface;
            return r;
        }

        VerticalLayoutGroup VStack(RectTransform r, int padX = 18, int padY = 14, float spacing = 8)
        {
            var v = r.gameObject.AddComponent<VerticalLayoutGroup>(); v.padding = new RectOffset(padX, padX, padY, padY); v.spacing = spacing;
            v.childControlWidth = v.childControlHeight = true; v.childForceExpandWidth = true; v.childForceExpandHeight = false;
            return v;
        }

        void BuildTopBar()
        {
            // centre: round pips · timer · score
            var top = BobUi.Pin(BobUi.Rect("TopCentre", hudRoot), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -14), new Vector2(760, 100));
            top.localScale = Vector3.one * 0.8f; var h = top.gameObject.AddComponent<HorizontalLayoutGroup>(); h.spacing = 16; h.childAlignment = TextAnchor.MiddleCenter;
            h.childControlWidth = h.childControlHeight = false; h.childForceExpandWidth = h.childForceExpandHeight = false;
            var pipBox = BobUi.Rect("Pips", top); pipBox.sizeDelta = new Vector2(150, 50);
            for (int i = 0; i < 3; i++) { pips[i] = BobUi.Disc(pipBox, BobUi.Line, 40, "Pip"); pips[i].rectTransform.anchoredPosition = new Vector2(-50 + i * 50, 0); }
            var tp = Card(top, "Timer", BobUi.Surface, 1f); tp.sizeDelta = new Vector2(250, 96); timerBg = tp.GetComponent<Image>();
            BobUi.Pin(BobUi.Img(tp, "icon_timer", 60).rectTransform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(20, 0), new Vector2(60, 60));
            timerText = BobUi.Label(tp, "100", 52, BobUi.Ink, TextAnchor.MiddleRight, FontStyle.Bold); BobUi.Stretch(timerText.rectTransform, 80, 0, 24, 0);
            var sc = Card(top, "Score", BobUi.Surface, 1f); sc.sizeDelta = new Vector2(250, 76);
            scoreText = BobUi.Label(sc, "", 26, BobUi.Ink, TextAnchor.MiddleCenter); BobUi.Stretch(scoreText.rectTransform); scoreText.richText = true;

            // right: budget + pause
            var bp = Card(hudRoot, "Budget", BobUi.Surface, 1f);
            BobUi.Pin(bp, Vector2.one, Vector2.one, new Vector2(-112, -12), new Vector2(230, 96)); bp.localScale = Vector3.one * 0.8f; bp.pivot = Vector2.one;
            BobUi.Pin(BobUi.Img(bp, "icon_budget", 56).rectTransform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(18, 0), new Vector2(56, 56));
            budgetText = BobUi.Label(bp, "", 40, BobUi.Ink, TextAnchor.MiddleRight, FontStyle.Bold); BobUi.Stretch(budgetText.rectTransform, 80, 0, 22, 0);
            var pause = BobUi.Button(hudRoot, "", BobUi.Surface, () => SetOverlay(Overlay.Pause));
            BobUi.Pin((RectTransform)pause.transform, Vector2.one, Vector2.one, new Vector2(-12, -12), new Vector2(104, 96)); pause.transform.localScale = Vector3.one * 0.8f;
            var pi = BobUi.Img(pause.transform, "icon_pause", 56); BobUi.Pin(pi.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(56, 56));
            if (!art.Has("icon_pause")) pause.GetComponentInChildren<TMP_Text>().text = "II";
            BuildCartSlotBar();
            Bob.UI.BobHitArea.Ensure(pause.targetGraphic, 144f); // UX spec 1.2: touch area >= 144u without growing the picture
        }

        MobileHUDController hudCtl;
        Image[] slotImgs = new Image[BobRules.MaxLayers];

        /// <summary>UX 5A.2 top right: the cart's layers as icons only, five slots.</summary>
        void BuildCartSlotBar()
        {
            var bar = BobUi.Pin(BobUi.Rect("CartSlots", hudRoot), Vector2.one, Vector2.one, new Vector2(-12, -92), new Vector2(5 * 56 + 4 * 6, 56));
            var h = bar.gameObject.AddComponent<HorizontalLayoutGroup>(); h.spacing = 6; h.childControlWidth = h.childControlHeight = true; h.childForceExpandWidth = h.childForceExpandHeight = true;
            for (int i = 0; i < slotImgs.Length; i++)
            {
                var back = BobUi.Disc(bar, new Color(1f, 0.99f, 0.95f, 0.85f), 56, "Slot");
                var ic = BobUi.Img(back.transform, "", 44); BobUi.Pin(ic.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(44, 44));
                slotImgs[i] = ic;
            }
        }

        /// <summary>Hand the code-built widgets to the Modular HUD controller (it drives timer pulse, slot icons and the main button).</summary>
        void BindHudController()
        {
            hudCtl = hudRoot.gameObject.AddComponent<MobileHUDController>();
            hudCtl.timerPill = timerBg; hudCtl.timerTextTmp = timerText; hudCtl.warnAt = 15f;
            hudCtl.cartSlots = slotImgs;
            hudCtl.primaryButton = ctxBtn; hudCtl.primaryLabelTmp = ctxLabel; hudCtl.primaryImage = (Image)ctxBtn.targetGraphic;
            hudCtl.submitButton = submitBtn; hudCtl.submitRing = submitHold != null ? submitHold.ring : null;
            hudCtl.Bind(); // BobGame wires pause / primary / submit itself; the controller drives their look (label, tint, ring)
        }

        void BuildOrderTicket()
        {
            var card = Card(hudRoot, "OrderTicket");
            card.anchorMin = card.anchorMax = new Vector2(0, 1); card.pivot = new Vector2(0, 1);
            card.sizeDelta = new Vector2(400, 0); card.anchoredPosition = new Vector2(12, -12); card.localScale = Vector3.one * 0.72f;
            card.GetComponent<Image>().color = new Color(1f, 0.99f, 0.95f, 0.9f);
            VStack(card, 18, 14, 6);
            card.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var head = BobUi.Label(card, "ใบงาน", 26, BobUi.Muted, TextAnchor.MiddleLeft, FontStyle.Bold); BobUi.Size(head, 32, 32);
            orderBody = BobUi.Rect("Body", card);
            VStack(orderBody, 0, 0, 6);
        }

        void BuildCartCard()
        {
            var card = Card(hudRoot, "CartCard"); cartCardRt = card;
            card.anchorMin = card.anchorMax = Vector2.one; card.pivot = Vector2.one;
            card.sizeDelta = new Vector2(400, 0); card.anchoredPosition = new Vector2(-12, -160); card.localScale = Vector3.one * 0.72f;
            card.GetComponent<Image>().color = new Color(1f, 0.99f, 0.95f, 0.9f);
            VStack(card, 16, 12, 6);
            card.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            cartBody = BobUi.Rect("Body", card);
            VStack(cartBody, 0, 0, 5);
        }

        void BuildHand()
        {
            handRow = BobUi.Pin(BobUi.Rect("Hand", hudRoot), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(-90, 10), new Vector2(3 * 148 + 2 * 14, 148)); handRow.localScale = Vector3.one * 0.75f;
            var h = handRow.gameObject.AddComponent<HorizontalLayoutGroup>(); h.spacing = 14;
            h.childControlWidth = h.childControlHeight = true; h.childForceExpandWidth = h.childForceExpandHeight = true;
        }

        void BuildActionButtons()
        {
            var col = hudRoot; // thumb arc, bottom right (UX 5A.2): big contextual main button, send (hold) beside it, ability further left
            ctxBtn = BobUi.Button(col, "หยิบ", BobUi.Sun, DoCtx, 40);
            BobUi.Pin((RectTransform)ctxBtn.transform, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-20, 20), new Vector2(230, 160));
            ctxLabel = ctxBtn.GetComponentInChildren<TMP_Text>(); ctxLabel.fontStyle = FontStyles.Bold;

            submitBtn = BobUi.Button(col, "กดค้างเพื่อส่ง", BobUi.Teal, Submit, 30, Color.white);
            BobUi.Pin((RectTransform)submitBtn.transform, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-262, 20), new Vector2(230, 100));
            submitBtn.GetComponentInChildren<TMP_Text>().fontStyle = FontStyles.Bold;
            // UX spec 14.4: hold 0.6 s to send (a ring fills), so a thumb slip cannot end the round. Settings: PlayerPrefs bob.holdSubmit.
            submitBtn.onClick.RemoveAllListeners();
            submitHold = Bob.UI.BobHoldButton.Attach(submitBtn.targetGraphic, BobUi.Circle, new Color(1f, 0.85f, 0.3f, 0.9f), Submit);

            abilityBtn = BobUi.Button(col, "", BobUi.Sun, UseAbility, 26);
            BobUi.Pin((RectTransform)abilityBtn.transform, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-504, 20), new Vector2(200, 100)); ((Image)abilityBtn.targetGraphic).color = new Color(1f, 0.82f, 0.35f, 0.95f);
            var lab = abilityBtn.GetComponentInChildren<TMP_Text>(); lab.alignment = TextAlignmentOptions.Left; BobUi.Stretch(lab.rectTransform, 84, 4, 8, 4);
            abilityLabel = lab; abilityLabel.richText = true;
            var port = BobUi.Img(abilityBtn.transform, charId + "_portrait", 72); port.name = "Portrait";
            BobUi.Pin(port.rectTransform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(8, 0), new Vector2(72, 72));
        }

        Bob.UI.BobHoldButton submitHold;

        void BuildJoystick()
        {
            joyBase = BobUi.Disc(hudRoot, new Color(BobUi.Ink.r, BobUi.Ink.g, BobUi.Ink.b, 0.22f), 240, "JoyBase").rectTransform;
            joyBase.anchorMin = joyBase.anchorMax = Vector2.zero; joyBase.GetComponent<Image>().raycastTarget = false;
            joyKnob = BobUi.Disc(joyBase, new Color(1, 1, 1, 0.9f), 110, "JoyKnob").rectTransform;
            joyKnob.GetComponent<Image>().raycastTarget = false;
            var ring = joyKnob.gameObject.AddComponent<Outline>(); ring.effectColor = BobUi.Ink; ring.effectDistance = new Vector2(3, -3);
            joyBase.gameObject.SetActive(false);
        }

        // ================================================================ per frame

        void TickHud()
        {
            bool playing = phase == Phase.Build;
            if (R != null && (playing || phase == Phase.Scoring || phase == Phase.Countdown))
            {
                hudCtl.SetTimer(time, freeze > 0, playing); // text, frozen tint and the red pulse under 15 s
                bool warn = time <= 15 && playing;
                if (warn && Mathf.CeilToInt(time) != lastTick) { lastTick = Mathf.CeilToInt(time); Sfx("timer_tick"); StartCoroutine(BobTween.Pop(timerBg.transform, 0.2f, 1.08f)); }
            }
            if (toastRt.gameObject.activeSelf && Time.unscaledTime > toastUntil) toastRt.gameObject.SetActive(false);
            var h = Hint(); if (hintText.text != h) hintText.text = h;
            PlaceStationPop();
            // joystick visuals (screen pixels -> canvas units)
            joyBase.gameObject.SetActive(joyActive && playing);
            if (joyActive)
            {
                float k = canvasRt.rect.height / Mathf.Max(1, Screen.height);
                joyBase.anchoredPosition = joyOrigin * k;
                joyKnob.anchoredPosition = joyVec * JoyRadius * k;
            }
        }

        void Toast(string msg)
        {
            toastText.text = msg; toastRt.gameObject.SetActive(true); toastUntil = Time.unscaledTime + 1.9f;
            StartCoroutine(BobTween.Pop(toastRt, 0.2f, 1.06f));
        }

        string Hint()
        {
            if (phase != Phase.Build) return "";
            if (near != null) return "แตะชิ้นส่วนเพื่อหยิบ (ถือได้ " + HandMax + " ชิ้น)";
            if (nearCart && hand.Count > 0) return "แตะหรือลากของในมือไปที่รถเข็น ลำดับการซ้อนมีผล";
            if (hand.Count > 0) return "เดินไปที่รถเข็นกลางห้องเพื่อวางของ";
            if (stack.Count == 0) return "ลากจอยซ้ายมือ หรือแตะพื้นเพื่อเดินไปที่ชั้นวาง";
            return "ดูใบงานซ้ายบน เส้นชมพูคือเป้า พร้อมแล้วกดค้างปุ่มส่ง";
        }

        // ================================================================ render on change

        void RenderHud()
        {
            if (hudRoot == null) return;
            bool inRound = R != null && (phase == Phase.Build || phase == Phase.Scoring || phase == Phase.Countdown);
            hudRoot.gameObject.SetActive(inRound);
            if (inRound)
            {
                for (int i = 0; i < 3; i++)
                {
                    bool? res = i < results.Count ? results[i] : (bool?)null;
                    pips[i].color = res == true ? BobUi.Good : res == false ? BobUi.Rose : (i == roundNo - 1 ? BobUi.Sun : BobUi.Line);
                }
                scoreText.text = $"รอบ {roundNo}/3  <b>{wins} : {losses}</b>";
                budgetText.text = BudgetLeft.ToString();
                RenderOrder(); RenderCart(); RenderHand(); RenderButtons(); RenderStationPop();
            }
            RenderScreen();
            RenderOverlay();
        }

        void RenderOrder()
        {
            BobUi.Clear(orderBody);
            var e = Ev();
            var foes = BobUi.HBox(orderBody, 10, "Foes"); foes.childForceExpandWidth = false; foes.childForceExpandHeight = false; BobUi.Size(foes, 72, 72);
            foreach (var id in R.Enemies)
            {
                var img = BobUi.Img(foes.transform, id + "_idle", 68);
                if (!art.Has(id + "_idle")) img.color = BobUi.Hex(BobMatch.Enemy(id).Color);
                var nm = BobUi.Label(foes.transform, BobMatch.Enemy(id).Name, 24, BobUi.Ink, TextAnchor.MiddleLeft, FontStyle.Bold); BobUi.Size(nm, 68, 68);
            }
            for (int i = 0; i < 4; i++) StatRow(orderBody, BobRules.StatKeys[i], e.Stats[i]);
            foreach (var f in e.Fx.Where(f => f.At == -1).Take(2))
                EffectRow(orderBody, f, 19);
            if (preview != null)
                BobUi.Label(orderBody, "เล็งเป้า: " + preview.Total + " คะแนน" + (preview.Pass ? " ผ่าน" : " ยังไม่ผ่าน"), 22, BobUi.Teal, TextAnchor.MiddleLeft, FontStyle.Bold);
        }

        void StatRow(Transform parent, string key, int v)
        {
            bool hasT = R.Need.TryGetValue(key, out var target);
            var row = BobUi.HBox(parent, 8, "Stat"); row.childForceExpandWidth = false; row.childForceExpandHeight = false; row.childAlignment = TextAnchor.MiddleLeft;
            BobUi.Size(row, 40, 40);
            var ic = BobUi.Img(row.transform, "stat_" + key, 34); if (!hasT) ic.color = new Color(1, 1, 1, 0.45f);
            var nm = BobUi.Label(row.transform, StatTh[key], 20, hasT ? BobUi.Ink : BobUi.Muted); BobUi.Size(nm, 30, 30, 0, 92);
            var bar = BobUi.Panel(row.transform, BobUi.Bg, "ui_bar_bg", "Bar"); BobUi.Size(bar, 24, 24, 1);
            var fill = BobUi.Panel(bar.transform, hasT && v < target ? BobUi.Sun : BobUi.Teal, null, "Fill");
            fill.rectTransform.anchorMin = Vector2.zero; fill.rectTransform.anchorMax = new Vector2(Mathf.Clamp01(v / 12f), 1);
            fill.rectTransform.offsetMin = new Vector2(3, 4); fill.rectTransform.offsetMax = new Vector2(0, -4);
            if (hasT)
            {
                var mk = BobUi.Panel(bar.transform, BobUi.Rose, null, "Target");
                float x = Mathf.Clamp01(target / 12f);
                mk.rectTransform.anchorMin = new Vector2(x, 0); mk.rectTransform.anchorMax = new Vector2(x, 1);
                mk.rectTransform.offsetMin = new Vector2(-3, -5); mk.rectTransform.offsetMax = new Vector2(3, 5);
            }
            var val = BobUi.Label(row.transform, hasT ? v + "/" + target : v.ToString(), 22, hasT ? (v >= target ? BobUi.Good : BobUi.Bad) : BobUi.Muted, TextAnchor.MiddleRight, FontStyle.Bold);
            BobUi.Size(val, 30, 30, 0, 64);
        }

        /// <summary>One reaction line: an icon (check = helps, cross = cancels, pause = switched off) + the text. Never colour alone.</summary>
        void EffectRow(Transform parent, Effect f, int fontSize)
        {
            var row = BobUi.HBox(parent, 6, "Fx"); row.childForceExpandWidth = false; row.childForceExpandHeight = false; row.childAlignment = TextAnchor.MiddleLeft;
            BobUi.Size(row, fontSize + 10, fontSize + 10);
            var col = f.Off ? BobUi.Muted : f.Good ? BobUi.Good : BobUi.Bad;
            BobUi.Img(row.transform, f.Off ? "icon_pause" : f.Good ? "icon_check" : "icon_cross", fontSize + 6, col);
            var tx = BobUi.Label(row.transform, f.Text, fontSize, col); BobUi.Size(tx, fontSize + 10, fontSize + 10, 1);
        }

        void RenderCart()
        {
            if (hudCtl != null)
            {
                var icons = new List<Sprite>(); foreach (var id in stack) if (!BobRules.IsMethod(id)) icons.Add(art.Has("part_" + id) ? art.Get("part_" + id) : null);
                hudCtl.SetCartSlots(icons);
            }
            BobUi.Clear(cartBody);
            var e = Ev();
            var head = BobUi.HBox(cartBody, 6, "Head"); head.childForceExpandHeight = false; BobUi.Size(head, 34, 34);
            BobUi.Size(BobUi.Label(head.transform, "รถเข็น", 26, BobUi.Ink, TextAnchor.MiddleLeft, FontStyle.Bold), 34, 34, 1);
            BobUi.Size(BobUi.Label(head.transform, $"ชั้น {e.Layers}/{BobRules.MaxLayers} · วิธี {BobRules.CountMethods(stack)}/{BobRules.MaxMethods}", 19, BobUi.Muted, TextAnchor.MiddleRight), 34, 34, 1);
            for (int i = stack.Count - 1; i >= 0; i--)
            {
                int idx = i; var id = stack[i]; var p = BobRules.Parts[id];
                bool top = i == stack.Count - 1;
                var row = Card(cartBody, "Layer", top ? BobUi.Hex("#FFF0C2") : BobUi.Surface, 1.6f);
                BobUi.Size(row, 62, 62);
                var h = row.gameObject.AddComponent<HorizontalLayoutGroup>(); h.padding = new RectOffset(10, 8, 4, 4); h.spacing = 8;
                h.childControlWidth = h.childControlHeight = true; h.childForceExpandWidth = false; h.childForceExpandHeight = false; h.childAlignment = TextAnchor.MiddleLeft;
                var dot = BobUi.Panel(row, BobUi.Family(p.Family), null, "Fam"); BobUi.Size(dot, 40, 40, 0, 8);
                BobUi.Img(row, "part_" + id, 48);
                var fx = e.Fx.Where(f => f.At == i && !f.Off).ToList();
                string badge = fx.Count == 0 ? "" : " " + string.Join(" ", fx.Select(f => $"<color={(f.Good ? "#1E8E5A" : "#C2362B")}>{(f.Good ? "▲" : "▼")}</color>"));
                var nm = BobUi.Label(row, p.Name + badge, 22); nm.richText = true; BobUi.Size(nm, 40, 40, 1);
                if (swapMode && i < stack.Count - 1) SmallButton(row, "สลับ↑", BobUi.Sun, () => SwapAt(idx));
                else if (top && nearCart && phase == Phase.Build) SmallButton(row, "ยกออก", BobUi.Surface, LiftTop);
                if (top && !swapMode)
                {
                    // drag the top layer back to the hand to lift it (needs a raycast target on the row)
                    var drag = row.gameObject.AddComponent<BobDrag>(); drag.icon = art.Get("part_" + id); drag.layer = hudRoot;
                    drag.onDrop = sp => { if (!RectTransformUtility.RectangleContainsScreenPoint(handRow, sp, null)) return false; LiftTop(); return true; };
                }
            }
            for (int k = e.Layers; k < BobRules.MaxLayers; k++)
            {
                var slot = Card(cartBody, "Empty", new Color(BobUi.Line.r, BobUi.Line.g, BobUi.Line.b, 0.55f), 1.6f); BobUi.Size(slot, 40, 40);
                var t = BobUi.Label(slot, "ชั้นว่าง", 18, BobUi.Muted, TextAnchor.MiddleCenter); BobUi.Stretch(t.rectTransform);
            }
            // latest reactions, newest first
            foreach (var f in e.Fx.Where(f => f.At >= 0).Reverse().Take(3))
                EffectRow(cartBody, f, 18);
            if (!nearCart && phase == Phase.Build && (hand.Count > 0 || stack.Count > 0))
                BobUi.Label(cartBody, "เดินไปใกล้รถเข็นเพื่อวางหรือยกออก", 18, BobUi.Muted, TextAnchor.MiddleCenter);
        }

        /// <summary>A drop counts as "on the cart" over the cart card or within reach of the cart in the room.</summary>
        bool DroppedOnCart(Vector2 sp)
        {
            if (cartCardRt != null && RectTransformUtility.RectangleContainsScreenPoint(cartCardRt, sp, null)) return true;
            var c = cam.WorldToScreenPoint(ToWorld(Raise(new Vector2(CartRect.center.x, CartRect.yMin), 0.6f)));
            return Vector2.Distance(sp, c) < Screen.height * 0.14f;
        }

        void SmallButton(Transform parent, string text, Color c, UnityEngine.Events.UnityAction a)
        {
            var b = BobUi.Button(parent, text, c, a, 20); BobUi.Size(b, 50, 50, 0, 96);
        }

        void RenderHand()
        {
            BobUi.Clear(handRow);
            bool canPlace = nearCart && phase == Phase.Build;
            for (int i = 0; i < HandMax; i++)
            {
                int idx = i;
                if (i < hand.Count)
                {
                    var id = hand[i]; var p = BobRules.Parts[id];
                    var b = BobUi.Button(handRow, "", canPlace ? BobUi.Hex("#FFF0C2") : BobUi.Surface, () => Place(idx));
                    b.GetComponentInChildren<TMP_Text>().text = "";
                    var ic = BobUi.Img(b.transform, "part_" + id, 84); BobUi.Pin(ic.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -12), new Vector2(84, 84));
                    var nm = BobUi.Label(b.transform, p.Name, 20, BobUi.Ink, TextAnchor.MiddleCenter); BobUi.Pin(nm.rectTransform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 10), new Vector2(140, 34));
                    var fam = BobUi.Disc(b.transform, BobUi.Family(p.Family), 26); BobUi.Pin(fam.rectTransform, Vector2.one, Vector2.one, new Vector2(-10, -10), new Vector2(26, 26));
                    if (canPlace) StartCoroutine(Pulse(b.transform));
                    var drag = b.gameObject.AddComponent<BobDrag>(); drag.icon = art.Get("part_" + id); drag.layer = hudRoot;
                    drag.onDrop = sp => { if (!DroppedOnCart(sp)) return false; Place(idx); return true; };
                }
                else
                {
                    var slot = Card(handRow, "Empty", new Color(1, 1, 1, 0.55f));
                    var t = BobUi.Label(slot, "ว่าง", 20, BobUi.Muted, TextAnchor.MiddleCenter); BobUi.Stretch(t.rectTransform);
                }
            }
        }

        IEnumerator Pulse(Transform t)
        {
            while (t != null && t.gameObject.activeInHierarchy)
            {
                float s = 1 + Mathf.Sin(Time.unscaledTime * 6) * 0.03f; t.localScale = new Vector3(s, s, 1); yield return null;
            }
        }

        void RenderButtons()
        {
            var h = Hero;
            abilityLabel.text = $"<b>{h.Ability}</b>\n<size=16>{(abUsed ? "ใช้แล้ว" : swapMode ? "แตะ สลับ↑" : "พร้อม · 1 ครั้ง/แมตช์")}</size>";
            abilityBtn.interactable = phase == Phase.Build && !abUsed;
            var pt = abilityBtn.transform.Find("Portrait");
            if (pt != null) pt.GetComponent<Image>().sprite = art.Get(charId + "_portrait");
            submitBtn.interactable = phase == Phase.Build && BobRules.CountLayers(stack) > 0;
            if (submitHold != null) submitHold.interactable = submitBtn.interactable;
            RenderCtx();
        }

        // ---------------------------------------------------------------- station shelf

        void RenderStationPop()
        {
            int si = near == null || phase != Phase.Build ? -1 : System.Array.IndexOf(StationIds, near);
            if (si < 0) { stationPop.gameObject.SetActive(false); popFor = null; return; }
            BobUi.Clear(stationPop);
            var title = BobUi.HBox(stationPop, 8, "Title"); title.childForceExpandHeight = false; BobUi.Size(title, 34, 34);
            BobUi.Size(BobUi.Label(title.transform, StationNames[si], 24, BobUi.Ink, TextAnchor.MiddleLeft, FontStyle.Bold), 34, 34, 1, 220);
            BobUi.Size(BobUi.Label(title.transform, $"ในมือ {hand.Count}/{HandMax}", 20, BobUi.Muted, TextAnchor.MiddleRight), 34, 34, 0, 120);
            var row = BobUi.HBox(stationPop, 12, "Items"); row.childForceExpandWidth = false; row.childForceExpandHeight = false;
            var items = near == "method" ? BobRules.Methods : R.Offer.Where(id => BobRules.Parts[id].Family == (Family)si).ToArray();
            foreach (var id in items)
            {
                var pid = id; var p = BobRules.Parts[id];
                bool afford = p.Cost <= BudgetLeft;
                var b = BobUi.Button(row.transform, "", BobUi.Surface, () => PickUp(pid));
                b.GetComponentInChildren<TMP_Text>().text = "";
                BobUi.Size(b, 196, 196, 0, 168);
                b.interactable = hand.Count < HandMax;
                var ic = BobUi.Img(b.transform, "part_" + id, 88); BobUi.Pin(ic.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -12), new Vector2(88, 88));
                var nm = BobUi.Label(b.transform, p.Name, 22, BobUi.Ink, TextAnchor.MiddleCenter, FontStyle.Bold); BobUi.Pin(nm.rectTransform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 52), new Vector2(160, 30));
                var st = string.Join(" ", Enumerable.Range(0, 4).Where(i => p.S[i] != 0).Select(i => StatTh[BobRules.StatKeys[i]].Substring(0, 2) + (p.S[i] > 0 ? "+" : "") + p.S[i]));
                var sl = BobUi.Label(b.transform, st, 15, BobUi.Muted, TextAnchor.MiddleCenter); BobUi.Pin(sl.rectTransform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 26), new Vector2(166, 24));
                var cost = BobUi.Disc(b.transform, afford ? BobUi.Sun : BobUi.Rose, 50, "Cost"); BobUi.Pin(cost.rectTransform, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(-14, -14), new Vector2(50, 50));
                var ct = BobUi.Label(cost.transform, p.Cost.ToString(), 20, BobUi.Ink, TextAnchor.MiddleCenter, FontStyle.Bold); BobUi.Stretch(ct.rectTransform);
                var fam = BobUi.Panel(b.transform, BobUi.Family(p.Family), null, "Fam"); BobUi.Pin(fam.rectTransform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 8), new Vector2(120, 6));
            }
            stationPop.gameObject.SetActive(true);
            if (popFor != near) { popFor = near; StartCoroutine(BobTween.Pop(stationPop, 0.22f, 1.06f)); Sfx("ui_card_flip"); }
            PlaceStationPop();
        }

        /// <summary>Keep the shelf next to its station on screen: below the top stations, above the bottom ones.</summary>
        void PlaceStationPop()
        {
            if (stationPop == null || !stationPop.gameObject.activeSelf || near == null) return;
            int si = System.Array.IndexOf(StationIds, near); var r = StationRects[si];
            bool topRow = si < 2;
            var wp = topRow ? Raise(new Vector2(r.center.x, r.yMin - 0.15f), 0f) : Raise(new Vector2(r.center.x, r.yMin), 1.3f);
            var sp = cam.WorldToScreenPoint(ToWorld(wp));
            RectTransformUtility.ScreenPointToLocalPointInRectangle(hudRoot, sp, null, out var lp);
            stationPop.anchorMin = stationPop.anchorMax = new Vector2(0.5f, 0.5f);
            stationPop.pivot = new Vector2(0.5f, topRow ? 1 : 0);
            var half = hudRoot.rect.size / 2; var sz = stationPop.rect.size;
            lp.x = Mathf.Clamp(lp.x, -half.x + sz.x / 2 + 10, half.x - sz.x / 2 - 10);
            lp.y = topRow ? Mathf.Clamp(lp.y, -half.y + sz.y + 160, half.y - 120) : Mathf.Clamp(lp.y, -half.y + 170, half.y - sz.y - 120);
            stationPop.anchoredPosition = lp;
        }

        // ================================================================ screens

        TMP_Text H1(Transform p, string s, int size = 56, Color? c = null)
        {
            var t = BobUi.Label(p, s, size, c ?? BobUi.Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
            BobUi.OutlineText(t, new Color(1, 1, 1, 0.6f), 0.25f);
            return t;
        }

        void RenderScreen()
        {
            bool show = phase != Phase.Build && phase != Phase.Countdown;
            modalRoot.gameObject.SetActive(show);
            if (!show) return;
            if (phase == Phase.Test && shownPhase != Phase.Test) testStart = Time.unscaledTime;
            shownPhase = phase;
            BobUi.Clear(screenRoot);
            switch (phase)
            {
                case Phase.Title: Home(); break;
                case Phase.Splash: Splash(); break;
                case Phase.Login: Login(); break;
                case Phase.Modes: ModeSelect(); break;
                case Phase.Team: TeamSetup(); break;
                case Phase.Online: OnlineRoom(); break;
                case Phase.Heroes: HeroSelect(); break;
                case Phase.Shop: Shop(); break;
                case Phase.Chapters: ChapterSelect(); break;
                case Phase.Loading: Busy("กำลังเตรียมห้อง…", "สุ่มห้องในเครื่อง"); break;
                case Phase.Scoring: Busy("กำลังทดสอบความสะอาด…", "คิดคะแนนในเครื่อง"); break;
                case Phase.Intro: RoomTicket(); break;
                case Phase.Test: if (SplitActive) SplitView(); else RoundResult(); break;
                case Phase.Result: MatchResult(); break;
            }
        }

        void MainMenu()
        {
            var S = screenRoot;
            ScreenBackdrop("lobby_rental_room");
            // top-right coins + settings
            var coin = Card(S, "Coins", BobUi.Surface, 1f); BobUi.Pin(coin, Vector2.one, Vector2.one, new Vector2(-150, -24), new Vector2(230, 96));
            BobUi.Pin(BobUi.Img(coin, "icon_star", 56).rectTransform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(16, 0), new Vector2(56, 56));
            var ctext = BobUi.Label(coin, coins.ToString(), 40, BobUi.Ink, TextAnchor.MiddleRight, FontStyle.Bold); BobUi.Stretch(ctext.rectTransform, 76, 0, 22, 0);
            var gear = BobUi.Button(S, "ตั้งค่า", BobUi.Surface, () => SetOverlay(Overlay.Settings), 24);
            BobUi.Pin((RectTransform)gear.transform, Vector2.one, Vector2.one, new Vector2(-24, -24), new Vector2(116, 96));

            // logo
            var logo = Card(S, "Logo", BobUi.Rose, 1f); BobUi.Pin(logo, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -40), new Vector2(330, 130));
            logo.localRotation = Quaternion.Euler(0, 0, -3);
            var lt = BobUi.Label(logo, "BOB", 92, BobUi.Surface, TextAnchor.MiddleCenter, FontStyle.Bold); BobUi.Stretch(lt.rectTransform);
            BobUi.OutlineText(lt, BobUi.Ink, 0.25f);
            var sub = H1(S, "สมรภูมิปัดกวาด", 40, BobUi.Surface); BobUi.Pin(sub.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -184), new Vector2(800, 56));
            BobUi.OutlineText(sub, BobUi.Ink, 0.3f);

            // hero showcase
            var h = Hero;
            var stage = BobUi.Disc(S, new Color(1, 1, 1, 0.22f), 470, "Stage"); BobUi.Pin(stage.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-300, -40), new Vector2(470, 470));
            var hero = BobUi.Rect("Hero", S); BobUi.Pin(hero, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-300, -30), new Vector2(460, 460));
            var himg = hero.gameObject.AddComponent<Image>(); himg.preserveAspect = true; himg.raycastTarget = false;
            var idle = art.Find(charId + "_idle");
            if (idle != null) { var an = hero.gameObject.AddComponent<BobSpriteAnimator>(); an.Play(idle.frames, idle.fps, true); } else himg.color = BobUi.Hex(h.Color);

            var info = Card(S, "HeroInfo"); BobUi.Pin(info, new Vector2(0.5f, 0.5f), new Vector2(0, 0.5f), new Vector2(-40, 40), new Vector2(620, 300));
            VStack(info, 32, 26, 10);
            var hn = BobUi.Label(info, h.Name, 60, BobUi.Hex(h.Color), TextAnchor.MiddleLeft, FontStyle.Bold);
            BobUi.OutlineText(hn, BobUi.Ink, 0.25f);
            var ab = BobUi.HBox(info, 10, "Ab"); ab.childForceExpandWidth = false; ab.childForceExpandHeight = false; BobUi.Size(ab, 56, 56);
            BobUi.Img(ab.transform, "icon_ability", 48);
            BobUi.Size(BobUi.Label(ab.transform, h.Ability, 36, BobUi.Ink, TextAnchor.MiddleLeft, FontStyle.Bold), 50, 50, 1);
            BobUi.Label(info, h.Desc + "\nใช้ได้รอบละครั้ง", 26, BobUi.Muted);

            // hero picker (bottom left) and PLAY (bottom right, thumb zone)
            var pick = BobUi.Pin(BobUi.Rect("Picker", S), Vector2.zero, Vector2.zero, new Vector2(40, 40), new Vector2(4 * 150 + 3 * 16, 150));
            var ph = pick.gameObject.AddComponent<HorizontalLayoutGroup>(); ph.spacing = 16; ph.childControlWidth = ph.childControlHeight = true;
            foreach (var c in BobMatch.Chars)
            {
                var cid = c.Id; bool sel = cid == charId;
                var b = BobUi.Button(pick, "", sel ? BobUi.Sun : BobUi.Surface, () => SelectChar(cid));
                b.GetComponentInChildren<TMP_Text>().text = "";
                var pi = BobUi.Img(b.transform, cid + "_portrait", 120); BobUi.Pin(pi.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 4), new Vector2(120, 120));
                if (sel) b.transform.localScale = Vector3.one * 1.08f;
            }
            var play = BobUi.Button(S, "เล่น", BobUi.Sun, StartMatch, 72);
            BobUi.Pin((RectTransform)play.transform, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-48, 40), new Vector2(440, 180));
            play.GetComponentInChildren<TMP_Text>().fontStyle = FontStyles.Bold;
            StartCoroutine(Breathe(play.transform));
        }

        IEnumerator Breathe(Transform t)
        {
            while (t != null && t.gameObject.activeInHierarchy) { float s = 1 + Mathf.Sin(Time.unscaledTime * 3) * 0.035f; t.localScale = new Vector3(s, s, 1); yield return null; }
        }

        void Busy(string title, string sub)
        {
            var card = Card(screenRoot, "Busy"); BobUi.Pin(card, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(760, 260));
            VStack(card, 40, 40, 14);
            BobUi.Label(card, title, 44, BobUi.Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
            BobUi.Label(card, sub, 26, BobUi.Muted, TextAnchor.MiddleCenter);
            var spin = BobUi.Img(screenRoot, "icon_star", 90); BobUi.Pin(spin.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 200), new Vector2(90, 90));
            StartCoroutine(Spin(spin.transform));
        }

        IEnumerator Spin(Transform t) { while (t != null && t.gameObject.activeInHierarchy) { t.Rotate(0, 0, -240 * Time.unscaledDeltaTime); yield return null; } }

        void RoomTicket()
        {
            var card = Card(screenRoot, "Ticket", BobUi.Bg); BobUi.Pin(card, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -10), new Vector2(1240, 0));
            VStack(card, 46, 34, 16);
            card.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var head = BobUi.HBox(card, 10, "Head"); head.childForceExpandHeight = false; BobUi.Size(head, 60, 60);
            BobUi.Size(BobUi.Label(head.transform, "ใบงาน · รอบ " + roundNo + "/3", 46, BobUi.Ink, TextAnchor.MiddleLeft, FontStyle.Bold), 60, 60, 1);
            BobUi.Size(BobUi.Label(head.transform, $"งบ {budget} · เวลา {RoundTime:0} วิ", 28, BobUi.Muted, TextAnchor.MiddleRight), 60, 60, 1);

            foreach (var id in R.Enemies)
            {
                var E = BobMatch.Enemy(id);
                var row = Card(card, "Foe", BobUi.Surface); BobUi.Size(row, 150, 150);
                var h = row.gameObject.AddComponent<HorizontalLayoutGroup>(); h.padding = new RectOffset(18, 18, 10, 10); h.spacing = 18;
                h.childControlWidth = h.childControlHeight = true; h.childForceExpandWidth = false; h.childForceExpandHeight = false; h.childAlignment = TextAnchor.MiddleLeft;
                var im = BobUi.Img(row, id + "_idle", 128); if (!art.Has(id + "_idle")) im.color = BobUi.Hex(E.Color);
                var idle = art.Find(id + "_idle"); if (idle != null) im.gameObject.AddComponent<BobSpriteAnimator>().Play(idle.frames, idle.fps, true);
                var t = BobUi.Label(row, $"<b><size=38>{E.Name}</size></b>  <color=#6B6E86>{E.Thai}</color>\nจุดอ่อน: {E.Tip}", 26); t.richText = true; BobUi.Size(t, 120, 120, 1);
            }
            var need = BobUi.HBox(card, 14, "Need"); need.childForceExpandWidth = false; need.childForceExpandHeight = false; BobUi.Size(need, 76, 76);
            BobUi.Size(BobUi.Label(need.transform, "ต้องผ่าน", 28, BobUi.Ink, TextAnchor.MiddleLeft, FontStyle.Bold), 70, 70, 0, 130);
            foreach (var kv in R.Need)
            {
                var chip = Card(need.transform, "Need", BobUi.Surface, 1.2f); BobUi.Size(chip, 70, 70, 0, 236);
                var ch = chip.gameObject.AddComponent<HorizontalLayoutGroup>(); ch.padding = new RectOffset(12, 14, 6, 6); ch.spacing = 8;
                ch.childControlWidth = ch.childControlHeight = true; ch.childForceExpandWidth = false; ch.childForceExpandHeight = false; ch.childAlignment = TextAnchor.MiddleLeft;
                BobUi.Img(chip, "stat_" + kv.Key, 44);
                BobUi.Size(BobUi.Label(chip, StatTh[kv.Key] + " " + kv.Value + "+", 26, BobUi.Ink, TextAnchor.MiddleLeft, FontStyle.Bold), 50, 50, 1);
            }
            if (R.Notes.Count > 0) BobUi.Label(card, "ลูกค้าขอเพิ่ม: " + string.Join(", ", R.Notes), 24, BobUi.Muted);
            var go = BobUi.Button(card, "ลุย!", BobUi.Sun, BeginBuild, 60); BobUi.Size(go, 140, 140);
            go.GetComponentInChildren<TMP_Text>().fontStyle = FontStyles.Bold;
            StartCoroutine(BobTween.Pop(card, 0.3f, 1.04f));
        }

        void RoundResult()
        {
            var L = last;
            var banner = H1(screenRoot, L.Win ? "ชนะรอบนี้!" : "คู่แข่งชนะรอบนี้", 84, L.Win ? BobUi.Sun : BobUi.Surface);
            BobUi.OutlineText(banner, BobUi.Ink, 0.3f);
            BobUi.Pin(banner.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -40), new Vector2(1200, 120));
            StartCoroutine(BobTween.Pop(banner.transform, 0.35f, 1.15f));

            Side(new Vector2(-330, 10), "ทีมเรา · " + Hero.Name, L.Mine, L.Stats, L.Stack, L.Win, true);
            Side(new Vector2(330, 10), "คู่แข่ง", L.Rival.Score, L.Rival.Ev.Stats, L.Rival.Stack, !L.Win, false);
            var vs = BobUi.Disc(screenRoot, BobUi.Rose, 120, "VS"); BobUi.Pin(vs.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 60), new Vector2(120, 120));
            var vt = BobUi.Label(vs.transform, "VS", 48, BobUi.Surface, TextAnchor.MiddleCenter, FontStyle.Bold); BobUi.Stretch(vt.rectTransform);

            var src = BobUi.Label(screenRoot, "คะแนนคิดในเครื่อง", 22, BobUi.Surface, TextAnchor.MiddleCenter);
            BobUi.Pin(src.rectTransform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 196), new Vector2(900, 34));
            var next = BobUi.Button(screenRoot, MatchOver ? "ดูผลแมตช์" : "รอบถัดไป", BobUi.Sun, AfterTest, 48);
            BobUi.Pin((RectTransform)next.transform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 40), new Vector2(520, 140));
            next.GetComponentInChildren<TMP_Text>().fontStyle = FontStyles.Bold;
        }

        void Side(Vector2 pos, string who, ScoreResult sc, int[] stats, List<string> st, bool winner, bool mine)
        {
            var card = Card(screenRoot, "Side", winner ? BobUi.Hex("#FFF0C2") : BobUi.Surface);
            BobUi.Pin(card, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), pos, new Vector2(560, 0));
            VStack(card, 30, 26, 10);
            card.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            BobUi.Label(card, who, 32, BobUi.Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
            var total = BobUi.Label(card, "0", 110, winner ? BobUi.Good : BobUi.Ink, TextAnchor.MiddleCenter, FontStyle.Bold); BobUi.Size(total, 130, 130);
            StartCoroutine(CountUp(total, sc.Total, 0.9f, mine));
            BobUi.Label(card, sc.Pass ? "ผ่านสเปก" : "ไม่ผ่านสเปก (คะแนนทดสอบหารครึ่ง)", 24, sc.Pass ? BobUi.Good : BobUi.Bad, TextAnchor.MiddleCenter, FontStyle.Bold);
            foreach (var kv in R.Need)
            {
                int k = System.Array.IndexOf(BobRules.StatKeys, kv.Key);
                BobUi.Label(card, $"{StatTh[kv.Key]}  {stats[k]} / {kv.Value}", 24, stats[k] >= kv.Value ? BobUi.Good : BobUi.Bad, TextAnchor.MiddleCenter);
            }
            BobUi.Label(card, $"ทดสอบ {Js(sc.Test)} · งบเหลือ {Js(sc.Budget)} · เวลา {Js(sc.Time)}", 20, BobUi.Muted, TextAnchor.MiddleCenter);
            var icons = BobUi.HBox(card, 4, "Stack"); icons.childForceExpandWidth = false; icons.childForceExpandHeight = false; icons.childAlignment = TextAnchor.MiddleCenter; BobUi.Size(icons, 60, 60);
            foreach (var id in st) BobUi.Img(icons.transform, "part_" + id, 54);
            if (st.Count == 0) BobUi.Label(icons.transform, "(ว่าง)", 22, BobUi.Muted);
        }

        IEnumerator CountUp(TMP_Text t, int target, float dur, bool sound)
        {
            float e = 0;
            while (e < dur && t != null) { e += Time.unscaledDeltaTime; t.text = Mathf.RoundToInt(target * BobTween.EaseOutCubic(Mathf.Clamp01(e / dur))).ToString(); yield return null; }
            if (t != null) { t.text = target.ToString(); if (sound) Sfx("score_count"); StartCoroutine(BobTween.Pop(t.transform, 0.2f, 1.15f)); }
        }

        void MatchResult()
        {
            bool w = wins > losses;
            var banner = H1(screenRoot, w ? "ชนะแมตช์!" : "แพ้แมตช์", 110, w ? BobUi.Sun : BobUi.Surface);
            BobUi.OutlineText(banner, BobUi.Ink, 0.3f);
            BobUi.Pin(banner.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -70), new Vector2(1200, 150));
            StartCoroutine(BobTween.Pop(banner.transform, 0.4f, 1.2f));

            var card = Card(screenRoot, "Summary"); BobUi.Pin(card, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 20), new Vector2(900, 420));
            VStack(card, 40, 30, 16);
            var pr = BobUi.HBox(card, 24, "Rounds"); pr.childForceExpandWidth = false; pr.childForceExpandHeight = false; pr.childAlignment = TextAnchor.MiddleCenter; BobUi.Size(pr, 110, 110);
            for (int i = 0; i < results.Count; i++)
            {
                var d = BobUi.Disc(pr.transform, results[i] ? BobUi.Good : BobUi.Rose, 100); BobUi.Size(d, 100, 100, 0, 100);
                var t = BobUi.Label(d.transform, results[i] ? "ชนะ" : "แพ้", 28, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold); BobUi.Stretch(t.rectTransform);
            }
            BobUi.Label(card, $"{wins} : {losses}  ·  {(w ? "ทุกคราบโบกมือลา" : "คราบยังอยู่ แต่ทีมได้ประสบการณ์")}", 34, BobUi.Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
            var cr = BobUi.HBox(card, 12, "Coins"); cr.childForceExpandWidth = false; cr.childForceExpandHeight = false; cr.childAlignment = TextAnchor.MiddleCenter; BobUi.Size(cr, 80, 80);
            BobUi.Img(cr.transform, "icon_star", 70);
            var ct = BobUi.Label(cr.transform, "+0", 60, BobUi.Ink, TextAnchor.MiddleLeft, FontStyle.Bold); BobUi.Size(ct, 80, 80, 0, 200);
            StartCoroutine(CountCoins(ct, gain));
            BobUi.Label(card, "เหรียญรวม " + coins, 24, BobUi.Muted, TextAnchor.MiddleCenter);
            BobUi.Label(card, $"XP +{xpGain}  ·  ระดับ {Level}", 28, BobUi.Ink, TextAnchor.MiddleCenter, FontStyle.Bold);

            var again = BobUi.Button(screenRoot, "เล่นอีกครั้ง", BobUi.Sun, StartMatch, 46);
            BobUi.Pin((RectTransform)again.transform, new Vector2(0.5f, 0), new Vector2(0, 0), new Vector2(20, 50), new Vector2(460, 140));
            again.GetComponentInChildren<TMP_Text>().fontStyle = FontStyles.Bold;
            var home = BobUi.Button(screenRoot, "หน้าหลัก", BobUi.Surface, ToTitle, 40);
            BobUi.Pin((RectTransform)home.transform, new Vector2(0.5f, 0), new Vector2(1, 0), new Vector2(-20, 50), new Vector2(360, 140));
        }

        IEnumerator CountCoins(TMP_Text t, int n)
        {
            for (int i = 0; i <= n && t != null; i += 2) { t.text = "+" + i; if (i % 6 == 0) Sfx("coin"); yield return new WaitForSecondsRealtime(0.03f); }
            if (t != null) t.text = "+" + n;
        }

        // ================================================================ pause / settings

        void SetOverlay(Overlay o)
        {
            overlay = o;
            // Every open of the board asks again: a score may have landed since the last look.
            if (o == Overlay.Board) { boardSnap = null; boardLoading = false; }
            // Same for the daily: the day may have turned or an attempt may have been spent since.
            if (o == Overlay.Daily) { dailyReply = null; dailyBusy = false; dailySnap = null; dailyBoardBusy = false; }
            // The sewing draft starts from what is worn, so the card never opens on an empty table.
            if (o == Overlay.Wardrobe)
                sewPicks = BobCosmetics.SewnPicks ?? new int[Bob.Rules.BobSewing.LayerCount];
            Time.timeScale = o != Overlay.None && phase == Phase.Build ? 0 : 1;
            joyActive = false;
            Sfx(o == Overlay.None ? "ui_cancel" : "ui_tap");
            RenderOverlay();
        }

        void RenderOverlay()
        {
            if (overlayRoot == null) return;
            overlayRoot.gameObject.SetActive(overlay != Overlay.None);
            if (overlay == Overlay.None) return;
            BobUi.Clear(overlayRoot);
            var card = Card(overlayRoot, "Card", BobUi.Bg); BobUi.Pin(card, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(760, 0));
            VStack(card, 46, 36, 18);
            card.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            if (overlay == Overlay.Pause)
            {
                BobUi.Label(card, "หยุดเกม", 52, BobUi.Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
                BobUi.Size(BobUi.Button(card, "เล่นต่อ", BobUi.Sun, () => SetOverlay(Overlay.None), 40), 120, 120);
                BobUi.Size(BobUi.Button(card, "ตั้งค่า", BobUi.Surface, () => SetOverlay(Overlay.Settings), 34), 104, 104);
                BobUi.Size(BobUi.Button(card, "เริ่มแมตช์ใหม่", BobUi.Surface, () => { SetOverlay(Overlay.None); StartMatch(); }, 34), 104, 104);
                BobUi.Size(BobUi.Button(card, "ออกไปหน้าหลัก", BobUi.Surface, () => { SetOverlay(Overlay.None); ToTitle(); }, 34), 104, 104);
                return;
            }
            if (overlay == Overlay.Board) { Board(card); return; }
            if (overlay == Overlay.Daily) { Daily(card); return; }
            if (overlay == Overlay.Wardrobe) { Wardrobe(card); return; }
            BobUi.Label(card, "ตั้งค่า", 52, BobUi.Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
            BobUi.Size(BobUi.Button(card, "เสียงเอฟเฟกต์: " + (soundOn ? "เปิด" : "ปิด"), soundOn ? BobUi.Sun : BobUi.Surface, () => { SetSound(!soundOn); RenderOverlay(); }, 34), 104, 104);
            BobUi.Size(BobUi.Button(card, "เพลง: " + (musicOn ? "เปิด" : "ปิด"), musicOn ? BobUi.Sun : BobUi.Surface, () => { SetMusic(!musicOn); RenderOverlay(); }, 34), 104, 104);
            BobUi.Size(BobUi.Button(card, "ปิด", BobUi.Surface, () => SetOverlay(phase == Phase.Build ? Overlay.Pause : Overlay.None), 34), 104, 104);
        }

        // ---------------------------------------------------------------- board (docs 31 section 3)

        /// <summary>
        /// The leaderboard card: one bar per row, length against the leader, my own row in sun yellow. No table of digits:
        /// rank in a disc, the name, a bar and the number at the end. Offline it says so and shows the device best.
        /// </summary>
        void Board(RectTransform card)
        {
            BobUi.Pin(card, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(980f, 0f));
            BobUi.Label(card, "อันดับ", 52, BobUi.Ink, TextAnchor.MiddleCenter, FontStyle.Bold);

            if (boardLoading)
            {
                BobUi.Label(card, "กำลังโหลด…", 30, BobUi.Muted, TextAnchor.MiddleCenter);
            }
            else if (boardSnap == null)
            {
                LoadBoard();
                BobUi.Label(card, "กำลังโหลด…", 30, BobUi.Muted, TextAnchor.MiddleCenter);
            }
            else if (!boardSnap.SignedIn)
            {
                BobUi.Label(card, "ออฟไลน์ · เล่นจบแมตช์เพื่อเก็บคะแนน", 28, BobUi.Muted, TextAnchor.MiddleCenter);
                BobUi.Label(card, $"ดีสุดในเครื่อง {Bob.TaskRace.BobTaskRaceProfile.BestPoints} แต้ม", 34, BobUi.Ink,
                    TextAnchor.MiddleCenter, FontStyle.Bold);
            }
            else if (boardSnap.Rows.Count == 0 && boardSnap.SelfRank <= 0)
            {
                BobUi.Label(card, boardSnap.Error != null ? "อ่านอันดับไม่ได้ · ลองใหม่ภายหลัง" : "ยังไม่มีใครมีคะแนน · เริ่มเลย",
                    28, BobUi.Muted, TextAnchor.MiddleCenter);
            }
            else
            {
                int top = 0;
                for (int i = 0; i < boardSnap.Rows.Count; i++) if (boardSnap.Rows[i].Score > top) top = boardSnap.Rows[i].Score;
                for (int i = 0; i < boardSnap.Rows.Count; i++) BoardRow(card, boardSnap.Rows[i], top);

                bool shown = false;
                for (int i = 0; i < boardSnap.Rows.Count; i++) if (boardSnap.Rows[i].Self) shown = true;
                string mine = boardSnap.SelfRank > 0
                    ? $"ของคุณ # {boardSnap.SelfRank} · {boardSnap.SelfScore} คะแนน"
                    : "ยังไม่มีคะแนนของคุณ · เล่นหนึ่งแมตช์";
                BobUi.Label(card, shown ? "แถวสีเหลืองคือคุณ" : mine, 28,
                    shown ? BobUi.Muted : BobUi.Teal, TextAnchor.MiddleCenter, shown ? FontStyle.Normal : FontStyle.Bold);
            }

            BobUi.Size(BobUi.Button(card, "ปิด", BobUi.Surface, () => SetOverlay(phase == Phase.Build ? Overlay.Pause : Overlay.None), 34), 104, 104);
        }

        /// <summary>One board line: a rank disc, the name, a bar scaled to the leader and the score on the right.</summary>
        void BoardRow(RectTransform card, BobBoardRow row, int top)
        {
            var line = BobUi.Panel(card, row.Self ? BobUi.Sun : new Color(1f, 1f, 1f, 0.62f), "ui_panel", "Row");
            BobUi.Size(line, 76, 76, 1);

            var disc = BobUi.Disc(line.transform, row.Self ? BobUi.Bg : BobUi.Sun, 52, "Rank");
            BobUi.Pin((RectTransform)disc.transform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(12f, 0f), new Vector2(52f, 52f));
            var rn = BobUi.Label(disc.transform, row.Rank.ToString(), 26, BobUi.Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
            BobUi.Stretch(rn.rectTransform);

            var nm = BobUi.Label(line.transform, row.Name, 28, BobUi.Ink, TextAnchor.MiddleLeft,
                row.Self ? FontStyle.Bold : FontStyle.Normal);
            BobUi.Pin(nm.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(78f, 0f), new Vector2(290f, 40f));

            var track = BobUi.Rect("Track", line.transform);
            var timg = track.gameObject.AddComponent<Image>();
            timg.color = new Color(0.90f, 0.86f, 0.77f, 1f); timg.raycastTarget = false;
            BobUi.Pin(track, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(380f, 0f), new Vector2(300f, 18f));
            var fill = BobUi.Rect("Fill", track);
            var fimg = fill.gameObject.AddComponent<Image>();
            fimg.color = row.Self ? BobUi.Ink : BobUi.Teal; fimg.raycastTarget = false;
            fill.anchorMin = Vector2.zero;
            fill.anchorMax = new Vector2(BobRaceScore.BarFraction(row.Score, top), 1f);
            fill.offsetMin = fill.offsetMax = Vector2.zero;

            var sc = BobUi.Label(line.transform, row.Score.ToString(), 30, BobUi.Ink, TextAnchor.MiddleRight, FontStyle.Bold);
            BobUi.Pin(sc.rectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-16f, 0f), new Vector2(190f, 44f));
        }

        // ---------------------------------------------------------------- daily challenge (docs 31 section 2.2)

        /// <summary>
        /// The daily card: the server's day, how many attempts are left today and one button. The seed is the server's,
        /// so a card that could not reach it offers a retry, not a made-up round. A spent day says when it refills.
        /// </summary>
        void Daily(RectTransform card)
        {
            BobUi.Pin(card, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(900f, 0f));
            BobUi.Label(card, "เดลี่วันนี้", 52, BobUi.Ink, TextAnchor.MiddleCenter, FontStyle.Bold);

            if (dailyReply == null)
            {
                if (!dailyBusy) LoadDaily();
                BobUi.Label(card, "กำลังถามเซิร์ฟเวอร์…", 30, BobUi.Muted, TextAnchor.MiddleCenter);
            }
            else if (!dailyReply.Valid)
            {
                BobUi.Label(card, "ออฟไลน์ · เล่นโหมดปกติไปก่อน", 30, BobUi.Muted, TextAnchor.MiddleCenter);
                BobUi.Label(card, dailyReply.error ?? "อ่านเดลี่ไม่ได้", 24, BobUi.Muted, TextAnchor.MiddleCenter);
                BobUi.Size(BobUi.Button(card, "ลองใหม่", BobUi.Surface, () => { dailyReply = null; dailyBusy = false; RenderOverlay(); }, 34), 104, 104);
            }
            else
            {
                int left = BobDailyStore.AttemptsLeft(dailyReply.dayNumber);
                BobUi.Label(card, "วัน " + dailyReply.day, 34, BobUi.Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
                BobUi.Label(card, left > 0 ? "เหลือ " + left + " รอบวันนี้" : "เล่นแล้ววันนี้ · กลับมาพรุ่งนี้",
                    left > 0 ? 40 : 34, left > 0 ? BobUi.Teal : BobUi.Muted, TextAnchor.MiddleCenter,
                    left > 0 ? FontStyle.Bold : FontStyle.Normal);
                BobUi.Label(card, "seed " + dailyReply.seed + " · ทุกคนได้ชุดงานเดียวกัน", 24, BobUi.Muted, TextAnchor.MiddleCenter);
                if (left > 0)
                    BobUi.Size(BobUi.Button(card, "เล่นเดลี่", BobUi.Sun, StartDaily, 44), 130, 130);
                else
                    BobUi.Label(card, "เปิดใหม่ตอน 00:00 น.", 24, BobUi.Muted, TextAnchor.MiddleCenter);
                DailyBoardRows(card);
            }

            BobUi.Size(BobUi.Button(card, "ปิด", BobUi.Surface, () => SetOverlay(phase == Phase.Build ? Overlay.Pause : Overlay.None), 34), 104, 104);
        }

        /// <summary>The daily round: one timed solo round on the server's seed, spent when it ends (MatchRequest.Daily).</summary>
        void StartDaily()
        {
            if (dailyReply == null || !dailyReply.Valid) { Toast("ยังไม่ได้ seed ของวันนี้"); return; }
            if (BobDailyStore.AttemptsLeft(dailyReply.dayNumber) <= 0) { Toast("เล่นเดลี่วันนี้แล้ว"); return; }
            Sfx("ui_confirm");
            MatchRequest.SetDaily(dailyReply.dayNumber, dailyReply.seed);
            SetOverlay(Overlay.None);
            LoadMatchScene("TaskRace");
        }

        /// <summary>
        /// The day's own board under the daily card (docs 31 section 2.2). It reads only BobDaily.DailyBoardName(day),
        /// so yesterday's runs never mix in; it marks no Self row because a daily page has no stable device rank.
        /// </summary>
        void DailyBoardRows(RectTransform card)
        {
            BobUi.Label(card, "อันดับเดลี่วันนี้ · seed เดียวกันทุกคน", 24, BobUi.Muted, TextAnchor.MiddleCenter);
            if (dailySnap == null)
            {
                if (!dailyBoardBusy) LoadDailyBoard();
                BobUi.Label(card, "กำลังโหลดอันดับ…", 22, BobUi.Muted, TextAnchor.MiddleCenter);
                return;
            }
            if (!dailySnap.SignedIn)
            {
                BobUi.Label(card, "ออฟไลน์ · ยังดูอันดับวันนี้ไม่ได้", 22, BobUi.Muted, TextAnchor.MiddleCenter);
                return;
            }
            if (dailySnap.Rows.Count == 0)
            {
                BobUi.Label(card, dailySnap.Error != null ? "อ่านอันดับวันนี้ไม่ได้" : "วันนี้ยังไม่มีใครส่งคะแนน",
                    22, BobUi.Muted, TextAnchor.MiddleCenter);
                return;
            }
            int top = 0;
            for (int i = 0; i < dailySnap.Rows.Count; i++) if (dailySnap.Rows[i].Score > top) top = dailySnap.Rows[i].Score;
            for (int i = 0; i < dailySnap.Rows.Count; i++) BoardRow(card, dailySnap.Rows[i], top);
        }

        void LoadDailyBoard()
        {
            if (dailyBoardBusy || dailyReply == null || !dailyReply.Valid) return;
            dailyBoardBusy = true;
            _ = LoadDailyBoardAsync(dailyReply.dayNumber);
        }

        async System.Threading.Tasks.Task LoadDailyBoardAsync(int day)
        {
            var snap = await BobBoardService.LoadDailyAsync(day, 5);
            dailyBoardBusy = false;
            dailySnap = snap ?? new BobBoardSnapshot { Error = "offline" };
            if (overlay == Overlay.Daily) RenderOverlay();
        }

        // ---------------------------------------------------------------- wardrobe (docs 31 section 2.3)

        /// <summary>
        /// S31, the wardrobe (docs 31 section 2.3, docs 11 S31): three tabs over the six-piece table and the sewing
        /// table, a preview that shows the worn title, the frame on the profile disc, the sewn cloth and the paint on
        /// the T04 cart, and one row per piece with its own progress. It sells nothing and changes no rule — the shop
        /// sells tools, this screen wears decoration.
        /// </summary>
        void Wardrobe(RectTransform card)
        {
            BobUi.Pin(card, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(980f, 0f));
            GrantUnlocks(); // an earned piece is granted the moment the screen opens, so no row is ever stuck
            var stats = BobCosmetics.Stats();
            BobUi.Label(card, "ตู้เสื้อผ้า", 52, BobUi.Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
            BobUi.Label(card, $"ปลดแล้ว {BobUnlocks.EarnedCount(stats)} จาก {BobUnlocks.All.Length} · ของแต่งไม่เพิ่มพลัง",
                26, BobUi.Muted, TextAnchor.MiddleCenter);

            var tabs = BobUi.HBox(card, 12, "Tabs");
            tabs.childForceExpandHeight = false; tabs.childAlignment = TextAnchor.MiddleCenter;
            BobUi.Size(tabs, 84, 84);
            var titles = BobUnlocks.Of(true);
            var frames = BobUnlocks.Of(false);
            WardrobeTab(tabs.transform, 0, "ป้าย " + Have(titles) + "/" + titles.Length);
            WardrobeTab(tabs.transform, 1, "กรอบ " + Have(frames) + "/" + frames.Length);
            WardrobeTab(tabs.transform, 2, "ตัดเย็บ");
            WardrobeTab(tabs.transform, 3, "กระเป๋า");

            WardrobePreview(card);

            if (wardrobeTab == 2)
            {
                SewTable(card);
            }
            else if (wardrobeTab == 3)
            {
                Bag(card);
            }
            else
            {
                var list = BobUnlocks.Of(wardrobeTab == 0);
                if (list.Length == 0) BobUi.Label(card, "ยังไม่มีของในหมวดนี้", 26, BobUi.Muted, TextAnchor.MiddleCenter);
                for (int i = 0; i < list.Length; i++) WardrobeRow(card, list[i], stats);
            }

            var foot = BobUi.HBox(card, 12, "Foot");
            foot.childForceExpandHeight = false; foot.childAlignment = TextAnchor.MiddleCenter;
            BobUi.Size(foot, 100, 100);
            BobUi.Size(BobUi.Button(foot.transform, "ถอดทั้งหมด", BobUi.Surface,
                () => { BobCosmetics.Equip(""); BobCosmetics.ClearSewn(); Sfx("ui_tap"); RenderOverlay(); }, 30), 96, 96, 0.5f);
            BobUi.Size(BobUi.Button(foot.transform, "ปิด", BobUi.Surface,
                () => SetOverlay(phase == Phase.Build ? Overlay.Pause : Overlay.None), 34), 96, 96, 0.5f);
        }

        /// <summary>How many pieces of one tab are owned.</summary>
        static int Have(UnlockDef[] list)
        {
            int have = 0;
            for (int i = 0; i < list.Length; i++) if (BobCosmetics.IsOwned(list[i].Id)) have++;
            return have;
        }

        /// <summary>One wardrobe tab: the two cosmetic tables plus the sewing table (docs 11 S31).</summary>
        void WardrobeTab(Transform tabs, int index, string label)
        {
            bool active = wardrobeTab == index;
            var b = BobUi.Button(tabs, label, active ? BobUi.Sun : BobUi.Surface, () =>
                {
                    if (wardrobeTab == index) return;
                    wardrobeTab = index; Sfx("ui_tap"); RenderOverlay();
                }, 28, active ? BobUi.Ink : BobUi.Muted);
            BobUi.Size(b, 64, 64, 0.5f);
        }

        /// <summary>
        /// What the pieces look like before committing: the worn title, the frame on the stage disc, and the same paint
        /// the T04 card will use (BobCosmetics.CartPaint), so the preview cannot drift from the game.
        /// </summary>
        void WardrobePreview(RectTransform card)
        {
            var box = BobUi.Panel(card, new Color(1f, 1f, 1f, 0.55f), "ui_panel", "Preview");
            BobUi.Size(box, 190, 190, 1);

            var disc = BobUi.Disc(box.transform, BobCosmetics.FrameTint, 140, "Stage");
            BobUi.Pin((RectTransform)disc.transform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(24f, 0f), new Vector2(140, 140));
            var cap = BobUi.Label(box.transform, "กรอบ", 20, BobUi.Muted, TextAnchor.MiddleCenter);
            BobUi.Pin(cap.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(24f, -76f), new Vector2(140, 24f));

            string title = BobCosmetics.TitleName;
            var head = BobUi.Label(box.transform, string.IsNullOrEmpty(title) ? "ยังไม่ใส่ป้าย" : title, 30,
                string.IsNullOrEmpty(title) ? BobUi.Muted : BobUi.Ink, TextAnchor.MiddleLeft, FontStyle.Bold);
            BobUi.Pin(head.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(190f, 52f), new Vector2(430f, 36f));
            var sub = BobUi.Label(box.transform, string.IsNullOrEmpty(title)
                    ? "เลือกป้ายจากแท็บ “ป้ายชื่อ” เพื่อแสดงใต้ชื่อฮีโร่"
                    : "ป้ายใต้ชื่อฮีโร่บนหน้าหลัก", 20, BobUi.Muted, TextAnchor.MiddleLeft);
            BobUi.Pin(sub.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(190f, 20f), new Vector2(430f, 28f));

            var frameName = BobCosmetics.HasFrame ? BobUnlocks.Get(BobCosmetics.FrameId).Name : "กรอบมาตรฐาน";
            var frameLine = BobUi.Label(box.transform, frameName, 26, BobUi.Ink, TextAnchor.MiddleLeft, FontStyle.Bold);
            BobUi.Pin(frameLine.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(190f, -22f), new Vector2(430f, 32f));
            var frameSub = BobUi.Label(box.transform, "จานรอง · แถบ XP · สีรถเข็น T04", 20, BobUi.Muted, TextAnchor.MiddleLeft);
            BobUi.Pin(frameSub.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(190f, -50f), new Vector2(430f, 28f));

            // The sewn cloth is the third decoration: when one is worn it is what the disc and the cart above show.
            var sewnLine = BobUi.Label(box.transform,
                BobCosmetics.HasSewn ? "ชุดตัดเย็บ · " + BobCosmetics.SewnName : "ยังไม่ได้ตัดชุด",
                20, BobCosmetics.HasSewn ? BobUi.Teal : BobUi.Muted, TextAnchor.MiddleLeft);
            BobUi.Pin(sewnLine.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(190f, -76f), new Vector2(430f, 28f));

            // The T04 cart in the same paint the card uses: the preview is the promise, not a second design.
            var cart = BobUi.Panel(box.transform, BobCosmetics.CartPaint, "ui_panel", "CartPreview");
            BobUi.Pin((RectTransform)cart.transform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-24f, 6f), new Vector2(190f, 120f));
            if (BobCosmetics.HasFrame)
            {
                var rim = BobUi.Rect("Rim", cart.transform); var img = rim.gameObject.AddComponent<Image>();
                img.raycastTarget = false; img.color = BobCosmetics.FrameColour;
                BobUi.Pin(rim, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 4f), new Vector2(182f, 6f));
            }
            var cartCap = BobUi.Label(box.transform, "รถเข็น T04", 20, BobUi.Muted, TextAnchor.MiddleCenter);
            BobUi.Pin(cartCap.rectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-24f, -66f), new Vector2(190f, 24f));
        }

        /// <summary>
        /// The sewing table (docs 11 S31 P1): four layers — fabric, colour, pattern, accessory — each a row of chips,
        /// the price in coins and material shards, the cloth it will make, and one button that pays and wears it.
        /// Shards are earned from matches (BobSewing.ShardsEarned) and spent here, so the table cannot be free.
        /// </summary>
        void SewTable(RectTransform card)
        {
            var picks = SewDraft();
            int coins = BobSewing.Coins(picks), shards = BobSewing.Shards(picks);
            bool canPay = Bob.TaskRace.BobTaskRaceProfile.Coins >= coins && BobCosmetics.SewShardsLeft >= shards;

            BobUi.Label(card, BobSewing.Describe(picks), 28, BobUi.Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
            BobUi.Label(card,
                $"ราคา {coins} เหรียญ + {shards} เกล็ด · มี {Bob.TaskRace.BobTaskRaceProfile.Coins} เหรียญ · เกล็ดเหลือ {BobCosmetics.SewShardsLeft}",
                22, canPay ? BobUi.Muted : BobUi.Rose, TextAnchor.MiddleCenter);

            for (int layer = 0; layer < BobSewing.LayerCount; layer++) SewLayerRow(card, layer, picks);

            var foot = BobUi.HBox(card, 12, "SewFoot");
            foot.childForceExpandHeight = false; foot.childAlignment = TextAnchor.MiddleCenter;
            BobUi.Size(foot, 84, 84);

            // The cloth the draft makes, beside the button: the preview is the promise, as on the cosmetic tabs.
            var cloth = BobSewing.Cloth(picks);
            var sw = BobUi.Panel(foot.transform, new Color(cloth.R, cloth.G, cloth.B, 1f), "ui_panel", "SewSwatch");
            BobUi.Size(sw, 64, 64, 0f, 84f);
            var name = BobUi.Label(foot.transform, BobCosmetics.HasSewn && BobCosmetics.SewnText == BobSewing.Encode(picks)
                ? "ชุดนี้ใส่อยู่" : "ตัวอย่างผ้า", 22, BobUi.Muted, TextAnchor.MiddleLeft);
            BobUi.Size(name, 64, 64, 1f);

            var buy = BobUi.Button(foot.transform, "ตัดชุดนี้", canPay ? BobUi.Sun : BobUi.Surface,
                () => BuySew(picks), 26);
            BobUi.Size(buy, 64, 64, 0f, 180f);
        }

        /// <summary>The draft look, created on first use; always exactly four valid indices.</summary>
        int[] SewDraft()
        {
            if (sewPicks == null || sewPicks.Length != BobSewing.LayerCount || !BobSewing.Complete(sewPicks))
                sewPicks = new int[BobSewing.LayerCount];
            return sewPicks;
        }

        /// <summary>One layer: its Thai name on the left, then one chip per choice with its extra price.</summary>
        void SewLayerRow(RectTransform card, int layer, int[] picks)
        {
            var row = BobUi.HBox(card, 6, "SewLayer" + layer);
            row.childForceExpandHeight = false; row.childAlignment = TextAnchor.MiddleCenter;
            BobUi.Size(row, 60, 60);

            var head = BobUi.Label(row.transform, BobSewing.LayerNames[layer], 22, BobUi.Muted, TextAnchor.MiddleLeft);
            BobUi.Size(head, 54, 54, 0f, 104f);

            var options = BobSewing.Layers[layer];
            for (int i = 0; i < options.Length; i++)
            {
                int pick = i;
                bool on = picks[layer] == pick;
                var opt = options[pick];
                string price = opt.Coins > 0 ? " · " + opt.Coins : "";
                string shard = opt.Shards > 0 ? "+" + opt.Shards + " เศษ" : "";
                var b = BobUi.Button(row.transform, opt.Name + price + shard,
                    on ? BobUi.Sun : BobUi.Surface,
                    () => { SewDraft()[layer] = pick; Sfx("ui_tap"); RenderOverlay(); },
                    20, on ? BobUi.Ink : BobUi.Muted);
                BobUi.Size(b, 54, 54, 1f);
            }
        }

        /// <summary>
        /// Pay for the draft and wear it. Both prices are checked before anything is taken, so a failed cut never
        /// leaves the wallet short or the shard pile half-spent.
        /// </summary>
        void BuySew(int[] picks)
        {
            int coins = BobSewing.Coins(picks), shards = BobSewing.Shards(picks);
            if (!BobSewing.Complete(picks)) { Toast("ยังเลือกไม่ครบสี่ชั้น"); return; }
            if (Bob.TaskRace.BobTaskRaceProfile.Coins < coins) { Toast("เหรียญไม่พอ"); return; }
            if (BobCosmetics.SewShardsLeft < shards) { Toast("เกล็ดไม่พอ · เล่นแมตช์เพิ่ม"); return; }
            if (!Bob.TaskRace.BobTaskRaceProfile.Spend(coins) || !BobCosmetics.SpendSewShards(shards))
            {
                Toast("ตัดชุดไม่สำเร็จ");
                return;
            }
            if (!BobCosmetics.WearSewn(picks)) { Toast("ตัดชุดไม่สำเร็จ"); return; }
            Sfx("ui_confirm");
            Toast("ตัดชุดแล้ว · " + BobSewing.Describe(picks));
            RenderOverlay();
        }

        /// <summary>
        /// S33, the bag (docs 11 S33): the read-only half of the wardrobe. It counts what the account owns, shows the
        /// sewn look and each owned piece as a tile, and prints the material line the sewing table spends. Nothing here
        /// is a button that buys or sells — BobBag.SellNote says so where a shop would put its sell price.
        /// </summary>
        void Bag(RectTransform card)
        {
            var owned = BobCosmetics.OwnedIds();
            var counts = BobBag.Count(owned);
            BobUi.Label(card, BobBag.Headline(counts), 30, BobUi.Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
            BobUi.Label(card,
                BobBag.Materials(BobSewing.ShardsEarned(Bob.TaskRace.BobTaskRaceProfile.Matches), BobCosmetics.SewShardsLeft),
                22, BobUi.Muted, TextAnchor.MiddleCenter);

            var names = new List<string>();
            var colours = new List<Color>();
            var wheres = new List<string>();
            if (BobCosmetics.HasSewn)
            {
                names.Add(BobCosmetics.SewnName); colours.Add(BobCosmetics.ClothColour);
                wheres.Add("ชุดตัดเย็บ · อยู่ในรอบ");
            }
            for (int i = 0; i < BobUnlocks.All.Length; i++)
            {
                var def = BobUnlocks.All[i];
                if (def == null || !owned.Contains(def.Id)) continue;
                names.Add(def.Name); colours.Add(def.Title ? BobUi.Teal : BobUi.Hex(def.Hex));
                wheres.Add(BobUnlocks.Where(def));
            }

            if (names.Count == 0)
            {
                BobUi.Label(card, "กระเป๋าว่าง", 26, BobUi.Muted, TextAnchor.MiddleCenter);
            }
            else
            {
                for (int i = 0; i < names.Count; i += 3)
                {
                    var row = BobUi.HBox(card, 8, "BagRow");
                    row.childForceExpandHeight = false; row.childAlignment = TextAnchor.MiddleCenter;
                    BobUi.Size(row, 116, 116);
                    for (int j = i; j < i + 3 && j < names.Count; j++) BagTile(row.transform, names[j], colours[j], wheres[j]);
                    for (int j = names.Count; j < i + 3; j++) BobUi.Size(BobUi.Rect("Gap", row.transform), 116, 116, 1);
                }
            }

            BobUi.Label(card, BobBag.SellNote, 22, BobUi.Muted, TextAnchor.MiddleCenter);
        }

        /// <summary>One owned piece in the bag: its colour, its name and where it shows. Never interactive.</summary>
        void BagTile(Transform parent, string name, Color colour, string where)
        {
            var tile = BobUi.Panel(parent, new Color(1f, 1f, 1f, 0.62f), "ui_panel", "BagTile");
            BobUi.Size(tile, 112, 112, 1);

            var sw = BobUi.Rect("Swatch", tile.transform);
            var img = sw.gameObject.AddComponent<Image>();
            img.raycastTarget = false; img.color = colour;
            BobUi.Pin(sw, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(10f, -10f), new Vector2(40f, 40f));

            var nm = BobUi.Label(tile.transform, name, 20, BobUi.Ink, TextAnchor.UpperLeft, FontStyle.Bold);
            BobUi.Pin(nm.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(58f, -10f), new Vector2(180f, 40f));

            var wh = BobUi.Label(tile.transform, where, 16, BobUi.Muted, TextAnchor.LowerLeft);
            BobUi.Pin(wh.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(10f, 8f), new Vector2(250f, 40f));
        }

        /// <summary>One cosmetic: a swatch, the name, where it shows (or what it asks), and ใช้ / ใช้อยู่ / ยังไม่ปลด.</summary>
        void WardrobeRow(RectTransform card, UnlockDef def, BobUnlockStats stats)
        {
            bool has = owned.Contains(def.Id);
            bool worn = def.Title ? BobCosmetics.TitleId == def.Id : BobCosmetics.FrameId == def.Id;
            var line = BobUi.Panel(card, worn ? BobUi.Sun : new Color(1f, 1f, 1f, 0.62f), "ui_panel", "Row");
            BobUi.Size(line, 84, 84, 1);

            var sw = BobUi.Rect("Swatch", line.transform);
            var simg = sw.gameObject.AddComponent<Image>();
            simg.raycastTarget = false;
            simg.color = has ? (def.Title ? BobUi.Teal : BobUi.Hex(def.Hex)) : new Color(0.78f, 0.75f, 0.70f, 1f);
            BobUi.Pin(sw, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(14f, 0f), new Vector2(44f, 44f));

            var nm = BobUi.Label(line.transform, def.Name, 28, BobUi.Ink, TextAnchor.MiddleLeft,
                worn ? FontStyle.Bold : FontStyle.Normal);
            BobUi.Pin(nm.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(74f, 12f), new Vector2(460f, 34f));
            // Owned pieces say where the decoration shows; locked ones say how far along the player is.
            var req = BobUi.Label(line.transform, has ? BobUnlocks.Where(def) : BobUnlocks.Requirement(def, stats), 22,
                has ? BobUi.Teal : BobUi.Muted, TextAnchor.MiddleLeft);
            BobUi.Pin(req.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(74f, -20f), new Vector2(460f, 28f));

            if (has && !worn)
            {
                var use = BobUi.Button(line.transform, "ใช้", BobUi.Sun, () => { BobCosmetics.Equip(def.Id); Sfx("ui_confirm"); RenderOverlay(); }, 28);
                BobUi.Pin((RectTransform)use.transform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-14f, 0f), new Vector2(130f, 60f));
            }
            else
            {
                var tag = BobUi.Label(line.transform, has ? "ใช้อยู่" : "ยังไม่ปลด", 24,
                    has ? BobUi.Ink : BobUi.Muted, TextAnchor.MiddleRight, has ? FontStyle.Bold : FontStyle.Normal);
                BobUi.Pin(tag.rectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-18f, 0f), new Vector2(200f, 40f));
            }
        }

        void LoadDaily()
        {
            if (dailyBusy) return;
            dailyBusy = true;
            StartCoroutine(BobDailyFeed.Fetch(r =>
            {
                dailyBusy = false;
                dailyReply = r;
                if (overlay == Overlay.Daily) RenderOverlay();
            }));
        }

        void LoadBoard()
        {
            if (boardLoading) return;
            boardLoading = true;
            _ = LoadBoardAsync();
        }

        async System.Threading.Tasks.Task LoadBoardAsync()
        {
            var snap = await BobBoardService.LoadAsync(10);
            boardLoading = false;
            boardSnap = snap;
            if (overlay == Overlay.Board) RenderOverlay();
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
