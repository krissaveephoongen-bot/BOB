// Battle of the Brooms: the map props were imported with an inverted-hull child named "Outline" (BobToon.Outline)
// before Resources/Materials/M_BOB_Outline existed, so every one of those children carries a null material. In the
// scene view they draw nothing; in play mode static batching folds them into one combined mesh and the slots with no
// material come out as Unity's error magenta — on the island that hull spans the whole top of the map (docs 24 section
// 15.5 "ค้าง (ภาพ)"). This menu switches the dead renderers off, which also keeps batching from picking them up. Map
// surfaces already get their line from the BOB/CurvedWorld outline pass.
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Bob.EditorTools
{
    public static class BobOutlineFix
    {
        [MenuItem("Bob/Fix Null Outline Renderers")]
        public static void Fix()
        {
            var report = new StringBuilder();
            int disabled = 0, scanned = 0, kept = 0;

            foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Resources/Props" }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var root = PrefabUtility.LoadPrefabContents(path);
                bool changed = false;
                foreach (var mr in root.GetComponentsInChildren<MeshRenderer>(true))
                {
                    if (mr.name != "Outline") continue;
                    scanned++;
                    var mat = mr.sharedMaterial;
                    if (mat != null && mat.shader != null) { kept++; continue; }
                    if (!mr.enabled) continue;
                    mr.enabled = false;
                    changed = true;
                    disabled++;
                }
                if (changed) PrefabUtility.SaveAsPrefabAsset(root, path);
                PrefabUtility.UnloadPrefabContents(root);
            }

            report.AppendLine($"[OutlineFix] scanned {scanned} Outline renderer(s): disabled {disabled}, kept {kept}");
            foreach (var scene in new[] { "Assets/Scenes/TaskRace.unity", "Assets/Scenes/TaskRace_Duo.unity", "Assets/Scenes/TaskRace_Team.unity" })
            {
                var s = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(scene, UnityEditor.SceneManagement.OpenSceneMode.Single);
                int n = 0;
                foreach (var mr in Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
                {
                    if (mr == null || mr.name != "Outline") continue;
                    if (mr.sharedMaterial != null && mr.sharedMaterial.shader != null) continue;
                    if (!mr.enabled) continue;
                    mr.enabled = false;
                    n++;
                }
                if (n > 0) UnityEditor.SceneManagement.EditorSceneManager.SaveScene(s);
                report.AppendLine($"[OutlineFix] {scene}: disabled {n} leftover(s) outside the prefab");
            }

            Debug.Log(report.ToString());
            AssetDatabase.SaveAssets();
        }
    }
}
