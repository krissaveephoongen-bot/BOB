// Battle of the Brooms: the emergency stains of docs 16 section 4 — the T16 row of section 5 — a living stain that
// appears at a seeded moment of the round on a station of the local island, puts 25 s on the clock, and is cleared
// with the weakness minigame of that enemy at one difficulty step below the round.
//
// The enemy never attacks, never steals and never chases: it is a task with a clock (a "คราบ"). When the grace runs
// out the stain spreads ("คราบลาม"): one short task joins the team's list and the weight denominator grows with it,
// so the progress bar steps back the way docs 16 section 2.4 asks.
//
// v1 scope, stated in docs 16 section 4 and docs 24 section 15.25:
//   - two enemies per round at D1 and D2, three at D3 (docs 16 section 5 row T16), at the moments the shared rules
//     draw (40 / 75 / 100 s of a 120 s round; a shorter chapter round scales them to its own clock).
//   - the weakness pool follows the chapter: เจ้าคราบ (T02) from round one, สไลม์ (T03) from chapter 2 or the second
//     round of a chapter, ขยะปีศาจ (T05) from chapter 2.
//   - the screen-edge arrow is drawn by the HUD (BobTaskRaceHud.RenderEmergencyArrow) while the station is off-camera.
//   - not in v1: the boss stain (two players, T02 + T03 at once) and the enemy portrait in the card. The HUD banner
//     names the station and the enemy model stands at it instead.
using System;
using System.Collections;
using System.Collections.Generic;
using Bob.Art;
using Bob.Rules;
using UnityEngine;

namespace Bob.TaskRace
{
    [DisallowMultipleComponent]
    public class BobEmergencyStains : MonoBehaviour
    {
        /// <summary>docs 16 section 4: the team has 25 s from the spawn to clear a stain.</summary>
        public const float DefaultGraceSeconds = 25f;

        [Tooltip("docs 16 section 4: seconds from spawn to คราบลาม.")]
        [SerializeField, Min(5f)] float graceSeconds = DefaultGraceSeconds;
        [Tooltip("The enemy stand-off from its station, in world units, so it never hides the station ring.")]
        [SerializeField] float standOff = 1.35f;

        TaskRaceRound round;
        TaskRaceModeManager manager;
        bool listening;

        float elapsed, roundSeconds = 120f;
        int spawned;
        bool planned;
        readonly List<Bob.Rules.BobTaskRaceRules.EmergencyMoment> plan =
            new List<Bob.Rules.BobTaskRaceRules.EmergencyMoment>();
        readonly List<TaskSpot> stations = new List<TaskSpot>();
        /// <summary>Slots already decided (cleared here, cleared by a friend, or spread). An index decides once.</summary>
        readonly HashSet<int> resolved = new HashSet<int>();
        Bob.Ugs.BobOnlineRoom room;
        Enemy current;

        sealed class Enemy
        {
            public int Index;
            public string Name, MinigameId, TaskName, SpotId;
            public int Difficulty;
            public float EndsAt;
            public GameObject Body;
            public float Born;
        }

        /// <summary>Raised on spawn, on clear and on คราบลาม so the HUD can toast one line.</summary>
        public event Action<string> Message;

        public bool Active => current != null;
        public string EnemyName => current != null ? current.Name : "";
        public string TaskName => current != null ? current.TaskName : "";
        public string MinigameId => current != null ? current.MinigameId : "";
        public int Difficulty => current != null ? current.Difficulty : 1;
        public float GraceSeconds => graceSeconds;
        /// <summary>Where the stain stands, so the HUD can draw its screen-edge arrow. False when nothing is up.</summary>
        public bool HasTarget => current != null && current.Body != null;
        public Vector3 TargetPosition => current != null && current.Body != null ? current.Body.transform.position : Vector3.zero;
        /// <summary>Seconds left in the 25 s grace, 0 when no stain is up.</summary>
        public float Remaining => current != null ? Mathf.Max(0f, current.EndsAt - elapsed) : 0f;
        /// <summary>Enemies planned for this round: 2 at D1/D2, 3 at D3, from the shared rules.</summary>
        public int PlannedSpawns => plan.Count;
        public int SpawnedCount => spawned;

        /// <summary>The HUD (or the scene builder in play mode) asks once; the component owns its own object.</summary>
        public static BobEmergencyStains Ensure(TaskRaceRound round, TaskRaceModeManager manager)
        {
            var stains = FindAnyObjectByType<BobEmergencyStains>();
            if (stains == null)
                stains = new GameObject("EmergencyStains").AddComponent<BobEmergencyStains>();
            stains.Bind(round, manager);
            return stains;
        }

        public void Bind(TaskRaceRound value, TaskRaceModeManager modeManager)
        {
            if (round != value || manager != modeManager) Unhook();
            round = value;
            manager = modeManager;
            Hook();
        }

        void Hook()
        {
            if (listening || manager == null) return;
            listening = true;
            manager.MatchStarted += ResetRound;
            manager.MatchEnded += OnMatchEnded;
            // docs 27 section 10.8: in a room every device plans the same slots, so one index is enough to say
            // "slot 1 is gone". Without this the friend's island keeps counting a stain that is already clean here.
            room = Bob.Game.MatchRequest.Has && Bob.Game.MatchRequest.Online ? Bob.Ugs.BobOnlineRoom.Instance : null;
            if (room != null) room.EmergencyResolved += OnRemoteResolved;
        }

        void Unhook()
        {
            if (!listening) return;
            listening = false;
            if (manager != null)
            {
                manager.MatchStarted -= ResetRound;
                manager.MatchEnded -= OnMatchEnded;
            }
            if (room != null) room.EmergencyResolved -= OnRemoteResolved;
            room = null;
        }

        void OnDisable() => Unhook();

        void OnMatchEnded(MatchResult _) => DespawnCurrent();

        /// <summary>Every round (and every story round) starts the schedule over with the new seed.</summary>
        void ResetRound()
        {
            elapsed = 0f;
            spawned = 0;
            planned = false;
            plan.Clear();
            resolved.Clear();
            DespawnCurrent();
            roundSeconds = manager != null ? Mathf.Max(30f, manager.RemainingSeconds) : 120f;
        }

        void Update()
        {
            if (!Application.isPlaying || manager == null) return;
            AnimateBody();
            // The clock is held by the 3-2-1 countdown, the pause card and the end of the match: a stain never spawns
            // on time that has not been played, and its 25 s never runs behind the pause card either.
            if (manager.IsCountingDown || manager.Paused || manager.IsMatchOver || manager.LastResult.HasValue) return;
            if (round == null) return;

            Plan();
            elapsed += Time.deltaTime;
            if (current == null && spawned < plan.Count && elapsed >= plan[spawned].At) Spawn();
            if (current != null && elapsed >= current.EndsAt) Spread();
        }

        // ---------------------------------------------------------------- schedule

        void Plan()
        {
            if (planned) return;
            planned = true;
            stations.Clear();
            foreach (var spot in round.StationsOf(TaskSpot.LocalTeam)) stations.Add(spot);
            if (stations.Count == 0) return;

            // The schedule is the shared rules' own draw (docs 16 section 4): the moments of the reference round scaled
            // to this clock, one station per moment, one weakness per moment. Because it is a pure function of the seed
            // and the station count, a room lists the same slots in the same order and <c>bob.emergency</c> can carry an
            // index (docs 27 section 10.8). Online the pool is all three weaknesses: two devices at different chapter
            // progress must not disagree about which enemy slot 1 is.
            var pool = Online
                ? Bob.Rules.BobTaskRaceRules.EmergencyOnlinePool
                : Bob.Rules.BobTaskRaceRules.EmergencyPool(BobChapters.Saved.Order, RoundNo);
            var drawn = Bob.Rules.BobTaskRaceRules.EmergencyPlan(
                round.CurrentSeed, round.Difficulty, roundSeconds, stations.Count, pool);
            for (int i = 0; i < drawn.Length; i++) plan.Add(drawn[i]);
            if (Application.isPlaying)
                Debug.Log($"[EmergencyStains] seed {round.CurrentSeed} · {plan.Count} stain(s) at " +
                          string.Join(", ", plan.ConvertAll(m =>
                              $"{m.At:0} s {m.MinigameId} @{StationName(m.Station)}")) +
                          (Online ? " · online (bob.emergency)" : ""));
        }

        bool Online => Bob.Game.MatchRequest.Has && Bob.Game.MatchRequest.Online;

        int RoundNo => manager != null && manager.Series != null ? manager.Series.Round : 1;

        string StationName(int index) =>
            index >= 0 && index < stations.Count && stations[index] != null ? stations[index].SpotId : "#" + index;

        // ---------------------------------------------------------------- spawn / clear / spread

        void Spawn()
        {
            int index = spawned++;
            if (resolved.Contains(index)) return; // a friend settled this slot while our clock was still behind
            var moment = plan[index];
            var spot = moment.Station >= 0 && moment.Station < stations.Count ? stations[moment.Station] : null;
            if (spot == null) return;
            var enemy = new Enemy
            {
                Index = index,
                Name = NameOf(moment.MinigameId),
                MinigameId = moment.MinigameId,
                TaskName = spot.TaskName,
                SpotId = spot.SpotId,
                Difficulty = Mathf.Max(1, round.Difficulty - 1),
                EndsAt = moment.At + graceSeconds,
                Born = Time.time,
            };
            enemy.Body = BuildBody(enemy, spot);
            current = enemy;
            BobSfx.Play("enemy_spawn", 0.9f);
            BobSfx.Play("timer_warning", 0.5f);
            Message?.Invoke($"{enemy.Name}โผล่ที่ {enemy.TaskName} · เหลือ {graceSeconds:0} วิ");
            if (Application.isPlaying)
                Debug.Log($"[EmergencyStains] spawn #{index} {enemy.Name} ({enemy.MinigameId} D{enemy.Difficulty}) at " +
                          $"{enemy.SpotId} \"{enemy.TaskName}\" · {graceSeconds:0} s grace.");
        }

        /// <summary>The card was solved in time. Returns false when no stain was up (a late tick).</summary>
        public bool Clear()
        {
            if (current == null) return false;
            return Resolve(current.Index, true, true);
        }

        /// <summary>The 25 s ran out: the stain spreads and one short task joins the team's list.</summary>
        void Spread()
        {
            if (current == null) return;
            Resolve(current.Index, false, true);
        }

        /// <summary>
        /// Settle one slot of this round's schedule, once. <paramref name="cleared"/> is the decision; a slot that was
        /// already settled here or by a friend (docs 27 section 10.8) is ignored, so a late message can never add the
        /// penalty task twice or undo a clean pass. The acting device tells the room first; the receiver applies the
        /// very same slot without broadcasting it again.
        /// </summary>
        bool Resolve(int index, bool cleared, bool local)
        {
            if (index < 0 || index >= plan.Count) return false;
            if (!resolved.Add(index)) return false;

            var gone = current != null && current.Index == index ? current : null;
            if (gone != null) current = null;
            if (gone != null && gone.Body != null) StartCoroutine(ShrinkAway(gone.Body));
            string minigameId = gone != null ? gone.MinigameId : plan[index].MinigameId;
            string name = gone != null ? gone.Name : NameOf(minigameId);
            string spotId = gone != null ? gone.SpotId : StationName(plan[index].Station);

            BobSfx.Play("enemy_defeat", cleared ? 0.9f : 0.7f);
            if (cleared)
            {
                Message?.Invoke(local ? $"ปราบ{name}แล้ว!" : "เพื่อนปราบคราบแล้ว!");
            }
            else
            {
                var spec = Library(minigameId);
                bool added = round != null && round.AddPenaltyTask(spec, TaskSpot.LocalTeam, "คราบลาม");
                Message?.Invoke(added ? "คราบลาม! เพิ่มงานอีก 1 อย่างในใบงาน" : "คราบลาม! แต่ใบงานเต็มแล้ว");
                if (Application.isPlaying)
                    Debug.Log($"[EmergencyStains] {name} at {spotId} was not cleared in {graceSeconds:0} s: " +
                              $"penalty task {(added ? "added" : "refused")}.");
            }
            NoteQuality(cleared);
            if (Application.isPlaying)
                Debug.Log($"[EmergencyStains] {(cleared ? "cleared" : "spread")} slot #{index} {name} at {spotId}" +
                          (local ? " here" : " by a friend") + $" · {resolved.Count}/{plan.Count} settled.");
            if (local) Broadcast(index, cleared);
            return true;
        }

        void Broadcast(int index, bool cleared)
        {
            if (room == null || !room.Connected) return;
            room.SendEmergency(index, cleared, plan.Count);
        }

        /// <summary>A friend settled a slot (docs 27 section 10.8). A plan of a different size means the two devices
        /// built different islands — the message is dropped rather than applied to the wrong station.</summary>
        void OnRemoteResolved(int index, bool cleared, int slots)
        {
            if (slots != plan.Count)
            {
                Debug.LogWarning($"[EmergencyStains] friend's plan has {slots} slot(s), ours has {plan.Count} — ignored.");
                return;
            }
            Resolve(index, cleared, false);
        }

        /// <summary>
        /// docs 16 section 2.5: the team quality star "จัดการฉุกเฉินทันทุกตัว". The chapter series counts every stain of
        /// every round, so the result card can say how the chapter was played without it ever deciding the win.
        /// </summary>
        void NoteQuality(bool cleared)
        {
            var series = manager != null ? manager.Series : null;
            if (series != null) series.NoteEmergency(cleared);
        }

        void DespawnCurrent()
        {
            if (current == null) return;
            if (current.Body != null) Destroy(current.Body);
            current = null;
        }

        static MinigameSpec Library(string id)
        {
            foreach (var spec in MinigameLibrary.All) if (spec.Id == id) return spec;
            return MinigameLibrary.All[0];
        }

        static string NameOf(string id) => id switch
        {
            "T02" => "เจ้าคราบ",
            "T03" => "สไลม์",
            "T05" => "ขยะปีศาจ",
            _ => "คราบ",
        };

        static string PrefabOf(string id) => id switch
        {
            "T02" => "SK_boss_stain",
            "T03" => "SK_enemy_slime_green",
            _ => "SK_enemy_slime_mud",
        };

        // ---------------------------------------------------------------- the body

        GameObject BuildBody(Enemy enemy, TaskSpot spot)
        {
            var root = new GameObject("Emergency_" + enemy.MinigameId);
            root.transform.SetParent(transform, false);
            var at = spot.transform.position;
            var side = spot.transform.TransformDirection(Vector3.right);
            root.transform.position = at + side * standOff;

            var prefab = Resources.Load<GameObject>("Characters/" + PrefabOf(enemy.MinigameId));
            if (prefab == null) return root; // the banner still names the station; no model is not a broken round

            var body = Instantiate(prefab, root.transform, false);
            body.name = prefab.name;
            float yaw = spot.SpotId.EndsWith("_B", StringComparison.Ordinal) ? -150f : 30f; // face the island centre
            body.transform.localRotation = Quaternion.Euler(0f, yaw, 0f) * prefab.transform.localRotation;
            foreach (var r in body.GetComponentsInChildren<Renderer>(true))
            {
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                r.receiveShadows = true;
            }

            var b = Bounds(body);
            if (b.size.y > 0.01f) body.transform.localScale *= 1.15f / b.size.y; // a squat stain, never a second hero
            b = Bounds(body);
            body.transform.position += new Vector3(0f, root.transform.position.y - b.min.y, 0f); // stand on its feet
            return root;
        }

        static Bounds Bounds(GameObject go)
        {
            var renderers = go.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return new Bounds(go.transform.position, Vector3.one * 0.5f);
            var bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            return bounds;
        }

        void AnimateBody()
        {
            if (current == null || current.Body == null) return;
            float pop = Mathf.Clamp01((Time.time - current.Born) / 0.35f);
            float breathe = 1f + 0.035f * Mathf.Sin(Time.time * 3.4f);
            current.Body.transform.localScale = Vector3.one * (0.55f + 0.45f * pop) * breathe;
        }

        IEnumerator ShrinkAway(GameObject body)
        {
            float t = 0f;
            var from = body.transform.localScale;
            while (t < 0.35f && body != null)
            {
                t += Time.deltaTime;
                body.transform.localScale = Vector3.Lerp(from, Vector3.zero, Mathf.Clamp01(t / 0.35f));
                yield return null;
            }
            if (body != null) Destroy(body);
        }

        // ---------------------------------------------------------------- verify hooks

        /// <summary>Verify: bring the next planned stain forward and open its card by hand.</summary>
        public bool DebugSpawnNow()
        {
            if (round == null || manager == null) return false;
            if (!planned) { planned = true; Plan(); }
            if (plan.Count == 0) return false;
            if (spawned >= plan.Count) spawned = plan.Count - 1;
            Spawn();
            return current != null;
        }

        /// <summary>Verify: let the grace run out without waiting 25 s.</summary>
        public void DebugSpreadNow()
        {
            if (current == null) return;
            current.EndsAt = elapsed;
        }
    }
}
