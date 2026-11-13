// Battle of the Brooms: the room (floor, 4 stations, cart, sofa, enemies), the hero and movement (tap / WASD).
// World units: the prototype's 800x500 room divided by 100, y up. Sprites are y-sorted for a 2.5D look.
using System.Collections.Generic;
using System.Linq;
using Bob.Art;
using Bob.Modular;
using Bob.Rules;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Bob.Game
{
    public partial class BobGame
    {
        const float RoomW = 8f, RoomH = 5f, Radius = 0.16f, Speed = 2.3f, NearStation = 0.46f, NearCart = 0.5f;

        // Prototype rects (x, y from the top-left, w, h in px) -> world rects.
        static Rect PR(float x, float y, float w, float h) => new Rect(x / 100f, (500f - y - h) / 100f, w / 100f, h / 100f);

        static readonly string[] StationIds = { "tool", "chem", "energy", "method" };
        static readonly string[] StationNames = { "ชั้นวางอุปกรณ์", "ตู้น้ำยา", "จุดแรงทำงาน", "กระดานวิธีใช้" };
        static readonly Rect[] StationRects = { PR(30, 30, 170, 64), PR(600, 30, 170, 64), PR(30, 406, 170, 64), PR(600, 406, 170, 64) };
        static readonly Rect CartHome = PR(355, 215, 90, 64), SofaRect = PR(300, 385, 200, 30); // sofa sits lower than the prototype so its sprite does not hide the cart
        Rect cartRect = CartHome;
        Rect CartRect => cartRect; // solo: the hero can push the cart (BobGame.Extras.cs)
        static readonly Vector2 PlayerStart = new Vector2(4f, 0.6f);

        Camera cam;
        SpriteRenderer themeBg;
        static readonly string[] RoundThemes = { "lobby_rental_room", "lobby_condo", "lobby_coffee_shop", "lobby_kitchen" }; // GDD chapters: rental room, condo, cafe
        Transform worldRoot, cartT, cartLayers, enemyRoot, handIcons, targetMarker;
        SpriteRenderer[] stationSr = new SpriteRenderer[4];
        SpriteRenderer cartSr, playerSr;
        BobSpriteAnimator playerAnim;
        string playerAnimKey;
        readonly List<SpriteRenderer> floorTiles = new List<SpriteRenderer>();
        readonly List<(string id, BobSpriteAnimator anim, Transform t)> enemies = new List<(string, BobSpriteAnimator, Transform)>();

        PlayerController player;           // movement (Modular/PlayerController.cs)
        InteractionDetector detector;      // what the hero stands next to (Modular/InteractionDetector.cs)
        Vector2 pos { get => player.Position; set => player.Position = value; }
        Vector2? target { get => player.Target; set => player.Target = value; }
        Vector2 lastMove;
        float stuck, stepT;
        string near;
        bool nearCart;
        int handShown = -1;

        Vector2 PlayerPos => pos;
        Vector2 CartTop => new Vector2(CartRect.center.x, CartRect.yMin + 0.9f);

        // ---------------------------------------------------------------- build

        void BuildWorld()
        {
            player = gameObject.AddComponent<PlayerController>();
            player.room = new Rect(0, 0, RoomW, RoomH); player.speed = Speed; player.radius = Radius; player.Solids = Solids; player.Position = PlayerStart;
            detector = gameObject.AddComponent<InteractionDetector>();
            cam = Camera.main;
            if (cam == null) { var cg = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener)) { tag = "MainCamera" }; cam = cg.GetComponent<Camera>(); }
            cam.orthographic = true; cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = BobUi.Hex("#E9DDB8");
            cam.transform.position = new Vector3(RoomW / 2, RoomH / 2 + 0.3f, -10); cam.transform.rotation = Quaternion.identity;
            cam.depth = 0;
            // The room camera only draws its own viewport; this one clears the rest of the screen (under the HUD) every frame.
            var back = new GameObject("Backdrop Camera").AddComponent<Camera>();
            back.depth = -10; back.cullingMask = 0; back.clearFlags = CameraClearFlags.SolidColor; back.backgroundColor = BobUi.Bg; back.orthographic = true;

            if (FindFirstObjectByType<EventSystem>() == null)
                new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

            worldRoot = new GameObject("Room").transform;
            themeBg = MakeSprite("ThemeBackdrop", worldRoot, null, new Vector2(RoomW / 2, RoomH / 2 + 0.35f), 0f, false);
            themeBg.sortingOrder = -3000;

            // Floor: 0.5 u tiles; dirty tiles show where the enemies have been (cleaned on a round win).
            var floor = new GameObject("Floor").transform; floor.SetParent(worldRoot);
            for (int y = 0; y < 10; y++)
                for (int x = 0; x < 16; x++)
                {
                    var sr = MakeSprite("tile", floor, "floor_tile", new Vector2(x * 0.5f + 0.25f, y * 0.5f + 0.25f), 0.5f, false);
                    sr.sortingOrder = -2000;
                    sr.color = new Color(1f, 1f, 1f, 0.4f); // the themed room picture's own floor shows through (3/4 look)
                    if (!art.Has("floor_tile")) sr.color = (x + y) % 2 == 0 ? BobUi.Floor : BobUi.Hex("#EAD7A0");
                    floorTiles.Add(sr);
                }
            var border = MakeSprite("RoomBorder", worldRoot, null, new Vector2(RoomW / 2, RoomH / 2), 1, false);
            border.transform.localScale = new Vector3(RoomW + 0.12f, RoomH + 0.12f, 1); border.color = new Color(BobUi.Ink.r, BobUi.Ink.g, BobUi.Ink.b, 0.45f); border.sortingOrder = -2100;

            for (int i = 0; i < 4; i++)
            {
                var r = StationRects[i];
                var sr = MakeSprite("Station_" + StationIds[i], worldRoot, "station_" + StationIds[i], new Vector2(r.center.x, r.yMin), r.width, true);
                if (!art.Has("station_" + StationIds[i])) { sr.transform.localScale = new Vector3(r.width, r.height, 1); sr.transform.position = r.center; sr.color = BobUi.Family((Family)i); }
                sr.sortingOrder = Order(r.yMin);
                stationSr[i] = sr;
                var shelfProp = sr.gameObject.AddComponent<StationProp>(); int si = i;
                shelfProp.Configure(InteractableKind.Shelf, StationIds[i], NearStation, null); shelfProp.BoundsProvider = () => StationRects[si]; detector.Register(shelfProp);
                // the shelf picture is empty: the parts it offers sit on it
                var onShelf = BobRules.Parts.Where(kv => kv.Value.Family == (Family)i).Select(kv => kv.Key).ToList();
                for (int n = 0; n < onShelf.Count && art.Has("station_" + StationIds[i]); n++)
                {
                    float fx = r.center.x + (n - (onShelf.Count - 1) / 2f) * 0.36f;
                    var ic = MakeSprite("Shelf_" + onShelf[n], worldRoot, "part_" + onShelf[n], new Vector2(fx, r.yMin + 0.46f), 0.3f, false);
                    ic.sortingOrder = sr.sortingOrder + 1;
                }
                WorldLabel(StationNames[i], new Vector2(r.center.x, r.yMin - 0.16f), 0.032f, BobUi.Ink);
            }

            var sofa = MakeSprite("Sofa", worldRoot, "sofa", new Vector2(SofaRect.center.x, SofaRect.yMin), SofaRect.width, true);
            if (!art.Has("sofa")) { sofa.transform.localScale = new Vector3(SofaRect.width, SofaRect.height, 1); sofa.transform.position = SofaRect.center; sofa.color = BobUi.Surface; }
            sofa.sortingOrder = Order(SofaRect.yMin);

            cartSr = MakeSprite("Cart", worldRoot, "cart", new Vector2(CartRect.center.x, CartRect.yMin), 1.2f, true);
            if (!art.Has("cart")) { cartSr.transform.localScale = new Vector3(CartRect.width, CartRect.height, 1); cartSr.transform.position = CartRect.center; cartSr.color = BobUi.Surface; }
            cartSr.sortingOrder = Order(CartRect.yMin);
            cartT = cartSr.transform;
            var cartProp = cartSr.gameObject.AddComponent<StationProp>();
            cartProp.Configure(InteractableKind.Cart, "cart", NearCart, null); cartProp.BoundsProvider = () => cartRect;
            cartProp.PromptProvider = () => CtxText(ComputeCtx()); cartProp.OnInteract = _ => DoCtx(); detector.Register(cartProp);
            BuildTable();
            cartLayers = new GameObject("CartLayers").transform; cartLayers.SetParent(worldRoot);

            enemyRoot = new GameObject("Enemies").transform; enemyRoot.SetParent(worldRoot);

            playerSr = MakeSprite("Hero", worldRoot, charId + "_idle", pos, 0f, true);
            playerAnim = playerSr.gameObject.AddComponent<BobSpriteAnimator>();
            handIcons = new GameObject("HandIcons").transform; handIcons.SetParent(playerSr.transform, false);
            RefreshHero();

            targetMarker = MakeSprite("Target", worldRoot, "fx_target", Vector2.zero, 0.35f, false).transform;
            targetMarker.GetComponent<SpriteRenderer>().sortingOrder = -1500;
            targetMarker.gameObject.SetActive(false);

            BuildSignals();
            ResetRoomForRound();
        }

        static int Order(float y) => Mathf.RoundToInt(-y * 100f);

        /// <summary>A sprite from the art library, scaled so its frame is `width` wide (or 1 u tall for width 0).</summary>
        SpriteRenderer MakeSprite(string name, Transform parent, string sprite, Vector2 at, float width, bool bottomPivot)
        {
            var go = new GameObject(name); go.transform.SetParent(parent, false); go.transform.position = at;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite != null && art != null ? art.Get(sprite) : BobArtLibrary.White;
            var size = sr.sprite.bounds.size;
            if (width > 0 && size.x > 0) go.transform.localScale = Vector3.one * (width / size.x);
            return sr;
        }

        TextMesh WorldLabel(string text, Vector2 at, float size, Color color, Transform parent = null)
        {
            var go = new GameObject("Label"); go.transform.SetParent(parent != null ? parent : worldRoot, false); go.transform.position = at;
            var tm = go.AddComponent<TextMesh>();
            tm.font = BobUi.Font; tm.text = text; tm.fontSize = 48; tm.characterSize = size; tm.anchor = TextAnchor.MiddleCenter; tm.alignment = TextAlignment.Center; tm.color = color;
            var mr = go.GetComponent<MeshRenderer>(); mr.sharedMaterial = BobUi.Font.material; mr.sortingOrder = 3000;
            return tm;
        }

        void RefreshHero()
        {
            if (playerSr == null) return;
            playerAnimKey = null;
            var s = art.Get(charId + "_idle");
            playerSr.transform.localScale = Vector3.one * (0.95f / Mathf.Max(0.01f, s.bounds.size.y));
            if (!art.Has(charId + "_idle")) { playerSr.transform.localScale = new Vector3(0.3f, 0.45f, 1); playerSr.color = BobUi.Hex(Hero.Color); }
            SetHeroAnim("idle");
        }

        void SetHeroAnim(string anim)
        {
            var key = charId + "_" + anim;
            if (key == playerAnimKey) return;
            playerAnimKey = key;
            var e = art.Find(key);
            if (e != null) playerAnim.Play(e.frames, e.fps, e.loop);
        }

        /// <summary>New round (or back to the title): enemies for this room, dirty floor, hero at the door.</summary>
        void ResetRoomForRound()
        {
            foreach (var en in enemies) if (en.t != null) Destroy(en.t.gameObject);
            enemies.Clear();
            var ids = R != null ? R.Enemies : new[] { "stain" };
            for (int i = 0; i < ids.Length; i++)
            {
                var x = RoomW / 2 + (i - (ids.Length - 1) / 2f) * 1.2f;
                var holder = new GameObject("Enemy_" + ids[i]).transform; holder.SetParent(enemyRoot, false);
                var sr = MakeSprite("Sprite", holder, ids[i] + "_idle", new Vector2(x, 3.75f), 0f, true);
                var idle = art.Find(ids[i] + "_idle");
                sr.transform.localScale = Vector3.one * (0.95f / Mathf.Max(0.01f, sr.sprite.bounds.size.y));
                if (idle == null) { sr.transform.localScale = new Vector3(0.6f, 0.5f, 1); sr.color = BobUi.Hex(BobMatch.Enemy(ids[i]).Color); }
                sr.sortingOrder = Order(3.75f);
                var an = sr.gameObject.AddComponent<BobSpriteAnimator>();
                if (idle != null) an.Play(idle.frames, idle.fps, true);
                WorldLabel(BobMatch.Enemy(ids[i]).Name, new Vector2(x, 3.62f), 0.03f, BobUi.Ink, holder);
                holder.gameObject.SetActive(false); // appear 2.5 s after the countdown (TickSignals)
                enemies.Add((ids[i], an, holder));
            }
            enemiesLive = false; buildElapsed = 0;

            var rng = new System.Random(seed * 13 + roundNo);
            bool dirty = R != null;
            foreach (var t in floorTiles) t.sprite = art.Get(dirty && rng.NextDouble() < 0.45 ? "floor_tile_dirty" : "floor_tile");

            if (themeBg != null)
            {
                var key = RoundThemes[Mathf.Clamp(R != null ? roundNo - 1 : 0, 0, RoundThemes.Length - 1)];
                if (art.Has(key))
                {
                    themeBg.sprite = art.Get(key);
                    float h = 8.8f, k = h / Mathf.Max(0.01f, themeBg.sprite.bounds.size.y);
                    themeBg.transform.localScale = Vector3.one * k;
                }
            }
            cartRect = CartHome; PlaceCart(); ClearTable();
            pos = PlayerStart; target = null; stuck = 0; near = null; nearCart = false; nearTable = false;
            handShown = -1;
        }

        // ---------------------------------------------------------------- per frame

        // Floating joystick: appears where the thumb lands in the left column, under the order ticket.
        const float JoyRadius = 110f, JoyDead = 0.15f;
        bool joyActive; int joyFinger = -99; Vector2 joyOrigin, joyVec;

        static bool OverUi(int pointerId) =>
            EventSystem.current != null && (pointerId >= 0 ? EventSystem.current.IsPointerOverGameObject(pointerId) : EventSystem.current.IsPointerOverGameObject());

        static bool InJoyZone(Vector2 p) => p.x < Screen.width * (SideCol + 0.04f) && p.y < Screen.height * 0.6f;

        void JoyBegin(int id, Vector2 p) { joyActive = true; joyFinger = id; joyOrigin = p; joyVec = Vector2.zero; target = null; }
        void JoyMove(Vector2 p)
        {
            float px = JoyRadius * Screen.height / 1080f;
            var d = (p - joyOrigin) / Mathf.Max(1, px);
            joyVec = d.magnitude > 1 ? d.normalized : d;
        }

        void Tap(Vector2 p)
        {
            if (!cam.pixelRect.Contains(p)) return;
            var w = cam.ScreenToWorldPoint(p);
            if (!Online && phase == Phase.Build && tableItems.Count > 0 && TableRect.Contains(w) && nearTable) { TakeFromTable(); return; }
            if (w.x < -0.3f || w.x > RoomW + 0.3f || w.y < -0.3f || w.y > RoomH + 0.3f) return;
            target = new Vector2(Mathf.Clamp(w.x, 0.18f, RoomW - 0.18f), Mathf.Clamp(w.y, 0.18f, RoomH - 0.18f)); stuck = 0;
        }

        void TickInput()
        {
            if (phase != Phase.Build || overlay != Overlay.None) { joyActive = false; return; }
            // keyboard shortcuts: 1-3 place from hand, Q lift the top layer, F ability, Enter submit
            for (int i = 0; i < HandMax; i++) if (Input.GetKeyDown(KeyCode.Alpha1 + i)) Place(i);
            if (Input.GetKeyDown(KeyCode.Q)) LiftTop();
            if (Input.GetKeyDown(KeyCode.T)) TakeFromTable();
            if (Input.GetKeyDown(KeyCode.F)) UseAbility();
            if (Input.GetKeyDown(KeyCode.Return) && BobRules.CountLayers(stack) > 0) Submit();

            if (Input.touchCount > 0)
            {
                for (int i = 0; i < Input.touchCount; i++)
                {
                    var t = Input.GetTouch(i);
                    if (t.phase == TouchPhase.Began)
                    {
                        if (OverUi(t.fingerId)) continue;
                        if (!joyActive && InJoyZone(t.position)) JoyBegin(t.fingerId, t.position);
                        else Tap(t.position);
                    }
                    else if (joyActive && t.fingerId == joyFinger)
                    {
                        if (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled) joyActive = false; else JoyMove(t.position);
                    }
                }
                return;
            }
            // mouse (Editor / desktop): same behaviour
            Vector2 mp = Input.mousePosition;
            if (Input.GetMouseButtonDown(0) && !OverUi(-1)) { if (InJoyZone(mp)) JoyBegin(-1, mp); else Tap(mp); }
            else if (joyActive && Input.GetMouseButton(0)) JoyMove(mp);
            else if (joyActive) joyActive = false;
        }

        static (float d, Vector2 c) RectDist(Vector2 p, Rect r)
        {
            var c = new Vector2(Mathf.Clamp(p.x, r.xMin, r.xMax), Mathf.Clamp(p.y, r.yMin, r.yMax));
            return (Vector2.Distance(p, c), c);
        }

        void TickWorld()
        {
            FitCamera();
            float dt = Time.deltaTime;
            Vector2 v = Vector2.zero;
            if (phase == Phase.Build)
            {
                if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) v.y += 1;
                if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) v.y -= 1;
                if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) v.x -= 1;
                if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) v.x += 1;
                if (v == Vector2.zero && joyActive && joyVec.magnitude > JoyDead) v = joyVec; // analogue: partial tilt walks slower
                if (v != Vector2.zero) player.ClearTarget();
            }
            else player.ClearTarget();

            var old = pos;
            player.Step(v, dt); // keys / stick, or the tap target when there is no direct input
            lastMove = player.LastMove;
            if (lastMove != Vector2.zero)
            {
                if (!Online) PushCart(old);
                stepT += dt; if (stepT > 0.32f) { stepT = 0; Sfx("footstep"); }
            }

            // nearest station / cart
            detector.Evaluate(pos);
            string n = detector.NearestId(InteractableKind.Shelf);
            bool nc = detector.IsNear(InteractableKind.Cart);
            bool ntb = !Online && detector.IsNear(InteractableKind.Table);
            if (n != near || nc != nearCart || ntb != nearTable) { near = n; nearCart = nc; nearTable = ntb; if (phase == Phase.Build) Sfx("ui_tap"); RenderAll(); }

            // hero
            playerSr.transform.position = pos;
            playerSr.sortingOrder = Order(pos.y);
            if (Mathf.Abs(lastMove.x) > 0.0005f) playerSr.flipX = lastMove.x < 0;
            SetHeroAnim(hand.Count > 0 ? "carry" : lastMove != Vector2.zero ? "walk" : phase == Phase.Result && wins > losses ? "cheer" : "idle");

            for (int i = 0; i < 4; i++) if (stationSr[i] != null && art.Has("station_" + StationIds[i]))
                stationSr[i].color = phase == Phase.Build && near == StationIds[i] ? new Color(1f, 0.93f, 0.6f) : Color.white;
            if (art.Has("cart")) cartSr.color = phase == Phase.Build && nearCart ? new Color(1f, 0.93f, 0.6f) : Color.white;

            targetMarker.gameObject.SetActive(target.HasValue);
            if (target.HasValue) targetMarker.position = target.Value;
            TickSignals(dt);
        }

        IEnumerable<Rect> Solids() { foreach (var r in StationRects) yield return r; if (Online) yield return CartRect; yield return SofaRect; if (!Online) yield return TableRect; }

        void FitCamera()
        {
            // Full-screen world (mobile landscape): the camera fills the safe area and the HUD floats on top. The room is fitted
            // into the free band between the side columns (order ticket, cart) and the top / bottom strips; the themed room
            // picture behind it fills the rest of the screen.
            var sa = Screen.safeArea; float W = Mathf.Max(1, Screen.width), H = Mathf.Max(1, Screen.height);
            cam.rect = new Rect(sa.xMin / W, sa.yMin / H, Mathf.Max(0.1f, sa.width / W), Mathf.Max(0.1f, sa.height / H));
            float needW = RoomW + 0.3f, needH = RoomH + 0.9f; // + room above the back stations for their art
            float fh = Mathf.Max(0.3f, 1f - TopStrip - BottomStrip), fw = Mathf.Max(0.3f, 1f - 2 * SideCol);
            float V = Mathf.Max(needH / fh, needW / (Mathf.Max(0.1f, cam.aspect) * fw)); // world height of the whole view
            cam.orthographicSize = V / 2;
            float roomY = RoomH / 2 + 0.35f, bandCentre = (BottomStrip + 1f - TopStrip) / 2f;
            var c = new Vector3(RoomW / 2, roomY - (bandCentre - 0.5f) * V, -10);
            cam.transform.position = c;
            if (themeBg != null && themeBg.sprite != null)
            {
                // 3/4 look: the room picture is scaled so ITS floor (lower part) lies under the play area and its walls run off the top.
                var size = themeBg.sprite.bounds.size;
                float h = 11.5f, k = Mathf.Max(h / Mathf.Max(0.01f, size.y), V * cam.aspect * 1.05f / Mathf.Max(0.01f, size.x));
                themeBg.transform.localScale = Vector3.one * k;
                themeBg.transform.position = new Vector3(RoomW / 2, -0.8f + size.y * k / 2, 0);
            }
        }

        // ---------------------------------------------------------------- reacting to the rules

        void RenderWorld()
        {
            if (cartLayers == null) return;
            RenderTable();
            {
                foreach (Transform c in cartLayers) Destroy(c.gameObject);
                float y0 = CartRect.yMin + 0.42f; int layer = 0;
                for (int i = 0; i < stack.Count; i++)
                {
                    var id = stack[i]; var p = BobRules.Parts[id];
                    if (!BobRules.IsMethod(id))
                    {
                        float y = y0 + layer * 0.13f;
                        var slab = MakeSprite("Layer_" + id, cartLayers, "cart_layer", new Vector2(CartRect.center.x, y), 0.78f, false);
                        if (!art.Has("cart_layer")) slab.transform.localScale = new Vector3(0.74f, 0.1f, 1);
                        slab.color = BobUi.Family(p.Family); slab.sortingOrder = cartSr.sortingOrder + 1 + layer * 3;
                        var icon = MakeSprite("Icon", cartLayers, "part_" + id, new Vector2(CartRect.center.x - 0.22f, y + 0.02f), 0.22f, false);
                        icon.sortingOrder = slab.sortingOrder + 1;
                        layer++;
                    }
                    else
                    {
                        float y = y0 + Mathf.Max(0, layer - 1) * 0.13f + 0.02f;
                        var icon = MakeSprite("Method_" + id, cartLayers, "part_" + id, new Vector2(CartRect.center.x + 0.24f, y), 0.2f, false);
                        icon.sortingOrder = cartSr.sortingOrder + 2 + Mathf.Max(0, layer - 1) * 3;
                    }
                }
            }
            if (hand.Count != handShown && handIcons != null)
            {
                handShown = hand.Count;
                foreach (Transform c in handIcons) Destroy(c.gameObject);
                float k = 1f / Mathf.Max(0.001f, playerSr.transform.localScale.x);
                for (int i = 0; i < hand.Count; i++)
                {
                    var ic = MakeSprite("Held_" + hand[i], handIcons, "part_" + hand[i], Vector2.zero, 0.24f, false);
                    ic.transform.localPosition = new Vector3((-0.24f + i * 0.24f) * k, 1.12f * k, 0);
                    ic.transform.localScale *= k;
                    ic.sortingOrder = 2500;
                }
            }
        }

        void PopHand() { if (handIcons != null) StartCoroutine(BobTween.Pop(playerSr.transform, 0.2f, 1.12f)); }

        void OnLayerPlaced(List<Effect> fresh)
        {
            StartCoroutine(BobTween.Squash(cartT));
            float y = CartTop.y + 0.25f;
            foreach (var f in fresh)
            {
                var col = f.Sum >= 0 ? BobUi.Good : BobUi.Bad;
                var tm = WorldLabel((f.Sum >= 0 ? "+ " : "- ") + f.Text, new Vector2(CartRect.center.x, y), 0.026f, col);
                StartCoroutine(BobTween.FloatUp(tm.transform, 0.6f, 1.4f));
                y += 0.22f;
                if (f.Sum >= 0) { Sfx("reaction_good"); SpawnFx("fx_sparkle", CartTop); }
                else { Sfx("reaction_bad"); SpawnFx(f.Text.Contains("ฟู่") ? "fx_fizz" : "fx_poof", CartTop); StartCoroutine(BobTween.Shake(cartT, 0.06f)); Buzz(); }
            }
        }

        void SpawnFx(string name, Vector2 at)
        {
            var e = art.Find(name); if (e == null) return;
            var sr = MakeSprite(name, worldRoot, name, at, 0.7f, false); sr.sortingOrder = 2800;
            var an = sr.gameObject.AddComponent<BobSpriteAnimator>(); an.destroyWhenDone = true; an.Play(e.frames, e.fps, false);
        }

        void OnRoundTested(bool win)
        {
            if (!enemiesLive) ActivateEnemies(false);
            foreach (var en in enemies)
            {
                var e = art.Find(en.id + (win ? "_defeat" : "_hit"));
                if (e != null) en.anim.Play(e.frames, e.fps, false);
            }
            if (win)
            {
                Sfx("enemy_defeat");
                foreach (var t in floorTiles) t.sprite = art.Get("floor_tile");
                SpawnFx("fx_sparkle", new Vector2(RoomW / 2, 3.8f)); SpawnFx("fx_sparkle", CartTop);
            }
            else Sfx("enemy_hit");
        }
    }
}
