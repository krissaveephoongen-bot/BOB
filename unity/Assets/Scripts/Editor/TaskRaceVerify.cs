// Battle of the Brooms: one-shot check for the Task Race scenes (docs 24).
//   -executeMethod Bob.EditorTools.TaskRaceVerify.Run       (solo)
//   -executeMethod Bob.EditorTools.TaskRaceVerify.RunDuo
//   -executeMethod Bob.EditorTools.TaskRaceVerify.RunTeam
// Reports the map pieces, task spot ids and both modes (barrier obstacles vs bridge links), then renders previews.
using System.IO;
using System.Linq;
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
                $"{s.SpotId}({s.Category},{s.ExpectedSeconds:0}s)@({s.transform.position.x:0.0},{s.transform.position.z:0.0})")));
            Debug.Log("[TaskRaceVerify] baked tasks: " + string.Join(", ", spots.Select(s =>
                $"{s.SpotId}={s.TaskName}({s.ExpectedSeconds:0.#}s)")));

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
            manager.SetMode(TaskRaceMode.BridgeRaid);
            var links = Object.FindObjectsByType<NavMeshLink>(FindObjectsInactive.Include)
                .Where(l => l.gameObject.transform.root == map.transform).ToArray();
            Debug.Log($"[TaskRaceVerify] BridgeRaid: barrier active={manager.riverBarrierGroup.activeSelf} " +
                      $"bridges active={manager.bridgesGroup.activeSelf} links={links.Length} ({linksBefore} before)");
            if (hero != null && farSpot != null) CheckNav("BridgeRaid", hero.transform.position, farSpot.transform.position);
            if (manager.bridgesGroup != null)
                foreach (Transform bridge in manager.bridgesGroup.transform)
                    ProbeBridge(bridge);

            manager.SetMode(TaskRaceMode.PureRace);
            var obstacles = Object.FindObjectsByType<NavMeshObstacle>(FindObjectsInactive.Include)
                .Where(o => o.gameObject.transform.root == map.transform).ToArray();
            Debug.Log($"[TaskRaceVerify] PureRace: barrier active={manager.riverBarrierGroup.activeSelf} " +
                      $"bridges active={manager.bridgesGroup.activeSelf} obstacles={obstacles.Length} " +
                      $"({obstaclesBefore} before) carving={obstacles.Count(o => o.carving)}");
            if (hero != null && farSpot != null) CheckNav("PureRace", hero.transform.position, farSpot.transform.position);
            manager.SetMode(mode);

            Render(suffix);
            Debug.Log($"[TaskRaceVerify] {scenePath} done.");
        }

        /// <summary>Prove the mode's bake: the path from the hero to a Team B spot must cross in BridgeRaid and must not in PureRace.</summary>
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

        /// <summary>Where the bridge ends land relative to the baked NavMesh: the join that makes BridgeRaid walkable.</summary>
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

        static void Render(string suffix)
        {
            Shot($"Logs/taskrace{suffix}-preview.png", new Vector3(0f, 10.5f, -15f), new Vector3(0f, 0.4f, 0.5f), 42f);
            Shot($"Logs/taskrace{suffix}-wide.png", new Vector3(0f, 16f, -26f), new Vector3(0f, 0.5f, 0f), 50f);

            // The shot the player actually gets: BobTaskRaceCamera's 8.5 up / 7.5 back / 40 degree lens on the hero.
            GameObject hero = null;
            try { hero = GameObject.FindGameObjectWithTag("Player"); } catch (UnityException) { }
            if (hero != null)
                Shot($"Logs/taskrace{suffix}-play.png", hero.transform.position + new Vector3(0f, 8.5f, -7.5f),
                    hero.transform.position + new Vector3(0f, 0.6f, 0f), 40f);
        }

        static void Shot(string file, Vector3 eye, Vector3 look, float fov)
        {
            var main = Camera.main;
            var go = new GameObject("VerifyCam", typeof(Camera));
            var cam = go.GetComponent<Camera>();
            if (main != null) cam.CopyFrom(main);
            cam.enabled = false;
            cam.transform.position = eye;
            cam.transform.LookAt(look);
            cam.fieldOfView = fov;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.72f, 0.88f, 0.98f);

            var rt = new RenderTexture(1600, 900, 24, RenderTextureFormat.ARGB32) { antiAliasing = 2 };
            cam.targetTexture = rt;
            cam.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(1600, 900, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, 1600, 900), 0, 0);
            tex.Apply();
            Directory.CreateDirectory("Logs");
            File.WriteAllBytes(Path.GetFullPath(file), tex.EncodeToPNG());
            RenderTexture.active = null;
            cam.targetTexture = null;
            Object.DestroyImmediate(rt);
            Object.DestroyImmediate(tex);
            Object.DestroyImmediate(go);
            Debug.Log($"[TaskRaceVerify] preview written to {file}");
        }
    }
}
