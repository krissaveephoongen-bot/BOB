// Battle of the Brooms: the 3D stage behind the S28 card (docs 11 section 4.4, docs 16 sections 5-6). The card's
// play area used to be flat uGUI shapes; it is now a RenderTexture lit stage with the imported props and slime rigs,
// so a card minigame happens on the same kind of object the map is made of.
//
// The camera is orthographic and looks down at a big table at 18 degrees. The play area is 1240 x 620 px, so
// 100 px = 1 world unit. Every anchor is projected onto the table plane (OnCounter), which is why a 2D hitbox and
// its 3D body always agree: props stand in a back row, the toys rest on the table in front of them.
//
// The stage lives on its own layer (31) 400 units under the map: its lights only touch that layer, and every other
// camera drops the layer from its mask, so nothing here can leak into the play view or into a verify shot.
using System.Collections.Generic;
using Bob.Game;
using Bob.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Bob.TaskRace
{
    [DisallowMultipleComponent]
    public class BobMinigameStage : MonoBehaviour
    {
        public const float PxPerUnit = 100f;
        public const int StageLayer = 31;

        const float Pitch = 18f;      // camera tilt onto the table
        const float CamUp = 4f;       // camera height above the table
        const float CamBack = 1.31f;  // camera z, stage-local
        const float PropRow = 70f;    // play-area row the backdrop props stand on
        const int RtW = 1240, RtH = 620;

        const float SinPitch = 0.3090f, CosPitch = 0.9511f; // sin/cos 18: the table projection needs them every frame

        static BobUiTheme T => BobUiTheme.Current;
        static Texture2D softDot;

        Camera cam;
        RenderTexture rt;
        RawImage view;
        Transform origin;
        Material lit, glass, shadow;

        readonly List<GameObject> spawned = new List<GameObject>();
        readonly List<Transform> sway = new List<Transform>();
        bool built;

        public bool Ready => built;

        // ---------------------------------------------------------------- build

        public void Build(RectTransform play)
        {
            if (built) return;
            built = true;
            if (!Live.Contains(this)) Live.Add(this); // edit-mode captures ask the list directly; Awake never runs there

            view = BobUi.Rect("Mini3D", play).gameObject.AddComponent<RawImage>();
            BobUi.Stretch((RectTransform)view.transform);
            view.raycastTarget = false;
            view.transform.SetAsFirstSibling();

            rt = new RenderTexture(RtW, RtH, 16, RenderTextureFormat.ARGB32) { antiAliasing = 4, name = "S28Stage" };
            rt.Create();
            view.texture = rt;

            // A world root, not a child of the card: the card's open animation scales its transform from 0, and any
            // inherited scale/rotation would collapse the stage (edit mode never runs that animation, so it stays 0).
            var holder = new GameObject("MiniStage");
            origin = holder.transform;
            origin.position = new Vector3(0f, -400f, 0f);
            origin.rotation = Quaternion.identity;
            origin.localScale = Vector3.one;

            var camGo = new GameObject("MiniStageCam", typeof(Camera));
            camGo.transform.SetParent(origin, false);
            // Sits CamUp over the table and CamBack behind it, so the table fills the frame and the props read as a row.
            camGo.transform.localPosition = new Vector3(0f, CamUp, -CamBack);
            camGo.transform.localRotation = Quaternion.Euler(Pitch, 0f, 0f);
            cam = camGo.GetComponent<Camera>();
            cam.cullingMask = 1 << StageLayer;
            cam.orthographic = true;
            cam.orthographicSize = 3.1f;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 40f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.84f, 0.92f, 0.97f);
            cam.allowHDR = false;
            cam.allowMSAA = true;
            cam.targetTexture = rt;
            cam.enabled = false;

            // One key light only: BOB/ToonLit has no ForwardAdd pass, so a second directional light would be ignored,
            // and anything above 1.0 on this pale table clips to white and takes the seams and shadows with it.
            var key = LightGo("MiniKey", new Vector3(38f, -28f, 0f), 0.85f, new Color(1f, 0.97f, 0.91f));
            key.shadows = LightShadows.Soft; // contact shadows are what makes the props sit on the table
            key.shadowStrength = 0.65f;
            key.shadowBias = 0.02f;

            // The stage layer must not exist anywhere else in the picture.
            foreach (var c in Camera.allCameras)
                if (c != null && c != cam) c.cullingMask &= ~(1 << StageLayer);

            lit = Material("BOB/ToonLit", Color.white);
            glass = Material("Sprites/Default", new Color(1f, 1f, 1f, 0.35f));
            shadow = new Material(Shader.Find("Sprites/Default")) { name = "S28Stage_Shadow", mainTexture = SoftDot() };
        }

        /// <summary>A soft round alpha mask for the contact shadows: at card size a blob reads better than a shadow map.</summary>
        static Texture2D SoftDot()
        {
            if (softDot != null) return softDot;
            const int n = 64;
            softDot = new Texture2D(n, n, TextureFormat.RGBA32, false) { name = "S28Stage_SoftDot", wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[n * n];
            for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float dx = (x + 0.5f) / n * 2f - 1f, dy = (y + 0.5f) / n * 2f - 1f;
                float a = Mathf.Clamp01(1f - Mathf.Sqrt(dx * dx + dy * dy));
                px[y * n + x] = new Color32(255, 255, 255, (byte)(Mathf.RoundToInt(a * a * 255f)));
            }
            softDot.SetPixels32(px);
            softDot.Apply();
            return softDot;
        }

        static readonly List<BobMinigameStage> Live = new List<BobMinigameStage>();

        void OnDestroy()
        {
            Live.Remove(this);
            if (rt != null)
            {
                rt.Release();
                DestroyNow(rt);
            }
            if (origin != null) DestroyNow(origin.gameObject); // the stage is a world root, it outlives no host
        }

        /// <summary>Edit-mode captures render one camera by hand and no engine camera runs, so the verify tool asks
        /// every open stage to draw its texture before the shot. Play mode never needs this.</summary>
        public static void RenderForCapture()
        {
            for (int i = 0; i < Live.Count; i++)
            {
                var s = Live[i];
                if (s == null || s.cam == null || !s.cam.enabled) continue;
                s.cam.Render();
            }
        }

        Light LightGo(string name, Vector3 euler, float intensity, Color colour)
        {
            var go = new GameObject(name, typeof(Light));
            go.transform.SetParent(origin, false);
            go.transform.localRotation = Quaternion.Euler(euler);
            var l = go.GetComponent<Light>();
            l.type = LightType.Directional;
            l.intensity = intensity;
            l.color = colour;
            l.cullingMask = 1 << StageLayer;
            l.renderMode = LightRenderMode.ForcePixel;
            return l;
        }

        static Material Material(string shader, Color colour)
        {
            var s = Shader.Find(shader) ?? Shader.Find("Unlit/Color");
            var m = new Material(s) { name = "S28Stage_" + shader.Replace('/', '_') };
            if (m.HasProperty("_Color")) m.SetColor("_Color", colour);
            if (m.HasProperty("_DirtAmount")) m.SetFloat("_DirtAmount", 0f);
            return m;
        }

        // ---------------------------------------------------------------- show / hide

        /// <summary>Swap the stage to this task: backdrop plus the props the task is about. Safe to call per step.</summary>
        public void Show(string taskId)
        {
            if (!built) return;
            Clear();
            cam.enabled = true;
            view.enabled = true;
            BuildBackdrop(taskId);

            switch (taskId)
            {
                case "T01":
                    Prop("SM_brush_scrub_wood", 1.0f, -4.1f);
                    Prop("SM_bucket_blue", 1.4f, 4.1f);
                    break;
                case "T14":
                    Prop("SM_brush_scrub_wood", 1.0f, -4.4f);
                    Prop("SM_bucket_metal_blue", 1.4f, 4.3f);
                    break;
                case "T02":
                    Prop("SM_sink_cabinet", 2.0f, 0f);
                    Prop("SM_spray_bottle", 1.2f, -4.5f);
                    break;
                case "T07":
                    Prop("SM_sink_cabinet", 2.0f, -0.6f);
                    Prop("SM_dish_rack_blue", 1.3f, 4.2f);
                    break;
                case "T08":
                    Prop("SM_window_arched", 2.4f, 1.6f, 90f); // the arch is a wall piece: turn its face to the camera
                    Prop("SM_bucket_blue", 1.2f, -4.5f); // not a spray bottle: the tap target already is one
                    break;
                case "T09":
                    Prop("SM_sink_cabinet", 2.0f, -0.6f);
                    Prop("SM_plates_stack", 1.0f, 4.0f);
                    break;
                case "T03":
                    Prop("SM_spray_bottle", 1.3f, -4.6f);
                    break;
                case "T05":
                    // Above the sortable cards (rows 130 / 10) and below the card's top edge: row + height <= 310.
                    Prop("SM_bin_recycle_blue", 1.0f, -3.9f, 0f, 205f);
                    Prop("SM_bin_trash_grey", 1.0f, 0f, 0f, 205f);
                    Prop("SM_bin_popup_blue", 1.0f, 3.9f, 0f, 205f);
                    break;
                case "T06":
                    Prop("SM_brush_pair_yellow", 1.3f, -4.2f); // the knob owns the centre, the meter the right edge
                    break;
                case "T10":
                    Prop("SM_bed_single_blue", 1.5f, 0f); // reads "the dust under the bed"
                    break;
                case "T11":
                    Prop("SM_detergent_pump", 1.8f, -2.6f);
                    break;
                case "T13":
                    Prop("SM_crate_oranges", 1.4f, 0f);
                    break;
                case "T15":
                    Prop("SM_washing_machine", 1.8f, -5.0f, 0f, -240f); // the four pads own the top half of the card
                    break;
            }
        }

        /// <summary>The table the whole card happens on: a slab of boards under the props, with seams for depth cues.</summary>
        void BuildBackdrop(string taskId)
        {
            // A warm wood under the cream card: pale enough for the pastel props, dark enough that the light does not
            // clip the surface to white and take the seams and shadows with it.
            var top = taskId == "T08" ? new Color(0.86f, 0.88f, 0.92f)
                    : taskId == "T09" ? new Color(0.88f, 0.83f, 0.73f)
                    : new Color(0.90f, 0.85f, 0.74f);
            var gap = new Color(top.r - 0.10f, top.g - 0.11f, top.b - 0.12f);
            Slab("Table", gap, new Vector3(0f, 0f, 11f), new Vector3(90f, 0f, 0f), new Vector3(34f, 34f, 1f));
            for (int i = 0; i < 12; i++)
            {
                float d = i % 2 == 0 ? 0f : 0.016f; // alternate boards, so the table reads as planks and not one flat field
                var plank = new Color(top.r - d, top.g - d, top.b - d * 0.7f);
                Slab("Plank", plank, new Vector3(0f, 0.003f, -4.4f + i * 2.4f), new Vector3(90f, 0f, 0f),
                    new Vector3(34f, 2.26f, 1f));
            }
        }

        /// <summary>A flat quad lying in the stage, in stage-local terms (the table, its seams).</summary>
        void Slab(string name, Color colour, Vector3 localPos, Vector3 euler, Vector3 scale)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.name = name;
            go.layer = StageLayer;
            go.transform.SetParent(origin, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = Quaternion.Euler(euler);
            go.transform.localScale = scale;
            var col = go.GetComponent<Collider>();
            if (col != null) DestroyNow(col);
            var mr = go.GetComponent<MeshRenderer>();
            mr.sharedMaterial = new Material(lit) { color = colour }; // lit, so the table takes the props' shadows
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = true;
            spawned.Add(go);
        }

        /// <summary>Drop one prop in the back row, scaled to the height the task needs, standing on the table.</summary>
        void Prop(string resource, float height, float xUnits, float yaw = 0f, float row = PropRow)
        {
            var prefab = Resources.Load<GameObject>("Props/" + resource);
            if (prefab == null) { Debug.LogWarning($"[S28Stage] no prop {resource}"); return; }
            // A wrapper carries the idle sway, so the prefab keeps the rotation its importer baked into the root.
            var wrap = new GameObject(resource).transform;
            wrap.SetParent(origin, false);
            var go = Instantiate(prefab, wrap);
            go.transform.localPosition = Vector3.zero;
            go.transform.localScale = Vector3.one;
            if (yaw != 0f) go.transform.localRotation = Quaternion.Euler(0f, yaw, 0f) * go.transform.localRotation;
            SetLayer(wrap.gameObject);
            var bounds = Bounds(go);
            float s = bounds.size.y > 0.001f ? height / bounds.size.y : 1f;
            go.transform.localScale = Vector3.one * s;
            bounds = Bounds(go);
            var target = OnCounter(new Vector2(xUnits * PxPerUnit, row));
            wrap.position += new Vector3(target.x - bounds.center.x, origin.position.y - bounds.min.y, target.z - bounds.center.z);
            foreach (var r in go.GetComponentsInChildren<Renderer>())
            {
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                r.receiveShadows = true;
            }
            bounds = Bounds(go);
            var blob = Shadow(bounds.center, Mathf.Max(bounds.size.x, bounds.size.z) * 1.1f, 0.30f);
            spawned.Add(blob.gameObject);
            sway.Add(wrap);
            spawned.Add(wrap.gameObject);
        }

        public void Hide()
        {
            Clear();
            if (cam != null) cam.enabled = false;
            if (view != null) view.enabled = false;
        }

        void Clear()
        {
            foreach (var go in spawned) if (go != null) DestroyNow(go);
            spawned.Clear();
            sway.Clear();
        }

        // ---------------------------------------------------------------- toys (host twins)

        /// <summary>A flat disc lying on the table: stains, plates, pads.</summary>
        public Transform AddDisc(Vector2 at, float sizePx, Color colour, float thickness = 0.05f)
        {
            var t = Primitive(PrimitiveType.Cylinder, colour, lit);
            t.localScale = new Vector3(sizePx / PxPerUnit, thickness, sizePx / PxPerUnit);
            Place(t, at);
            return Base(t, thickness * 0.5f + 0.005f, sizePx / PxPerUnit * 1.05f, 0.22f);
        }

        /// <summary>A squashed puff resting on the table: dust piles that must read as volume, not balls.</summary>
        public Transform AddBlob(Vector2 at, float sizePx, Color colour)
        {
            var t = Primitive(PrimitiveType.Sphere, colour, lit);
            float d = sizePx / PxPerUnit;
            t.localScale = new Vector3(d, d * 0.55f, d);
            Place(t, at);
            return Base(t, d * 0.22f, d * 1.05f, 0.38f);
        }

        /// <summary>A box for sortable items and pads, sitting on the table.</summary>
        public Transform AddBox(Vector2 at, Vector2 sizePx, Color colour)
        {
            var t = Primitive(PrimitiveType.Cube, colour, lit);
            t.localScale = new Vector3(sizePx.x / PxPerUnit, sizePx.y / PxPerUnit, 0.22f);
            // Anchor the base on the rect's bottom edge, so the body's screen centre lands on the rect's label.
            Place(t, at - new Vector2(0f, sizePx.y * 0.5f));
            return Base(t, sizePx.y / PxPerUnit * 0.5f, Mathf.Max(sizePx.x, sizePx.y) / PxPerUnit * 0.9f, 0.30f);
        }

        /// <summary>A wet smear lying on the table: flat, translucent, the window-wipe step.</summary>
        public Transform AddPane(Vector2 at, float sizePx, Color colour)
        {
            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = "Pane";
            quad.layer = StageLayer;
            var col = quad.GetComponent<Collider>();
            if (col != null) DestroyNow(col);
            var mr = quad.GetComponent<MeshRenderer>();
            mr.sharedMaterial = new Material(glass) { color = colour };
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
            quad.transform.SetParent(origin, false);
            quad.transform.localScale = new Vector3(sizePx / PxPerUnit, sizePx / PxPerUnit, 1f);
            quad.transform.localRotation = Quaternion.Euler(90f, 0f, 0f); // lie flat, normal up
            spawned.Add(quad);
            Place(quad.transform, at);
            return Base(quad.transform, 0.012f, sizePx / PxPerUnit * 0.9f, 0.10f);
        }

        /// <summary>A real slime rig from Resources/Characters, wrapped so the host can place it by its centre.</summary>
        public Transform AddSlime(Vector2 at, float sizePx, int variant)
        {
            string[] names = { "SK_enemy_slime_green", "SK_enemy_slime_mud", "SK_enemy_slime_dust", "SK_enemy_slime_dark" };
            var prefab = Resources.Load<GameObject>("Characters/" + names[Mathf.Abs(variant) % names.Length]);
            if (prefab == null) return AddBlob(at, sizePx, T.success);

            var wrap = new GameObject("Slime").transform;
            wrap.SetParent(origin, false);
            var body = Instantiate(prefab, wrap);
            SetLayer(body);
            var b = Bounds(body);
            float s = b.size.y > 0.001f ? sizePx / PxPerUnit / b.size.y : 1f;
            body.transform.localScale *= s; // the imported rig carries its own root scale (×100): multiply, never replace
            b = Bounds(body);
            // Ground the rig on its own feet, so whatever pivot the importer baked in cannot float it off the table.
            body.transform.position += new Vector3(wrap.position.x - b.center.x, wrap.position.y - b.min.y, wrap.position.z - b.center.z);
            foreach (var r in body.GetComponentsInChildren<Renderer>())
            {
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                r.receiveShadows = true;
            }
            spawned.Add(wrap.gameObject);
            Place(wrap, at);
            return Base(wrap, 0.002f, sizePx / PxPerUnit * 1.15f, 0.42f); // grounded on its feet, so no lift
        }

        /// <summary>A real prop used as a game piece: scaled to a height and centred on its play-area pixel.</summary>
        public Transform AddProp(string resource, Vector2 at, float height, float yaw = 0f)
        {
            var prefab = Resources.Load<GameObject>("Props/" + resource);
            if (prefab == null) return AddBox(at, new Vector2(height * PxPerUnit, height * PxPerUnit), Color.white);
            var wrap = new GameObject(resource).transform;
            wrap.SetParent(origin, false);
            var go = Instantiate(prefab, wrap);
            go.transform.localPosition = Vector3.zero;
            go.transform.localScale = Vector3.one;
            if (yaw != 0f) go.transform.localRotation = Quaternion.Euler(0f, yaw, 0f) * go.transform.localRotation;
            SetLayer(wrap.gameObject);
            var b = Bounds(go);
            go.transform.localScale = Vector3.one * (b.size.y > 0.001f ? height / b.size.y : 1f);
            b = Bounds(go);
            go.transform.position -= b.center - wrap.position; // centre the body on its wrapper, not on world zero
            foreach (var r in go.GetComponentsInChildren<Renderer>())
            {
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                r.receiveShadows = true;
            }
            b = Bounds(go);
            spawned.Add(wrap.gameObject);
            Place(wrap, at - new Vector2(0f, height * PxPerUnit * 0.5f));
            return Base(wrap, height * 0.5f, Mathf.Max(b.size.x, b.size.z) * 1.15f, 0.34f);
        }

        Transform Primitive(PrimitiveType type, Color colour, Material baseMat)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = type.ToString();
            go.layer = StageLayer;
            go.transform.SetParent(origin, false);
            var col = go.GetComponent<Collider>();
            if (col != null) DestroyNow(col);
            var mr = go.GetComponent<MeshRenderer>();
            mr.sharedMaterial = new Material(baseMat) { color = colour };
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            mr.receiveShadows = true;
            spawned.Add(go);
            return go.transform;
        }

        /// <summary>Keep a twin under its hitbox, resting on the table where that play-area pixel lands.</summary>
        public void Place(Transform twin, Vector2 anchored)
        {
            if (twin == null) return;
            var ts = twin.GetComponent<TwinScale>();
            var ground = OnCounter(anchored);
            twin.position = ground + Vector3.up * (ts != null ? ts.Lift : 0f);
            if (ts == null || ts.Shadow == null) return;
            float k = ts.Base.x > 0.0001f ? twin.localScale.x / ts.Base.x : 1f;
            float d = ts.ShadowSize * Mathf.Max(k, 0.01f);
            ts.Shadow.position = new Vector3(ground.x, origin.position.y + 0.008f, ground.z);
            ts.Shadow.localScale = new Vector3(d, d, 1f);
        }

        /// <summary>Grow/shrink a twin around the scale it was built at (grime thins as it lifts, liquid rises).</summary>
        public void SetSize(Transform twin, float factor)
        {
            if (twin == null) return;
            var twinScale = twin.GetComponent<TwinScale>();
            if (twinScale == null) return;
            twin.localScale = twinScale.Base * Mathf.Max(factor, 0.01f);
        }

        public void Tint(Transform twin, Color colour)
        {
            if (twin == null) return;
            foreach (var r in twin.GetComponentsInChildren<Renderer>())
            {
                var m = r.material;
                if (m.HasProperty("_Color")) m.color = colour;
            }
        }

        /// <summary>Point on the stage that lands on this play-area pixel, at the given camera depth.</summary>
        Vector3 At(Vector2 px, float depth) =>
            cam.transform.position + cam.transform.rotation * new Vector3(px.x / PxPerUnit, px.y / PxPerUnit, depth);

        /// <summary>Where this play-area pixel lands on the table: same pixel on screen, on the table plane.</summary>
        public Vector3 OnCounter(Vector2 px)
        {
            float y = px.y / PxPerUnit;
            return At(px, (CamUp + y * CosPitch) / SinPitch);
        }

        public void SetActive(Transform twin, bool on)
        {
            if (twin == null) return;
            if (twin.gameObject.activeSelf != on) twin.gameObject.SetActive(on);
            var ts = twin.GetComponent<TwinScale>();
            if (ts != null && ts.Shadow != null && ts.Shadow.gameObject.activeSelf != on) ts.Shadow.gameObject.SetActive(on);
        }

        public void Kill(Transform twin)
        {
            if (twin == null) return;
            var ts = twin.GetComponent<TwinScale>();
            if (ts != null && ts.Shadow != null) DestroyNow(ts.Shadow.gameObject);
            DestroyNow(twin.gameObject);
        }

        void Update()
        {
            if (!built || cam == null || !cam.enabled) return;
            float t = Time.unscaledTime;
            for (int i = 0; i < sway.Count; i++)
            {
                var tr = sway[i];
                if (tr == null) continue;
                tr.localRotation = Quaternion.Euler(0f, Mathf.Sin(t * 0.6f + i * 1.7f) * 1.6f, 0f);
            }
        }

        // ---------------------------------------------------------------- helpers

        /// <summary>A soft blob under a body. One unlit quad is enough to sit a prop on the table at card size.</summary>
        Transform Shadow(Vector3 ground, float diameter, float alpha)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.name = "ContactShadow";
            go.layer = StageLayer;
            DestroyNow(go.GetComponent<Collider>());
            go.transform.SetParent(origin, false);
            go.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            go.transform.position = new Vector3(ground.x, origin.position.y + 0.008f, ground.z);
            go.transform.localScale = new Vector3(diameter, diameter, 1f);
            var mr = go.GetComponent<MeshRenderer>();
            mr.sharedMaterial = shadow;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
            var block = new MaterialPropertyBlock();
            block.SetColor("_Color", new Color(0.30f, 0.25f, 0.24f, alpha));
            mr.SetPropertyBlock(block);
            return go.transform;
        }

        Transform Base(Transform twin, float lift, float shadowDiameter, float shadowAlpha)
        {
            if (twin == null) return null;
            var ts = twin.GetComponent<TwinScale>();
            if (ts == null) ts = twin.gameObject.AddComponent<TwinScale>();
            ts.Base = twin.localScale;
            ts.Lift = lift;
            ts.ShadowSize = shadowDiameter;
            ts.Shadow = Shadow(twin.position, shadowDiameter, shadowAlpha);
            spawned.Add(ts.Shadow.gameObject);
            return twin;
        }

        void SetLayer(GameObject go)
        {
            go.layer = StageLayer;
            foreach (Transform child in go.transform) SetLayer(child.gameObject);
        }

        static Bounds Bounds(GameObject go)
        {
            var rends = go.GetComponentsInChildren<Renderer>();
            if (rends.Length == 0) return new Bounds(go.transform.position, Vector3.one);
            var b = rends[0].bounds;
            for (int i = 1; i < rends.Length; i++) b.Encapsulate(rends[i].bounds);
            return b;
        }

        static void DestroyNow(Object o)
        {
            if (o == null) return;
            if (Application.isPlaying) Destroy(o);
            else DestroyImmediate(o);
        }
    }

    /// <summary>Remembers the scale a twin was built at, so SetSize can grow it without drifting, how far it sits
    /// above the table so Place can rest it on the surface, and the blob shadow that follows it.</summary>
    public class TwinScale : MonoBehaviour
    {
        public Vector3 Base = Vector3.one;
        public float Lift;
        public Transform Shadow;
        public float ShadowSize = 1f;
    }
}
