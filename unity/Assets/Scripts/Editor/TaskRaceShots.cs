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
        const string ArmKey = "Bob.TaskRaceShots.Arm", R1Key = "Bob.TaskRaceShots.R1", HeroesKey = "Bob.TaskRaceShots.Heroes",
            CardsKey = "Bob.TaskRaceShots.Cards";
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

        /// <summary>The Home overlay cards (อันดับ / เดลี่ / ตู้เสื้อผ้า / ตั้งค่า / หยุดเกม) opened the way a player opens them.</summary>
        [MenuItem("Bob/Run UI R1 Card Checks")]
        public static void RunR1Cards()
        {
            Run();
            SessionState.SetBool(CardsKey, true);
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
            SessionState.SetBool(CardsKey, false);
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
            bool cards = SessionState.GetBool(CardsKey, false);
            bool r1 = SessionState.GetBool(R1Key, false);
            if (heroes || cards || r1)
            {
                // Editor time catches missing dependencies even when the player is not advancing frames.
                Application.runInBackground = true;
                if (r1WatchdogAt < 0) r1WatchdogAt = EditorApplication.timeSinceStartup;
                float budget = heroes ? 60f : cards ? 150f : 100f;
                if (EditorApplication.timeSinceStartup - r1WatchdogAt > budget)
                {
                    Check(false, $"{(heroes ? "hero" : cards ? "card" : "R1")} timeout; started={r1Started} frame={Time.frameCount} " +
                        $"manager={manager != null} hud={hud != null} round={round != null} events={EventSystem.current != null}");
                    Finish();
                    return;
                }
            }
            if (Time.realtimeSinceStartup < 1f) return;

            if (heroes || cards)
            {
                var game = UnityEngine.Object.FindAnyObjectByType<BobGame>();
                if (!r1Started && EventSystem.current != null && game != null && Field<RectTransform>(game, "screenRoot") != null &&
                    (Field<object>(game, "phase").ToString() == "Title" || Field<object>(game, "phase").ToString() == "Login"))
                {
                    r1Started = true;
                    Debug.Log($"[UIR1] {(cards ? "overlay card" : "character")} checks start after menu boot; frame=" + Time.frameCount);
                    game.StartCoroutine(cards ? CardChecks(game) : HeroChecks(game));
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

        static bool CloseColour(Color a, Color b) => Mathf.Approximately(a.r, b.r) && Mathf.Approximately(a.g, b.g) &&
            Mathf.Approximately(a.b, b.b) && Mathf.Approximately(a.a, b.a);

        static void Check(bool pass, string name)
        {
            ok &= pass;
            Debug.Log($"[UIR1] {(pass ? "PASS" : "FAIL")} {name}");
        }

        static IEnumerator CaptureR1(string name, Action<Texture2D> inspect = null)
        {
            yield return new WaitForEndOfFrame();
            string dir = Environment.GetEnvironmentVariable("BOB_R1_OUT") ?? "Logs/ui-r1";
            Directory.CreateDirectory(dir);
            string path = Path.Combine(dir, name + "-" + Screen.width + "x" + Screen.height + ".png");
            var texture = ScreenCapture.CaptureScreenshotAsTexture();
            File.WriteAllBytes(path, texture.EncodeToPNG());
            inspect?.Invoke(texture);
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

        static IEnumerator DragWorksheetBottom()
        {
            var viewport = Field<RectTransform>(hud, "rowsViewport");
            var scroll = viewport.GetComponent<ScrollRect>();
            scroll.StopMovement();
            scroll.verticalNormalizedPosition = 1f;
            yield return null;
            var start = viewport.TransformPoint(new Vector2(viewport.rect.center.x, viewport.rect.yMin + 16f));
            var hit = Hit(RectTransformUtility.WorldToScreenPoint(null, start), out var pointer);
            if (hit == null) yield break;
            var receiver = ExecuteEvents.GetEventHandler<IDragHandler>(hit);
            Check(receiver == viewport.gameObject, "worksheet bottom drag belongs to ScrollRect, not joystick");
            if (receiver != viewport.gameObject) yield break;
            ExecuteEvents.ExecuteHierarchy(hit, pointer, ExecuteEvents.pointerDownHandler);
            ExecuteEvents.ExecuteHierarchy(hit, pointer, ExecuteEvents.initializePotentialDrag);
            ExecuteEvents.ExecuteHierarchy(hit, pointer, ExecuteEvents.beginDragHandler);
            yield return null;
            pointer.position += Vector2.up * 80f;
            pointer.dragging = true;
            ExecuteEvents.ExecuteHierarchy(receiver, pointer, ExecuteEvents.dragHandler);
            yield return null;
            Check(Field<RectTransform>(hud, "rowsRoot").anchoredPosition.y > 0f &&
                !BobTaskRaceTouch.Current.IsHeld && BobTaskRaceTouch.Current.Move == Vector2.zero,
                "dragging worksheet bottom scrolls without moving hero");
            ExecuteEvents.ExecuteHierarchy(receiver, pointer, ExecuteEvents.endDragHandler);
            ExecuteEvents.ExecuteHierarchy(receiver, pointer, ExecuteEvents.pointerUpHandler);
            scroll.StopMovement();
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
            // Screenshot encoding can stall an editor frame past 0.6 s; short timing uses explicit ticks between live pointer events.
            mini.Tick(0.12f);
            float releasedProgress = Field<float>(mini, "cartHold");
            Check(!Field<bool>(mini, "cartLocked") && Mathf.Approximately(releasedProgress, 0.2f),
                "0.12-second hold advances to 20% without completing");
            ExecuteEvents.ExecuteHierarchy(receiver ?? hit, pointer, ExecuteEvents.pointerUpHandler);
            Check(!Field<bool>(mini, "holding"), "lock pointer releases hold");
            mini.Tick(0.05f);
            Check(!Field<bool>(mini, "cartLocked") && Mathf.Approximately(Field<float>(mini, "cartHold"), 0.1f),
                "released hold decays by dt times two");
            mini.Tick(0.06f);
            Debug.Log($"[UIR1] hold {state}: release={releasedProgress:F3} decayed={Field<float>(mini, "cartHold"):F3}");
            Check(!Field<bool>(mini, "cartLocked") && Field<float>(mini, "cartHold") <= 0f, "short hold releases and decays without locking");
            yield return null;
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

        /// <summary>
        /// The Home dock cards, opened the way a player opens them and checked as R1 kit: the rounded cream plate instead
        /// of ui_panel, the R1 Plex faces instead of the pre-baked SDF twins, the copy each branch is meant to say, and the
        /// close route. Settings and wardrobe edits are reverted before the run ends — nothing here may change a rule.
        /// </summary>
        static IEnumerator CardChecks(BobGame game)
        {
            Debug.Log($"[UIR1] native Play Mode {Screen.width}x{Screen.height}; Home overlay cards opened through the dock");
            var overlayType = Field<object>(game, "overlay").GetType();
            var phaseType = Field<object>(game, "phase").GetType();
            var setOverlay = typeof(BobGame).GetMethod("SetOverlay", Private);
            var phaseField = typeof(BobGame).GetField("phase", Private);
            var owned = Field<HashSet<string>>(game, "owned");
            var overlayRoot = Field<RectTransform>(game, "overlayRoot");
            var screenRoot = Field<RectTransform>(game, "screenRoot");
            string OverlayName() => Field<object>(game, "overlay").ToString();
            void Open(string name) => setOverlay.Invoke(game, new object[] { Enum.Parse(overlayType, name) });
            RectTransform Card() => overlayRoot.GetComponentsInChildren<RectTransform>(true)
                .FirstOrDefault(r => r.name == "Card" && r.gameObject.activeInHierarchy);
            TMP_Text[] Labels(RectTransform root) => root.GetComponentsInChildren<TMP_Text>().Where(t => t.isActiveAndEnabled).ToArray();
            bool Shows(string text)
            {
                var card = Card();
                return card != null && Labels(card).Any(t => t.text.Trim().StartsWith(text, StringComparison.Ordinal));
            }
            Button CardButton(string text)
            {
                var card = Card();
                if (card == null) return null;
                return card.GetComponentsInChildren<Button>().FirstOrDefault(b => b.isActiveAndEnabled &&
                    b.GetComponentInChildren<TMP_Text>() != null &&
                    b.GetComponentInChildren<TMP_Text>().text.Trim().StartsWith(text, StringComparison.Ordinal));
            }
            Button Dock(string label) => screenRoot.GetComponentsInChildren<Button>().FirstOrDefault(b => b.isActiveAndEnabled &&
                b.GetComponentInChildren<TMP_Text>() != null && b.GetComponentInChildren<TMP_Text>().text.Trim() == label);
            IEnumerator WaitUntilSettled(string loading, string what, float seconds)
            {
                double until = EditorApplication.timeSinceStartup + seconds;
                while (Shows(loading) && EditorApplication.timeSinceStartup < until) yield return null;
                Check(!Shows(loading), what + " card leaves its loading line");
            }
            // A card is rebuilt from nothing each time it opens and its height comes from a ContentSizeFitter, so a click
            // in the frame right after that rebuild lands on the dim instead of the button. Force the layout pass and wait
            // for a drawn frame before tapping anything inside a card.
            IEnumerator Drawn(RectTransform root = null)
            {
                root = root != null ? root : Card();
                if (root != null) LayoutRebuilder.ForceRebuildLayoutImmediate(root);
                Canvas.ForceUpdateCanvases();
                yield return new WaitForEndOfFrame();
            }
            IEnumerator Tap(Button button)
            {
                yield return Drawn();
                Click(button.transform as RectTransform);
                yield return new WaitForEndOfFrame();
            }
            // A row below the fold is clipped by the viewport mask, so a click there would never reach it: the list is
            // scrolled until the row sits in the visible window.
            IEnumerator Reveal(RectTransform row)
            {
                var scroll = Card().GetComponentInChildren<ScrollRect>(true);
                if (scroll == null) yield break;
                LayoutRebuilder.ForceRebuildLayoutImmediate(scroll.content);
                float view = scroll.viewport.rect.height, full = scroll.content.rect.height;
                if (full > view)
                {
                    float top = -scroll.content.InverseTransformPoint(row.position).y;
                    float wanted = Mathf.Clamp(top - (view - row.rect.height) * 0.5f, 0f, full - view);
                    scroll.verticalNormalizedPosition = 1f - wanted / (full - view);
                }
                yield return new WaitForEndOfFrame();
            }
            void CheckKit(string what)
            {
                var card = Card();
                Check(card != null, what + " card draws on the overlay root");
                if (card == null) return;
                var plate = card.GetComponent<Image>();
                Check(plate != null && plate.sprite != null && plate.sprite.texture.width == 64 && plate.type == Image.Type.Sliced,
                    what + " card plate is the R1 rounded sprite, not ui_panel");
                var labels = Labels(card);
                // The R1 faces are made at runtime by CreateFontAsset, which leaves the asset name empty, so the atlas mode
                // is what tells them apart from the pre-baked SDF twins in Resources (those are Static).
                Debug.Log($"[UIR1] {what} fonts: " + string.Join(" | ", labels.Select(t => t.font == null ? "<none>"
                    : (string.IsNullOrEmpty(t.font.name) ? "<runtime>" : t.font.name) + " / " + t.font.atlasPopulationMode).Distinct()));
                Check(labels.Length > 0 && labels.All(t => t.font != null && t.font != BobUi.BodyTmp && t.font != BobUi.HeadingTmp &&
                    t.font.atlasPopulationMode == AtlasPopulationMode.Dynamic), what + " labels are set in the R1 Plex faces");
                Check(Labels(card).Count(t => t.raycastTarget) == 0, what + " labels do not eat clicks");
                CheckBounds(card, what + " card");
            }
            int RowCount() => Card().GetComponentsInChildren<Image>().Count(i => i.name == "Row" && i.gameObject.activeInHierarchy);

            // A board snapshot that never touches the service: rows are built by reflection over the plain data types in
            // Scripts/UGS so the card's signed-in branch can be drawn and measured inside the editor.
            object BoardFixture(int lines, string error)
            {
                var asm = typeof(BobGame).Assembly;
                var snapType = asm.GetType("Bob.Ugs.BobBoardSnapshot");
                var rowType = asm.GetType("Bob.Ugs.BobBoardRow");
                var pub = BindingFlags.Public | BindingFlags.Instance;
                var snap = Activator.CreateInstance(snapType);
                snapType.GetField("SignedIn", pub).SetValue(snap, true);
                snapType.GetField("Error", pub).SetValue(snap, error);
                var rows = (IList)snapType.GetField("Rows", pub).GetValue(snap);
                for (int i = 1; i <= lines; i++)
                {
                    var row = Activator.CreateInstance(rowType, i, i == 3 ? "บ๊อบ" : "นักกวาด " + i, 1400 - i * 90);
                    rowType.GetField("Self", pub).SetValue(row, i == 3);
                    rows.Add(row);
                }
                snapType.GetField("SelfRank", pub).SetValue(snap, lines > 0 ? 3 : -1);
                snapType.GetField("SelfScore", pub).SetValue(snap, lines > 0 ? 1130 : 0);
                return snap;
            }
            var boardSnapField = typeof(BobGame).GetField("boardSnap", Private);

            var boardDock = Dock("อันดับ");
            Check(boardDock != null, "Home dock offers อันดับ");
            if (boardDock == null) { Finish(); yield break; }
            Click(boardDock.transform as RectTransform); yield return new WaitForEndOfFrame();
            Check(OverlayName() == "Board", "dock อันดับ opens the board card");
            yield return WaitUntilSettled("กำลังโหลด", "board", 20f);
            CheckKit("board");
            Check(Shows("อันดับ"), "board card keeps its title");
            bool boardOffline = Shows("ออฟไลน์ · เล่นจบแมตช์เพื่อเก็บคะแนน");
            bool boardEmpty = Shows("ยังไม่มีใครมีคะแนน") || Shows("อ่านอันดับไม่ได้");
            Debug.Log($"[UIR1] board branch: live offline={boardOffline} empty={boardEmpty} rows={RowCount()} " +
                $"deviceBest={Bob.TaskRace.BobTaskRaceProfile.BestPoints}");
            if (boardOffline) Check(Shows("ดีสุดในเครื่อง"), "offline board prints the device best");
            else Check(boardEmpty || RowCount() > 0, "the live board lands in one of its branches");
            Check(CardButton("ปิด") != null, "board card offers ปิด");
            yield return CaptureR1("card-board");
            yield return CaptureLargerText(Card(), "card-board");

            // The leaderboard service is not reachable from the editor, so the rows a signed-in player sees are fed in
            // here and drawn by the same Board → BoardRow path. Nothing is sent to the service, and the field goes back.
            var renderOverlay = typeof(BobGame).GetMethod("RenderOverlay", Private);
            var boardSnapWas = boardSnapField.GetValue(game);
            boardSnapField.SetValue(game, BoardFixture(10, null));
            renderOverlay.Invoke(game, null);
            yield return Drawn();
            CheckKit("ranked board");
            Check(RowCount() == 10, "a signed-in board prints one row per rank");
            Check(Shows("แถวสีเหลืองคือคุณ"), "the ranked board marks my own row");
            yield return CaptureR1("card-board-ranked");
            yield return CaptureLargerText(Card(), "card-board-ranked");
            boardSnapField.SetValue(game, BoardFixture(0, "no config"));
            renderOverlay.Invoke(game, null);
            yield return Drawn();
            Check(Shows("อ่านอันดับไม่ได้"), "a board the service refuses says so instead of an empty list");
            boardSnapField.SetValue(game, boardSnapWas);
            renderOverlay.Invoke(game, null);
            yield return Drawn();

            yield return Tap(CardButton("ปิด"));
            Check(OverlayName() == "None", "board ปิด returns to Home");

            var dailyDock = Dock("เดลี่");
            Check(dailyDock != null, "Home dock offers เดลี่");
            if (dailyDock == null) { Finish(); yield break; }
            Click(dailyDock.transform as RectTransform); yield return new WaitForEndOfFrame();
            Check(OverlayName() == "Daily", "dock เดลี่ opens the daily card");
            yield return WaitUntilSettled("กำลังถามเซิร์ฟเวอร์", "daily", 25f);
            CheckKit("daily");
            Check(Shows("เดลี่วันนี้"), "daily card keeps its title");
            bool served = Shows("วัน 20");
            Debug.Log($"[UIR1] daily branch: serverAnswered={served} url={PlayerPrefs.GetString(BobDailyFeed.UrlKey, "<default>")}");
            if (served)
            {
                Check(Shows("seed"), "daily card prints the seed the server handed out");
                Check(Shows("เหลือ") || Shows("เล่นแล้ววันนี้"), "daily card prints the rounds left today");
                Check(CardButton("เล่นเดลี่") != null || Shows("เปิดใหม่ตอน"), "daily card offers play, or names the refill");
            }
            else
            {
                Check(Shows("ออฟไลน์ · เล่นโหมดปกติไปก่อน"), "an unreachable daily says so and points at the normal mode");
                Check(CardButton("ลองใหม่") != null, "an unreachable daily offers a retry");
            }
            Check(CardButton("ปิด") != null, "daily card offers ปิด");
            yield return CaptureR1("card-daily");
            yield return CaptureLargerText(Card(), "card-daily");
            yield return Tap(CardButton("ปิด"));
            Check(OverlayName() == "None", "daily ปิด returns to Home");

            Open("Pause"); yield return new WaitForEndOfFrame();
            Check(OverlayName() == "Pause", "the pause card renders");
            CheckKit("pause");
            Check(Shows("หยุดเกม"), "pause card titles itself หยุดเกม");
            Check(CardButton("เล่นต่อ") != null && CardButton("ตั้งค่า") != null && CardButton("เริ่มแมตช์ใหม่") != null &&
                CardButton("ออกไปหน้าหลัก") != null, "pause card carries its four actions");
            yield return CaptureR1("card-pause");
            yield return CaptureLargerText(Card(), "card-pause");
            yield return Tap(CardButton("ตั้งค่า"));
            Check(OverlayName() == "Settings", "the pause card hands over to the settings card");
            CheckKit("settings");
            Check(Shows("ตั้งค่า"), "settings card titles itself ตั้งค่า");
            bool soundWas = Field<bool>(game, "soundOn"), musicWas = Field<bool>(game, "musicOn");
            yield return CaptureR1("card-settings");
            yield return CaptureLargerText(Card(), "card-settings");
            yield return Tap(CardButton("เสียงเอฟเฟกต์"));
            Check(Field<bool>(game, "soundOn") != soundWas, "the sound switch flips");
            Check(Shows("เสียงเอฟเฟกต์: " + (Field<bool>(game, "soundOn") ? "เปิด" : "ปิด")), "the sound switch says the state it is in");
            yield return Tap(CardButton("เสียงเอฟเฟกต์"));
            Check(Field<bool>(game, "soundOn") == soundWas, "the sound switch is restored");
            yield return Tap(CardButton("เพลง:"));
            Check(Field<bool>(game, "musicOn") != musicWas, "the music switch flips");
            yield return Tap(CardButton("เพลง:"));
            Check(Field<bool>(game, "musicOn") == musicWas, "the music switch is restored");
            // A failed flip check must not leave the profile with the wrong switches, so the settings are forced back
            // through the game's own setters before the card is closed.
            if (Field<bool>(game, "soundOn") != soundWas)
                typeof(BobGame).GetMethod("SetSound", Private).Invoke(game, new object[] { soundWas });
            if (Field<bool>(game, "musicOn") != musicWas)
                typeof(BobGame).GetMethod("SetMusic", Private).Invoke(game, new object[] { musicWas });
            Check(Field<bool>(game, "soundOn") == soundWas && Field<bool>(game, "musicOn") == musicWas,
                "the card run leaves the sound and music switches as it found them");
            yield return Tap(CardButton("ปิด"));
            Check(OverlayName() == "None", "settings ปิด returns to Home");

            var shopDock = Dock("ร้านค้า");
            Check(shopDock != null, "Home dock offers ร้านค้า");
            if (shopDock == null) { Finish(); yield break; }
            Click(shopDock.transform as RectTransform); yield return new WaitForEndOfFrame();
            Check(Field<object>(game, "phase").ToString() == "Shop", "dock ร้านค้า opens the shop");
            var wardrobeDock = screenRoot.GetComponentsInChildren<Button>().FirstOrDefault(b => b.isActiveAndEnabled &&
                b.GetComponentInChildren<TMP_Text>() != null &&
                b.GetComponentInChildren<TMP_Text>().text.StartsWith("ตู้เสื้อผ้า", StringComparison.Ordinal));
            Check(wardrobeDock != null, "the shop offers the wardrobe");
            if (wardrobeDock == null) { Finish(); yield break; }
            const string fixtureFrame = "frame_teal";
            string frameWas = BobCosmetics.FrameId, titleWas = BobCosmetics.TitleId, ownedWas = PlayerPrefs.GetString(BobCosmetics.OwnedKey, "");
            bool fixture = !BobCosmetics.IsOwned(fixtureFrame);
            if (fixture)
            {
                // One owned piece so the row that can be worn is on screen; the prefs go back exactly as they were below.
                owned.Add(fixtureFrame);
                PlayerPrefs.SetString(BobCosmetics.OwnedKey, ownedWas.Length == 0 ? fixtureFrame : ownedWas + "," + fixtureFrame);
                PlayerPrefs.Save();
            }
            yield return Drawn(screenRoot);
            Click(wardrobeDock.transform as RectTransform); yield return new WaitForEndOfFrame();
            Check(OverlayName() == "Wardrobe", "the shop button opens the wardrobe card");
            CheckKit("wardrobe");
            Check(Shows("ตู้เสื้อผ้า"), "wardrobe card titles itself ตู้เสื้อผ้า");
            Check(Shows("ปลดแล้ว"), "wardrobe header counts the pieces earned");
            Check(RowCount() == BobUnlocks.All.Length, "wardrobe shows one row per piece in the table");
            // The list scrolls, so the row is first brought into the viewport; any piece that is owned and not worn will
            // do — which row that is depends on what this profile has already earned.
            var wearRow = Card().GetComponentsInChildren<Image>().Where(i => i.name == "Row")
                .Select(i => new KeyValuePair<RectTransform, Button>(i.rectTransform, i.GetComponentsInChildren<Button>()
                    .FirstOrDefault(b => b.GetComponentInChildren<TMP_Text>()?.text.Trim() == "ใช้")))
                .FirstOrDefault(p => p.Value != null);
            Debug.Log($"[UIR1] wardrobe: fixture={fixture} wearRowFound={wearRow.Value != null} " +
                $"owned=\"{PlayerPrefs.GetString(BobCosmetics.OwnedKey, "")}\" worn={BobCosmetics.FrameId}/{BobCosmetics.TitleId}");
            Check(wearRow.Value != null, "an owned piece that is not worn offers ใช้");
            if (wearRow.Value != null)
            {
                string wornWas = BobCosmetics.FrameId + "/" + BobCosmetics.TitleId;
                yield return Reveal(wearRow.Key);
                yield return Tap(wearRow.Value);
                Check(BobCosmetics.FrameId + "/" + BobCosmetics.TitleId != wornWas, "ใช้ wears the piece");
                Check(Shows("ใช้อยู่"), "the worn row reads ใช้อยู่");
            }
            yield return CaptureR1("card-wardrobe");
            yield return CaptureLargerText(Card(), "card-wardrobe");
            yield return Tap(CardButton("ถอดทั้งหมด"));
            Check(BobCosmetics.FrameId.Length == 0 && BobCosmetics.TitleId.Length == 0, "ถอดทั้งหมด empties both slots");
            yield return Tap(CardButton("ปิด"));
            if (fixture)
            {
                owned.Remove(fixtureFrame);
                PlayerPrefs.SetString(BobCosmetics.OwnedKey, ownedWas);
                PlayerPrefs.Save();
            }
            if (frameWas.Length > 0) BobCosmetics.Equip(frameWas);
            if (titleWas.Length > 0) BobCosmetics.Equip(titleWas);
            Debug.Log($"[UIR1] cosmetics restored: frame=\"{BobCosmetics.FrameId}\" title=\"{BobCosmetics.TitleId}\" " +
                $"owned=\"{PlayerPrefs.GetString(BobCosmetics.OwnedKey, "")}\"");
            phaseField.SetValue(game, Enum.Parse(phaseType, "Title"));
            typeof(BobGame).GetMethod("RenderAll", Private).Invoke(game, null);
            yield return null;
            Check(!overlayRoot.gameObject.activeSelf, "every card is closed and Home is drawn again");
            Debug.Log(ok ? "[UIR1] CARD CHECKS PASSED" : "[UIR1] CARD CHECKS FAILED");
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
            foreach (var id in new[] { "sweep", "bubble", "spray", "press" })
            {
                var prefab = Resources.Load<GameObject>(BobTaskRaceTeammates.HeroResourcePath + id);
                Check(prefab != null && prefab.GetComponentsInChildren<Renderer>(true)
                    .Where(r => r.enabled).All(r => r.sharedMaterials.Length > 0 && r.sharedMaterials
                        .All(m => m != null && m.shader != null && m.shader.isSupported)),
                    "hero " + id + " has supported persistent materials");
            }
            Check(hud.RowCount == round.TaskCount, "all worksheet rows retained");
            var series = UnityEngine.Object.FindAnyObjectByType<BobSeriesBanner>();
            var seriesDim = series.GetComponentsInChildren<Image>(true).First(i => i.name == "SeriesDim");
            Check(!seriesDim.gameObject.activeInHierarchy, "series dim is hidden during the round");
            var pillCanvas = series.GetComponentsInChildren<Canvas>(true).First(c => c.name == "SeriesPillCanvas");
            Check(pillCanvas.isRootCanvas && pillCanvas.sortingOrder < hud.UiCanvas.sortingOrder,
                "series pill has a separate root canvas behind HUD cards");
            Check(hud.UiCanvas.renderMode == RenderMode.ScreenSpaceOverlay, "HUD renders after world post-processing");
            CheckBounds(Field<RectTransform>(hud, "card"), "worksheet");
            CheckBounds(Field<RectTransform>(hud, "topBar"), "score group");
            CheckBounds(Field<Button>(hud, "pauseButton").transform as RectTransform, "pause");
            var topBar = Field<RectTransform>(hud, "topBar");
            var paperSample = RectTransformUtility.WorldToScreenPoint(null,
                topBar.TransformPoint(new Vector2(topBar.rect.xMin + 16f, topBar.rect.center.y)));
            Color paperBefore = Color.clear;
            yield return CaptureR1("hud", texture => paperBefore = texture.GetPixel((int)paperSample.x, (int)paperSample.y));
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
            yield return DragWorksheetBottom();
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
            // The worn cosmetic frame has to reach the trolley (docs 31 §2.3): equip, rebuild the card, read the outline back.
            string wornFrame = PlayerPrefs.GetString(BobCosmetics.FrameKey, "");
            PlayerPrefs.SetString(BobCosmetics.FrameKey, "frame_teal");
            round.StartRound(1019); yield return null;
            spot = round.StationsOf(TaskSpot.LocalTeam).First(s => s.TaskId == "T04");
            if (!spot.TryOpen("ui-r1", out why))
            {
                Check(false, "tint fixture open " + why);
                PlayerPrefs.SetString(BobCosmetics.FrameKey, wornFrame);
                Finish(); yield break;
            }
            Check(hud.DebugOpenCard(spot), "tint fixture card opens"); yield return null;
            // The trolley is built when the card reaches the stacking step, so pick before reading its outline.
            foreach (string id in good) { Click(Part(mini, id)); yield return new WaitForSecondsRealtime(0.22f); }
            var trolleyZone = Field<Image>(mini, "cartDropZone");
            var trolley = trolleyZone != null ? trolleyZone.GetComponent<Outline>() : null;
            Check(trolley != null && CloseColour(trolley.effectColor, BobUi.Hex("#2F7F6F")),
                "trolley outline takes the worn frame colour");
            yield return CaptureR1("cart-frame-teal");
            PlayerPrefs.SetString(BobCosmetics.FrameKey, wornFrame);
            round.StartRound(1019); yield return null;
            spot = round.StationsOf(TaskSpot.LocalTeam).First(s => s.TaskId == "T04");
            if (!spot.TryOpen("ui-r1", out why)) { Check(false, "cancel fixture open " + why); Finish(); yield break; }
            Check(hud.DebugOpenCard(spot), "joystick cancel fixture opens");
            yield return null;
            var zone = Field<RectTransform>(touch, "zone");
            stickHit = Hit(RectTransformUtility.WorldToScreenPoint(null, zone.TransformPoint(new Vector2(24f, 24f))), out stickPointer);
            if (stickHit == null) { Finish(); yield break; }
            stickReceiver = ExecuteEvents.ExecuteHierarchy(stickHit, stickPointer, ExecuteEvents.pointerDownHandler);
            Check(touch.IsHeld, "joystick receives touch above minigame dim");
            stickPointer.position += Vector2.right * 150f * hud.UiCanvas.scaleFactor;
            ExecuteEvents.ExecuteHierarchy(stickReceiver, stickPointer, ExecuteEvents.dragHandler);
            yield return null; yield return null;
            Check(!hud.CardOpen && !spot.IsOpen, "joystick still cancels open station card");
            ExecuteEvents.ExecuteHierarchy(stickReceiver, stickPointer, ExecuteEvents.pointerUpHandler);
            while (round.TaskCount < 10) round.AddPenaltyTask(round.Tasks[0].Spec, TaskSpot.LocalTeam);
            yield return DragWorksheetBottom();
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
            int orderWas = hud.UiCanvas.sortingOrder;
            int layerWas = hud.UiCanvas.sortingLayerID;
            Shot(Path.Combine(Environment.GetEnvironmentVariable("BOB_R1_OUT") ?? "Logs/ui-r1", "camera-capture.png"));
            Check(hud.UiCanvas.renderMode == RenderMode.ScreenSpaceOverlay && hud.UiCanvas.worldCamera == cameraWas &&
                hud.UiCanvas.sortingOrder == orderWas && hud.UiCanvas.sortingLayerID == layerWas,
                "RenderTexture capture restores overlay, camera and sorting");
            yield return null;
            Click(Field<Button>(hud, "cardClose").transform as RectTransform); yield return null;
            Check(!hud.CardOpen, "close exits emergency card");
            rig.yaw = yaw;
            Check(manager.Series != null && manager.Series.Round == 1, "round overlay fixture remains in the first round");
            if (manager.Series == null || manager.Series.Round != 1) { Finish(); yield break; }
            manager.DebugFinishRound();
            var roundCard = Field<RectTransform>(series, "card");
            Check(seriesDim.gameObject.activeInHierarchy && roundCard.gameObject.activeInHierarchy,
                "round end shows both series card and dim");
            Check(roundCard.GetComponentInParent<Canvas>().sortingOrder > hud.UiCanvas.sortingOrder,
                "between-round card renders above HUD");
            CheckBounds(roundCard, "between-round card");
            yield return CaptureR1("series-round-end", texture =>
            {
                var after = texture.GetPixel((int)paperSample.x, (int)paperSample.y);
                Debug.Log($"[UIR1] series paper before={paperBefore} after={after}");
                foreach (var c in UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsInactive.Exclude))
                    Debug.Log($"[UIR1] canvas {c.name}: root={c.isRootCanvas} mode={c.renderMode} layer={c.sortingLayerID} order={c.sortingOrder} rendered={c.renderOrder}");
                Check(after.r < paperBefore.r - 0.08f && after.g < paperBefore.g - 0.08f && after.b < paperBefore.b - 0.08f,
                    "captured series dim darkens HUD paper, not only the world");
            });
            yield return new WaitForSecondsRealtime(manager.nextRoundDelay + 0.2f);
            Check(manager.Series.Round == 2 && !seriesDim.gameObject.activeInHierarchy && !roundCard.gameObject.activeInHierarchy,
                "next round hides both series card and dim");
            yield return CaptureR1("series-next-round");
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
            SessionState.SetBool(CardsKey, false);
            EditorApplication.update -= Tick;
            Debug.Log(ok ? "[TaskRaceShots] SHOTS PASSED" : "[TaskRaceShots] SHOTS FAILED");
            EditorApplication.ExitPlaymode();
            EditorApplication.delayCall += () => EditorApplication.Exit(ok ? 0 : 1);
        }
    }
}
