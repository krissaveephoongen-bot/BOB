// Battle of the Brooms: GDD sections 9 and 20 gameplay that is not in the core loop.
//  - Pushing the cart: walk into it (solo). Park it next to the shelves you need. Online the server owns the cart position, so it stays put.
//  - Item rest table (โต๊ะพักของ): stash up to 2 hand parts, take them back later (solo; online needs a server op).
//  - Split-screen test view: our room and the rival's side by side for a few seconds before the scores (GDD 9).
using System.Collections;
using System.Collections.Generic;
using Bob.Art;
using Bob.Modular;
using Bob.Rules;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Bob.Game
{
    public partial class BobGame
    {
        // ---------------------------------------------------------------- cart push

        bool pushHinted, pushedFrame;
        Vector2 cartVel; float cartTilt, cartDust;
        SpriteRenderer cartShadow;
        float nextPushHud;

        /// <summary>Put the cart sprite where cartRect says (bottom-pivot, or a coloured box without art).</summary>
        void PlaceCart()
        {
            if (cartT == null) return;
            if (art.Has("cart")) cartT.localPosition = new Vector3(cartRect.center.x, cartRect.yMin, 0);
            else cartT.localPosition = new Vector3(cartRect.center.x, cartRect.center.y, 0);
            cartSr.sortingOrder = Order(cartRect.yMin);
            if (World3D)
            {
                if (cartShadow == null) cartShadow = MakeShadow(worldRoot, cartRect.center, 1.1f);
                cartShadow.transform.localPosition = new Vector3(cartRect.center.x, cartRect.yMin + 0.12f, -0.005f); cartShadow.sortingOrder = Order(cartRect.yMin) - 2;
            }
        }

        /// <summary>The hero walked into the cart: slide it along unless a shelf, the sofa, the table or a wall is in the way.</summary>
        void PushCart(Vector2 old)
        {
            var (d, c) = RectDist(pos, cartRect);
            if (d >= Radius) return;
            Vector2 n = d > 0.0001f ? (c - pos) / d : (pos - old).normalized;
            var moved = new Rect(cartRect.position + n * (Radius - d), cartRect.size);
            bool ok = moved.xMin >= 0.1f && moved.xMax <= RoomW - 0.1f && moved.yMin >= 0.1f && moved.yMax <= RoomH - 0.1f
                      && !SofaRect.Overlaps(moved) && !TableRect.Overlaps(moved);
            if (ok) foreach (var r in StationRects) if (r.Overlaps(moved)) { ok = false; break; }
            if (!ok) { if (d > 0.0001f) pos = c + (pos - c) / d * Radius; return; } // blocked: the cart acts as a wall
            cartVel = Time.deltaTime > 0.0001f ? Vector2.ClampMagnitude((moved.position - cartRect.position) / Time.deltaTime, 2.6f) : cartVel;
            pushedFrame = true;
            cartRect = moved; PlaceCart();
            RenderWorld();
            if (Time.unscaledTime > nextPushHud) { nextPushHud = Time.unscaledTime + 0.1f; RenderHud(); }
            if (!pushHinted) { pushHinted = true; Toast("เข็นรถเข็นไปจอดใกล้จุดเก็บของได้"); }
        }

        bool CartSpotFree(Rect moved)
        {
            bool ok = moved.xMin >= 0.1f && moved.xMax <= RoomW - 0.1f && moved.yMin >= 0.1f && moved.yMax <= RoomH - 0.1f
                      && !SofaRect.Overlaps(moved) && !TableRect.Overlaps(moved);
            if (ok) foreach (var r in StationRects) if (r.Overlaps(moved)) return false;
            return ok;
        }

        /// <summary>The cart keeps rolling after a push (friction), tilts into the turn, its load bounces, and it kicks up dust when fast.</summary>
        void TickCart(float dt)
        {
            if (cartT == null || Online) return;
            if (pushedFrame) pushedFrame = false;
            else if (cartVel.sqrMagnitude > 0.0009f)
            {
                var moved = new Rect(cartRect.position + cartVel * dt, cartRect.size);
                if (CartSpotFree(moved)) { cartRect = moved; PlaceCart(); RenderWorld(); }
                else cartVel *= -0.25f; // bumps into a shelf
                cartVel *= Mathf.Exp(-5f * dt);
            }
            else cartVel = Vector2.zero;
            float tiltTarget = Mathf.Clamp(-cartVel.x * 5f, -10f, 10f);
            cartTilt = Mathf.Lerp(cartTilt, tiltTarget, 1 - Mathf.Exp(-12f * dt));
            cartT.localRotation = Quaternion.Euler(World3D ? CamPitch - 90f : 0f, 0, cartTilt);
            float bounce = Mathf.Min(cartVel.magnitude, 2.5f) * 0.02f * Mathf.Abs(Mathf.Sin(Time.time * 18f));
            if (cartLayers != null) cartLayers.localPosition = Raise(Vector2.zero, bounce);
            if (World3D && cartVel.magnitude > 1.1f)
            {
                cartDust -= dt;
                if (cartDust <= 0) { cartDust = 0.12f; SpawnFxSized("fx_poof", Raise(new Vector2(cartRect.center.x, cartRect.yMin) - cartVel.normalized * 0.3f, 0.05f), 0.3f); }
            }
        }

        // ---------------------------------------------------------------- item rest table

        static readonly Rect TableRect = PR(40, 215, 80, 64);
        const int TableMax = 2;
        readonly List<string> tableItems = new List<string>();
        Transform tableItemsRoot;
        SpriteRenderer tableSr;
        bool nearTable;

        void BuildTable()
        {
            string sp = art.Has("desk") ? "desk" : null; // Leonardo desk picture
            tableSr = MakeSprite("RestTable", worldRoot, sp, new Vector2(TableRect.center.x, TableRect.yMin), TableRect.width * 1.15f, true);
            if (sp == null) { tableSr.transform.localScale = new Vector3(TableRect.width, TableRect.height, 1); tableSr.transform.localPosition = TableRect.center; tableSr.color = BobUi.Hex("#C9A66B"); }
            tableSr.sortingOrder = Order(TableRect.yMin);
            WorldLabel("โต๊ะพักของ", new Vector2(TableRect.center.x, TableRect.yMin - 0.16f), 0.03f, BobUi.Ink);
            var tableProp = tableSr.gameObject.AddComponent<StationProp>();
            tableProp.Configure(InteractableKind.Table, "table", NearCart, null); tableProp.BoundsProvider = () => TableRect;
            tableProp.CanInteractProvider = () => !Online; tableProp.PromptProvider = () => CtxText(ComputeCtx()); tableProp.OnInteract = _ => DoCtx(); detector.Register(tableProp);
            tableItemsRoot = new GameObject("TableItems").transform; tableItemsRoot.SetParent(worldRoot, false);
        }

        void ClearTable() { tableItems.Clear(); }

        void StashOnTable(int i)
        {
            if (i < 0 || i >= hand.Count) return;
            if (tableItems.Count >= TableMax) { Toast("โต๊ะเต็ม วางได้ " + TableMax + " ชิ้น"); Sfx("drop_invalid"); return; }
            tableItems.Add(hand[i]); hand.RemoveAt(i);
            Sfx("place_layer"); Toast("วางไว้บนโต๊ะพักของ แตะโต๊ะเพื่อหยิบคืน"); RenderAll();
        }

        void TakeFromTable()
        {
            if (Online || phase != Phase.Build || !nearTable || tableItems.Count == 0) return;
            if (hand.Count >= HandMax) { Toast("มือเต็มแล้ว ถือได้ " + HandMax + " ชิ้น"); Sfx("drop_invalid"); return; }
            hand.Add(tableItems[tableItems.Count - 1]); tableItems.RemoveAt(tableItems.Count - 1);
            Sfx("pickup"); PopHand(); RenderAll();
        }

        void RenderTable()
        {
            if (tableItemsRoot == null) return;
            foreach (Transform c in tableItemsRoot) Destroy(c.gameObject);
            if (Online) return;
            for (int i = 0; i < tableItems.Count; i++)
            {
                var ic = MakeSprite("T_" + tableItems[i], tableItemsRoot, "part_" + tableItems[i], Raise(new Vector2(TableRect.center.x - 0.16f + i * 0.32f, TableRect.yMin - 0.03f), 0.5f), 0.3f, false);
                ic.sortingOrder = Order(TableRect.yMin) + 3;
            }
        }

        // ---------------------------------------------------------------- screen backdrop (GDD themed rooms)

        /// <summary>A full-screen room picture behind a menu screen, softened so the cards on top stay readable.</summary>
        void ScreenBackdrop(string sprite)
        {
            if (!art.Has(sprite)) return;
            var r = BobUi.Rect("Backdrop", screenRoot); r.SetAsFirstSibling();
            r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f); r.sizeDelta = new Vector2(100, 100);
            var img = r.gameObject.AddComponent<Image>(); img.sprite = art.Get(sprite); img.raycastTarget = false;
            var fit = r.gameObject.AddComponent<AspectRatioFitter>(); fit.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent; fit.aspectRatio = 16f / 9f;
            var veil = BobUi.Rect("Veil", screenRoot); veil.SetSiblingIndex(1); BobUi.Stretch(veil);
            var vi = veil.gameObject.AddComponent<Image>(); vi.color = new Color(1f, 0.96f, 0.86f, 0.22f); vi.raycastTarget = false;
        }


        // ---------------------------------------------------------------- context button, world signals, spawn (UX spec 5A.3, 5A.4, 5A.8)

        enum Ctx { None, Place, Lift, Stash, TakeTable, Full }
        Ctx ctx;
        Button ctxBtn; TMP_Text ctxLabel;
        SpriteRenderer ctxArrow, teamRing, spawnRing;
        TextMesh heroName, spawnLabel;
        bool enemiesLive; float buildElapsed;
        const float EnemySpawnDelay = 2.5f; // seconds after "ลุย!"; every shelf is >= 4 tiles (2 u) from the spawn point

        Ctx ComputeCtx()
        {
            if (phase != Phase.Build || overlay != Overlay.None) return Ctx.None;
            if (nearCart && hand.Count > 0) return Ctx.Place;
            if (nearCart && stack.Count > 0) return Ctx.Lift;
            if (nearTable && hand.Count > 0 && tableItems.Count < TableMax) return Ctx.Stash;
            if (nearTable && tableItems.Count > 0 && hand.Count < HandMax) return Ctx.TakeTable;
            if (near != null && hand.Count >= HandMax) return Ctx.Full;
            return Ctx.None;
        }

        static string CtxText(Ctx c)
        {
            switch (c)
            {
                case Ctx.Place: return "วาง";
                case Ctx.Lift: return "ยก";
                case Ctx.Stash: return "พักของ";
                case Ctx.TakeTable: return "หยิบคืน";
                case Ctx.Full: return "มือเต็ม";
                default: return "เดินเข้าใกล้";
            }
        }

        Vector3? CtxTarget(Ctx c)
        {
            switch (c)
            {
                case Ctx.Place:
                case Ctx.Lift: return CartTop;
                case Ctx.Stash:
                case Ctx.TakeTable: return Raise(new Vector2(TableRect.center.x, TableRect.yMin), 0.95f);
                default: return null;
            }
        }

        void DoCtx()
        {
            switch (ComputeCtx())
            {
                case Ctx.Place: Place(0); break;
                case Ctx.Lift: LiftTop(); break;
                case Ctx.Stash: StashOnTable(0); break;
                case Ctx.TakeTable: TakeFromTable(); break;
                case Ctx.Full: Toast("มือเต็มแล้ว วางลงรถเข็นก่อน"); Sfx("drop_invalid"); break;
            }
        }

        void RenderCtx()
        {
            if (ctxBtn == null || hudCtl == null) return;
            ctx = ComputeCtx();
            hudCtl.SetPrimary(CtxText(ctx), ctx != Ctx.None && ctx != Ctx.Full,
                ctx == Ctx.Full ? new Color(0.75f, 0.75f, 0.78f, 0.95f) : ctx == Ctx.None ? new Color(1f, 0.99f, 0.95f, 0.85f) : BobUi.Sun);
            if (cartCardRt != null) cartCardRt.gameObject.SetActive(nearCart); // spec 5A.4: cart info appears when standing next to the cart
        }

        void BuildSignals()
        {
            ctxArrow = MakeSprite("CtxArrow", worldRoot, "icon_star", Vector2.zero, 0.32f, false); ctxArrow.sortingOrder = 2900; ctxArrow.gameObject.SetActive(false);
            teamRing = MakeSprite("TeamRing", worldRoot, "fx_target", Vector2.zero, 0.8f, false);
            spawnRing = MakeSprite("SpawnRing", worldRoot, "fx_target", PlayerStart, 1.0f, false); spawnRing.sortingOrder = -1400; spawnRing.gameObject.SetActive(false);
            spawnLabel = WorldLabel("1", PlayerStart + new Vector2(0, -0.34f), 0.05f, BobUi.Ink); spawnLabel.gameObject.SetActive(false);
            heroName = WorldLabel("", Vector2.zero, 0.028f, BobUi.Ink);
        }

        void TickSignals(float dt)
        {
            if (teamRing == null) return;
            var hc = BobUi.Hex(Hero.Color);
            bool inGame = R != null && phase != Phase.Title && phase != Phase.Loading;
            teamRing.gameObject.SetActive(inGame); heroName.gameObject.SetActive(inGame);
            if (inGame)
            {
                teamRing.color = new Color(hc.r, hc.g, hc.b, 0.85f); teamRing.transform.localPosition = pos + new Vector2(0, 0.02f); teamRing.sortingOrder = Order(pos.y) - 1;
                heroName.text = Hero.Name; heroName.transform.localPosition = pos + new Vector2(0, -0.16f); heroName.color = hc;
            }
            bool pre = R != null && (phase == Phase.Intro || phase == Phase.Countdown);
            spawnRing.gameObject.SetActive(pre); spawnLabel.gameObject.SetActive(pre);
            if (pre) spawnRing.color = new Color(hc.r, hc.g, hc.b, 0.9f);

            // the thing the main button will act on gets a bobbing star above it
            var tgt = CtxTarget(ctx);
            ctxArrow.gameObject.SetActive(tgt.HasValue);
            if (tgt.HasValue) ctxArrow.transform.localPosition = tgt.Value + Raise(Vector2.zero, 0.08f * Mathf.Sin(Time.unscaledTime * 6f));

            // enemies appear 2.5 s after "ลุย!" (UX 5A.8) and never before everyone could move
            if (phase == Phase.Build && overlay == Overlay.None && !Online) buildElapsed += dt;
            if (!enemiesLive && phase == Phase.Build && buildElapsed >= EnemySpawnDelay) ActivateEnemies(true);
        }

        void ActivateEnemies(bool withFx)
        {
            enemiesLive = true;
            foreach (var en in enemies)
            {
                if (en.t == null) continue;
                en.t.gameObject.SetActive(true);
                if (!withFx) continue;
                var sp = art.Find(en.id + "_spawn");
                if (sp != null) en.anim.Play(sp.frames, sp.fps, false);
                SpawnFx("fx_poof", Raise((Vector2)en.anim.transform.localPosition, 0.4f));
            }
            if (withFx) Sfx("enemy_spawn");
        }

        // ---------------------------------------------------------------- split-screen test view

        public const float SplitTime = 4.5f;
        float testStart = -99f;
        Phase shownPhase = Phase.Title;
        Coroutine splitCo;
        bool SplitActive => last != null && Time.unscaledTime - testStart < SplitTime;

        void SkipSplit() { testStart = -99f; if (phase == Phase.Test) RenderScreen(); }

        IEnumerator SplitTimer()
        {
            yield return new WaitForSecondsRealtime(SplitTime);
            if (phase == Phase.Test && !SplitActive) RenderScreen();
        }

        void SplitView()
        {
            var L = last;
            var ban = H1(screenRoot, "ทดสอบความสะอาด!", 64, BobUi.Surface);
            BobUi.OutlineText(ban, BobUi.Ink, 0.3f);
            BobUi.Pin(ban.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -36), new Vector2(1200, 100));
            SplitHalf(0.25f, "ทีมเรา · " + Hero.Name, L.Mine, L.Stats, L.Win);
            SplitHalf(0.75f, Online ? "ทีมคู่แข่ง" : "คู่แข่ง", L.Rival.Score, L.Rival.Ev.Stats, !L.Win);
            var line = BobUi.Rect("Divider", screenRoot); line.gameObject.AddComponent<Image>().color = BobUi.Ink;
            BobUi.Pin(line, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -20), new Vector2(6, 760));
            var hint = BobUi.Label(screenRoot, "แตะเพื่อข้าม", 24, BobUi.Surface, TextAnchor.MiddleCenter);
            BobUi.Pin(hint.rectTransform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 40), new Vector2(500, 40));
            var skip = BobUi.Rect("Skip", screenRoot); BobUi.Stretch(skip);
            skip.gameObject.AddComponent<Image>().color = new Color(0, 0, 0, 0);
            skip.gameObject.AddComponent<Button>().onClick.AddListener(SkipSplit);
            if (splitCo != null) StopCoroutine(splitCo);
            splitCo = StartCoroutine(SplitTimer());
        }

        void SplitHalf(float ax, string who, ScoreResult sc, int[] stats, bool winner)
        {
            var card = Card(screenRoot, "Half", winner ? BobUi.Hex("#FFF0C2") : BobUi.Surface);
            BobUi.Pin(card, new Vector2(ax, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -20), new Vector2(700, 0));
            VStack(card, 34, 26, 14);
            card.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            BobUi.Label(card, who, 36, BobUi.Ink, TextAnchor.MiddleCenter, FontStyle.Bold);

            var row = BobUi.HBox(card, 12, "Foes"); row.childForceExpandWidth = false; row.childForceExpandHeight = false; row.childAlignment = TextAnchor.MiddleCenter; BobUi.Size(row, 140, 140);
            foreach (var id in R.Enemies)
            {
                var im = BobUi.Img(row.transform, id + "_idle", 130);
                if (!art.Has(id + "_idle")) im.color = BobUi.Hex(BobMatch.Enemy(id).Color);
                var an = im.gameObject.AddComponent<BobSpriteAnimator>();
                StartCoroutine(EnemyReact(an, id, winner));
            }

            foreach (var kv in R.Need)
            {
                int k = System.Array.IndexOf(BobRules.StatKeys, kv.Key);
                int val = stats[k], need = kv.Value; bool ok = val >= need;
                var r = BobUi.HBox(card, 12, "Stat"); r.childForceExpandWidth = false; r.childForceExpandHeight = false; r.childAlignment = TextAnchor.MiddleCenter; BobUi.Size(r, 40, 40);
                BobUi.Size(BobUi.Label(r.transform, StatTh[kv.Key], 26, BobUi.Ink, TextAnchor.MiddleLeft), 36, 36, 0, 170);
                var bar = BobUi.Rect("Bar", r.transform); BobUi.Size(bar, 30, 30, 1, 200); bar.gameObject.AddComponent<Image>().color = BobUi.Hex("#E9DDB8");
                float max = Mathf.Max(need * 1.4f, val, 1f);
                var fill = BobUi.Rect("Fill", bar); fill.gameObject.AddComponent<Image>().color = ok ? BobUi.Good : BobUi.Bad;
                fill.anchorMin = Vector2.zero; fill.anchorMax = new Vector2(0, 1); fill.offsetMin = fill.offsetMax = Vector2.zero;
                var tick = BobUi.Rect("Target", bar); tick.gameObject.AddComponent<Image>().color = BobUi.Ink;
                tick.anchorMin = new Vector2(need / max, 0); tick.anchorMax = new Vector2(need / max, 1); tick.sizeDelta = new Vector2(5, 0); tick.anchoredPosition = Vector2.zero;
                BobUi.Size(BobUi.Label(r.transform, val + "/" + need, 26, ok ? BobUi.Good : BobUi.Bad, TextAnchor.MiddleRight, FontStyle.Bold), 36, 36, 0, 90);
                StartCoroutine(FillBar(fill, Mathf.Clamp01(val / max)));
            }
            BobUi.Label(card, sc.Pass ? "ผ่านสเปก" : "ไม่ผ่านสเปก", 32, sc.Pass ? BobUi.Good : BobUi.Bad, TextAnchor.MiddleCenter, FontStyle.Bold);
        }

        IEnumerator EnemyReact(BobSpriteAnimator an, string id, bool winner)
        {
            var idle = art.Find(id + "_idle");
            if (idle != null && an != null) an.Play(idle.frames, idle.fps, true);
            yield return new WaitForSecondsRealtime(1.4f);
            var e = art.Find(id + (winner ? "_defeat" : "_hit"));
            if (e != null && an != null) an.Play(e.frames, e.fps, false);
        }

        IEnumerator FillBar(RectTransform fill, float frac)
        {
            float e = 0, dur = 1.3f;
            while (e < dur && fill != null) { e += Time.unscaledDeltaTime; fill.anchorMax = new Vector2(frac * BobTween.EaseOutCubic(Mathf.Clamp01(e / dur)), 1); yield return null; }
            if (fill != null) fill.anchorMax = new Vector2(frac, 1);
        }
    }
}
