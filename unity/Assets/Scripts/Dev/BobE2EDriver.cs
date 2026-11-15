// Battle of the Brooms: the end-to-end run of the shipped flow (docs 24 section 15.17). It only exists when
// `Bob > Run E2E` arms it: a persistent object that presses the real buttons (Login guest, Home play, the HUD's
// ทำงาน button, กลับเมนู), walks the hero with the same destination the touch input sets, and reads the match state
// from the public API. Every step is logged as `[E2E] …`; the editor harness turns E2E PASSED / E2E FAILED into an
// exit code and restores the wallet it spent.
using System.Collections;
using System.IO;
using System.Linq;
using Bob.TaskRace;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Bob.Dev
{
    public class BobE2EDriver : MonoBehaviour
    {
        public const string ArmKey = "Bob.E2E.Arm";
        const string Log = "[E2E] ";
        const float ShotGap = 0.45f;

        static bool Armed
        {
            get
            {
#if UNITY_EDITOR
                return SessionState.GetBool(ArmKey, false);
#else
                return false;
#endif
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (!Armed || FindAnyObjectByType<BobE2EDriver>() != null) return;
            var go = new GameObject("BobE2EDriver");
            DontDestroyOnLoad(go);
            go.AddComponent<BobE2EDriver>();
        }

        void Start() { StartCoroutine(Run()); }

        IEnumerator Run()
        {
            int coinsBefore = PlayerPrefs.GetInt("bob.coins", 0);
            Debug.Log(Log + "start: splash → login → home → TaskRace → chapter → home");

            // ---------------------------------------------------------------- S02 login
            Button guest = null;
            yield return WaitFor(() => (guest = FindButton("เล่นเลย", false)) != null, 30f);
            if (guest == null) { Fail("S02 never showed the guest button"); yield break; }
            yield return new WaitForSecondsRealtime(0.4f);
            Shot("1-login");
            guest.onClick.Invoke();
            Debug.Log(Log + "pressed \"เล่นเลย (Guest)\"");

            // ---------------------------------------------------------------- S10 home → play
            Button play = null;
            yield return WaitFor(() => (play = FindButton("เล่น", true)) != null, 25f);
            if (play == null) { Fail("S10 never showed the play button"); yield break; }
            yield return new WaitForSecondsRealtime(0.6f);
            Shot("2-home");
            play.onClick.Invoke();
            Debug.Log(Log + "pressed \"เล่น\": loading TaskRace");

            // ---------------------------------------------------------------- TaskRace boots
            yield return WaitFor(() => TaskRaceModeManager.Instance != null, 25f);
            var m = TaskRaceModeManager.Instance;
            if (m == null) { Fail("the TaskRace scene never loaded"); yield break; }
            Debug.Log($"{Log}TaskRace up: stage {m.stage?.Order} \"{m.stage?.Title}\", series={m.storySeries}, " +
                      $"{m.StorySecondsForRound:0} s × {m.seriesWinsToTake} wins, local {m.localTeam}");

            // A2 countdown holds the clock; the match starts when it ends.
            yield return WaitFor(() => m.IsCountingDown, 6f);
            bool countedDown = m.IsCountingDown;
            yield return WaitFor(() => !m.IsCountingDown, 12f);
            if (m.IsCountingDown) { Fail("the countdown never finished"); yield break; }
            yield return new WaitForSecondsRealtime(0.2f);
            float clock0 = m.RemainingSeconds;
            yield return new WaitForSecondsRealtime(1f);
            bool clockRuns = m.RemainingSeconds < clock0 - 0.05f;
            Shot("3-play");
            if (!clockRuns) { Fail($"the clock did not run ({clock0:0.0} → {m.RemainingSeconds:0.0})"); yield break; }
            Debug.Log($"{Log}countdown={countedDown}, clock {clock0:0.0} → {m.RemainingSeconds:0.0} in 1 s");

            // ---------------------------------------------------------------- rounds
            for (int round = 1; round <= StorySeries.Rounds; round++)
            {
                if (m.LastResult.HasValue) break;

                // A round is playable only once the banner has handed over and the 3-2-1 countdown is done: the
                // stations are re-dealt in BeginRound, and FinishRound refuses to close a round mid-handover.
                TaskSpot spot = null;
                yield return WaitFor(() =>
                {
                    spot = PickOwnSpot();
                    return !m.CardOpen && !m.IsCountingDown && m.RemainingSeconds > 1f && spot != null;
                }, 45f);
                if (spot == null) { Fail($"round {round}: the round never became playable"); yield break; }
                Debug.Log($"{Log}round {round}: round is live, clock {m.RemainingSeconds:0}, series {m.Series?.Score}");

                // A round is decided on task counts first, so one card is not enough if the rival is ahead: keep
                // working until we lead (equal tasks fall to the points lead added after every card).
                int cleaned = 0;
                float spent = 0f;
                while (!m.LastResult.HasValue && spent < 70f)
                {
                    var roundObj = FindAnyObjectByType<TaskRaceRound>();
                    if (cleaned > 0 && roundObj != null &&
                        roundObj.DoneCountOf(m.localTeam) >= roundObj.DoneCountOf(OtherTeamOf(m.localTeam))) break;
                    spot = PickOwnSpot();
                    if (spot == null) break;
                    float t0 = Time.unscaledTime;
                    yield return CleanSpot(m, round, spot);
                    if (failed) yield break;
                    spent += Time.unscaledTime - t0;
                    cleaned++;
                    m.DebugAddPoints(400);
                }
                Debug.Log($"{Log}round {round}: {cleaned} station(s) cleaned in {spent:0.0}s, score {m.LocalPoints}");

                // Close the round as its clock would; retry while the previous banner still holds the handover.
                float tried = 0f;
                while (!(m.LastResult.HasValue || (m.Series?.LastRound?.Number ?? 0) >= round) && tried < 15f)
                {
                    m.DebugFinishRound();
                    yield return new WaitForSecondsRealtime(0.5f);
                    tried += 0.5f;
                }
                if (m.Series?.LastRound != null && m.Series.LastRound.Value.Number >= round)
                    Debug.Log($"{Log}round {round} closed: {m.Series.LastRound.Value} · series {m.Series.Score}");
                else Debug.LogWarning($"{Log}round {round} was not closed after {tried:0}s");
            }

            // ---------------------------------------------------------------- chapter card
            yield return WaitFor(() => m.LastResult.HasValue, 25f);
            if (!m.LastResult.HasValue) { Fail("the series never ended"); yield break; }
            Button back = null;
            yield return WaitFor(() => (back = FindButton("กลับเมนู", true)) != null, 15f);
            yield return new WaitForSecondsRealtime(0.8f);
            Shot("7-chapter");
            if (back == null) { Fail("the chapter card has no กลับเมนู"); yield break; }

            var result = m.LastResult.Value;
            var series = m.Series;
            int coinsAfter = PlayerPrefs.GetInt("bob.coins", 0);
            int stars = series != null ? series.Stars : 0;
            bool tookSeries = series != null && series.LocalTookSeries;
            Debug.Log($"{Log}chapter card: {result} · series {series?.Score} · stars {stars}/3 · " +
                      $"coins {coinsBefore} → {coinsAfter}");
            if (coinsAfter <= coinsBefore) { Fail($"the match paid nothing ({coinsBefore} → {coinsAfter})"); yield break; }
            if (tookSeries && stars <= 0) { Fail($"the won chapter saved {stars} stars"); yield break; }

            back.onClick.Invoke();
            Debug.Log(Log + "pressed \"กลับเมนู\"");

            // ---------------------------------------------------------------- back in the lobby
            Button home = null;
            yield return WaitFor(() => (home = FindButton("เล่น", true)) != null, 30f);
            yield return new WaitForSecondsRealtime(0.6f);
            Shot("8-home-return");
            if (home == null) { Fail("the game did not come back to Home"); yield break; }

            Debug.Log($"{Log}E2E PASSED: login → home → TaskRace (stage {m.stage?.Order}) → " +
                      $"{series?.Score} with {stars}/3 stars, +{coinsAfter - coinsBefore} coins → home");
        }

        // ---------------------------------------------------------------- helpers

        bool failed;

        IEnumerator WaitFor(System.Func<bool> done, float timeout)
        {
            float t = 0f;
            while (!done() && t < timeout) { t += Time.unscaledDeltaTime; yield return null; }
        }

        void Fail(string why) { failed = true; Debug.LogError(Log + "E2E FAILED: " + why); }

        static TeamId OtherTeamOf(TeamId team) => team == TeamId.TeamB ? TeamId.TeamA : TeamId.TeamB;

        /// <summary>Nearest station that is our team's and still dirty: what a player would walk to first.</summary>
        static TaskSpot PickOwnSpot()
        {
            var hero = FindAnyObjectByType<BobTaskRaceHero>();
            return FindObjectsByType<TaskSpot>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
                .Where(s => !s.IsEnemySpot && s.HasTask && !s.Done)
                .OrderBy(s => hero != null ? Vector3.Distance(s.transform.position, hero.transform.position) : 0f)
                .FirstOrDefault();
        }

        /// <summary>Walk to one station on the map and clear it with the real HUD button and the card's own solver.</summary>
        IEnumerator CleanSpot(TaskRaceModeManager m, int round, TaskSpot spot)
        {
            var hero = FindAnyObjectByType<BobTaskRaceHero>();
            if (hero == null) { Fail($"round {round}: the scene has no local hero"); yield break; }

            // Aim at the station itself: a standoff point can stop the walk (arriveRadius is 0.4) just outside the
            // spot's own InteractRadius, and then the HUD never offers the work button.
            int points0 = m.LocalPoints;
            hero.SetDestination(spot.transform.position);
            Debug.Log($"{Log}round {round}: walk from {hero.transform.position} to {spot.transform.position} " +
                      $"({spot.SpotId}, r={spot.InteractRadius:0.00})");
            float walked = 0f;
            while (!spot.IsNear && walked < 25f)
            {
                walked += Time.unscaledDeltaTime;
                yield return null;
                if (Mathf.Repeat(walked, 4f) < Time.unscaledDeltaTime)
                    Debug.Log($"{Log}walk {walked:0}s: hero {hero.transform.position} " +
                              $"d={Vector3.Distance(hero.transform.position, spot.transform.position):0.00} " +
                              $"hasDest={hero.HasDestination} moving={hero.IsMoving} frozen={hero.IsFrozen} stunned={hero.IsStunned}");
            }
            if (!spot.IsNear)
            {
                Shot("4-nowalk");
                Fail($"round {round}: the hero never reached {spot.SpotId}");
                yield break;
            }
            Shot("4-near");
            Debug.Log($"{Log}round {round}: near {spot.SpotId} \"{spot.TaskName}\" ({spot.TaskId}) " +
                      $"passes {spot.PassesDone}/{spot.RequiredPasses}");

            // A multi-pass station needs one card per pass; every card is opened with the HUD's own button.
            int guard = 0;
            while (!spot.Done && guard++ < 10)
            {
                Button work = null;
                yield return WaitFor(() => (work = FindButton("ทำงาน", false)) != null, 6f);
                if (work == null) { Debug.LogWarning(Log + "no ทำงาน button yet, retrying"); yield return null; continue; }
                work.onClick.Invoke();
                yield return WaitFor(() => m.CardOpen, 8f);
                if (!m.CardOpen) { Debug.LogWarning(Log + "the card did not stay open, retrying"); continue; }
                yield return new WaitForSecondsRealtime(0.4f);
                Shot("5-card");
                int solve = 0;
                while (m.CardOpen && solve++ < 14)
                {
                    var host = FindObjectsByType<BobMinigameHost>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                        .FirstOrDefault();
                    host?.DebugSolve();
                    yield return new WaitForSecondsRealtime(0.3f);
                }
            }
            yield return WaitFor(() => !m.CardOpen, 6f);
            Shot("6-clean");
            if (!spot.Done)
            {
                Fail($"round {round}: {spot.SpotId} never finished ({spot.PassesDone}/{spot.RequiredPasses})");
                yield break;
            }
            Debug.Log($"{Log}round {round}: {spot.SpotId} clean, score {points0} → {m.LocalPoints}");
        }

        static void Shot(string name)
        {
            var dir = Path.Combine(Application.dataPath, "../Logs");
            Directory.CreateDirectory(dir);
            var view = System.Environment.GetEnvironmentVariable("BOB_VIEW");
            var tag = string.IsNullOrEmpty(view) ? "" : "-" + view;
            ScreenCapture.CaptureScreenshot(Path.Combine(dir, "e2e-" + name + tag + ".png"));
        }

        /// <summary>The menu labels are TMP text, which this file does not reference at compile time: read the string property instead.</summary>
        static string LabelOf(GameObject go)
        {
            foreach (var c in go.GetComponentsInChildren<Component>(true))
            {
                if (c == null) continue;
                var type = c.GetType();
                if (type.Namespace == null || !type.Namespace.StartsWith("TMPro")) continue;
                var p = type.GetProperty("text");
                var text = p?.GetValue(c) as string;
                if (!string.IsNullOrEmpty(text)) return text;
            }
            return null;
        }

        static Button FindButton(string label, bool exact)
        {
            foreach (var b in FindObjectsByType<Button>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (!b.isActiveAndEnabled) continue;
                var text = LabelOf(b.gameObject);
                if (text == null) continue;
                var t = text.Trim();
                if (exact ? t == label : t.Contains(label)) return b;
            }
            return null;
        }
    }
}
