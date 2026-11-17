// Battle of the Brooms: the twin-island Task Race map, one layout with two rules (docs 24). Left island = Team A,
// right island = Team B, the river channel between them. The prop/station layout is identical on both sides and only
// translated, never mirrored, so the mobile HUD (joystick bottom-left, action bottom-right, task list top-left) hides
// the same corners on both islands and never covers a task spot.
//
//   PureRace        the red-white gates stay shut for the whole 120 s match: each team keeps to its island.
//   TimedBridgeRaid the gates shake at 65 s remaining and drop 1.5 u at 60 s, the deck NavMeshLinks switch on, and the
//                   teams may cross to sabotage the opponent's clean stations.
//
// The match clock, the shared team point pools (100 per completed task, 150 for a 30 s shield) and the atomic shield
// purchase live here; the spots own the reach rules. The manager also owns the S28 camera focus (open -> zoom + shift
// right, close -> back) and the "joystick past 70% closes the card" rule.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Bob.Rules;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;

namespace Bob.TaskRace
{
    public enum TaskRaceMode { PureRace, TimedBridgeRaid }

    [DisallowMultipleComponent, DefaultExecutionOrder(-50)]
    public class TaskRaceModeManager : MonoBehaviour
    {
        [Header("Map groups (built by Tools > BOB > Task Race Map Assembler)")]
        [Tooltip("River bank colliders, fence and hedge props with NavMeshObstacle (carve). Active in PureRace only.")]
        public GameObject riverBarrierGroup;
        [Tooltip("The single footbridge deck and its NavMeshLink. Active in TimedBridgeRaid only.")]
        public GameObject bridgesGroup;
        [Tooltip("The red-white gates. Always active: they block until the TimedBridgeRaid clock reaches 60 s.")]
        public GameObject bridgeBarrierGroup;

        [Header("NavMesh (baked by the scene builder, one per mode)")]
        [Tooltip("The surface on TaskRaceMap. Swapped between the two bakes when the mode changes.")]
        public NavMeshSurface navMeshSurface;
        [Tooltip("Bake with the bank barrier on and the bridges off: two separate islands.")]
        public NavMeshData pureNavMesh;
        [Tooltip("Bake with the bridges on: the two islands are linked.")]
        public NavMeshData raidNavMesh;

        [Header("Mode")]
        public TaskRaceMode mode = TaskRaceMode.TimedBridgeRaid;
        [Tooltip("Which island the local player defends. Team A by default (the left island).")]
        public TeamId localTeam = TeamId.TeamA;

        [Header("Match clock (docs 24 section 12)")]
        [Tooltip("A PvP match lasts 120 s. The clock runs in both modes.")]
        [SerializeField, Min(10f)] public float matchDuration = 120f;
        [Tooltip("Seconds remaining when the 5 s warning starts (barrier shake + HUD warning).")]
        [SerializeField, Min(1f)] public float warningAtRemaining = 65f;
        [Tooltip("Seconds remaining when the gates drop and the bridges open.")]
        [SerializeField, Min(1f)] public float openAtRemaining = 60f;
        [Tooltip("How far the red-white gates drop on Y when the bridges open.")]
        [SerializeField, Min(0.1f)] public float barrierDropHeight = 1.5f;
        [Tooltip("Seconds for the drop.")]
        [SerializeField, Min(0.05f)] public float barrierDropSeconds = 0.6f;
        [Tooltip("Length of the warning shake before the drop.")]
        [SerializeField, Min(0f)] public float warningShakeSeconds = 5f;

        [Header("Story series (docs 24 section 15.3 P3, GDD 9)")]
        [Tooltip("On: a chapter is three rounds, best of three (100 s each). Off: the 120 s single PvP match.")]
        [SerializeField] public bool storySeries = true;
        [Tooltip("Rounds needed to take the chapter.")]
        [SerializeField, Min(1)] public int seriesWinsToTake = 2;
        [Tooltip("Clock of one story round.")]
        [SerializeField, Min(10f)] public float storyRoundSeconds = 100f;
        [Tooltip("Extra seconds for the round after two losses in a row.")]
        [SerializeField, Min(0f)] public float loseStreakBonusSeconds = 15f;
        [Tooltip("Pause between the round card and the next round.")]
        [SerializeField, Min(0.5f)] public float nextRoundDelay = 3f;

        [Header("Stage (StageConfig, docs 24 §15.3 Phase 3)")]
        [Tooltip("Follows the chapter pick by order and is filled from Resources/Stages in Awake.")]
        public StageConfig stage;
        /// <summary>Paid to the winning team when the stage closes (stage 6 = 200).</summary>
        public int StageBonus => stage != null ? stage.StageBonus : 0;

        [Header("Countdown (A2, before the clock runs)")]
        [Tooltip("3-2-1-ลุย! with the clock held at its start value. Off: the clock runs from the first frame.")]
        [SerializeField] public bool countdownBeforeMatch = true;

        [Header("Team points (shared per team)")]
        [Tooltip("Awarded on every Dirty -> Clean completion, including a re-clean after a sabotage.")]
        [SerializeField, Min(0)] public int pointsPerTaskComplete = 100;
        [Tooltip("Cost of the timed station shield.")]
        [SerializeField, Min(0)] public int shieldItemCost = 150;
        [Tooltip("Length of the purchased shield.")]
        [SerializeField, Min(1f)] public float shieldSeconds = 30f;
        [SerializeField] int teamAPoints, teamBPoints;

        [Header("Camera")]
        [Tooltip("Filled automatically from the scene when empty.")]
        public BobTaskRaceCamera cameraRig;

        [Header("Joystick")]
        [Tooltip("The HUD joystick as a vector (length 1 = full tilt). Empty = keyboard axes for desktop testing.")]
        public Func<Vector2> joystickInput;
        [Tooltip("S28: joystick magnitude that closes an open minigame card immediately.")]
        [Range(0.3f, 1f)] public float cancelThreshold = 0.7f;

        [Header("Layout (optional, for inspection)")]
        public Transform teamA, teamB;

        static TaskRaceModeManager cached;

        /// <summary>The scene's manager. Edit-mode verify and the debug bar reach it without Awake running.</summary>
        public static TaskRaceModeManager Instance
        {
            get
            {
                if (cached == null) cached = FindAnyObjectByType<TaskRaceModeManager>();
                return cached;
            }
        }

        public static event Action<TaskRaceMode> ModeChanged;

        /// <summary>Raised after a pool changes: (team, new total). The HUD polls PointsOf as well.</summary>
        public event Action<TeamId, int> PointsChanged;
        /// <summary>65 s remaining: the barriers start to shake. The HUD shows the warning toast.</summary>
        public event Action BridgesWillOpen;
        /// <summary>The gates have dropped and the bridges are walkable.</summary>
        public event Action BridgesOpened;
        /// <summary>Any one-line message the HUD should toast.</summary>
        public event Action<string> ToastRequested;
        /// <summary>The match is on: the clock restarted, the score is zero and both islands are fresh.</summary>
        public event Action MatchStarted;
        /// <summary>The clock ran out or one team finished its list. Raised once per match, rewards already paid.</summary>
        public event Action<MatchResult> MatchEnded;
        /// <summary>A story round closed and another one follows. Not raised on the round that ends the chapter.</summary>
        public event Action<SeriesRound> RoundEnded;
        /// <summary>The 3-2-1-ลุย! countdown is over: the match clock is free to run.</summary>
        public event Action CountdownFinished;

        readonly List<BobBridgeBarrier> barriers = new List<BobBridgeBarrier>();
        TaskRaceRound round;
        StorySeries series;
        BobRaceCountdown countdown;
        bool warned, opened, cancelArmed = true, roundEnding, countingDown;

        /// <summary>Seconds left on the match clock. The top HUD bar reads this.</summary>
        public float RemainingSeconds { get; private set; }
        public bool IsMatchOver => RemainingSeconds <= 0f;
        /// <summary>A2: true while 3-2-1-ลุย! is on screen. The clock is held until it ends.</summary>
        public bool IsCountingDown => countingDown;
        /// <summary>Length of the countdown in seconds (2.7 s), for HUD/debug copy.</summary>
        public static float CountdownSeconds => BobRaceCountdown.TotalSeconds;
        /// <summary>True once the gates have dropped: the only time sabotage is available.</summary>
        public bool AreBridgesOpen => opened;
        public bool BridgeRaid => mode == TaskRaceMode.TimedBridgeRaid;
        public int TeamAPoints => teamAPoints;
        public int TeamBPoints => teamBPoints;
        public IReadOnlyList<BobBridgeBarrier> Barriers => barriers;
        /// <summary>A player holds a TASK_LOCK and the S28 card is up: the hero stands still.</summary>
        public bool CardOpen { get; private set; }
        /// <summary>The match is held by the pause card: the clock, the bot and the hero all stand still.</summary>
        public bool Paused { get; private set; }
        /// <summary>Pause is refused while a card holds a TASK_LOCK or after the result is decided.</summary>
        public bool CanPause => !Paused && !CardOpen && !countingDown && !LastResult.HasValue;
        /// <summary>Set once the match is decided; the HUD draws the result overlay from it.</summary>
        public MatchResult? LastResult { get; private set; }
        /// <summary>The leaderboard score of the match just decided (BobRaceScore): 0 before EndMatch and after a reset.</summary>
        public int LastScore { get; private set; }
        /// <summary>False when the last EndMatch only composed the result (the editor verify shot): nothing is saved.</summary>
        public bool LastMatchRecorded { get; private set; }
        /// <summary>The chapter series while storySeries is on; null in a PvP match.</summary>
        public StorySeries Series => series;
        /// <summary>A story round is running and the chapter is still open.</summary>
        public bool InSeries => series != null && !series.Over;
        /// <summary>The round that just closed (story series only).</summary>
        public SeriesRound? LastRound => series?.LastRound;
        public TeamId OtherTeam => localTeam == TeamId.TeamB ? TeamId.TeamA : TeamId.TeamB;

        /// <summary>Device-local count of chapters lost in a row: two of them buy the next attempt 15 s (GDD 9).</summary>
        public const string LossStreakKey = "bob.story.lossStreak";

        public static int StoryLossStreak
        {
            get => PlayerPrefs.GetInt(LossStreakKey, 0);
            set { PlayerPrefs.SetInt(LossStreakKey, Mathf.Max(0, value)); PlayerPrefs.Save(); }
        }

        /// <summary>A story round is 100 s, plus the mercy seconds once two chapters were lost in a row.</summary>
        public float StorySecondsForRound =>
            storyRoundSeconds + (StoryLossStreak >= 2 ? loseStreakBonusSeconds : 0f);

        public int PointsOf(TeamId team) =>
            team == TeamId.TeamB ? teamBPoints : team == TeamId.TeamA ? teamAPoints : 0;

        public int LocalPoints => PointsOf(localTeam);
        public int RivalPoints => PointsOf(OtherTeam);

        void Awake()
        {
            cached = this;
            EnsureEventSystem();
            ApplyMatchRequest();
            ApplyChapter();
            ApplyStage();
            RemainingSeconds = matchDuration;
            if (cameraRig == null) cameraRig = FindFirstObjectByType<BobTaskRaceCamera>();
            CollectBarriers();
            DioramaAmbient.Ensure(gameObject);
            // The finished match posts its combined score to the leaderboard (docs 27); offline it stays local.
            Bob.Ugs.BobRaceScoreReporter.Attach(this);
        }

        /// <summary>
        /// The Task Race scenes carry no EventSystem (the lobby builds its own inside the Game scene, and scene loads
        /// destroy it), so every uGUI pointer event here — joystick, tap-to-walk, the card, ping, the mate tray — would
        /// be dead in a build and in a direct Play of the scene. One is created if the scene has none, with the legacy
        /// StandaloneInputModule, because Project Settings pins activeInputHandler to 0 (the new Input System package is
        /// not in the manifest, so the newer module would throw).
        /// </summary>
        static void EnsureEventSystem()
        {
            if (UnityEngine.EventSystems.EventSystem.current != null) return;
            if (UnityEngine.Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() != null) return;
            var go = new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem),
                typeof(UnityEngine.EventSystems.StandaloneInputModule));
            if (Application.isPlaying) UnityEngine.Object.DontDestroyOnLoad(go);
        }

        /// <summary>
        /// The lobby's pick (docs 11 S11 -> S14): a story chapter is three rounds and a lone hero, while a Solo/Duo/Team
        /// pick is one timed round and Team may carry offline teammates. Runs before ApplyChapter, so the chapter's
        /// difficulty lands on the team size the player chose. A direct play of this scene carries no request and keeps
        /// exactly what the scene was built with.
        /// </summary>
        void ApplyMatchRequest()
        {
            if (!Bob.Game.MatchRequest.Has) return;
            storySeries = Bob.Game.MatchRequest.Series;
            var list = FindFirstObjectByType<TaskRaceRound>();
            if (list != null)
            {
                // An online match draws the host's seed on every device (docs 27); an offline pick keeps the scene's own.
                int seed = Bob.Game.MatchRequest.Seed > 0 ? Bob.Game.MatchRequest.Seed : list.ConfiguredSeed;
                list.ConfigureRound(Bob.Game.MatchRequest.Players, list.Difficulty, seed);
                if (Bob.Game.MatchRequest.Seed > 0) list.LockSeed(seed);
            }
            if (Application.isPlaying)
                Debug.Log($"[TaskRace] lobby pick: {Bob.Game.MatchRequest.ModeId} · " +
                          $"{Bob.Game.MatchRequest.Players} player(s)/side · " +
                          $"{(storySeries ? "story series (3 rounds)" : "one round")} · " +
                          $"{Bob.Game.MatchRequest.Helpers.Length} offline teammate(s)" +
                          $"{(Bob.Game.MatchRequest.Online ? $" · online {(Bob.Game.MatchRequest.Host ? "host" : "client")} seed {Bob.Game.MatchRequest.Seed}" : "")}" +
                          $"{(Bob.Game.MatchRequest.Daily ? $" · daily day {Bob.Game.MatchRequest.DayNumber} seed {Bob.Game.MatchRequest.Seed}" : "")}");
        }

        /// <summary>
        /// S13 (docs 24 section 15.3): the chapter picked in the lobby sets the difficulty of this round's list and the
        /// match clock. Runs in Awake, before TaskRaceRound.Start draws the list, so the seeded composition already
        /// follows the chapter. Only these two values are applied; the draw itself stays with BobTaskRaceRules.
        /// </summary>
        void ApplyChapter()
        {
            var chapter = Bob.Rules.BobChapters.Saved;
            matchDuration = Mathf.Max(30f, chapter.RoundTime);
            var list = FindFirstObjectByType<TaskRaceRound>();
            if (list != null) list.ConfigureRound(list.Players, chapter.Difficulty, list.ConfiguredSeed);
            if (Application.isPlaying)
                Debug.Log($"[TaskRace] chapter {chapter.Order} \"{chapter.Title}\" · D{chapter.Difficulty} · {matchDuration:0} s clock");
        }

        /// <summary>
        /// S13 (docs 24 section 15.3 Phase 3): the stage with the same order as the picked chapter sets the clock, the
        /// moment the gates drop, the points per task and the two assignment rules. Runs after ApplyChapter, before
        /// TaskRaceRound.Start draws, so the very first round of the scene already follows the stage.
        /// </summary>
        void ApplyStage() => ApplyStage(BobStages.Saved);

        void ApplyStage(StageConfig s)
        {
            stage = s;
            matchDuration = stage.ClockSeconds; // PvP clock; the story series keeps its own 3 × storyRoundSeconds
            openAtRemaining = stage.BridgeOpenAtRemaining;
            warningAtRemaining = stage.WarningAtRemaining;
            pointsPerTaskComplete = stage.PointsPerTask;
            var list = FindAnyObjectByType<TaskRaceRound>();
            if (list != null) list.SetStageRules(stage.RandomizeSpots, stage.TwoPassStains);
            if (Application.isPlaying)
                Debug.Log($"[TaskRace] stage {stage.Order} \"{stage.Title}\" · {stage.Tag} · {stage.PointsPerTask} pts/task" +
                          $"{(stage.StageBonus > 0 ? $" · bonus {stage.StageBonus}" : "")}" +
                          $"{(stage.FragileStunSeconds > 0 ? $" · fragile {stage.FragileStunSeconds:0.#} s" : "")}" +
                          $"{(stage.TwoPassStains ? " · stains take two passes" : "")}{(stage.RandomizeSpots ? " · stations randomized" : "")}");
        }

        void OnDestroy()
        {
            // Leaving the scene while paused must not hand a frozen Game scene to the lobby.
            if (Paused) { Paused = false; Time.timeScale = 1f; }
            if (round != null) round.RoundWon -= OnRoundWon;
            if (cached == this) cached = null;
        }

        void OnEnable()
        {
            TaskSpot.AnyOpened += OnSpotOpened;
            TaskSpot.AnyClosed += OnSpotClosed;
        }

        void OnDisable()
        {
            TaskSpot.AnyOpened -= OnSpotOpened;
            TaskSpot.AnyClosed -= OnSpotClosed;
        }

        void Start()
        {
            TaskSpot.LocalTeam = localTeam;
            if (cameraRig != null && cameraRig.target != null) TaskSpot.SetLocalPlayer(cameraRig.target);
            else
            {
                try
                {
                    var hero = GameObject.FindGameObjectWithTag("Player");
                    if (hero != null) TaskSpot.SetLocalPlayer(hero.transform);
                }
                catch (UnityException) { }
            }
            if (joystickInput == null)
            {
                var hero = FindAnyObjectByType<BobTaskRaceHero>();
                if (hero != null) joystickInput = () => hero.InputVector;
            }
            round = FindAnyObjectByType<TaskRaceRound>();
            if (round != null) round.RoundWon += OnRoundWon;
            // Team pick: the empty slots of the player's island are offline teammates until docs 27's lobby is wired.
            if (Bob.Game.MatchRequest.Has && Bob.Game.MatchRequest.Helpers.Length > 0)
                BobTaskRaceTeammates.Spawn(Bob.Game.MatchRequest.Helpers, localTeam);
            // Online room (docs 27): presence and the shared station states over Netcode named messages.
            if (Bob.Game.MatchRequest.Has && Bob.Game.MatchRequest.Online)
                BobTaskRaceOnline.Attach();
            ApplyMode();
            BeginMatch();
            if (series != null) BobSeriesBanner.Attach(this);
        }

        /// <summary>The local island finished its whole list: the race is over even with time on the clock.</summary>
        void OnRoundWon(TaskRaceRound finished)
        {
            if (series != null) FinishRound();
            else EndMatch();
        }

        void Update()
        {
            // A2 holds the clock for the 3-2-1-ลุย! countdown and A1 holds it behind the pause card, so no points are
            // ever earned on time that has not started.
            if (Application.isPlaying && !Paused && !IsMatchOver && !roundEnding && !countingDown)
            {
                RemainingSeconds = Mathf.Max(0f, RemainingSeconds - Time.deltaTime);
                TickClock();
                if (IsMatchOver)
                {
                    if (series != null) FinishRound();
                    else EndMatch();
                }
            }

            float tilt = Paused ? 0f : ReadJoystick();
            if (tilt > cancelThreshold && cancelArmed)
            {
                cancelArmed = false;
                TaskSpot.CancelOpenSessions();
            }
            else if (tilt < cancelThreshold * 0.4f) cancelArmed = true;
        }

        // ---------------------------------------------------------------- pause

        /// <summary>
        /// Freeze or resume the whole match. Time.timeScale carries the freeze, so every rule that reads deltaTime —
        /// the clock, the bot, the hero — holds still without each of them knowing about the pause. Refused while a
        /// card owns a TASK_LOCK or after the result is decided, so a card can never tick on a frozen clock.
        /// </summary>
        public bool SetPaused(bool on)
        {
            if (on == Paused) return true;
            if (on && !CanPause) return false;
            Paused = on;
            Time.timeScale = on ? 0f : 1f;
            if (Application.isPlaying)
                Debug.Log(on ? $"[TaskRace] paused at {RemainingSeconds:0.0} s: clock and both teams frozen."
                             : $"[TaskRace] resumed with {RemainingSeconds:0.0} s left.");
            return true;
        }

        public void TogglePause() => SetPaused(!Paused);

        /// <summary>Every new round starts from the player's own island, whatever the last round left behind.</summary>
        void SendHeroHome()
        {
            var body = cameraRig != null && cameraRig.target != null
                ? cameraRig.target.GetComponent<BobTaskRaceHero>()
                : FindAnyObjectByType<BobTaskRaceHero>();
            if (body != null) body.GoHome();
        }

        // ---------------------------------------------------------------- match

        /// <summary>Restart the score and the clock: 120 s PvP, or round one of a three-round story chapter.</summary>
        public void BeginMatch()
        {
            Paused = false;
            Time.timeScale = 1f;
            teamAPoints = 0; teamBPoints = 0;
            roundEnding = false;
            series = storySeries ? new StorySeries(storyRoundSeconds, seriesWinsToTake) : null;
            RemainingSeconds = series != null ? StorySecondsForRound : matchDuration;
            warned = opened = false;
            LastResult = null;
            LastScore = 0;
            TaskSpot.LocalTeam = localTeam;
            CloseBarriers();
            PointsChanged?.Invoke(TeamId.TeamA, teamAPoints);
            PointsChanged?.Invoke(TeamId.TeamB, teamBPoints);
            MatchStarted?.Invoke();
            SendHeroHome();
            if (series != null && StoryLossStreak >= 2)
            {
                ToastRequested?.Invoke($"แพ้ติด 2 บท — รอบแรกได้เวลาเพิ่ม {loseStreakBonusSeconds:0} วิ");
                if (Application.isPlaying) Debug.Log($"[TaskRace] mercy: {StoryLossStreak} chapters lost in a row, round 1 is {RemainingSeconds:0} s.");
            }
            if (Application.isPlaying)
                Debug.Log(series != null
                    ? $"[TaskRace] chapter start: round 1/{StorySeries.Rounds}, {RemainingSeconds:0} s each, {mode}, local team {localTeam}."
                    : $"[TaskRace] match start: {matchDuration:0} s, {mode}, local team {localTeam}.");
            StartCountdown();
        }

        /// <summary>
        /// A2: show 3-2-1-ลุย! and hold the clock until it ends. No-op outside Play mode, so the edit-mode verify shots
        /// and the scene builder never grow a canvas object. The countdown re-runs for every story round and every reset.
        /// </summary>
        public void StartCountdown()
        {
            if (!Application.isPlaying || !countdownBeforeMatch) { countingDown = false; return; }
            if (countdown == null) countdown = BobRaceCountdown.Attach(this);
            countingDown = true;
            countdown.Begin(EndCountdown);
            Debug.Log($"[TaskRace] countdown: 3-2-1-{BobRaceCountdown.Steps[BobRaceCountdown.Steps.Length - 1]} over " +
                      $"{BobRaceCountdown.TotalSeconds:0.##} s, clock held at {RemainingSeconds:0} s.");
        }

        /// <summary>Drop the countdown and release the clock (debug / the match ending underneath it).</summary>
        public void SkipCountdown()
        {
            if (!countingDown) return;
            countdown?.Cancel();
            EndCountdown();
        }

        void EndCountdown()
        {
            if (!countingDown) return;
            countingDown = false;
            CountdownFinished?.Invoke();
            Debug.Log("[TaskRace] countdown done: the clock runs.");
        }

        /// <summary>
        /// Decide and pay the match. Runs when the clock hits zero or when either list is finished; the second call
        /// in the same match is a no-op, so rewards are never paid twice. `record` false composes the result for the
        /// editor's verify shot without touching the wallet. The series flags override the single-round verdict when a
        /// three-round chapter ends 2-1 or 1-1.
        /// </summary>
        public void EndMatch(bool record = true, bool? seriesWon = null, bool? seriesDraw = null)
        {
            if (LastResult.HasValue) return;
            SetPaused(false); // the result overlay has to appear on a running scene
            SkipCountdown(); // a match decided during the countdown must not keep it on screen
            if (round == null) round = FindAnyObjectByType<TaskRaceRound>();

            int total = round != null ? round.TaskCount : 0;
            int tasksA = round?.DoneCountOf(TeamId.TeamA) ?? 0;
            int tasksB = round?.DoneCountOf(TeamId.TeamB) ?? 0;
            int mine = localTeam == TeamId.TeamB ? tasksB : tasksA;
            int theirs = localTeam == TeamId.TeamB ? tasksA : tasksB;
            int localPool = PointsOf(localTeam);
            int rivalPool = PointsOf(OtherTeam);
            // The score's clock term reads the time left, so it is taken before the clock is zeroed below. A chapter
            // counts the locks it passed (0-3, the same thing star 3 asks); a lone round counts its met conditions.
            int secondsLeft = Mathf.Max(0, Mathf.CeilToInt(RemainingSeconds));
            int cartPasses = series != null ? series.CartPasses : round != null ? round.CartPassCount : 0;

            bool localFinished = total > 0 && mine >= total;
            bool rivalFinished = total > 0 && theirs >= total;
            bool draw = seriesDraw ?? (!localFinished && !rivalFinished && localPool == rivalPool);
            bool won = seriesWon ?? (localFinished || (!rivalFinished && !draw && localPool > rivalPool));

            // Stage 6 pays its bonus once, to the winner, before the result (and the wallet) is written.
            if (won && StageBonus > 0)
            {
                AddPoints(localTeam, StageBonus);
                localPool = PointsOf(localTeam);
                ToastRequested?.Invoke($"โบนัสปิดด่าน {StageBonus} แต้ม!");
            }

            RemainingSeconds = 0f;

            // A story chapter pays for everything it cleaned: the three rounds add up, and the first clear of a
            // chapter carries a one-off bonus. A single PvP round keeps its own totals.
            int rewardTasks = series != null ? series.TotalLocalTasks : mine;
            int rewardPoints = series != null ? series.TotalLocalPoints : localPool;
            int bonus = 0;
            if (series != null && series.Over && series.LocalTookSeries)
            {
                var chapter = Bob.Rules.BobChapters.Saved;
                if (!Bob.Rules.BobChapters.Cleared(chapter.Order)) bonus = BobTaskRaceProfile.FirstClearBonus;
            }

            LastResult = BobTaskRaceProfile.Record(won, draw, teamAPoints, teamBPoints, tasksA, tasksB, total,
                rewardPoints, rewardTasks, record, bonus);
            LastMatchRecorded = record;

            // Leaderboard score (docs 16 section 2.6): one number per finished match, from the same facts the phone
            // shows on the result card. A chapter counts its whole series (rewardTasks), a lone round its own list.
            int level = round != null ? round.Difficulty : 1;
            LastScore = Bob.Rules.BobRaceScore.Of(rewardTasks, cartPasses, won, draw, secondsLeft, level);
            if (Application.isPlaying)
                Debug.Log("[TaskRace] score " + Bob.Rules.BobRaceScore.Explain(rewardTasks, cartPasses, won, draw,
                    secondsLeft, level));

            // Daily challenge (docs 31 section 2.2): the server handed out today's seed, so the attempt is spent when the
            // round ends, whatever the result. The store keys it by the day the seed belonged to, never by the phone's
            // clock, so a device with a wrong date cannot farm attempts.
            if (Bob.Game.MatchRequest.Daily)
            {
                int day = Bob.Game.MatchRequest.DayNumber;
                Bob.Game.BobDailyStore.Consume(day);
                if (Application.isPlaying)
                    Debug.Log($"[TaskRace] daily day {day} · {Bob.Game.BobDailyStore.AttemptsLeft(day)} attempt(s) left");
            }

            // A3: a decided story chapter earns 0-3 stars, written before MatchEnded so the card reads the new best.
            if (record && series != null && series.Over)
            {
                var chapter = Bob.Rules.BobChapters.Saved;
                int best = Bob.Rules.BobChapters.SaveStars(chapter.Order, series.Stars);
                if (series.LocalTookSeries) Bob.Rules.BobChapters.MarkCleared(chapter.Order);
                if (Application.isPlaying)
                    Debug.Log($"[TaskRace] chapter {chapter.Order} closed {series.Score}: {series.Stars}/3 stars (best {best}), " +
                              $"{rewardTasks} tasks over {series.RoundsPlayed} round(s), +{bonus} first-clear xp.");
            }

            MatchEnded?.Invoke(LastResult.Value);
            if (Application.isPlaying)
                Debug.Log($"[TaskRace] match end: {LastResult.Value}");
        }

        /// <summary>Play another round from zero: new seed, fresh score and clock, both islands restocked.</summary>
        public void RestartMatch()
        {
            BeginMatch();
            if (round == null) round = FindAnyObjectByType<TaskRaceRound>();
            round?.StartRound(UnityEngine.Random.Range(1, 100000));
            SendHeroHome();
            if (Application.isPlaying) Debug.Log("[TaskRace] restart: score 0, clock reset, gates shut, new round.");
        }

        /// <summary>
        /// Close a story round. Two wins either way (or three rounds played) hands the chapter to EndMatch, which pays
        /// once; otherwise the round banner sits for nextRoundDelay seconds and the next round starts on a fresh list.
        /// </summary>
        void FinishRound()
        {
            if (series == null || series.Over || roundEnding) return;
            roundEnding = true;
            if (round == null) round = FindAnyObjectByType<TaskRaceRound>();

            int tasksA = round?.DoneCountOf(TeamId.TeamA) ?? 0;
            int tasksB = round?.DoneCountOf(TeamId.TeamB) ?? 0;
            int mine = localTeam == TeamId.TeamB ? tasksB : tasksA;
            int theirs = localTeam == TeamId.TeamB ? tasksA : tasksB;
            RemainingSeconds = 0f;

            var line = series.Register(mine, theirs, PointsOf(localTeam), PointsOf(OtherTeam));
            if (Application.isPlaying) Debug.Log($"[TaskRace] {line}");
            if (line.Outcome == RoundOutcome.Win && series.ChallengeOffered)
            {
                ToastRequested?.Invoke("ชนะขาด! เปิดด่านท้าทายของบทนี้แล้ว");
                if (Application.isPlaying) Debug.Log($"[TaskRace] challenge stage offered: win margin {line.LocalTasks - line.RivalTasks} tasks.");
            }
            else
            {
                ToastRequested?.Invoke(series.Over
                    ? (series.LocalTookSeries ? $"ชนะซีรีส์ {series.Score}!" : series.LocalWins == series.RivalWins ? $"เสมอซีรีส์ {series.Score}" : $"แพ้ซีรีส์ {series.Score}")
                    : $"จบรอบ {line.Number}: {(line.Outcome == RoundOutcome.Win ? "เราชนะ" : line.Outcome == RoundOutcome.Loss ? "เราแพ้" : "เสมอ")} {series.Score} · รอบหน้า {series.SecondsForRound:0} วิ");
            }
            RoundEnded?.Invoke(line);

            if (series.Over)
            {
                roundEnding = false;
                // Two chapters lost in a row buy the next attempt 15 s; a win clears the streak for good.
                if (series.LocalTookSeries) StoryLossStreak = 0;
                else if (series.LocalWins < series.RivalWins) StoryLossStreak = StoryLossStreak + 1;
                EndMatch(seriesWon: series.LocalTookSeries, seriesDraw: series.LocalWins == series.RivalWins);
                return;
            }
            StartCoroutine(NextRoundAfter(nextRoundDelay));
        }

        IEnumerator NextRoundAfter(float delay)
        {
            yield return new WaitForSeconds(delay);
            if (series == null || series.Over) { roundEnding = false; yield break; }
            BeginRound(series.SecondsForRound);
        }

        /// <summary>Fresh list, zero points, gates shut, new seed; the clock is this round's series length.</summary>
        void BeginRound(float seconds)
        {
            roundEnding = false;
            teamAPoints = 0; teamBPoints = 0;
            RemainingSeconds = seconds;
            warned = opened = false;
            CloseBarriers();
            PointsChanged?.Invoke(TeamId.TeamA, teamAPoints);
            PointsChanged?.Invoke(TeamId.TeamB, teamBPoints);
            if (round == null) round = FindAnyObjectByType<TaskRaceRound>();
            round?.StartRound(UnityEngine.Random.Range(1, 100000));
            MatchStarted?.Invoke();
            SendHeroHome(); // a round must not open with the hero stranded on the rival island
            ToastRequested?.Invoke($"รอบ {series?.Round ?? 2}/{StorySeries.Rounds} · {seconds:0} วิ");
            if (Application.isPlaying)
                Debug.Log($"[TaskRace] round {(series?.Round ?? 0)}/{StorySeries.Rounds} start: {seconds:0} s, series {series?.Score}.");
            StartCountdown();
        }

        void TickClock()
        {
            if (mode != TaskRaceMode.TimedBridgeRaid) return;

            if (!warned && RemainingSeconds <= warningAtRemaining)
            {
                warned = true;
                ShakeBarriers();
                BridgesWillOpen?.Invoke();
                ToastRequested?.Invoke("สะพานจะเปิดใน 5 วินาที!");
                if (Application.isPlaying)
                    Debug.Log($"[TaskRace] warning at {RemainingSeconds:0.0} s remaining: {barriers.Count} barrier(s) shaking.");
            }

            if (!opened && RemainingSeconds <= openAtRemaining) OpenBridges();
        }

        /// <summary>Drop the gates and switch the bridge NavMeshLinks on. Debug can call this at any time.</summary>
        public void OpenBridges()
        {
            if (opened) return;
            opened = true;
            if (bridgesGroup != null && !bridgesGroup.activeSelf) bridgesGroup.SetActive(true);
            EnsureBridgeLinks();
            foreach (var barrier in barriers) barrier.Open();
            BridgesOpened?.Invoke();
            ToastRequested?.Invoke("สะพานเปิดแล้ว! ข้ามไปป่วนจุดงานคู่แข่งได้!");
            if (Application.isPlaying)
                Debug.Log($"[TaskRace] bridges open at {RemainingSeconds:0.0} s remaining: {barriers.Count} gate(s) lowered " +
                          $"{barrierDropHeight:0.##} u over {barrierDropSeconds:0.##} s.");
        }

        public void CloseBarriers()
        {
            opened = false;
            warned = false;
            foreach (var barrier in barriers) barrier.CloseInstant();
        }

        void ShakeBarriers()
        {
            foreach (var barrier in barriers) barrier.Shake(warningShakeSeconds);
        }

        void CollectBarriers()
        {
            barriers.Clear();
            if (bridgeBarrierGroup != null)
                barriers.AddRange(bridgeBarrierGroup.GetComponentsInChildren<BobBridgeBarrier>(true));
            if (barriers.Count == 0)
                barriers.AddRange(FindObjectsByType<BobBridgeBarrier>(FindObjectsInactive.Include));
            foreach (var barrier in barriers)
                barrier.Configure(barrier.Width, barrierDropHeight, barrierDropSeconds);
        }

        // ---------------------------------------------------------------- points / items

        /// <summary>Every Dirty -> Clean completion runs through here (TaskSpot.Close).</summary>
        public void OnTaskCleaned(TaskSpot spot)
        {
            if (spot == null) return;
            var team = spot.OwnerTeam == TeamId.None ? localTeam : spot.OwnerTeam;
            AddPoints(team, pointsPerTaskComplete);
            if (Application.isPlaying) Debug.Log($"[TaskRace] +{pointsPerTaskComplete} to {team} ({PointsOf(team)}) for {spot.SpotId}.");
        }

        /// <summary>Stage 4: the first pass of a two-pass stain landed; the station stays Dirty until the second.</summary>
        public void OnTaskPass(TaskSpot spot)
        {
            if (spot == null) return;
            ToastRequested?.Invoke($"{spot.TaskName}: รอบแรกเสร็จ ขัดอีกรอบให้สะอาด!");
            if (Application.isPlaying)
                Debug.Log($"[TaskRace] {spot.SpotId} pass {spot.PassesDone}/{spot.RequiredPasses}: still Dirty.");
        }

        public void AddPoints(TeamId team, int amount)
        {
            if (amount == 0) return;
            if (team == TeamId.TeamB) teamBPoints = Mathf.Max(0, teamBPoints + amount);
            else if (team == TeamId.TeamA) teamAPoints = Mathf.Max(0, teamAPoints + amount);
            else return;
            PointsChanged?.Invoke(team, PointsOf(team));
        }

        /// <summary>
        /// Atomic shield purchase: one check-then-deduct step, so two taps in the same frame cannot spend the same
        /// 150 points twice. Returns false without changing anything when the pool, the station or the state is wrong.
        /// </summary>
        public bool TryPurchaseShield(TeamId team, TaskSpot targetSpot)
        {
            if (targetSpot == null) return false;
            if (team == TeamId.None) team = localTeam;
            if (targetSpot.OwnerTeam != team) return false;
            if (targetSpot.State != SpotState.Clean) return false;
            if (targetSpot.IsProtected || targetSpot.SabotageUsed) return false;
            if (PointsOf(team) < shieldItemCost) return false;

            AddPoints(team, -shieldItemCost);
            targetSpot.ApplyShield(shieldSeconds);
            ToastRequested?.Invoke($"ติดเกราะป้องกัน {shieldSeconds:0} วิแล้ว!");
            if (Application.isPlaying)
                Debug.Log($"[TaskRace] {team} bought a {shieldSeconds:0} s shield on {targetSpot.SpotId} for {shieldItemCost}.");
            return true;
        }

        /// <summary>The round drops the defending count by one and puts the same task back on the station.</summary>
        public void OnSpotSabotaged(TaskSpot spot)
        {
            if (spot == null) return;
            if (round == null) round = FindAnyObjectByType<TaskRaceRound>();
            round?.HandleSabotage(spot);
            ToastRequested?.Invoke($"คราบกลับมา! จุดงาน {spot.TaskName} ถูกคู่แข่งป่วน!");
            // docs 24 section 15.29 names ping_pop for this moment; the clip only existed from 16 Nov, so the line was
            // silent until now. It is the same earcon the ping wheel uses, which is how the team learns "someone acted".
            if (Application.isPlaying) Bob.Art.BobSfx.Play("ping_pop", 0.7f);
            if (Application.isPlaying)
                Debug.Log($"[TaskRace] sabotage on {spot.SpotId} ({spot.OwnerTeam}): state Dirty again.");
        }

        // ---------------------------------------------------------------- mode

        public void SetMode(TaskRaceMode next)
        {
            mode = next;
            ApplyMode();
        }

        public void ToggleMode() => SetMode(BridgeRaid ? TaskRaceMode.PureRace : TaskRaceMode.TimedBridgeRaid);

        [ContextMenu("Mode: PureRace")]
        void ModePureRace() => SetMode(TaskRaceMode.PureRace);
        [ContextMenu("Mode: TimedBridgeRaid")]
        void ModeTimedBridgeRaid() => SetMode(TaskRaceMode.TimedBridgeRaid);

        /// <summary>Show the right group, carve/disable the barrier, and make sure the active mode has its components.</summary>
        public void ApplyMode()
        {
            // Awake does not run for scene objects in the editor, so the verify script and the debug bar need the
            // barrier list built here too (it is idempotent: Configure only resizes the existing blockers).
            CollectBarriers();
            bool pure = mode == TaskRaceMode.PureRace;
            if (riverBarrierGroup != null) riverBarrierGroup.SetActive(pure);
            if (bridgesGroup != null) bridgesGroup.SetActive(!pure);
            if (bridgeBarrierGroup != null) bridgeBarrierGroup.SetActive(true);

            if (pure) EnsureBarrierObstacles();
            else EnsureBridgeLinks();
            ApplyNavMesh(pure ? pureNavMesh : raidNavMesh);
            CloseBarriers();

            ModeChanged?.Invoke(mode);
            if (Application.isPlaying)
                Debug.Log($"[TaskRace] mode {mode}: barrier {(pure ? "on" : "off")}, bridges {(pure ? "off" : "on")}, gates closed.");
        }

        // ---------------------------------------------------------------- PureRace: carved bank barrier

        /// <summary>Swap the baked NavMesh to the one that matches the mode (two islands vs two linked islands).</summary>
        void ApplyNavMesh(NavMeshData data)
        {
            if (navMeshSurface == null || data == null || navMeshSurface.navMeshData == data) return;
            navMeshSurface.RemoveData();
            navMeshSurface.navMeshData = data;
            navMeshSurface.AddData();
            if (Application.isPlaying) Debug.Log($"[TaskRace] navmesh -> {data.name}");
        }

        void EnsureBarrierObstacles()
        {
            if (riverBarrierGroup == null) return;
            var colliders = riverBarrierGroup.GetComponentsInChildren<Collider>(true);
            int made = 0;
            foreach (var col in colliders)
            {
                if (col.isTrigger) continue; // trigger banks are for detection, not for blocking
                if (EnsureObstacle(col.gameObject)) made++;
            }
            if (Application.isPlaying && colliders.Length == 0)
                Debug.LogWarning("[TaskRace] RiverBarrier_Group has no colliders; PureRace cannot block the river.");
            if (Application.isPlaying && made > 0)
                Debug.Log($"[TaskRace] {made} barrier collider(s) now carve the NavMesh.");
        }

        static bool EnsureObstacle(GameObject go)
        {
            var obstacle = go.GetComponent<NavMeshObstacle>();
            bool added = obstacle == null;
            if (added) obstacle = go.AddComponent<NavMeshObstacle>();
            obstacle.shape = NavMeshObstacleShape.Box;
            obstacle.carving = true;
            obstacle.carveOnlyStationary = true;

            GetWorldBox(go, out var worldSize, out var worldCentre);
            var scale = go.transform.lossyScale;
            obstacle.size = new Vector3(
                worldSize.x / Mathf.Max(0.0001f, Mathf.Abs(scale.x)),
                worldSize.y / Mathf.Max(0.0001f, Mathf.Abs(scale.y)),
                worldSize.z / Mathf.Max(0.0001f, Mathf.Abs(scale.z)));
            obstacle.center = go.transform.InverseTransformPoint(worldCentre);
            return added;
        }

        // ---------------------------------------------------------------- TimedBridgeRaid: links between the islands

        void EnsureBridgeLinks()
        {
            if (bridgesGroup == null) return;
            foreach (Transform child in bridgesGroup.transform) EnsureBridgeLink(child.gameObject);
        }

        /// <summary>One link per bridge, spanning its longest horizontal axis at the walk surface.</summary>
        static void EnsureBridgeLink(GameObject bridge)
        {
            var walkable = bridge.transform.Find("Walkable");
            GameObject measure = walkable != null ? walkable.gameObject : bridge;
            if (!GetWorldBox(measure, out var size, out var centre)) return;

            bool alongX = size.x >= size.z;
            float half = (alongX ? size.x : size.z) * 0.5f;
            float y = centre.y + size.y * 0.5f + 0.05f;
            var a = alongX ? new Vector3(centre.x - half, y, centre.z) : new Vector3(centre.x, y, centre.z - half);
            var b = alongX ? new Vector3(centre.x + half, y, centre.z) : new Vector3(centre.x, y, centre.z + half);

            var start = Marker(bridge.transform, "LinkStart", a);
            var end = Marker(bridge.transform, "LinkEnd", b);

            var link = bridge.GetComponent<NavMeshLink>();
            if (link == null) link = bridge.AddComponent<NavMeshLink>();
            link.startPoint = bridge.transform.InverseTransformPoint(start.position);
            link.endPoint = bridge.transform.InverseTransformPoint(end.position);
            link.width = 1.6f;
            link.bidirectional = true;
            link.costModifier = 0f;
            link.activated = true;
        }

        static Transform Marker(Transform parent, string name, Vector3 worldPos)
        {
            var t = parent.Find(name);
            if (t == null)
            {
                var go = new GameObject(name);
                go.transform.SetParent(parent, false);
                t = go.transform;
            }
            t.position = worldPos;
            return t;
        }

        /// <summary>Combined collider/renderer bounds in world space; false when there is nothing to measure.</summary>
        static bool GetWorldBox(GameObject go, out Vector3 size, out Vector3 centre)
        {
            size = Vector3.zero; centre = Vector3.zero;
            var col = go.GetComponent<Collider>();
            if (col != null)
            {
                if (col is BoxCollider box)
                {
                    var s = go.transform.lossyScale;
                    size = new Vector3(Mathf.Abs(box.size.x * s.x), Mathf.Abs(box.size.y * s.y), Mathf.Abs(box.size.z * s.z));
                    centre = go.transform.TransformPoint(box.center);
                    return true;
                }
                centre = col.bounds.center; size = col.bounds.size;
                return true;
            }
            var renderers = go.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0) return false;
            var bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            centre = bounds.center; size = bounds.size;
            return true;
        }

        // ---------------------------------------------------------------- input + camera

        float ReadJoystick()
        {
            if (joystickInput != null) return Mathf.Clamp01(joystickInput().magnitude);
            return Mathf.Clamp01(new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")).magnitude);
        }

        void OnSpotOpened(TaskSpot spot)
        {
            CardOpen = true;
            if (cameraRig != null) cameraRig.SetMinigameFocus(true);
        }

        void OnSpotClosed(TaskSpot spot)
        {
            CardOpen = false;
            if (cameraRig != null) cameraRig.SetMinigameFocus(false);

            // Stage 3 "fragile": abandoning a card on your own island costs a short stun.
            if (spot == null || spot.LastCardCompleted || stage == null || stage.FragileStunSeconds <= 0f) return;
            if (spot.OwnerTeam != localTeam || IsMatchOver) return;
            var body = cameraRig != null && cameraRig.target != null
                ? cameraRig.target.GetComponent<BobTaskRaceHero>()
                : FindAnyObjectByType<BobTaskRaceHero>();
            if (body == null) return;
            body.Stun(stage.FragileStunSeconds);
            ToastRequested?.Invoke($"งานผิวบอบบาง! ปิดก่อนเสร็จ หยุด {stage.FragileStunSeconds:0.#} วิ");
            if (Application.isPlaying)
                Debug.Log($"[TaskRace] fragile stun {stage.FragileStunSeconds:0.#} s after closing {spot.SpotId} early.");
        }

        // ---------------------------------------------------------------- debug test bar

        /// <summary>Debug bar: jump the clock. Jumping above the open point shuts the gates again for a repeat test.</summary>
        public void DebugSetRemaining(float seconds)
        {
            RemainingSeconds = Mathf.Clamp(seconds, 0f, matchDuration);
            if (RemainingSeconds > warningAtRemaining) { warned = false; }
            if (RemainingSeconds > openAtRemaining && opened)
            {
                CloseBarriers();
                if (Application.isPlaying) Debug.Log("[TaskRace] debug: clock reset above 60 s, gates closed again.");
            }
            // Update is the only other caller, so the debug bar and the verify script move the clock crossings here too.
            TickClock();
        }

        public void DebugAddTime(float seconds) => DebugSetRemaining(RemainingSeconds + seconds);

        public void DebugOpenBridges() => OpenBridges();

        public void DebugAddPoints(int amount) => AddPoints(localTeam, amount);

        /// <summary>Force a sabotage on the first clean enemy station (bridge state and lock are ignored).</summary>
        public bool DebugSabotageNearest()
        {
            var target = FindObjectsByType<TaskSpot>(FindObjectsInactive.Exclude)
                .Where(s => s.IsEnemySpot && s.State == SpotState.Clean && !s.SabotageUsed)
                .OrderBy(s => s.SpotId)
                .FirstOrDefault();
            if (target == null) return false;
            bool ok = target.DebugForceSabotage();
            if (Application.isPlaying) Debug.Log($"[TaskRace] debug sabotage {(ok ? "ok" : "failed")} on {target.SpotId}.");
            return ok;
        }

        /// <summary>Force a free shield on the nearest clean own station.</summary>
        public bool DebugShieldNearest()
        {
            var target = FindObjectsByType<TaskSpot>(FindObjectsInactive.Exclude)
                .Where(s => !s.IsEnemySpot && s.State == SpotState.Clean && !s.IsProtected)
                .OrderBy(s => s.SpotId)
                .FirstOrDefault();
            if (target == null) return false;
            target.ApplyShield(shieldSeconds);
            ToastRequested?.Invoke($"ติดเกราะป้องกัน {shieldSeconds:0} วิแล้ว!");
            return true;
        }

        public void DebugResetMatch() => RestartMatch();

        /// <summary>
        /// Verify: put a closed series on the manager as task pairs per round, so the A3 chapter card can be composed
        /// outside Play mode (the real series only exists once BeginMatch has run).
        /// </summary>
        public StorySeries DebugComposeSeries(params int[] tasksPerRound)
        {
            series = new StorySeries(storyRoundSeconds, seriesWinsToTake);
            for (int i = 0; i + 1 < tasksPerRound.Length; i += 2)
                series.Register(tasksPerRound[i], tasksPerRound[i + 1], 0, 0);
            return series;
        }

        /// <summary>Debug: play another stage (1-6) without leaving the scene. The pick is not saved to the profile.</summary>
        public void DebugApplyStage(int order)
        {
            ApplyStage(BobStages.Get(order));
            DebugResetMatch();
            ToastRequested?.Invoke($"ดีบัก: ด่าน {stage.Order} \"{stage.Title}\" · {stage.Tag}");
            if (Application.isPlaying) Debug.Log($"[TaskRace] debug stage {stage.Order} \"{stage.Title}\" · {stage.Tag}.");
        }

        /// <summary>Check hook: close the running story round now, as if its clock had run out.</summary>
        public void DebugFinishRound()
        {
            if (series != null) FinishRound();
            else EndMatch(record: false);
        }

        void OnValidate()
        {
            cancelThreshold = Mathf.Clamp(cancelThreshold, 0.3f, 1f);
            matchDuration = Mathf.Max(10f, matchDuration);
            storyRoundSeconds = Mathf.Max(10f, storyRoundSeconds);
            seriesWinsToTake = Mathf.Max(1, seriesWinsToTake);
            nextRoundDelay = Mathf.Max(0.5f, nextRoundDelay);
            loseStreakBonusSeconds = Mathf.Max(0f, loseStreakBonusSeconds);
            openAtRemaining = Mathf.Clamp(openAtRemaining, 1f, matchDuration);
            warningAtRemaining = Mathf.Clamp(warningAtRemaining, openAtRemaining, matchDuration);
            pointsPerTaskComplete = Mathf.Max(0, pointsPerTaskComplete);
            shieldItemCost = Mathf.Max(0, shieldItemCost);
            shieldSeconds = Mathf.Max(1f, shieldSeconds);
        }

        void OnDrawGizmos()
        {
            if (teamA != null)
            {
                Gizmos.color = new Color(0.25f, 0.65f, 0.95f, 0.5f);
                Gizmos.DrawWireCube(teamA.position, new Vector3(8f, 0.2f, 8f));
            }
            if (teamB != null)
            {
                Gizmos.color = new Color(0.95f, 0.45f, 0.35f, 0.5f);
                Gizmos.DrawWireCube(teamB.position, new Vector3(8f, 0.2f, 8f));
            }
        }
    }
}
