// Look-dev pass for the pastel island skin (docs 24 §15.13, owner's reference art/reference/islands). It opens the
// solo Task Race scene, restyles it in memory and writes three renders, then exits WITHOUT saving the scene: nothing
// about the layout, the stations or the rules changes, so the owner can judge the look before any of it is committed.
//
// What the skin does, relative to the scene as built:
//   background  cream paper instead of the gradient sky and the blue fog
//   water       one flat matte sheet instead of the cloud sea
//   island      grass / soil / rock become flat reference colours with no strata bands
//   bridge      bare planks (BobBridgeDressing.rails = false)
//
// Unity: Bob > Render Island Skin (look-dev)
// Headless: -executeMethod Bob.EditorTools.BobIslandSkinDev.Run   (log: Logs/skin-dev.log)
using System.IO;
using Bob.TaskRace;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace Bob.EditorTools
{
    public static class BobIslandSkinDev
    {
        const string ScenePath = "Assets/Scenes/TaskRace.unity";

        // Sampled from art/reference/islands (docs 24 §15.13).
        static readonly Color Cream = Hex(0xFB, 0xE6, 0xCC);
        static readonly Color Wall = Hex(0xF6, 0xE7, 0xCE);
        static readonly Color Counter = Hex(0xF7, 0xEB, 0xD6);
        static readonly Color CounterBody = Hex(0xD9, 0xBF, 0x98);
        static readonly Color Grass = Hex(0xA3, 0xAC, 0x58);
        static readonly Color Soil = Hex(0xA5, 0x6F, 0x39);
        static readonly Color Sand = Hex(0xC0, 0x8B, 0x5A);
        static readonly Color Water = Hex(0x75, 0xC1, 0xD9);
        static readonly Color Channel = Hex(0xD6, 0xE5, 0xE1);
        static readonly Color Falls = Hex(0xBF, 0xE4, 0xEC);
        static readonly Color PaleRock = Hex(0xE4, 0xDE, 0xC8);
        static readonly Color Wood = Hex(0xD3, 0xC9, 0x93);

        static Texture2D flatWhite;

        [MenuItem("Bob/Render Island Skin (look-dev)")]
        public static void Render() => Run();

        public static void Run()
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            var map = GameObject.Find("TaskRaceMap");
            if (map == null) { Debug.LogError("[SkinDev] TaskRaceMap not found in " + ScenePath); return; }

            flatWhite = new Texture2D(1, 1, TextureFormat.RGBA32, false) { hideFlags = HideFlags.DontSave };
            flatWhite.SetPixel(0, 0, Color.white);
            flatWhite.Apply();

            int skyOff = Off<BobSkyBackdrop>();
            int seaOff = Off<BobCloudSea>();
            int tinted = Tint("TaskRaceMap/TeamA/Field", Grass, 0.02f)
                       + Tint("TaskRaceMap/TeamB/Field", Grass, 0.02f)
                       + Tint("TaskRaceMap/TeamA/Field_Soil", Soil, 0f)
                       + Tint("TaskRaceMap/TeamB/Field_Soil", Soil, 0f)
                       + Tint("TaskRaceMap/TeamA/Island", Sand, 0f)
                       + Tint("TaskRaceMap/TeamB/Island", Sand, 0f);
            // The dressings, the yard wall and the waterfall are built at runtime *without* a parent, so they are
            // invisible to a search under TaskRaceMap: match by name anywhere in the scene, ancestors included.
            // A multiply tint keeps the daisies white and the stones grey; a flat tint kills that variation.
            int dressing = TintScene("(Dressing)", Hex(0xA0, 0xA9, 0x5C), 0.04f, true);
            int walls = TintScene("BackdropWall", Wall, 0f, true)
                      + TintScene("WallSeg", Wall, 0f, true)
                      + TintScene("WallCurb", Wall, 0f, true);
            int falls = TintScene("(Waterfall)", Falls, 0f, true);
            int deck = TintScene("(Bridge)", Wood, 0f, true)
                     + TintScene("Bridge_", Wood, 0f, true);
            int rocks = TintScene("SM_rocks_grass", PaleRock, 0f, true);
            int channel = TintScene("RiverChannel", Channel, 0f, true);
            int bodies = TintScene("Counter", CounterBody, 0f, true);
            int tops = TintScene("CounterTop", Counter, 0f, true)
                     + TintScene("Counter_Spine", Counter, 0f, true);
            int barriers = Hide<BobBridgeBarrier>();
            int bareBridges = NoRails();

            RenderSettings.fog = false;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = Hex(0xF2, 0xEC, 0xDE);
            RenderSettings.ambientIntensity = 1f;

            var water = AddWater();
            HideHud();

            var hero = Object.FindFirstObjectByType<BobTaskRaceHero>();
            Vector3 focus = hero != null ? hero.transform.position : new Vector3(0f, 0f, 0f);
            Shot("Logs/skin-play.png", focus + new Vector3(0f, 6.2f, -5.2f), focus + new Vector3(0f, 0.72f, 0f), 40f);
            Shot("Logs/skin-high.png", new Vector3(0f, 21f, -17f), new Vector3(0f, 1f, 0f), 34f);
            Shot("Logs/skin-wide.png", new Vector3(0f, 12f, -26f), new Vector3(0f, 1.2f, 0f), 40f);

            Debug.Log($"[SkinDev] cream background (sky objects off {skyOff}, cloud seas off {seaOff}, fog off), " +
                      $"{tinted} slab(s) + {dressing} dressing + {walls} wall + {falls} waterfall + {rocks} cliff rock + " +
                      $"{channel} river channel + " +
                      $"{bodies} counter body + {tops} counter top + {deck} bridge part tinted, " +
                      $"{barriers} bridge barrier(s) hidden, {bareBridges} bridge(s) without rails, water channel at " +
                      $"y {water:F2}. Scene NOT saved; 3 renders in Logs.");
        }

        static int Off<T>() where T : Behaviour
        {
            int n = 0;
            foreach (var c in Object.FindObjectsByType<T>(FindObjectsInactive.Exclude)) { c.enabled = false; n++; }
            return n;
        }

        /// <summary>Switch the whole object off, not just the component: these build a hidden child mesh that would stay.</summary>
        static int Hide<T>() where T : Behaviour
        {
            int n = 0;
            foreach (var c in Object.FindObjectsByType<T>(FindObjectsInactive.Exclude)) { c.gameObject.SetActive(false); n++; }
            return n;
        }

        /// <summary>Flat colour for a palette-slab: swap the atlas for a white texel and tint, so the sample reads true.</summary>
        static int Tint(string path, Color colour, float mottle)
        {
            var go = GameObject.Find(path);
            if (go == null) { Debug.LogWarning("[SkinDev] missing " + path); return 0; }
            var block = new MaterialPropertyBlock();
            int n = 0;
            foreach (var r in go.GetComponentsInChildren<Renderer>(true))
            {
                Apply(r, block, colour, mottle);
                n++;
            }
            return n;
        }

        /// <summary>
        /// Tint every renderer whose own name or any ancestor's name contains <paramref name="contains"/>.
        /// The runtime dressings and the waterfalls are unparented and flagged HideAndDontSave, which keeps them out
        /// of FindObjectsByType entirely - Resources.FindObjectsOfTypeAll is the only way to reach them.
        /// </summary>
        static int TintScene(string contains, Color colour, float mottle, bool flat)
        {
            var block = new MaterialPropertyBlock();
            int n = 0;
            foreach (var r in Resources.FindObjectsOfTypeAll<Renderer>())
            {
                if (r == null || EditorUtility.IsPersistent(r)) continue;
                bool hit = false;
                for (var t = r.transform; t != null && !hit; t = t.parent)
                    hit = t.name.IndexOf(contains, System.StringComparison.Ordinal) >= 0;
                if (!hit) continue;
                if (flat) Apply(r, block, colour, mottle);
                else Multiply(r, block, colour, mottle);
                n++;
            }
            return n;
        }

        /// <summary>Keep the palette's own variation (flowers, stones) and shift the hue: dressing lawn to olive.</summary>
        static void Multiply(Renderer r, MaterialPropertyBlock block, Color colour, float mottle)
        {
            r.GetPropertyBlock(block);
            block.SetColor("_Color", colour);
            block.SetFloat("_Mottle", mottle);
            r.SetPropertyBlock(block);
        }

        static void Apply(Renderer r, MaterialPropertyBlock block, Color colour, float mottle)
        {
            r.GetPropertyBlock(block);
            block.SetTexture("_MainTex", flatWhite);
            block.SetColor("_Color", colour);
            block.SetFloat("_Strata", 0f);
            block.SetFloat("_Mottle", mottle);
            block.SetFloat("_DirtAmount", 0f);
            block.SetColor("_CliffShade", Color.Lerp(colour, Color.black, 0.18f));
            r.SetPropertyBlock(block);
        }

        static int NoRails()
        {
            int n = 0;
            foreach (var b in Object.FindObjectsByType<BobBridgeDressing>(FindObjectsInactive.Exclude))
            {
                if (!b.rails) continue;
                b.rails = false;
                b.Build();
                n++;
            }
            return n;
        }

        /// <summary>
        /// The reference only shows water in the notch between the twin islands (plus the falls), on cream paper —
        /// so the sheet is a narrow channel down the bridge axis, not a sea around the whole map.
        /// </summary>
        static float AddWater()
        {
            var sea = Object.FindFirstObjectByType<BobCloudSea>();
            float level = sea != null ? sea.transform.position.y + sea.seaLevel : -5.5f;

            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = "SkinWater";
            Object.DestroyImmediate(quad.GetComponent<Collider>());
            quad.transform.SetPositionAndRotation(new Vector3(0f, level, 0f), Quaternion.Euler(90f, 0f, 0f));
            quad.transform.localScale = new Vector3(6f, 8f, 1f);
            var mat = new Material(Shader.Find("BOB/Unlit")) { hideFlags = HideFlags.DontSave };
            mat.SetColor("_Color", Water);
            var mr = quad.GetComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = ShadowCastingMode.Off;
            mr.receiveShadows = false;
            return level;
        }

        static void HideHud()
        {
            foreach (var name in new[] { "TaskRaceHUD" })
            {
                var go = GameObject.Find(name);
                if (go != null) go.SetActive(false);
            }
        }

        /// <summary>Same capture path as TaskRaceVerify.Shot, but the sky is the camera's own cream clear colour.</summary>
        static void Shot(string file, Vector3 eye, Vector3 look, float fov)
        {
            int w = 1600, h = 900;
            ViewSize(out w, out h);

            var go = new GameObject("SkinCam");
            go.SetActive(false);
            var cam = go.AddComponent<Camera>();
            var main = Camera.main;
            if (main != null) cam.CopyFrom(main);
            var post = go.AddComponent<BobPostGrade>();
            post.vignette = 0.05f;
            post.saturation = 1.04f;
            post.contrast = 1f;
            go.SetActive(true);

            cam.enabled = false;
            cam.transform.position = eye;
            cam.transform.LookAt(look);
            cam.fieldOfView = fov;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Cream;
            cam.aspect = (float)w / h;
            QualitySettings.shadowDistance = 45f;

            var rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32) { antiAliasing = 2 };
            cam.targetTexture = rt;
            BobMinigameStage.RenderForCapture();
            cam.Render();

            RenderTexture.active = rt;
            var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
            tex.Apply();
            Directory.CreateDirectory("Logs");
            var png = tex.EncodeToPNG();
            if (w != 1600 || h != 900)
                file = Path.Combine(Path.GetDirectoryName(file) ?? "Logs",
                    Path.GetFileNameWithoutExtension(file) + "-" + w + "x" + h + Path.GetExtension(file));
            try { File.WriteAllBytes(Path.GetFullPath(file), png); }
            catch (IOException)
            {
                file = Path.Combine(Path.GetDirectoryName(file) ?? "Logs",
                    Path.GetFileNameWithoutExtension(file) + "-new" + Path.GetExtension(file));
                File.WriteAllBytes(Path.GetFullPath(file), png);
            }
            RenderTexture.active = null;
            cam.targetTexture = null;
            Object.DestroyImmediate(rt);
            Object.DestroyImmediate(tex);
            Object.DestroyImmediate(go);
            Debug.Log("[SkinDev] render written to " + file);
        }

        static void ViewSize(out int w, out int h)
        {
            w = 1600; h = 900;
            var v = System.Environment.GetEnvironmentVariable("BOB_VIEW");
            if (string.IsNullOrEmpty(v)) return;
            var wh = v.Split('x');
            if (wh.Length == 2 && int.TryParse(wh[0], out var vw) && int.TryParse(wh[1], out var vh) && vw > 0 && vh > 0)
            {
                w = vw; h = vh;
            }
        }

        static Color Hex(int r, int g, int b)
        {
            Color c = new Color32((byte)r, (byte)g, (byte)b, 255);
            return QualitySettings.activeColorSpace == ColorSpace.Linear ? c.linear : c;
        }
    }
}
