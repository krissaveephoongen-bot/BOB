// Battle of the Brooms: T12 จัดโต๊ะเก้าอี้ (docs 16 section 5) — the one card that is played in the world instead of
// the S28 card. Each seat of the team is a tipped chair near the station: walk to it, press "จับเก้าอี้", drag it home
// (the body walks 20% slower) and put it in a slot beside the station. When every chair is home the station is clean.
//
// Why the world: docs 16 section 5 asks for exactly this — no card, the chair is an object that follows the hand, and
// the server would hold its position like the pushed cart. The component builds its own chairs and slots from the
// shipped chair prop, so the map needs no scene rebuild; it lives on the station it was handed by TaskRaceRound.
//
// v1 scope: the team ring under a chair is the chair's own colour, the owner portrait of the spec is not drawn, the
// obstacle clutter per difficulty is not placed, and the slots are thin rings rather than dashed rectangles. The
// prompt lives on the main context button (BobTaskRaceHud), so no extra button is added while walking.
using System;
using System.Collections.Generic;
using Bob.Art;
using UnityEngine;

namespace Bob.TaskRace
{
    [DisallowMultipleComponent]
    public class BobChairTask : MonoBehaviour
    {
        /// <summary>Reach of the "จับเก้าอี้" press, in flat world units.</summary>
        public const float GrabRadius = 1.05f;
        /// <summary>How close the slot must be for the chair to seat itself on release.</summary>
        public const float SeatRadius = 1.15f;
        /// <summary>docs 16 section 5: dragging a chair makes the body walk 20% slower.</summary>
        public const float DragSlow = 0.8f;
        const int RingSegments = 40;

        sealed class Chair
        {
            public Transform body;
            public LineRenderer ring;
            public bool seated;
            public int slot = -1;
        }

        /// <summary>The chair task of the current round, or null. The HUD reads it for the context button.</summary>
        public static BobChairTask Current { get; private set; }
        /// <summary>Toast lines for the HUD, e.g. "เก้าอี้เข้าที่ 1/2".</summary>
        public static event Action<string> AnyMessage;

        TaskSpot spot;
        BobTaskRaceHero hero;
        int seats = 2;
        readonly List<Chair> chairs = new List<Chair>();
        readonly List<Vector3> slots = new List<Vector3>();
        readonly List<LineRenderer> slotRings = new List<LineRenderer>();
        Chair held;
        Transform root;
        float finishTimer = -1f, pulse;
        bool finished, built;

        public bool Active => built && !finished;
        public int Seats => seats;
        public int Seated { get; private set; }
        public bool Holding => held != null;

        // ---------------------------------------------------------------- attach / teardown

        /// <summary>
        /// TaskRaceRound hands a station its T12 card. One component per station: a restock on the same station reuses
        /// it, and the component tears itself down as soon as the station carries another task.
        /// </summary>
        public static BobChairTask Attach(TaskSpot station, int players)
        {
            if (station == null) return null;
            var comp = station.GetComponent<BobChairTask>();
            if (comp == null) comp = station.gameObject.AddComponent<BobChairTask>();
            comp.Bind(station, players);
            return comp;
        }

        void Bind(TaskSpot station, int players)
        {
            DropHeld(false);
            Clear();
            spot = station;
            hero = FindAnyObjectByType<BobTaskRaceHero>();
            seats = Mathf.Clamp(players <= 1 ? 2 : players, 2, 4); // docs 16: one chair per player, two for a lone hero
            finished = false;
            finishTimer = -1f;
            Seated = 0;
            built = true;
            Build();
            Current = this;
            Debug.Log($"[BobChairTask] {spot.SpotId}: {seats} chairs, players {players}.");
        }

        void OnDisable()
        {
            DropHeld(false);
            Clear();
            if (Current == this) Current = null;
        }

        // ---------------------------------------------------------------- build

        void Build()
        {
            var anchor = spot.transform;
            float floorY = anchor.position.y;
            var go = new GameObject("T12_Chairs");
            go.transform.SetParent(anchor, false);
            root = go.transform;

            // Slots: a short arc in front of the station, so the hero can see the empty holes from the chair he picks up.
            for (int i = 0; i < seats; i++)
            {
                float u = seats <= 1 ? 0.5f : i / (float)(seats - 1);
                float x = Mathf.Lerp(-0.95f, 0.95f, u);
                var local = new Vector3(x, 0f, 1.75f);
                var world = anchor.TransformPoint(local);
                world.y = floorY;
                slots.Add(world);
                slotRings.Add(Ring("Slot_" + i, world, 0.42f, new Color(0.55f, 0.50f, 0.42f, 0.55f), 0.055f));
            }

            var prefab = Resources.Load<GameObject>("Props/SM_chair_bentwood");
            if (prefab == null) Debug.LogWarning("[BobChairTask] Props/SM_chair_bentwood missing: the slots still count.");
            var rng = new System.Random(spot.SpotId.GetHashCode() ^ (seats * 397));
            for (int i = 0; i < seats; i++)
            {
                // Spread the fallen chairs around the back and the sides: never on top of a slot, never on each other.
                float a = Mathf.Lerp(-150f, 150f, seats <= 1 ? 0.5f : i / (float)(seats - 1)) + (float)(rng.NextDouble() * 14.0 - 7.0);
                float r = 2.45f + (float)rng.NextDouble() * 0.45f;
                var dir = Quaternion.Euler(0f, a, 0f) * anchor.forward;
                var world = anchor.position + dir * r;
                world.y = floorY;
                var chair = new Chair();
                if (prefab != null)
                {
                    var body = Instantiate(prefab, root, false);
                    body.name = "Chair_" + i;
                    body.transform.position = world;
                    // Tipped: a school chair on its side, then turned a little so each one reads differently.
                    body.transform.rotation = Quaternion.Euler(0f, (float)rng.NextDouble() * 360f, 0f) *
                                              Quaternion.Euler(78f, 0f, 18f) * prefab.transform.rotation;
                    foreach (var rr in body.GetComponentsInChildren<Renderer>(true))
                    {
                        rr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                        rr.receiveShadows = true;
                    }
                    var bounds = Bounds(body);
                    body.transform.position += Vector3.up * (world.y + 0.02f - bounds.min.y);
                    chair.body = body.transform;
                }
                chair.ring = Ring("ChairRing_" + i, world, 0.36f, TeamColour(spot.OwnerTeam), 0.06f);
                chairs.Add(chair);
            }
            RenderSlots();
        }

        static Color TeamColour(TeamId team) => team == TeamId.TeamB
            ? new Color(0.98f, 0.55f, 0.42f, 0.85f)
            : new Color(0.38f, 0.72f, 0.98f, 0.85f);

        /// <summary>A flat ring on the floor, drawn the way the station ring is (LineRenderer, no scene asset).</summary>
        LineRenderer Ring(string name, Vector3 centre, float radius, Color colour, float width)
        {
            var go = new GameObject(name);
            go.transform.SetParent(root != null ? root : transform, false);
            var ring = go.AddComponent<LineRenderer>();
            ring.useWorldSpace = true;
            ring.loop = true;
            ring.positionCount = RingSegments;
            ring.startWidth = ring.endWidth = width;
            ring.numCapVertices = 2;
            ring.numCornerVertices = 2;
            ring.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            ring.receiveShadows = false;
            var shader = Shader.Find("Sprites/Default") ?? Shader.Find("Unlit/Color");
            if (shader != null) ring.material = new Material(shader);
            ring.startColor = ring.endColor = colour;
            SetRing(ring, centre, radius);
            return ring;
        }

        void SetRing(LineRenderer ring, Vector3 centre, float radius)
        {
            var points = new Vector3[RingSegments];
            for (int i = 0; i < RingSegments; i++)
            {
                float a = i / (float)RingSegments * Mathf.PI * 2f;
                var p = centre + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * radius;
                p.y = centre.y + 0.035f;
                points[i] = p + Vector3.up * BobCurvedWorld.Drop(p); // the curved shader bends the ground, not the ring
            }
            ring.SetPositions(points);
        }

        // ---------------------------------------------------------------- per frame

        void Update()
        {
            if (!built || spot == null) return;
            if (spot.TaskId != TaskRaceRound.ChairId)
            {
                // The station was restocked with another card: the chairs belong to the old one.
                Clear();
                built = false;
                if (Current == this) Current = null;
                return;
            }

            if (hero == null) hero = FindAnyObjectByType<BobTaskRaceHero>();
            if (Current == null && !finished) Current = this; // another chair task finished and handed the slot back
            pulse += Time.deltaTime;
            RenderSlots();

            if (finished)
            {
                finishTimer += Time.deltaTime;
                if (finishTimer > 1.1f) Clear();
                return;
            }

            if (held != null)
            {
                if (hero == null) { DropHeld(false); return; }
                var hand = hero.transform.position + hero.transform.forward * 0.85f;
                hand.y = spot.transform.position.y;
                held.body.position = hand;
                held.body.rotation = Quaternion.Euler(0f, hero.transform.eulerAngles.y + 90f, 0f);
                hero.dragFactor = DragSlow;
                return;
            }

            if (hero != null) hero.dragFactor = 1f;
        }

        /// <summary>Slot colour: grey when empty, warm when the chair in hand may go in, green when filled.</summary>
        void RenderSlots()
        {
            int target = HeldSlotIndex();
            for (int i = 0; i < slotRings.Count; i++)
            {
                var ring = slotRings[i];
                if (ring == null) continue;
                bool filled = i < slots.Count && IsFilled(i);
                Color c;
                if (filled) c = new Color(0.35f, 0.78f, 0.45f, 0.9f);
                else if (i == target) c = Color.Lerp(new Color(1f, 0.87f, 0.45f, 0.8f), Color.white, 0.5f + 0.5f * Mathf.Sin(pulse * 6f));
                else c = new Color(0.55f, 0.50f, 0.42f, 0.5f);
                ring.startColor = ring.endColor = c;
            }
        }

        bool IsFilled(int slot)
        {
            foreach (var c in chairs) if (c.seated && c.slot == slot) return true;
            return false;
        }

        int HeldSlotIndex()
        {
            if (held == null || hero == null) return -1;
            return NearestFreeSlot(hero.transform.position, SeatRadius + 0.35f);
        }

        int NearestFreeSlot(Vector3 from, float radius)
        {
            int best = -1;
            float bestDistance = radius;
            for (int i = 0; i < slots.Count; i++)
            {
                if (IsFilled(i)) continue;
                float d = Flat(slots[i] - from).magnitude;
                if (d <= bestDistance) { best = i; bestDistance = d; }
            }
            return best;
        }

        Chair NearestTippedChair(Vector3 from)
        {
            Chair best = null;
            float bestDistance = GrabRadius;
            foreach (var c in chairs)
            {
                if (c.seated || c.body == null) continue;
                float d = Flat(c.body.position - from).magnitude;
                if (d <= bestDistance) { best = c; bestDistance = d; }
            }
            return best;
        }

        static Vector3 Flat(Vector3 v) { v.y = 0f; return v; }

        // ---------------------------------------------------------------- main button (HUD)

        /// <summary>
        /// HUD context button for this station: the label and whether it can be pressed right now. The chairs are
        /// 2–3 units out from the station, further than the station's own reach, so the prompt follows the hero and
        /// the chair in his hands, not only the station. False when the press belongs to the normal "ทำงาน" path.
        /// </summary>
        public bool PromptFor(TaskSpot near, out string label, out bool can)
        {
            label = null; can = false;
            if (!Active) return false;
            if (hero == null) hero = FindAnyObjectByType<BobTaskRaceHero>();
            if (hero == null) return false;
            var pos = hero.transform.position;

            if (held != null)
            {
                bool inSlot = HeldSlotIndex() >= 0;
                label = inSlot ? "วางเก้าอี้ที่ช่อง" : "ลากเก้าอี้ไปที่โต๊ะ";
                can = inSlot;
                return true;
            }

            var chair = NearestTippedChair(pos);
            if (chair != null)
            {
                label = "จับเก้าอี้";
                can = true;
                return true;
            }
            // Standing at the station with nothing in reach: keep the reminder, but the press does nothing.
            bool atStation = near != null && near == spot;
            if (!atStation && Flat(pos - spot.transform.position).magnitude > 4f) return false;
            label = $"เดินไปที่เก้าอี้ ({Seated}/{seats})";
            can = false;
            return true;
        }

        /// <summary>
        /// HUD press: grab the chair at the hero's feet, or seat the one in hand when he stands at a slot. Returns
        /// false only when the press is not this task's business (the normal card path then runs).
        /// </summary>
        public bool TryPress(TaskSpot near)
        {
            if (!Active) return false;
            if (hero == null) hero = FindAnyObjectByType<BobTaskRaceHero>();
            if (hero == null) return false;
            var pos = hero.transform.position;

            if (held != null)
            {
                int slot = NearestFreeSlot(pos, SeatRadius);
                if (slot >= 0) Seat(held, slot);
                else BobMinigameSfx.Wrong("drop_invalid");
                return true;
            }

            var chair = NearestTippedChair(pos);
            if (chair == null)
            {
                // At the station itself the press must not fall through to the S28 card: T12 has no card.
                bool atStation = near != null && near == spot;
                if (!atStation) return false;
                AnyMessage?.Invoke("เดินไปที่เก้าอี้ที่ล้มอยู่ก่อน");
                return true;
            }
            held = chair;
            BobSfx.Play("cart_grab", 0.85f);
            BobSfx.Loop("cart_roll_loop", true, 0.45f);
            if (hero != null) hero.dragFactor = DragSlow;
            return true;
        }

        void Seat(Chair chair, int slot)
        {
            chair.seated = true;
            chair.slot = slot;
            held = null;
            Seated++;
            BobSfx.Loop("cart_roll_loop", false);
            BobSfx.Play("table_put", 0.9f);
            if (hero != null) hero.dragFactor = 1f;
            if (chair.body != null)
            {
                var at = slots[slot];
                chair.body.position = new Vector3(at.x, at.y + 0.02f, at.z);
                chair.body.rotation = Quaternion.LookRotation(Flat(spot.transform.position - at), Vector3.up);
                var bounds = Bounds(chair.body.gameObject);
                chair.body.position += Vector3.up * (at.y + 0.02f - bounds.min.y);
            }
            if (chair.ring != null)
            {
                chair.ring.startColor = chair.ring.endColor = new Color(0.35f, 0.78f, 0.45f, 0.9f);
                SetRing(chair.ring, slots[slot], 0.36f);
            }
            spot.SetProgress(Seated / (float)Mathf.Max(1, seats));
            AnyMessage?.Invoke($"เก้าอี้เข้าที่ {Seated}/{seats}");
            RenderSlots();
            if (Seated < seats) return;

            finished = true;
            finishTimer = 0f;
            AnyMessage?.Invoke($"จัดโต๊ะครบ {seats} ที่แล้ว");
            if (Current == this) Current = null;
            spot.SetProgress(1f);
            // The station banks the task through the normal Closed path: no card, no lock, the same points.
            spot.ForceComplete();
            Debug.Log($"[BobChairTask] {spot.SpotId}: all {seats} chairs home, station force-completed.");
        }

        void DropHeld(bool message)
        {
            if (held == null) return;
            held = null;
            BobSfx.Loop("cart_roll_loop", false);
            if (hero != null) hero.dragFactor = 1f;
            if (message) AnyMessage?.Invoke("วางเก้าอี้ไม่ลงช่อง");
        }

        void Clear()
        {
            DropHeld(false);
            foreach (var c in chairs)
            {
                if (c.body != null) DestroySafe(c.body.gameObject);
                if (c.ring != null) DestroySafe(c.ring.gameObject);
            }
            chairs.Clear();
            foreach (var r in slotRings) if (r != null) DestroySafe(r.gameObject);
            slotRings.Clear();
            slots.Clear();
            if (root != null) DestroySafe(root.gameObject);
            root = null;
            Seated = 0;
        }

        static void DestroySafe(GameObject go)
        {
            if (go == null) return;
            if (Application.isPlaying) Destroy(go); else DestroyImmediate(go);
        }

        static Bounds Bounds(GameObject go)
        {
            var renderers = go.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return new Bounds(go.transform.position, Vector3.one * 0.5f);
            var bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            return bounds;
        }

        // ---------------------------------------------------------------- verify hooks

        /// <summary>Verify: grab the nearest chair without walking there.</summary>
        public bool DebugGrabNow()
        {
            if (!Active || hero == null) return false;
            var chair = NearestTippedChair(hero.transform.position);
            if (chair == null && chairs.Count > 0) chair = chairs.Find(c => !c.seated);
            if (chair == null) return false;
            held = chair;
            if (hero != null) hero.dragFactor = DragSlow;
            return true;
        }

        /// <summary>Verify: seat every chair at once, so the round can be checked without a drag.</summary>
        public int DebugSeatAll()
        {
            if (!Active) return 0;
            for (int i = 0; i < chairs.Count; i++)
            {
                if (finished) break;
                if (chairs[i].seated) continue;
                int slot = NearestFreeSlot(slots[i], 99f);
                if (slot < 0) break;
                held = chairs[i];
                Seat(chairs[i], slot);
            }
            return Seated;
        }
    }
}
