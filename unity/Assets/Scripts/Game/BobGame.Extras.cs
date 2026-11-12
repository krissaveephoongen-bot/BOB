// Battle of the Brooms: GDD sections 9 and 20 gameplay that is not in the core loop.
//  - Pushing the cart: walk into it (solo). Park it next to the shelves you need. Online the server owns the cart position, so it stays put.
//  - Item rest table (โต๊ะพักของ): stash up to 2 hand parts, take them back later (solo; online needs a server op).
//  - Split-screen test view: our room and the rival's side by side for a few seconds before the scores (GDD 9).
using System.Collections;
using System.Collections.Generic;
using Bob.Art;
using Bob.Rules;
using UnityEngine;
using UnityEngine.UI;

namespace Bob.Game
{
    public partial class BobGame
    {
        // ---------------------------------------------------------------- cart push

        bool pushHinted;
        float nextPushHud;

        /// <summary>Put the cart sprite where cartRect says (bottom-pivot, or a coloured box without art).</summary>
        void PlaceCart()
        {
            if (cartT == null) return;
            if (art.Has("cart")) cartT.position = new Vector3(cartRect.center.x, cartRect.yMin, 0);
            else cartT.position = new Vector3(cartRect.center.x, cartRect.center.y, 0);
            cartSr.sortingOrder = Order(cartRect.yMin);
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
            cartRect = moved; PlaceCart();
            RenderWorld();
            if (Time.unscaledTime > nextPushHud) { nextPushHud = Time.unscaledTime + 0.1f; RenderHud(); }
            if (!pushHinted) { pushHinted = true; Toast("เข็นรถเข็นไปจอดใกล้จุดเก็บของได้"); }
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
            string sp = art.Has("prop_desk") ? "prop_desk" : null;
            tableSr = MakeSprite("RestTable", worldRoot, sp, new Vector2(TableRect.center.x, TableRect.yMin), TableRect.width, true);
            if (sp == null) { tableSr.transform.localScale = new Vector3(TableRect.width, TableRect.height, 1); tableSr.transform.position = TableRect.center; tableSr.color = BobUi.Hex("#C9A66B"); }
            tableSr.sortingOrder = Order(TableRect.yMin);
            WorldLabel("โต๊ะพักของ", new Vector2(TableRect.center.x, TableRect.yMin - 0.16f), 0.03f, BobUi.Ink);
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
                var ic = MakeSprite("T_" + tableItems[i], tableItemsRoot, "part_" + tableItems[i], new Vector2(TableRect.center.x - 0.16f + i * 0.32f, TableRect.yMin + 0.5f), 0.3f, false);
                ic.sortingOrder = Order(TableRect.yMin) + 3;
            }
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
            ban.GetComponent<Outline>().effectColor = BobUi.Ink; ban.GetComponent<Outline>().effectDistance = new Vector2(4, -4);
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
