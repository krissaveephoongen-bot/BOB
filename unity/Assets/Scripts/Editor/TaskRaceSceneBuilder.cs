// Battle of the Brooms: builds the Task Race scenes (docs 24) from the Task Race Map Assembler.
// One command does what the Inspector guide lists by hand: camera + hero + light, the twin-island map with real
// bob_map_* prefabs, the mode's station set on Team A cloned to Team B, and the scene added to Build Settings.
//   Tools > BOB > Build Task Race Scene (solo / duo / team)
//   -executeMethod Bob.EditorTools.TaskRaceSceneBuilder.Build        (solo, the default scene)
//   -executeMethod Bob.EditorTools.TaskRaceSceneBuilder.BuildDuo
//   -executeMethod Bob.EditorTools.TaskRaceSceneBuilder.BuildTeam
// The three modes share every rule; only the map size and the number of stations differ (docs 24 section 13).
using System.IO;
using System.Linq;
using Bob.TaskRace;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;

namespace Bob.EditorTools
{
    public static class TaskRaceSceneBuilder
    {
        public const string ScenePath = "Assets/Scenes/TaskRace.unity";          // solo
        public const string DuoScenePath = "Assets/Scenes/TaskRace_Duo.unity";   // 1v1
        public const string TeamScenePath = "Assets/Scenes/TaskRace_Team.unity"; // 2v2 / 3v3
        static readonly string[] BuildOrder = { ScenePath, DuoScenePath, TeamScenePath };
        const string StationMaterialPath = "Assets/Resources/Materials/M_BOB_Station.mat";

        // Station positions are authored for the 12 u island and scaled by islandSize / 12, so every mode keeps the
        // same relative layout on its field (92% of the field ellipse is usable after the counter footprint).
        static readonly StationSpec[] SoloStations =
        {
            new StationSpec("S1_A", "ล้างเครื่องชงกาแฟ", TaskCategory.Wash, 12f, new Vector3(-3f, 0f, -2f),
                ("espresso_machine", new Vector3(0f, 0.63f, 0f), 270f)),
            new StationSpec("S2_A", "ขัดพื้นทางเดิน", TaskCategory.Scrub, 16f, new Vector3(0.5f, 0f, 1.5f),
                ("bucket_metal_blue", new Vector3(-0.38f, 0.63f, 0.1f), 270f),
                ("brush_scrub_wood", new Vector3(0.32f, 0.63f, 0f), 250f)),
            new StationSpec("S3_A", "กวาดใบไม้", TaskCategory.Sweep, 10f, new Vector3(3f, 0f, -1f),
                ("basket_clothes", new Vector3(0f, 0.63f, 0f), 270f),
                ("brush_pair_yellow", new Vector3(0.95f, 0f, 0.45f), 300f)),
        };

        static readonly StationSpec[] DuoStations = SoloStations.Concat(new[]
        {
            new StationSpec("S4_A", "ฉีดน้ำยาเช็ดกระจก", TaskCategory.Spray, 14f, new Vector3(-3f, 0f, 1.5f),
                ("spray_bottle", new Vector3(-0.3f, 0.63f, 0f), 260f),
                ("gloves_pair", new Vector3(0.35f, 0.63f, 0.05f), 280f)),
        }).ToArray();

        static readonly StationSpec[] TeamStations = DuoStations.Concat(new[]
        {
            new StationSpec("S5_A", "ล้างถังน้ำยา", TaskCategory.Wash, 20f, new Vector3(3.2f, 0f, 1.8f),
                ("bucket_blue", new Vector3(-0.3f, 0.63f, 0f), 270f),
                ("detergent_pump", new Vector3(0.32f, 0.63f, 0f), 250f)),
            new StationSpec("S6_A", "เก็บขยะและใบไม้", TaskCategory.Everyone, 18f, new Vector3(-0.5f, 0f, -2.4f),
                ("trash_bag_black", new Vector3(0f, 0.63f, 0f), 270f),
                ("brush_pair_yellow", new Vector3(0.9f, 0f, 0.5f), 300f)),
        }).ToArray();

        [MenuItem("Tools/BOB/Build Task Race Scene (solo)")]
        public static void Build() => BuildVariant(ScenePath, 12f, 16f, SoloStations, 1);

        [MenuItem("Tools/BOB/Build Task Race Scene (duo)")]
        public static void BuildDuo() => BuildVariant(DuoScenePath, 15f, 20f, DuoStations, 2);

        [MenuItem("Tools/BOB/Build Task Race Scene (team)")]
        public static void BuildTeam() => BuildVariant(TeamScenePath, 18f, 24f, TeamStations, 3);

        static void BuildVariant(string scenePath, float islandSize, float islandGap, StationSpec[] stations, int players)
        {
            Directory.CreateDirectory("Assets/Scenes");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            RenderSettings.skybox = null;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.68f, 0.82f, 0.96f);
            RenderSettings.ambientEquatorColor = new Color(0.82f, 0.86f, 0.9f);
            RenderSettings.ambientGroundColor = new Color(0.45f, 0.40f, 0.55f);

            var sunGo = new GameObject("Sun", typeof(Light));
            var sun = sunGo.GetComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = new Color(1f, 0.96f, 0.88f);
            sun.intensity = 1.15f;
            sun.shadows = LightShadows.Soft;
            sunGo.transform.rotation = Quaternion.Euler(48f, -35f, 0f);

            var camGo = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener)) { tag = "MainCamera" };
            var cam = camGo.GetComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.72f, 0.88f, 0.98f);
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 400f;
            camGo.transform.position = new Vector3(-islandGap * 0.5f, 9f, -7f);

            var hero = BuildHero(ChooseSpawn(-islandGap * 0.5f, islandSize, stations));

            // The window's own builder: prefabs, flat walk colliders, mode groups, camera rig, curved materials.
            var assembler = ScriptableObject.CreateInstance<BobMapAssemblerEditor>();
            assembler.LoadDefaultPrefabs();
            assembler.SetLayout(islandSize, islandGap);
            assembler.BuildMap();

            var map = GameObject.Find("TaskRaceMap");
            if (map == null) { Debug.LogError("[TaskRaceScene] the map was not built: run Bob > Art > Build Map Prefabs first."); return; }

            var propsA = map.transform.Find("TeamA/Props");
            if (propsA == null) { Debug.LogError("[TaskRaceScene] TeamA/Props is missing; the map skeleton did not build."); return; }
            var mat = StationMaterialAsset();
            float k = islandSize / 12f;
            foreach (var spec in stations) AddStation(propsA, spec, k, mat);
            assembler.CloneTeamA();

            var rig = camGo.GetComponent<BobTaskRaceCamera>();
            if (rig != null) rig.SetTarget(hero.transform, true);

            // The round director: seeded list of docs 16 section 2.2 sized for this mode. Authoring it here bakes the
            // planned minigame names into the scene, so the editor view matches what the players will see.
            var systems = map.transform.Find("Systems");
            if (systems != null)
            {
                var round = systems.gameObject.AddComponent<TaskRaceRound>();
                round.ConfigureRound(players, 1, 1975);
                round.StartRound(1975);
            }
            else Debug.LogWarning("[TaskRaceScene] Systems object missing: no TaskRaceRound; stations keep their fixed tasks.");

            TagWalkables(map);
            BakeModes(map, scenePath);

            EditorSceneManager.SaveScene(scene, scenePath);
            // the three modes sit together in Build Settings, always in solo -> duo -> team order
            var order = BuildOrder.ToList();
            var current = EditorBuildSettings.scenes.ToList();
            int first = current.FindIndex(s => order.Contains(s.path));
            var others = current.Where(s => !order.Contains(s.path)).ToList();
            if (first < 0) first = others.Count;
            var modes = order.Where(p => p == scenePath || current.Any(s => s.path == p))
                             .Select(p => new EditorBuildSettingsScene(p, true));
            EditorBuildSettings.scenes = others.Take(first).Concat(modes).Concat(others.Skip(first)).ToArray();
            AssetDatabase.SaveAssets();

            var spots = Object.FindObjectsByType<TaskSpot>(FindObjectsInactive.Exclude);
            Debug.Log($"[TaskRaceScene] {scenePath} built: {spots.Length} task spot(s), island {islandSize} u, gap {islandGap} u, " +
                      $"map prefabs {(assembler.islandPrefab != null ? "real" : "placeholder")}, added to Build Settings.");
            Selection.activeGameObject = map;
        }

        /// <summary>Only the Field slabs and the bridge decks may enter the NavMesh; hedges, cliffs and decor may not.</summary>
        static void TagWalkables(GameObject map)
        {
            int kept = 0, ignored = 0;
            foreach (var col in map.GetComponentsInChildren<Collider>(true))
            {
                bool walkable = col.gameObject.name == "Field" || col.gameObject.name == "Walkable";
                var mod = col.GetComponent<NavMeshModifier>();
                if (mod == null) mod = col.gameObject.AddComponent<NavMeshModifier>();
                mod.ignoreFromBuild = !walkable;
                if (walkable) kept++; else ignored++;
            }
            Debug.Log($"[TaskRaceScene] navmesh geometry: {kept} walkable collider(s), {ignored} ignored.");
        }

        /// <summary>Bake one NavMesh per mode (docs 24 section 11) and hand both to the manager.</summary>
        static void BakeModes(GameObject map, string scenePath)
        {
            var manager = map.GetComponentInChildren<TaskRaceModeManager>();
            var surface = map.GetComponent<NavMeshSurface>();
            if (manager == null || surface == null)
            {
                Debug.LogWarning("[TaskRaceScene] no NavMeshSurface or mode manager: NavMesh not baked.");
                return;
            }

            Directory.CreateDirectory("Assets/Scenes/NavMesh");
            string stem = Path.GetFileNameWithoutExtension(scenePath);
            var previous = manager.mode;

            manager.SetMode(TaskRaceMode.BridgeRaid);
            var raid = Bake(surface, $"Assets/Scenes/NavMesh/{stem}_BridgeRaid.asset");
            manager.SetMode(TaskRaceMode.PureRace);
            var pure = Bake(surface, $"Assets/Scenes/NavMesh/{stem}_PureRace.asset");

            manager.pureNavMesh = pure;
            manager.raidNavMesh = raid;
            surface.navMeshData = pure;
            EditorUtility.SetDirty(surface);
            EditorUtility.SetDirty(manager);

            // NavMesh.CalculateTriangulation reads whatever bake is added right now, so switch once per mode to measure.
            manager.SetMode(TaskRaceMode.PureRace);
            int pureTri = NavMesh.CalculateTriangulation().indices.Length / 3;
            manager.SetMode(TaskRaceMode.BridgeRaid);
            int raidTri = NavMesh.CalculateTriangulation().indices.Length / 3;
            manager.SetMode(previous);

            Debug.Log($"[TaskRaceScene] navmesh baked: PureRace {pureTri} tri, BridgeRaid {raidTri} tri " +
                      $"(bridge decks add {raidTri - pureTri}), saved as Assets/Scenes/NavMesh/{stem}_*.asset.");
        }

        static NavMeshData Bake(NavMeshSurface surface, string assetPath)
        {
            surface.BuildNavMesh();
            var data = surface.navMeshData;
            if (data == null) { Debug.LogError($"[TaskRaceScene] NavMesh bake returned no data for {assetPath}."); return null; }
            if (AssetDatabase.LoadAssetAtPath<NavMeshData>(assetPath) != null) AssetDatabase.DeleteAsset(assetPath);
            AssetDatabase.CreateAsset(data, assetPath);
            return data;
        }

        /// <summary>The south-half point of the field that is farthest from every station, so the hero never starts
        /// inside a preview/NEAR ring (the 6-station team layout otherwise puts S6 on the old spawn).</summary>
        static Vector3 ChooseSpawn(float islandCentreX, float islandSize, StationSpec[] stations)
        {
            float a = islandSize * 0.4f * 0.9f, c = islandSize * 0.3f * 0.9f; // 0.8 field ellipse, 10% margin
            float k = islandSize / 12f;
            var best = Vector3.zero;
            float bestClear = -1f;
            for (float x = -a; x <= a; x += 0.5f)
            for (float z = -c; z <= 0f; z += 0.5f)
            {
                if (x * x / (a * a) + z * z / (c * c) > 1f) continue;
                float clear = float.MaxValue;
                foreach (var s in stations)
                {
                    var d = new Vector2(s.At.x * k - x, s.At.z * k - z);
                    clear = Mathf.Min(clear, d.magnitude);
                }
                if (clear > bestClear) { bestClear = clear; best = new Vector3(x, 0.05f, z); }
            }
            Debug.Log($"[TaskRaceScene] hero spawn ({best.x:0.0},{best.z:0.0}), nearest station {bestClear:0.00} u.");
            return best + new Vector3(islandCentreX, 0f, 0f);
        }

        static GameObject BuildHero(Vector3 at)
        {
            var root = new GameObject("Player") { tag = "Player" };
            root.transform.position = at;
            var cc = root.AddComponent<CharacterController>();
            cc.height = 1.15f; cc.radius = 0.3f; cc.center = new Vector3(0f, 0.575f, 0f);
            root.AddComponent<BobTaskRaceHero>();

            // Cleaned hero mesh (doc 23): pivot at the bottom centre, so it drops onto y = 0 as is.
            var prefab = Resources.Load<GameObject>("Characters/SK_hero_sweep");
            if (prefab != null)
            {
                var body = (GameObject)PrefabUtility.InstantiatePrefab(prefab, root.transform);
                body.name = "SK_hero_sweep";
                body.transform.localPosition = Vector3.zero;
                // the prefab root carries the FBX importer's upright rotation (X 270) and cm compensation; compose, never replace
                body.transform.localRotation = Quaternion.Euler(0f, 90f, 0f) * prefab.transform.localRotation;
                return root;
            }

            var capsule = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            capsule.name = "Body";
            capsule.transform.SetParent(root.transform, false);
            capsule.transform.localScale = new Vector3(0.55f, 0.575f, 0.55f);
            capsule.transform.localPosition = new Vector3(0f, 0.575f, 0f);
            Object.DestroyImmediate(capsule.GetComponent<Collider>());
            return root;
        }

        static void AddStation(Transform props, StationSpec spec, float k, Material mat)
        {
            var station = new GameObject("Station_" + spec.Id);
            station.transform.SetParent(props, false);
            station.transform.localPosition = spec.At * k;

            // Low work counter: the hero is 1.15 u, so the top edge stays below the chest and never hides the props.
            var body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.name = "Counter";
            body.transform.SetParent(station.transform, false);
            body.transform.localPosition = new Vector3(0f, 0.275f, 0f);
            body.transform.localScale = new Vector3(1.25f, 0.55f, 0.85f);
            Object.DestroyImmediate(body.GetComponent<Collider>());
            body.GetComponent<MeshRenderer>().sharedMaterial = mat;
            PaletteCell(body, 10, 1);   // PALETTE "wood"

            var top = GameObject.CreatePrimitive(PrimitiveType.Cube);
            top.name = "CounterTop";
            top.transform.SetParent(station.transform, false);
            top.transform.localPosition = new Vector3(0f, 0.59f, 0f);
            top.transform.localScale = new Vector3(1.36f, 0.08f, 0.95f);
            Object.DestroyImmediate(top.GetComponent<Collider>());
            top.GetComponent<MeshRenderer>().sharedMaterial = mat;
            PaletteCell(top, 10, 2);    // PALETTE "stone"

            foreach (var (prop, at, yaw) in spec.Decor) AddProp(station.transform, prop, at, yaw);

            var spot = station.AddComponent<TaskSpot>();
            spot.Configure(spec.Id, spec.Name, spec.Category, spec.Seconds);
        }

        static Transform AddProp(Transform parent, string name, Vector3 localPos, float yaw)
        {
            var prefab = Resources.Load<GameObject>("Props/SM_" + name);
            if (prefab == null) { Debug.LogWarning("[TaskRaceScene] prop prefab missing: SM_" + name); return null; }
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            go.name = "SM_" + name;
            go.transform.localPosition = localPos;
            go.transform.localRotation = Quaternion.Euler(0f, yaw, 0f) * prefab.transform.localRotation;
            return go.transform;
        }

        /// <summary>M_BOB_Station: the shared palette material, so station props colour like the map pieces.</summary>
        static Material StationMaterialAsset()
        {
            var palette = AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/Materials/M_BOB_CurvedMap.mat");
            var m = AssetDatabase.LoadAssetAtPath<Material>(StationMaterialPath);
            if (m == null)
            {
                if (palette == null) return null;
                m = new Material(palette.shader);
                AssetDatabase.CreateAsset(m, StationMaterialPath);
            }
            if (palette != null) { m.mainTexture = palette.mainTexture; m.color = Color.white; }
            EditorUtility.SetDirty(m);
            return m;
        }

        /// <summary>Point every UV at one palette cell (colour comes from T_BOB_Palette.png, not a flat tint).</summary>
        static void PaletteCell(GameObject go, int cellX, int cellY, int grid = 16)
        {
            var mf = go.GetComponent<MeshFilter>();
            if (mf == null || mf.sharedMesh == null) return;
            var mesh = Object.Instantiate(mf.sharedMesh);
            mesh.name = go.name + "_PaletteUV";
            var uv = new Vector2[mesh.vertexCount];
            var centre = new Vector2((cellX + 0.5f) / grid, 1f - (cellY + 0.5f) / grid);
            for (int i = 0; i < uv.Length; i++) uv[i] = centre;
            mesh.uv = uv;
            mf.sharedMesh = mesh;
        }

        readonly struct StationSpec
        {
            public readonly string Id, Name;
            public readonly TaskCategory Category;
            public readonly float Seconds;
            public readonly Vector3 At;
            public readonly (string prop, Vector3 at, float yaw)[] Decor;

            public StationSpec(string id, string name, TaskCategory category, float seconds, Vector3 at,
                params (string, Vector3, float)[] decor)
            {
                Id = id; Name = name; Category = category; Seconds = seconds; At = at; Decor = decor;
            }
        }
    }
}
