// Battle of the Brooms: Task Race map assembler (docs 24). One window that turns the cleaned Tripo map pieces into the
// playable twin-island layout and switches it between the two modes:
//
//   Tools > BOB > Task Race Map Assembler
//   1 · Build map         two islands, river, north/south bridges, waterfall cliff, flat walk colliders, mode groups
//   2 · Clone Team A      duplicates every prop and TaskSpot to Team B with an exact +X translation (never mirrored)
//                         and gives each cloned spot a unique id (S3_A -> S3_B)
//   3 · Mode buttons      PureRace / BridgeRaid, applied to the TaskRaceModeManager in the scene
//
// The bob_map_* prefabs do not exist in the project yet (the raw GLBs are in OneDrive/Rig). Every slot can be assigned
// by hand; an empty slot builds a labelled placeholder box so the layout and the modes can be tested immediately.
using System.Collections.Generic;
using System.Linq;
using Bob.TaskRace;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

namespace Bob.EditorTools
{
    public class BobMapAssemblerEditor : EditorWindow
    {
        const string RootName = "TaskRaceMap";
        const float FieldInset = 0.8f; // flat play field covers this share of the island's footprint

        [Header("Terrain prefabs (bob_map_*)")]
        public GameObject islandPrefab, islandPrefabB, riverPrefab, bridgePrefab, cliffPrefab;

        [Header("Layout")]
        [SerializeField] float islandSize = 12f;
        [SerializeField] float islandGap = 16f;
        [SerializeField] float bridgeYaw;
        [SerializeField] float riverYaw = 90f;
        [SerializeField] bool allowPrimitives = true;
        [SerializeField] bool replaceExisting;
        [SerializeField] bool setupCamera = true;
        [SerializeField] bool swapMaterialsToCurved = true;

        [Header("Clone")]
        [SerializeField] bool replaceTeamBProps = true;

        [SerializeField] TaskRaceMode mode = TaskRaceMode.PureRace;

        Vector2 scroll;

        [MenuItem("Tools/BOB/Task Race Map Assembler")]
        public static void Open() => GetWindow<BobMapAssemblerEditor>("Task Race Map");

        void OnEnable() => LoadDefaultPrefabs();

        public void LoadDefaultPrefabs()
        {
            if (islandPrefab == null) islandPrefab = FindPrefab("bob_map_island_land", "island_land", "floating island");
            if (islandPrefabB == null) islandPrefabB = FindPrefab("bob_map_island_land2", "island_land2", "floating island 2");
            if (riverPrefab == null) riverPrefab = FindPrefab("bob_map_river_channel", "river_channel", "river water");
            if (bridgePrefab == null) bridgePrefab = FindPrefab("bob_map_wooden_bridge", "wooden_bridge", "wooden footbridge");
            if (cliffPrefab == null) cliffPrefab = FindPrefab("bob_map_cliff_waterfall", "cliff_waterfall", "waterfall");
        }

        /// <summary>Layout override for the scene builder: one map size per game mode (docs 24 section 13).</summary>
        public void SetLayout(float size, float gap)
        {
            islandSize = Mathf.Max(6f, size);
            islandGap = Mathf.Max(islandSize + 3f, gap);
        }

        // ================================================================ window

        void OnGUI()
        {
            scroll = EditorGUILayout.BeginScrollView(scroll);
            EditorGUILayout.LabelField("Task Race Map", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Two identical islands (Team A / Team B) split by the river channel. Team B is an exact translation of " +
                "Team A, never a mirror, so the HUD corners cover the same ground on both sides.",
                MessageType.None);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("1 · Terrain prefabs", EditorStyles.boldLabel);
            DrawPrefab("Island A (land)", ref islandPrefab);
            DrawPrefab("Island B (land, optional)", ref islandPrefabB);
            DrawPrefab("River channel", ref riverPrefab);
            DrawPrefab("Wooden bridge", ref bridgePrefab);
            DrawPrefab("Cliff / waterfall", ref cliffPrefab);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Find by name")) LoadDefaultPrefabs();
                if (GUILayout.Button("Clear")) { islandPrefab = islandPrefabB = riverPrefab = bridgePrefab = cliffPrefab = null; }
            }

            bool missing = islandPrefab == null || riverPrefab == null || bridgePrefab == null;
            if (missing)
                EditorGUILayout.HelpBox(
                    "bob_map_* prefabs are not in the project yet. Empty slots build labelled placeholder boxes when " +
                    "\"Use placeholders\" is on, so the layout and modes work before the art pack is imported.",
                    MessageType.Warning);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("2 · Layout", EditorStyles.boldLabel);
            islandSize = EditorGUILayout.FloatField(new GUIContent("Island width (units)", "Longest horizontal side after scaling."), islandSize);
            islandGap = EditorGUILayout.FloatField(new GUIContent("Island centres (units)", "Centre-to-centre distance on X; the river sits between."), islandGap);
            bridgeYaw = EditorGUILayout.FloatField("Bridge yaw", bridgeYaw);
            riverYaw = EditorGUILayout.FloatField("River yaw", riverYaw);
            using (new EditorGUILayout.HorizontalScope())
            {
                allowPrimitives = EditorGUILayout.ToggleLeft("Use placeholders", allowPrimitives, GUILayout.Width(160));
                replaceExisting = EditorGUILayout.ToggleLeft("Replace existing map", replaceExisting, GUILayout.Width(180));
            }
            setupCamera = EditorGUILayout.ToggleLeft("Add BobTaskRaceCamera to Main Camera", setupCamera);
            swapMaterialsToCurved = EditorGUILayout.ToggleLeft("Swap map materials to BOB/CurvedWorld", swapMaterialsToCurved);

            EditorGUILayout.Space();
            if (GUILayout.Button("Build map skeleton", GUILayout.Height(32))) BuildMap();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("3 · Team B layout", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Put the station props and their TaskSpot components under TaskRaceMap/TeamA/Props, then clone them. " +
                "Each cloned spot gets a unique id: S3_A -> S3_B.",
                MessageType.None);
            replaceTeamBProps = EditorGUILayout.ToggleLeft("Replace TeamB/Props before cloning", replaceTeamBProps);
            if (GUILayout.Button("Clone Team A layout to Team B", GUILayout.Height(28))) CloneTeamA();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("4 · Mode", EditorStyles.boldLabel);
            mode = (TaskRaceMode)EditorGUILayout.EnumPopup("Mode", mode);
            using (new EditorGUILayout.HorizontalScope())
            {
                GUI.backgroundColor = mode == TaskRaceMode.PureRace ? new Color(0.75f, 0.9f, 1f) : Color.white;
                if (GUILayout.Button("PureRace\nbank barrier on")) { mode = TaskRaceMode.PureRace; ApplyMode(); }
                GUI.backgroundColor = mode == TaskRaceMode.BridgeRaid ? new Color(1f, 0.86f, 0.75f) : Color.white;
                if (GUILayout.Button("BridgeRaid\nbridges on")) { mode = TaskRaceMode.BridgeRaid; ApplyMode(); }
                GUI.backgroundColor = Color.white;
            }
            if (GUILayout.Button("Validate map")) ValidateMap();

            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(
                "Physics and NavMesh stay flat: only the shader bends the vertices. NavMeshObstacle(Carve) is added to the " +
                "bank colliders in PureRace; the bridges get a NavMeshLink (or OffMeshLink when the AI Navigation package " +
                "is not installed) in BridgeRaid.",
                MessageType.Info);
            EditorGUILayout.EndScrollView();
        }

        static void DrawPrefab(string label, ref GameObject prefab)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                prefab = (GameObject)EditorGUILayout.ObjectField(label, prefab, typeof(GameObject), false);
                GUILayout.Label(prefab == null ? "missing" : "ok", GUILayout.Width(48));
            }
        }

        // ================================================================ build

        public void BuildMap()
        {
            var existing = GameObject.Find(RootName);
            if (existing != null)
            {
                if (!replaceExisting)
                {
                    Selection.activeGameObject = existing;
                    EditorUtility.DisplayDialog("Task Race Map", "TaskRaceMap already exists. Turn on \"Replace existing map\" to rebuild it.", "OK");
                    return;
                }
                if (!EditorUtility.DisplayDialog("Task Race Map", "Delete the existing TaskRaceMap hierarchy and rebuild it?", "Replace", "Cancel")) return;
                Undo.DestroyObjectImmediate(existing);
            }

            var root = Child(null, RootName);
            float half = islandGap * 0.5f;
            float islandDepth = islandSize * 0.75f;

            // ---- two islands: identical rotation, only the X position differs (no mirroring)
            // The Tripo islands are assemblies (ground shell + rocks + floating decor), so the bounds top is a tree or
            // a floating rock, not the floor. Align each island by its ground surface and lay a flat Field slab on top:
            // that slab is the play plane the colliders, props and the curved shader all agree on.
            var teamA = Child(root, "TeamA");
            teamA.localPosition = new Vector3(-half, 0f, 0f);
            var islandA = SpawnPiece(islandPrefab, "Island", teamA, Vector3.zero, new Color(0.42f, 0.62f, 0.36f),
                new Vector3(islandSize, 0.6f, islandDepth), 0f, islandSize);
            if (islandA != null)
            {
                FitAxisAligned(islandA, islandSize, islandDepth);
                AlignGround(islandA, -0.05f);
                AddField(teamA, islandA);
            }
            var propsA = Child(teamA, "Props");

            var teamB = Child(root, "TeamB");
            teamB.localPosition = new Vector3(half, 0f, 0f);
            var islandB = SpawnPiece(islandPrefabB != null ? islandPrefabB : islandPrefab, "Island", teamB, Vector3.zero,
                new Color(0.45f, 0.6f, 0.4f), new Vector3(islandSize, 0.6f, islandDepth), 0f, islandSize);
            if (islandB != null)
            {
                FitAxisAligned(islandB, islandSize, islandDepth);
                AlignGround(islandB, -0.05f);
                AddField(teamB, islandB);
            }
            var propsB = Child(teamB, "Props");

            // ---- river between the islands, stretched along the channel and just under the walk plane
            float channel = Mathf.Max(1.5f, islandGap - islandSize);
            var river = SpawnPiece(riverPrefab, "RiverChannel", root, new Vector3(0f, 0f, 0f),
                new Color(0.25f, 0.58f, 0.86f), new Vector3(channel, 0.2f, islandDepth * 1.1f), riverYaw, channel);
            if (river != null)
            {
                FitAxisAligned(river, channel, islandDepth * 1.15f);
                AlignTop(river, -0.15f);
            }

            // ---- waterfall cliff as the back drop, north of the river mouth
            float cliffWidth = islandSize * 2f / 3f;
            var cliff = SpawnPiece(cliffPrefab, "CliffWaterfall", root, new Vector3(0f, 0f, islandDepth * 0.8f + 2f),
                new Color(0.48f, 0.45f, 0.5f), new Vector3(cliffWidth, cliffWidth * 0.31f, cliffWidth * 0.37f), 0f, cliffWidth);
            if (cliff != null) AlignTop(cliff, 0.5f);

            // ---- mode groups
            var barrier = Child(root, "RiverBarrier_Group");
            float bankX = Mathf.Max(0.4f, (islandGap - islandSize) * 0.5f - 0.15f);
            float bankDepth = islandDepth * 1.05f;
            CreateHedge(barrier, "Bank_West", new Vector3(-bankX, 0.22f, 0f), new Vector3(0.32f, 0.45f, bankDepth));
            CreateHedge(barrier, "Bank_East", new Vector3(bankX, 0.22f, 0f), new Vector3(0.32f, 0.45f, bankDepth));

            var bridges = Child(root, "Bridges_Group");
            float bridgeZ = islandDepth * 0.3f;
            const float bridgeWidth = 2f; // a footbridge: wide enough for the walk collider, narrow enough to read as a plank
            // The Field slab is an ellipse, so the bank at the bridge's z sits inboard of the island's widest line.
            // Measure that edge and let the deck land 1.2 u past it: the NavMesh erodes by the agent radius (0.5 u),
            // and a deck that stops short of the bank bakes as two islands with no path between them.
            float fieldHalfX = islandSize * FieldInset * 0.5f;
            float fieldHalfZ = islandDepth * FieldInset * 0.5f;
            float bridgeTarget = 0f;
            foreach (var (name, z) in new[] { ("Bridge_North", bridgeZ), ("Bridge_South", -bridgeZ) })
            {
                float bankEdgeX = fieldHalfX * Mathf.Sqrt(Mathf.Clamp01(1f - Mathf.Pow(z / fieldHalfZ, 2f)));
                bridgeTarget = 2f * (half - bankEdgeX + 1.2f);
                var b = SpawnPiece(bridgePrefab, name, bridges, new Vector3(0f, 0f, z),
                    new Color(0.62f, 0.44f, 0.28f), new Vector3(bridgeTarget, 0.25f, 1.8f), bridgeYaw, bridgeTarget);
                if (b == null) continue;
                FaceLongAxisX(b);
                FitAxisAligned(b, bridgeTarget, bridgeWidth);
                AlignTop(b, 0f);
                AddWalkable(b, 0f, 0.02f);
            }

            // ---- systems
            var systems = Child(root, "Systems");
            var manager = systems.gameObject.AddComponent<TaskRaceModeManager>();
            manager.riverBarrierGroup = barrier.gameObject;
            manager.bridgesGroup = bridges.gameObject;
            manager.teamA = teamA;
            manager.teamB = teamB;
            manager.mode = mode;

            var curved = systems.gameObject.AddComponent<BobCurvedWorldController>();
            curved.curvature = BobCurvedWorld.DefaultCurvature;

            // One surface for the whole map; the scene builder bakes it twice (barrier on / bridges on) and the
            // manager swaps the bake with the mode. Physics colliders only: the Field slabs and bridge decks are the
            // walkable geometry, so the decorative rocks and trees never enter the NavMesh.
            var surface = root.gameObject.AddComponent<NavMeshSurface>();
            surface.collectObjects = CollectObjects.Children;
            surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
            manager.navMeshSurface = surface;

            if (setupCamera)
            {
                var cam = Camera.main;
                if (cam != null)
                {
                    var rig = cam.GetComponent<BobTaskRaceCamera>() ?? Undo.AddComponent<BobTaskRaceCamera>(cam.gameObject);
                    manager.cameraRig = rig;
                    var hero = FindHero();
                    if (hero != null) rig.SetTarget(hero.transform, false);
                }
                else Debug.LogWarning("[TaskRaceMap] no Main Camera: add BobTaskRaceCamera to the play camera by hand.");
            }

            if (swapMaterialsToCurved) SwapToCurved(root);

            Undo.RegisterFullObjectHierarchyUndo(barrier.gameObject, "Apply Task Race mode");
            manager.SetMode(mode);

            Selection.activeGameObject = root.gameObject;
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Debug.Log($"[TaskRaceMap] built: islands {islandSize} u wide, {islandGap} u apart, channel {channel:0.##} u, " +
                      $"bridges {bridgeTarget:0.##} u long, mode {mode}.");
        }

        // ================================================================ clone

        public void CloneTeamA()
        {
            var root = GameObject.Find(RootName)?.transform;
            var aProps = root != null ? root.Find("TeamA/Props") : null;
            var bProps = root != null ? root.Find("TeamB/Props") : null;
            if (aProps == null || bProps == null)
            {
                EditorUtility.DisplayDialog("Task Race Map", "TaskRaceMap/TeamA/Props and TeamB/Props were not found. Build the map skeleton first.", "OK");
                return;
            }
            if (aProps.childCount == 0)
            {
                EditorUtility.DisplayDialog("Task Race Map", "TeamA/Props is empty. Add the station props and TaskSpot components first.", "OK");
                return;
            }

            if (replaceTeamBProps && bProps.childCount > 0)
            {
                if (!EditorUtility.DisplayDialog("Clone Team A layout", "Replace the existing props under TeamB/Props?", "Replace", "Cancel")) return;
                for (int i = bProps.childCount - 1; i >= 0; i--) Undo.DestroyObjectImmediate(bProps.GetChild(i).gameObject);
            }

            float dx = bProps.position.x - aProps.position.x;
            int cloned = 0, spots = 0;
            foreach (Transform src in aProps)
            {
                var copy = Instantiate(src.gameObject, bProps, false);
                Undo.RegisterCreatedObjectUndo(copy, "Clone Team A layout");
                copy.name = src.name + "_B";
                var world = aProps.TransformPoint(src.localPosition) + Vector3.right * dx;
                copy.transform.position = world;
                copy.transform.rotation = src.rotation; // exact copy: no mirroring
                copy.transform.localScale = src.localScale;
                cloned++;

                foreach (var spot in copy.GetComponentsInChildren<TaskSpot>(true))
                {
                    string wanted = spot.SpotId.EndsWith("_A")
                        ? spot.SpotId.Substring(0, spot.SpotId.Length - 2) + "_B"
                        : spot.SpotId + "_B";
                    spot.SetSpotId(UniqueSpotId(wanted, spot));
                    spots++;
                }
            }

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Debug.Log($"[TaskRaceMap] cloned {cloned} prop(s) to TeamB/Props with {spots} unique task spot id(s). No mirroring.");
        }

        static string UniqueSpotId(string wanted, TaskSpot self)
        {
            var taken = new HashSet<string>(
                Object.FindObjectsByType<TaskSpot>(FindObjectsInactive.Include)
                    .Where(s => s != self)
                    .Select(s => s.SpotId));
            string id = wanted; int n = 2;
            while (taken.Contains(id)) id = wanted + "_" + n++;
            return id;
        }

        // ================================================================ mode / validate

        void ApplyMode()
        {
            var manager = Object.FindAnyObjectByType<TaskRaceModeManager>();
            if (manager == null)
            {
                EditorUtility.DisplayDialog("Task Race Map", "No TaskRaceModeManager in the scene. Build the map skeleton first.", "OK");
                return;
            }
            Undo.RegisterFullObjectHierarchyUndo(manager.riverBarrierGroup != null ? manager.riverBarrierGroup : manager.gameObject, "Apply Task Race mode");
            if (manager.bridgesGroup != null) Undo.RegisterFullObjectHierarchyUndo(manager.bridgesGroup, "Apply Task Race mode");
            Undo.RecordObject(manager, "Apply Task Race mode");
            manager.SetMode(mode);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Debug.Log($"[TaskRaceMap] mode {mode} applied.");
        }

        void ValidateMap()
        {
            var spots = Object.FindObjectsByType<TaskSpot>(FindObjectsInactive.Exclude);
            var duplicates = spots.GroupBy(s => s.SpotId).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
            int empty = spots.Count(s => string.IsNullOrEmpty(s.SpotId));
            var manager = Object.FindAnyObjectByType<TaskRaceModeManager>();
            string mgr = manager == null ? "missing TaskRaceModeManager"
                : manager.riverBarrierGroup == null ? "manager has no RiverBarrier_Group"
                : manager.bridgesGroup == null ? "manager has no Bridges_Group"
                : "manager ok";
            Debug.Log($"[TaskRaceMap] validate: {spots.Length} task spot(s), {duplicates.Count} duplicate id(s)" +
                      (duplicates.Count > 0 ? " (" + string.Join(", ", duplicates) + ")" : "") +
                      $", {empty} empty id(s), {mgr}.");
            if (duplicates.Count > 0 || empty > 0) Debug.LogWarning("[TaskRaceMap] fix the task spot ids before shipping the map.");
        }

        // ================================================================ scene helpers

        static Transform Child(Transform parent, string name)
        {
            var go = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(go, "Build Task Race Map");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;
            return go.transform;
        }

        GameObject SpawnPiece(GameObject prefab, string name, Transform parent, Vector3 localPos, Color placeholder,
            Vector3 placeholderSize, float yaw, float targetWidth)
        {
            GameObject go;
            if (prefab != null)
            {
                go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
                go.name = name;
                go.transform.localPosition = localPos;
                // compose like TryProp: never overwrite the importer's upright rotation
                go.transform.localRotation = Quaternion.Euler(0f, yaw, 0f) * go.transform.localRotation;
                if (targetWidth > 0f) ScaleToWidth(go, targetWidth);
            }
            else
            {
                if (!allowPrimitives)
                {
                    Debug.LogWarning($"[TaskRaceMap] no prefab for {name} and placeholders are off.");
                    return null;
                }
                go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                Undo.RegisterCreatedObjectUndo(go, "Build Task Race Map");
                go.name = name + "_Placeholder";
                go.transform.SetParent(parent, false);
                go.transform.localPosition = localPos;
                go.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
                go.transform.localScale = placeholderSize;
                var col = go.GetComponent<Collider>();
                if (col != null) DestroyImmediate(col);
                Tint(go, placeholder);
                go.name = name;
            }
            return go;
        }

        void CreateHedge(Transform parent, string name, Vector3 localPos, Vector3 size)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Undo.RegisterCreatedObjectUndo(go, "Build Task Race Map");
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = size;
            UsePaletteCell(go, 10, 2);   // PALETTE "stone"
        }

        static void ScaleToWidth(GameObject go, float width)
        {
            if (!WorldBounds(go, out var b)) return;
            float longest = Mathf.Max(b.size.x, b.size.z);
            if (longest > 0.0001f) go.transform.localScale *= width / longest;
        }

        /// <summary>Scale X and Z independently (world axes), keeping the model's importer rotation intact.</summary>
        static void FitAxisAligned(GameObject go, float targetX, float targetZ)
        {
            if (!WorldBounds(go, out var b)) return;
            var t = go.transform;
            int ix = DominantLocalAxis(t, Vector3.right);
            int iz = DominantLocalAxis(t, Vector3.forward);
            if (ix < 0 || iz < 0 || ix == iz) return;
            var s = t.localScale;
            if (b.size.x > 0.0001f) s[ix] *= targetX / b.size.x;
            if (b.size.z > 0.0001f) s[iz] *= targetZ / b.size.z;
            t.localScale = s;
        }

        static int DominantLocalAxis(Transform t, Vector3 worldAxis)
        {
            int best = -1; float bestDot = 0.6f;
            for (int i = 0; i < 3; i++)
            {
                var axis = i == 0 ? Vector3.right : i == 1 ? Vector3.up : Vector3.forward;
                float dot = Mathf.Abs(Vector3.Dot(t.TransformDirection(axis).normalized, worldAxis));
                if (dot > bestDot) { bestDot = dot; best = i; }
            }
            return best;
        }

        /// <summary>Turn the piece so its long horizontal side runs on X (bridges must cross the channel).</summary>
        static void FaceLongAxisX(GameObject go)
        {
            if (!WorldBounds(go, out var b) || b.size.z <= b.size.x) return;
            go.transform.rotation = Quaternion.AngleAxis(90f, Vector3.up) * go.transform.rotation;
        }

        /// <summary>
        /// Move the piece so the floor under the inner footprint sits on `planeY`. Tripo islands carry rocks and
        /// floating decor far above the ground, so aligning by bounds top would bury the play area.
        /// </summary>
        static void AlignGround(GameObject go, float planeY)
        {
            if (!WorldBounds(go, out var b)) return;
            var heights = new List<float>();
            float rx = Mathf.Max(0.001f, b.extents.x * 0.75f);
            float rz = Mathf.Max(0.001f, b.extents.z * 0.75f);
            foreach (var mf in go.GetComponentsInChildren<MeshFilter>(true))
            {
                var mesh = mf.sharedMesh;
                if (mesh == null) continue;
                var m = mf.transform.localToWorldMatrix;
                var verts = mesh.vertices;
                var normals = mesh.normals;
                for (int i = 0; i < verts.Length; i++)
                {
                    var w = m.MultiplyPoint3x4(verts[i]);
                    if (m.MultiplyVector(normals[i]).y < 0.6f) continue; // floors only
                    float dx = (w.x - b.center.x) / rx, dz = (w.z - b.center.z) / rz;
                    if (dx * dx + dz * dz > 1f) continue;
                    heights.Add(w.y);
                }
            }
            if (heights.Count < 8) { AlignTop(go, planeY); return; }
            heights.Sort();
            float floor = heights[Mathf.Clamp(Mathf.RoundToInt(heights.Count * 0.6f), 0, heights.Count - 1)];
            go.transform.position += Vector3.up * (planeY - floor);
        }

        /// <summary>
        /// Flat grass slab over the island: the visible play plane, exactly where the colliders, props and rings live.
        /// Its convex mesh collider is the walkable surface (and the NavMesh bake source).
        /// </summary>
        static GameObject AddField(Transform parent, GameObject island)
        {
            if (!WorldBounds(island, out var b)) return null;
            float a = b.extents.x * FieldInset, c = b.extents.z * FieldInset;
            var grass = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Undo.RegisterCreatedObjectUndo(grass, "Build Task Race Map");
            grass.name = "Field";
            grass.transform.SetParent(parent, false);
            grass.transform.position = new Vector3(b.center.x, -0.11f, b.center.z);
            grass.transform.localScale = new Vector3(a * 2f, 0.11f, c * 2f);
            Object.DestroyImmediate(grass.GetComponent<Collider>());
            var mesh = grass.AddComponent<MeshCollider>();
            mesh.sharedMesh = grass.GetComponent<MeshFilter>().sharedMesh;
            mesh.convex = true;
            UsePaletteCell(grass, 8, 2);   // PALETTE "grass"

            var soil = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Undo.RegisterCreatedObjectUndo(soil, "Build Task Race Map");
            soil.name = "Field_Soil";
            soil.transform.SetParent(parent, false);
            soil.transform.position = new Vector3(b.center.x, -0.26f, b.center.z);
            soil.transform.localScale = new Vector3(a * 2.09f, 0.09f, c * 2.09f);
            Object.DestroyImmediate(soil.GetComponent<Collider>());
            UsePaletteCell(soil, 14, 1);   // PALETTE "dirt"
            return grass;
        }

        /// <summary>Point every UV at one palette cell and use the shared palette material, like the map prefabs.</summary>
        static void UsePaletteCell(GameObject go, int cellX, int cellY, int grid = 16)
        {
            var mf = go.GetComponent<MeshFilter>();
            if (mf != null && mf.sharedMesh != null)
            {
                var mesh = Object.Instantiate(mf.sharedMesh);
                mesh.name = go.name + "_PaletteUV";
                var uv = new Vector2[mesh.vertexCount];
                var centre = new Vector2((cellX + 0.5f) / grid, 1f - (cellY + 0.5f) / grid);
                for (int i = 0; i < uv.Length; i++) uv[i] = centre;
                mesh.uv = uv;
                mf.sharedMesh = mesh;
                var mc = go.GetComponent<MeshCollider>();
                if (mc != null) mc.sharedMesh = mesh;
            }
            var palette = AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/Materials/M_BOB_CurvedMap.mat");
            if (palette == null) return;
            foreach (var r in go.GetComponentsInChildren<MeshRenderer>(true)) r.sharedMaterial = palette;
        }

        static void AlignTop(GameObject go, float topY)
        {
            if (!WorldBounds(go, out var b)) return;
            go.transform.position += Vector3.up * (topY - b.max.y);
        }

        /// <summary>A flat BoxCollider over the walkable footprint with its top exactly on `topY` (the flat play plane).</summary>
        static void AddWalkable(GameObject piece, float topY, float inset = 0.04f)
        {
            if (!WorldBounds(piece, out var b)) return;
            var go = new GameObject("Walkable");
            Undo.RegisterCreatedObjectUndo(go, "Build Task Race Map");
            go.transform.SetParent(piece.transform, true);
            go.transform.rotation = Quaternion.identity; // stay axis-aligned in world, whatever the piece's yaw
            go.transform.localScale = Vector3.one;
            // The bridge is a turned, non-uniformly scaled prefab, so a unit child still inherits a scale. Divide the
            // box by that scale instead of overwriting localScale: setting a 90 degree turn's inverse by hand makes a
            // deck 21 u long and 0.8 u wide (X and Z land on the wrong axes).
            var s = go.transform.lossyScale;
            go.transform.position = new Vector3(b.center.x, topY - 0.5f, b.center.z);
            var box = go.AddComponent<BoxCollider>();
            box.size = new Vector3(
                b.size.x * (1f - inset * 2f) / Mathf.Max(0.0001f, Mathf.Abs(s.x)),
                1f / Mathf.Max(0.0001f, Mathf.Abs(s.y)),
                b.size.z * (1f - inset * 2f) / Mathf.Max(0.0001f, Mathf.Abs(s.z)));
        }

        static bool WorldBounds(GameObject go, out Bounds bounds)
        {
            var renderers = go.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0) { bounds = new Bounds(go.transform.position, Vector3.zero); return false; }
            bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            return true;
        }

        static void Tint(GameObject go, Color colour)
        {
            var m = BobCurvedWorld.CreateMaterial(colour);
            if (m == null) return;
            foreach (var r in go.GetComponentsInChildren<MeshRenderer>(true)) r.sharedMaterial = m;
        }

        static void SwapToCurved(Transform root)
        {
            var shader = Resources.Load<Shader>("Shaders/BobCurvedWorld") ?? Shader.Find("BOB/CurvedWorld");
            if (shader == null) { Debug.LogWarning("[TaskRaceMap] BOB/CurvedWorld shader not found; materials left as-is."); return; }
            int swapped = 0;
            foreach (var r in root.GetComponentsInChildren<Renderer>(true))
            {
                var mats = r.sharedMaterials;
                for (int i = 0; i < mats.Length; i++)
                {
                    var old = mats[i];
                    if (old == null || old.shader == shader) continue;
                    var m = new Material(shader);
                    if (old.HasProperty("_MainTex")) m.mainTexture = old.mainTexture;
                    if (old.HasProperty("_Color")) m.color = old.color;
                    m.SetFloat("_CurvatureScale", 1f);
                    mats[i] = m;
                    swapped++;
                }
                r.sharedMaterials = mats;
            }
            if (swapped > 0) Debug.Log($"[TaskRaceMap] {swapped} material slot(s) switched to BOB/CurvedWorld.");
        }

        static GameObject FindPrefab(params string[] names)
        {
            foreach (var name in names)
            {
                var resource = Resources.Load<GameObject>("Props/SM_" + name) ?? Resources.Load<GameObject>("Props/" + name);
                if (resource != null) return resource;
                foreach (var guid in AssetDatabase.FindAssets(name + " t:Prefab"))
                {
                    var go = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));
                    if (go != null) return go;
                }
            }
            return null;
        }

        static GameObject FindHero()
        {
            try { return GameObject.FindGameObjectWithTag("Player"); }
            catch (UnityException) { return null; }
        }
    }
}
