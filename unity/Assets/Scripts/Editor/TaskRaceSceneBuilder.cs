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
            // The south bridge deck cuts across the island at z = -islandDepth * 0.3, so every inner counter sits
            // 1.0 u farther toward the island's centre line than the round-2 layout (docs 24 section 3.1); S2 sits on
            // the centre line itself, because its counter is the one nearest the west bridge landing.
            new StationSpec("S1_A", "ล้างเครื่องชงกาแฟ", TaskCategory.Wash, 12f, new Vector3(-3f, 0f, -1f),
                ("espresso_machine", new Vector3(0f, 0.63f, 0f), 270f)),
            new StationSpec("S2_A", "ขัดพื้นทางเดิน", TaskCategory.Scrub, 16f, new Vector3(0.5f, 0f, 0f),
                ("bucket_metal_blue", new Vector3(-0.38f, 0.63f, 0.1f), 270f),
                ("brush_scrub_wood", new Vector3(0.32f, 0.63f, 0f), 250f)),
            new StationSpec("S3_A", "กวาดใบไม้", TaskCategory.Sweep, 10f, new Vector3(3f, 0f, -1f),
                ("basket_clothes", new Vector3(0f, 0.63f, 0f), 270f),
                ("brush_pair_yellow", new Vector3(0.95f, 0f, 0.45f), 300f)),
        };

        static readonly StationSpec[] DuoStations = SoloStations.Concat(new[]
        {
            new StationSpec("S4_A", "ฉีดน้ำยาเช็ดกระจก", TaskCategory.Spray, 14f, new Vector3(-3f, 0f, 0.5f),
                ("spray_bottle", new Vector3(-0.3f, 0.63f, 0f), 260f),
                ("gloves_pair", new Vector3(0.35f, 0.63f, 0.05f), 280f)),
        }).ToArray();

        static readonly StationSpec[] TeamStations = DuoStations.Concat(new[]
        {
            new StationSpec("S5_A", "ล้างถังน้ำยา", TaskCategory.Wash, 20f, new Vector3(3.2f, 0f, 0.8f),
                ("bucket_blue", new Vector3(-0.3f, 0.63f, 0f), 270f),
                ("detergent_pump", new Vector3(0.32f, 0.63f, 0f), 250f)),
            new StationSpec("S6_A", "เก็บขยะและใบไม้", TaskCategory.Everyone, 18f, new Vector3(-0.5f, 0f, -1.4f),
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

            // The trilight ambient is what BOB/CurvedWorld and BOB/ToonLit read, so this one block lights the map, the
            // props and the heroes. Warm sky above, a pale horizon, a violet bounce under the grass.
            RenderSettings.skybox = null;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.56f, 0.70f, 0.94f);
            RenderSettings.ambientEquatorColor = new Color(0.82f, 0.80f, 0.82f);
            RenderSettings.ambientGroundColor = new Color(0.42f, 0.36f, 0.50f);
            RenderSettings.ambientIntensity = 1f;

            // A little fog pulls the far islands into the sky and stops the curved horizon from ending on a hard line.
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(0.80f, 0.89f, 0.98f);
            RenderSettings.fogStartDistance = 25f;
            RenderSettings.fogEndDistance = 90f;

            // The key light: behind-left and well off the camera axis, so a cast shadow lands beside each counter,
            // prop and hero instead of hiding behind it from the low front-on play camera.
            var sunGo = new GameObject("Sun", typeof(Light));
            var sun = sunGo.GetComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = new Color(1f, 0.96f, 0.88f);
            sun.intensity = 0.85f;
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = 0.85f;
            sun.shadowBias = 0.02f;
            sun.shadowNormalBias = 0.2f;
            sun.shadowNearPlane = 0.2f;
            sunGo.transform.rotation = Quaternion.Euler(46f, 75f, 0f);

            // A cool fill from the far side: it lifts the shadow side of the counters and heroes without a second
            // shadow map, which is what keeps the pastel shadow readable instead of a flat grey band.
            var fillGo = new GameObject("Fill", typeof(Light));
            var fill = fillGo.GetComponent<Light>();
            fill.type = LightType.Directional;
            fill.color = new Color(0.62f, 0.75f, 1f);
            fill.intensity = 0.3f;
            fill.shadows = LightShadows.None;
            fillGo.transform.rotation = Quaternion.Euler(28f, -110f, 0f);

            // The map is ~30 u across; the 150 u default shadow distance spends all the cascade resolution on empty sky.
            QualitySettings.shadowDistance = 45f;

            var camGo = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener)) { tag = "MainCamera" };
            var cam = camGo.GetComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.80f, 0.90f, 0.98f);
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 400f;
            camGo.transform.position = new Vector3(-islandGap * 0.5f, 9f, -7f);
            camGo.AddComponent<BobSkyBackdrop>();
            camGo.AddComponent<BobPostGrade>();

            var hero = BuildHero(ChooseSpawn(-islandGap * 0.5f, islandSize, stations));

            // The window's own builder: prefabs, flat walk colliders, mode groups, camera rig, curved materials.
            // The dressing material has to exist before the map builds, because the seeded bridge deck and waterfall
            // load it while they are created.
            DressingMaterial();
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
            AddIslandDecor(propsA, k);
            assembler.CloneTeamA();
            // The prop/character prefabs ship with shadows off (the rental room's painted look), so a Task Race scene
            // has to switch them back on or no cast shadow ever reaches the grass.
            EnableShadows(map);
            EnableShadows(hero);
            AddContactShadows(map);
            AddDressing(map);
            // Now that both teams carry their stations and decor: the bridge lanes can only be judged after the clone.
            BobMapAssemblerEditor.ValidateClearance(map.transform);

            var rig = camGo.GetComponent<BobTaskRaceCamera>();
            if (rig != null) rig.SetTarget(hero.transform, true);

            // The round director: seeded list of docs 16 section 2.2 sized for this mode. Authoring it here bakes the
            // planned minigame names into the scene, so the editor view matches what the players will see.
            var systems = map.transform.Find("Systems");
            if (systems != null)
            {
                systems.gameObject.AddComponent<BobCloudSea>();
                var round = systems.gameObject.AddComponent<TaskRaceRound>();
                round.ConfigureRound(players, 1, 1975);
                round.StartRound(1975);

                // The HUD of docs 24 section 10: task list, weight bar and the main context button. Built here so the
                // scene carries it and the verify renders show the list.
                var hudGo = new GameObject("TaskRaceHUD");
                var hud = hudGo.AddComponent<BobTaskRaceHud>();
                hud.Bind(round);
                hud.Build(cam);
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

        /// <summary>Turn cast/receive shadows back on for everything under `root`.</summary>
        static void EnableShadows(GameObject root)
        {
            var renderers = root.GetComponentsInChildren<Renderer>(true);
            foreach (var r in renderers)
            {
                r.shadowCastingMode = ShadowCastingMode.On;
                r.receiveShadows = true;
            }
            Debug.Log($"[TaskRaceScene] cast/receive shadows on for {renderers.Length} renderer(s) under {root.name}.");
        }

        /// <summary>A soft radial shadow on the grass under every counter (docs 24 section 3.1). A shadow map alone
        /// leaves the contact edge bright, so props read as pasted on; this decal grounds them.</summary>
        static void AddContactShadows(GameObject map)
        {
            var mat = ContactShadowMaterial();
            if (mat == null) return;
            int n = 0;
            foreach (var spot in Object.FindObjectsByType<TaskSpot>(FindObjectsInactive.Exclude))
            {
                // Measure the counter top, not every renderer under the station: a station can carry a ring or a long
                // tool, and union bounds of those produced a 20 u decal — the violet pool that ringed each island.
                var top = spot.transform.Find("CounterTop")?.GetComponent<Renderer>();
                var b = top != null ? top.bounds : spot.GetComponentInChildren<Renderer>(true).bounds;

                var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
                Undo.RegisterCreatedObjectUndo(quad, "Build Task Race Map");
                quad.name = "ContactAO";
                quad.transform.SetParent(spot.transform, true);
                quad.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
                quad.transform.position = new Vector3(b.center.x, 0.02f, b.center.z);
                float s = Mathf.Clamp(Mathf.Max(b.size.x, b.size.z) * 1.45f, 1.6f, 2.6f);
                quad.transform.localScale = new Vector3(s, s, 1f);
                Object.DestroyImmediate(quad.GetComponent<Collider>());
                var mr = quad.GetComponent<MeshRenderer>();
                mr.sharedMaterial = mat;
                mr.shadowCastingMode = ShadowCastingMode.Off;
                mr.receiveShadows = false;
                n++;
            }
            Debug.Log($"[TaskRaceScene] contact shadows under {n} station(s).");
        }

        static Material ContactShadowMaterial()
        {
            const string path = "Assets/Resources/Materials/M_BOB_ContactAO.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m != null) return m;
            var shader = Resources.Load<Shader>("Shaders/BobGroundAO") ?? Shader.Find("BOB/GroundAO");
            if (shader == null) { Debug.LogWarning("[TaskRaceScene] BOB/GroundAO shader not found: no contact shadows."); return null; }
            m = new Material(shader) { name = "M_BOB_ContactAO" };
            AssetDatabase.CreateAsset(m, path);
            return m;
        }

        /// <summary>Grass tufts, daisies and stones over each island (docs 24 section 3.1). Density follows the field
        /// area, the seed is fixed per side, and a station keeps a 1.7 u clear ring.</summary>
        static void AddDressing(GameObject map)
        {
            if (DressingMaterial() == null) return;
            int i = 0;
            foreach (var side in new[] { "TeamA", "TeamB" })
            {
                // Same seed both sides: the island must look identical, or the race is not fair to the eye.
                var field = map.transform.Find(side + "/Field");
                if (field == null) continue;
                var r = field.GetComponent<Renderer>();
                if (r == null) continue;
                var b = r.bounds;
                var go = new GameObject("Dressing_" + side);
                go.transform.SetParent(map.transform, false);
                var d = go.AddComponent<BobFieldDressing>();
                d.centre = new Vector3(b.center.x, 0f, b.center.z);
                d.halfX = b.extents.x * 0.9f;
                d.halfZ = b.extents.z * 0.9f;
                d.seed = 1975;
                // Short blades disappear at play distance unless they are dense: scale counts by the ellipse area.
                float area = Mathf.PI * d.halfX * d.halfZ;
                int tufts = Mathf.RoundToInt(area * 18f);
                d.tufts = tufts;
                d.flowers = Mathf.RoundToInt(area * 1.8f);
                d.rocks = Mathf.Max(10, Mathf.RoundToInt(area * 0.3f));
                d.patches = Mathf.Max(8, Mathf.RoundToInt(area * 0.35f));
                // AddComponent already built once with the component's defaults; rebuild with the island's numbers.
                d.Build();
                i++;
            }
            Debug.Log("[TaskRaceScene] field dressing: " + i + " island(s).");
        }

        static Material DressingMaterial()
        {
            const string path = "Assets/Resources/Materials/M_BOB_Dressing.mat";
            var shader = Resources.Load<Shader>("Shaders/BobDressing") ?? Shader.Find("BOB/Dressing");
            var palette = AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/Materials/M_BOB_CurvedMap.mat");
            if (shader == null || palette == null) return null;
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null)
            {
                m = new Material(shader) { name = "M_BOB_Dressing" };
                AssetDatabase.CreateAsset(m, path);
            }
            // No inverted-hull outline on this shader: the zero-width pass painted the back of every grass blade with
            // the dark outline colour. Strata and mottle stay off — the dressing bakes its shape into vertex colours.
            m.shader = shader;
            m.mainTexture = palette.mainTexture;
            m.SetFloat("_Mottle", 0f);
            m.SetFloat("_Strata", 0f);
            m.SetFloat("_Rim", 0f);
            m.SetFloat("_Sheen", 0.04f);
            EditorUtility.SetDirty(m);
            return m;
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

            manager.SetMode(TaskRaceMode.TimedBridgeRaid);
            var raid = Bake(surface, $"Assets/Scenes/NavMesh/{stem}_TimedBridgeRaid.asset");
            manager.SetMode(TaskRaceMode.PureRace);
            var pure = Bake(surface, $"Assets/Scenes/NavMesh/{stem}_PureRace.asset");
            // the mode used to be called BridgeRaid; drop the asset left behind by that name
            var legacy = $"Assets/Scenes/NavMesh/{stem}_BridgeRaid.asset";
            if (AssetDatabase.LoadAssetAtPath<NavMeshData>(legacy) != null) AssetDatabase.DeleteAsset(legacy);

            manager.pureNavMesh = pure;
            manager.raidNavMesh = raid;
            surface.navMeshData = pure;
            EditorUtility.SetDirty(surface);
            EditorUtility.SetDirty(manager);

            // NavMesh.CalculateTriangulation reads whatever bake is added right now, so switch once per mode to measure.
            manager.SetMode(TaskRaceMode.PureRace);
            int pureTri = NavMesh.CalculateTriangulation().indices.Length / 3;
            manager.SetMode(TaskRaceMode.TimedBridgeRaid);
            int raidTri = NavMesh.CalculateTriangulation().indices.Length / 3;
            manager.SetMode(previous);

            Debug.Log($"[TaskRaceScene] navmesh baked: PureRace {pureTri} tri, TimedBridgeRaid {raidTri} tri " +
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
            // Team A is the left island; the assembler sets TeamB on each clone.
            spot.SetOwnerTeam(TeamId.TeamA);
        }

        /// <summary>The dressed set (docs 24 section 8, "อุปกรณ์ประกอบฉาก"): lamps, bushes, a cafe corner, crates, a
        /// planter rim and a few stones, all placed under TeamA/Props before the clone so Team B gets the exact same
        /// arrangement by translation. Positions are authored on the 12 u island and scaled by the mode.</summary>
        static void AddIslandDecor(Transform props, float k)
        {
            // Every prop sits in the clear centre band: the north and south bridge decks cross the island at
            // z = +-islandDepth * 0.3 and the rebuild keeps a 1.0 u lane along each deck's sides plus a 1.5 u landing at
            // its ends (docs 24 section 3.1). The decks cover the field's north and south edges, so the band between
            // them (|z| <= 0.74 after the prop's own footprint) is the only real estate that clears the lanes.
            var set = new (string name, float x, float z, float yaw, float height)[]
            {
                ("street_lamp",      -4.30f, -0.45f,   0f, 2.2f),
                ("street_lamp",       4.30f, -0.45f,   0f, 2.2f),
                // the flowering bush is 3.0 x 2.9 u at 1.3 m, far wider than the band: keep it as a 0.6 m shrub
                ("bush_green",       -4.35f,  0.00f,  15f, 0.6f),
                ("table_round_cafe",  4.15f,  0.00f,  10f, 0.75f),
                ("stool_red",         4.60f,  0.10f,   0f, 0.45f),
                ("stool_red",         3.30f,  0.00f,   0f, 0.45f),
                ("crate_oranges",    -1.20f,  0.15f,  20f, 0.55f),
                ("crate_oranges",    -0.80f,  0.00f,  35f, 0.5f),
                ("chalkboard_easel", -4.30f,  0.10f,  75f, 1.1f),
                ("sign_stand_wood",  -4.35f, -0.10f,  90f, 1.0f),
                ("rocks_grass",       2.40f,  0.15f, 140f, 0.35f),
                ("rocks_grass",       1.80f,  0.20f,  80f, 0.3f),
                ("pot_blue",         -4.05f, -0.15f,   0f, 0.6f),
                ("pot_ceramic_gold",  4.05f, -0.30f,   0f, 0.55f),
            };
            int n = 0;
            foreach (var (name, x, z, yaw, height) in set)
                if (AddDecor(props, name, new Vector3(x * k, 0f, z * k), yaw, height)) n++;
            Debug.Log($"[TaskRaceScene] island decor: {n} prop(s) placed on Team A, cloned to Team B.");
        }

        /// <summary>Instantiate a prop from Assets/Resources/Props, scale it to a target height and sit it on the
        /// grass (the FBX pivots are not at the bottom).</summary>
        static bool AddDecor(Transform parent, string name, Vector3 at, float yaw, float height)
        {
            var prefab = Resources.Load<GameObject>("Props/SM_" + name);
            if (prefab == null) { Debug.LogWarning("[TaskRaceScene] decor prefab missing: SM_" + name); return false; }
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            go.name = "Decor_" + name;
            go.transform.localPosition = at;
            go.transform.localRotation = Quaternion.Euler(0f, yaw, 0f) * prefab.transform.localRotation;
            if (height > 0.01f && BoundsOf(go, out var b0) && b0.size.y > 0.001f)
                go.transform.localScale *= height / b0.size.y;
            if (BoundsOf(go, out var b1))
                go.transform.localPosition = at + Vector3.up * (0.02f - b1.min.y);
            return true;
        }

        static bool BoundsOf(GameObject go, out Bounds b)
        {
            var renderers = go.GetComponentsInChildren<Renderer>();
            b = default;
            if (renderers.Length == 0) return false;
            b = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) b.Encapsulate(renderers[i].bounds);
            return true;
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
