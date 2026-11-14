// Battle of the Brooms: a bot that plays one whole match through the real game code (walk, pick up, place, ability,
// submit, 3 rounds), for the automated check Bob > Run Game Test. It only runs when that menu / command line asks for it.
// Success = the Console shows "GAME TEST PASSED". Screenshots go to <project>/Logs/bob-game-*.png.
using System.Collections;
using System.Linq;
using Bob.Rules;
using TMPro;
using UnityEngine;

namespace Bob.Game
{
    public partial class BobGame
    {
        public const string AutoKey = "Bob.AutoGame", OnlineKey = "Bob.AutoOnline";

        bool OnlineTestRequested()
        {
#if UNITY_EDITOR
            return UnityEditor.SessionState.GetBool(OnlineKey, false);
#else
            return false;
#endif
        }

        bool AutoRequested()
        {
#if UNITY_EDITOR
            return UnityEditor.SessionState.GetBool(AutoKey, false);
#else
            return false;
#endif
        }

        void Shot(string name)
        {
            var dir = System.IO.Path.Combine(Application.dataPath, "../Logs");
            System.IO.Directory.CreateDirectory(dir);
            ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(dir, "bob-game-" + name + ".png"));
        }

        IEnumerator WaitFor(System.Func<bool> cond, float timeout)
        {
            float t = 0; while (!cond() && t < timeout) { t += Time.unscaledDeltaTime; yield return null; }
        }

        IEnumerator WalkTo(Vector2 p, System.Func<bool> arrived)
        {
            target = p; stuck = 0;
            float t0 = Time.unscaledTime;
            yield return WaitFor(() => arrived(), 8f);
            Debug.Log($"[BobGameBot] walked to {p} in {Time.unscaledTime - t0:0.0}s arrived={arrived()} phase={phase} pos={pos} ts={Time.timeScale}");
            if (!arrived()) { Debug.Log("[BobGameBot] walk got stuck, stepping to " + p); pos = p; yield return null; }
        }

        /// <summary>Screenshots of every out-of-match screen (UX spec section 4), taken before the bot plays.</summary>
        IEnumerator MenuTour()
        {
            var steps = new (Phase p, string shot)[] { (Phase.Splash, "m1-splash"), (Phase.Login, "m2-login"), (Phase.Title, "m3-home"), (Phase.Modes, "m4-modes") };
            foreach (var (p, s) in steps) { phase = p; RenderAll(); yield return new WaitForSeconds(0.7f); Shot(s); yield return new WaitForSeconds(0.3f); }
            OpenHeroSelect(true); yield return new WaitForSeconds(0.7f); Shot("m5-heroes"); yield return new WaitForSeconds(0.3f); heroTimerOn = false;
            phase = Phase.Shop; RenderAll(); yield return new WaitForSeconds(0.7f); Shot("m6-shop"); yield return new WaitForSeconds(0.3f);
            AskBuy("sponge"); yield return new WaitForSeconds(0.5f); Shot("m7-confirm"); yield return new WaitForSeconds(0.3f);
            phase = Phase.Title; RenderAll();
        }

        IEnumerator AutoPlay()
        {
            Debug.Log("[BobGameBot] waiting for the server check");
            yield return WaitFor(() => netNote != "กำลังเชื่อมต่อเซิร์ฟเวอร์…", 12f);
            Debug.Log("[BobGameBot] " + netNote);
            if (OnlineTestRequested()) { yield return AutoPlayOnline(); yield break; }
#if UNITY_EDITOR
            if (UnityEditor.SessionState.GetBool("bob.taskbot", false)) { yield return AutoPlayTasks(); yield break; }
#endif
            yield return MenuTour();
            yield return new WaitForSeconds(0.5f);
            Shot("1-title");
            yield return new WaitForSeconds(0.3f);
            StartMatch();
            int serverScored = 0;
            while (true)
            {
                yield return WaitFor(() => phase == Phase.Intro, 15f);
                if (roundNo == 1) { Shot("2-intro"); yield return new WaitForSeconds(0.3f); }
                BeginBuild();
                yield return WaitFor(() => phase == Phase.Build, 6f);
                var plan = BobMatch.AiBest(R.Offer, R, budget, 400, new Mulberry(seed + roundNo), 0.8);
                Debug.Log($"[BobGameBot] round {roundNo} enemies {string.Join(",", R.Enemies)} plan {string.Join(",", plan.Stack)}");
                for (int i = 0; i < plan.Stack.Count; i++)
                {
                    var id = plan.Stack[i];
                    int si = BobRules.IsMethod(id) ? 3 : (int)BobRules.Parts[id].Family;
                    var r = StationRects[si];
                    var stand = new Vector2(r.center.x, si < 2 ? r.yMin - 0.25f : r.yMax + 0.25f);
                    yield return WalkTo(stand, () => near == StationIds[si]);
                    if (roundNo == 1 && i == 0) { yield return new WaitForSeconds(0.4f); Shot("3a-shelf"); yield return new WaitForSeconds(0.2f); }
                    PickUp(id);
                    yield return new WaitForSeconds(0.15f);
                    var side = new Vector2(r.center.x < RoomW / 2 ? CartRect.xMin - 0.3f : CartRect.xMax + 0.3f, CartRect.center.y);
                    yield return WalkTo(side, () => nearCart);
                    Place(0);
                    yield return new WaitForSeconds(0.15f);
                    if (roundNo == 1 && i == 1) { Shot("3-build"); yield return new WaitForSeconds(0.2f); }
                }
                if (charId != "sweep") UseAbility();
                if (BobRules.CountLayers(stack) == 0) Debug.LogError("[BobGameBot] nothing was placed");
                Submit();
                yield return WaitFor(() => phase == Phase.Test, 15f);
                if (last.FromServer) serverScored++;
                Debug.Log($"[BobGameBot] round {roundNo}: us {last.Mine.Total} ({(last.FromServer ? "server" : "local")}) vs rival {last.Rival.Score.Total} -> {(last.Win ? "win" : "lose")}");
                if (roundNo == 1) { yield return new WaitForSeconds(0.8f); Shot("4a-split"); yield return new WaitForSeconds(SplitTime); Shot("4-test"); yield return new WaitForSeconds(0.3f); }
                bool over = MatchOver;
                AfterTest();
                if (over) break;
            }
            yield return WaitFor(() => phase == Phase.Result, 5f);
            yield return new WaitForSeconds(0.4f); Shot("5-result"); yield return new WaitForSeconds(0.5f);
            bool ok = phase == Phase.Result && (!online || serverScored == roundNo);
            Debug.Log(ok ? $"GAME TEST PASSED: {wins}:{losses} in {roundNo} rounds, {serverScored} scored by the server, online={online}"
                         : $"GAME TEST FAILED: phase {phase}, server scored {serverScored}/{roundNo}, online={online}");
        }

        /// <summary>Task Race bot: walks to every task point, opens the card, plays the minigame with a scripted pointer, and expects the win screen.</summary>
        IEnumerator AutoPlayTasks()
        {
            mode = "tasks"; PressPlay();
            yield return new WaitForSeconds(0.6f);
            int shots = 0;
            while (taskMode && !taskEnded)
            {
                var next = taskList.FirstOrDefault(x => !x.Done);
                if (next == null) break;
                var at = TaskPos(next.Point);
                yield return WalkTo(at + new Vector2(0, -0.2f), () => taskNear == next);
                if (taskNear != next) { Debug.Log("GAME TEST FAILED: task point not reached " + next.Point); yield break; }
                OpenNearTask(); yield return new WaitForSeconds(0.4f);
                if (shots++ < 3) { Shot("task-" + next.Kind); yield return new WaitForSeconds(0.2f); }
                botPtr = true; botDown = false; botPos = new Vector2(-9999, -9999);
                if (next.Kind == "T01")
                {
                    botDown = true;
                    foreach (var p in miniParts.Where(x => x != null && x.name == "Dust").Select(x => x.anchoredPosition).ToList()) { botPos = p; yield return null; yield return null; }
                }
                else if (next.Kind == "T05")
                {
                    foreach (var d in miniDrags.ToList())
                    {
                        botPos = d.from; botPressed = true; botDown = true; yield return null; yield return null;
                        botPos = d.to; yield return null; botDown = false; botReleased = true; yield return null; yield return null;
                    }
                }
                else
                {
                    var spray = miniArea.GetComponentInChildren<UnityEngine.UI.Button>(); spray.onClick.Invoke(); yield return null;
                    botDown = true;
                    for (float y = -260; y <= 260; y += 60) for (float x = -520; x <= 640; x += 50) { botPos = new Vector2(x, y); yield return null; if (miniCard == null) break; }
                }
                botDown = false; botPtr = false; yield return new WaitForSeconds(0.4f);
            }
            yield return new WaitForSeconds(0.5f); Shot("task-end");
            int done = taskList.Count(x => x.Done);
            Debug.Log(taskEnded && done == taskList.Count ? $"GAME TEST PASSED: task race {done}/{taskList.Count} in {Mathf.RoundToInt(TaskRoundTime - taskTime)} s" : $"GAME TEST FAILED: task race {done}/{taskList.Count}");
        }
    }
}
