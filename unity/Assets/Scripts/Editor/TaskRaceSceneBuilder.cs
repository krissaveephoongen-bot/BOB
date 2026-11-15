// Battle of the Brooms: builds the Task Race scenes (docs 24) from the Task Race Map Assembler.
// One command does what the Inspector guide lists by hand: camera + hero + light, the twin-island map with real
// bob_map_* prefabs, the six-station rig on Team A mirrored to Team B, and the scene added to Build Settings.
//   Tools > BOB > Build Task Race Scene (solo / duo / team)
//   -executeMethod Bob.EditorTools.TaskRaceSceneBuilder.Build        (solo, the default scene)
//   -executeMethod Bob.EditorTools.TaskRaceSceneBuilder.BuildDuo
//   -executeMethod Bob.EditorTools.TaskRaceSceneBuilder.BuildTeam
// The three modes share every rule; only the map size and the seeded list length differ (docs 24 section 13). The
// station layout is the same six-station rig on all three (phase 2, docs 24 section 15.3).
using System.IO;
using System.Linq;
using Bob.Game;
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

        // The station layout is the Phase 2 twin-island rig (BobTwinIslandRig): the same six stations on every mode,
        // mirrored to Team B. The mode only changes the map size and the seeded list length.
        [MenuItem("Tools/BOB/Build Task Race Scene (solo)")]
        public static void Build() => BuildVariant(ScenePath, 12f, 16f, 1);

        [MenuItem("Tools/BOB/Build Task Race Scene (duo)")]
        public static void BuildDuo() => BuildVariant(DuoScenePath, 15f, 20f, 2);

        [MenuItem("Tools/BOB/Build Task Race Scene (team)")]
        public static void BuildTeam() => BuildVariant(TeamScenePath, 18f, 24f, 3);

        static void BuildVariant(string scenePath, float islandSize, float islandGap, int players)
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

            var hero = BuildHero(ChooseSpawn(-islandGap * 0.5f, islandSize));
            // Solo has a visible opponent on Team B (docs 24 §15.4 Phase 4); the other modes keep the pace rival.
            var bot = players == 1 ? BuildBot(ChooseSpawn(islandGap * 0.5f, islandSize)) : null;

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
            var propsB = map.transform.Find("TeamB/Props");
            var mat = StationMaterialAsset();
            // Phase 2 of docs 24 section 15.3: the six-station kitbash on Team A, mirrored to Team B (x -> -x).
            BobTwinIslandRig.Build(propsA, propsB, mat);
            // The prop/character prefabs ship with shadows off (the rental room's painted look), so a Task Race scene
            // has to switch them back on or no cast shadow ever reaches the grass.
            EnableShadows(map);
            EnableShadows(hero);
            if (bot != null) EnableShadows(bot);
            AddContactShadows(map);
            AddDressing(map);
            // Both islands now carry the rig: the bridge lanes can only be judged after the mirror.
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
                // The opponent until the server exists: the solo bot walks Team B, the other modes keep the pace model
                // (docs 24 section 12).
                if (bot == null) systems.gameObject.AddComponent<BobTaskRaceRival>();

                // The HUD of docs 24 section 10: task list, weight bar and the main context button. Built here so the
                // scene carries it and the verify renders show the list.
                var hudGo = new GameObject("TaskRaceHUD");
                var hud = hudGo.AddComponent<BobTaskRaceHud>();
                hud.Bind(round);
                // No BobGame in this scene to set the static, so the row icons need the library handed over here.
                hud.BindArt(AssetDatabase.LoadAssetAtPath<BobArtLibrary>("Assets/Art/BobArtLibrary.asset"));
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
        /// inside a preview/NEAR ring.</summary>
        static Vector3 ChooseSpawn(float islandCentreX, float islandSize)
        {
            float a = islandSize * 0.4f * 0.9f, c = islandSize * 0.3f * 0.9f; // 0.8 field ellipse, 10% margin
            var anchors = BobTwinIslandRig.StationAnchors;
            var best = Vector3.zero;
            float bestClear = -1f;
            for (float x = -a; x <= a; x += 0.5f)
            for (float z = -c; z <= 0f; z += 0.5f)
            {
                if (x * x / (a * a) + z * z / (c * c) > 1f) continue;
                float clear = float.MaxValue;
                foreach (var at in anchors)
                {
                    var d = new Vector2(at.x - x, at.z - z);
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

        /// <summary>The solo opponent (docs 24 §15.4 Phase 4): the same hero mesh on Team B, driven by
        /// TaskRaceBotController. No CharacterController — the bot walks waypoints on the flat map.</summary>
        static GameObject BuildBot(Vector3 at)
        {
            var root = new GameObject("Bot");
            root.transform.position = at;
            root.AddComponent<TaskRaceBotController>();

            var prefab = Resources.Load<GameObject>("Characters/SK_hero_sweep");
            if (prefab != null)
            {
                var body = (GameObject)PrefabUtility.InstantiatePrefab(prefab, root.transform);
                body.name = "SK_hero_sweep";
                body.transform.localPosition = Vector3.zero;
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
    }
}
