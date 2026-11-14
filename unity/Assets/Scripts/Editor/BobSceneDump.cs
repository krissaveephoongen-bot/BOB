// Inventory of the Task Race scene: every enabled renderer over 3 u, everything that reaches above the walk plane, and
// the bridge group's imported pieces. Written for the "which model draws that pale slab" passes; keep it as the quick
// way to answer that question again.
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Bob.EditorTools
{
    public static class BobSceneDump
    {
        [MenuItem("Tools/BOB/Dump Task Race Renderers")]
        public static void Run()
        {
            EditorSceneManager.OpenScene(TaskRaceSceneBuilder.ScenePath);
            var map = GameObject.Find("TaskRaceMap");
            foreach (var r in Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Exclude))
            {
                var b = r.bounds;
                if (r.enabled && (b.size.x > 3f || b.size.z > 3f))
                    Debug.Log($"[Dump] big {r.name} on {r.transform.parent?.name} mat={(r.sharedMaterial ? r.sharedMaterial.name : "null")} " +
                              $"active={r.gameObject.activeInHierarchy} c=({b.center.x:0.0},{b.center.y:0.0},{b.center.z:0.0}) " +
                              $"s=({b.size.x:0.0},{b.size.y:0.0},{b.size.z:0.0})");
            }
            foreach (var r in Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Exclude))
            {
                var b = r.bounds;
                if (!r.enabled || b.max.y < 0.8f) continue;
                Debug.Log($"[Dump] tall {r.name} on {r.transform.parent?.name} mat={(r.sharedMaterial ? r.sharedMaterial.name : "null")} " +
                          $"c=({b.center.x:0.0},{b.center.y:0.0},{b.center.z:0.0}) s=({b.size.x:0.0},{b.size.y:0.0},{b.size.z:0.0})");
            }
            var bridges = map.transform.Find("Bridges_Group");
            foreach (var r in bridges.GetComponentsInChildren<Renderer>(true))
                Debug.Log($"[Dump] bridges/{r.name} enabled={r.enabled} active={r.gameObject.activeInHierarchy} " +
                          $"mat={(r.sharedMaterial ? r.sharedMaterial.name : "null")}");
        }
    }
}
