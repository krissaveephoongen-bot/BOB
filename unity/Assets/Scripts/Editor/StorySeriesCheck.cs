// Check for the story series of docs 24 section 15.3 P3. First the rules on their own, then one real chapter in Play
// mode: every round is force-closed, so the strip, the round card and the chapter result can be screenshotted.
//   -executeMethod Bob.EditorTools.StorySeriesCheck.Run     (Editor closed on this project)
// Success = the Console shows "[SeriesCheck] SERIES CHECK PASSED"; shots go to unity/Logs/series-*.png.
using System.IO;
using Bob.TaskRace;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Bob.EditorTools
{
    public static class StorySeriesCheck
    {
        const string ArmKey = "Bob.SeriesCheck.Arm";

        [MenuItem("Bob/Run Story Series Check")]
        public static void Run()
        {
            failures = 0;
            CheckRules();
            entered = false; step = 0;
            SessionState.SetBool(ArmKey, true);
            EditorSceneManager.OpenScene(TaskRaceSceneBuilder.ScenePath);
            quietFrom = EditorApplication.timeSinceStartup;
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
        }

        // ---------------------------------------------------------------- rules (no play mode)

        static int failures;

        static void CheckRules()
        {
            var lost = new StorySeries(100f, 2);
            lost.Register(6, 4, 600, 400);
            lost.Register(5, 7, 500, 700);
            var third = lost.Register(2, 8, 200, 800);
            Check("three rounds", third.Number == 3 && lost.Over);
            Check("series 1-2 lost", !lost.LocalTookSeries && lost.Score == "1-2");
            Check("round clock 100 s", lost.SecondsForRound == 100f);

            var blowout = new StorySeries(100f, 2);
            var won = blowout.Register(9, 5, 900, 500);
            Check("win by 4 opens the challenge", won.Outcome == RoundOutcome.Win && blowout.ChallengeOffered);

            var streak = new StorySeries(100f, 2);
            streak.Register(4, 6, 400, 600);
            var tie = streak.Register(5, 5, 500, 700);
            Check("points break a task tie", tie.Outcome == RoundOutcome.Loss);
            Check("two losses end the chapter", streak.Over && streak.LocalWins == 0 && streak.RivalWins == 2);

            var draw = new StorySeries(100f, 2);
            var equal = draw.Register(3, 3, 300, 300);
            Check("equal tasks and points draw", equal.Outcome == RoundOutcome.Draw && !draw.Over && draw.Round == 2);
            draw.Register(3, 3, 300, 300);
            Check("two draws still play round three", !draw.Over && draw.Round == 3);
            var lastDraw = draw.Register(3, 3, 300, 300);
            Check("three draws end the chapter level", draw.Over && lastDraw.Number == 3 && draw.Score == "0-0");

            Debug.Log(failures == 0 ? "[SeriesCheck] rules PASSED" : $"[SeriesCheck] rules FAILED ({failures})");
        }

        static void Check(string name, bool ok)
        {
            if (!ok) failures++;
            Debug.Log($"[SeriesCheck] {name}: {(ok ? "ok" : "FAIL")}");
        }

        // ---------------------------------------------------------------- one chapter in play mode

        static double quietFrom;
        static bool entered;
        static int step;
        static float stepAt;
        static MeshRenderer fieldOff, soilOff;
        static BobFieldDressing dressing;
        static BobCloudSea clouds;
        static bool fogWas;

        /// <summary>Entering Play mode reloads the domain and drops the update hook, so arm it again from SessionState.</summary>
        [InitializeOnLoadMethod]
        static void Rehook()
        {
            if (!SessionState.GetBool(ArmKey, false)) return;
            if (EditorApplication.isPlaying) entered = true;
            else quietFrom = EditorApplication.timeSinceStartup;
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
        }

        static void Tick()
        {
            if (!SessionState.GetBool(ArmKey, false)) { EditorApplication.update -= Tick; return; }

            if (!entered)
            {
                if (EditorApplication.isCompiling || EditorApplication.isUpdating) quietFrom = EditorApplication.timeSinceStartup;
                if (EditorApplication.timeSinceStartup - quietFrom < 12) return;
                entered = true;
                EditorApplication.EnterPlaymode();
                return;
            }
            if (!EditorApplication.isPlaying) return;

            var manager = TaskRaceModeManager.Instance;
            if (manager == null || manager.Series == null) return;

            // Screenshots are written at the end of the frame they are asked for, so every capture gets a step of its
            // own and the action it is meant to show happens a frame later.
            if (step == 0 && Time.timeSinceLevelLoad > 2f)
            {
                var banner = Object.FindAnyObjectByType<BobSeriesBanner>();
                Debug.Log($"[SeriesCheck] start: clock {manager.RemainingSeconds:0} s · round {manager.Series.Round}/{StorySeries.Rounds} · " +
                          $"series {manager.Series.Score} · banner {banner != null} · storySeries {manager.storySeries}");
                LogRenderState();
                Capture("1-start");
                // The play shots show the island top as flat magenta. Hide the Field cap for one frame: if the slab goes,
                // it is that renderer; if it stays, something drawn over it is the one with the bad material.
                var field = GameObject.Find("TaskRaceMap/TeamA/Field");
                fieldOff = field != null ? field.GetComponent<MeshRenderer>() : null;
                if (fieldOff != null) fieldOff.enabled = false;
                stepAt = Time.unscaledTime;
                step = 1;
            }
            else if (step == 1 && Time.unscaledTime > stepAt + 0.3f)
            {
                Debug.Log($"[SeriesCheck] field cap hidden: {(fieldOff != null ? "yes" : "not found")}");
                Capture("1b-field-off");
                if (fieldOff != null) fieldOff.enabled = true;
                // The slab survives the Field being hidden, so it is drawn over it. Hide the layers that sit on the
                // field, one at a time, until the slab's owner is named.
                var soil = GameObject.Find("TaskRaceMap/TeamA/Field_Soil");
                soilOff = soil != null ? soil.GetComponent<MeshRenderer>() : null;
                if (soilOff != null) soilOff.enabled = false;
                stepAt = Time.unscaledTime;
                step = 2;
            }
            else if (step == 2 && Time.unscaledTime > stepAt + 0.3f)
            {
                Debug.Log($"[SeriesCheck] Field_Soil hidden: {(soilOff != null ? "yes" : "not found")} " +
                          $"(material {(soilOff != null && soilOff.sharedMaterial != null ? soilOff.sharedMaterial.name : "null")})");
                Capture("1c-soil-off");
                if (soilOff != null) soilOff.enabled = true;
                dressing = GameObject.Find("TaskRaceMap/TeamA/Field")?.GetComponent<BobFieldDressing>();
                if (dressing != null) dressing.enabled = false;
                stepAt = Time.unscaledTime;
                step = 3;
            }
            else if (step == 3 && Time.unscaledTime > stepAt + 0.3f)
            {
                Debug.Log($"[SeriesCheck] field dressing off: {(dressing != null ? "yes" : "not found")}");
                Capture("1d-dressing-off");
                if (dressing != null) dressing.enabled = true;
                clouds = Object.FindAnyObjectByType<BobCloudSea>();
                if (clouds != null) clouds.enabled = false;
                stepAt = Time.unscaledTime;
                step = 4;
            }
            else if (step == 4 && Time.unscaledTime > stepAt + 0.3f)
            {
                Debug.Log($"[SeriesCheck] cloud sea off: {(clouds != null ? "yes" : "not found")}");
                Capture("1e-clouds-off");
                if (clouds != null) clouds.enabled = true;
                // Only the CurvedWorld surfaces go magenta, which is what fog would do to that shader alone: name the
                // fog settings, then shoot the same frame with fog off.
                Debug.Log($"[SeriesCheck] fog {RenderSettings.fog} {RenderSettings.fogMode} colour " +
                          $"{RenderSettings.fogColor} start {RenderSettings.fogStartDistance:0.0} end {RenderSettings.fogEndDistance:0.0} " +
                          $"density {RenderSettings.fogDensity:0.000} · ambient {RenderSettings.ambientMode} {RenderSettings.ambientLight}");
                fogWas = RenderSettings.fog;
                RenderSettings.fog = false;
                stepAt = Time.unscaledTime;
                step = 5;
            }
            else if (step == 5 && Time.unscaledTime > stepAt + 0.3f)
            {
                Debug.Log("[SeriesCheck] fog off shot");
                Capture("1f-nofog");
                RenderSettings.fog = fogWas;
                stepAt = Time.unscaledTime;
                step = 6;
            }
            else if (step == 6 && Time.unscaledTime > stepAt + 0.3f)
            {
                manager.DebugFinishRound();
                stepAt = Time.unscaledTime;
                step = 7;
            }
            else if (step == 7 && Time.unscaledTime > stepAt + 0.6f)
            {
                Debug.Log($"[SeriesCheck] round card: {manager.LastRound}");
                Capture("2-round1");
                stepAt = Time.unscaledTime;
                step = 8;
            }
            else if (step == 8 && Time.unscaledTime > stepAt + manager.nextRoundDelay + 0.6f)
            {
                Debug.Log($"[SeriesCheck] round {manager.Series.Round} running: clock {manager.RemainingSeconds:0} s");
                Capture("3-round2");
                stepAt = Time.unscaledTime;
                step = 9;
            }
            else if (step == 9 && Time.unscaledTime > stepAt + 0.5f)
            {
                manager.DebugFinishRound();
                stepAt = Time.unscaledTime;
                step = 10;
            }
            else if (step == 10 && Time.unscaledTime > stepAt + manager.nextRoundDelay + 0.6f)
            {
                Debug.Log($"[SeriesCheck] round {manager.Series.Round} running: clock {manager.RemainingSeconds:0} s");
                Capture("4-round3");
                stepAt = Time.unscaledTime;
                step = 11;
            }
            else if (step == 11 && Time.unscaledTime > stepAt + 0.5f)
            {
                manager.DebugFinishRound();
                stepAt = Time.unscaledTime;
                step = 12;
            }
            else if (step == 12 && Time.unscaledTime > stepAt + 1.2f)
            {
                var hud = Object.FindAnyObjectByType<BobTaskRaceHud>();
                bool ok = manager.LastResult.HasValue && hud != null && hud.ResultOpen && manager.Series.Over &&
                          manager.Series.LastRound?.Number == StorySeries.Rounds;
                Debug.Log($"[SeriesCheck] chapter end: last round {manager.Series.LastRound?.Number} · result {manager.LastResult} · " +
                          $"overlay {hud?.ResultOpen} · series {manager.Series.Score}");
                Capture("5-result");
                finalOk = ok;
                stepAt = Time.unscaledTime;
                step = 13;
            }
            else if (step == 13 && Time.unscaledTime > stepAt + 1f)
            {
                Debug.Log(finalOk ? "[SeriesCheck] SERIES CHECK PASSED" : "[SeriesCheck] SERIES CHECK FAILED");
                SessionState.SetBool(ArmKey, false);
                EditorApplication.update -= Tick;
                EditorApplication.ExitPlaymode();
                EditorApplication.delayCall += () => EditorApplication.Exit(finalOk ? 0 : 1);
            }
        }

        static bool finalOk;

        /// <summary>Name the shaders the map asks for and any renderer that will draw the magenta error material, so a
        /// missing shader in play mode is a log line instead of a purple slab in the screenshot.</summary>
        static void LogRenderState()
        {
            var cam = Camera.main;
            Debug.Log($"[SeriesCheck] camera {(cam != null ? cam.transform.position.ToString("0.0") + " fwd " + cam.transform.forward.ToString("0.0") : "missing")}");
            foreach (var name in new[] { "BOB/CloudSea", "BOB/CurvedWorld", "BOB/Dressing", "BOB/GroundAO", "BOB/SkyNDC", "BOB/PostGrade", "BOB/Unlit", "BOB/ToonLit" })
            {
                var shader = Shader.Find(name);
                Debug.Log($"[SeriesCheck] shader {name}: {(shader == null ? "MISSING" : shader.isSupported ? "ok" : "UNSUPPORTED")}");
            }

            int bad = 0;
            foreach (var r in Resources.FindObjectsOfTypeAll<Renderer>())
            {
                if (r == null) continue;
                var shader = r.sharedMaterial != null ? r.sharedMaterial.shader : null;
                if (shader != null && shader.isSupported) continue;
                bad++;
                if (bad <= 12)
                    Debug.LogWarning($"[SeriesCheck] no working shader: \"{r.name}\" on \"{r.gameObject.name}\" at " +
                                     $"{r.transform.position.ToString("0.0")} material {(r.sharedMaterial != null ? r.sharedMaterial.name : "null")} " +
                                     $"shader {(shader != null ? shader.name : "null")}");
            }
            Debug.Log($"[SeriesCheck] renderers without a working shader: {bad}");

            // The one surface the play shots show as magenta: name its material and the shader that is actually bound.
            foreach (var path in new[] { "TaskRaceMap/TeamA/Field", "TaskRaceMap/TeamB/Field" })
            {
                var go = GameObject.Find(path);
                var mr = go != null ? go.GetComponent<MeshRenderer>() : null;
                var mat = mr != null ? mr.sharedMaterial : null;
                Debug.Log($"[SeriesCheck] {path}: renderer {mr != null} material {(mat != null ? mat.name : "null")} " +
                          $"shader {(mat != null && mat.shader != null ? mat.shader.name : "null")} " +
                          $"supported {(mat != null && mat.shader != null && mat.shader.isSupported)} " +
                          $"curvature {Shader.GetGlobalFloat("_BobCurvature"):0.0000}");
            }
            // The magenta surface is neither Field nor Field_Soil (both hidden with no change): ask the colliders what
            // the camera is actually looking at, then all TeamA renderers so the material carrying the magenta is named.
            if (cam != null)
            {
                var hits = Physics.RaycastAll(cam.ViewportPointToRay(new Vector3(0.55f, 0.55f)), 200f);
                Debug.Log($"[SeriesCheck] raycast hits: {hits.Length}");
                for (int i = 0; i < Mathf.Min(6, hits.Length); i++)
                {
                    var mr = hits[i].collider.GetComponent<MeshRenderer>() ?? hits[i].collider.GetComponentInParent<MeshRenderer>();
                    var mat = mr != null ? mr.sharedMaterial : null;
                    Debug.Log($"[SeriesCheck] hit {i}: \"{hits[i].collider.name}\" at {hits[i].point.ToString("0.0")} " +
                              $"renderer {(mr != null ? mr.name : "none")} material {(mat != null ? mat.name : "null")} " +
                              $"shader {(mat != null && mat.shader != null ? mat.shader.name : "null")} " +
                              $"tex {(mat != null && mat.mainTexture != null ? mat.mainTexture.name : "null")} " +
                              $"colour {(mat != null && mat.HasProperty("_Color") ? mat.GetColor("_Color").ToString() : "n/a")}");
                }
            }
            var teamA = GameObject.Find("TaskRaceMap/TeamA");
            if (teamA != null)
            {
                var dumped = 0;
                foreach (var mr in teamA.GetComponentsInChildren<MeshRenderer>(true))
                {
                    if (dumped >= 40) break;
                    dumped++;
                    var mats = mr.sharedMaterials;
                    var names = string.Join(",", System.Array.ConvertAll(mats, m => m == null ? "null" :
                        m.name + ":" + (m.shader != null ? m.shader.name : "no-shader") + ":" +
                        (m.mainTexture != null ? m.mainTexture.name : "no-tex")));
                    Debug.Log($"[SeriesCheck] TeamA renderer \"{mr.name}\" bounds {mr.bounds.size.ToString("0.0")} mats [{names}]");
                }
            }

            int errorShader = 0;
            foreach (var r in Resources.FindObjectsOfTypeAll<Renderer>())
            {
                var shader = r != null && r.sharedMaterial != null ? r.sharedMaterial.shader : null;
                if (shader == null || !shader.name.Contains("ErrorShader")) continue;
                errorShader++;
                if (errorShader <= 8)
                    Debug.LogWarning($"[SeriesCheck] error shader: \"{r.name}\" on \"{r.gameObject.name}\" at {r.transform.position.ToString("0.0")} " +
                                     $"material {r.sharedMaterial.name}");
            }
            Debug.Log($"[SeriesCheck] renderers on the error shader: {errorShader}");
        }

        static void Capture(string name) =>
            ScreenCapture.CaptureScreenshot(Path.Combine("Logs", "series-" + name + ".png"));
    }
}
