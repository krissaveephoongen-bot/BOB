// Battle of the Brooms: the Task Race round — the team task list of docs 16 section 2.2 played on the map of docs 24.
// A round draws its minigames from a seeded shuffle (docs 24 section 14): the cart T04 always opens the list, then
// the chapter composition (2 long + 1 common + N short) fills in, never repeating a minigame inside one round.
// Both islands get the same list in the same order, so the race stays fair.
//
// Stations stay where they are; the round hands each spot the next minigame of its own category and restocks it when
// the task is done, so 3 stations can carry a 6-task solo round and 6 stations a 9-task team round. A spot with no
// task left is idle for the rest of the round (SetTaskActive(false)) instead of showing an empty ring.
using System;
using System.Collections.Generic;
using System.Linq;
using Bob.Game;
using UnityEngine;

namespace Bob.TaskRace
{
    /// <summary>Short / Long / Common of docs 16 section 2.1. The bar weight follows the step count.</summary>
    public enum TaskSpan { Short, Long, Common }

    /// <summary>One cleaning minigame of docs 16 sections 5–6, as the round list sees it.</summary>
    public readonly struct MinigameSpec
    {
        public readonly string Id, Name;
        public readonly TaskCategory Category;
        public readonly TaskSpan Span;
        public readonly int Weight;
        public readonly float Seconds;

        public MinigameSpec(string id, string name, TaskCategory category, TaskSpan span, int weight, float seconds)
        {
            Id = id; Name = name; Category = category; Span = span; Weight = weight; Seconds = seconds;
        }
    }

    public static class MinigameLibrary
    {
        /// <summary>The 15 list minigames (docs 16). T16 is the emergency stain spawn and never enters the list.</summary>
        public static readonly MinigameSpec[] All =
        {
            new MinigameSpec("T01", "กวาดตามทาง", TaskCategory.Sweep, TaskSpan.Short, 1, 5f),
            new MinigameSpec("T02", "ถูคราบวน", TaskCategory.Wash, TaskSpan.Short, 1, 4f),
            new MinigameSpec("T03", "ฉีดให้ตรงจุด", TaskCategory.Spray, TaskSpan.Short, 1, 8f),
            new MinigameSpec("T04", "จัดรถเข็นตามสูตร", TaskCategory.Everyone, TaskSpan.Long, 2, 20f),
            new MinigameSpec("T05", "แยกขยะ", TaskCategory.Sweep, TaskSpan.Short, 1, 6.5f),
            new MinigameSpec("T06", "บิดผ้าถู", TaskCategory.Scrub, TaskSpan.Short, 1, 3.5f),
            new MinigameSpec("T07", "ล้างจานกองสูง", TaskCategory.Wash, TaskSpan.Long, 3, 15f),
            new MinigameSpec("T08", "เช็ดกระจก", TaskCategory.Wash, TaskSpan.Short, 1, 5f),
            new MinigameSpec("T09", "แช่คราบรอ", TaskCategory.Everyone, TaskSpan.Long, 2, 22f),
            new MinigameSpec("T10", "ไล่ฝุ่นใต้โต๊ะ", TaskCategory.Sweep, TaskSpan.Short, 1, 6f),
            new MinigameSpec("T11", "เติมน้ำยาตามระดับ", TaskCategory.Spray, TaskSpan.Short, 1, 4f),
            new MinigameSpec("T12", "จัดโต๊ะเก้าอี้", TaskCategory.Everyone, TaskSpan.Common, 2, 7f),
            new MinigameSpec("T13", "ยกของหนักสองคน", TaskCategory.Everyone, TaskSpan.Common, 4, 4f),
            new MinigameSpec("T14", "ล้างพื้นทั้งแถว", TaskCategory.Scrub, TaskSpan.Long, 3, 11f),
            new MinigameSpec("T15", "จำลำดับเครื่องซักผ้า", TaskCategory.Everyone, TaskSpan.Short, 1, 10f),
        };

        public static MinigameSpec Cart => All[3]; // T04: in every round, docs 16 section 2.2
    }

    /// <summary>One line of the team task list (docs 16 section 2): the planned task and where it sits right now.</summary>
    public sealed class TaskEntry
    {
        internal TaskEntry(MinigameSpec spec) { Spec = spec; }

        public MinigameSpec Spec { get; }
        /// <summary>The station hosting this task right now; null while it waits in the queue or after it is done.</summary>
        public TaskSpot Spot { get; internal set; }
        public bool Done { get; internal set; }
        /// <summary>A player has the S28 card open on this task (TASK_LOCK of docs 16 section 2.1).</summary>
        public bool Locked => Spot != null && Spot.IsOpen;
    }

    [DisallowMultipleComponent]
    public class TaskRaceRound : MonoBehaviour
    {
        /// <summary>Hero walk speed (BobTaskRaceHero); the workload estimate uses it for the walking share.</summary>
        public const float WalkSpeed = 3.2f;
        /// <summary>D2 / D3 stretch every minigame by this much (docs 16: difficulty bands).</summary>
        public const float DifficultyStep = 0.15f;

        [Header("Round (docs 16 section 2.2)")]
        [Tooltip("Team size: 1 -> 6 tasks, 2 -> 7, 3 -> 9, 4 -> 11 (the F29 list composition of docs 16).")]
        [SerializeField, Min(1)] int players = 1;
        [Tooltip("Chapter band: 1 = chapters 1-2, 2 = 3-4, 3 = 5-6. Each step stretches the minigames by 15%.")]
        [SerializeField, Range(1, 3)] int difficulty = 1;
        [Tooltip("Fixed seed for replays and tests; ignored at play time when randomSeedEachPlay is on.")]
        [SerializeField] int seed = 1975;
        [SerializeField] bool randomSeedEachPlay = true;
        [SerializeField] bool autoStart = true;
        [Tooltip("A cleaned station stays Clean (and sabotageable) this long before the next queued task arrives.")]
        [SerializeField, Min(0f)] float restockDelay = 6f;

        public int Players => players;
        public int Difficulty => difficulty;
        public int ConfiguredSeed => seed;

        public int CurrentSeed { get; private set; }
        public int TaskCount { get; private set; }
        public int DoneCount => sideA.done;
        /// <summary>Completed tasks of either island: the defender's count drops back by one after a sabotage.</summary>
        public int DoneCountOf(TeamId team) => team == TeamId.TeamB ? sideB.done : sideA.done;
        public int TotalWeight { get; private set; }
        public int DoneWeight => sideA.doneWeight;
        public bool Won => TaskCount > 0 && sideA.done >= TaskCount;
        /// <summary>Sum of the scaled card times of the list.</summary>
        public float EstimatedCardSeconds { get; private set; }
        /// <summary>Cards plus the walking share (mean station distance per task at hero speed).</summary>
        public float EstimatedSeconds { get; private set; }
        /// <summary>One line for the HUD / log, e.g. "6 tasks (long 2 / common 0 / short 4) · weight 9 · cards 54 s at D1 · workload 63 s".</summary>
        public string Summary { get; private set; } = "";

        /// <summary>The round list in plan order (T04 first). The HUD reads Spot / Done for each row.</summary>
        public IReadOnlyList<TaskEntry> Tasks => tasks;
        /// <summary>The stations of one island in spot-id order (the rival walks the Team B list).</summary>
        public IReadOnlyList<TaskSpot> StationsOf(TeamId team) => team == TeamId.TeamB ? sideB.Spots : sideA.Spots;
        /// <summary>Bumps when a row changes: new round, a task moves to a station, a lock opens or closes, a task is done.</summary>
        public int Version { get; private set; }

        public event Action<TaskRaceRound> RoundWon;

        sealed class Side
        {
            public readonly List<TaskSpot> Spots = new List<TaskSpot>();
            public List<MinigameSpec> Remaining = new List<MinigameSpec>();
            public int done, doneWeight;
        }

        readonly List<MinigameSpec> plan = new List<MinigameSpec>();
        readonly List<TaskEntry> tasks = new List<TaskEntry>();
        readonly Dictionary<string, TaskEntry> entryOf = new Dictionary<string, TaskEntry>();
        readonly Dictionary<TaskSpot, MinigameSpec> assigned = new Dictionary<TaskSpot, MinigameSpec>();
        readonly Dictionary<TaskSpot, MinigameSpec> completedSpec = new Dictionary<TaskSpot, MinigameSpec>();
        readonly Dictionary<TaskSpot, Side> sideOf = new Dictionary<TaskSpot, Side>();
        readonly List<Restock> restocks = new List<Restock>();

        struct Restock
        {
            public TaskSpot Spot;
            public Side Side;
            public float At;
        }
        readonly List<TaskSpot> subscribed = new List<TaskSpot>();
        readonly Side sideA = new Side();
        readonly Side sideB = new Side();
        bool wonNotified;

        void OnDisable() => UnsubscribeAll();

        void SubscribeTo(TaskSpot spot)
        {
            spot.Closed -= HandleClosed;
            spot.Closed += HandleClosed;
            spot.Opened -= HandleOpened;
            spot.Opened += HandleOpened;
            if (!subscribed.Contains(spot)) subscribed.Add(spot);
        }

        void UnsubscribeAll()
        {
            foreach (var spot in subscribed)
            {
                if (spot == null) continue;
                spot.Closed -= HandleClosed;
                spot.Opened -= HandleOpened;
            }
            subscribed.Clear();
        }

        void Start()
        {
            // P0 of docs 24 section 15.3: the chapter picked on S13 sets the band. The serialized value stays the
            // fallback for the scene builder and for tests that configure a round without going through the lobby.
            var chapter = Chapters.Selected;
            if (chapter != null) difficulty = chapter.Difficulty;
            if (autoStart) StartRound(randomSeedEachPlay ? UnityEngine.Random.Range(1, 100000) : seed);
        }

        // ---------------------------------------------------------------- setup (editor / tests)

        /// <summary>Scene builder / tests: set the composition without entering play mode.</summary>
        public void ConfigureRound(int newPlayers, int newDifficulty, int newSeed)
        {
            players = Mathf.Max(1, newPlayers);
            difficulty = Mathf.Clamp(newDifficulty, 1, 3);
            seed = newSeed;
        }

        [ContextMenu("Round: reroll seed and restart")]
        void RerollFromMenu() => StartRound(UnityEngine.Random.Range(1, 100000));

        // ---------------------------------------------------------------- round

        public void StartRound(int newSeed)
        {
            CurrentSeed = newSeed;
            wonNotified = false;
            GatherSides();

            var categories = sideA.Spots.Select(s => s.Category).Distinct().ToList();
            plan.Clear();
            plan.AddRange(PlanRound(players, difficulty, newSeed, categories));
            TaskCount = plan.Count;
            TotalWeight = plan.Sum(g => g.Weight);

            tasks.Clear();
            entryOf.Clear();
            foreach (var spec in plan)
            {
                var entry = new TaskEntry(spec);
                tasks.Add(entry);
                entryOf[spec.Id] = entry;
            }

            foreach (var side in new[] { sideA, sideB })
            {
                side.done = 0;
                side.doneWeight = 0;
                side.Remaining = new List<MinigameSpec>(plan);
            }
            assigned.Clear();
            completedSpec.Clear();
            restocks.Clear();
            foreach (var side in new[] { sideA, sideB })
                foreach (var spot in side.Spots)
                    AssignTo(spot, side);

            EstimatedCardSeconds = plan.Sum(g => ScaleSeconds(g, difficulty));
            float walk = TaskCount * MeanSpotDistance(sideA.Spots) / WalkSpeed;
            EstimatedSeconds = EstimatedCardSeconds + walk;
            Summary = Describe(plan, difficulty) + $" · workload {EstimatedSeconds:0} s";
            Version++;

            var chapter = Chapters.Selected;
            Debug.Log($"[TaskRaceRound] seed {CurrentSeed} · chapter {chapter?.Title ?? "-"} · players {players} · difficulty {difficulty} · {Summary}");
            foreach (var spot in sideA.Spots)
                Debug.Log($"[TaskRaceRound]   {spot.SpotId} {spot.Category}: " +
                          (assigned.TryGetValue(spot, out var g) ? $"{g.Id} {g.Name} ({g.Seconds * (1f + DifficultyStep * (difficulty - 1)):0.#}s)" : "idle"));
        }

        /// <summary>Composition of docs 16 section 2.2 (per team, per round).</summary>
        public static void CompositionFor(int players, out int common, out int longs, out int shorts)
        {
            if (players <= 1) { common = 0; longs = 2; shorts = 4; }
            else if (players == 2) { common = 1; longs = 2; shorts = 4; }
            else if (players == 3) { common = 1; longs = 2; shorts = 6; }
            else { common = 1; longs = 3; shorts = 7; }
        }

        /// <summary>
        /// Same table, but D2/D3 stretch every card by 15–30%: a 3–4 player list would then run past the
        /// 90–100 s target of docs 16 section 2.2, so it sheds one short per difficulty step above D1.
        /// </summary>
        public static void CompositionFor(int players, int difficulty, out int common, out int longs, out int shorts)
        {
            CompositionFor(players, out common, out longs, out shorts);
            if (players >= 3) shorts -= Mathf.Clamp(difficulty - 1, 0, 2);
        }

        /// <summary>
        /// The seeded task list: cart first, then the long task, the common one, the shorts, all shuffled and never
        /// repeated. Only minigames whose category exists on this map (or Everyone) are drawn.
        /// </summary>
        public static List<MinigameSpec> PlanRound(int players, int difficulty, int seed, IList<TaskCategory> stationCategories)
        {
            CompositionFor(players, difficulty, out int common, out int longs, out int shorts);
            int total = common + longs + shorts;
            var rng = new System.Random(seed);

            var pool = MinigameLibrary.All
                .Where(g => g.Category == TaskCategory.Everyone || stationCategories.Contains(g.Category))
                .ToList();
            var list = new List<MinigameSpec> { MinigameLibrary.Cart };

            AddShuffled(longs - 1, g => g.Span == TaskSpan.Long && g.Id != "T04");
            AddShuffled(common, g => g.Span == TaskSpan.Common);
            AddShuffled(shorts, g => g.Span == TaskSpan.Short);
            AddShuffled(total - list.Count, _ => true);                     // top up if a category pool ran dry
            AddShuffled(total - list.Count, _ => true, MinigameLibrary.All); // last resort: borrow from other categories
            return list;

            void AddShuffled(int count, Func<MinigameSpec, bool> filter, MinigameSpec[] source = null)
            {
                var candidates = (source ?? pool.ToArray())
                    .Where(g => filter(g) && !list.Any(x => x.Id == g.Id)).ToList();
                for (int i = candidates.Count - 1; i > 0; i--)
                {
                    int j = rng.Next(i + 1);
                    (candidates[i], candidates[j]) = (candidates[j], candidates[i]);
                }
                foreach (var g in candidates)
                {
                    if (count <= 0) break;
                    list.Add(g); count--;
                }
            }
        }

        public static float ScaleSeconds(MinigameSpec spec, int difficulty) =>
            spec.Seconds * (1f + DifficultyStep * (Mathf.Clamp(difficulty, 1, 3) - 1));

        /// <summary>Editor / verify: the summary line without touching scene objects.</summary>
        public static string DescribePlan(int players, int difficulty, int seed, IEnumerable<TaskCategory> categories)
        {
            var list = PlanRound(players, difficulty, seed, categories.ToList());
            return Describe(list, difficulty) + " · " + string.Join(" ", list.Select(g => g.Id));
        }

        /// <summary>Editor / verify: cards plus the walking share, from the mean station distance of the built map.</summary>
        public static float EstimateWorkload(int players, int difficulty, int seed,
                                            IEnumerable<TaskCategory> categories, float meanDistance)
        {
            var list = PlanRound(players, difficulty, seed, categories.ToList());
            return list.Sum(g => ScaleSeconds(g, difficulty)) + list.Count * meanDistance / WalkSpeed;
        }

        /// <summary>Composition and card time of a plan; the workload (cards + walk) is reported separately.</summary>
        public static string Describe(IReadOnlyCollection<MinigameSpec> list, int difficulty)
        {
            int common = list.Count(g => g.Span == TaskSpan.Common);
            int longs = list.Count(g => g.Span == TaskSpan.Long);
            int shorts = list.Count(g => g.Span == TaskSpan.Short);
            int weight = list.Sum(g => g.Weight);
            float cards = list.Sum(g => ScaleSeconds(g, difficulty));
            return $"{list.Count} tasks (long {longs} / common {common} / short {shorts}) · weight {weight} · " +
                   $"cards {cards:0} s at D{difficulty}";
        }

        // ---------------------------------------------------------------- assignment

        void GatherSides()
        {
            sideA.Spots.Clear();
            sideB.Spots.Clear();
            sideOf.Clear();
            UnsubscribeAll();
            var spots = FindObjectsByType<TaskSpot>(FindObjectsInactive.Exclude)
                .OrderBy(s => s.SpotId, StringComparer.Ordinal);
            foreach (var spot in spots)
            {
                var side = spot.SpotId.EndsWith("_B", StringComparison.Ordinal) ? sideB : sideA;
                side.Spots.Add(spot);
                sideOf[spot] = side;
                SubscribeTo(spot);
            }
        }

        void AssignTo(TaskSpot spot, Side side)
        {
            for (int i = restocks.Count - 1; i >= 0; i--)
                if (restocks[i].Spot == spot) restocks.RemoveAt(i);
            completedSpec.Remove(spot);

            var spec = TakeNext(side, spot.Category);
            if (spec == null)
            {
                assigned.Remove(spot);
                spot.SetTaskActive(false);
                return;
            }
            spot.ResetTask();
            spot.Configure(spot.SpotId, spec.Value.Name, spot.Category, ScaleSeconds(spec.Value, difficulty), spec.Value.Id);
            spot.SetTaskActive(true);
            assigned[spot] = spec.Value;
            if (entryOf.TryGetValue(spec.Value.Id, out var entry)) entry.Spot = spot;
            Version++;
        }

        /// <summary>Next list entry this station can host: its own category first, then an Everyone task, then any.</summary>
        static MinigameSpec? TakeNext(Side side, TaskCategory station)
        {
            int pick = side.Remaining.FindIndex(g => g.Category == station);
            if (pick < 0) pick = side.Remaining.FindIndex(g => g.Category == TaskCategory.Everyone);
            if (pick < 0 && side.Remaining.Count > 0) pick = 0;
            if (pick < 0) return null;
            var spec = side.Remaining[pick];
            side.Remaining.RemoveAt(pick);
            return spec;
        }

        void HandleOpened(TaskSpot spot) => Version++;

        void HandleClosed(TaskSpot spot, bool completed)
        {
            Version++; // the TASK_LOCK is gone even when the card was closed early
            if (!completed) return;
            if (!assigned.TryGetValue(spot, out var spec)) return;
            if (!sideOf.TryGetValue(spot, out var side)) return;

            side.done++;
            side.doneWeight += spec.Weight;
            // The station stays Clean for `restockDelay` seconds (docs 24 section 12): that clean window is what the
            // opponent can sabotage once the bridges are open. `completedSpec` remembers the task so a sabotage can
            // put the very same one back.
            assigned.Remove(spot);
            completedSpec[spot] = spec;
            if (entryOf.TryGetValue(spec.Id, out var entry)) { entry.Done = true; entry.Spot = null; }
            if (side.Remaining.Count > 0)
                restocks.Add(new Restock { Spot = spot, Side = side, At = Time.time + restockDelay });
            Version++;

            if (!wonNotified && Won)
            {
                wonNotified = true;
                Debug.Log($"[TaskRaceRound] won: {sideA.done}/{TaskCount} tasks, weight {sideA.doneWeight}/{TotalWeight}.");
                RoundWon?.Invoke(this);
            }
        }

        /// <summary>
        /// The opponent's sabotage undid this station: drop the team's completed count by one and put the same task
        /// back (Dirty, ring on). The next clean completion scores again, docs 24 section 12.
        /// </summary>
        public void HandleSabotage(TaskSpot spot)
        {
            if (spot == null) return;
            for (int i = restocks.Count - 1; i >= 0; i--)
                if (restocks[i].Spot == spot) restocks.RemoveAt(i);
            if (!completedSpec.TryGetValue(spot, out var spec)) { Version++; return; }
            completedSpec.Remove(spot);
            if (!sideOf.TryGetValue(spot, out var side)) return;

            side.done = Mathf.Max(0, side.done - 1);
            side.doneWeight = Mathf.Max(0, side.doneWeight - spec.Weight);
            assigned[spot] = spec;
            if (entryOf.TryGetValue(spec.Id, out var entry)) { entry.Done = false; entry.Spot = spot; }
            spot.RestoreSabotagedTask();
            Version++;
            Debug.Log($"[TaskRaceRound] sabotage undid {spec.Id} on {spot.SpotId}: " +
                      $"{side.done}/{TaskCount} tasks, weight {side.doneWeight}/{TotalWeight}.");
        }

        void Update()
        {
            if (restocks.Count == 0) return;
            float now = Time.time;
            for (int i = restocks.Count - 1; i >= 0; i--)
            {
                var restock = restocks[i];
                if (restock.Spot == null) { restocks.RemoveAt(i); continue; }
                if (now < restock.At) continue;
                restocks.RemoveAt(i);
                completedSpec.Remove(restock.Spot);
                AssignTo(restock.Spot, restock.Side);
            }
        }

        /// <summary>Mean flat distance between every pair of stations, used for the walking share of the estimate.</summary>
        public static float MeanSpotDistance(IReadOnlyList<TaskSpot> spots)
        {
            if (spots.Count < 2) return 0f;
            float sum = 0f;
            int pairs = 0;
            for (int i = 0; i < spots.Count; i++)
            for (int j = i + 1; j < spots.Count; j++)
            {
                var d = spots[i].transform.position - spots[j].transform.position;
                d.y = 0f;
                sum += d.magnitude;
                pairs++;
            }
            return sum / pairs;
        }
    }
}
