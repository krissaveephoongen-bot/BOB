// Battle of the Brooms: creates the two shared toon material assets under Assets/Resources/Materials (menu Bob > Art > Build Toon Materials).
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Bob.EditorTools
{
    public static class BobToonSetup
    {
        [MenuItem("Bob/Art/Build Toon Materials")]
        public static void Build()
        {
            Directory.CreateDirectory("Assets/Resources/Materials");
            Make("Assets/Resources/Materials/M_BOB_Toon.mat", "BOB/ToonLit");
            Make("Assets/Resources/Materials/M_BOB_Outline.mat", "BOB/Outline");
            AssetDatabase.SaveAssets(); AssetDatabase.Refresh();
            Debug.Log("Bob: toon materials built.");
        }

        static void Make(string path, string shaderName)
        {
            var sh = Shader.Find(shaderName);
            if (sh == null) { Debug.LogError("Bob: shader not found: " + shaderName); return; }
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null) { m = new Material(sh); AssetDatabase.CreateAsset(m, path); } else m.shader = sh;
            EditorUtility.SetDirty(m);
        }
    }
}
