// Phase 2 of the twin-island kitbash (docs 24 section 15.3): the modular yard each team defends. One layout is built
// on Team A from the props already in Resources/Props and mirrored to Team B by reflecting every transform across the
// island's x = 0 plane (position x -> -x, rotation conjugated by the mirror). No localScale.x = -1 anywhere, so
// colliders, NavMesh, text labels and light normals stay correct on the mirrored island.
//
// The rig is six stations in three pieces of furniture:
//   E-counter (0.85 m, 4.2 m spine, three 1.3 m arms facing the field)  S1 dishes · S2 bins · S3 cart
//   open-leg table with dust underneath                                S4 dust under the table
//   doorless three-tier shelf + washing machine beside it              S5 glass · S6 washer sequence
// plus the 135-degree backdrop wall (1.85 m tapering to 0.35 m curbs) with five WallMountSocket anchors and the three
// nature tiers. Everything is authored for Team A and placed in one call so the scene rebuild reproduces it exactly.
using System.Collections.Generic;
using Bob.TaskRace;
using UnityEditor;
using UnityEngine;

namespace Bob.EditorTools
{
    public static class BobTwinIslandRig
    {
        public const string RigName = "Rig";

        // ---- E-counter: spine along x at z 1.30..1.80, three arms 1.3 m long reaching south to z 0
        // The whole yard sits west of the north bridge mouth: anything east of x ~0.7 with z inside the deck band is
        // the 1.5 u walk-in the clearance check protects, so the counter stops well short of it.
        const float CounterHeight = 0.85f;
        const float SpineLength = 4.2f, SpineDepth = 0.5f, SpineX = -1.75f, SpineZ = 1.55f;
        const float ArmWidth = 0.8f, ArmDepth = 1.3f, ArmZ = 0.65f;
        static readonly float[] ArmX = { -3.15f, -1.75f, -0.35f };

        // ---- backdrop wall: 135-degree arc behind the yard, tall in the middle, curbs at the ends. The arc centre is
        // west of the island centre so the east tip stops 1.4 u short of the north bridge mouth and the apex stays on
        // the island (radius 2.8 -> apex z 4.25 on a 9 u deep island).
        const float WallRadius = 2.8f, WallCentreX = -1.75f, WallCentreZ = 1.45f, WallSweep = 135f, WallThick = 0.16f;
        const float WallTall = 1.85f, WallCurb = 0.35f, WallFullTo = 45f;   // full height out to +-45 deg, then taper
        const int WallSegments = 18;

        // ---- station anchors (island-local, Team A). Each sits in front of its furniture, inside the NEAR radius.
        static readonly Vector3[] Anchors =
        {
            new Vector3(ArmX[0], 0f, 0.85f),   // S1 dishes
            new Vector3(ArmX[1], 0f, 0.85f),   // S2 bins
            new Vector3(ArmX[2], 0f, 0.85f),   // S3 cart
            new Vector3(-4.55f, 0f, 0.55f),    // S4 open-leg table
            new Vector3(3.25f, 0f, 0.85f),     // S5 shelf (wipe glass)
            new Vector3(4.55f, 0f, 0.45f),     // S6 washing machine
        };

        public static IReadOnlyList<Vector3> StationAnchors => Anchors;

        static Material _stationMat, _mapMat;

        /// <summary>Build the whole yard on Team A under propsA, then mirror it to propsB (which must be empty).</summary>
        public static void Build(Transform propsA, Transform propsB, Material stationMat)
        {
            _stationMat = stationMat;
            _mapMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/Materials/M_BOB_CurvedMap.mat") ?? stationMat;
            if (propsA == null || stationMat == null) { Debug.LogError("[TwinRig] propsA / station material missing."); return; }

            var built = new List<Transform>();

            var rig = new GameObject(RigName);
            rig.transform.SetParent(propsA, false);
            built.Add(rig.transform);

            BuildCounter(rig.transform);
            BuildWall(rig.transform);
            BuildNature(rig.transform);

            var stations = BuildStations(propsA);
            built.AddRange(stations);
            BuildTable(stations[3]);
            BuildShelf(stations[4]);
            BuildWasher(stations[5]);

            int mirrored = 0;
            if (propsB != null)
                foreach (var root in built)
                    if (Mirror(root, propsB) != null) mirrored++;
            Debug.Log($"[TwinRig] {built.Count} root(s) built on Team A, {mirrored} mirrored to Team B " +
                      $"(x -> -x, rotation conjugated; no negative scale).");
        }

        // ================================================================ E-counter

        static void BuildCounter(Transform rig)
        {
            Box(rig, "Counter_Spine", new Vector3(SpineX, CounterHeight * 0.5f, SpineZ),
                new Vector3(SpineLength, CounterHeight, SpineDepth), 10, 1, _stationMat);
            Box(rig, "Counter_SpineTop", new Vector3(SpineX, CounterHeight + 0.04f, SpineZ),
                new Vector3(SpineLength + 0.10f, 0.08f, SpineDepth + 0.10f), 10, 2, _stationMat);
            // A low toe kick reads as the E's bar from the play camera and keeps the counter from floating.
            Box(rig, "Counter_Kick", new Vector3(SpineX, 0.06f, SpineZ + 0.16f),
                new Vector3(SpineLength - 0.2f, 0.12f, 0.12f), 10, 0, _stationMat);
        }

        // ================================================================ stations

        static Transform[] BuildStations(Transform props)
        {
            var specs = new[]
            {
                ("S1_A", "ล้างจานกองสูง", TaskCategory.Wash, 15f, "T07"),
                ("S2_A", "แยกขยะ", TaskCategory.Sweep, 6.5f, "T05"),
                ("S3_A", "จัดรถเข็นตามสูตร", TaskCategory.Everyone, 20f, "T04"),
                ("S4_A", "ไล่ฝุ่นใต้โต๊ะ", TaskCategory.Sweep, 6f, "T10"),
                ("S5_A", "เช็ดกระจก", TaskCategory.Wash, 5f, "T08"),
                ("S6_A", "จำลำดับเครื่องซักผ้า", TaskCategory.Everyone, 10f, "T15"),
            };
            var stations = new Transform[specs.Length];
            for (int i = 0; i < specs.Length; i++)
            {
                var (id, name, cat, seconds, minigame) = specs[i];
                var go = new GameObject("Station_" + id);
                go.transform.SetParent(props, false);
                go.transform.localPosition = Anchors[i];
                var spot = go.AddComponent<TaskSpot>();
                spot.Configure(id, name, cat, seconds, minigame);
                spot.SetOwnerTeam(TeamId.TeamA);
                stations[i] = go.transform;
                if (i <= 2) BuildArm(go.transform);
            }

            Dress(stations[0],   // S1 dishes
                ("plates_stack", new Vector3(-0.20f, CounterHeight + 0.08f, 0.30f), 10f, 0.26f),
                ("dish_rack_blue", new Vector3(0.22f, CounterHeight + 0.08f, 0.50f), 90f, 0.34f));
            Dress(stations[1],   // S2 bins
                ("bin_recycle_blue", new Vector3(-0.22f, CounterHeight + 0.08f, 0.45f), 180f, 0.48f),
                ("bin_trash_grey", new Vector3(0.24f, CounterHeight + 0.08f, 0.45f), 180f, 0.46f),
                ("trash_bag_black", new Vector3(0.02f, CounterHeight + 0.08f, 0.72f), -20f, 0.30f));
            Dress(stations[2],   // S3 cart + recipe board
                ("crate_oranges", new Vector3(-0.24f, CounterHeight + 0.08f, 0.30f), 15f, 0.32f),
                ("crate_oranges", new Vector3(0.20f, CounterHeight + 0.08f, 0.42f), -25f, 0.28f),
                ("sack_coffee", new Vector3(0.02f, CounterHeight + 0.08f, 0.66f), 5f, 0.26f),
                ("board_menu", new Vector3(0f, CounterHeight + 0.08f, 0.18f), 180f, 0.34f));
            return stations;
        }

        /// <summary>The arm under S1..S3: its own Counter/CounterTop pair, so the arm counts as the station's
        /// footprint for the bridge clearance check and the contact-shadow decal.</summary>
        static void BuildArm(Transform station)
        {
            float localZ = ArmZ - Anchors[0].z;
            Box(station, "Counter", new Vector3(0f, CounterHeight * 0.5f, localZ),
                new Vector3(ArmWidth, CounterHeight, ArmDepth), 10, 1, _stationMat);
            Box(station, "CounterTop", new Vector3(0f, CounterHeight + 0.04f, localZ),
                new Vector3(ArmWidth + 0.08f, 0.08f, ArmDepth + 0.08f), 10, 2, _stationMat);
        }

        static void Dress(Transform station, params (string prop, Vector3 at, float yaw, float height)[] props)
        {
            foreach (var p in props) Prop(station, p.prop, p.at, p.yaw, p.height, 1.0f);
        }

        // ================================================================ table (S4)

        static void BuildTable(Transform station)
        {
            float h = 0.62f, w = 1.10f, d = 0.70f, z = 0.45f;
            for (int i = 0; i < 4; i++)
            {
                float sx = (i % 2 == 0 ? -1f : 1f), sz = (i < 2 ? -1f : 1f);
                Box(station, i == 0 ? "Counter" : "Leg" + i,
                    new Vector3(sx * (w * 0.5f - 0.07f), h * 0.5f, z + sz * (d * 0.5f - 0.07f)),
                    new Vector3(0.09f, h, 0.09f), 10, 1, _stationMat);
            }
            Box(station, "CounterTop", new Vector3(0f, h + 0.04f, z), new Vector3(w + 0.08f, 0.08f, d + 0.08f), 10, 2, _stationMat);
            Box(station, "TableStretcher", new Vector3(0f, 0.2f, z), new Vector3(w - 0.2f, 0.06f, 0.06f), 10, 1, _stationMat);

            // The dust the station asks the player to chase: three flattened piles under the top, where the ring
            // anchor sits, so the in-world table matches the S28 card.
            Dust(station, new Vector3(-0.28f, 0.05f, z + 0.10f), new Vector3(0.46f, 0.05f, 0.34f), 12f);
            Dust(station, new Vector3(0.16f, 0.05f, z - 0.14f), new Vector3(0.38f, 0.05f, 0.30f), -18f);
            Dust(station, new Vector3(0.32f, 0.05f, z + 0.18f), new Vector3(0.28f, 0.04f, 0.24f), 35f);

            Prop(station, "bucket_metal_blue", new Vector3(0.62f, 0f, z + 0.10f), 30f, 0.30f, 1.0f);
            Prop(station, "brush_scrub_wood", new Vector3(-0.62f, 0f, z + 0.08f), -20f, 0.16f, 1.0f);
        }

        // ================================================================ shelf (S5) + washer (S6)

        static void BuildShelf(Transform station)
        {
            float w = 1.00f, d = 0.42f, h = 1.52f, z = 0.42f;
            Box(station, "ShelfSideW", new Vector3(-(w * 0.5f - 0.03f), h * 0.5f, z), new Vector3(0.06f, h, d), 10, 1, _stationMat);
            Box(station, "ShelfSideE", new Vector3(w * 0.5f - 0.03f, h * 0.5f, z), new Vector3(0.06f, h, d), 10, 1, _stationMat);
            Box(station, "ShelfBack", new Vector3(0f, h * 0.5f, z + d * 0.5f - 0.02f), new Vector3(w, h, 0.04f), 10, 1, _stationMat);
            // three boards = three open tiers; the bottom board is the "Counter" the clearance check measures
            float[] tierY = { 0.34f, 0.78f, 1.22f };
            for (int i = 0; i < tierY.Length; i++)
                Box(station, i == 0 ? "Counter" : "Shelf_Board" + i,
                    new Vector3(0f, tierY[i], z), new Vector3(w, 0.05f, d), 10, 2, _stationMat);
            Box(station, "CounterTop", new Vector3(0f, h + 0.03f, z), new Vector3(w + 0.04f, 0.06f, d + 0.04f), 10, 2, _stationMat);

            var top = Slot(station, "Slot_Top", z, 1.22f + 0.025f);
            var mid = Slot(station, "Slot_Mid", z, 0.78f + 0.025f);
            Slot(station, "Slot_Bottom", z, 0.34f + 0.025f);
            Prop(top, "spray_bottle", Vector3.zero, 0f, 0.26f, 1.0f);
            Prop(mid, "gloves_pair", Vector3.zero, 20f, 0.10f, 1.0f);
        }

        static void BuildWasher(Transform station)
        {
            float z = 0.55f;
            Prop(station, "washing_machine", new Vector3(0f, 0f, z), 180f, 0.84f, 1.4f);
            Box(station, "Counter", new Vector3(0f, 0.04f, z), new Vector3(0.78f, 0.08f, 0.72f), 10, 2, _stationMat);
            Box(station, "CounterTop", new Vector3(0f, 0.87f, z), new Vector3(0.70f, 0.05f, 0.64f), 10, 2, _stationMat);
            Prop(station, "basket_clothes", new Vector3(0.62f, 0f, z + 0.10f), 10f, 0.34f, 1.0f);
            Prop(station, "detergent_pump", new Vector3(-0.56f, 0f, z + 0.12f), -15f, 0.42f, 1.0f);
        }

        static Transform Slot(Transform station, string name, float z, float y)
        {
            var go = new GameObject(name);
            go.transform.SetParent(station, false);
            go.transform.localPosition = new Vector3(0f, y, z - 0.02f);
            return go.transform;
        }

        // ================================================================ backdrop wall

        static void BuildWall(Transform rig)
        {
            var wall = new GameObject("BackdropWall");
            wall.transform.SetParent(rig, false);

            float step = WallSweep / WallSegments;
            for (int i = 0; i < WallSegments; i++)
            {
                float a0 = -WallSweep * 0.5f + i * step;
                float a = a0 + step * 0.5f;
                float h = WallHeight(a);
                float rad = a * Mathf.Deg2Rad;
                var p = new Vector3(WallCentreX + WallRadius * Mathf.Sin(rad), h * 0.5f,
                                    WallCentreZ + WallRadius * Mathf.Cos(rad));
                var seg = Box(wall.transform, "WallSeg" + i.ToString("00"), p,
                    new Vector3(WallRadius * Mathf.Deg2Rad * step * 1.06f, h, WallThick), 10, 2, _mapMat);
                seg.transform.localRotation = Quaternion.Euler(0f, a, 0f);
            }
            foreach (float end in new[] { -1f, 1f })
            {
                float rad0 = end * WallSweep * 0.5f * Mathf.Deg2Rad;
                var cap = Box(wall.transform, end < 0f ? "WallCurbW" : "WallCurbE",
                    new Vector3(WallCentreX + WallRadius * Mathf.Sin(rad0), WallCurb * 0.5f,
                                WallCentreZ + WallRadius * Mathf.Cos(rad0)),
                    new Vector3(0.42f, WallCurb, WallThick + 0.06f), 10, 2, _mapMat);
                cap.transform.localRotation = Quaternion.Euler(0f, end * WallSweep * 0.5f, 0f);
            }

            var mounts = new (float angle, string prop)[]
            {
                (-54f, "industrial_fan"),
                (-27f, "shelf_wall_books"),
                (0f, "tv_flat"),
                (27f, "clock_alarm"),
                (54f, "ac_wall_teal"),
            };
            int index = 1;
            foreach (var (angle, propName) in mounts)
            {
                float rad = angle * Mathf.Deg2Rad;
                float inset = WallThick * 0.5f + 0.02f;
                float mountHeight = Mathf.Min(WallHeight(angle) * 0.62f, 1.30f);
                var socketGo = new GameObject("WallSocket_" + index, typeof(WallMountSocket));
                socketGo.transform.SetParent(wall.transform, false);
                socketGo.transform.localPosition = new Vector3(
                    WallCentreX + WallRadius * Mathf.Sin(rad) - Mathf.Sin(rad) * inset,
                    mountHeight,
                    WallCentreZ + WallRadius * Mathf.Cos(rad) - Mathf.Cos(rad) * inset);
                socketGo.transform.localRotation = Quaternion.Euler(0f, angle + 180f, 0f);
                var socket = socketGo.GetComponent<WallMountSocket>();
                socket.index = index++;
                socket.prop = "SM_" + propName;
                Mount(socketGo.transform, propName, MountHeight(propName));
            }
        }

        /// <summary>1.85 m across the middle 90 degrees, then a linear taper to the 0.35 m curb at +-67.5 degrees.</summary>
        static float WallHeight(float angle)
        {
            float a = Mathf.Abs(angle);
            if (a <= WallFullTo) return WallTall;
            return Mathf.Lerp(WallTall, WallCurb, Mathf.InverseLerp(WallFullTo, WallSweep * 0.5f, a));
        }

        static float MountHeight(string prop) => prop switch
        {
            "clock_alarm" => 0.26f,
            "ac_wall_teal" => 0.40f,
            "tv_flat" => 0.52f,
            "shelf_wall_books" => 0.58f,
            _ => 0.50f,
        };

        // ================================================================ nature (three tiers)

        static void BuildNature(Transform rig)
        {
            var nature = new GameObject("Nature");
            nature.transform.SetParent(rig, false);

            // Tier 1, large corner greenery: the biggest plants the library has, kept on the outer (west) rim and the
            // east strip between the two bridge ramps, with a width cap so nothing leans onto a walk-in.
            Prop(nature.transform, "bush_green", new Vector3(-4.85f, 0f, -2.95f), 20f, 1.30f, 1.80f);
            Prop(nature.transform, "bush_green", new Vector3(-4.85f, 0f, -0.65f), 205f, 1.15f, 1.80f);
            Prop(nature.transform, "bush_green", new Vector3(5.00f, 0f, -0.55f), 140f, 1.20f, 1.70f);

            // Tier 2, medium pots and planters, at 0.85-1.2x of their natural height, yaw varied so no two match.
            var medium = new (string prop, Vector3 at, float yaw, float h)[]
            {
                ("plant_pot", new Vector3(-5.30f, 0f, 1.15f), 0f, 0.95f),
                ("pot_blue", new Vector3(-5.25f, 0f, 2.45f), 25f, 0.62f),
                ("pot_ceramic_gold", new Vector3(-5.25f, 0f, -1.95f), 70f, 0.58f),
                ("plant_pot", new Vector3(-3.30f, 0f, 3.05f), 12f, 0.85f),
            };
            foreach (var (propName, at, yaw, h) in medium) Prop(nature.transform, propName, at, yaw, h, 0.9f);

            // Tier 3, small tufts and stones that tie the furniture into the grass.
            var small = new (Vector3 at, float yaw)[]
            {
                (new Vector3(-4.65f, 0f, 1.65f), 0f),
                (new Vector3(0.85f, 0f, 0.35f), 40f),
                (new Vector3(-1.60f, 0f, -0.95f), 15f),
                (new Vector3(-2.95f, 0f, -1.30f), 65f),
            };
            foreach (var (at, yaw) in small) Prop(nature.transform, "rocks_grass", at, yaw, 0.22f, 0.8f);
        }

        // ================================================================ mirror

        /// <summary>Copy `src` under `dstParent` as an exact reflection across the container's x = 0 plane. The
        /// reflection of a TRS node is just its own TRS mirrored (x negated, rotation quaternion conjugated, scale
        /// kept), so no node ever carries a negative scale. Task spots on the copy get the _B id and the TeamB owner.</summary>
        static Transform Mirror(Transform src, Transform dstParent)
        {
            if (src == null) return null;
            var copy = Object.Instantiate(src.gameObject, dstParent, false);
            copy.name = src.name.EndsWith("_A") ? src.name.Substring(0, src.name.Length - 2) + "_B" : src.name;
            Undo.RegisterCreatedObjectUndo(copy, "Build Task Race Map");

            var srcNodes = src.GetComponentsInChildren<Transform>(true);
            var dstNodes = copy.GetComponentsInChildren<Transform>(true);

            // Depth-first order puts every parent before its children, so each local value can be mirrored on its own.
            for (int i = 0; i < srcNodes.Length && i < dstNodes.Length; i++)
            {
                var s = srcNodes[i];
                var d = dstNodes[i];
                var p = s.localPosition; p.x = -p.x;
                var r = s.localRotation;
                d.localPosition = p;
                d.localRotation = new Quaternion(r.x, -r.y, -r.z, r.w);
                d.localScale = s.localScale;
            }

            foreach (var spot in copy.GetComponentsInChildren<TaskSpot>(true))
            {
                string id = spot.SpotId.EndsWith("_A") ? spot.SpotId.Substring(0, spot.SpotId.Length - 2) + "_B"
                                                        : spot.SpotId + "_B";
                spot.SetSpotId(id);
                spot.SetOwnerTeam(TeamId.TeamB);
            }
            return copy.transform;
        }

        // ================================================================ primitives

        static GameObject Box(Transform parent, string name, Vector3 centre, Vector3 size, int cellX, int cellY,
                              Material mat)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Undo.RegisterCreatedObjectUndo(go, "Build Task Race Map");
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = centre;
            go.transform.localScale = size;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.GetComponent<MeshRenderer>().sharedMaterial = mat;
            Cell(go, cellX, cellY);
            return go;
        }

        static void Dust(Transform parent, Vector3 at, Vector3 size, float yaw)
        {
            var go = Box(parent, "Dust", at, size, 10, 0, _stationMat);
            go.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
        }

        /// <summary>Instantiate a prop from Resources/Props, scale it to a target height (and optionally a max width)
        /// and sit its base at `at.y`. `maxWidth` keeps the two-metre bushes off the counter and the bridge lanes.</summary>
        static Transform Prop(Transform parent, string name, Vector3 at, float yaw, float height, float maxWidth = 0f)
        {
            var prefab = Resources.Load<GameObject>("Props/SM_" + name);
            if (prefab == null) { Debug.LogWarning("[TwinRig] prefab missing: SM_" + name); return null; }
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            go.name = "SM_" + name;
            go.transform.localPosition = at;
            go.transform.localRotation = Quaternion.Euler(0f, yaw, 0f) * prefab.transform.localRotation;

            if (!Bounds(go, out var b0)) return go.transform;
            if (height > 0.01f && b0.size.y > 0.001f)
                go.transform.localScale *= height / b0.size.y;
            if (maxWidth > 0.01f && Bounds(go, out var b1))
            {
                float wide = Mathf.Max(b1.size.x, b1.size.z);
                if (wide > maxWidth) go.transform.localScale *= maxWidth / wide;
            }
            if (Bounds(go, out var b2))
                go.transform.localPosition = at + Vector3.up * (at.y - b2.min.y);
            return go.transform;
        }

        /// <summary>Hang a prop on a socket: centred on the socket's origin and pushed out of the wall along its +Z.</summary>
        static void Mount(Transform socket, string name, float height)
        {
            var t = Prop(socket, name, Vector3.zero, 0f, height, 1.4f);
            if (t == null || !Bounds(t.gameObject, out var b)) return;
            var centreLocal = socket.InverseTransformPoint(b.center);
            t.localPosition = -centreLocal + new Vector3(0f, 0f, b.extents.z * 0.9f);
        }

        static bool Bounds(GameObject go, out Bounds b)
        {
            var renderers = go.GetComponentsInChildren<Renderer>();
            b = default;
            if (renderers.Length == 0) return false;
            b = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) b.Encapsulate(renderers[i].bounds);
            return true;
        }

        /// <summary>Point every UV at one palette cell so an untextured cube samples the map palette like a map piece.</summary>
        static void Cell(GameObject go, int cellX, int cellY, int grid = 16)
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
    }
}
