// Battle of the Brooms: turns the cleaned FBX files from art/tools/blender/bob_prop_cleanup.py into prefabs.
// Props (menu Bob > Art > Build Prop Prefabs) go to Resources/Props, characters (Bob > Art > Build Character Prefabs)
// to Resources/Characters. Every model is one merged mesh with palette UVs, pivot at the bottom centre and a game
// height in metres, so a prefab is dropped at y = 0 as is.
using System.IO;
using System.Linq;
using Bob.Modular;
using UnityEditor;
using UnityEngine;

namespace Bob.EditorTools
{
    public static class BobPropImporter
    {
        const string MatPath = "Assets/Resources/Materials/M_BOB_Props.mat";

        [MenuItem("Bob/Art/Build Prop Prefabs")]
        public static void BuildProps() => Build("Assets/Art/Props/", "Assets/Resources/Props", false);

        [MenuItem("Bob/Art/Build Character Prefabs")]
        public static void BuildCharacters() => Build("Assets/Art/Characters/", "Assets/Resources/Characters", false);

        /// <summary>Task Race map pieces (docs 24): BOB/CurvedWorld already carries an outline pass, so no hull child.</summary>
        [MenuItem("Bob/Art/Build Map Prefabs")]
        public static void BuildMapProps() => Build("Assets/Art/Map/", "Assets/Resources/Props", true);

        static void Build(string dir, string outDir, bool curved)
        {
            if (!AssetDatabase.IsValidFolder(dir.TrimEnd('/'))) { Debug.LogWarning("Bob: no folder " + dir); return; }
            Directory.CreateDirectory(outDir);
            Directory.CreateDirectory("Assets/Resources/Materials");
            var pal = AssetDatabase.LoadAssetAtPath<Texture2D>(dir + "T_BOB_Palette.png");
            if (pal != null)
            {
                var ti = (TextureImporter)AssetImporter.GetAtPath(dir + "T_BOB_Palette.png");
                ti.filterMode = FilterMode.Point; ti.textureCompression = TextureImporterCompression.Uncompressed; ti.mipmapEnabled = false; ti.SaveAndReimport();
            }
            var matPath = curved ? "Assets/Resources/Materials/M_BOB_CurvedMap.mat" : MatPath;
            var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (mat == null)
            {
                var shader = curved ? Resources.Load<Shader>("Shaders/BobCurvedWorld") ?? Shader.Find("BOB/CurvedWorld") : Shader.Find("BOB/ToonLit");
                if (shader == null) { Debug.LogError("Bob: shader not found for " + matPath); return; }
                mat = new Material(shader) { color = Color.white };
                AssetDatabase.CreateAsset(mat, matPath);
            }
            if (pal != null) mat.mainTexture = pal;

            var fbx = AssetDatabase.FindAssets("t:Model", new[] { dir.TrimEnd('/') })
                .Select(g => AssetDatabase.GUIDToAssetPath(g)).Where(p => p.EndsWith(".fbx")).OrderBy(p => p).ToArray();
            int made = 0;
            foreach (var path in fbx)
            {
                var imp = (ModelImporter)AssetImporter.GetAtPath(path);
                if (imp == null) continue;
                imp.materialImportMode = ModelImporterMaterialImportMode.None;
                imp.animationType = ModelImporterAnimationType.None;
                imp.importAnimation = false;
                imp.useFileScale = true;
                imp.globalScale = 1f;
                imp.SaveAndReimport();

                var model = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (model == null) { Debug.LogError("Bob: cannot load " + path); continue; }
                var inst = (GameObject)PrefabUtility.InstantiatePrefab(model);
                foreach (var r in inst.GetComponentsInChildren<Renderer>())
                {
                    r.sharedMaterials = r.sharedMaterials.Select(_ => mat).ToArray();
                    var outline = BobToon.Outline(r.gameObject, 2f);
                    // Prefabs cannot retain the temporary material created by Outline.
                    if (outline != null)
                        outline.GetComponent<Renderer>().sharedMaterial = Resources.Load<Material>("Materials/M_BOB_Outline");
                }
                foreach (var t in inst.GetComponentsInChildren<Transform>())
                    GameObjectUtility.SetStaticEditorFlags(t.gameObject, StaticEditorFlags.BatchingStatic);
                string outPath = outDir + "/" + Path.GetFileNameWithoutExtension(path) + ".prefab";
                PrefabUtility.SaveAsPrefabAsset(inst, outPath);
                Object.DestroyImmediate(inst);
                made++;
            }
            AssetDatabase.SaveAssets(); AssetDatabase.Refresh();
            Debug.Log($"Bob: built {made} prefabs under {outDir}");
        }
    }
}
