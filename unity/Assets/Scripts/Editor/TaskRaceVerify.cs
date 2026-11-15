// Battle of the Brooms: one-shot check for the Task Race scenes (docs 24).
//   -executeMethod Bob.EditorTools.TaskRaceVerify.Run       (solo)
//   -executeMethod Bob.EditorTools.TaskRaceVerify.RunDuo
//   -executeMethod Bob.EditorTools.TaskRaceVerify.RunTeam
// Reports the map pieces, task spot ids and both modes (barrier obstacles vs bridge links), then renders previews.
using System.IO;
using System.Linq;
using Bob.Rules;
using Bob.TaskRace;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;

namespace Bob.EditorTools
{
    public static class TaskRaceVerify
    {
        public static void Run() => RunVariant(TaskRaceSceneBuilder.ScenePath, "");
        public static void RunDuo() => RunVariant(TaskRaceSceneBuilder.DuoScenePath, "-duo");
        public static void RunTeam() => RunVariant(TaskRaceSceneBuilder.TeamScenePath, "-team");

        static void RunVariant(string scenePath, string suffix)
        {
            EditorSceneManager.OpenScene(scenePath);

            var map = GameObject.Find("TaskRaceMap");
            if (map == null) { Debug.LogError("[TaskRaceVerify] TaskRaceMap missing."); return; }

            var placeholders = map.GetComponentsInChildren<Transform>(true).Count(t => t.name.EndsWith("_Placeholder"));
            var teamA = map.transform.Find("TeamA/Island");
            var teamB = map.transform.Find("TeamB/Island");
            int obstaclesBefore = Object.FindObjectsByType<NavMeshObstacle>(FindObjectsInactive.Include).Length;
            int linksBefore = Object.FindObjectsByType<NavMeshLink>(FindObjectsInactive.Include).Length;
            Debug.Log($"[TaskRaceVerify] map ok: A={Describe(teamA)} B={Describe(teamB)} " +
                      $"river={Describe(map.transform.Find("RiverChannel"))} " +
                      $"cliff={Describe(map.transform.Find("CliffWaterfall"))} placeholders={placeholders}");

            var spots = Object.FindObjectsByType<TaskSpot>(FindObjectsInactive.Exclude)
                .OrderBy(s => s.SpotId).ToArray();
            Debug.Log("[TaskRaceVerify] spots: " + string.Join(", ", spots.Select(s =>
                $"{s.SpotId}({s.TaskId},{s.Category},{s.ExpectedSeconds:0}s)@({s.transform.position.x:0.0},{s.transform.position.z:0.0})")));
            Debug.Log("[TaskRaceVerify] baked tasks: " + string.Join(", ", spots.Select(s =>
                $"{s.SpotId}={s.TaskId} {s.TaskName}({s.ExpectedSeconds:0.#}s)")));

            var round = Object.FindAnyObjectByType<TaskRaceRound>();
            if (round != null)
            {
                var sideA = spots.Where(s => !s.SpotId.EndsWith("_B")).ToArray();
                var cats = sideA.Select(s => s.Category).Distinct().ToList();
                float mean = TaskRaceRound.MeanSpotDistance(sideA);
                for (int d = 1; d <= 3; d++)
                    Debug.Log($"[TaskRaceVerify] round D{d}: players {round.Players} · seed {round.ConfiguredSeed} · " +
                              $"mean station {mean:0.00} u · workload ~" +
                              $"{TaskRaceRound.EstimateWorkload(round.Players, d, round.ConfiguredSeed, cats, mean):0} s · " +
                              TaskRaceRound.DescribePlan(round.Players, d, round.ConfiguredSeed, cats));
            }
            else Debug.LogWarning("[TaskRaceVerify] no TaskRaceRound: the task list is not randomized.");

            var manager = Object.FindAnyObjectByType<TaskRaceModeManager>();
            if (manager == null) { Debug.LogError("[TaskRaceVerify] TaskRaceModeManager missing."); return; }

            GameObject hero = null;
            try { hero = GameObject.FindGameObjectWithTag("Player"); } catch (UnityException) { }
            var farSpot = spots.FirstOrDefault(s => s.SpotId.EndsWith("_B"));

            Physics.SyncTransforms();
            var floors = spots.Select(s =>
            {
                var p = s.transform.position;
                var y = Physics.Raycast(new Vector3(p.x, 50f, p.z), Vector3.down, out var hit, 100f,
                    ~0, QueryTriggerInteraction.Ignore) ? hit.point.y : float.NaN;
                return $"({p.x:0.0},{p.z:0.0})y={y:0.00}";
            });
            Debug.Log("[TaskRaceVerify] floor under spots: " + string.Join(", ", floors));

            var mode = manager.mode;
            manager.SetMode(TaskRaceMode.TimedBridgeRaid);
            var links = Object.FindObjectsByType<NavMeshLink>(FindObjectsInactive.Include)
                .Where(l => l.gameObject.transform.root == map.transform).ToArray();
            Debug.Log($"[TaskRaceVerify] TimedBridgeRaid: river barrier active={manager.riverBarrierGroup.activeSelf} " +
                      $"bridges active={manager.bridgesGroup.activeSelf} gates active={manager.bridgeBarrierGroup?.activeSelf} " +
                      $"barriers={manager.Barriers.Count} links={links.Length} ({linksBefore} before)");
            // Two proofs, because the rule is "shut until 60 s, then cross": with the gates down the carve must cut the
            // deck (PathPartial), with the gates up the same query must reach Team B (PathComplete).
            if (hero != null && farSpot != null)
                CheckNav("TimedBridgeRaid gates shut", hero.transform.position, farSpot.transform.position);
            manager.DebugOpenBridges();
            if (manager.bridgeBarrierGroup != null) manager.bridgeBarrierGroup.SetActive(false);
            if (manager.navMeshSurface != null && manager.raidNavMesh != null)
            {
                manager.navMeshSurface.RemoveData();
                manager.navMeshSurface.navMeshData = manager.raidNavMesh;
                manager.navMeshSurface.AddData();
            }
            if (hero != null && farSpot != null)
                CheckNav("TimedBridgeRaid gates open", hero.transform.position, farSpot.transform.position);
            if (manager.bridgesGroup != null)
                foreach (Transform bridge in manager.bridgesGroup.transform)
                    ProbeBridge(bridge);
            if (manager.bridgeBarrierGroup != null) manager.bridgeBarrierGroup.SetActive(true);

            manager.SetMode(TaskRaceMode.PureRace);
            var obstacles = Object.FindObjectsByType<NavMeshObstacle>(FindObjectsInactive.Include)
                .Where(o => o.gameObject.transform.root == map.transform).ToArray();
            Debug.Log($"[TaskRaceVerify] PureRace: river barrier active={manager.riverBarrierGroup.activeSelf} " +
                      $"bridges active={manager.bridgesGroup.activeSelf} obstacles={obstacles.Length} " +
                      $"({obstaclesBefore} before) carving={obstacles.Count(o => o.carving)}");
            if (hero != null && farSpot != null) CheckNav("PureRace", hero.transform.position, farSpot.transform.position);
            manager.SetMode(mode);

            CheckStageRules(spots);
            CheckBot(manager);
            CheckGameplay(manager, spots);

            var hud = Object.FindAnyObjectByType<BobTaskRaceHud>();
            if (hud != null)
            {
                // the saved scene carries the authored canvas; rebuild from a fresh round so the render is honest
                if (round != null) round.StartRound(round.ConfiguredSeed);
                hud.Build(Camera.main);
                hud.SetDebugBarVisible(false); // the shot must show the player's HUD, not the authoring bar
                var touch = hud.GetComponent<BobTaskRaceTouch>();
                var rival = Object.FindAnyObjectByType<BobTaskRaceRival>();
                var bot = Object.FindAnyObjectByType<TaskRaceBotController>();
                Debug.Log($"[TaskRaceVerify] player HUD: {hud.RowCount} task row(s), canvas {hud.UiCanvas.renderMode}, " +
                          $"touch={(touch != null && touch.HasHero ? "joystick + walk" : "missing")}, " +
                          $"opponent={(bot != null ? $"bot {bot.JobName}" : rival != null ? $"rival pace {rival.pace:0.00}" : "missing")}, " +
                          $"the round bar reads {round?.DoneWeight}/{round?.TotalWeight} weight");
            }
            else Debug.LogWarning("[TaskRaceVerify] no BobTaskRaceHud in the scene: the task list is not drawn.");

            Render(suffix, hud);
            RenderS28(suffix, hud, spots, hero);
            RenderResult(suffix, hud, manager, hero);
            RenderChapter(suffix, hud, manager, hero);
            RenderPause(suffix, hud, manager, hero);
            Debug.Log($"[TaskRaceVerify] {scenePath} done.");
        }

        /// <summary>Shoot the S27 result overlay: the score, both task counts and the reward line.</summary>
        static void RenderResult(string suffix, BobTaskRaceHud hud, TaskRaceModeManager manager, GameObject hero)
        {
            if (hud == null || manager == null) return;
            hud.DebugShowResult();
            Debug.Log($"[TaskRaceVerify] result overlay: open={hud.ResultOpen} {hud.ShownResult}");
            var eye = hero != null ? hero.transform.position + new Vector3(0f, 6.2f, -5.2f) : new Vector3(0f, 10.5f, -15f);
            var look = hero != null ? hero.transform.position + new Vector3(0f, 0.72f, 0f) : new Vector3(0f, 0.4f, 0.5f);
            Shot($"Logs/taskrace{suffix}-result.png", eye, look, 40f, hud);
            hud.DebugCloseResult();
            manager.DebugResetMatch();
        }

        /// <summary>
        /// Shoot the A3 chapter card: stars off a composed 2-0 series (one win by the challenge margin), the three star
        /// asks, and the next chapter with its level gate. Rewards stay unpaid, so the stars are never written here.
        /// </summary>
        static void RenderChapter(string suffix, BobTaskRaceHud hud, TaskRaceModeManager manager, GameObject hero)
        {
            if (hud == null || manager == null) return;
            var series = manager.DebugComposeSeries(6, 3, 6, 4);
            hud.DebugShowResult();
            var chapter = BobChapters.Saved;
            var next = BobChapters.Next(chapter);
            int level = BobChapters.LevelFromXp(BobTaskRaceProfile.Xp);
            Debug.Log($"[TaskRaceVerify] chapter card: chapter {chapter.Order} \"{chapter.Title}\" · series {series.Score} " +
                      $"· stars {series.Stars}/3 · next {(next != null ? $"#{next.Order} \"{next.Title}\" LV {next.UnlockLevel}" : "none (end of story)")} " +
                      $"· player LV {level}");
            var eye = hero != null ? hero.transform.position + new Vector3(0f, 6.2f, -5.2f) : new Vector3(0f, 10.5f, -15f);
            var look = hero != null ? hero.transform.position + new Vector3(0f, 0.72f, 0f) : new Vector3(0f, 0.4f, 0.5f);
            Shot($"Logs/taskrace{suffix}-chapter.png", eye, look, 40f, hud);
            hud.DebugCloseResult();
            manager.DebugResetMatch();
        }

        /// <summary>Shoot the pause card (A1) and prove the freeze it asks the manager for: Time.timeScale 0, Paused on.
        /// The clock standing still over real seconds needs Play mode — <see cref="PauseCheck"/> covers that.</summary>
        static void RenderPause(string suffix, BobTaskRaceHud hud, TaskRaceModeManager manager, GameObject hero)
        {
            if (hud == null || manager == null) return;
            bool open = hud.DebugShowPause();
            bool frozen = manager.Paused && Mathf.Approximately(Time.timeScale, 0f);
            Debug.Log($"[TaskRaceVerify] pause card: open={open} paused={manager.Paused} timeScale={Time.timeScale:0.00} " +
                      $"button visible={hud.PauseButtonVisible} (the button hides behind the dim)");
            var eye = hero != null ? hero.transform.position + new Vector3(0f, 6.2f, -5.2f) : new Vector3(0f, 10.5f, -15f);
            var look = hero != null ? hero.transform.position + new Vector3(0f, 0.72f, 0f) : new Vector3(0f, 0.4f, 0.5f);
            Shot($"Logs/taskrace{suffix}-pause.png", eye, look, 40f, hud);
            hud.DebugClosePause();
            Debug.Log($"[TaskRaceVerify] pause closed: open={hud.PauseOpen} paused={manager.Paused} " +
                      $"timeScale={Time.timeScale:0.00} frozen-while-open={frozen}");
            Time.timeScale = 1f; // an aborted resume must never leave the editor frozen
        }

        /// <summary>Open the S28 card on each kind of card task and shoot it: the play area, the step and the bar.</summary>
        static void RenderS28(string suffix, BobTaskRaceHud hud, TaskSpot[] spots, GameObject hero)
        {
            if (hud == null || hero == null) return;
            var spot = spots.FirstOrDefault(s => s.OwnerTeam == TeamId.TeamA && s.HasTask);
            if (spot == null)
            {
                Debug.LogWarning("[TaskRaceVerify] no card task on Team A: the S28 shot is skipped.");
                return;
            }
            var camera = hero.transform.position + new Vector3(0f, 6.2f, -5.2f);
            var look = hero.transform.position + new Vector3(0f, 0.72f, 0f);
            // One shot per interaction kind: scrub, trace, sort, fill, sequence, hold, tap, multi-step glass.
            foreach (var id in new[] { "T02", "T01", "T05", "T11", "T15", "T06", "T10", "T08" })
            {
                var spec = MinigameLibrary.All.FirstOrDefault(g => g.Id == id);
                if (spec.Id != id) continue;
                spot.ResetTask();
                spot.Configure(spot.SpotId, spec.Name, spec.Category, spec.Seconds, id);
                if (!spot.TryOpen("verify", out var why))
                {
                    Debug.LogWarning($"[TaskRaceVerify] S28 {id}: station refused to open ({why}).");
                    continue;
                }
                bool live = hud.DebugOpenCard(spot);
                hud.DebugAdvanceCard(id == "T15" ? 1.5f : 0.9f);
                Debug.Log($"[TaskRaceVerify] S28 card {id}: {spot.SpotId} \"{spot.TaskName}\" " +
                          $"live={live} cardOpen={hud.CardOpen} step=\"{hud.CardStep}\" hint=\"{hud.CardHint}\"");
                Shot($"Logs/taskrace{suffix}-s28-{id.ToLowerInvariant()}.png", camera, look, 40f, hud);
                spot.Close(false);
                hud.DebugCloseCard();
            }
        }

        /// <summary>The StageConfig layer (docs 24 §15.3 Phase 3): the six assets validate, and the three rule hooks the
        /// stage toggles — the two-pass stain gate, the random assignment order and the fragile stun — really fire.</summary>
        static void CheckStageRules(TaskSpot[] spots)
        {
            var problems = BobStages.Validate();
            var saved = BobStages.Saved;
            Debug.Log($"[TaskRaceVerify] stages: {BobStages.Count} asset(s) " +
                      string.Join(" | ", BobStages.All.Select(s => $"#{s.Order} {s.Title} {s.Tag} +{s.PointsPerTask}" +
                          (s.StageBonus > 0 ? $" bonus {s.StageBonus}" : "") + (s.FragileStunSeconds > 0f ? $" stun {s.FragileStunSeconds:0.#}" : "") +
                          (s.TwoPassStains ? " two-pass" : "") + (s.RandomizeSpots ? " random" : ""))) +
                      " · " + (problems.Count > 0 ? "PROBLEMS " + string.Join("; ", problems) : "validate ok") +
                      $" · saved #{saved.Order} \"{saved.Title}\"");

            var spot = spots.FirstOrDefault(s => s.OwnerTeam == TeamId.TeamA);
            var round = Object.FindAnyObjectByType<TaskRaceRound>();
            if (spot == null || round == null) { Debug.LogWarning("[TaskRaceVerify] stage rules: need a Team A spot and the round."); return; }

            // Stage 4: the stain card needs two passes; the first one leaves the station Dirty with no points.
            spot.ResetTask();
            spot.Configure(spot.SpotId, "ถูคราบวน", TaskCategory.Wash, 10f, TaskRaceRound.StainId);
            spot.SetRequiredPasses(2);
            spot.TryOpen("verify", out _); spot.Close(true);
            var afterFirst = spot.State;
            int firstPasses = spot.PassesDone;
            spot.TryOpen("verify", out _); spot.Close(true);
            Debug.Log($"[TaskRaceVerify] stage two-pass {TaskRaceRound.StainId}: first pass -> {afterFirst} (passes {firstPasses}/{spot.RequiredPasses}), " +
                      $"second -> {spot.State}");
            spot.SetRequiredPasses(1);

            // Stage 5: the random order ignores the station category, so at least one task lands on a station of another family.
            round.SetStageRules(true, false);
            round.StartRound(round.ConfiguredSeed);
            int off = 0;
            foreach (var s in spots.Where(s => s.OwnerTeam == TeamId.TeamA && s.HasTask))
            {
                var spec = MinigameLibrary.All.FirstOrDefault(g => g.Id == s.TaskId);
                if (spec.Id != s.TaskId) continue;
                if (spec.Category != s.Category && spec.Category != TaskCategory.Everyone) off++;
            }
            Debug.Log($"[TaskRaceVerify] stage random: {off} Team A station(s) hold a task from another family " +
                      "(0 would mean the category always wins)");
            round.SetStageRules(false, false);
            round.StartRound(round.ConfiguredSeed);

            // Stage 3: a closed-before-finished card stuns the body; the stun is what the manager hands out.
            var body = Object.FindAnyObjectByType<BobTaskRaceHero>();
            if (body == null) { Debug.LogWarning("[TaskRaceVerify] stage stun: no BobTaskRaceHero in the scene."); return; }
            body.Stun(saved.FragileStunSeconds > 0f ? saved.FragileStunSeconds : 1.5f);
            Debug.Log($"[TaskRaceVerify] stage stun: stunned={body.IsStunned} for {body.StunRemaining:0.0}s " +
                      $"(stage {saved.Order} sets {saved.FragileStunSeconds:0.#})");
        }

        /// <summary>The solo opponent: the scene must carry exactly one bot (or none, in the PvP variants) and it must be
        /// able to pick a job and route across the bridge. Only reads state — the walk happens in Play, not here.</summary>
        static void CheckBot(TaskRaceModeManager manager)
        {
            var bots = Object.FindObjectsByType<TaskRaceBotController>(FindObjectsInactive.Exclude);
            if (bots.Length == 0)
            {
                Debug.Log("[TaskRaceVerify] bot: none (PvP variant, the human rival plays Team B)");
                return;
            }
            if (bots.Length > 1) Debug.LogWarning($"[TaskRaceVerify] bot: {bots.Length} controllers in one scene, expected 1.");
            var bot = bots[0];
            var side = bot.transform.position.x > 0f ? TeamId.TeamB : TeamId.TeamA;
            Debug.Log($"[TaskRaceVerify] bot: {bot.JobName} at ({bot.transform.position.x:0.0},{bot.transform.position.z:0.0}) " +
                      $"on {side} · pace {bot.pace:0.00} · shield at {manager.shieldItemCost} pts · " +
                      $"clean {bot.CleanCount}, sabotage {bot.SabotageCount}");
        }

        /// <summary>Drive the rules the scene builder cannot prove by looking: the clock and gate state, the team pool on
        /// clean, the one-time sabotage undoing the defender's count, and the paid shield blocking the next sabotage.</summary>
        static void CheckGameplay(TaskRaceModeManager manager, TaskSpot[] spots)
        {
            var mine = spots.FirstOrDefault(s => s.OwnerTeam == TeamId.TeamA);
            var theirs = spots.FirstOrDefault(s => s.OwnerTeam == TeamId.TeamB);
            if (mine == null || theirs == null) { Debug.LogWarning("[TaskRaceVerify] gameplay: need one spot per team."); return; }

            manager.DebugResetMatch();
            Debug.Log($"[TaskRaceVerify] gameplay start: clock {manager.RemainingSeconds:0}s, bridges open={manager.AreBridgesOpen}, " +
                      $"barriers={manager.Barriers.Count}, points A/B {manager.PointsOf(TeamId.TeamA)}/{manager.PointsOf(TeamId.TeamB)}");

            if (!mine.TryOpen("verify", out var m)) Debug.LogWarning("[TaskRaceVerify] gameplay: own spot refused to open: " + m);
            else mine.Close(true);
            if (!theirs.TryOpen("verify", out m)) Debug.LogWarning("[TaskRaceVerify] gameplay: enemy spot refused to open: " + m);
            else theirs.Close(true);
            var round = Object.FindAnyObjectByType<TaskRaceRound>();
            Debug.Log($"[TaskRaceVerify] gameplay clean: points A/B {manager.PointsOf(TeamId.TeamA)}/{manager.PointsOf(TeamId.TeamB)} " +
                      $"(expected +{manager.pointsPerTaskComplete} each), round done A {round?.DoneCount}/{round?.TaskCount} " +
                      $"B {round?.DoneCountOf(TeamId.TeamB)}/{round?.TaskCount}");

            bool blocked = !theirs.TrySabotage("verify", out var closedMsg);
            Debug.Log($"[TaskRaceVerify] gameplay sabotage while shut: blocked={blocked} msg=\"{closedMsg}\"");
            manager.DebugOpenBridges();
            int defenderBefore = round?.DoneCountOf(TeamId.TeamB) ?? 0;
            bool sabotaged = theirs.TrySabotage("verify", out var sbMsg);
            if (sabotaged) theirs.CompleteSabotage();
            Debug.Log($"[TaskRaceVerify] gameplay sabotage open: opened={sabotaged} msg=\"{sbMsg}\" " +
                      $"state={theirs.State} count={theirs.SabotageCount}/{TaskSpot.MaxSabotagesPerMatch} " +
                      $"defender B done {defenderBefore}->{round?.DoneCountOf(TeamId.TeamB)}/{round?.TaskCount} " +
                      $"(points A/B {manager.PointsOf(TeamId.TeamA)}/{manager.PointsOf(TeamId.TeamB)})");
            // One per station per match: the defender re-cleans the restored task, and the second attempt is refused
            // with the "already sabotaged" message instead of opening the card again.
            theirs.TryOpen("verify", out _);
            theirs.Close(true);
            bool again = theirs.TrySabotage("verify", out var againMsg);
            Debug.Log($"[TaskRaceVerify] gameplay sabotage repeat: opened={again} msg=\"{againMsg}\" " +
                      $"re-clean B done {round?.DoneCountOf(TeamId.TeamB)}/{round?.TaskCount}");

            // The shield costs 150 and a clean awards 100, so the second clean is topped up the way two tasks would.
            mine.TryOpen("verify", out _);
            mine.Close(true);
            if (manager.PointsOf(TeamId.TeamA) < manager.shieldItemCost)
                manager.DebugAddPoints(manager.shieldItemCost - manager.PointsOf(TeamId.TeamA));
            int poolBeforeShield = manager.PointsOf(TeamId.TeamA);
            bool bought = mine.TryBuyShield(out var shieldMsg);
            // Fire the sabotage as the other team: the shield branch only runs for an enemy standing on the clean spot.
            var savedTeam = TaskSpot.LocalTeam;
            TaskSpot.LocalTeam = TeamId.TeamB;
            bool shieldBlocked = !mine.TrySabotage("raid", out var enemyMsg);
            TaskSpot.LocalTeam = savedTeam;
            Debug.Log($"[TaskRaceVerify] gameplay shield: bought={bought} msg=\"{shieldMsg}\" " +
                      $"protected={mine.IsProtected} timer {mine.ShieldRemaining:0}s, pool A {poolBeforeShield}->" +
                      $"{manager.PointsOf(TeamId.TeamA)} (cost {manager.shieldItemCost}), " +
                      $"enemy sabotage blocked={shieldBlocked} msg=\"{enemyMsg}\"");

            // Start from a shut clock so the 65 -> 60 crossing is exercised by this check, not by the earlier manual open.
            manager.DebugResetMatch();
            manager.DebugSetRemaining(65f);
            manager.DebugSetRemaining(60f);
            Debug.Log($"[TaskRaceVerify] gameplay clock: at {manager.RemainingSeconds:0}s bridges open={manager.AreBridgesOpen}, " +
                      $"gates dropped={manager.Barriers.Count > 0 && manager.Barriers.All(b => b.IsOpen)} " +
                      $"({manager.Barriers.Count} barrie(s))");
            manager.DebugResetMatch();
        }

        /// <summary>Prove the mode's bake: the path from the hero to a Team B spot must cross in TimedBridgeRaid and must not in PureRace.</summary>
        static void CheckNav(string label, Vector3 from, Vector3 to)
        {
            var tri = NavMesh.CalculateTriangulation();
            var min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            var max = new Vector3(float.MinValue, float.MinValue, float.MinValue);
            foreach (var v in tri.vertices) { min = Vector3.Min(min, v); max = Vector3.Max(max, v); }
            Debug.Log($"[TaskRaceVerify] navmesh {label}: {tri.indices.Length / 3} tri, " +
                      $"x[{min.x:0.0},{max.x:0.0}] y[{min.y:0.00},{max.y:0.00}] z[{min.z:0.0},{max.z:0.0}]");

            bool aHit = NavMesh.SamplePosition(from, out var a, 4f, NavMesh.AllAreas);
            bool bHit = NavMesh.SamplePosition(to, out var b, 4f, NavMesh.AllAreas);
            if (!aHit || !bHit)
            {
                Debug.LogWarning($"[TaskRaceVerify] nav {label}: no NavMesh within 4 u of the hero ({aHit}) or the Team B spot ({bHit}).");
                return;
            }
            var path = new NavMeshPath();
            bool found = NavMesh.CalculatePath(a.position, b.position, NavMesh.AllAreas, path);
            float length = 0f;
            for (int i = 1; i < path.corners.Length; i++) length += Vector3.Distance(path.corners[i - 1], path.corners[i]);
            Debug.Log($"[TaskRaceVerify] nav {label}: hero -> Team B spot · path={found} status={path.status} " +
                      $"corners={path.corners.Length} length {length:0.0} u " +
                      $"(snap {Vector3.Distance(from, a.position):0.00}/{Vector3.Distance(to, b.position):0.00} u)");
        }

        /// <summary>Where the bridge ends land relative to the baked NavMesh: the join that makes TimedBridgeRaid walkable.</summary>
        static void ProbeBridge(Transform bridge)
        {
            var renderers = bridge.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0) { Probe(bridge.name, bridge.position); return; }
            var b = renderers[0].bounds;
            foreach (var r in renderers) b.Encapsulate(r.bounds);
            var z = b.center.z;
            var deck = bridge.Find("Walkable");
            var col = deck != null ? deck.GetComponent<Collider>() : null;
            string collider = "MISSING";
            if (col != null)
                collider = $"{col.GetType().Name} x[{col.bounds.min.x:0.00},{col.bounds.max.x:0.00}] " +
                           $"y[{col.bounds.min.y:0.00},{col.bounds.max.y:0.00}] " +
                           $"z[{col.bounds.min.z:0.00},{col.bounds.max.z:0.00}]";
            Debug.Log($"[TaskRaceVerify] deck {bridge.name}: active={bridge.gameObject.activeInHierarchy} " +
                      $"render x[{b.min.x:0.00},{b.max.x:0.00}] z[{b.min.z:0.00},{b.max.z:0.00}] collider {collider}");
            Probe(bridge.name + " west end", new Vector3(b.min.x, 0f, z));
            Probe(bridge.name + " centre", new Vector3(b.center.x, 0f, z));
            Probe(bridge.name + " east end", new Vector3(b.max.x, 0f, z));
        }

        static void Probe(string label, Vector3 p)
        {
            bool hit = NavMesh.SamplePosition(p, out var snapped, 8f, NavMesh.AllAreas);
            Debug.Log($"[TaskRaceVerify] probe {label} @({p.x:0.00},{p.z:0.00}): " +
                      (hit ? $"on navmesh, off by {Vector3.Distance(new Vector3(p.x, snapped.position.y, p.z), snapped.position):0.00} u"
                           : "OFF navmesh"));
        }

        static string Describe(Transform t)
        {
            if (t == null) return "missing";
            var bounds = t.GetComponentsInChildren<Renderer>(true).Select(r => r.bounds).ToList();
            if (bounds.Count == 0) return t.name + ":no-renderers";
            var b = bounds[0];
            foreach (var x in bounds.Skip(1)) b.Encapsulate(x);
            return $"{t.name}:size({b.size.x:0.0},{b.size.y:0.0},{b.size.z:0.0})top{b.max.y:0.00}";
        }

        static void Render(string suffix, BobTaskRaceHud hud = null)
        {
            Shot($"Logs/taskrace{suffix}-preview.png", new Vector3(0f, 10.5f, -15f), new Vector3(0f, 0.4f, 0.5f), 42f);
            Shot($"Logs/taskrace{suffix}-wide.png", new Vector3(0f, 16f, -26f), new Vector3(0f, 0.5f, 0f), 50f);

            // The shot the player actually gets: BobTaskRaceCamera's 6.2 up / 5.2 back / 40 degree lens on the hero.
            GameObject hero = null;
            try { hero = GameObject.FindGameObjectWithTag("Player"); } catch (UnityException) { }
            if (hero != null)
                Shot($"Logs/taskrace{suffix}-play.png", hero.transform.position + new Vector3(0f, 6.2f, -5.2f),
                    hero.transform.position + new Vector3(0f, 0.72f, 0f), 40f);
            if (hero != null && hud != null)
                Shot($"Logs/taskrace{suffix}-hud.png", hero.transform.position + new Vector3(0f, 6.2f, -5.2f),
                    hero.transform.position + new Vector3(0f, 0.72f, 0f), 40f, hud);
        }

        /// <summary>BOB_VIEW=2400x1080 renders the phone aspect instead of the 16:9 default (the APK is landscape-locked).</summary>
        static void ViewSize(out int w, out int h)
        {
            w = 1600; h = 900;
            var v = System.Environment.GetEnvironmentVariable("BOB_VIEW");
            if (string.IsNullOrEmpty(v)) return;
            var wh = v.Split('x');
            if (wh.Length == 2 && int.TryParse(wh[0], out var vw) && int.TryParse(wh[1], out var vh) && vw > 0 && vh > 0) { w = vw; h = vh; }
        }

        static void Shot(string file, Vector3 eye, Vector3 look, float fov, BobTaskRaceHud hud = null)
        {
            ViewSize(out var w, out var h);
            var main = Camera.main;
            var go = new GameObject("VerifyCam", typeof(Camera));
            var cam = go.GetComponent<Camera>();
            if (main != null) cam.CopyFrom(main);
            cam.enabled = false;
            cam.transform.position = eye;
            cam.transform.LookAt(look);
            cam.fieldOfView = fov;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.80f, 0.90f, 0.98f);
            cam.aspect = (float)w / h;
            // The shot camera is new, so it needs its own written sky and the same grade the play camera carries.
            go.AddComponent<BobSkyBackdrop>().Fit();
            go.AddComponent<BobPostGrade>();
            QualitySettings.shadowDistance = 45f;

            // A screen-space camera canvas follows its own camera: point it at the shot camera for this render.
            Camera hudWas = null;
            if (hud != null && hud.UiCanvas != null) { hudWas = hud.UiCanvas.worldCamera; hud.UiCanvas.worldCamera = cam; }

            var rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32) { antiAliasing = 2 };
            cam.targetTexture = rt;
            cam.Render();

            if (hud != null && hud.UiCanvas != null) hud.UiCanvas.worldCamera = hudWas;

            RenderTexture.active = rt;
            var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
            tex.Apply();
            Directory.CreateDirectory("Logs");
            var png = tex.EncodeToPNG();
            if (w != 1600 || h != 900)
                file = Path.Combine(Path.GetDirectoryName(file) ?? "Logs",
                    Path.GetFileNameWithoutExtension(file) + "-" + w + "x" + h + Path.GetExtension(file));
            string written = file;
            try { File.WriteAllBytes(Path.GetFullPath(file), png); }
            catch (IOException)
            {
                // An open image viewer locks the old PNG on Windows; write beside it instead of failing the check.
                written = Path.Combine(Path.GetDirectoryName(file) ?? "Logs",
                    Path.GetFileNameWithoutExtension(file) + "-new" + Path.GetExtension(file));
                File.WriteAllBytes(Path.GetFullPath(written), png);
            }
            RenderTexture.active = null;
            cam.targetTexture = null;
            Object.DestroyImmediate(rt);
            Object.DestroyImmediate(tex);
            Object.DestroyImmediate(go);
            Debug.Log($"[TaskRaceVerify] preview written to {written}");
        }
    }
}
