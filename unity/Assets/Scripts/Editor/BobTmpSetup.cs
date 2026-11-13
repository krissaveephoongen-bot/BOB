// Battle of the Brooms: TextMeshPro setup. Step 1 imports TMP Essential Resources (shaders, default settings),
// step 2 builds two SDF font assets from the bundled Thai fonts and pre-fills their atlases with Thai + Latin glyphs
// so the first frame never has to rasterise them (important on low-end Android).
//   Unity.exe -batchmode -quit -projectPath unity -executeMethod Bob.EditorTools.BobTmpSetup.ImportEssentials
//   Unity.exe -batchmode -quit -projectPath unity -executeMethod Bob.EditorTools.BobTmpSetup.BuildFontAssets
using System.IO;
using System.Linq;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace Bob.EditorTools
{
    public static class BobTmpSetup
    {
        const string FontDir = "Assets/Resources/Fonts/";
        const string OutDir = "Assets/Resources/Fonts/TMP/";

        [MenuItem("Bob/TextMeshPro/1 Import Essentials")]
        public static void ImportEssentials()
        {
            if (Directory.Exists("Assets/TextMesh Pro")) { Debug.Log("Bob: TMP Essentials already imported."); return; }
            var pkg = Directory.GetDirectories("Library/PackageCache", "com.unity.ugui@*").FirstOrDefault();
            if (pkg == null) { Debug.LogError("Bob: com.unity.ugui not found in the package cache."); return; }
            var path = Path.Combine(pkg, "Package Resources", "TMP Essential Resources.unitypackage");
            if (!File.Exists(path)) { Debug.LogError("Bob: " + path + " not found."); return; }
            if (Application.isBatchMode)
            {
                // the import finishes asynchronously: stay alive until Unity says it is done (run WITHOUT -quit)
                AssetDatabase.importPackageCompleted += n => { Debug.Log("Bob: imported " + n); EditorApplication.Exit(0); };
                AssetDatabase.importPackageFailed += (n, e) => { Debug.LogError("Bob: import failed " + n + " " + e); EditorApplication.Exit(1); };
            }
            AssetDatabase.ImportPackage(path, false);
            Debug.Log("Bob: importing TMP Essential Resources from " + path);
        }

        static string Glyphs()
        {
            var sb = new StringBuilder();
            for (int c = 0x20; c <= 0x7E; c++) sb.Append((char)c);          // Latin, digits, punctuation
            for (int c = 0x0E01; c <= 0x0E5B; c++) sb.Append((char)c);      // Thai
            sb.Append("×▲▼●○★☆✓✕→←↑↓…·•–—“”‘’฿ ");
            return sb.ToString();
        }

        [MenuItem("Bob/TextMeshPro/2 Build Font Assets")]
        public static void BuildFontAssets()
        {
            Directory.CreateDirectory(OutDir);
            foreach (var name in new[] { "IBMPlexSansThai-Medium", "Mitr-Medium" })
            {
                var font = AssetDatabase.LoadAssetAtPath<Font>(FontDir + name + ".ttf");
                if (font == null) { Debug.LogError("Bob: missing " + FontDir + name + ".ttf"); continue; }
                var assetPath = OutDir + name + " SDF.asset";
                AssetDatabase.DeleteAsset(assetPath);
                var fa = TMP_FontAsset.CreateFontAsset(font, 72, 8, GlyphRenderMode.SDFAA, 2048, 2048, AtlasPopulationMode.Dynamic, true);
                if (fa == null) { Debug.LogError("Bob: could not create a font asset for " + name); continue; }
                fa.name = name + " SDF";
                AssetDatabase.CreateAsset(fa, assetPath);
                fa.atlasTextures[0].name = name + " Atlas";
                fa.material.name = name + " Material";
                AssetDatabase.AddObjectToAsset(fa.atlasTextures[0], fa);
                AssetDatabase.AddObjectToAsset(fa.material, fa);
                fa.TryAddCharacters(Glyphs(), out var missing);
                if (!string.IsNullOrEmpty(missing)) Debug.LogWarning("Bob: " + name + " has no glyph for: " + missing);
                EditorUtility.SetDirty(fa); EditorUtility.SetDirty(fa.material);
                Debug.Log($"Bob: built {assetPath}: {fa.characterTable.Count} characters, {fa.atlasTextureCount} atlas page(s).");
            }
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }
}
