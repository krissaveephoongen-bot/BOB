// Captures the HUD pieces that only exist while a round is playing: the mate tray of Duo, the ping wheel, the emergency
// stain banner with its screen-edge arrow, and the T04 cart card. Edit-mode TaskRaceVerify cannot show them (the bots,
// the stains and the ping wheel all live behind Application.isPlaying), so this driver enters Play, walks the steps and
// leaves a PNG per step.
//   -executeMethod Bob.EditorTools.TaskRaceShots.Run     (Editor closed on this project; no -quit)
// Success = the Console shows "[TaskRaceShots] SHOTS PASSED" and the PNGs under Logs/.
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Bob.Game;
using Bob.Rules;
using TMPro;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Reflection;
using Bob.TaskRace;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Bob.EditorTools
{
    public static class TaskRaceShots
    {
        const string ArmKey = "Bob.TaskRaceShots.Arm", R1Key = "Bob.TaskRaceShots.R1", HeroesKey = "Bob.TaskRaceShots.Heroes";
        static bool r1Started;
        static double r1WatchdogAt = -1;
        static readonly BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

        [MenuItem("Bob/Run UI R1 Checks")]
        public static void RunR1() { Run(); SessionState.SetBool(R1Key, true); }

        public static void RunR1Heroes()
        {
            Run();
            SessionState.SetBool(HeroesKey, true);
            EditorSceneManager.OpenScene(BobGameScene.ScenePath);
        }

        static float armedAt, stepAt, yawWas, toastGoneAt;
        static int step;
        static bool entered, ok = true, seeded, spawnedOnce, arrowTurned, toastShot, cardOpened;
        static TaskRaceModeManager manager;
        static TaskRaceRound round;
        static BobTaskRaceHud hud;

        [MenuItem("Bob/Run TaskRace Shots")]
        public static void Run()
        {
            SessionState.SetBool(R1Key, false);
            SessionState.SetBool(HeroesKey, false);
            typeof(BobGameScene).GetMethod("ApplyViewSize", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, null);
            r1Started = false;
            r1WatchdogAt = -1;
            step = 0;
            entered = false;
            ok = true;
            seeded = false;
            spawnedOnce = false;
            arrowTurned = false;
            toastShot = false;
            cardOpened = false;
            yawWas = 0f;
            stepAt = 0f;
            toastGoneAt = 0f;
            manager = null;
            round = null;
            hud = null;
            SessionState.SetBool(ArmKey, true);
            EditorSceneManager.OpenScene(TaskRaceSceneBuilder.DuoScenePath);
            armedAt = (float)EditorApplication.timeSinceStartup;
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
        }

        /// <summary>Entering Play reloads the domain and drops the update hook, so arm it again from SessionState.</summary>
        [InitializeOnLoadMethod]
        static void Rehook()
        {
            if (!SessionState.GetBool(ArmKey, false)) return;
            if (EditorApplication.isPlaying) { entered = true; stepAt = 0f; }
            else armedAt = (float)EditorApplication.timeSinceStartup;
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
        }

        static void Tick()
        {
            if (!SessionState.GetBool(ArmKey, false)) { EditorApplication.update -= Tick; return; }

            if (!entered)
            {
                if (EditorApplication.isCompiling || EditorApplication.isUpdating)
                    armedAt = (float)EditorApplication.timeSinceStartup;
                if ((float)EditorApplication.timeSinceStartup - armedAt < 10f) return;
                entered = true;
                EditorApplication.EnterPlaymode();
                return;
            }
            if (!EditorApplication.isPlaying) return;
            bool heroes = SessionState.GetBool(HeroesKey, false);
            bool r1 = SessionState.GetBool(R1Key, false);
            if (heroes || r1)
            {
                // Editor time catches missing dependencies even when the player is not advancing frames.
                Application.runInBackground = true;
                if (r1WatchdogAt < 0) r1WatchdogAt = EditorApplication.timeSinceStartup;
                if (EditorApplication.timeSinceStartup - r1WatchdogAt > (heroes ? 60f : 100f))
                {
                    Check(false, $"{(heroes ? "hero" : "R1")} timeout; started={r1Started} frame={Time.frameCount} " +
                        $"manager={manager != null} hud={hud != null} round={round != null} events={EventSystem.current != null}");
                    Finish();
                    return;
                }
            }
            if (Time.realtimeSinceStartup < 1f) return;

            if (heroes)
            {
                var game = UnityEngine.Object.FindAnyObjectByType<BobGame>();
                if (!r1Started && EventSystem.current != null && game != null && Field<RectTransform>(game, "screenRoot") != null &&
                    (Field<object>(game, "phase").ToString() == "Title" || Field<object>(game, "phase").ToString() == "Login"))
                {
                    r1Started = true;
                    Debug.Log("[UIR1] character checks start after menu boot; frame=" + Time.frameCount);
                    game.StartCoroutine(HeroChecks(game));
                }
                return;
            }

            if (manager == null) manager = UnityEngine.Object.FindAnyObjectByType<TaskRaceModeManager>();
            if (hud == null) hud = UnityEngine.Object.FindAnyObjectByType<BobTaskRaceHud>();
            if (round == null) round = UnityEngine.Object.FindAnyObjectByType<TaskRaceRound>();
            if (manager == null || hud == null || round == null) return;
            if (manager.IsCountingDown) manager.SkipCountdown();
            if (manager.IsCountingDown) return;

            if (stepAt == 0f) stepAt = Time.realtimeSinceStartup;
            if (r1)
            {
                if (EventSystem.current == null) return;
                if (!r1Started) { r1Started = true; hud.StartCoroutine(R1Checks()); }
                return;
            }

            switch (step)
            {
                case 0: // a direct scene play has no MatchRequest, so no teammates; seed one and let the tray scan
                    if (!seeded)
                    {
                        seeded = true;
                        int mates = BobTaskRaceTeammates.Spawn(new[] { "sweep" }, TaskSpot.LocalTeam);
                        Debug.Log($"[TaskRaceShots] teammate(s) spawned: {mates}");
                        stepAt = Time.realtimeSinceStartup;
                        return;
                    }
                    if (Time.realtimeSinceStartup - stepAt < 1.4f) return;
                    Shot("Logs/taskrace-duo-mate.png");
                    Debug.Log($"[TaskRaceShots] mate tray: open={hud.DebugMateTray} chips={hud.DebugMateChips}");
                    Next();
                    break;
                case 1:
                    if (Time.realtimeSinceStartup - stepAt < 0.15f) return;
                    bool open = hud.DebugOpenPing();
                    Debug.Log($"[TaskRaceShots] ping wheel: open={open}");
                    if (!open) ok = false; else Shot("Logs/taskrace-ping-wheel.png");
                    hud.DebugClosePing();
                    Next();
                    break;
                case 2:
                {
                    var stains = BobEmergencyStains.Ensure(round, manager);
                    if (!spawnedOnce)
                    {
                        spawnedOnce = true;
                        bool spawned = stains.DebugSpawnNow();
                        Debug.Log($"[TaskRaceShots] emergency: spawned={spawned} enemy=\"{stains.EnemyName}\" " +
                                  $"task=\"{stains.TaskName}\" difficulty={stains.Difficulty} hasTarget={stains.HasTarget} " +
                                  $"planned={stains.PlannedSpawns} remaining={stains.Remaining:0.0}");
                        if (!spawned) { ok = false; Finish(); return; }
                        stepAt = Time.realtimeSinceStartup;
                        return; // the HUD renders the banner on its next update, not inside this call
                    }
                    if (Time.realtimeSinceStartup - stepAt < 0.45f) return;
                    Shot("Logs/taskrace-emergency.png");
                    Next();
                    break;
                }
                case 3:
                {
                    var rig = UnityEngine.Object.FindFirstObjectByType<BobTaskRaceCamera>();
                    if (!arrowTurned)
                    {
                        if (rig == null) { Debug.LogWarning("[TaskRaceShots] no camera rig for the arrow shot."); ok = false; Next(); break; }
                        yawWas = rig.yaw;
                        rig.yaw = yawWas + 180f; // face away from the stain so the edge arrow has to appear
                        arrowTurned = true;
                        stepAt = Time.realtimeSinceStartup;
                        return;
                    }
                    if (Time.realtimeSinceStartup - stepAt < 0.9f) return; // followTime 0.22 s, settle well past it
                    var arrowField = typeof(BobTaskRaceHud).GetField("emergencyArrow", BindingFlags.Instance | BindingFlags.NonPublic);
                    // The arrow stands down while a toast is up, and the spawn toast is what is on screen right now:
                    // shoot that state first, then wait the toast out (cap 6 s) plus a frame to repaint and shoot the arrow.
                    var toastField = typeof(BobTaskRaceHud).GetField("toast", BindingFlags.Instance | BindingFlags.NonPublic);
                    var toastGo = (toastField?.GetValue(hud) as Component)?.gameObject;
                    bool toastUp = toastGo != null && toastGo.activeSelf;
                    if (toastUp && !toastShot && Time.realtimeSinceStartup - stepAt < 6f)
                    {
                        toastShot = true;
                        var duringArrow = arrowField?.GetValue(hud) as RectTransform;
                        bool duringOn = duringArrow != null && duringArrow.gameObject.activeSelf;
                        Debug.Log($"[TaskRaceShots] edge arrow during toast: active={duringOn} (must be False)");
                        if (duringOn) ok = false;
                        Shot("Logs/taskrace-emergency-toast.png");
                        return;
                    }
                    if (toastUp && Time.realtimeSinceStartup - stepAt < 6f) return;
                    if (toastGoneAt == 0f) { toastGoneAt = Time.realtimeSinceStartup; return; }
                    if (Time.realtimeSinceStartup - toastGoneAt < 0.15f) return;
                    var arrow = arrowField?.GetValue(hud) as RectTransform;
                    bool arrowOn = arrow != null && arrow.gameObject.activeSelf;
                    float arrowY = arrow != null ? arrow.anchoredPosition.y : 0f;
                    float arrowX = arrow != null ? arrow.anchoredPosition.x : 0f;
                    // page is 1080 high: the centre sits 540 from the bottom, so |y| must stay under 330 to clear the
                    // 156 px banner and the 148 px top bar. Anything above 330 means the arrow is back on a strip.
                    bool clear = Mathf.Abs(arrowY) <= 330f;
                    Debug.Log($"[TaskRaceShots] edge arrow: active={arrowOn} pos=({arrowX:0},{arrowY:0}) clears strips={clear}");
                    if (!arrowOn || !clear) ok = false;
                    Shot("Logs/taskrace-emergency-arrow.png");
                    if (rig != null) rig.yaw = yawWas;
                    Next();
                    break;
                }
                case 4:
                {
                    if (!cardOpened)
                    {
                        cardOpened = true;
                        var card = typeof(BobTaskRaceHud).GetMethod("OpenEmergencyCard", BindingFlags.Instance | BindingFlags.NonPublic);
                        if (card != null) card.Invoke(hud, null);
                        if (!hud.CardOpen) { Debug.Log("[TaskRaceShots] emergency card: open=False"); ok = false; Finish(); return; }
                        Debug.Log($"[TaskRaceShots] emergency card: open=True step=\"{hud.CardStep}\" hint=\"{hud.CardHint}\"");
                        stepAt = Time.realtimeSinceStartup;
                        return;
                    }
                    // the spawn toast rides above the card for 2.2 s and the banner hides a frame after the card opens
                    if (Time.realtimeSinceStartup - stepAt < 2.6f) return;
                    Shot("Logs/taskrace-emergency-card.png");
                    hud.DebugCloseCard();
                    Next();
                    break;
                }
                default:
                {
                    if (Time.realtimeSinceStartup - stepAt < 0.3f) return;
                    var spot = UnityEngine.Object.FindObjectsByType<TaskSpot>(FindObjectsInactive.Exclude)
                        .FirstOrDefault(s => s.HasTask && s.TaskId == "T04" && s.OwnerTeam == TeamId.TeamA);
                    if (spot == null) { Debug.LogWarning("[TaskRaceShots] no T04 station on Team A."); ok = false; Finish(); return; }
                    spot.ResetTask();
                    if (!spot.TryOpen("shots", out var why)) { Debug.LogWarning($"[TaskRaceShots] T04 refused ({why})."); ok = false; Finish(); return; }
                    bool live = hud.DebugOpenCard(spot);
                    hud.DebugAdvanceCard(0.9f);
                    Debug.Log($"[TaskRaceShots] cart card: {spot.SpotId} live={live} open={hud.CardOpen} step=\"{hud.CardStep}\" hint=\"{hud.CardHint}\"");
                    if (!live) ok = false; else Shot("Logs/taskrace-cart-card.png");
                    hud.DebugCloseCard();
                    Finish();
                    break;
                }
            }
        }

        static T Field<T>(object target, string name) => (T)target.GetType().GetField(name, Private | BindingFlags.Public).GetValue(target);

        static void Check(bool pass, string name)
        {
            ok &= pass;
            Debug.Log($"[UIR1] {(pass ? "PASS" : "FAIL")} {name}");
        }

        static IEnumerator CaptureR1(string name)
        {
            yield return new WaitForEndOfFrame();
            string dir = Environment.GetEnvironmentVariable("BOB_R1_OUT") ?? "Logs/ui-r1";
            Directory.CreateDirectory(dir);
            string path = Path.Combine(dir, name + "-" + Screen.width + "x" + Screen.height + ".png");
            var texture = ScreenCapture.CaptureScreenshotAsTexture();
            File.WriteAllBytes(path, texture.EncodeToPNG());
            UnityEngine.Object.Destroy(texture);
            var labels = UnityEngine.Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Exclude)
                .Where(t => t.GetComponentInParent<Canvas>() != null && t.isActiveAndEnabled)
                .Select(t => new LabelMeasure {
                    text = t.text, temporaryFx = IsTemporaryFx(t), size = t.fontSize, width = t.rectTransform.rect.width, height = t.rectTransform.rect.height,
                    preferredHeight = t.GetPreferredValues(t.text, t.rectTransform.rect.width, float.PositiveInfinity).y
                }).ToArray();
            File.WriteAllText(Path.ChangeExtension(path, ".json"), JsonUtility.ToJson(new LabelMeasures { labels = labels }, true));
            Debug.Log("[UIR1] screenshot " + path);
        }

        [Serializable] sealed class LabelMeasure { public string text; public bool temporaryFx; public float size, width, height, preferredHeight; }
        [Serializable] sealed class LabelMeasures { public LabelMeasure[] labels; }

        // CartDelta reaction pills have a 1.5 s lifetime in TickJuice; other Fx children (e.g. waitLabel) do not.
        static bool IsTemporaryFx(TMP_Text label) => label.transform.parent != null &&
            label.transform.parent.name.StartsWith("Delta-", StringComparison.Ordinal) &&
            label.transform.parent.parent != null && label.transform.parent.parent.name == "Fx";

        static IEnumerator CaptureLargerText(RectTransform root, string name)
        {
            var labels = root.GetComponentsInChildren<TMP_Text>().Where(t => t.isActiveAndEnabled).ToArray();
            var sizes = labels.Select(t => t.fontSize).ToArray();
            var auto = labels.Select(t => t.enableAutoSizing).ToArray();
            var temporary = labels.Select(IsTemporaryFx).ToArray();
            var words = labels.Select(t => t.text).ToArray();
            for (int i = 0; i < labels.Length; i++) { labels[i].enableAutoSizing = false; labels[i].fontSize = sizes[i] * 1.3f; }
            yield return CaptureR1(name + "-text130");
            for (int i = 0; i < labels.Length; i++)
            {
                var label = labels[i];
                if (label == null)
                {
                    if (temporary[i]) Debug.Log("[UIR1] TEMPORARY FX expired before 130% measurement: " + words[i]);
                    else Check(false, "persistent label disappeared during 130% measurement: " + words[i]);
                    continue;
                }
                label.ForceMeshUpdate();
                Check(label.GetPreferredValues(label.text, label.rectTransform.rect.width, float.PositiveInfinity).y <= label.rectTransform.rect.height + 1f,
                    (temporary[i] ? "130% temporary FX text height: " : "130% text height: ") + label.text);
                Check(!label.isTextTruncated, "130% text is not truncated: " + label.text);
                label.fontSize = sizes[i]; label.enableAutoSizing = auto[i];
            }
        }

        static Vector2 ScreenCentre(RectTransform rect)
        {
            Canvas.ForceUpdateCanvases();
            var canvas = rect.GetComponentInParent<Canvas>();
            return RectTransformUtility.WorldToScreenPoint(canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera,
                rect.TransformPoint(rect.rect.center));
        }

        static GameObject Hit(Vector2 position, out PointerEventData pointer)
        {
            pointer = new PointerEventData(EventSystem.current) { position = position, pressPosition = position, button = PointerEventData.InputButton.Left };
            var hits = new List<RaycastResult>();
            EventSystem.current.RaycastAll(pointer, hits);
            if (hits.Count == 0) { Check(false, "no UI hit at " + position); return null; }
            pointer.pointerCurrentRaycast = pointer.pointerPressRaycast = hits[0];
            return hits[0].gameObject;
        }

        static void Click(RectTransform rect)
        {
            if (rect == null) { Check(false, "missing click target"); return; }
            var hit = Hit(ScreenCentre(rect), out var pointer);
            if (hit == null) return;
            // Cart artwork falls through to MiniSurface; buttons and ping options must hit their own handler.
            if (rect.GetComponent<Button>() != null || rect.name.StartsWith("Ping_", StringComparison.Ordinal))
            {
                bool targetHit = hit.transform.IsChildOf(rect);
                Check(targetHit, "raycast " + rect.name + " hits " + hit.name);
                var clickReceiver = ExecuteEvents.GetEventHandler<IPointerClickHandler>(hit);
                Check(clickReceiver == rect.gameObject, "click handler belongs to " + rect.name);
                if (!targetHit || clickReceiver != rect.gameObject) return;
            }
            ExecuteEvents.ExecuteHierarchy(hit, pointer, ExecuteEvents.pointerDownHandler);
            ExecuteEvents.ExecuteHierarchy(hit, pointer, ExecuteEvents.pointerUpHandler);
            ExecuteEvents.ExecuteHierarchy(hit, pointer, ExecuteEvents.pointerClickHandler);
        }

        static IEnumerator Drag(RectTransform rect, Vector2 destination)
        {
            var hit = Hit(ScreenCentre(rect), out var pointer);
            if (hit == null) yield break;
            var receiver = ExecuteEvents.ExecuteHierarchy(hit, pointer, ExecuteEvents.pointerDownHandler);
            var host = rect.GetComponentInParent<BobMinigameHost>();
            var held = Field<object>(host, "cartChipHeld");
            Check(held != null && Field<RectTransform>(held, "rect") == rect, "drag acquires " + rect.name);
            ExecuteEvents.ExecuteHierarchy(hit, pointer, ExecuteEvents.beginDragHandler);
            yield return null;
            pointer.position = destination; pointer.dragging = true;
            ExecuteEvents.ExecuteHierarchy(receiver ?? hit, pointer, ExecuteEvents.dragHandler);
            yield return null;
            ExecuteEvents.ExecuteHierarchy(receiver ?? hit, pointer, ExecuteEvents.pointerUpHandler);
            ExecuteEvents.ExecuteHierarchy(receiver ?? hit, pointer, ExecuteEvents.endDragHandler);
            yield return new WaitForSecondsRealtime(0.25f);
        }

        static RectTransform Part(BobMinigameHost mini, string id)
        {
            var text = mini.GetComponentsInChildren<TMP_Text>().FirstOrDefault(t => t.text == BobRules.Parts[id].Name);
            return text != null ? text.transform.parent as RectTransform : null;
        }

        static IEnumerator HoldLock(BobMinigameHost mini, string state)
        {
            var rect = Field<Image>(mini, "cartLock").rectTransform;
            var hit = Hit(ScreenCentre(rect), out var pointer);
            if (hit == null) yield break;
            var receiver = ExecuteEvents.ExecuteHierarchy(hit, pointer, ExecuteEvents.pointerDownHandler);
            Check(Field<bool>(mini, "holding"), "lock pointer acquires hold");
            yield return null;
            float releasedProgress = Field<float>(mini, "cartHold");
            Check(!Field<bool>(mini, "cartLocked") && releasedProgress > 0f && releasedProgress < 1f,
                "one-frame hold advances without completing");
            ExecuteEvents.ExecuteHierarchy(receiver ?? hit, pointer, ExecuteEvents.pointerUpHandler);
            Check(!Field<bool>(mini, "holding"), "lock pointer releases hold");
            float deadline = Time.realtimeSinceStartup + 2f;
            while (Field<float>(mini, "cartHold") > 0f && Time.realtimeSinceStartup < deadline) yield return null;
            Debug.Log($"[UIR1] hold {state}: release={releasedProgress:F3} decayed={Field<float>(mini, "cartHold"):F3}");
            Check(!Field<bool>(mini, "cartLocked") && Field<float>(mini, "cartHold") <= 0f, "short hold releases and decays without locking");
            receiver = ExecuteEvents.ExecuteHierarchy(hit, pointer, ExecuteEvents.pointerDownHandler);
            Check(Field<bool>(mini, "holding"), "full lock hold acquired");
            yield return CaptureR1("cart-holding-" + state);
            yield return new WaitForSecondsRealtime(0.7f);
            ExecuteEvents.ExecuteHierarchy(receiver ?? hit, pointer, ExecuteEvents.pointerUpHandler);
        }

        static void CheckSeparated(RectTransform a, RectTransform b, string name)
        {
            Rect Bounds(RectTransform r)
            {
                var corners = new Vector3[4]; r.GetWorldCorners(corners);
                var canvas = r.GetComponentInParent<Canvas>();
                var cam = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
                var points = corners.Select(c => RectTransformUtility.WorldToScreenPoint(cam, c)).ToArray();
                return Rect.MinMaxRect(points.Min(p => p.x), points.Min(p => p.y), points.Max(p => p.x), points.Max(p => p.y));
            }
            Check(!Bounds(a).Overlaps(Bounds(b)), name + " do not overlap");
        }

        static void CheckBounds(RectTransform rect, string name)
        {
            var corners = new Vector3[4]; rect.GetWorldCorners(corners);
            var canvas = rect.GetComponentInParent<Canvas>();
            var cam = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            var points = corners.Select(c => RectTransformUtility.WorldToScreenPoint(cam, c)).ToArray();
            var min = new Vector2(points.Min(p => p.x), points.Min(p => p.y));
            var max = new Vector2(points.Max(p => p.x), points.Max(p => p.y));
            var safe = Screen.safeArea;
            Check(min.x >= safe.xMin - 1 && min.y >= safe.yMin - 1 && max.x <= safe.xMax + 1 && max.y <= safe.yMax + 1, name + " inside actual safe area");
        }

        static IEnumerator HeroChecks(BobGame game)
        {
            string original = Field<string>(game, "charId");
            var open = typeof(BobGame).GetMethod("OpenHeroSelect", Private);
            RectTransform Named(string name) => Field<RectTransform>(game, "screenRoot").GetComponentsInChildren<RectTransform>()
                .FirstOrDefault(r => r.name == name && r.gameObject.activeInHierarchy);
            open.Invoke(game, new object[] { false });
            yield return null;
            var dialog = Named("HeroSelectR1");
            Check(dialog != null, "character dialog opens");
            CheckBounds(dialog, "character modal");
            Check(dialog.GetComponentsInChildren<Button>().Count(b => b.name.StartsWith("Choose_")) == 3,
                "four heroes with three selection actions");
            Check(Named("ActiveHero").GetComponent<Button>() == null, "active hero is informational, not a button");
            yield return CaptureR1("character");
            yield return CaptureLargerText(dialog, "character");
            foreach (var hero in BobMatch.Chars.Where(c => c.Id != original))
            {
                Click(Named("Choose_" + hero.Id)); yield return null;
                Check(Field<string>(game, "charId") == hero.Id, "select hero " + hero.Id);
                yield return CaptureLargerText(Named("HeroSelectR1"), "character-" + hero.Id);
            }
            Click(Named("Choose_" + original)); yield return new WaitForEndOfFrame();
            Check(Field<string>(game, "charId") == original, "original character restored through selector");
            Check(Named("HeroDone").GetComponentInChildren<TMP_Text>().text == "เสร็จสิ้น", "nonmatch completion copy");
            Click(Named("HeroDone")); yield return null;
            Check(Field<object>(game, "phase").ToString() == "Title", "done returns home");
            open.Invoke(game, new object[] { true });
            yield return new WaitForSecondsRealtime(1.2f);
            float before = Field<float>(game, "heroTimer");
            string other = BobMatch.Chars.First(c => c.Id != original).Id;
            Click(Named("Choose_" + other)); yield return null;
            Check(Field<float>(game, "heroTimer") < before && Field<float>(game, "heroTimer") < 14f,
                "selection preserves running match countdown");
            yield return CaptureR1("character-match");
            Click(Named("Choose_" + original)); yield return new WaitForEndOfFrame();
            Click(Named("HeroClose")); yield return null;
            Check(Field<object>(game, "phase").ToString() == "Title" && !Field<bool>(game, "heroTimerOn"),
                "close returns home and stops countdown");
            Debug.Log(ok ? "[UIR1] HERO CHECKS PASSED" : "[UIR1] HERO CHECKS FAILED");
            Finish();
        }

        static IEnumerator R1Checks()
        {
            Debug.Log($"[UIR1] native Play Mode {Screen.width}x{Screen.height}; pointer events raycast through live EventSystem; no physical touch evidence");
            round.ConfigureRound(2, 1, 1019); round.StartRound(1019);
            var stains = BobEmergencyStains.Ensure(round, manager);
            stains.enabled = false;
            BobTaskRaceTeammates.Spawn(new[] { "sweep" }, TaskSpot.LocalTeam);
            yield return new WaitForSecondsRealtime(0.6f);
            Check(hud.RowCount == round.TaskCount, "all worksheet rows retained");
            var series = UnityEngine.Object.FindAnyObjectByType<BobSeriesBanner>();
            var seriesDim = series.GetComponentsInChildren<Image>(true).First(i => i.name == "SeriesDim");
            Check(!seriesDim.gameObject.activeInHierarchy, "series dim is hidden during the round");
            var pillCanvas = series.GetComponentsInChildren<Canvas>(true).First(c => c.name == "SeriesPill");
            Check(pillCanvas.overrideSorting && pillCanvas.sortingOrder < hud.UiCanvas.sortingOrder,
                "series pill renders behind HUD cards");
            Check(hud.UiCanvas.renderMode == RenderMode.ScreenSpaceOverlay, "HUD renders after world post-processing");
            CheckBounds(Field<RectTransform>(hud, "card"), "worksheet");
            CheckBounds(Field<RectTransform>(hud, "topBar"), "score group");
            CheckBounds(Field<Button>(hud, "pauseButton").transform as RectTransform, "pause");
            yield return CaptureR1("hud");
            yield return CaptureLargerText(Field<RectTransform>(hud, "page"), "hud");
            while (round.TaskCount < 10) round.AddPenaltyTask(round.Tasks[0].Spec, TaskSpot.LocalTeam);
            yield return null;
            var viewport = Field<RectTransform>(hud, "rowsViewport");
            var scrollHit = Hit(ScreenCentre(viewport), out var scrollPointer);
            scrollPointer.scrollDelta = new Vector2(0f, -10f);
            ExecuteEvents.ExecuteHierarchy(scrollHit, scrollPointer, ExecuteEvents.scrollHandler);
            yield return null;
            Check(hud.RowCount == 10 && Field<RectTransform>(hud, "rowsRoot").anchoredPosition.y > 0f,
                "overflow worksheet retains ten rows and scrolls");
            yield return CaptureR1("worksheet-scrolled");
            round.StartRound(1019);
            yield return null;
            var touch = BobTaskRaceTouch.Current;
            var stickHit = Hit(ScreenCentre(Field<RectTransform>(touch, "hint")), out var stickPointer);
            var stickReceiver = ExecuteEvents.ExecuteHierarchy(stickHit, stickPointer, ExecuteEvents.pointerDownHandler);
            Check(touch.IsHeld && touch.Move == Vector2.zero, "joystick press stays inside dead zone");
            stickPointer.position += Vector2.right * 150f * hud.UiCanvas.scaleFactor;
            ExecuteEvents.ExecuteHierarchy(stickReceiver, stickPointer, ExecuteEvents.dragHandler);
            Check(touch.Move.x > 0.95f, "joystick drag reaches full input");
            yield return CaptureR1("joystick-held");
            ExecuteEvents.ExecuteHierarchy(stickReceiver, stickPointer, ExecuteEvents.pointerUpHandler);
            Check(!touch.IsHeld && touch.Move == Vector2.zero, "joystick release stops input");
            Click(Field<Button>(hud, "pauseButton").transform as RectTransform);
            yield return null;
            float pausedAt = manager.RemainingSeconds;
            yield return new WaitForSecondsRealtime(0.4f);
            Check(hud.PauseOpen && manager.Paused && Mathf.Approximately(pausedAt, manager.RemainingSeconds), "pause button freezes clock");
            yield return CaptureR1("pause");
            yield return CaptureLargerText(Field<RectTransform>(hud, "pauseRoot"), "pause");
            var resume = Field<RectTransform>(hud, "pauseRoot").GetComponentsInChildren<Button>()
                .FirstOrDefault(b => b.GetComponentInChildren<TMP_Text>().text.Contains("เล่นต่อ"));
            Click(resume != null ? resume.transform as RectTransform : null);
            yield return new WaitForSecondsRealtime(0.3f);
            Check(!hud.PauseOpen && manager.RemainingSeconds < pausedAt, "resume button restarts clock");
            Click(Field<RectTransform>(hud, "pingButton"));
            yield return CaptureR1("call-wheel");
            var options = Field<RectTransform>(hud, "pingRoot").GetComponentsInChildren<RectTransform>().FirstOrDefault(r => r.name == "Ping_0");
            Click(options); yield return null;
            Check(!Field<bool>(hud, "pingWheelOpen"), "call option closes wheel");
            string firstWord = ((string[])typeof(BobTaskRaceHud).GetField("PingWords", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null))[0];
            Check(Field<TMP_Text>(hud, "pingBubbleLabel").text.StartsWith(firstWord, StringComparison.Ordinal) &&
                Field<float>(hud, "pingBubbleUntil") > Time.unscaledTime, "call option sends its message");
            yield return CaptureR1("call-sent");

            foreach (var bot in UnityEngine.Object.FindObjectsByType<TaskRaceBotController>(FindObjectsSortMode.None)) bot.enabled = false;
            round.StartRound(1019);
            yield return null;
            var spot = round.StationsOf(TaskSpot.LocalTeam).FirstOrDefault(s => s.TaskId == "T04");
            if (spot == null) { Check(false, "T04 available"); Finish(); yield break; }
            // Seeded station setup is test-only; picking, dropping and holding use the live UI handlers.
            if (!spot.TryOpen("ui-r1", out var why)) { Check(false, "T04 open " + why); Finish(); yield break; }
            Check(hud.DebugOpenCard(spot), "T04 card opens");
            yield return new WaitForSecondsRealtime(0.3f);
            var mini = Field<BobMinigameHost>(hud, "mini");
            CheckBounds(Field<Image>(hud, "cardPanel").rectTransform, "cart modal");
            yield return CaptureR1("cart-pick");
            yield return CaptureLargerText(Field<Image>(hud, "cardPanel").rectTransform, "cart-pick");
            string[] good = { "sponge", "brush", "steam", "cloth" };
            foreach (string id in good) { Click(Part(mini, id)); yield return new WaitForSecondsRealtime(0.22f); }
            Check(mini.StepNumber == 2 && Field<List<string>>(mini, "cartTray").Count == 4, "fourth pick auto advances");
            yield return CaptureR1("cart-build");
            yield return CaptureLargerText(Field<Image>(hud, "cardPanel").rectTransform, "cart-build");
            var play = Field<RectTransform>(mini, "play");
            Vector2 drop = RectTransformUtility.WorldToScreenPoint(hud.UiCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : hud.UiCanvas.worldCamera, play.TransformPoint(new Vector2(play.rect.width * 0.30f, -80)));
            foreach (string id in good) yield return Drag(Part(mini, id), drop);
            Check(Field<List<string>>(mini, "cartStack").SequenceEqual(good), "four distinct parts drop in order");
            Check(mini.CartStats.SequenceEqual(new[] { 10, 7, 3, 6 }), "fixture stack stats 10/7/3/6");
            Check(BobRules.StackCost(Field<List<string>>(mini, "cartStack")) == 75, "stack cost remains 75");
            yield return CaptureR1("cart-ready");
            yield return CaptureLargerText(Field<Image>(hud, "cardPanel").rectTransform, "cart-ready");
            yield return HoldLock(mini, "success");
            Check(mini.CartPassed && mini.CartPassCount == 4, "successful formula grants four conditions");
            Check(!hud.CardOpen, "successful lock completes and closes station card");
            yield return CaptureR1("cart-complete");

            round.StartRound(1019); yield return null;
            spot = round.StationsOf(TaskSpot.LocalTeam).First(s => s.TaskId == "T04");
            if (!spot.TryOpen("ui-r1", out why)) { Check(false, "T04 fixture open " + why); Finish(); yield break; }
            Check(hud.DebugOpenCard(spot), "T04 fixture card opens"); yield return null;
            foreach (string id in new[] { "soak", "brush", "sponge", "polish" }) { Click(Part(mini, id)); yield return new WaitForSecondsRealtime(0.22f); }
            play = Field<RectTransform>(mini, "play");
            drop = RectTransformUtility.WorldToScreenPoint(hud.UiCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : hud.UiCanvas.worldCamera, play.TransformPoint(new Vector2(play.rect.width * 0.30f, -80)));
            yield return Drag(Part(mini, "soak"), drop);
            Check(Field<List<string>>(mini, "cartStack").Count == 0 && Field<List<string>>(mini, "cartTray").Contains("soak") &&
                Field<string>(mini, "cartError") == "วางไม่ได้ · วิธีต้องวางบนของ", "method first rejected and returned to tray");
            yield return CaptureR1("cart-invalid");
            yield return Drag(Part(mini, "brush"), drop);
            yield return Drag(Part(mini, "sponge"), drop);
            var layers = Field<IList>(mini, "cartLayers");
            var lower = Field<RectTransform>(layers[0], "rect");
            Click(lower); yield return null;
            Check(Field<List<string>>(mini, "cartStack").Count == 2, "lower layer tap cannot remove");
            var top = Field<RectTransform>(layers[1], "rect");
            Check(top.rect.height >= 144f, "top removable layer minimum 144u");
            Click(top); yield return new WaitForSecondsRealtime(0.25f);
            Check(Field<List<string>>(mini, "cartStack").SequenceEqual(new[] { "brush" }) && !Field<List<string>>(mini, "cartTray").Contains("sponge"), "top removal does not return part to tray");
            yield return CaptureR1("cart-removed");
            yield return HoldLock(mini, "no-buff");
            Check(!mini.CartPassed && mini.CartPassCount == 0 && !hud.CardOpen, "unsuccessful formula still completes without buff");
            yield return CaptureR1("cart-no-buff");
            round.StartRound(1019); yield return null;
            spot = round.StationsOf(TaskSpot.LocalTeam).First(s => s.TaskId == "T04");
            if (!spot.TryOpen("ui-r1", out why)) { Check(false, "T04 fixture open " + why); Finish(); yield break; }
            Check(hud.DebugOpenCard(spot), "T04 fixture card opens"); yield return null;
            foreach (string id in good) { Click(Part(mini, id)); yield return new WaitForSecondsRealtime(0.22f); }
            yield return HoldLock(mini, "empty");
            Check(!mini.CartPassed && !hud.CardOpen, "empty stack can lock without buff");
            round.StartRound(1019); yield return null;
            stains.enabled = true;
            stains.Bind(round, manager);
            // Update must build the schedule before the debug hook brings its first entry forward.
            yield return null;
            Check(stains.PlannedSpawns > 0, "emergency schedule initialized");
            if (stains.PlannedSpawns == 0) { Finish(); yield break; }
            var moments = Field<IList>(stains, "plan");
            typeof(BobEmergencyStains).GetField("elapsed", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(stains, Field<float>(moments[stains.SpawnedCount], "At"));
            bool spawned = stains.DebugSpawnNow();
            Check(spawned, "emergency fixture spawns");
            if (!spawned) { Finish(); yield break; }
            Check(Mathf.Abs(stains.Remaining - stains.GraceSeconds) < 0.1f, "emergency starts with full grace period");
            var rig = UnityEngine.Object.FindFirstObjectByType<BobTaskRaceCamera>();
            float yaw = rig.yaw; rig.yaw += 180f;
            yield return new WaitForSecondsRealtime(0.6f);
            var banner = Field<RectTransform>(hud, "emergencyRoot");
            var toastRoot = Field<RectTransform>(hud, "toastRoot");
            var arrow = Field<RectTransform>(hud, "emergencyArrow");
            Check(banner.gameObject.activeSelf && toastRoot.gameObject.activeSelf && !arrow.gameObject.activeSelf,
                "emergency toast suppresses arrow");
            CheckBounds(banner, "emergency banner");
            CheckBounds(toastRoot, "toast");
            CheckSeparated(banner, toastRoot, "toast and emergency");
            CheckSeparated(banner, Field<RectTransform>(hud, "pingButton"), "emergency and call");
            yield return CaptureR1("emergency-toast");
            yield return CaptureLargerText(banner, "emergency");
            yield return new WaitForSecondsRealtime(Mathf.Max(0f, Field<float>(hud, "toastUntil") - Time.unscaledTime) + 0.2f);
            Check(arrow.gameObject.activeSelf && !toastRoot.gameObject.activeSelf, "arrow returns after toast");
            CheckBounds(arrow, "emergency arrow");
            foreach (var obstacle in new[] { banner, Field<RectTransform>(hud, "card"), Field<RectTransform>(hud, "topBar"),
                Field<RectTransform>(hud, "mateTray"), Field<RectTransform>(hud, "pingButton"),
                (RectTransform)Field<Button>(hud, "mainButton").transform, (RectTransform)Field<Button>(hud, "pauseButton").transform })
                CheckSeparated(arrow, obstacle, "arrow and " + obstacle.name);
            yield return CaptureR1("emergency-arrow");
            Click(banner); yield return new WaitForSecondsRealtime(0.3f);
            Check(hud.CardOpen, "emergency banner opens minigame");
            Check(mini.GetComponentsInChildren<RawImage>().Any(i => i.isActiveAndEnabled && i.texture is RenderTexture),
                "non-cart minigame retains RenderTexture stage");
            yield return CaptureR1("emergency-card");
            var cameraWas = hud.UiCanvas.worldCamera;
            Shot(Path.Combine(Environment.GetEnvironmentVariable("BOB_R1_OUT") ?? "Logs/ui-r1", "camera-capture.png"));
            Check(hud.UiCanvas.renderMode == RenderMode.ScreenSpaceOverlay && hud.UiCanvas.worldCamera == cameraWas,
                "RenderTexture capture restores overlay and camera");
            yield return null;
            Click(Field<Button>(hud, "cardClose").transform as RectTransform); yield return null;
            Check(!hud.CardOpen, "close exits emergency card");
            rig.yaw = yaw;
            Debug.Log(ok ? "[UIR1] R1 CHECKS PASSED" : "[UIR1] R1 CHECKS FAILED");
            Finish();
        }

        static void Next()
        {
            step++;
            stepAt = Time.realtimeSinceStartup;
        }

        static void Shot(string file)
        {
            GameObject hero = null;
            try { hero = GameObject.FindGameObjectWithTag("Player"); } catch (UnityException) { }
            if (hero == null) { Debug.LogWarning($"[TaskRaceShots] no hero for {file}."); ok = false; return; }
            var rig = UnityEngine.Object.FindFirstObjectByType<BobTaskRaceCamera>();
            float up = rig != null ? rig.Height : 5.2f;
            float back = rig != null ? rig.Distance : 5.2f;
            float yaw = rig != null ? rig.yaw : 0f;
            // same orbit the rig uses, yaw included, so the HUD's edge-arrow maths sees the camera it is drawn from
            TaskRaceVerify.Shot(file, hero.transform.position + Quaternion.Euler(0f, yaw, 0f) * new Vector3(0f, up, -back),
                hero.transform.position + new Vector3(0f, 0.72f, 0f), 40f, hud);
        }

        static void Finish()
        {
            SessionState.SetBool(ArmKey, false);
            SessionState.SetBool(R1Key, false);
            SessionState.SetBool(HeroesKey, false);
            EditorApplication.update -= Tick;
            Debug.Log(ok ? "[TaskRaceShots] SHOTS PASSED" : "[TaskRaceShots] SHOTS FAILED");
            EditorApplication.ExitPlaymode();
            EditorApplication.delayCall += () => EditorApplication.Exit(ok ? 0 : 1);
        }
    }
}
