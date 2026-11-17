// Battle of the Brooms: the play area of the S28 card (docs 11 section 4.4, docs 16 section 5–6). One component
// hosts every card minigame: trace (T01, T14), scrub (T02, T07 step 1, T08 step 2, T09 step 2), tap (T03, T10,
// T07 step 3, T08 step 1), sort (T05), hold (T06, T07 step 2, T09 step 1, T13), fill (T11) and sequence (T15).
//
// T04 (cart) and T12 (chairs) stay in the world: Begin returns false for them and the HUD keeps its timed bar, as
// the spec asks for no card on those two. Sabotage cards are timed too.
//
// The host owns only the 1240 x 620 play area and the pointer surface; the HUD keeps the frame, the title, the
// progress bar and the × button. Ticks are fed by the HUD with unscaled delta time, and a completion is signalled
// by Tick returning true, so TaskSpot.Close(true) stays the only place points are awarded.
//
// Mistakes cost time only (docs 16 section 3): a wrong tap restarts the sequence, an overflowing bottle keeps half
// the liquid, a miss just does not count. Closing the card keeps the step progress through TaskSpot's 10 s window
// only at the station level; the card restarts its current step.
using System;
using System.Collections.Generic;
using Bob.Art;
using Bob.Game;
using Bob.Rules;
using Bob.UI;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Bob.TaskRace
{
    public enum MiniKind { Timed, Trace, Scrub, Tap, Sort, Hold, Fill, Sequence, Cart }

    [DisallowMultipleComponent]
    public class BobMinigameHost : MonoBehaviour
    {
        public const float AreaW = 1240f, AreaH = 620f;
        const float HalfW = AreaW * 0.5f - 20f, HalfH = AreaH * 0.5f - 24f;

        static BobUiTheme T => BobUiTheme.Current;

        sealed class Item
        {
            public RectTransform rect;
            public Image img;
            public Transform twin;
            public Vector2 home, vel;
            public float hp = 1f, life, maxLife, size;
            public bool alive = true, removed;
            public int bin;
            public Vector2 slotHome;          // sort: the dealt slot a wrong drop returns to
            public float popLife, punch, shake;
            public bool flying, flyRemove;
            public Vector2 flyFrom, flyTo;
            public float flyT;
        }

        sealed class RingFx { public Image img; public float t, life, from, to, spin, aspect = 1f; public Color colour; }

        /// <summary>A short-lived "+3" / "-2" pill over a stat bar, so a reaction is seen without a sentence.</summary>
        sealed class CartBadge
        {
            public Image img;
            public TMP_Text label;
            public float life, from, fraction;
            public int stat;
            public Color colour;
        }

        // Among Us feedback grammar: every touch answers back inside one frame and the answer is over in < 0.3 s.
        const float PopTime = 0.18f, RingTime = 0.30f, PunchTime = 0.20f, ShakeTime = 0.26f, FlyTime = 0.16f;

        RectTransform play, layer, fx;
        BobMinigameStage stage3d;
        MiniSurface surface;
        TMP_Text waitLabel, placeholder;
        int slimeVariant;

        TaskSpot spot;
        string taskId = "";
        MiniKind kind = MiniKind.Timed, stepKind = MiniKind.Timed;
        int difficulty = 1, step, stepCount = 1;
        bool emergency; // T16: no station, the stain itself is the task
        float stepProgress, holdFill, waitTimer, fillLevel, bandCentre, bandHalf;
        bool playing, complete, holding;
        Vector2 lastPointer, holdCentre;
        Item held;
        Vector2 heldOffset;
        float turnAccum, lastAngle;

        struct GrimeDot { public Item Dot, Stain; }

        readonly List<Item> items = new List<Item>();
        readonly List<Item> bins = new List<Item>();
        readonly List<Item> pads = new List<Item>();
        readonly List<GrimeDot> grime = new List<GrimeDot>();
        readonly List<GameObject> decor = new List<GameObject>();
        readonly List<RingFx> rings = new List<RingFx>();
        readonly List<int> sequence = new List<int>();
        int seqIndex, playIndex;
        float flashTimer;
        int flashPad = -1, stage;
        float bandFlash, lastPuff = -1f;

        static readonly Vector2 FillBottleAt = new Vector2(-80f, -10f);

        // The Leonardo RAW set (art/docs/25) cut to RGBA by art/tools/raw_vfx_to_unity.py: a hit star, a step
        // sparkle, scrub foam, a dust puff and a splash. Missing files just fall back to the flat ring.
        static bool fxLoaded;
        static Sprite fxHit, fxSparkle, fxFoam, fxDust, fxSplash, fxSplat;

        Image brush, meterTrack, meterFill, bottleFill, band;
        Item holdButton;
        bool bandLit;
        bool rotate;
        float holdSeconds = 2f;
        int turnRequired = 6;
        float pourSpeed = 0.3f;

        // ---- T04 cart (docs 16 section 4): pick four parts off the shelf, stack them on the cart, hold to lock the
        // formula. The stack is judged by the shipped Bob.Rules reactions, the same file the server scores with.
        readonly List<string> cartShelf = new List<string>();
        readonly List<string> cartTray = new List<string>();
        readonly List<string> cartStack = new List<string>();
        readonly List<Item> cartChips = new List<Item>();
        readonly List<Item> cartLayers = new List<Item>();
        readonly List<Image> cartBars = new List<Image>();
        readonly List<Image> cartMarks = new List<Image>();   // parallel to cartLayers: the reaction mark, or null
        readonly List<TMP_Text> cartValues = new List<TMP_Text>();
        readonly List<CartBadge> cartBadges = new List<CartBadge>();
        sealed class CartPartView
        {
            public RectTransform icon, check;
            public TMP_Text name, cost;
        }
        sealed class CartSlotView
        {
            public Item item;
            public TMP_Text name, cost, hint;
        }
        readonly List<Item> cartShelfCards = new List<Item>();
        readonly Dictionary<Item, CartPartView> cartPartViews = new Dictionary<Item, CartPartView>();
        readonly List<CartSlotView> cartSlots = new List<CartSlotView>();
        readonly List<Image> cartPickSlots = new List<Image>();
        readonly List<TMP_Text> cartPickNames = new List<TMP_Text>();
        readonly List<TMP_Text> cartMetWords = new List<TMP_Text>();
        readonly List<Image> cartTargets = new List<Image>();
        readonly List<Action<float>> cartLayout = new List<Action<float>>();
        Image cartDropZone, cartInlineHint, cartPickFill;
        TMP_Text cartPickCount, cartTrayEmpty, cartTrayHint, cartSlotCount;
        string cartError = "";
        float cartLayoutWidth = -1f;
        const float CartHeight = 780f, CartGap = 16f, CartHitMin = 144f;
        readonly bool[] cartBarMet = new bool[4];
        Item cartChipHeld;
        Image cartLock, cartLockFill;
        TMP_Text cartLockLabel, cartRecipe, cartCostText;
        Image cartBudgetFill;
        Dictionary<string, int> cartNeed = new Dictionary<string, int>();
        int cartSeed, cartStar;
        readonly int[] cartStats = new int[4];
        float cartHold;
        bool cartLocked;
        bool cartRecipeIsBuild;

        // Team buffs the round hands to every card (docs 16 section 4): set by the HUD before Begin, read by the
        // card setups so a won buff changes the next card for real instead of only being announced.
        IReadOnlyList<string> teamBuffs = Array.Empty<string>();
        readonly List<GameObject> buffRibbon = new List<GameObject>();
        float buffRibbonTime;
        float flashSeconds = 0.45f; // T15: shine stretches the flash

        /// <summary>T04: the four stats of the locked cart (clean, gentle, speed, shine), zero before the lock.</summary>
        public int[] CartStats => cartStats;
        /// <summary>T04: how many worksheet conditions the locked cart met, 0-4 (see BobTaskRaceRules.CartNeed).</summary>
        public int CartPassCount { get; private set; }
        /// <summary>T04: did the formula pass (the buff) or only the lock (the step)?</summary>
        public bool CartPassed { get; private set; }

        /// <summary>
        /// HUD: hand the round's cart buffs to the next card before Begin. Same string keys as CartKeys, so a card
        /// only reads the buff it understands (clean = easier scrub, gentle = calmer motion, shine = easier precision).
        /// </summary>
        public void SetRoundBuffs(IReadOnlyList<string> buffs)
        {
            teamBuffs = buffs ?? Array.Empty<string>();
        }

        bool Buffed(string key)
        {
            for (int i = 0; i < teamBuffs.Count; i++)
                if (teamBuffs[i] == key) return true;
            return false;
        }


        System.Random rng = new System.Random(7);

        public bool Playing => playing;
        public bool Complete => complete;
        public int StepNumber => step + 1;
        public int StepCount => stepCount;
        public string StepText => stepCount > 1 ? $"ขั้น {step + 1}/{stepCount}" : "ขั้น 1/1";
        public string Hint { get; private set; } = "";
        public float Progress => complete ? 1f : Mathf.Clamp01((step + stepProgress) / Mathf.Max(1, stepCount));

        // ---------------------------------------------------------------- build

        public void Build(RectTransform card)
        {
            LoadFx();
            play = BobUi.Rect("MiniPlay", card);
            BobUi.Pin(play, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 30f), new Vector2(AreaW, AreaH));
            var bg = play.gameObject.AddComponent<Image>();
            bg.color = new Color(T.surfaceCream.r, T.surfaceCream.g, T.surfaceCream.b, 0.9f);
            bg.raycastTarget = true;
            surface = play.gameObject.AddComponent<MiniSurface>();
            surface.host = this;

            // The 3D stage is the backdrop and the bodies; the rects below stay as hitboxes and labels.
            var stageGo = new GameObject("MiniStage3D");
            stageGo.transform.SetParent(transform, false);
            stage3d = stageGo.AddComponent<BobMinigameStage>();
            stage3d.Build(play);

            layer = BobUi.Rect("Items", play);
            BobUi.Stretch(layer);
            fx = BobUi.Rect("Fx", play);
            BobUi.Stretch(fx);

            brush = BobUi.Disc(fx, new Color(1f, 1f, 1f, 0.75f), 88f, "Brush");
            brush.raycastTarget = false;
            brush.gameObject.SetActive(false);

            waitLabel = BobUi.Label(fx, "", 46, T.ink, TextAnchor.MiddleCenter, FontStyle.Bold);
            BobUi.Pin(waitLabel.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 120f), new Vector2(900f, 70f));
            waitLabel.gameObject.SetActive(false);

            placeholder = BobUi.Label(fx, "", 46, T.inkMuted, TextAnchor.MiddleCenter, FontStyle.Bold);
            BobUi.Pin(placeholder.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(1000f, 180f));
            placeholder.gameObject.SetActive(false);
        }

        /// <summary>HUD: start the minigame for this station. False when the task is a timed (world) card.</summary>
        public bool Begin(TaskSpot station, string id, int roundDifficulty)
        {
            taskId = BaseId(id);
            kind = KindOf(taskId);
            SetPlayArea(kind == MiniKind.Cart);
            if (kind == MiniKind.Timed) { Hide(); return false; }

            emergency = false;
            spot = station;
            difficulty = Mathf.Clamp(roundDifficulty, 1, 3);
            stepCount = StepsOf(taskId);
            step = 0; stepProgress = 0f; complete = false; playing = true;
            placeholder.gameObject.SetActive(false);
            play.gameObject.SetActive(true);
            SetupStep();
            if (kind != MiniKind.Cart) BobTutorialClip.Play(play, taskId);
            BobMinigameSfx.Open();
            return true;
        }

        /// <summary>
        /// HUD: the T16 emergency card (docs 16 section 4). There is no station: the enemy is the task, so the same
        /// play area runs the weakness minigame of the stain at the difficulty the caller chose. False when the id is
        /// a world card, which T16 never deals.
        /// </summary>
        public bool BeginEmergency(string id, int roundDifficulty)
        {
            taskId = BaseId(id);
            kind = KindOf(taskId);
            SetPlayArea(kind == MiniKind.Cart);
            if (kind == MiniKind.Timed) { Hide(); return false; }

            emergency = true;
            spot = null;
            difficulty = Mathf.Clamp(roundDifficulty, 1, 3);
            stepCount = StepsOf(taskId);
            step = 0; stepProgress = 0f; complete = false; playing = true;
            placeholder.gameObject.SetActive(false);
            play.gameObject.SetActive(true);
            SetupStep();
            if (kind != MiniKind.Cart) BobTutorialClip.Play(play, taskId);
            BobMinigameSfx.Open();
            return true;
        }

        /// <summary>HUD: the card is a timed one (sabotage / cart / chairs): show one line instead of a play area.</summary>
        public void ShowPlaceholder(string text)
        {
            SetPlayArea(false);
            playing = false; complete = false;
            HideItems();
            if (stage3d != null) stage3d.Hide();
            play.gameObject.SetActive(true);
            placeholder.text = text;
            placeholder.gameObject.SetActive(true);
            waitLabel.gameObject.SetActive(false);
            if (brush != null) brush.gameObject.SetActive(false);
        }

        public void Hide()
        {
            playing = false;
            emergency = false;
            BobMinigameSfx.Scrub(false); BobMinigameSfx.Pour(false);
            HideItems();
            if (stage3d != null) stage3d.Hide();
            if (play != null) play.gameObject.SetActive(false);
            if (waitLabel != null) waitLabel.gameObject.SetActive(false);
            if (placeholder != null) placeholder.gameObject.SetActive(false);
        }

        /// <summary>HUD closed the card (× or joystick): drop the step state, the station keeps its own retention.</summary>
        public void Cancel()
        {
            playing = false; complete = false;
            emergency = false;
            BobMinigameSfx.Scrub(false); BobMinigameSfx.Pour(false);
            HideItems();
            if (stage3d != null) stage3d.Hide();
            if (brush != null) brush.gameObject.SetActive(false);
        }

        /// <summary>Verify only: finish the current card without input.</summary>
        public void DebugSolve()
        {
            stepProgress = 1f;
            Advance();
        }

        /// <summary>Verify only: fire one ring and squash one live item, so the log proves the new tweens run.</summary>
        public string DebugFeedbackProbe()
        {
            Ring(Vector2.zero, T.success, 200f);
            foreach (var it in items)
            {
                if (!it.alive || it.popLife > 0f) continue;
                it.alive = false; it.removed = true;
                Pop(it, T.primaryLight);
                break;
            }
            return DebugFeedbackState;
        }

        public string DebugFeedbackState
        {
            get
            {
                int popping = 0, flying = 0;
                foreach (var it in items)
                {
                    if (it.popLife > 0f) popping++;
                    if (it.flying) flying++;
                }
                int art = (fxHit != null ? 1 : 0) + (fxSparkle != null ? 1 : 0) + (fxFoam != null ? 1 : 0) +
                          (fxDust != null ? 1 : 0) + (fxSplash != null ? 1 : 0) + (fxSplat != null ? 1 : 0);
                return $"rings={rings.Count} popping={popping} flying={flying} fx={art}/6";
            }
        }

        // ---------------------------------------------------------------- tick

        public bool Tick(float dt)
        {
            RefreshCartLayout();
            TickJuice(dt); // feedback keeps moving even when the card is already solved
            Sync3D();
            if (complete) return true;
            if (!playing || (spot == null && !emergency)) return false;

            if (waitTimer > 0f)
            {
                waitTimer = Mathf.Max(0f, waitTimer - dt);
                waitLabel.text = "รอคราบนุ่ม " + Mathf.CeilToInt(waitTimer) + " วิ";
                if (waitTimer <= 0f)
                {
                    waitLabel.gameObject.SetActive(false);
                    Hint = HintFor(taskId, step);
                }
                return false;
            }

            switch (stepKind)
            {
                case MiniKind.Tap: TickTap(dt); break;
                case MiniKind.Hold: TickHold(dt); break;
                case MiniKind.Fill: TickFill(dt); break;
                case MiniKind.Sequence: TickSequence(dt); break;
                case MiniKind.Cart: TickCart(dt); break;
                case MiniKind.Scrub: PaintScrub(); break;
                case MiniKind.Sort: break;
                case MiniKind.Trace: break;
            }

            if (stepProgress >= 1f) Advance();
            return complete;
        }

        void Advance()
        {
            if (stepKind == MiniKind.Hold)
            {
                Ring(holdCentre, T.success, 360f);
                Fx(fxSparkle, holdCentre, 420f, 0.5f, 35f);
            }
            if (stepKind == MiniKind.Fill)
            {
                Ring(FillBottleAt, T.success, 320f);
                Fx(fxSparkle, FillBottleAt, 380f, 0.5f, 35f);
                bandFlash = 0.4f;
            }
            if (step + 1 < stepCount)
            {
                BobMinigameSfx.StepDone();
                step++;
                SetupStep();
                // T09: the soak wait sits between the pour and the scrub, exactly as docs 16 section 5 asks.
                if (taskId == "T09" && step == 1)
                {
                    waitTimer = 4f;
                    waitLabel.gameObject.SetActive(true);
                    waitLabel.text = "รอคราบนุ่ม 4 วิ";
                }
                return;
            }
            complete = true; playing = false;
            BobMinigameSfx.Finished();
            Hint = "เสร็จแล้ว";
            if (spot != null) spot.SetProgress(1f);
        }

        /// <summary>Among Us feedback grammar: every touch answers in one frame — a ring blooms under the finger,
        /// the hit body squashes away, a wrong drop flies back and shakes, a sorted card flies into its bin.</summary>
        void TickJuice(float dt)
        {
            for (int i = rings.Count - 1; i >= 0; i--)
            {
                var r = rings[i];
                r.t += dt;
                float u = Mathf.Clamp01(r.t / r.life);
                float k = 1f - (1f - u) * (1f - u); // ease-out: fast bloom, soft finish
                var rt = (RectTransform)r.img.transform;
                float s = Mathf.Lerp(r.from, r.to, k);
                rt.sizeDelta = new Vector2(s, s * r.aspect);
                if (r.spin != 0f) rt.localRotation = Quaternion.Euler(0f, 0f, r.spin * k);
                var c = r.colour; c.a *= 1f - u;
                r.img.color = c;
                if (u >= 1f) { DestroyNow(r.img.gameObject); rings.RemoveAt(i); }
            }

            for (int i = 0; i < items.Count; i++)
                if (!pads.Contains(items[i])) TickItem(items[i], dt); // pads live in both lists: tick them once
            for (int i = 0; i < bins.Count; i++) TickItem(bins[i], dt);
            for (int i = 0; i < pads.Count; i++) TickItem(pads[i], dt);

            if (band != null)
            {
                bandFlash = Mathf.Max(0f, bandFlash - dt);
                var c = T.success;
                c.a = 0.35f + 0.55f * Mathf.Clamp01(bandFlash / 0.4f); // the green band blinks as the liquid reaches it
                band.color = c;
            }

            // Reaction pills drift up and fade, so the numbers read as an answer to the drop, not as furniture.
            for (int i = cartBadges.Count - 1; i >= 0; i--)
            {
                var b = cartBadges[i];
                b.life -= dt;
                if (b.img == null) { cartBadges.RemoveAt(i); continue; }
                var rt = (RectTransform)b.img.transform;
                rt.anchoredPosition = CartDeltaPoint(b.stat, b.fraction) + new Vector2(0f, 8f * (b.from - b.life) / b.from);
                float fade = Mathf.Clamp01(b.life / b.from);
                var c = b.colour; c.a = fade;
                b.img.color = c;
                if (b.label != null) { var lc = b.label.color; lc.a = fade; b.label.color = lc; }
                if (b.life <= 0f) { DestroyNow(b.img.gameObject); cartBadges.RemoveAt(i); }
            }
            if (buffRibbonTime > 0f)
            {
                buffRibbonTime -= dt;
                if (buffRibbonTime <= 0f)
                    foreach (var go in buffRibbon) if (go != null) go.SetActive(false);
            }
        }

        void TickItem(Item it, float dt)
        {
            if (it == null || it.rect == null) return;
            if (it.popLife > 0f)
            {
                it.popLife -= dt;
                float k = Mathf.Clamp01(it.popLife / PopTime);
                it.rect.localScale = Vector3.one * (0.45f * k + 0.55f * k * k);
                if (it.popLife <= 0f) it.rect.gameObject.SetActive(false);
                return;
            }
            if (it.punch > 0f) it.punch = Mathf.Max(0f, it.punch - dt);
            if (it.flying)
            {
                it.flyT += dt / FlyTime;
                float u = Mathf.Clamp01(it.flyT);
                float k = 1f - (1f - u) * (1f - u);
                it.home = Vector2.Lerp(it.flyFrom, it.flyTo, k);
                it.rect.anchoredPosition = it.home;
                if (u >= 1f)
                {
                    it.flying = false;
                    if (it.flyRemove) Pop(it, T.primaryLight);
                    else { it.rect.localScale = Vector3.one; it.shake = ShakeTime; }
                }
                return;
            }
            if (it.shake > 0f)
            {
                it.shake = Mathf.Max(0f, it.shake - dt);
                float amp = 18f * (it.shake / ShakeTime);
                it.rect.anchoredPosition = it.home + new Vector2(Mathf.Sin(Time.unscaledTime * 70f) * amp, 0f);
                if (it.shake <= 0f) it.rect.anchoredPosition = it.home;
            }
        }

        void Ring(Vector2 at, Color colour, float size)
        {
            if (fx == null) return;
            var img = BobUi.Disc(fx, new Color(colour.r, colour.g, colour.b, 0.55f), size * 0.35f, "HitRing");
            img.raycastTarget = false;
            ((RectTransform)img.transform).anchoredPosition = at;
            rings.Add(new RingFx
            {
                img = img, life = RingTime, from = size * 0.35f, to = size * 1.5f,
                colour = new Color(colour.r, colour.g, colour.b, 0.55f),
            });
        }

        /// <summary>A painted burst instead of the flat ring: the Leonardo sprite blooms, turns and fades the same way.</summary>
        void Fx(Sprite sprite, Vector2 at, float size, float life, float spin = 0f)
        {
            if (fx == null || sprite == null) return;
            var r = BobUi.Rect("Burst", fx);
            var img = r.gameObject.AddComponent<Image>();
            img.sprite = sprite;
            img.raycastTarget = false;
            float aspect = sprite.rect.height / Mathf.Max(1f, sprite.rect.width);
            r.anchoredPosition = at;
            r.sizeDelta = new Vector2(size * 0.35f, size * 0.35f * aspect);
            var colour = Color.white;
            img.color = colour;
            rings.Add(new RingFx
            {
                img = img, life = life, from = size * 0.35f, to = size * 1.5f,
                colour = colour, spin = spin, aspect = aspect,
            });
        }

        static void LoadFx()
        {
            if (fxLoaded) return;
            fxLoaded = true;
            fxHit = LoadSprite("fx_hitstar");
            fxSparkle = LoadSprite("fx_sparkle");
            fxFoam = LoadSprite("fx_foam");
            fxDust = LoadSprite("fx_dust");
            fxSplash = LoadSprite("fx_splash");
            fxSplat = LoadSprite("fx_slimesplat");
        }

        /// <summary>The PNGs import as plain textures in this 3D project, so the sprite is built here, not imported.</summary>
        static Sprite LoadSprite(string file)
        {
            var tex = Resources.Load<Texture2D>("Textures/Minigame/" + file);
            if (tex == null) return null;
            return Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
        }

        /// <summary>Squash an item away; the hitbox dies at once, the body shrinks for PopTime.</summary>
        void Pop(Item it, Color ringColour, Sprite art = null)
        {
            if (it == null || it.rect == null || it.popLife > 0f) return;
            it.popLife = PopTime;
            // Big bodies (cards, slimes, pads) get the painted star; small ones keep the cheap ring so a row of
            // dust specks does not turn into a wall of sparks. A caller can force its own sprite.
            var sprite = art != null ? art : it.size >= 120f ? fxHit : null;
            if (sprite != null) Fx(sprite, it.rect.anchoredPosition, it.size * 1.7f, PopTime * 2.2f, 70f);
            else Ring(it.rect.anchoredPosition, ringColour, it.size * 1.4f);
        }

        void StartFly(Item it, Vector2 to, bool removeAtEnd)
        {
            if (it == null || it.rect == null) return;
            it.flying = true; it.flyRemove = removeAtEnd;
            it.flyFrom = it.rect.anchoredPosition; it.flyTo = to; it.flyT = 0f;
        }

        void ClearRings()
        {
            foreach (var r in rings)
                if (r.img != null) DestroyNow(r.img.gameObject);
            rings.Clear();
        }

        /// <summary>Keep every 3D twin on the stage under its hitbox, and hidden exactly when the hitbox is.</summary>
        void Sync3D()
        {
            if (stage3d == null || !stage3d.Ready) return;
            foreach (var it in items) SyncTwin(it);
            foreach (var bin in bins) SyncTwin(bin);
            foreach (var pad in pads) SyncTwin(pad);
            foreach (var dot in grime) SyncTwin(dot.Dot);
        }

        void SyncTwin(Item it)
        {
            if (it == null || it.twin == null) return;
            bool on = it.alive || it.popLife > 0f || it.flying;
            on = on && it.rect != null && it.rect.gameObject.activeSelf;
            stage3d.SetActive(it.twin, on);
            if (!on) return;
            float punch = it.punch > 0f ? 1f + 0.18f * Mathf.Sin((1f - it.punch / PunchTime) * Mathf.PI) : 1f;
            stage3d.SetSize(it.twin, it.rect.localScale.y * punch);
            stage3d.Place(it.twin, it.rect.anchoredPosition); // after the size, so the contact shadow scales with the body
        }

        void LateUpdate()
        {
            RefreshCartLayout();
            Sync3D();
        }

        // ---------------------------------------------------------------- pointer

        internal void PointerDown(Vector2 p)
        {
            if (!playing || waitTimer > 0f) return;
            lastPointer = p;
            if (brush != null)
            {
                brush.gameObject.SetActive(stepKind == MiniKind.Trace || stepKind == MiniKind.Scrub);
                brush.rectTransform.anchoredPosition = p;
            }

            switch (stepKind)
            {
                case MiniKind.Tap: TapAt(p); break;
                case MiniKind.Sort: PickUp(p); break;
                case MiniKind.Hold: StartHold(p); break;
                case MiniKind.Fill: holding = true; BobMinigameSfx.Pour(true); break;
                case MiniKind.Sequence: PadAt(p); break;
                case MiniKind.Trace: CoverAt(p); break;
                case MiniKind.Scrub: BobMinigameSfx.Scrub(true); ScrubAt(0f, p); break;
                case MiniKind.Cart: CartDown(p); break;
            }
        }

        internal void PointerDrag(Vector2 p)
        {
            if (!playing || waitTimer > 0f) return;
            var delta = p - lastPointer;
            lastPointer = p;
            if (brush != null) brush.rectTransform.anchoredPosition = p;

            switch (stepKind)
            {
                case MiniKind.Trace: CoverAt(p); break;
                case MiniKind.Scrub: ScrubAt(delta.magnitude, p); break;
                case MiniKind.Sort: DragHeld(p); break;
                case MiniKind.Hold: TurnAt(p); break;
                case MiniKind.Cart: CartDrag(p); break;
            }
        }

        internal void PointerUp(Vector2 p)
        {
            if (!playing) return;
            if (brush != null) brush.gameObject.SetActive(false);

            if (stepKind == MiniKind.Scrub) BobMinigameSfx.Scrub(false);
            if (stepKind == MiniKind.Sort) DropHeld(p);
            if (stepKind == MiniKind.Cart) CartUp(p);
            if (stepKind == MiniKind.Hold) holding = false;
            if (stepKind == MiniKind.Fill && holding)
            {
                holding = false;
                BobMinigameSfx.Pour(false);
                if (Mathf.Abs(fillLevel - bandCentre) <= bandHalf) { stepProgress = 1f; Hint = "ระดับพอดี"; }
                else if (fillLevel > bandCentre + bandHalf)
                {
                    fillLevel = 0.5f;
                    Hint = "ล้น! ลองใหม่";
                    bandLit = false;
                    BobMinigameSfx.Wrong();
                    Ring(FillBottleAt, T.danger, 300f);
                }
            }
        }

        // ---------------------------------------------------------------- steps

        void SetupStep()
        {
            HideItems();
            BobMinigameSfx.Scrub(false); BobMinigameSfx.Pour(false);
            stepProgress = 0f; holdFill = 0f; waitTimer = 0f; fillLevel = 0f;
            turnAccum = 0f; lastAngle = float.NaN; held = null; holding = false;
            seqIndex = 0; playIndex = 0; flashTimer = 0.35f; flashPad = -1; stage = taskId == "T15" ? 0 : 1;
            bandLit = false; lastPuff = -1f;
            rng = new System.Random((spot != null ? spot.SpotId.GetHashCode() : 7) ^ (step * 397) ^ taskId.GetHashCode());

            stepKind = kind;
            rotate = false;
            if (taskId == "T07") stepKind = step == 0 ? MiniKind.Scrub : step == 1 ? MiniKind.Hold : MiniKind.Tap;
            else if (taskId == "T08") stepKind = step == 0 ? MiniKind.Tap : MiniKind.Scrub;
            else if (taskId == "T09") stepKind = step == 0 ? MiniKind.Hold : MiniKind.Scrub;
            if (taskId == "T06") rotate = true;
            Hint = HintFor(taskId, step);

            // T04: the shelf and the stack survive the step change; the lock and the drag do not.
            cartChipHeld = null; cartHold = 0f; cartLocked = false; CartPassCount = 0; CartPassed = false;
            cartRecipeIsBuild = false;
            if (taskId == "T04" && step == 0)
            {
                cartShelf.Clear(); cartTray.Clear(); cartStack.Clear();
                cartChips.Clear(); cartLayers.Clear(); cartBars.Clear();
                for (int i = 0; i < 4; i++) cartBarMet[i] = false;
                cartSeed = spot != null ? spot.SpotId.GetHashCode() : 1975;
                cartStar = BobTaskRaceRules.CartStar(cartSeed);
                cartNeed = BobTaskRaceRules.CartNeed(cartSeed, difficulty);
            }

            slimeVariant = 0;
            if (stage3d != null && stage3d.Ready)
            {
                if (stepKind == MiniKind.Cart) stage3d.Hide(); // R1 is native uGUI, not stage twins with hidden faces.
                else stage3d.Show(taskId); // other minigames keep their existing stage
            }
            if (stepKind != MiniKind.Cart) AddContext();
            switch (stepKind)
            {
                case MiniKind.Trace: SetupTrace(); break;
                case MiniKind.Scrub: SetupScrub(); break;
                case MiniKind.Tap: SetupTap(); break;
                case MiniKind.Sort: SetupSort(); break;
                case MiniKind.Hold: SetupHold(); break;
                case MiniKind.Fill: SetupFill(); break;
                case MiniKind.Sequence: SetupSequence(); break;
                case MiniKind.Cart: SetupCart(); break;
            }
            ShowBuffRibbon();
        }

        /// <summary>
        /// A short row of the buffs the team won from the cart, shown for a few seconds as the card opens. The colour
        /// is the stat's colour, so the same chip reads on the worksheet and here; the row fades, it does not nag.
        /// </summary>
        void ShowBuffRibbon()
        {
            if (taskId == "T04" || teamBuffs.Count == 0) return;
            float x = -598f;
            for (int k = 0; k < BobTaskRaceRules.CartKeys.Length; k++)
            {
                if (!Buffed(BobTaskRaceRules.CartKeys[k])) continue;
                var chip = BobUi.Panel(layer, CartStatColour[k], "ui_panel", "BuffChip");
                chip.raycastTarget = false;
                BobUi.Pin((RectTransform)chip.transform, new Vector2(0.5f, 0.5f), new Vector2(0f, 0.5f),
                    new Vector2(x, 262f), new Vector2(112f, 30f));
                var text = BobUi.Label(chip.transform, CartBarLabels[k], 18, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
                BobUi.Stretch(text.rectTransform);
                Track(chip.gameObject);
                buffRibbon.Add(chip.gameObject);
                x += 118f;
            }
            buffRibbonTime = 4f;
        }

        /// <summary>The room the task happens in, painted under the items: a counter for scrubbing, a tiled floor for
        /// sweeping, a window for the glass, a machine front for the washer. Flat shapes only, so the card keeps the
        /// same toon look as the diorama and no new sprites are needed.</summary>
        void AddContext()
        {
            var wall = new Color(0.87f, 0.92f, 0.97f, 1f);
            var wood = new Color(0.93f, 0.82f, 0.62f, 1f);
            var woodEdge = new Color(0.74f, 0.58f, 0.37f, 1f);
            var steel = new Color(0.83f, 0.88f, 0.92f, 1f);

            if (taskId == "T08")
            {
                Bg(Vector2.zero, new Vector2(AreaW, AreaH), new Color(0.55f, 0.78f, 0.97f, 1f));
                Bg(new Vector2(0f, 0f), new Vector2(AreaW, 16f), Color.white);
                Bg(new Vector2(0f, 190f), new Vector2(AreaW, 12f), Color.white);
                Bg(new Vector2(0f, -190f), new Vector2(AreaW, 12f), Color.white);
                for (int i = -1; i <= 1; i++) Bg(new Vector2(i * 400f, 0f), new Vector2(12f, AreaH), Color.white);
            }
            else if (taskId == "T10")
            {
                Bg(Vector2.zero, new Vector2(AreaW, AreaH), new Color(0.80f, 0.85f, 0.92f, 1f));
                Bg(new Vector2(0f, 280f), new Vector2(AreaW, 60f), new Color(0.55f, 0.40f, 0.25f, 1f));
                Bg(new Vector2(-330f, 180f), new Vector2(34f, 200f), new Color(0.58f, 0.42f, 0.27f, 1f));
                Bg(new Vector2(330f, 180f), new Vector2(34f, 200f), new Color(0.58f, 0.42f, 0.27f, 1f));
            }
            else if (taskId == "T02" || taskId == "T09")
            {
                Bg(new Vector2(0f, 140f), new Vector2(AreaW, 340f), wall);
                Bg(new Vector2(0f, -110f), new Vector2(AreaW, 380f), wood);
                Bg(new Vector2(0f, -300f), new Vector2(AreaW, 18f), woodEdge);
            }
            else if (taskId == "T07")
            {
                Bg(new Vector2(0f, -60f), new Vector2(AreaW, 500f), steel);
                Bg(new Vector2(0f, -310f), new Vector2(AreaW, 10f), new Color(0.6f, 0.68f, 0.75f, 1f));
            }
            else if (taskId == "T01" || taskId == "T14")
            {
                TileFloor(taskId == "T14" ? new Color(0.86f, 0.90f, 0.82f, 1f) : new Color(0.85f, 0.88f, 0.94f, 1f));
            }
            else if (stepKind == MiniKind.Sort)
            {
                TileFloor(new Color(0.93f, 0.88f, 0.78f, 1f));
            }
            else if (stepKind == MiniKind.Tap)
            {
                TileFloor(new Color(0.84f, 0.90f, 0.86f, 1f));
            }
            else if (stepKind == MiniKind.Hold)
            {
                // Diameter = the 180 px touch radius, and faint: on the 3D stage a strong white disc reads as a torch beam.
                var ring = BobUi.Disc(layer, new Color(1f, 1f, 1f, 0.14f), 360f, "HoldRing");
                ring.raycastTarget = false;
                ((RectTransform)ring.transform).anchoredPosition = new Vector2(0f, -20f);
                Track(ring.gameObject);
            }
            else if (stepKind == MiniKind.Fill)
            {
                Bg(new Vector2(0f, -250f), new Vector2(AreaW, 120f), wood);
                Bg(new Vector2(0f, -312f), new Vector2(AreaW, 10f), woodEdge);
            }
            else if (stepKind == MiniKind.Sequence)
            {
                Bg(Vector2.zero, new Vector2(AreaW, AreaH), new Color(0.90f, 0.92f, 0.95f, 1f));
                Bg(new Vector2(0f, 210f), new Vector2(1120f, 140f), new Color(0.78f, 0.83f, 0.89f, 1f));
                var display = BobUi.Panel(layer, T.ink, null, "Display");
                Track(display.gameObject);
                BobUi.Pin((RectTransform)display.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                    new Vector2(0f, 210f), new Vector2(420f, 76f));
                display.raycastTarget = false;
                var text = BobUi.Label(display.transform, "ซัก 40 องศา", 40, T.primaryLight, TextAnchor.MiddleCenter, FontStyle.Bold);
                BobUi.Stretch(text.rectTransform);
            }
        }

        void TileFloor(Color baseColour)
        {
            Bg(Vector2.zero, new Vector2(AreaW, AreaH), baseColour);
            var line = new Color(baseColour.r * 0.86f, baseColour.g * 0.86f, baseColour.b * 0.86f, 1f);
            for (int i = -3; i <= 3; i++) Bg(new Vector2(i * 155f, 0f), new Vector2(6f, AreaH), line);
            for (int i = -2; i <= 2; i++) Bg(new Vector2(0f, i * 103f), new Vector2(AreaW, 6f), line);
        }

        /// <summary>A flat backdrop rect in the play area, behind the items (negative order pushes it further back).</summary>
        void Bg(Vector2 pos, Vector2 size, Color colour, int order = 0)
        {
            if (stage3d != null && stage3d.Ready) return; // the 3D stage paints the room instead
            var rt = BobUi.Rect("Bg", layer);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = colour;
            img.raycastTarget = false;
            BobUi.Pin(rt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), pos, size);
            if (order < 0) rt.SetAsFirstSibling();
            Track(rt.gameObject);
        }

        void SetupTrace()
        {
            int count = 20 + (difficulty - 1) * 6;
            bool rows = taskId == "T14";
            for (int i = 0; i < count; i++)
            {
                float u = count <= 1 ? 0.5f : i / (float)(count - 1);
                float x = Mathf.Lerp(-HalfW + 40f, HalfW - 40f, u);
                float y = rows
                    ? 150f - step * 190f
                    : Mathf.Sin(u * Mathf.PI * (1.4f + 0.4f * difficulty)) * (120f + 35f * (difficulty - 1));
                var dust = MakeDisc(new Vector2(x, y), rows ? 70f : 58f, new Color(0.82f, 0.74f, 0.53f));
                dust.hp = 0f;
            }
            brush.color = new Color(T.primary.r, T.primary.g, T.primary.b, 0.85f);
        }

        void SetupScrub()
        {
            var grime = new Color(0.55f, 0.42f, 0.24f);
            if (taskId == "T02")
            {
                int n = difficulty;
                var spots = new[] { new Vector2(-330f, -90f), new Vector2(300f, -140f), new Vector2(-40f, -40f) };
                for (int i = 0; i < n; i++)
                {
                    var stain = MakeDisc(spots[i], 220f - i * 20f, grime);
                    stain.hp = 1f;
                    AddGrimeDots(stain, 7);
                }
            }
            else if (taskId == "T07")
            {
                int plates = 2 + difficulty;
                for (int i = 0; i < plates; i++)
                {
                    float x = Mathf.Lerp(-420f, 420f, plates <= 1 ? 0.5f : i / (float)(plates - 1));
                    var plate = MakeDisc(new Vector2(x, 20f), 190f, new Color(0.96f, 0.93f, 0.84f));
                    plate.hp = 1f;
                }
            }
            else if (taskId == "T08")
            {
                for (int i = 0; i < 10; i++)
                {
                    float x = Mathf.Lerp(-500f, 500f, i / 9f);
                    float y = i % 2 == 0 ? 80f : -90f;
                    MakeDisc(new Vector2(x, y), 210f, new Color(0.94f, 0.97f, 1f, 0.92f)).hp = 1f;
                }
            }
            else // T09 step 2
            {
                var stain = MakeDisc(new Vector2(0f, -70f), 270f, grime);
                stain.hp = 1f;
                AddGrimeDots(stain, 9);
            }
            brush.color = new Color(T.sky.r, T.sky.g, T.sky.b, 0.85f);
        }

        /// <summary>Speckles around a stain so it reads as grime instead of one flat disc; they fade with the same hp.</summary>
        void AddGrimeDots(Item stain, int count)
        {
            for (int i = 0; i < count; i++)
            {
                var dir = new Vector2((float)rng.NextDouble() * 2f - 1f, (float)rng.NextDouble() * 2f - 1f);
                if (dir.sqrMagnitude > 0.0001f) dir.Normalize();
                float radius = stain.size * (0.3f + 0.24f * (float)rng.NextDouble());
                float dotSize = 22f + 30f * (float)rng.NextDouble();
                var tint = stain.img.color;
                var img = BobUi.Disc(layer, new Color(tint.r * 0.92f, tint.g * 0.88f, tint.b * 0.8f, 0.9f), dotSize, "Grime");
                img.raycastTarget = false;
                var rt = (RectTransform)img.transform;
                rt.anchoredPosition = stain.home + dir * radius;
                rt.SetSiblingIndex(stain.rect.GetSiblingIndex()); // behind the stain: the specks read as a ragged halo
                Track(img.gameObject);
                var dot = new Item { rect = rt, img = img, home = rt.anchoredPosition };
                if (stage3d != null && stage3d.Ready)
                {
                    dot.twin = stage3d.AddBlob(rt.anchoredPosition, dotSize * 0.9f, img.color);
                    img.enabled = false;
                }
                grime.Add(new GrimeDot { Dot = dot, Stain = stain });
            }
        }

        void SetupTap()
        {
            if (taskId == "T10")
            {
                int wanted = 6 + (difficulty - 1) * 2;
                float life = (1.6f - 0.3f * (difficulty - 1)) * (Buffed("gentle") ? 1.35f : 1f);
                int live = difficulty == 1 ? 1 : 2;
                for (int i = 0; i < wanted; i++)
                {
                    var dust = MakeDisc(RandomSpot(), 150f, new Color(0.78f, 0.70f, 0.52f));
                    dust.life = dust.maxLife = life;
                    dust.alive = i < live;
                    dust.rect.gameObject.SetActive(i < live);
                }
                return;
            }

            if (taskId == "T07")
            {
                MakeDisc(Vector2.zero, 340f, new Color(T.surfaceCream.r, T.surfaceCream.g, T.surfaceCream.b, 1f)).hp = 1f;
                return;
            }

            if (taskId == "T08")
            {
                // The whole bottle is the touch target: the stage shows the real prop, the rect stays as the hitbox.
                var body = MakeBox(new Vector2(-380f, -30f), new Vector2(180f, 300f), T.secondary, "สเปรย์", 34,
                    true, "SM_spray_bottle");
                body.bin = -1;
                return;
            }

            // T03: slimes crawl slowly; a hit removes one. Gentle buff: they crawl slower, so the card is calmer.
            int count = 5 + (difficulty - 1) * 2;
            float speed = (80f + 60f * (difficulty - 1)) * (Buffed("gentle") ? 0.62f : 1f);
            for (int i = 0; i < count; i++)
            {
                var slime = MakeDisc(RandomSpot(), 200f - 25f * (difficulty - 1), T.success);
                float a = (float)rng.NextDouble() * Mathf.PI * 2f;
                slime.vel = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * speed;
            }
        }

        void SetupSort()
        {
            // T16: the trash enemy is dealt the short four-card version of docs 16 section 4, not the six-card job.
            int count = emergency && taskId == "T05" ? 4 + (difficulty - 1) * 2 : 6 + (difficulty - 1) * 2;
            string[] names = { "ขวด", "กระดาษ", "เศษ" };
            Color[] colours = { T.secondary, T.primary, T.success };
            for (int i = 0; i < 3; i++)
            {
                var bin = MakeBox(new Vector2(-400f + i * 400f, -210f), new Vector2(300f, 160f), colours[i], names[i], 34, false);
                bin.bin = i;
                bins.Add(bin);
                Track(bin.rect.gameObject);
            }
            // Dealt onto a shuffled grid instead of pure random spots: 6-10 cards never pile up on one another.
            // The rows sit under the backdrop bins and above the tray row, so no card lands on a prop.
            int cols = 4, rows = Mathf.CeilToInt(count / 4f);
            var slots = new List<Vector2>();
            for (int r = 0; r < rows; r++)
            for (int c = 0; c < cols; c++)
                slots.Add(new Vector2(-360f + c * 240f, 130f - r * 120f));
            for (int i = slots.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (slots[i], slots[j]) = (slots[j], slots[i]);
            }
            for (int i = 0; i < count; i++)
            {
                int kindIndex = rng.Next(3);
                var bite = MakeBox(slots[i], new Vector2(180f, 110f),
                    new Color(T.surface.r, T.surface.g, T.surface.b, 1f), names[kindIndex], 30);
                bite.bin = kindIndex;
                bite.slotHome = slots[i];
            }
        }

        void SetupHold()
        {
            string label = taskId == "T13" ? "กดค้างยก" : taskId == "T06" ? "กดค้างแล้วหมุน" : "กดค้าง";
            var button = MakeDisc(new Vector2(0f, -20f), 320f, new Color(T.primary.r, T.primary.g, T.primary.b, 1f));
            holdButton = button;
            var text = BobUi.Label(button.rect, label, 40, T.ink, TextAnchor.MiddleCenter, FontStyle.Bold);
            BobUi.Stretch(text.rectTransform);
            holdCentre = new Vector2(0f, -20f);
            holdSeconds = taskId == "T09" ? 2.5f : 2f;
            if (Buffed("shine")) holdSeconds *= 0.8f; // shine: a finished surface needs less force, so the hold is shorter
            turnRequired = 4 + difficulty * 2;

            meterTrack = BobUi.Panel(layer, new Color(0.94f, 0.87f, 0.73f), null, "MeterTrack");
            Track(meterTrack.gameObject);
            var meterOutline = meterTrack.gameObject.AddComponent<Outline>();
            meterOutline.effectColor = new Color(T.ink.r, T.ink.g, T.ink.b, 0.3f);
            meterOutline.effectDistance = new Vector2(3f, -3f);
            BobUi.Pin((RectTransform)meterTrack.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(430f, -20f), new Vector2(64f, 340f));
            meterTrack.raycastTarget = false;
            meterFill = BobUi.Rect("MeterFill", meterTrack.transform).gameObject.AddComponent<Image>();
            var mf = (RectTransform)meterFill.transform;
            mf.anchorMin = new Vector2(0f, 0f); mf.anchorMax = new Vector2(1f, 0f);
            mf.pivot = new Vector2(0.5f, 0f);
            mf.offsetMin = Vector2.zero; mf.offsetMax = Vector2.zero;
            mf.anchoredPosition = new Vector2(0f, 6f);
            mf.sizeDelta = new Vector2(-12f, 6f);
            meterFill.color = T.success;
            meterFill.raycastTarget = false;
        }

        void SetupFill()
        {
            bandCentre = 0.66f;
            bandHalf = 0.12f - 0.03f * (difficulty - 1);
            if (Buffed("shine")) bandHalf += 0.05f; // shine: a wider target band, the finish forgives a wobble
            pourSpeed = 0.25f + 0.05f * (difficulty - 1);
            fillLevel = 0f;

            var bottle = BobUi.Panel(play, new Color(0.93f, 0.96f, 1f, 1f), "ui_panel", "Bottle");
            BobUi.Pin((RectTransform)bottle.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(-80f, -10f), new Vector2(230f, 440f));
            bottle.raycastTarget = false;
            bottle.gameObject.AddComponent<Mask>().showMaskGraphic = true;
            Track(bottle.gameObject);

            var cap = BobUi.Panel(play, T.secondary, "ui_panel", "Cap");
            BobUi.Pin((RectTransform)cap.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(-80f, 228f), new Vector2(126f, 48f));
            cap.raycastTarget = false;
            Track(cap.gameObject);

            bottleFill = BobUi.Rect("Liquid", bottle.transform).gameObject.AddComponent<Image>();
            var bf = (RectTransform)bottleFill.transform;
            bf.anchorMin = new Vector2(0f, 0f); bf.anchorMax = new Vector2(1f, 0f);
            bf.pivot = new Vector2(0.5f, 0f);
            bf.anchoredPosition = new Vector2(0f, 8f);
            bf.sizeDelta = new Vector2(-16f, 0f);
            bottleFill.color = T.sky;
            bottleFill.raycastTarget = false;

            band = BobUi.Rect("Band", bottle.transform).gameObject.AddComponent<Image>();
            var bandRect = (RectTransform)band.transform;
            bandRect.anchorMin = new Vector2(0f, 0f);
            bandRect.anchorMax = new Vector2(1f, 0f);
            bandRect.pivot = new Vector2(0.5f, 0f);
            bandRect.anchoredPosition = new Vector2(0f, (bandCentre - bandHalf) * 424f + 8f);
            bandRect.sizeDelta = new Vector2(-16f, bandHalf * 2f * 424f);
            band.color = new Color(T.success.r, T.success.g, T.success.b, 0.35f);
            band.raycastTarget = false;

            var labelPlate = BobUi.Panel(bottle.transform, new Color(1f, 1f, 1f, 0.92f), "ui_panel", "Label");
            BobUi.Pin((RectTransform)labelPlate.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, -70f), new Vector2(176f, 112f));
            labelPlate.raycastTarget = false;
            var labelText = BobUi.Label(labelPlate.transform, "น้ำยา", 36, T.secondary, TextAnchor.MiddleCenter, FontStyle.Bold);
            BobUi.Stretch(labelText.rectTransform);

            var hint = BobUi.Label(play, "กดค้างที่ขวดเพื่อเท", 42, T.ink, TextAnchor.MiddleLeft, FontStyle.Bold);
            BobUi.Pin(hint.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 0.5f), new Vector2(120f, 20f), new Vector2(500f, 80f));
            Track(hint.gameObject);
        }

        void SetupSequence()
        {
            string[] labels = { "▲ น้ำ", "● ผง", "· ปั่น", "▼ อุณหภูมิ" };
            Color[] colours = { T.sky, T.success, T.primary, T.accent };
            for (int i = 0; i < 4; i++)
            {
                var pad = MakeBox(new Vector2(-390f + i * 260f, -60f), new Vector2(210f, 190f), colours[i], labels[i], 30);
                pads.Add(pad);
            }
            int length = 2 + difficulty;
            for (int i = 0; i < length; i++) sequence.Add(rng.Next(4));
            // Shine buff: the flashed order stays up longer, so the same sequence is easier to follow.
            flashSeconds = Buffed("shine") ? 0.62f : 0.45f;
            flashTimer = flashSeconds;
        }

        // ---------------------------------------------------------------- T04 cart (docs 16 section 4)

        static readonly string[] CartBarLabels = { "ขจัดคราบ", "ถนอม", "เร็ว", "เงา" };

        // One colour per stat, used by the bars, the pips on every part, the delta pills and the buff ribbon, so the
        // player learns one colour and reads the whole card without reading.
        static readonly Color[] CartStatColour =
        {
            new Color(0.21f, 0.58f, 0.95f, 1f), // clean  = water blue
            new Color(0.09f, 0.54f, 0.31f, 1f), // gentle = leaf green
            new Color(0.99f, 0.47f, 0.07f, 1f), // speed  = orange
            new Color(0.96f, 0.77f, 0.19f, 1f), // shine  = gold
        };

        // R1 geometry uses top-left coordinates inside MiniPlay (screen y=232 at 1080 reference height).
        // Keep these local to T04: the other games still use AreaW/AreaH and the original theme/stage.
        void SetPlayArea(bool cart)
        {
            if (play == null) return;
            float width = cart ? Mathf.Max(1f, ((RectTransform)play.parent).rect.width - 96f) : AreaW;
            BobUi.Pin(play, Vector2.one * 0.5f, Vector2.one * 0.5f,
                new Vector2(0f, cart ? -82f : 30f), new Vector2(width, cart ? CartHeight : AreaH));
            play.GetComponent<Image>().color = cart ? BobUiR1.Cream
                : new Color(T.surfaceCream.r, T.surfaceCream.g, T.surfaceCream.b, 0.9f);
        }

        void RefreshCartLayout()
        {
            if (taskId != "T04" || (!playing && !complete) || play == null || !play.gameObject.activeInHierarchy || cartLayout.Count == 0) return;
            float width = Mathf.Max(1f, ((RectTransform)play.parent).rect.width - 96f);
            if (Mathf.Abs(cartLayoutWidth - width) <= 0.1f) return;
            SetPlayArea(true);
            LayoutCart();
        }

        // At 1440 safe-page width, MiniPlay is 1216 wide: 189-wide shelf cards and a 2x2 tray.
        // The six-across shelf needs at least 944 inner pixels (1168 safe-page pixels) for 144-wide hits.
        static bool CartGrid(float w) => w < 1450f;
        static void CartPlace(RectTransform rt, Rect bounds)
        {
            BobUi.Pin(rt, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(bounds.x, -bounds.y), bounds.size);
        }

        void CartResponsive(RectTransform rt, Func<float, Rect> bounds)
        {
            cartLayout.Add(w => { if (rt != null) CartPlace(rt, bounds(w)); });
            CartPlace(rt, bounds(play.rect.width));
        }

        Image CartPanel(string name, Color colour, Func<float, Rect> bounds)
        {
            var panel = BobUiR1.Panel(layer, colour, name: name);
            panel.raycastTarget = false;
            Track(panel.gameObject);
            CartResponsive(panel.rectTransform, bounds);
            return panel;
        }

        static void CartOutline(Image img, Color colour)
        {
            var outline = img.GetComponent<Outline>() ?? img.gameObject.AddComponent<Outline>();
            outline.effectColor = colour;
            outline.effectDistance = new Vector2(1f, -1f);
        }

        static TMP_Text CartLabel(Transform parent, string text, int size, Color colour,
            TextAnchor align = TextAnchor.MiddleLeft, bool bold = false)
        {
            var label = BobUiR1.Label(parent, text, size, colour, align, bold ? FontStyle.Bold : FontStyle.Normal);
            label.raycastTarget = false;
            // IBM Plex Thai needs room for stacked vowel marks without shrinking the glyphs.
            label.enableAutoSizing = false;
            label.overflowMode = TextOverflowModes.Ellipsis;
            return label;
        }

        // A horizontal strip within a parent that follows its width without changing font or hit size.
        static void CartStrip(RectTransform rt, float left, float top, float right, float height)
        {
            rt.anchorMin = new Vector2(0f, 1f); rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0f, 1f);
            rt.offsetMin = new Vector2(left, -top - height); rt.offsetMax = new Vector2(-right, -top);
        }

        TMP_Text CartText(string name, string text, int size, Color colour, Func<float, Rect> bounds, bool bold = false)
        {
            var label = CartLabel(layer, text, size, colour, bold: bold);
            label.name = name;
            Track(label.gameObject);
            CartResponsive(label.rectTransform, bounds);
            return label;
        }

        static Image CartFill(Transform parent, string name, Color colour)
        {
            var fill = BobUiR1.Panel(parent, colour, name: name);
            fill.raycastTarget = false;
            CartFillAmount(fill, 0f);
            return fill;
        }

        static void CartFillAmount(Image fill, float amount)
        {
            if (fill == null) return;
            var rt = fill.rectTransform;
            rt.anchorMin = Vector2.zero; rt.anchorMax = new Vector2(Mathf.Clamp01(amount), 1f);
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }

        void SetupCart()
        {
            SetPlayArea(true);
            if (brush != null) brush.gameObject.SetActive(false);
            if (waitLabel != null) waitLabel.gameObject.SetActive(false);
            var tutorial = play.GetComponentInChildren<BobTutorialClip>(true);
            if (tutorial != null) tutorial.gameObject.SetActive(false); // do not cover the first shelf card/stat
            cartError = "";
            if (step == 0) SetupCartShelf(); else SetupCartBuild();
            CartApply();
        }

        void SetupCartShelf()
        {
            // The six seeded unique parts and automatic fourth-pick transition are unchanged.
            cartShelf.Clear();
            cartShelf.AddRange(BobTaskRaceRules.CartShelf(cartSeed, difficulty));
            for (int i = 0; i < cartShelf.Count; i++)
            {
                var box = CartPart(cartShelf[i], "Shelf-" + cartShelf[i]);
                box.bin = i;
                cartShelfCards.Add(box);
            }
            var selection = CartPanel("PickedParts", BobUiR1.Paper, w => new Rect(0f, 220f, w * 0.59f, 202f));
            var title = CartLabel(selection.transform, "อุปกรณ์ที่หยิบ", 30, BobUiR1.Ink, bold: true);
            CartStrip(title.rectTransform, 24f, 4f, 90f, 66f);
            cartPickCount = CartLabel(selection.transform, "0/4", 30, BobUiR1.Ink, TextAnchor.MiddleRight, true);
            CartStrip(cartPickCount.rectTransform, 24f, 4f, 24f, 66f);
            for (int i = 0; i < BobTaskRaceRules.CartParts; i++)
            {
                var slot = BobUiR1.Panel(selection.transform, BobUiR1.Cream, name: "PickedSlot-" + (i + 1));
                slot.raycastTarget = false;
                CartOutline(slot, BobUiR1.Line);
                cartPickSlots.Add(slot);
                var label = CartLabel(slot.transform, (i + 1).ToString(), 25, BobUiR1.Ink, TextAnchor.MiddleCenter, true);
                BobUi.Stretch(label.rectTransform, 8f, 2f, 8f, 2f);
                cartPickNames.Add(label);
                int index = i;
                CartResponsive(slot.rectTransform, w => new Rect(24f + index * ((w * 0.59f - 72f) / 4f + 8f), 80f,
                    (w * 0.59f - 72f) / 4f, 112f));
            }
            var budget = CartPanel("PickBudgetInfo", BobUiR1.Paper, w => new Rect(w * 0.59f + 24f, 220f, w * 0.41f - 24f, 202f));
            var budgetTitle = CartLabel(budget.transform, $"งบจัดกอง {BobRules.RoundBudget} เหรียญ", 30, BobUiR1.Ink, bold: true);
            CartStrip(budgetTitle.rectTransform, 24f, 2f, 24f, 66f);
            cartCostText = CartLabel(budget.transform, "", 31, BobUiR1.Ink, bold: true);
            CartStrip(cartCostText.rectTransform, 24f, 68f, 24f, 68f);
            var info = CartLabel(budget.transform, "คิดงบเมื่อวางลงรถเข็น", 26, BobUiR1.Muted);
            CartStrip(info.rectTransform, 24f, 136f, 24f, 58f);
            BuildCartBars();
            cartRecipe = CartText("PickProgress", "", 30, BobUiR1.Ink, w => new Rect(0f, 648f, w, 72f), true);
            var progress = CartPanel("PickTrack", BobUiR1.Line, w => new Rect(0f, 738f, w, 12f));
            cartPickFill = CartFill(progress.transform, "PickFill", BobUiR1.Green);
        }

        void SetupCartBuild()
        {
            BuildCartBars();
            // Grid: heading to 246, two 144-high rows to 554, hint to 626; budget starts at 632.
            var tray = CartPanel("CartTray", BobUiR1.Paper,
                w => new Rect(0f, CartGrid(w) ? 180f : 192f, w * 0.57f, CartGrid(w) ? 450f : 366f));
            var title = CartLabel(tray.transform, "อุปกรณ์ในถาด", 30, BobUiR1.Ink, bold: true);
            CartStrip(title.rectTransform, 24f, 0f, 244f, 66f);
            var dragHint = CartLabel(tray.transform, "ลากไปที่รถเข็น", 25, BobUiR1.Muted, TextAnchor.MiddleRight);
            BobUi.Pin(dragHint.rectTransform, Vector2.one, Vector2.one, new Vector2(-24f, -6f), new Vector2(208f, 56f));
            cartTrayEmpty = CartLabel(tray.transform, "", 32, BobUiR1.Green, TextAnchor.MiddleCenter, true);
            CartStrip(cartTrayEmpty.rectTransform, 24f, 92f, 24f, 144f);
            cartInlineHint = CartPanel("InlineCartHint", BobUiR1.Cream,
                w => new Rect(24f, CartGrid(w) ? 562f : 482f, w * 0.57f - 48f, 64f));
            cartTrayHint = CartLabel(cartInlineHint.transform, "", 25, BobUiR1.Muted);
            BobUi.Stretch(cartTrayHint.rectTransform, 16f, 4f, 16f, 4f);

            cartDropZone = CartPanel("CartDropZone", BobUiR1.Paper, w => new Rect(w * 0.57f + 24f, 192f, w * 0.43f - 24f, 428f));
            CartOutline(cartDropZone, BobUiR1.Line);
            var cartTitle = CartLabel(cartDropZone.transform, "รถเข็น", 30, BobUiR1.Ink, bold: true);
            CartStrip(cartTitle.rectTransform, 24f, 0f, 170f, 66f);
            cartSlotCount = CartLabel(cartDropZone.transform, "", 28, BobUiR1.Ink, TextAnchor.MiddleRight, true);
            CartStrip(cartSlotCount.rectTransform, 150f, 0f, 24f, 66f);
            for (int i = 0; i < BobTaskRaceRules.CartParts; i++)
            {
                var item = CartItem("Stack-layer-" + i, BobUiR1.Cream);
                item.bin = i;
                var slot = new CartSlotView
                {
                    item = item,
                    name = CartLabel(item.rect, "", 30, BobUiR1.Ink, bold: true),
                    cost = CartLabel(item.rect, "", 25, BobUiR1.Muted, TextAnchor.MiddleRight),
                    hint = CartLabel(item.rect, "", 26, BobUiR1.Green),
                };
                cartSlots.Add(slot);
            }
            for (int i = 0; i < cartTray.Count; i++) CartAddChip(i);
            cartCostText = CartText("StackBudget", "", 30, BobUiR1.Ink,
                w => new Rect(0f, CartGrid(w) ? 632f : 574f, w * 0.57f, 66f), true);
            cartRecipe = CartText("PassConditions", "", 28, BobUiR1.Muted,
                w => new Rect(0f, CartGrid(w) ? 700f : 644f, w * 0.57f, 62f));
            cartRecipeIsBuild = true;
            var budget = CartPanel("BudgetTrack", BobUiR1.Line,
                w => new Rect(0f, CartGrid(w) ? 768f : 712f, w * 0.57f, 12f));
            cartBudgetFill = CartFill(budget.transform, "BudgetFill", BobUiR1.Green);

            // Never disabled for an empty/incomplete/unpassed formula: lock completion and buff are distinct.
            CartPanel("CartLockShadow", new Color(BobUiR1.Ink.r, BobUiR1.Ink.g, BobUiR1.Ink.b, 0.17f),
                w => new Rect(w * 0.57f + 24f, 633f, w * 0.43f - 24f, CartHitMin));
            cartLock = CartPanel("CartLock", BobUiR1.Sun,
                w => new Rect(w * 0.57f + 24f, 624f, w * 0.43f - 24f, CartHitMin));
            cartLockLabel = CartLabel(cartLock.transform, "กดค้าง ล็อกสูตร", 34, BobUiR1.Ink, TextAnchor.MiddleCenter, true);
            CartStrip(cartLockLabel.rectTransform, 20f, 4f, 20f, 76f);
            var holdHint = CartLabel(cartLock.transform, "กดค้าง 0.6 วินาที", 22, BobUiR1.Muted, TextAnchor.MiddleCenter);
            CartStrip(holdHint.rectTransform, 16f, 80f, 16f, 48f);
            var holdTrack = BobUi.Rect("LockTrack", cartLock.transform);
            CartStrip(holdTrack, 16f, 132f, 16f, 8f);
            cartLockFill = CartFill(holdTrack, "LockFill", BobUiR1.Green);
        }

        // T04 bypasses MakeBox: that helper intentionally hides uGUI images in favour of 3D twins.
        Item CartItem(string name, Color colour)
        {
            var image = BobUiR1.Panel(layer, colour, name: name);
            image.raycastTarget = false;
            CartOutline(image, BobUiR1.Line);
            var item = new Item { rect = image.rectTransform, img = image, bin = -1, size = CartHitMin };
            items.Add(item);
            return item;
        }

        Item CartPart(string id, string name)
        {
            var part = BobRules.Parts[id];
            var item = CartItem(name, BobUiR1.Paper);
            var view = new CartPartView
            {
                icon = CartIcon(item.rect, id, 54f, BobUiR1.Ink),
                check = CartIcon(item.rect, "check", 28f, BobUiR1.Green),
                name = CartLabel(item.rect, part.Name, part.Name.Length > 10 ? 25 : 30, BobUiR1.Ink, TextAnchor.MiddleCenter, true),
                cost = CartLabel(item.rect, $"{part.Cost} เหรียญ", 26, BobUiR1.Muted, TextAnchor.MiddleCenter),
            };
            view.check.gameObject.SetActive(false);
            cartPartViews.Add(item, view);
            return item;
        }

        void CartAddChip(int trayIndex)
        {
            var chip = CartPart(cartTray[trayIndex], "Tray-" + cartTray[trayIndex]);
            chip.bin = trayIndex;
            cartChips.Add(chip);
        }

        Rect CartChipBounds(int i)
        {
            float w = play.rect.width, tw = w * 0.57f;
            bool grid = CartGrid(w);
            int columns = grid ? 2 : 4;
            float cw = (tw - 48f - (columns - 1) * CartGap) / columns;
            return new Rect(24f + (i % columns) * (cw + CartGap), (grid ? 250f : 270f) + (i / columns) * (CartHitMin + CartGap),
                cw, grid ? CartHitMin : 192f);
        }

        Vector2 CartCentre(Rect bounds) => new Vector2(-play.rect.width * 0.5f + bounds.center.x, CartHeight * 0.5f - bounds.center.y);
        Vector2 CartChipHome(int i) => CartCentre(CartChipBounds(i));

        void CartPlaceItem(Item item, Rect bounds)
        {
            item.home = CartCentre(bounds);
            BobUi.Pin(item.rect, Vector2.one * 0.5f, Vector2.one * 0.5f, item.home, bounds.size);
        }

        void LayoutCartPart(Item item, Rect bounds, bool compact)
        {
            // Resizing during a drag keeps the grabbed object and pointer offset, not a rebuilt/re-dealt hand.
            if (item != cartChipHeld) CartPlaceItem(item, bounds);
            else item.rect.sizeDelta = bounds.size;
            var view = cartPartViews[item];
            float iconSize = compact ? 36f : 54f;
            var iconAnchor = new Vector2(compact ? 0f : 0.5f, 1f);
            BobUi.Pin(view.icon, iconAnchor, iconAnchor, new Vector2(compact ? 12f : 0f, compact ? -18f : -8f), new Vector2(54f, 54f));
            view.icon.localScale = Vector3.one * (iconSize / 54f);
            // Compact cards use two text rows, with the icon beside the name, not above it.
            CartStrip(view.name.rectTransform, compact ? 58f : 10f, compact ? 4f : 64f, 10f, 66f);
            CartStrip(view.cost.rectTransform, 10f, compact ? 78f : 130f, 10f, 58f);
            BobUi.Pin(view.check, Vector2.one, Vector2.one, new Vector2(-14f, -14f), new Vector2(28f, 28f));
        }

        void LayoutCart()
        {
            float w = play.rect.width;
            cartLayoutWidth = w;
            foreach (var layout in cartLayout) layout(w);
            if (step == 0)
            {
                float cw = (w - CartGap * 5f) / 6f;
                for (int i = 0; i < cartShelfCards.Count; i++)
                    LayoutCartPart(cartShelfCards[i], new Rect(i * (cw + CartGap), 0f, cw, 192f), false);
                return;
            }
            for (int i = 0; i < cartChips.Count; i++)
            {
                cartChips[i].bin = i;
                LayoutCartPart(cartChips[i], CartChipBounds(i), CartGrid(w));
            }
            // Four rows fit below the 66-high heading: 144 + 3*66 + 3*4 = 354, from y=262 to 616.
            float bottom = 616f;
            for (int i = 0; i < cartSlots.Count; i++)
            {
                var slot = cartSlots[i];
                bool filled = i < cartStack.Count, top = filled && i == cartStack.Count - 1;
                float h = top ? CartHitMin : 66f;
                CartPlaceItem(slot.item, new Rect(w * 0.57f + 48f, bottom - h, w * 0.43f - 72f, h));
                bottom -= h + 4f;
                slot.item.alive = filled;
                slot.item.img.color = filled ? BobUiR1.Mint : BobUiR1.Cream;
                CartOutline(slot.item.img, top ? BobUiR1.Green : BobUiR1.Line);
                slot.name.text = $"{i + 1}  " + (filled ? BobRules.Parts[cartStack[i]].Name : "ว่าง");
                slot.cost.text = filled ? BobRules.Parts[cartStack[i]].Cost.ToString() : "";
                CartStrip(slot.name.rectTransform, 18f, top ? 6f : 0f, 110f, 66f);
                CartStrip(slot.cost.rectTransform, 18f, top ? 6f : 0f, 18f, 66f);
                CartStrip(slot.hint.rectTransform, 18f, 80f, 18f, 58f);
                slot.hint.text = cartLocked ? "ชั้นบนสุด · ล็อกแล้ว" : "แตะยกชั้นบนสุดออก";
                slot.hint.gameObject.SetActive(top);
            }
        }

        // Small native uGUI strokes, not screenshots or font-dependent icon glyphs. Coordinates are a 24-unit grid.
        static RectTransform CartIcon(Transform parent, string id, float size, Color colour)
        {
            var icon = BobUi.Rect("Icon-" + id, parent);
            icon.sizeDelta = new Vector2(size, size);
            void Stroke(params float[] points)
            {
                for (int i = 2; i < points.Length; i += 2)
                {
                    var a = new Vector2(points[i - 2], -points[i - 1]) * size / 24f;
                    var b = new Vector2(points[i], -points[i + 1]) * size / 24f;
                    var line = BobUi.Rect("Stroke", icon).gameObject.AddComponent<Image>();
                    line.color = colour; line.raycastTarget = false;
                    BobUi.Pin(line.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 0.5f), a, new Vector2((b - a).magnitude, size * 0.075f));
                    line.rectTransform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(b.y - a.y, b.x - a.x) * Mathf.Rad2Deg);
                }
            }
            switch (id)
            {
                case "check": Stroke(5, 12, 10, 17, 21, 5); break;
                case "sponge":
                    Stroke(4, 5, 20, 5, 20, 19, 4, 19, 4, 5); Stroke(8, 9, 10, 9); Stroke(15, 10, 17, 10); Stroke(11, 15, 13, 15); break;
                case "brush": case "broom":
                    Stroke(15, 2, 12, 12); Stroke(7, 12, 17, 12, 20, 21, 4, 21, 7, 12); Stroke(9, 16, 9, 21); Stroke(14, 16, 14, 21); break;
                case "steam": case "hot":
                    Stroke(6, 20, 18, 20);
                    for (int x = 7; x <= 17; x += 5) Stroke(x, 15, x - 2, 12, x + 1, 8, x + 1, 6, x - 1, 3);
                    break;
                case "cloth": Stroke(5, 3, 20, 6, 16, 21, 2, 17, 5, 3); Stroke(8, 7, 13, 17); break;
                case "polish": case "vinegar": case "spray":
                    Stroke(9, 2, 15, 2, 15, 7, 18, 11, 18, 21, 6, 21, 6, 11, 9, 7, 9, 2); Stroke(9, 14, 15, 14); break;
                case "soak":
                    Stroke(3, 11, 21, 11, 19, 21, 5, 21, 3, 11);
                    for (int x = 7; x <= 17; x += 5) Stroke(x, 3, x, 8);
                    break;
                case "soap": case "soda":
                    Stroke(4, 8, 8, 5, 20, 5, 20, 16, 16, 20, 4, 20, 4, 8, 16, 8, 20, 5); Stroke(16, 8, 16, 20); break;
                case "air": Stroke(3, 7, 17, 7, 20, 5, 19, 2, 16, 2); Stroke(3, 12, 22, 12); Stroke(3, 17, 16, 17, 19, 20, 17, 22); break;
                case "circle": Stroke(19, 7, 15, 3, 8, 3, 3, 8, 3, 15, 8, 21, 15, 21, 21, 16); Stroke(14, 7, 20, 8, 20, 2); break;
                case "gentle": Stroke(12, 21, 3, 12, 2, 8, 4, 4, 8, 3, 12, 7, 16, 3, 20, 4, 22, 8, 21, 12, 12, 21); break;
                case "speed": case "scrub": Stroke(14, 1, 3, 14, 11, 14, 10, 23, 21, 9, 13, 9, 14, 1); break;
                case "shine": Stroke(12, 1, 15, 9, 23, 12, 15, 15, 12, 23, 9, 15, 1, 12, 9, 9, 12, 1); break;
                default: Stroke(12, 2, 15, 8, 22, 9, 17, 14, 18, 21, 12, 18, 6, 21, 7, 14, 2, 9, 9, 8, 12, 2); break;
            }
            return icon;
        }

        /// <summary>The painted star of the art set, gold, or a plain disc when the texture is missing in the editor.</summary>
        Image StarSprite(Transform parent, float size, Color tint)
        {
            Image img;
            if (fxHit != null)
            {
                var rt = BobUi.Rect("Star", parent);
                img = rt.gameObject.AddComponent<Image>();
                img.sprite = fxHit;
                img.preserveAspect = true;
                img.raycastTarget = false;
                rt.sizeDelta = new Vector2(size, size);
            }
            else
            {
                img = BobUi.Disc(parent, tint, size, "Star");
                img.raycastTarget = false;
            }
            img.color = tint;
            return img;
        }

        /// <summary>Four horizontal, 164-high R1 stat cards: real ceiling scale, target tick and explicit met word.</summary>
        void BuildCartBars()
        {
            for (int i = 0; i < BobTaskRaceRules.CartKeys.Length; i++)
            {
                int index = i;
                bool star = i == cartStar;
                var card = CartPanel("Stat-" + BobTaskRaceRules.CartKeys[i], star ? BobUi.Hex("#FFF0BB") : BobUiR1.Paper,
                    w => new Rect(index * ((w - 3f * CartGap) / 4f + CartGap), step == 0 ? 456f : 0f,
                        (w - 3f * CartGap) / 4f, 164f));
                var icon = CartIcon(card.transform, star ? "star" : BobTaskRaceRules.CartKeys[i], 34f, BobUiR1.Ink);
                CartPlace(icon, new Rect(22f, 15f, 34f, 34f));
                var title = CartLabel(card.transform, CartBarLabels[i], 28, BobUiR1.Ink, bold: true);
                CartStrip(title.rectTransform, 68f, 0f, 20f, 64f);
                var track = BobUiR1.Panel(card.transform, BobUiR1.Line, name: "BarTrack");
                track.raycastTarget = false;
                CartStrip(track.rectTransform, 24f, 76f, 24f, 12f);
                cartBars.Add(CartFill(track.transform, "BarFill", BobUiR1.Green));
                var target = BobUiR1.Panel(track.transform, BobUiR1.Ink, name: "BarNeed");
                target.raycastTarget = false;
                float u = Mathf.Clamp01(Need(i) / (float)Mathf.Max(1, BobTaskRaceRules.CartCeiling[i]));
                BobUi.Pin(target.rectTransform, new Vector2(u, 0.5f), Vector2.one * 0.5f, Vector2.zero, new Vector2(4f, 28f));
                cartTargets.Add(target);
                var value = CartLabel(card.transform, "", 30, BobUiR1.Ink, bold: true);
                CartStrip(value.rectTransform, 24f, 96f, 130f, 66f);
                cartValues.Add(value);
                var met = CartLabel(card.transform, "เป้าหมาย", 23, BobUiR1.Muted, TextAnchor.MiddleRight);
                CartStrip(met.rectTransform, 110f, 96f, 24f, 64f);
                cartMetWords.Add(met);
            }
        }

        Vector2 CartBarPoint(int stat, float fraction)
        {
            if (stat < 0 || stat >= cartBars.Count) return Vector2.zero;
            var track = (RectTransform)cartBars[stat].transform.parent;
            var r = track.rect;
            return play.InverseTransformPoint(track.TransformPoint(new Vector3(Mathf.Lerp(r.xMin, r.xMax, Mathf.Clamp01(fraction)), r.center.y, 0f)));
        }

        int Need(int i) => cartNeed != null && cartNeed.TryGetValue(BobTaskRaceRules.CartKeys[i], out var v) ? v : 5;

        /// <summary>Re-score the stack and repaint the bars (called after every add and remove).</summary>
        void CartApply()
        {
            LayoutCart();
            var ev = BobRules.EvalStack(cartStack);
            for (int i = 0; i < 4; i++) cartStats[i] = ev.Stats[i];
            for (int i = 0; i < cartBars.Count && i < 4; i++)
            {
                float max = Mathf.Max(1f, BobTaskRaceRules.CartCeiling[i]);
                CartFillAmount(cartBars[i], cartStats[i] / max);
                bool ok = cartStats[i] >= Need(i);
                if (ok && !cartBarMet[i] && cartStack.Count > 0)
                    Fx(fxSparkle, CartBarPoint(i, Need(i) / max), 110f, 0.45f, 45f);
                cartBarMet[i] = ok;
                cartBars[i].color = ok ? BobUiR1.Green : BobUi.Hex("#34738B");
                cartTargets[i].color = ok ? BobUiR1.Green : BobUiR1.Ink;
                cartValues[i].text = $"{cartStats[i]}/{Need(i)}";
                cartValues[i].color = ok ? BobUiR1.Green : BobUiR1.Ink;
                cartMetWords[i].text = ok ? "ผ่าน" : "เป้าหมาย";
                cartMetWords[i].color = ok ? BobUiR1.Green : BobUiR1.Muted;
            }
            if (step == 0)
            {
                int picked = cartTray.Count;
                if (cartCostText != null) cartCostText.text = $"ของที่หยิบ {BobRules.StackCost(cartTray)} เหรียญ";
                if (cartPickCount != null) cartPickCount.text = $"{picked}/{BobTaskRaceRules.CartParts}";
                for (int i = 0; i < cartPickSlots.Count; i++)
                {
                    cartPickSlots[i].color = i < picked ? BobUiR1.Mint : BobUiR1.Cream;
                    cartPickNames[i].text = i < picked ? BobRules.Parts[cartTray[i]].Name : (i + 1).ToString();
                }
                foreach (var box in cartShelfCards)
                {
                    bool selected = box.bin < 0;
                    box.img.color = selected ? BobUiR1.Mint : BobUiR1.Paper;
                    CartOutline(box.img, selected ? BobUiR1.Green : BobUiR1.Line);
                    cartPartViews[box].check.gameObject.SetActive(selected);
                }
                if (cartRecipe != null) cartRecipe.text = picked == BobTaskRaceRules.CartParts
                    ? "หยิบครบแล้ว · กำลังไปขั้นจัดกอง"
                    : $"หยิบแล้ว {picked}/4 · ครบสี่ชิ้นไปขั้นต่อไปอัตโนมัติ";
                CartFillAmount(cartPickFill, picked / (float)BobTaskRaceRules.CartParts);
                return;
            }
            int spent = BobRules.StackCost(cartStack);
            if (cartCostText != null)
            {
                cartCostText.text = $"ใช้ {spent}/{BobRules.RoundBudget} เหรียญ · เหลือ {BobRules.RoundBudget - spent}";
                cartCostText.color = spent > BobRules.RoundBudget ? BobUiR1.Red : BobUiR1.Ink;
            }
            CartFillAmount(cartBudgetFill, spent / (float)BobRules.RoundBudget);
            if (cartBudgetFill != null) cartBudgetFill.color = spent > BobRules.RoundBudget ? BobUiR1.Red : BobUiR1.Green;
            if (cartRecipe != null && cartRecipeIsBuild)
            {
                int passes = BobTaskRaceRules.CartPassCount(cartStats, cartNeed);
                cartRecipe.text = $"ผ่าน {passes}/4 เงื่อนไข · D{difficulty} ต้องอย่างน้อย {Mathf.Min(3, difficulty)}";
            }
            if (cartSlotCount != null) cartSlotCount.text = $"{cartStack.Count}/4 ช่อง";
            if (cartTrayEmpty != null)
            {
                cartTrayEmpty.gameObject.SetActive(cartTray.Count == 0);
                cartTrayEmpty.text = cartStack.Count == BobTaskRaceRules.CartParts
                    ? "วางครบแล้ว\nกดค้างเพื่อล็อกสูตร" : "ถาดว่างแล้ว\nกดค้างเพื่อล็อกกองที่เหลือ";
            }
            if (cartTrayHint != null)
            {
                cartTrayHint.text = string.IsNullOrEmpty(cartError)
                    ? $"วิธีใช้ {BobRules.CountMethods(cartStack)}/{BobRules.MaxMethods} · วางบนของ ไม่ต่อวิธีใช้"
                    : cartError;
                cartTrayHint.color = string.IsNullOrEmpty(cartError) ? BobUiR1.Muted : BobUiR1.Red;
                cartInlineHint.color = string.IsNullOrEmpty(cartError) ? BobUiR1.Cream : BobUiR1.Pink;
            }
        }

        bool CartContains(RectTransform rt, Vector2 p)
        {
            return rt != null && rt.gameObject.activeInHierarchy &&
                rt.rect.Contains((Vector2)rt.InverseTransformPoint(play.TransformPoint(p)));
        }

        void CartDown(Vector2 p)
        {
            if (cartLocked) return;
            if (step == 0)
            {
                if (cartTray.Count >= BobTaskRaceRules.CartParts) return;
                foreach (var box in cartShelfCards)
                {
                    if (box.bin < 0 || box.bin >= cartShelf.Count || !CartContains(box.rect, p)) continue;
                    cartTray.Add(cartShelf[box.bin]);
                    box.bin = -1;
                    Ring(box.home, BobUiR1.Mint, 90f);
                    BobSfx.Play("pass_part", 0.8f);
                    stepProgress = cartTray.Count / (float)BobTaskRaceRules.CartParts;
                    Hint = $"หยิบแล้ว {cartTray.Count}/{BobTaskRaceRules.CartParts}";
                    CartApply(); // selected cards remain readable and mint until Tick advances the fourth pick
                    return;
                }
                return;
            }
            if (cartChipHeld != null || holding) return;
            // Only the visible, 144-high top is tappable. Test it before the disjoint lock rectangle.
            if (cartLayers.Count > 0 && CartContains(cartLayers[cartLayers.Count - 1].rect, p))
            {
                int last = cartStack.Count - 1;
                var top = cartLayers[last];
                Ring(top.home, BobUiR1.Pink, 90f);
                cartStack.RemoveAt(last); // deliberately NOT returned to the tray
                cartLayers.RemoveAt(last);
                if (cartMarks.Count > last)
                {
                    if (cartMarks[last] != null) DestroyNow(cartMarks[last].gameObject);
                    cartMarks.RemoveAt(last);
                }
                cartError = "";
                Hint = "ยกชั้นบนสุดออกแล้ว";
                BobSfx.Play("lift_layer", 0.8f);
                CartApply();
                return;
            }
            if (cartLock != null && CartContains(cartLock.rectTransform, p))
            {
                holding = true;
                return;
            }
            for (int i = cartChips.Count - 1; i >= 0; i--)
            {
                var chip = cartChips[i];
                if (!CartContains(chip.rect, p)) continue;
                cartChipHeld = chip;
                chip.shake = 0f;
                heldOffset = chip.rect.anchoredPosition - p;
                chip.rect.SetAsLastSibling();
                chip.img.color = BobUiR1.Sky;
                CartOutline(chip.img, BobUiR1.Green);
                BobSfx.Play("pickup", 0.7f);
                return;
            }
        }

        void CartDrag(Vector2 p)
        {
            if (cartChipHeld == null) return;
            cartChipHeld.home = p + heldOffset;
            cartChipHeld.rect.anchoredPosition = cartChipHeld.home;
        }

        /// <summary>Why this part cannot go on top right now, or null. The card plays by the main stack rules.</summary>
        string CartWhyCannot(string id)
        {
            if (cartStack.Count >= BobTaskRaceRules.CartParts) return "รถเข็นมี 4 ช่อง";
            if (BobRules.IsMethod(id))
            {
                if (BobRules.CountMethods(cartStack) >= BobRules.MaxMethods) return "ใช้วิธีได้ 2 อย่าง";
                if (cartStack.Count == 0 || BobRules.IsMethod(cartStack[cartStack.Count - 1])) return "วิธีต้องวางบนของ";
            }
            if (BobRules.StackCost(cartStack) + BobRules.Parts[id].Cost > BobRules.RoundBudget) return "งบไม่พอ";
            return null;
        }

        void CartUp(Vector2 p)
        {
            holding = false;
            if (step == 0 || cartChipHeld == null) return;
            var chip = cartChipHeld;
            cartChipHeld = null;
            chip.img.color = BobUiR1.Paper;
            CartOutline(chip.img, BobUiR1.Line);
            bool inCart = cartDropZone != null && CartContains(cartDropZone.rectTransform, p);
            int trayIndex = cartChips.IndexOf(chip);
            string id = trayIndex >= 0 ? cartTray[trayIndex] : null;
            string why = inCart && id != null ? CartWhyCannot(id) : null;
            if (!inCart || trayIndex < 0 || why != null)
            {
                cartError = why != null ? "วางไม่ได้ · " + why : "ลากอุปกรณ์ไปวางในรถเข็น";
                Hint = cartError;
                CartApply();
                chip.home = CartChipHome(trayIndex < 0 ? 0 : trayIndex);
                chip.rect.anchoredPosition = chip.home;
                chip.shake = ShakeTime;
                if (!inCart || why != null) BobMinigameSfx.Wrong("drop_invalid");
                return;
            }
            // Into the cart: only the stack spends budget; no wallet access and no replacement tray part.
            cartTray.RemoveAt(trayIndex);
            cartStack.Add(id);
            cartChips.Remove(chip);
            cartPartViews.Remove(chip);
            items.Remove(chip);
            chip.rect.gameObject.SetActive(false);
            DestroyNow(chip.rect.gameObject);
            int index = cartStack.Count - 1;
            var top = cartSlots[index].item;
            cartLayers.Add(top);
            cartError = "";
            CartApply(); // reflow bottom-up slots before anchoring the reaction feedback
            BobSfx.Play("place_layer", 0.85f);

            var ev = BobRules.EvalStack(cartStack);
            bool reacted = false;
            int sum = 0;
            foreach (var f in ev.Fx)
            {
                if (f.At != index || f.Src == "enemy") continue;
                reacted = true;
                sum += f.Sum;
                CartEffect(f);
            }
            Image mark = null;
            if (reacted)
            {
                bool good = sum >= 0;
                mark = StarSprite(top.rect, 22f, good ? BobUiR1.Sun : BobUiR1.Red);
                BobUi.Pin(mark.rectTransform, new Vector2(1f, 0.5f), Vector2.one * 0.5f,
                    new Vector2(-82f, 0f), new Vector2(22f, 22f));
                Fx(good ? fxSparkle : fxDust, top.home, 150f, 0.4f, good ? 35f : -30f);
                BobSfx.Play(good ? "reaction_good" : "reaction_bad", 0.75f);
            }
            cartMarks.Add(mark);
        }

        /// <summary>One pill per stat a reaction moves, at the bar it moved, coloured by that stat.</summary>
        void CartEffect(Bob.Rules.Effect f)
        {
            for (int i = 0; i < 4; i++)
            {
                int d = f.D[i];
                if (d == 0) continue;
                CartDelta(i, d);
            }
        }

        Vector2 CartDeltaPoint(int stat, float fraction)
        {
            var at = CartBarPoint(stat, fraction);
            at.x = Mathf.Clamp(at.x, CartBarPoint(stat, 0f).x + 29f, CartBarPoint(stat, 1f).x - 29f);
            return at;
        }

        void CartDelta(int stat, int delta)
        {
            var colour = delta > 0 ? BobUiR1.Green : BobUiR1.Red;
            var pill = BobUiR1.Panel(fx, colour, name: "Delta-" + BobTaskRaceRules.CartKeys[stat]);
            pill.raycastTarget = false;
            float fraction = cartStats[stat] / (float)Mathf.Max(1, BobTaskRaceRules.CartCeiling[stat]);
            BobUi.Pin(pill.rectTransform, Vector2.one * 0.5f, Vector2.one * 0.5f,
                CartDeltaPoint(stat, fraction), new Vector2(58f, 52f));
            var text = CartLabel(pill.transform, (delta > 0 ? "+" : "") + delta, 22, BobUiR1.Paper,
                TextAnchor.MiddleCenter, true);
            BobUi.Stretch(text.rectTransform);
            cartBadges.Add(new CartBadge { img = pill, label = text, life = 1.5f, from = 1.5f,
                colour = colour, stat = stat, fraction = fraction });
        }

        void TickCart(float dt)
        {
            if (step != 1 || cartLocked) return;
            if (!holding) { cartHold = Mathf.Max(0f, cartHold - dt * 2f); }
            else cartHold = Mathf.Clamp01(cartHold + dt / 0.6f);
            CartFillAmount(cartLockFill, cartHold);
            if (cartHold >= 1f) CartLock();
        }

        /// <summary>Lock the stack: score it against the worksheet, then let Tick's Advance bank the step.</summary>
        void CartLock()
        {
            if (cartLocked) return;
            cartLocked = true;
            holding = false;
            CartPassCount = BobTaskRaceRules.CartPassCount(cartStats, cartNeed);
            CartPassed = BobTaskRaceRules.CartPassed(CartPassCount, difficulty);
            if (cartLockLabel != null) cartLockLabel.text = CartPassed ? "ผ่านสูตร · ทีมได้บัฟ" : "ล็อกแล้ว · ยังไม่ได้บัฟ";
            if (cartLock != null) cartLock.color = CartPassed ? BobUiR1.Sun : BobUiR1.Paper;
            LayoutCart();
            BobSfx.Play(CartPassed ? "reaction_good" : "reaction_bad", 0.9f);
            var lockAt = cartLock != null
                ? (Vector2)play.InverseTransformPoint(cartLock.rectTransform.TransformPoint(cartLock.rectTransform.rect.center))
                : Vector2.zero;
            Ring(lockAt, CartPassed ? BobUiR1.Green : BobUiR1.Sun, 160f);
            if (CartPassed) Fx(fxHit, lockAt, 240f, 0.6f, 25f);
            Hint = CartPassed
                ? $"ผ่านสูตร {CartPassCount}/4 · ทีมได้บัฟ"
                : $"ล็อกได้ แต่ผ่าน {CartPassCount}/4 · ยังไม่ได้บัฟ";
            Debug.Log($"[BobMinigameHost] T04 lock: {string.Join("+", cartStack)} -> " +
                      $"clean {cartStats[0]} gentle {cartStats[1]} speed {cartStats[2]} shine {cartStats[3]} · " +
                      $"{CartPassCount}/4 (D{difficulty}) pass={CartPassed}");
            stepProgress = 1f;
        }

        // ---------------------------------------------------------------- per-kind tick / input

        void TickTap(float dt)
        {
            foreach (var it in items)
            {
                if (!it.alive) continue;
                if (taskId == "T03")
                {
                    it.home += it.vel * dt;
                    if (Mathf.Abs(it.home.x) > HalfW - it.size * 0.5f) { it.vel.x = -it.vel.x; it.home.x = Mathf.Clamp(it.home.x, -HalfW + it.size * 0.5f, HalfW - it.size * 0.5f); }
                    if (Mathf.Abs(it.home.y) > HalfH - it.size * 0.5f) { it.vel.y = -it.vel.y; it.home.y = Mathf.Clamp(it.home.y, -HalfH + it.size * 0.5f, HalfH - it.size * 0.5f); }
                    it.rect.anchoredPosition = it.home;
                }
                else if (taskId == "T10")
                {
                    it.life -= dt;
                    if (it.alive && it.life <= 0f)
                    {
                        it.alive = false;
                        Pop(it, T.surface, fxDust); // the pile puffs out instead of blinking off
                        it.home = RandomSpot();
                        it.life = it.maxLife;
                    }
                }
            }
            if (taskId == "T10") WakeDust();
        }

        void TickHold(float dt)
        {
            if (holding && !rotate) holdFill = Mathf.Clamp01(holdFill + dt / holdSeconds);
            if (!holding && !rotate) return;
            stepProgress = Mathf.Clamp01(holdFill);
            if (meterFill != null)
            {
                var rt = (RectTransform)meterFill.transform;
                rt.sizeDelta = new Vector2(-12f, 6f + holdFill * 328f);
            }
        }

        void TickFill(float dt)
        {
            if (!holding) return;
            fillLevel = Mathf.Clamp01(fillLevel + pourSpeed * dt);
            var rt = (RectTransform)bottleFill.transform;
            rt.sizeDelta = new Vector2(rt.sizeDelta.x, fillLevel * 424f);

            bool inBand = Mathf.Abs(fillLevel - bandCentre) <= bandHalf;
            if (inBand && !bandLit)
            {
                bandLit = true;
                bandFlash = 0.4f;
                BobMinigameSfx.Touch(MiniKind.Fill, taskId);
                Ring(FillBottleAt, T.success, 260f); // the band blinks the moment the liquid reaches it
                Fx(fxSplash, FillBottleAt, 300f, 0.35f);
            }
            else if (!inBand) bandLit = false;
        }

        void TickSequence(float dt)
        {
            if (stage != 0) return;
            flashTimer -= dt;
            if (flashTimer > 0f) return;
            if (flashPad >= 0) SetPadLit(flashPad, false);
            flashPad = -1;
            if (playIndex >= sequence.Count)
            {
                stage = 1;
                Hint = HintFor(taskId, step);
                return;
            }
            flashPad = sequence[playIndex++];
            SetPadLit(flashPad, true);
            flashTimer = flashSeconds;
        }

        void TapAt(Vector2 p)
        {
            var hit = Nearest(p, 150f);
            if (hit == null) return;
            Remove(hit);
            BobMinigameSfx.Touch(MiniKind.Tap, taskId);
            int gone = 0;
            foreach (var it in items) if (it.removed) gone++;
            stepProgress = items.Count == 0 ? 1f : gone / (float)items.Count;
        }

        void CoverAt(Vector2 p)
        {
            int covered = 0; bool touched = false;
            foreach (var it in items)
            {
                if (!it.alive) { covered++; continue; }
                if (Vector2.Distance(it.home, p) <= 54f)
                {
                    it.alive = false;
                    Pop(it, T.primaryLight);
                    covered++; touched = true;
                }
            }
            if (touched) BobMinigameSfx.Touch(MiniKind.Trace, taskId);
            stepProgress = items.Count == 0 ? 1f : covered / (float)items.Count;
        }

        void ScrubAt(float distance, Vector2 p)
        {
            if (distance <= 0f) return;
            var stain = Nearest(p, 160f);
            if (stain == null || stain.hp <= 0f) return;
            float required = taskId == "T08" ? 900f : taskId == "T09" ? 700f : 1500f - 200f * (difficulty - 1);
            // Clean buff: the same stroke lifts more grime, so every scrub card asks for about a quarter less work.
            if (Buffed("clean")) required *= 0.72f;
            stain.hp = Mathf.Max(0f, stain.hp - distance / required);
            if (Time.unscaledTime - lastPuff > 0.14f)
            {
                lastPuff = Time.unscaledTime;
                // A little foam pop under the finger so scrubbing feels like contact, not a number going down.
                if (fxFoam != null) Fx(fxFoam, p, 180f, 0.3f, -40f);
                else Ring(p, T.surface, 110f);
            }
            PaintScrub();
            float left = 0f;
            foreach (var it in items) left += Mathf.Max(0f, it.hp);
            stepProgress = items.Count == 0 ? 1f : 1f - left / items.Count;
        }

        void PaintScrub()
        {
            foreach (var it in items)
            {
                if (it.popLife > 0f) continue; // already squashing away
                float hp = Mathf.Max(0f, it.hp);
                it.img.color = new Color(it.img.color.r, it.img.color.g, it.img.color.b, 0.2f + 0.8f * hp);
                it.rect.localScale = Vector3.one * (0.75f + 0.25f * hp);
                if (hp <= 0.02f && it.alive) { it.alive = false; Pop(it, T.primaryLight); }
            }
            foreach (var dot in grime)
            {
                float hp = dot.Stain != null ? Mathf.Max(0f, dot.Stain.hp) : 0f;
                bool show = hp > 0.02f;
                dot.Dot.rect.gameObject.SetActive(show);
                if (!show) continue;
                var c = dot.Dot.img.color;
                dot.Dot.img.color = new Color(c.r, c.g, c.b, 0.9f * hp);
                dot.Dot.rect.localScale = Vector3.one * (0.55f + 0.45f * hp);
            }
        }

        void StartHold(Vector2 p)
        {
            if (Vector2.Distance(p, holdCentre) > 180f) return;
            holding = true; lastAngle = float.NaN;
            if (holdButton != null) holdButton.punch = PunchTime;
            BobMinigameSfx.Touch(MiniKind.Hold, taskId);
            Ring(holdCentre, T.primaryLight, 340f);
        }

        void TurnAt(Vector2 p)
        {
            if (!rotate || !holding) return;
            var v = p - holdCentre;
            if (v.sqrMagnitude < 100f) return;
            float angle = Mathf.Atan2(v.y, v.x);
            if (!float.IsNaN(lastAngle))
            {
                float d = Mathf.DeltaAngle(lastAngle * Mathf.Rad2Deg, angle * Mathf.Rad2Deg) * Mathf.Deg2Rad;
                turnAccum += Mathf.Abs(d);
                while (turnAccum >= Mathf.PI)
                {
                    turnAccum -= Mathf.PI;
                    holdFill = Mathf.Clamp01(holdFill + 1f / turnRequired);
                }
            }
            lastAngle = angle;
        }

        void PickUp(Vector2 p)
        {
            held = Nearest(p, 120f);
            if (held == null) return;
            heldOffset = p - held.home;
            BobMinigameSfx.Touch(MiniKind.Sort, taskId);
        }

        void DragHeld(Vector2 p)
        {
            if (held == null) return;
            held.home = p - heldOffset;
            held.rect.anchoredPosition = held.home;
        }

        void DropHeld(Vector2 p)
        {
            if (held == null) return;
            var card = held;
            held = null;
            foreach (var bin in bins)
            {
                if (Vector2.Distance(bin.home, p) > 180f) continue;
                if (bin.bin == card.bin)
                {
                    card.alive = false; card.removed = true; // counts at once; the card flies into the bin
                    StartFly(card, bin.home, true);
                    bin.punch = PunchTime;
                    Ring(bin.home, T.success, 260f);
                    Fx(fxSparkle, bin.home, 360f, 0.5f, 30f);
                    BobMinigameSfx.Sorted(true);
                    Hint = HintFor(taskId, step);
                    int sorted = 0;
                    foreach (var it in items) if (!it.alive) sorted++;
                    stepProgress = items.Count == 0 ? 1f : sorted / (float)items.Count;
                }
                else
                {
                    StartFly(card, card.slotHome, false); // wrong bin: slide home and shake, never teleport
                    Ring(p, T.danger, 200f);
                    BobMinigameSfx.Sorted(false);
                    Hint = "ผิดถัง! ลองอีกที";
                }
                return;
            }
            StartFly(card, card.slotHome, false);
        }

        void PadAt(Vector2 p)
        {
            if (stage != 1) return;
            for (int i = 0; i < pads.Count; i++)
            {
                if (Vector2.Distance(pads[i].home, p) > 120f) continue;
                if (i == sequence[seqIndex])
                {
                    seqIndex++;
                    stepProgress = seqIndex / (float)sequence.Count;
                    pads[i].punch = PunchTime;
                    BobMinigameSfx.Touch(MiniKind.Sequence, taskId);
                    Ring(pads[i].home, PadBase(pads[i]), 230f);
                }
                else
                {
                    seqIndex = 0; playIndex = 0; stage = 0; flashTimer = flashSeconds; flashPad = -1;
                    Hint = "ผิด! ดูอีกรอบ";
                    BobMinigameSfx.Wrong("ui_cancel");
                    pads[i].shake = ShakeTime;
                    Ring(pads[i].home, T.danger, 230f);
                }
                return;
            }
        }

        void SetPadLit(int index, bool lit)
        {
            if (index < 0 || index >= pads.Count) return;
            var pad = pads[index];
            pad.img.color = lit ? T.primary : PadBase(pad);
            pad.rect.localScale = Vector3.one * (lit ? 1.06f : 1f);
            if (lit) BobMinigameSfx.Flash();
            if (pad.twin != null && stage3d != null) stage3d.Tint(pad.twin, pad.img.color);
        }

        Color PadBase(Item pad)
        {
            var colours = new[] { T.sky, T.success, T.primary, T.accent };
            int i = pads.IndexOf(pad);
            return i >= 0 && i < colours.Length ? colours[i] : T.surfaceCream;
        }

        /// <summary>Keep two dust piles on screen at once (T10): a vanished one comes back after a short pause.</summary>
        void WakeDust()
        {
            int alive = 0;
            foreach (var it in items) if (it.alive) alive++;
            int wanted = difficulty == 1 ? 1 : 2;
            foreach (var it in items)
            {
                if (alive >= wanted) break;
                if (it.alive || it.removed || it.popLife > 0f) continue;
                it.alive = true;
                it.life = it.maxLife;
                it.home = RandomSpot();
                it.rect.localScale = Vector3.one;
                it.rect.anchoredPosition = it.home;
                it.rect.gameObject.SetActive(true);
                alive++;
            }
        }

        // ---------------------------------------------------------------- helpers

        /// <summary>
        /// T16's คราบลาม rows carry a "~n" suffix so the round list keeps unique ids ("T03~1"). Everything the card
        /// draws is the real minigame, so strip the suffix before the id reaches the kind tables.
        /// </summary>
        static string BaseId(string id)
        {
            if (string.IsNullOrEmpty(id)) return "";
            int cut = id.IndexOf('~');
            return cut < 0 ? id : id.Substring(0, cut);
        }

        static MiniKind KindOf(string id) => id switch
        {
            "T01" => MiniKind.Trace, "T02" => MiniKind.Scrub, "T03" => MiniKind.Tap, "T04" => MiniKind.Cart,
            "T05" => MiniKind.Sort, "T06" => MiniKind.Hold, "T07" => MiniKind.Scrub, "T08" => MiniKind.Scrub,
            "T09" => MiniKind.Hold, "T10" => MiniKind.Tap, "T11" => MiniKind.Fill, "T13" => MiniKind.Hold,
            "T14" => MiniKind.Trace, "T15" => MiniKind.Sequence, _ => MiniKind.Timed,
        };

        static int StepsOf(string id) => id switch { "T04" => 2, "T07" => 3, "T08" => 2, "T09" => 2, "T14" => 3, _ => 1 };

        static string HintFor(string id, int step) => id switch
        {
            "T01" => "ลากนิ้วตามเส้นฝุ่น",
            "T02" => "ถูวนบนคราบ",
            "T03" => "แตะสไลม์ให้หมด",
            "T04" => step == 0 ? $"แตะหยิบอุปกรณ์ {BobTaskRaceRules.CartParts} อย่างจากชั้น" : "ลากให้ผ่านสูตร แล้วกดค้างล็อก",
            "T05" => "ลากขยะลงถังให้ตรง",
            "T06" => "กดค้างแล้วหมุนครึ่งวง",
            "T07" => step == 0 ? "ถูจานจนสะอาด" : step == 1 ? "กดค้างล้างฟอง" : "แตะเพื่อวางจาน",
            "T08" => step == 0 ? "แตะขวดสเปรย์" : "ลากเช็ดกระจก",
            "T09" => step == 0 ? "กดค้างเทน้ำยา" : "ถูคราบที่อ่อนตัว",
            "T10" => "แตะกองฝุ่นให้ทัน",
            "T11" => "กดค้างให้ถึงขีดเขียว",
            "T13" => "กดค้างยกพร้อมกัน",
            "T14" => "ลากไม้ถูตามแถว",
            "T15" => "กดตามลำดับที่กะพริบ",
            _ => "",
        };

        Vector2 RandomSpot() => new Vector2(
            Mathf.Lerp(-HalfW + 90f, HalfW - 90f, (float)rng.NextDouble()),
            Mathf.Lerp(-HalfH + 90f, HalfH - 90f, (float)rng.NextDouble()));

        Item Nearest(Vector2 p, float radius)
        {
            Item best = null;
            float bestDistance = float.MaxValue;
            foreach (var it in items)
            {
                if (!it.alive) continue;
                float reach = Mathf.Max(radius, it.size * 0.55f);
                float d = Vector2.Distance(it.home, p);
                if (d > reach || d >= bestDistance) continue;
                best = it; bestDistance = d;
            }
            return best;
        }

        void Remove(Item it)
        {
            if (it == null) return;
            it.alive = false;
            it.removed = true;
            Pop(it, T.primaryLight, taskId == "T03" ? fxSplat : null);
        }

        Item MakeDisc(Vector2 pos, float size, Color colour)
        {
            var img = BobUi.Disc(layer, colour, size, "MiniItem");
            img.raycastTarget = false;
            var rt = (RectTransform)img.transform;
            rt.anchoredPosition = pos;
            var outline = img.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(T.ink.r, T.ink.g, T.ink.b, 0.75f);
            outline.effectDistance = new Vector2(3f, -3f);
            var item = new Item { rect = rt, img = img, home = pos, size = size };
            if (stage3d != null && stage3d.Ready)
            {
                item.twin = DiscTwin(item, colour);
                img.enabled = false; // the stage body is the picture; the rect stays as the hitbox
            }
            items.Add(item);
            return item;
        }

        /// <summary>The 3D body for a round item: a real slime rig, a smear on the table, a plate, or a dust puff.</summary>
        Transform DiscTwin(Item it, Color colour)
        {
            if (taskId == "T03") return stage3d.AddSlime(it.home, it.size * 0.9f, slimeVariant++);
            if (taskId == "T08" && stepKind == MiniKind.Scrub) return stage3d.AddPane(it.home, it.size * 0.8f, colour);
            if (taskId == "T06") return stage3d.AddDisc(it.home, it.size * 0.9f, colour, 0.30f); // a dial, not a puff
            if (taskId == "T07") return stage3d.AddDisc(it.home, it.size * 0.9f, colour, 0.12f);
            if (taskId == "T02" || taskId == "T09") return stage3d.AddDisc(it.home, it.size * 0.9f, colour, 0.06f);
            return stage3d.AddBlob(it.home, it.size * 0.7f, colour);
        }

        Item MakeBox(Vector2 pos, Vector2 size, Color colour, string text, int fontSize, bool addToItems = true,
            string prop = null)
        {
            var img = BobUi.Panel(layer, colour, null, "MiniBox");
            img.raycastTarget = false;
            var rt = (RectTransform)img.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            var outline = img.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(T.ink.r, T.ink.g, T.ink.b, 0.55f);
            outline.effectDistance = new Vector2(3f, -3f);
            var label = BobUi.Label(rt, text, fontSize, T.ink, TextAnchor.MiddleCenter, FontStyle.Bold);
            BobUi.Stretch(label.rectTransform);
            var item = new Item { rect = rt, img = img, home = pos, size = Mathf.Max(size.x, size.y) };
            if (stage3d != null && stage3d.Ready)
            {
                item.twin = prop != null
                    ? stage3d.AddProp(prop, pos, size.y / BobMinigameStage.PxPerUnit)
                    : stage3d.AddBox(pos, size, colour);
                img.enabled = false; // the label stays on top of the 3D box
            }
            if (addToItems) items.Add(item);
            return item;
        }

        void HideItems()
        {
            foreach (var it in items)
                if (it.rect != null) DestroyNow(it.rect.gameObject);
            foreach (var go in decor)
                if (go != null) DestroyNow(go);
            ClearRings();
            items.Clear();
            bins.Clear();
            pads.Clear();
            grime.Clear();
            decor.Clear();
            sequence.Clear();
            held = null;
            holdButton = null;
            bandLit = false;
            meterTrack = meterFill = bottleFill = band = null;
            // T04: the data lists survive (the tray is handed from step 1 to step 2); only the widgets die here.
            cartChips.Clear();
            cartLayers.Clear();
            cartBars.Clear();
            cartValues.Clear();
            cartMarks.Clear();
            cartShelfCards.Clear();
            cartPartViews.Clear();
            cartSlots.Clear();
            cartPickSlots.Clear();
            cartPickNames.Clear();
            cartMetWords.Clear();
            cartTargets.Clear();
            cartLayout.Clear();
            cartLayoutWidth = -1f;
            cartDropZone = cartInlineHint = cartPickFill = null;
            cartPickCount = cartTrayEmpty = cartTrayHint = cartSlotCount = null;
            cartError = "";
            foreach (var b in cartBadges) if (b.img != null) DestroyNow(b.img.gameObject);
            cartBadges.Clear();
            buffRibbon.Clear();
            buffRibbonTime = 0f;
            cartChipHeld = null; cartLock = null; cartLockFill = null; cartLockLabel = null; cartRecipe = null;
            cartCostText = null; cartBudgetFill = null; cartRecipeIsBuild = false;
            cartHold = 0f;
        }

        GameObject Track(GameObject go) { decor.Add(go); return go; }

        static void DestroyNow(GameObject go)
        {
            if (Application.isPlaying) Destroy(go);
            else DestroyImmediate(go);
        }
    }

    /// <summary>The 1240 x 620 pointer surface: one finger, events forwarded in play-area pixels.</summary>
    public class MiniSurface : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        public BobMinigameHost host;
        RectTransform rect;

        public void OnPointerDown(PointerEventData e) => Pass(e, host != null ? (Action<Vector2>)host.PointerDown : null);
        public void OnDrag(PointerEventData e) => Pass(e, host != null ? (Action<Vector2>)host.PointerDrag : null);
        public void OnPointerUp(PointerEventData e) => Pass(e, host != null ? (Action<Vector2>)host.PointerUp : null);

        void Pass(PointerEventData e, Action<Vector2> action)
        {
            if (host == null || action == null) return;
            if (rect == null) rect = (RectTransform)transform;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, e.position, e.pressEventCamera, out var local))
                action(local);
        }
    }
}
