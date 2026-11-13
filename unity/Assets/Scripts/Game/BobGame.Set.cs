// Battle of the Brooms: the 3D set around the play floor (cafeteria first, as in the target picture): tiled floor, a teal play mat,
// long tables with plates, benches, a back wall with shelves, station signs. Everything uses the toon material and outline from
// Modular/BobToon.cs, so the 3D heroes that come later sit in the same world. World axes: x right, y up, z = room depth.
using Bob.Art;
using Bob.Modular;
using TMPro;
using UnityEngine;

namespace Bob.Game
{
    public partial class BobGame
    {
        static readonly Color CreamWall = new Color(0.93f, 0.97f, 0.92f), TableTop = new Color(1f, 0.89f, 0.55f), TableSide = new Color(0.96f, 0.72f, 0.36f),
            Plate = new Color(0.98f, 0.97f, 0.95f), PlateMat = new Color(0.96f, 0.56f, 0.45f), Mat = new Color(0.62f, 0.86f, 0.80f);

        Transform setRoot;
        readonly System.Collections.Generic.List<TextMeshPro> signs = new System.Collections.Generic.List<TextMeshPro>();

        Texture2D TileTexture(Color a, Color b)
        {
            const int N = 64; var tex = new Texture2D(N, N, TextureFormat.RGBA32, true) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Repeat };
            for (int y = 0; y < N; y++) for (int x = 0; x < N; x++)
                {
                    bool edge = x < 2 || y < 2 || x >= N - 1 || y >= N - 1;
                    bool alt = (x / 32 + y / 32) % 2 == 0;
                    var c = edge ? Color.Lerp(a, b, 0.5f) * 0.92f : alt ? a : b; c.a = 1; tex.SetPixel(x, y, c);
                }
            tex.Apply(); return tex;
        }

        void Quad(string name, Vector3 centre, Vector2 size, Color c, Texture tex = null, Vector2? tiling = null)
        {
            var q = GameObject.CreatePrimitive(PrimitiveType.Quad); q.name = name; q.transform.SetParent(setRoot, false);
            var col = q.GetComponent<Collider>(); if (col != null) Destroy(col);
            q.transform.position = centre; q.transform.rotation = Quaternion.Euler(90, 0, 0); q.transform.localScale = new Vector3(size.x, size.y, 1);
            var m = BobToon.Toon(c, tex); if (tex != null && tiling.HasValue) m.mainTextureScale = tiling.Value;
            q.GetComponent<MeshRenderer>().sharedMaterial = m;
        }

        void Table(Vector3 centre, Vector3 size)
        {
            // top, side apron, four legs, and plates on top
            BobToon.Shape(PrimitiveType.Cube, setRoot, "Table", centre + new Vector3(0, 0.62f, 0), new Vector3(size.x, 0.1f, size.z), TableTop, true, 2.5f);
            BobToon.Shape(PrimitiveType.Cube, setRoot, "Apron", centre + new Vector3(0, 0.5f, 0), new Vector3(size.x * 0.94f, 0.14f, size.z * 0.94f), TableSide, false);
            foreach (var sx in new[] { -1, 1 }) foreach (var sz in new[] { -1, 1 })
                    BobToon.Shape(PrimitiveType.Cube, setRoot, "Leg", centre + new Vector3(sx * (size.x / 2 - 0.08f), 0.28f, sz * (size.z / 2 - 0.12f)), new Vector3(0.1f, 0.56f, 0.1f), TableSide, false);
            bool alongZ = size.z > size.x; int n = Mathf.Max(2, Mathf.FloorToInt((alongZ ? size.z : size.x) / 1.0f));
            for (int i = 0; i < n; i++)
            {
                float t = (i + 0.5f) / n - 0.5f; var off = alongZ ? new Vector3(0, 0.7f, t * (size.z - 0.5f)) : new Vector3(t * (size.x - 0.5f), 0.7f, 0);
                BobToon.Shape(PrimitiveType.Cylinder, setRoot, "Mat", centre + off - new Vector3(0, 0.04f, 0), new Vector3(0.46f, 0.012f, 0.46f), PlateMat, false);
                BobToon.Shape(PrimitiveType.Cylinder, setRoot, "Plate", centre + off, new Vector3(0.34f, 0.018f, 0.34f), Plate, false);
                BobToon.Shape(PrimitiveType.Sphere, setRoot, "Food", centre + off + new Vector3(0.16f, 0.06f, 0), new Vector3(0.12f, 0.1f, 0.12f), new Color(1f, 0.85f, 0.35f), false);
            }
        }

        void Bench(Vector3 centre, Vector3 size)
        {
            BobToon.Shape(PrimitiveType.Cube, setRoot, "Bench", centre + new Vector3(0, 0.3f, 0), new Vector3(size.x, 0.1f, size.z), new Color(0.98f, 0.8f, 0.45f), true, 2f);
            foreach (var s in new[] { -1, 1 }) BobToon.Shape(PrimitiveType.Cube, setRoot, "BenchLeg", centre + new Vector3(0, 0.14f, s * (size.z / 2 - 0.2f)), new Vector3(size.x * 0.9f, 0.28f, 0.08f), TableSide, false);
        }

        /// <summary>Everything outside the 8 x 5 play floor. The floor tiles of the play area itself are the teal mat.</summary>
        void BuildSet()
        {
            setRoot = new GameObject("Set").transform;
            // light: Art Bible 21 section 6.1 (warm key from upper left, blue-violet ambient, no hard black)
            var lightGo = new GameObject("Key"); lightGo.transform.SetParent(setRoot, false);
            var sun = lightGo.AddComponent<Light>(); sun.type = LightType.Directional; sun.intensity = 1.0f; sun.color = new Color(1f, 0.957f, 0.878f);
            sun.shadows = LightShadows.None; lightGo.transform.rotation = Quaternion.Euler(50, -30, 0);
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.867f, 0.922f, 0.98f); RenderSettings.ambientEquatorColor = new Color(0.75f, 0.74f, 0.86f); RenderSettings.ambientGroundColor = new Color(0.56f, 0.525f, 0.75f);
            RenderSettings.ambientLight = new Color(0.7f, 0.72f, 0.8f);

            // floor: pale tiles everywhere, the play area is a teal mat with a light border
            var tiles = TileTexture(new Color(1f, 0.97f, 0.82f), new Color(0.98f, 0.92f, 0.70f));
            Quad("Floor", new Vector3(RoomW / 2, -0.02f, RoomH / 2), new Vector2(40, 30), Color.white, tiles, new Vector2(20, 15));
            Quad("MatBorder", new Vector3(RoomW / 2, -0.012f, RoomH / 2), new Vector2(RoomW + 0.5f, RoomH + 0.5f), new Color(0.80f, 0.94f, 0.90f));
            Quad("PlayMat", new Vector3(RoomW / 2, -0.008f, RoomH / 2), new Vector2(RoomW, RoomH), Mat);
            foreach (var sx in new[] { 0.12f, RoomW - 0.12f }) Quad("Lane", new Vector3(sx, -0.004f, RoomH / 2), new Vector2(0.06f, RoomH - 0.4f), new Color(1f, 0.88f, 0.35f));

            // back wall with shelves and a counter, low side walls
            BobToon.Shape(PrimitiveType.Cube, setRoot, "BackWall", new Vector3(RoomW / 2, 1.2f, RoomH + 1.5f), new Vector3(16f, 2.4f, 0.3f), CreamWall, true, 2.5f);
            BobToon.Shape(PrimitiveType.Cube, setRoot, "BackTrim", new Vector3(RoomW / 2, 0.3f, RoomH + 1.32f), new Vector3(16f, 0.6f, 0.1f), new Color(0.62f, 0.82f, 0.78f), false);
            for (int i = 0; i < 4; i++)
            {
                float x = 0.2f + i * 2.5f;
                BobToon.Shape(PrimitiveType.Cube, setRoot, "Shelf", new Vector3(x, 1.35f, RoomH + 1.28f), new Vector3(1.5f, 0.08f, 0.35f), TableSide, true, 2f);
                for (int k = 0; k < 4; k++) BobToon.Shape(PrimitiveType.Cylinder, setRoot, "Jar", new Vector3(x - 0.5f + k * 0.33f, 1.5f, RoomH + 1.28f), new Vector3(0.18f, 0.12f, 0.18f), k % 2 == 0 ? new Color(0.55f, 0.78f, 0.95f) : new Color(0.98f, 0.62f, 0.5f), false);
            }
            BobToon.Shape(PrimitiveType.Cube, setRoot, "Counter", new Vector3(RoomW / 2, 0.45f, RoomH + 1.0f), new Vector3(13f, 0.9f, 0.7f), new Color(0.72f, 0.80f, 0.92f), true, 2.5f);
            BobToon.Shape(PrimitiveType.Cube, setRoot, "CounterTop", new Vector3(RoomW / 2, 0.93f, RoomH + 1.0f), new Vector3(13.1f, 0.08f, 0.8f), new Color(0.62f, 0.45f, 0.45f), false);

            // long tables with plates on both sides (benches on the play-floor side), a row across the front
            Table(new Vector3(-1.9f, 0, RoomH / 2 + 0.2f), new Vector3(1.0f, 0.7f, 5.4f));
            Table(new Vector3(-3.5f, 0, RoomH / 2 + 0.2f), new Vector3(1.0f, 0.7f, 5.4f));
            Table(new Vector3(RoomW + 1.9f, 0, RoomH / 2 + 0.2f), new Vector3(1.0f, 0.7f, 5.4f));
            Table(new Vector3(RoomW + 3.5f, 0, RoomH / 2 + 0.2f), new Vector3(1.0f, 0.7f, 5.4f));
            Bench(new Vector3(-0.85f, 0, RoomH / 2 + 0.2f), new Vector3(0.4f, 0.3f, 5.2f));
            Bench(new Vector3(RoomW + 0.85f, 0, RoomH / 2 + 0.2f), new Vector3(0.4f, 0.3f, 5.2f));
            for (int i = 0; i < 3; i++) Table(new Vector3(1.2f + i * 2.8f, 0, -1.9f), new Vector3(2.4f, 0.7f, 1.0f));
            for (int i = 0; i < 3; i++) Table(new Vector3(1.2f + i * 2.8f, 0, -3.6f), new Vector3(2.4f, 0.7f, 1.0f));
            BuildSigns();
        }

        void BuildSigns()
        {
            var cols = new[] { new Color(1f, 0.62f, 0.2f), new Color(0.35f, 0.65f, 0.95f), new Color(0.93f, 0.28f, 0.38f), new Color(0.58f, 0.46f, 0.88f) };
            for (int i = 0; i < 4; i++)
            {
                var r = StationRects[i];
                var plank = BobToon.Shape(PrimitiveType.Cube, setRoot, "SignBoard_" + StationIds[i], new Vector3(r.center.x, 1.55f, r.yMin - 0.05f), new Vector3(1.5f, 0.38f, 0.06f), cols[i], true, 2f);
                plank.transform.rotation = Quaternion.Euler(CamPitch - 90f + 90f - 90f, 0, 0);
                var go = new GameObject("SignText_" + StationIds[i]); go.transform.SetParent(setRoot, false);
                go.transform.position = new Vector3(r.center.x, 1.55f, r.yMin - 0.12f);
                var t = go.AddComponent<TextMeshPro>();
                if (BobUi.HeadingTmp != null) t.font = BobUi.HeadingTmp;
                t.text = StationNames[i]; t.fontSize = 2.2f; t.alignment = TextAlignmentOptions.Center; t.color = Color.white; t.textWrappingMode = TextWrappingModes.NoWrap;
                t.outlineColor = BobToon.LineColor; t.outlineWidth = 0.25f;
                signs.Add(t);
            }
        }

        // ---------------------------------------------------------------- 3D hero model (Art Bible 21 D2): used when Resources/Heroes/SK_<id> exists

        GameObject heroModel; Animator heroAnim; float heroYaw;

        /// <summary>Swap the hero sprite for a 3D model from the art pipeline (Tripo / Blender kit). Falls back to the sprite when none is imported.</summary>
        void TryHeroModel()
        {
            if (heroModel != null) { Destroy(heroModel); heroModel = null; heroAnim = null; }
            if (!World3D || playerSr == null) return;
            var prefab = Resources.Load<GameObject>("Heroes/SK_" + charId); Debug.Log("Bob: hero model " + charId + " -> " + (prefab != null ? "3D prefab" : "sprite"));
            playerSr.enabled = prefab == null;
            if (prefab == null) return;
            heroModel = Instantiate(prefab); heroModel.name = "Hero3D";
            // scale to the hero height of the 2D sprite so the room stays in proportion (Art Bible: hero 1.15 u in a 14 x 8 room, ours is 8 x 5)
            var rs = heroModel.GetComponentsInChildren<Renderer>(); var b = rs.Length > 0 ? rs[0].bounds : new Bounds(Vector3.zero, Vector3.one);
            foreach (var r in rs) b.Encapsulate(r.bounds);
            float k = 0.95f / Mathf.Max(0.01f, b.size.y); heroModel.transform.localScale *= k;
            foreach (var r in rs)
            {
                var mats = r.sharedMaterials;
                for (int i = 0; i < mats.Length; i++)
                {
                    var src = mats[i]; Texture tex = src != null && src.HasProperty("_MainTex") ? src.mainTexture : null;
                    var col = src != null && src.HasProperty("_Color") ? src.color : Color.white;
                    mats[i] = BobToon.Toon(col, tex);
                }
                r.sharedMaterials = mats;
                if (r is MeshRenderer || r is SkinnedMeshRenderer) BobToon.Outline(r.gameObject, 2.5f);
            }
            heroAnim = heroModel.GetComponentInChildren<Animator>();
        }

        void TickHeroModel(float dt)
        {
            if (heroModel == null) return;
            float bounce = lastMove != Vector2.zero ? Mathf.Abs(Mathf.Sin(Time.time * 14f)) * 0.05f : 0f;
            heroModel.transform.position = new Vector3(pos.x, bounce, pos.y);
            if (lastMove.sqrMagnitude > 0.00001f) heroYaw = Mathf.LerpAngle(heroYaw, Mathf.Atan2(lastMove.x, lastMove.y) * Mathf.Rad2Deg, 1 - Mathf.Exp(-14f * dt));
            heroModel.transform.rotation = Quaternion.Euler(0, heroYaw, 0);
            if (heroAnim != null)
            {
                if (HasParam(heroAnim, "Speed")) heroAnim.SetFloat("Speed", player.Velocity.magnitude);
                if (HasParam(heroAnim, "Carry")) heroAnim.SetBool("Carry", hand.Count > 0);
            }
        }

        static bool HasParam(Animator a, string n) { foreach (var p in a.parameters) if (p.name == n) return true; return false; }

        /// <summary>Signs and their boards always face the camera.</summary>
        void FaceSigns()
        {
            if (cam == null) return;
            foreach (var s in signs) if (s != null) s.transform.rotation = cam.transform.rotation;
            if (setRoot == null) return;
            foreach (Transform c in setRoot) if (c.name.StartsWith("SignBoard_")) c.rotation = cam.transform.rotation;
        }
    }
}
