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
using Bob.Game;
using Bob.UI;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Bob.TaskRace
{
    public enum MiniKind { Timed, Trace, Scrub, Tap, Sort, Hold, Fill, Sequence }

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
            kind = KindOf(id);
            taskId = id ?? "";
            if (kind == MiniKind.Timed) { Hide(); return false; }

            spot = station;
            difficulty = Mathf.Clamp(roundDifficulty, 1, 3);
            stepCount = StepsOf(taskId);
            step = 0; stepProgress = 0f; complete = false; playing = true;
            placeholder.gameObject.SetActive(false);
            play.gameObject.SetActive(true);
            SetupStep();
            BobTutorialClip.Play(play, taskId);
            BobMinigameSfx.Open();
            return true;
        }

        /// <summary>HUD: the card is a timed one (sabotage / cart / chairs): show one line instead of a play area.</summary>
        public void ShowPlaceholder(string text)
        {
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
            TickJuice(dt); // feedback keeps moving even when the card is already solved
            Sync3D();
            if (complete) return true;
            if (!playing || spot == null) return false;

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

        void LateUpdate() => Sync3D();

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
            }
        }

        internal void PointerUp(Vector2 p)
        {
            if (!playing) return;
            if (brush != null) brush.gameObject.SetActive(false);

            if (stepKind == MiniKind.Scrub) BobMinigameSfx.Scrub(false);
            if (stepKind == MiniKind.Sort) DropHeld(p);
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

            slimeVariant = 0;
            if (stage3d != null && stage3d.Ready) stage3d.Show(taskId); // also drops last step's twins
            AddContext();
            switch (stepKind)
            {
                case MiniKind.Trace: SetupTrace(); break;
                case MiniKind.Scrub: SetupScrub(); break;
                case MiniKind.Tap: SetupTap(); break;
                case MiniKind.Sort: SetupSort(); break;
                case MiniKind.Hold: SetupHold(); break;
                case MiniKind.Fill: SetupFill(); break;
                case MiniKind.Sequence: SetupSequence(); break;
            }
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
                var text = BobUi.Label(display.transform, "ซัก 40°", 40, T.primaryLight, TextAnchor.MiddleCenter, FontStyle.Bold);
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
                float life = 1.6f - 0.3f * (difficulty - 1);
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

            // T03: slimes crawl slowly; a hit removes one.
            int count = 5 + (difficulty - 1) * 2;
            float speed = 80f + 60f * (difficulty - 1);
            for (int i = 0; i < count; i++)
            {
                var slime = MakeDisc(RandomSpot(), 200f - 25f * (difficulty - 1), T.success);
                float a = (float)rng.NextDouble() * Mathf.PI * 2f;
                slime.vel = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * speed;
            }
        }

        void SetupSort()
        {
            int count = 6 + (difficulty - 1) * 2;
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
            string[] labels = { "▲ น้ำ", "● ผง", "■ ปั่น", "▼ อุณหภูมิ" };
            Color[] colours = { T.sky, T.success, T.primary, T.accent };
            for (int i = 0; i < 4; i++)
            {
                var pad = MakeBox(new Vector2(-390f + i * 260f, -60f), new Vector2(210f, 190f), colours[i], labels[i], 30);
                pads.Add(pad);
            }
            int length = 2 + difficulty;
            for (int i = 0; i < length; i++) sequence.Add(rng.Next(4));
            flashTimer = 0.5f;
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
            flashTimer = 0.45f;
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
                    seqIndex = 0; playIndex = 0; stage = 0; flashTimer = 0.5f; flashPad = -1;
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

        static MiniKind KindOf(string id) => id switch
        {
            "T01" => MiniKind.Trace, "T02" => MiniKind.Scrub, "T03" => MiniKind.Tap, "T05" => MiniKind.Sort,
            "T06" => MiniKind.Hold, "T07" => MiniKind.Scrub, "T08" => MiniKind.Scrub, "T09" => MiniKind.Hold,
            "T10" => MiniKind.Tap, "T11" => MiniKind.Fill, "T13" => MiniKind.Hold, "T14" => MiniKind.Trace,
            "T15" => MiniKind.Sequence, _ => MiniKind.Timed,
        };

        static int StepsOf(string id) => id switch { "T07" => 3, "T08" => 2, "T09" => 2, "T14" => 3, _ => 1 };

        static string HintFor(string id, int step) => id switch
        {
            "T01" => "ลากนิ้วตามเส้นฝุ่น",
            "T02" => "ถูวนบนคราบ",
            "T03" => "แตะสไลม์ให้หมด",
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
