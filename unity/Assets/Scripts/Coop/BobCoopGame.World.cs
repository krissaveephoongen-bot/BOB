// Battle of the Brooms co-op: the two copies of the room (team 0 at x = 0, team 1 at x = TeamOffsetX), players, carts,
// parking tables, and the split-screen before/after view of the test phase (GDD 9, UX spec S25).
using System.Collections.Generic;
using System.Linq;
using Bob.Art;
using Bob.Game;
using Bob.Net;
using Bob.Rules;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Bob.Coop
{
    public partial class BobCoopGame
    {
        sealed class RoomView
        {
            public Transform root, cart, cartLayers, table, enemies;
            public SpriteRenderer cartSr;
            public TextMesh cartTag;
            public readonly List<SpriteRenderer> tiles = new List<SpriteRenderer>();
            public readonly List<bool> dirty = new List<bool>();
            public float cleanT = -1, cleanTo; // test phase: how much of the dirt is wiped, animated
            public string stackKey, tableKey;
        }

        sealed class Avatar
        {
            public Transform t, handRoot; public SpriteRenderer sr; public BobSpriteAnimator anim; public TextMesh label;
            public string animKey, handKey; public Vector2 shown; public int team = -1;
        }

        readonly RoomView[] rooms = new RoomView[2];
        readonly Dictionary<string, Avatar> avatars = new Dictionary<string, Avatar>();
        Camera cam, camRight;
        bool split, walking;

        static int Order(float y) => Mathf.RoundToInt(-y * 100f);
        static Vector3 W(int team, float x, float y) => new Vector3(x + team * BobCoopLayout.TeamOffsetX, y, 0);

        // ---------------------------------------------------------------- build

        void BuildWorld()
        {
            cam = Camera.main;
            if (cam == null) { var g = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener)) { tag = "MainCamera" }; cam = g.GetComponent<Camera>(); }
            cam.orthographic = true; cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = BobUi.Hex("#E9DDB8");
            // The room cameras only clear their own viewport; this one clears the whole screen first, so nothing stale shows around the room.
            var back = new GameObject("Backdrop Camera").AddComponent<Camera>();
            back.depth = -10; back.cullingMask = 0; back.clearFlags = CameraClearFlags.SolidColor; back.backgroundColor = BobUi.Hex("#E9DDB8"); back.orthographic = true;
            camRight = new GameObject("Right Camera (split view)").AddComponent<Camera>();
            camRight.orthographic = true; camRight.clearFlags = CameraClearFlags.SolidColor; camRight.backgroundColor = BobUi.Hex("#DCCFA6");
            camRight.enabled = false;
            if (FindFirstObjectByType<EventSystem>() == null) new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            for (int t = 0; t < 2; t++) rooms[t] = BuildRoom(t);
            SetSplit(false);
        }

        RoomView BuildRoom(int team)
        {
            var v = new RoomView { root = new GameObject("Room_Team" + team).transform };
            var floor = new GameObject("Floor").transform; floor.SetParent(v.root);
            for (int y = 0; y < 10; y++)
                for (int x = 0; x < 16; x++)
                {
                    var sr = Sprite("tile", floor, "floor_tile", W(team, x * 0.5f + 0.25f, y * 0.5f + 0.25f), 0.5f);
                    sr.sortingOrder = -2000;
                    if (!art.Has("floor_tile")) sr.color = (x + y) % 2 == 0 ? BobUi.Floor : BobUi.Hex("#EAD7A0");
                    v.tiles.Add(sr); v.dirty.Add(false);
                }
            var border = Sprite("Border", v.root, null, W(team, BobCoopLayout.RoomW / 2, BobCoopLayout.RoomH / 2), 1);
            border.transform.localScale = new Vector3(BobCoopLayout.RoomW + 0.12f, BobCoopLayout.RoomH + 0.12f, 1);
            border.color = BobUi.Ink; border.sortingOrder = -2100;

            for (int i = 0; i < 4; i++)
            {
                var b = BobCoopLayout.Stations[i]; var id = BobCoopLayout.StationIds[i];
                var sr = Sprite("Station_" + id, v.root, "station_" + id, W(team, b.Cx, b.Y0), b.W);
                if (!art.Has("station_" + id)) { sr.transform.localScale = new Vector3(b.W, b.H, 1); sr.transform.position = W(team, b.Cx, b.Cy); sr.color = BobUi.Family((Family)i); }
                sr.sortingOrder = Order(b.Y0);
                Label(BobCoopLayout.StationNames[i], W(team, b.Cx, b.Y0 < 2f ? b.Y0 - 0.12f : b.Y0 - 0.2f), 0.045f, BobUi.Ink, v.root); // bottom shelves: label closer, the floor ends just below them
            }

            var tb = BobCoopLayout.Table;
            var table = Sprite("Table", v.root, null, W(team, tb.Cx, tb.Cy), 1);
            table.transform.localScale = new Vector3(tb.W, tb.H, 1); table.color = BobUi.Hex("#C9925B"); table.sortingOrder = Order(tb.Y0);
            Label("โต๊ะพักของ", W(team, tb.Cx, tb.Y1 + 0.2f), 0.045f, BobUi.Ink, v.root);
            v.table = new GameObject("TableItems").transform; v.table.SetParent(v.root);

            var so = BobCoopLayout.Sofa;
            var sofa = Sprite("Sofa", v.root, "sofa", W(team, so.Cx, so.Y0), so.W);
            if (!art.Has("sofa")) { sofa.transform.localScale = new Vector3(so.W, so.H, 1); sofa.transform.position = W(team, so.Cx, so.Cy); sofa.color = BobUi.Surface; }
            sofa.sortingOrder = Order(so.Y0);

            v.cartSr = Sprite("Cart", v.root, "cart", W(team, BobCoopLayout.CartStartX, BobCoopLayout.CartStartY - BobCoopLayout.CartH / 2), 1.2f);
            if (!art.Has("cart")) { v.cartSr.transform.localScale = new Vector3(BobCoopLayout.CartW, BobCoopLayout.CartH, 1); v.cartSr.color = BobUi.Surface; }
            v.cart = v.cartSr.transform;
            v.cartLayers = new GameObject("CartLayers").transform; v.cartLayers.SetParent(v.root);
            v.cartTag = Label("", Vector3.zero, 0.045f, BobUi.Ink, v.root);
            v.enemies = new GameObject("Enemies").transform; v.enemies.SetParent(v.root);
            Label(team == 0 ? "ห้องของทีม 1" : "ห้องของทีม 2", W(team, BobCoopLayout.RoomW / 2, BobCoopLayout.RoomH + 0.25f), 0.04f, BobUi.Ink, v.root);
            return v;
        }

        SpriteRenderer Sprite(string name, Transform parent, string sprite, Vector3 at, float width)
        {
            var go = new GameObject(name); go.transform.SetParent(parent, false); go.transform.position = at;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite != null ? art.Get(sprite) : BobArtLibrary.White;
            var size = sr.sprite.bounds.size;
            if (width > 0 && size.x > 0) go.transform.localScale = Vector3.one * (width / size.x);
            return sr;
        }

        TextMesh Label(string text, Vector3 at, float size, Color color, Transform parent)
        {
            var go = new GameObject("Label"); go.transform.SetParent(parent, false); go.transform.position = at;
            var tm = go.AddComponent<TextMesh>();
            tm.font = BobUi.Font; tm.text = text; tm.fontSize = 48; tm.characterSize = size; tm.anchor = TextAnchor.MiddleCenter; tm.alignment = TextAlignment.Center; tm.color = color;
            var mr = go.GetComponent<MeshRenderer>(); mr.sharedMaterial = BobUi.Font.material; mr.sortingOrder = 3000;
            return tm;
        }

        // ---------------------------------------------------------------- per round

        /// <summary>New round: this room's enemies, dirty floor, clean slate on the carts and tables.</summary>
        void ResetRooms(MpState s)
        {
            for (int t = 0; t < 2; t++)
            {
                var v = rooms[t];
                ClearChildren(v.enemies);
                var ids = s.room?.enemies ?? new string[0];
                for (int i = 0; i < ids.Length; i++)
                {
                    var x = BobCoopLayout.RoomW / 2 + (i - (ids.Length - 1) / 2f) * 1.2f;
                    var sr = Sprite(ids[i], v.enemies, ids[i] + "_idle", W(t, x, 3.55f), 0f);
                    sr.transform.localScale = Vector3.one * (0.8f / Mathf.Max(0.01f, sr.sprite.bounds.size.y));
                    sr.sortingOrder = Order(3.55f);
                    var idle = art.Find(ids[i] + "_idle");
                    if (idle != null) sr.gameObject.AddComponent<BobSpriteAnimator>().Play(idle.frames, idle.fps, true);
                    else { sr.transform.localScale = new Vector3(0.5f, 0.4f, 1); sr.color = BobUi.Hex(BobMatch.Enemy(ids[i]).Color); }
                }
                var rng = new System.Random(s.seed * 13 + s.round + t);
                for (int i = 0; i < v.tiles.Count; i++) { v.dirty[i] = rng.NextDouble() < 0.45; v.tiles[i].sprite = art.Get(v.dirty[i] ? "floor_tile_dirty" : "floor_tile"); }
                v.cleanT = -1; v.stackKey = v.tableKey = null;
            }
        }

        // ---------------------------------------------------------------- drawing what the server says

        void RenderWorld()
        {
            var s = net?.State; if (s?.teams == null) return;
            for (int t = 0; t < 2 && t < s.teams.Length; t++) DrawCart(t, s.teams[t], s);
            // players
            var seen = new HashSet<string>();
            if (s.players != null)
                foreach (var p in s.players)
                {
                    seen.Add(p.id);
                    if (!avatars.TryGetValue(p.id, out var a)) { a = MakeAvatar(p); avatars[p.id] = a; a.shown = new Vector2(p.x, p.y); }
                    if (a.team != p.team) { a.team = p.team; a.label.color = p.team == net.MyTeam ? BobUi.Good : BobUi.Muted; }
                    a.label.text = (p.id == net.Me ? "คุณ" : Short(p.name)) + (p.vote ? " ✓" : "") + (p.online ? "" : " (หลุด)");
                    a.t.gameObject.SetActive(s.phase != "lobby");
                    var key = string.Join(",", p.hand ?? new string[0]);
                    if (key != a.handKey) { a.handKey = key; DrawHand(a, p.hand); }
                    SetAnim(a, p.@char, p.hand != null && p.hand.Length > 0 ? "carry" : "idle");
                }
            foreach (var id in avatars.Keys.Where(k => !seen.Contains(k)).ToList()) { Destroy(avatars[id].t.gameObject); avatars.Remove(id); }
        }

        void DrawCart(int t, MpTeam team, MpState s)
        {
            var v = rooms[t];
            v.cart.position = W(t, team.cx, team.cy - BobCoopLayout.CartH / 2);
            v.cartSr.sortingOrder = Order(team.cy - BobCoopLayout.CartH / 2);
            var pusher = string.IsNullOrEmpty(team.pusher) ? null : s.players?.FirstOrDefault(p => p.id == team.pusher);
            v.cartTag.text = team.submitted ? "ส่งแล้ว" : pusher != null ? (pusher.id == net.Me ? "คุณกำลังเข็น" : Short(pusher.name) + " กำลังเข็น") : "";
            v.cartTag.transform.position = W(t, team.cx, team.cy - BobCoopLayout.CartH / 2 - 0.15f);
            var key = string.Join(",", team.stack ?? new string[0]) + "@" + team.cx.ToString("0.00") + "," + team.cy.ToString("0.00");
            if (key != v.stackKey)
            {
                v.stackKey = key; ClearChildren(v.cartLayers);
                var stack = team.stack ?? new string[0]; float y = team.cy + 0.15f;
                foreach (var id in stack)
                {
                    bool m = BobRules.Parts.TryGetValue(id, out var part) && part.Family == Family.Method;
                    var sr = Sprite(id, v.cartLayers, "part_" + id, W(t, team.cx, y), m ? 0.22f : 0.34f);
                    if (!art.Has("part_" + id) && part != null) { sr.transform.localScale = new Vector3(0.5f, 0.12f, 1); sr.color = BobUi.Family(part.Family); }
                    sr.sortingOrder = Order(team.cy - BobCoopLayout.CartH / 2) + 1;
                    y += m ? 0.14f : 0.22f;
                }
            }
            var tkey = string.Join(",", team.table ?? new string[0]);
            if (tkey != v.tableKey)
            {
                v.tableKey = tkey; ClearChildren(v.table);
                var items = team.table ?? new string[0]; var tb = BobCoopLayout.Table;
                for (int i = 0; i < items.Length; i++)
                {
                    var sr = Sprite(items[i], v.table, "part_" + items[i], W(t, tb.X0 + 0.15f + i * 0.24f, tb.Cy + 0.05f), 0.24f);
                    sr.sortingOrder = Order(tb.Y0) + 1;
                }
            }
        }

        Avatar MakeAvatar(MpPlayer p)
        {
            var a = new Avatar();
            a.sr = Sprite("Player_" + Short(p.name), null, p.@char + "_idle", W(p.team, p.x, p.y), 0f);
            a.t = a.sr.transform;
            a.sr.transform.localScale = Vector3.one * (0.95f / Mathf.Max(0.01f, a.sr.sprite.bounds.size.y));
            if (!art.Has(p.@char + "_idle")) { a.sr.transform.localScale = new Vector3(0.3f, 0.45f, 1); a.sr.color = BobUi.Hex(BobMatch.Char(p.@char).Color); }
            a.anim = a.sr.gameObject.AddComponent<BobSpriteAnimator>();
            a.label = Label("", W(p.team, p.x, p.y + 1.25f), 0.045f, BobUi.Ink, null);
            a.handRoot = new GameObject("Hand").transform;
            return a;
        }

        void SetAnim(Avatar a, string ch, string anim)
        {
            if (walking && net != null && avatars.TryGetValue(net.Me, out var mine) && mine == a) anim = anim == "carry" ? "carry" : "walk";
            var key = ch + "_" + anim; if (key == a.animKey) return;
            a.animKey = key;
            var e = art.Find(key) ?? art.Find(ch + "_idle");
            if (e != null) a.anim.Play(e.frames, e.fps, e.loop);
        }

        void DrawHand(Avatar a, string[] hand)
        {
            ClearChildren(a.handRoot);
            if (hand == null) return;
            for (int i = 0; i < hand.Length; i++)
            {
                var sr = Sprite(hand[i], a.handRoot, "part_" + hand[i], Vector3.zero, 0.26f);
                sr.transform.localPosition = new Vector3((i - (hand.Length - 1) / 2f) * 0.28f, 0, 0);
                sr.sortingOrder = 2900;
            }
        }

        /// <summary>Moving my own cart right away while I push it; the server's POS overwrites it 10 times a second.</summary>
        void PredictCart(Vector2 delta)
        {
            var c = net.MyCart; var p = BobCoopLayout.SolveCart(c.cx + delta.x, c.cy + delta.y);
            c.cx = p.x; c.cy = p.y; DrawCart(net.MyTeam, c, net.State);
        }

        // ---------------------------------------------------------------- per frame

        void TickWorld(float dt)
        {
            var s = net?.State;
            if (s?.players != null)
                foreach (var p in s.players)
                {
                    if (!avatars.TryGetValue(p.id, out var a)) continue;
                    var target = p.id == net.Me && havePos ? myPos : new Vector2(p.x, p.y);
                    a.shown = p.id == net.Me ? target : Vector2.Lerp(a.shown, target, 1 - Mathf.Exp(-12f * dt)); // others: smooth 10 Hz updates
                    var w = W(p.team, a.shown.x, a.shown.y);
                    a.t.position = w; a.sr.sortingOrder = Order(a.shown.y);
                    a.label.transform.position = w + new Vector3(0, 1.25f, 0);
                    a.handRoot.position = w + new Vector3(0, 1.3f, 0);
                }
            // test phase: wipe the dirt away, as far as the score says
            for (int t = 0; t < 2; t++)
            {
                var v = rooms[t]; if (v.cleanT < 0 || v.cleanT >= v.cleanTo) continue;
                float before = v.cleanT; v.cleanT = Mathf.Min(v.cleanTo, v.cleanT + dt / 3f);
                int n = v.tiles.Count;
                for (int i = Mathf.FloorToInt(before * n); i < Mathf.FloorToInt(v.cleanT * n) && i < n; i++)
                    if (v.dirty[i]) { v.dirty[i] = false; v.tiles[i].sprite = art.Get("floor_tile"); }
            }
            if (!split && cam != null && net != null && net.MePlayer != null)
            {
                FitRoomCamera();
                var c = W(net.MyTeam, BobCoopLayout.RoomW / 2, BobCoopLayout.RoomH / 2 + 0.3f);
                cam.transform.position = new Vector3(c.x, c.y, -10);
            }
        }

        /// <summary>Tap on the floor of my room: walk there (taps on the HUD never reach here).</summary>
        void TapWorld(Vector2 screenPos)
        {
            if (screen != View.Build || cam == null || net?.MePlayer == null) return;
            var w = cam.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, 10));
            float x = w.x - net.MyTeam * BobCoopLayout.TeamOffsetX, y = w.y;
            if (x < 0 || x > BobCoopLayout.RoomW || y < 0 || y > BobCoopLayout.RoomH) return;
            tapTarget = new Vector2(x, y);
        }

        // ---------------------------------------------------------------- split view (test phase)

        // In a match the room sits between the side columns (order ticket + cart card left, actions + send right), under the
        // top bar and above the hand row, so no panel covers the floor (UX spec S22). Fractions of the safe area.
        public const float SideCol = 0.24f, TopStrip = 0.125f, BottomStrip = 0.2f;

        void FitRoomCamera()
        {
            if (screen != View.Build) return;
            var sa = Screen.safeArea; float W = Mathf.Max(1, Screen.width), H = Mathf.Max(1, Screen.height);
            float x0 = sa.xMin / W + SideCol * sa.width / W, x1 = sa.xMax / W - SideCol * sa.width / W;
            float y0 = sa.yMin / H + BottomStrip * sa.height / H, y1 = sa.yMax / H - TopStrip * sa.height / H;
            cam.rect = new Rect(x0, y0, Mathf.Max(0.1f, x1 - x0), Mathf.Max(0.1f, y1 - y0));
            float needW = BobCoopLayout.RoomW + 0.3f, needH = BobCoopLayout.RoomH + 0.9f; // + room above the back shelves for their art
            cam.orthographicSize = Mathf.Max(needH / 2, needW / 2 / Mathf.Max(0.1f, cam.aspect));
        }

        void SetSplit(bool on)
        {
            split = on;
            if (cam == null) return;
            camRight.enabled = on;
            if (!on)
            {
                cam.rect = new Rect(0, 0, 1, 1); cam.orthographicSize = 3.2f;
                return;
            }
            int mine = net != null ? net.MyTeam : 0;
            // the rooms sit between the score boxes (top) and the result line (bottom) so neither covers the floor
            cam.rect = new Rect(0, 0.12f, 0.5f, 0.6f); camRight.rect = new Rect(0.5f, 0.12f, 0.5f, 0.6f);
            float aspect = Mathf.Max(0.3f, cam.aspect); // already the half-screen aspect after setting rect
            float size = Mathf.Max(2.9f, (BobCoopLayout.RoomW / 2 + 0.25f) / aspect);
            cam.orthographicSize = camRight.orthographicSize = size;
            var a = W(mine, BobCoopLayout.RoomW / 2, BobCoopLayout.RoomH / 2 + 0.2f); var b = W(1 - mine, BobCoopLayout.RoomW / 2, BobCoopLayout.RoomH / 2 + 0.2f);
            cam.transform.position = new Vector3(a.x, a.y, -10); camRight.transform.position = new Vector3(b.x, b.y, -10);
        }

        /// <summary>Both rooms side by side: each team's final cart, then the dirt wiped away as far as its test score.</summary>
        void ShowTestRooms(MpResult r)
        {
            if (r?.teams == null) return;
            foreach (var tr in r.teams)
            {
                if (tr.team < 0 || tr.team > 1) continue;
                var v = rooms[tr.team];
                v.cleanT = 0; v.cleanTo = tr.score == null ? 0.3f : tr.score.pass ? 1f : 0.45f;
                if (tr.score != null && tr.score.pass)
                    foreach (Transform e in v.enemies)
                    {
                        var an = e.GetComponent<BobSpriteAnimator>(); var def = art.Find(e.name + "_defeat");
                        if (an != null && def != null) an.Play(def.frames, def.fps, false); else e.gameObject.SetActive(false);
                    }
            }
        }

        static void ClearChildren(Transform t) { for (int i = t.childCount - 1; i >= 0; i--) Destroy(t.GetChild(i).gameObject); }
        static string Short(string n) => string.IsNullOrEmpty(n) ? "?" : n.Length > 10 ? n.Substring(0, 10) : n;
    }
}
