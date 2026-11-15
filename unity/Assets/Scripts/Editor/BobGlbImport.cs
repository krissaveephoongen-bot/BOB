// Battle of the Brooms: the .glb drop folder. Unity 6 has no glTF importer of its own, so the project carries
// com.unity.cloud.gltfast (Packages/manifest.json); every file in Assets/TripoRaw becomes a prefab with its meshes,
// materials and textures as sub-assets. Menu: Bob > Art > Reimport GLB (also run headless:
//   Unity.exe -batchmode -quit -projectPath unity -executeMethod Bob.EditorTools.BobGlbImport.ReimportAll)
#if UNITY_EDITOR
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Bob.EditorTools
{
    public static class BobGlbImport
    {
        public const string Drop = "Assets/TripoRaw/";

        [MenuItem("Bob/Art/Reimport GLB")]
        public static void ReimportAll()
        {
            if (AssetDatabase.IsValidFolder(Drop.TrimEnd('/')))
                AssetDatabase.ImportAsset(Drop.TrimEnd('/'), ImportAssetOptions.ImportRecursive | ImportAssetOptions.ForceUpdate);

            var files = Directory.Exists(Drop)
                ? Directory.GetFiles(Drop, "*.glb", SearchOption.AllDirectories).Select(p => p.Replace('\\', '/')).OrderBy(p => p).ToArray()
                : new string[0];

            int ok = 0, empty = 0;
            foreach (var path in files)
            {
                var assets = AssetDatabase.LoadAllAssetsAtPath(path);
                var meshes = assets.OfType<Mesh>().ToArray();
                var prefabs = assets.OfType<GameObject>().ToArray();
                if (meshes.Length == 0 && prefabs.Length == 0)
                {
                    empty++;
                    Debug.LogWarning($"[BobGlb] {path} did not import: is com.unity.cloud.gltfast resolved? (Window > Package Manager)");
                    continue;
                }
                ok++;
                long tris = meshes.Sum(m => (long)m.triangles.Length / 3);
                long verts = meshes.Sum(m => (long)m.vertexCount);
                Debug.Log($"[BobGlb] {Path.GetFileName(path)}: {meshes.Length} mesh(es), {verts} verts, {tris} tris, {prefabs.Length} prefab(s)");
            }
            Debug.Log($"[BobGlb] {ok}/{files.Length} clip(s) imported, {empty} still unreadable, from {Drop}");
        }
    }
}
#endif
